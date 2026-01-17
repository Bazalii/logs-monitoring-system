using System.Globalization;
using System.Text.RegularExpressions;
using Logly.RawLogsHandler.Domain.Models.Parsing;
using Logly.RawLogsHandler.Infrastructure.Helpers;

namespace Logly.RawLogsHandler.Domain.Services.LogParsers.Implementations;

public sealed class SyslogParser : IParser
{
    // <PRI>VERSION TIMESTAMP HOST APP PROCID MSGID [SD] MSG
    private static readonly Regex Header = new(
        @"^\<(?<pri>\d{1,3})\>(?<ver>\d)\s+(?<ts>\S+)\s+(?<host>\S+)\s+(?<app>\S+)\s+(?<proc>\S+)\s+(?<msgid>\S+)\s+(?<rest>.*)$",
        RegexOptions.Compiled);

    private static readonly Regex SdBlock = new(@"\[(?<content>[^\]]*)\]", RegexOptions.Compiled);

    private static readonly Regex SdParam = new(
        @"(?<k>[A-Za-z0-9_\-\.]+)=""(?<v>(?:\\.|[^""])*)""", RegexOptions.Compiled);

    public LogEntry Parse(string raw)
    {
        var m = Header.Match(raw.Trim());
        if (m.Success is false)
        {
            throw new ArgumentException($"Invalid log entry format: {raw}");
        }

        var receivedAt = DateTimeOffset.UtcNow;

        var pri = int.Parse(m.Groups["pri"].Value, CultureInfo.InvariantCulture);
        var severity = pri % 8;
        var level = SeverityToLevel(severity);

        var tsStr = m.Groups["ts"].Value;
        var createdAt = ParseSyslogTimestamp(tsStr) ?? receivedAt;

        var host = NormalizeNilValue(m.Groups["host"].Value) ?? "unknown";
        var app = NormalizeNilValue(m.Groups["app"].Value) ?? "unknown";

        var rest = m.Groups["rest"].Value;

        var (payload, message) = ParseStructuredDataAndMessage(rest);

        var source = app; // app — это APP‑NAME из syslog‑заголовка
        var env = "unknown";

        if (payload.TryGetValue("service", out var svc))
        {
            source = svc?.ToString() ?? source;
            payload.Remove("service");
        }

        if (payload.TryGetValue("source", out var src))
        {
            source = src?.ToString() ?? source;
            payload.Remove("source");
        }

        if (payload.TryGetValue("env", out var en))
        {
            env = en?.ToString() ?? env;
            payload.Remove("env");
        }

        if (payload.TryGetValue("environment", out var en2))
        {
            env = en2?.ToString() ?? env;
            payload.Remove("environment");
        }

        // Пытаемся переопределить host из structured data, но не оставляем его в payload
        if (payload.TryGetValue("host", out var h))
        {
            host = h?.ToString() ?? host;
            payload.Remove("host");
        }

        if (payload.TryGetValue("hostname", out var h2))
        {
            host = h2?.ToString() ?? host;
            payload.Remove("hostname");
        }

        // Пытаемся переопределить level из structured data, но не оставляем его в payload
        if (payload.TryGetValue("level", out var lvl))
        {
            var normalized = NormalizeLevel(lvl?.ToString());
            if (string.IsNullOrWhiteSpace(normalized) is false)
            {
                level = normalized;
            }

            payload.Remove("level");
        }

        var userId = TryGetUlong(payload, "user_id");
        if (userId is not null)
        {
            payload["user_id"] = userId;
        }

        var durationMs = TryGetUint(payload, "duration_ms");
        if (durationMs is not null)
        {
            payload["duration_ms"] = durationMs;
        }

        var httpStatus = TryGetUshort(payload, "http_status_code");
        if (httpStatus is not null)
        {
            payload["http_status_code"] = httpStatus;
        }

        var errorType = TryGetString(payload, "error_type");
        if (errorType is not null)
        {
            payload["error_type"] = errorType;
        }

        var stackTrace = TryGetString(payload, "stack_trace");
        if (stackTrace is not null)
        {
            payload["stack_trace"] = stackTrace;
        }

        return new LogEntry(
            createdAt, receivedAt, level, source,
            host, env, message, payload.AsReadOnly());
    }

    private static string? NormalizeLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return null;
        }

        var v = level.Trim();

        return v.ToLowerInvariant() switch
        {
            "critical" or "crit" or "fatal" => "Critical",
            "error" or "err" => "Error",
            "warning" or "warn" => "Warning",
            "info" or "information" => "Info",
            "debug" or "dbg" => "Debug",
            _ => null
        };
    }

    private static string SeverityToLevel(int sev) => sev switch
    {
        0 => "Critical",
        1 => "Critical",
        2 => "Critical",
        3 => "Error",
        4 => "Warning",
        5 => "Info",
        6 => "Info",
        7 => "Debug",
        _ => "Info"
    };

    private static DateTimeOffset? ParseSyslogTimestamp(string ts)
    {
        if (ts == "-" || string.IsNullOrWhiteSpace(ts))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(
                ts, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
        {
            return dto;
        }

        return null;
    }

    private static string? NormalizeNilValue(string v) => v == "-" ? null : v;

    private static (Dictionary<string, object?> extra, string message) ParseStructuredDataAndMessage(string rest)
    {
        var s = rest.TrimStart();
        var extra = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (s.StartsWith('-'))
        {
            var msg = s.Length > 1
                ? s[1..].TrimStart()
                : "";

            return (extra, msg);
        }

        var lastEnd = -1;
        foreach (Match b in SdBlock.Matches(s))
        {
            lastEnd = b.Index + b.Length;
            var content = b.Groups["content"].Value;

            foreach (Match p in SdParam.Matches(content))
            {
                var k = p.Groups["k"].Value;
                var v = UnescapeSyslogValue(p.Groups["v"].Value);

                if (ulong.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ul))
                {
                    extra[k] = ul;
                }
                else if (long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                {
                    extra[k] = l;
                }
                else if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                {
                    extra[k] = d;
                }
                else
                {
                    extra[k] = v;
                }
            }
        }

        var message = lastEnd >= 0 && lastEnd < s.Length
            ? s[lastEnd..].TrimStart()
            : s;

        return (extra, message);
    }

    private static string UnescapeSyslogValue(string v) =>
        v
            .Replace(@"\]", "]", StringComparison.Ordinal)
            .Replace(@"\\", "\\", StringComparison.Ordinal)
            .Replace("\\\"", "\"", StringComparison.Ordinal);

    private static ulong? TryGetUlong(Dictionary<string, object?> d, string k)
    {
        if (d.TryGetValue(k, out var v) is false || v is null)
        {
            return null;
        }

        return v switch
        {
            ulong ul => ul,
            long l and >= 0 => (ulong)l,
            int i and >= 0 => (ulong)i,
            string s when ulong.TryParse(s, out var ul2) => ul2,
            _ => null
        };
    }

    private static uint? TryGetUint(Dictionary<string, object?> d, string k)
    {
        if (d.TryGetValue(k, out var v) is false || v is null)
        {
            return null;
        }

        return v switch
        {
            uint ui => ui,
            ulong ul and <= uint.MaxValue => (uint)ul,
            long l and >= 0 and <= uint.MaxValue => (uint)l,
            int i and >= 0 => (uint)i,
            string s when uint.TryParse(s, out var ui2) => ui2,
            _ => null
        };
    }

    private static ushort? TryGetUshort(Dictionary<string, object?> d, string k)
    {
        if (d.TryGetValue(k, out var v) is false || v is null)
        {
            return null;
        }

        return v switch
        {
            ushort us => us,
            uint ui and <= ushort.MaxValue => (ushort)ui,
            ulong ul and <= ushort.MaxValue => (ushort)ul,
            long l and >= 0 and <= ushort.MaxValue => (ushort)l,
            int i and >= 0 and <= ushort.MaxValue => (ushort)i,
            string s when ushort.TryParse(s, out var us2) => us2,
            _ => null
        };
    }

    private static string? TryGetString(Dictionary<string, object?> d, string k)
    {
        if (d.TryGetValue(k, out var v) is false || v is null)
        {
            return null;
        }

        return v.ToString();
    }
}