using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Logly.Kafka.Models.Contracts;
using Logly.Kafka.Models.Settings;
using Logly.Serialization.Serializers;

namespace Logly.Kafka.Producers;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly string _topic;
    private readonly IProducer<string, byte[]> _producer;
    private readonly ISerializer _serializer;

    protected KafkaProducer(
        KafkaProducerOptions options,
        ISerializer serializer)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            ClientId = options.ClientId,
            LingerMs = options.LingerMs,
            BatchSize = options.BatchSize,
            Acks = options.Acks
        };

        _producer = new ProducerBuilder<string, byte[]>(config).Build();
        _serializer = serializer;

        _topic = options.Topic;
    }

    public async Task ProduceAsync<T>(
        T value,
        string? key = null,
        KafkaHeaders? headers = null,
        CancellationToken cancellation = default)
    {
        var payload = _serializer.SerializeToBytes(value);

        await ProduceBytesAsync(payload, key, headers, cancellation);
    }

    protected async Task ProduceBytesAsync(
        byte[] value,
        string? key,
        KafkaHeaders? headers,
        CancellationToken cancellation)
    {
        var confluentHeaders = new Headers();

        if (headers is not null)
        {
            foreach (var header in headers)
            {
                confluentHeaders.Add(header.Key, header.Value);
            }
        }

        var message = new Message<string, byte[]>
        {
            Key = key,
            Value = value,
            Headers = confluentHeaders
        };

        await _producer.ProduceAsync(_topic, message, cancellation);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}