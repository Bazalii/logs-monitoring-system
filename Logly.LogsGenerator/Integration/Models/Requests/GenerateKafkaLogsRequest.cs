namespace Logly.LogsGenerator.Integration.Models.Requests;

public sealed record GenerateKafkaLogsRequest
{
    public required int NumberOfLogs { get; init; }
    public required string Format { get; init; }
}