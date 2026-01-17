using System.Text.Json;
using System.Text.RegularExpressions;
using Logly.RawLogsHandler.Domain.Models.Enums;

namespace Logly.RawLogsHandler.Infrastructure.Helpers;

public static class LogFormatDetector
{
    // RFC5424 starts with "<PRI>VERSION " e.g. "<34>1 ..."
    private static readonly Regex SyslogStart = new(@"^\<\d{1,3}\>\d\s", RegexOptions.Compiled);

    // CLF typical: ip ident authuser [date] "request" status bytes
    private static readonly Regex ClfStart = new(@"^(?<ip>\S+)\s+\S+\s+\S+\s+\[.+\]\s+""", RegexOptions.Compiled);

    public static LogFormat Detect(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return LogFormat.Unknown;
        }

        var s = raw.TrimStart();

        // JSON
        if (s.Length > 0 && (s[0] == '{' || s[0] == '[') && IsJson(s))
        {
            return LogFormat.Json;
        }

        if (SyslogStart.IsMatch(s))
        {
            return LogFormat.SyslogRfc5424;
        }

        if (ClfStart.IsMatch(s))
        {
            return LogFormat.Clf;
        }

        return LogFormat.Unknown;
    }

    private static bool IsJson(string s)
    {
        try
        {
            using var doc = JsonDocument.Parse(s);
            return doc.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array;
        }
        catch
        {
            return false;
        }
    }
}