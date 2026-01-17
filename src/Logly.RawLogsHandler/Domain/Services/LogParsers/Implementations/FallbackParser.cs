using Logly.RawLogsHandler.Domain.Models.Parsing;
using Logly.RawLogsHandler.Infrastructure.Helpers;

namespace Logly.RawLogsHandler.Domain.Services.LogParsers.Implementations;

public sealed class FallbackParser : IParser
{
    public LogEntry Parse(string raw)
    {
        var receivedAt = DateTimeOffset.UtcNow;

        const string source = "unknown";
        const string host = "unknown";
        const string env = "unknown";

        var payload = new Dictionary<string, object?>
            {
                ["raw"] = raw
            }
            .AsReadOnly();

        return new LogEntry(
            receivedAt, receivedAt, "Info", source,
            host, env, raw, payload);
    }
}