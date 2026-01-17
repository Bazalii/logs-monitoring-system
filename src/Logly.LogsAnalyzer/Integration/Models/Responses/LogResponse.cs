using Logly.LogsAnalyzer.Domain.Models.Core.Logs;

namespace Logly.LogsAnalyzer.Integration.Models.Responses;

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