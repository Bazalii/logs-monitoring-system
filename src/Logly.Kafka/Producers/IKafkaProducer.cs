using System.Threading;
using System.Threading.Tasks;
using Logly.Kafka.Models.Contracts;

namespace Logly.Kafka.Producers;

public interface IKafkaProducer
{
    Task ProduceAsync<T>(
        T value,
        string? key = null,
        KafkaHeaders? headers = null,
        CancellationToken cancellation = default);
}