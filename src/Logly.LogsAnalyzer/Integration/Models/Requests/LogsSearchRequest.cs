namespace Logly.LogsAnalyzer.Integration.Models.Requests;

public sealed record LogsSearchRequest(
    string? Level,
    string? Source,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Limit = 100,
    int Offset = 0);