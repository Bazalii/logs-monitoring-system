namespace Logly.Kafka.Models.Settings;

public sealed record KafkaConsumerOptions
{
    public string BootstrapServers { get; init; }
    public string GroupId { get; init; }
    public string AutoOffsetReset { get; init; } = "earliest";
    public bool EnableAutoCommit { get; init; } = false;
    public string Topic { get; init; }
    public int BatchSize { get; init; } = 100;
    public int BatchMaxWaitMs { get; init; } = 1000;
    public int PollTimeoutMs { get; init; } = 200;
}