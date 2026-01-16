namespace Logly.LogsAnalyzer.Controllers;

public sealed record LogsSearchResponse(
    ulong Total,
    IReadOnlyCollection<LogResponse> Logs);