using System.Text;
using Logly.Kafka.Handlers;
using Logly.Kafka.Models.Contracts;
using Logly.RawLogsHandler.Domain.Services.Logs;

namespace Logly.RawLogsHandler.MessageQueues;

public class LogsMessagesHandler(
    ILogsService logsService)
    : IKafkaMessageBatchHandler
{
    public async Task HandleAsync(
        IReadOnlyCollection<KafkaConsumedMessage> messages,
        CancellationToken cancellation)
    {
        foreach (var message in messages)
        {
            var log = Encoding.UTF8.GetString(message.Value);

            await logsService.SendRawLogAsync(log, cancellation);
        }
    }
}