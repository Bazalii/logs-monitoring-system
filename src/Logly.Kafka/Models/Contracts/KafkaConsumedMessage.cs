using System;

namespace Logly.Kafka.Models.Contracts;

public sealed record KafkaConsumedMessage(
    string Topic,
    int Partition,
    long Offset,
    string? Key,
    byte[] Value,
    KafkaHeaders Headers,
    DateTimeOffset Timestamp);