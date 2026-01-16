using Logly.LogsAnalyzer.Domain.Models.Logs;

namespace Logly.LogsAnalyzer.Controllers;

public sealed record LogResponse(
    DateTimeOffset CreatedAt,
    string Level,
    string Source,
    string Message,
    IReadOnlyDictionary<string, object?>? Payload)
{
    public static LogResponse FromDomain(ILog log) =>
        new(
            log.CreatedAt,
            log.Level,
            log.Source,
            log.Message,
            log.Payload);
}