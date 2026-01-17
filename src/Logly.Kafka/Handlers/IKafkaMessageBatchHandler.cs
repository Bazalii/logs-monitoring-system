using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Logly.Kafka.Models.Contracts;

namespace Logly.Kafka.Handlers;

public interface IKafkaMessageBatchHandler
{
    Task HandleAsync(
        IReadOnlyCollection<KafkaConsumedMessage> messages,
        CancellationToken cancellation);
}