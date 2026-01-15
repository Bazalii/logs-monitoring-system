using System.Globalization;
using System.Text.RegularExpressions;
using Logly.RawLogsHandler.Domain.Models.Parsing;
using Logly.RawLogsHandler.Infrastructure.Helpers;

namespace Logly.RawLogsHandler.Domain.Services.LogParsers.Implementations;

public sealed partial class ClfParser : IParser
{
    private static readonly Regex Clf = new(
        @"^(?<host>\S+)\s+(?<ident>\S+)\s+(?<authuser>\S+)\s+\[(?<date>[^\]]+)\]\s+""(?<request>[^""]*)""\s+(?<status>\d{3})\s+(?<bytes>\S+)\s*(?<rest>.*)$",
        RegexOptions.Compiled);

    public LogEntry Parse(string raw)
    {
        var clfRegexMatch = Clf.Match(raw.Trim());
        if (clfRegexMatch.Success is false)
        {
            throw new ArgumentException("Invalid Clf format", nameof(raw));
        }

        var receivedAt = DateTimeOffset.UtcNow;
        var createdAt = ParseClfDate(clfRegexMatch.Groups["date"].Value) ?? receivedAt;

        var host = clfRegexMatch.Groups["host"].Value;
        var request = clfRegexMatch.Groups["request"].Value;

        ushort.TryParse(
            clfRegexMatch.Groups["status"].Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var status);

        var level = status >= 500 ? "Error"
            : status >= 400 ? "Warning"
            : "Info";

        string? customSource = null;
        string? customEnv = null;

        var payload = new Dictionary<string, object?>
        {
            ["http_status_code"] = status,
        };

        var userId = TryParseUserId(clfRegexMatch.Groups["authuser"].Value);
        if (userId is not null)
        {
            payload["user_id"] = userId;
        }

        var ident = NormalizeNil(clfRegexMatch.Groups["ident"].Value);
        if (ident is not null)
        {
            payload["ident"] = ident;
        }

        var errorType = status >= 500 ? "Http5xx" : status >= 400 ? "Http4xx" : null;
        if (errorType is not null)
        {
            payload["error_type"] = errorType;
        }

        var rest = clfRegexMatch.Groups["rest"].Value.Trim();
        if (string.IsNullOrEmpty(rest) is false)
        {
            var pairs = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var idx = pair.IndexOf('=');
                if (idx > 0 && idx < pair.Length - 1)
                {
                    var key = pair[..idx];
                    var value = pair[(idx + 1)..];
                    if (key.Equals("service", StringComparison.OrdinalIgnoreCase)
                        || key.Equals("source", StringComparison.OrdinalIgnoreCase))
                    {
                        customSource = value;
                        continue;
                    }

                    if (key.Equals("env", StringComparison.OrdinalIgnoreCase)
                        || key.Equals("environment", StringComparison.OrdinalIgnoreCase))
                    {
                        customEnv = value;
                        continue;
                    }

                    payload[key] = value;
                }
            }
        }

        var source = customSource ?? "web-access";
        var env = customEnv ?? "unknown";

        var id = LogIdGenerator.GenerateStableUInt64(
            $"{source}|{host}|{env}|{createdAt:O}|{request}|{raw}");

        return new LogEntry(
            id,
            createdAt,
            receivedAt,
            level,
            source,
            host,
            env,
            request,
            payload.AsReadOnly());
    }

    private static string? NormalizeNil(string s) => s == "-" ? null : s;

    private static ulong? TryParseUserId(string authUser)
    {
        if (authUser == "-" || string.IsNullOrWhiteSpace(authUser))
        {
            return null;
        }

        return ulong.TryParse(authUser, NumberStyles.Integer, CultureInfo.InvariantCulture, out var u) ? u : null;
    }

    private static DateTimeOffset? ParseClfDate(string s)
    {
        if (DateTimeOffset.TryParseExact(
                s,
                "dd/MMM/yyyy:HH:mm:ss zzz",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dto))
        {
            return dto.ToUniversalTime();
        }

        var fixedTz = FixOffset(s);
        if (fixedTz is not null && DateTimeOffset.TryParseExact(
                fixedTz,
                "dd/MMM/yyyy:HH:mm:ss zzz",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dto2))
        {
            return dto2.ToUniversalTime();
        }

        return null;
    }

    private static string? FixOffset(string input)
    {
        var m = OffsetRegex().Match(input);

        return m.Success is false
            ? null
            : $"{m.Groups[1].Value} {m.Groups[2].Value}:{m.Groups[3].Value}";
    }

    [GeneratedRegex(@"^(.*)\s([+-]\d{2})(\d{2})$")]
    private static partial Regex OffsetRegex();
}