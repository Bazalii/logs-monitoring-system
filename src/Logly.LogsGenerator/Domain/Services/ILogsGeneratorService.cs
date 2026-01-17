namespace Logly.LogsGenerator.Domain.Services;

public interface ILogsGeneratorService
{
    Task SendLogsToKafkaAsync(
        int numberOfLogs,
        TimeSpan? period,
        string format,
        CancellationToken cancellation);
}