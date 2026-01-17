using Logly.RawLogsHandler.Domain.Models.Parsing;

namespace Logly.RawLogsHandler.Domain.Services.LogParsers;

public interface IParser
{
    LogEntry Parse(string raw);
}