using Confluent.Kafka;

namespace Logly.Kafka.Models.Settings;

public sealed record KafkaProducerOptions
{
    public string BootstrapServers { get; init; }
    public string ClientId { get; init; }
    public string Topic { get; init; }
    public int LingerMs { get; init; } = 5;
    public int BatchSize { get; init; } = 100;
    public Acks Acks { get; init; } = Acks.All;
}