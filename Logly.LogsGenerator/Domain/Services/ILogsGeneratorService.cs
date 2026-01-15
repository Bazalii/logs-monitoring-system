namespace Logly.LogsGenerator.Domain.Services;

public interface ILogsGeneratorService
{
    Task SendLogsToKafkaAsync(
        int numberOfLogs,
        string format);
}