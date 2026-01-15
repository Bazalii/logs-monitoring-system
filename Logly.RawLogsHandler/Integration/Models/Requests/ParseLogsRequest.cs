namespace Logly.RawLogsHandler.Integration.Models.Requests;

public sealed record ParseLogsRequest
{
    public required string FilePath { get; init; }
}