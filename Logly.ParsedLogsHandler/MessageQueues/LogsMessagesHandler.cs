using Logly.Kafka.Handlers;
using Logly.Kafka.Models.Contracts;
using Logly.ParsedLogsHandler.Domain.Services.Logs;
using Logly.ParsedLogsHandler.Integration.Models.Messages.Logs;
using Logly.Serialization.Serializers.Implementations;

namespace Logly.RawLogsHandler.MessageQueues;

public class LogsMessagesHandler(
    ILogsService logsService)
    : IKafkaMessageBatchHandler
{
    public Task HandleAsync(
        IReadOnlyCollection<KafkaConsumedMessage> messages,
        CancellationToken cancellation)
    {
        var logs = messages
            .Select(message => JsonSerializer.SnakeCase.Deserialize<LogMessage>(message.Value))
            .ToArray();

        return logsService.InsertAsync(logs, cancellation);
    }
}