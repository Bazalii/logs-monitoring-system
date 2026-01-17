using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Logly.Kafka.Models.Contracts;

namespace Logly.Kafka.Consumers;

public interface IKafkaConsumer
{
    Task<IReadOnlyCollection<KafkaConsumedMessage>> ConsumeBatchAsync(
        CancellationToken cancellation);

    void Commit(IReadOnlyCollection<KafkaConsumedMessage> messages);
}