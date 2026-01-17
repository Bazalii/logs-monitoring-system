using Logly.RawLogsHandler.Domain.Models.Enums;
using Logly.RawLogsHandler.Domain.Models.Parsing;
using Logly.RawLogsHandler.Domain.Services.LogParsers.Implementations;
using Logly.RawLogsHandler.Infrastructure.Helpers;

namespace Logly.RawLogsHandler.Domain.Services.LogParsers.Facades;

public sealed class LogParsersFacade(
    ClfParser clfParser,
    JsonLogParser jsonLogParser,
    SyslogParser syslogParser,
    FallbackParser fallbackParser)
{
    public LogEntry Parse(string raw)
    {
        var logFormat = LogFormatDetector.Detect(raw);
        var parser = GetParser(logFormat);

        return parser.Parse(raw);
    }

    private IParser GetParser(LogFormat logFormat)
    {
        return logFormat switch
        {
            LogFormat.Json => jsonLogParser,
            LogFormat.SyslogRfc5424 => syslogParser,
            LogFormat.Clf => clfParser,
            _ => fallbackParser,
        };
    }
}