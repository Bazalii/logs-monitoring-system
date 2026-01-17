namespace Logly.LogsAnalyzer.Integration.Models.Responses;

public sealed record LogsSearchResponse(
    ulong Total,
    IReadOnlyCollection<LogResponse> Logs);