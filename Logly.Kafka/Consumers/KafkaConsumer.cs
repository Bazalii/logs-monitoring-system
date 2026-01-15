using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Logly.Kafka.Models.Contracts;
using Logly.Kafka.Models.Settings;

namespace Logly.Kafka.Consumers;

public class KafkaConsumer : IKafkaConsumer, IDisposable
{
    private readonly TimeSpan _consumeTimeout;
    private readonly IConsumer<string, byte[]> _consumer;
    private readonly KafkaConsumerOptions _options;

    protected KafkaConsumer(KafkaConsumerOptions options)
    {
        _options = options;

        if (string.IsNullOrEmpty(_options.BootstrapServers))
        {
            throw new ArgumentException("KafkaConsumerOptions.Topic must contain valid topic name.");
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            EnableAutoCommit = _options.EnableAutoCommit,
            AutoOffsetReset = ParseAutoOffsetReset(_options.AutoOffsetReset)
        };

        _consumeTimeout = TimeSpan.FromMilliseconds(_options.PollTimeoutMs);

        _consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        _consumer.Subscribe(_options.Topic);
    }

    public async Task<IReadOnlyCollection<KafkaConsumedMessage>> ConsumeBatchAsync(CancellationToken cancellation)
    {
        var batch = new List<KafkaConsumedMessage>(_options.BatchSize);
        var batchStart = DateTimeOffset.UtcNow;

        while (batch.Count < _options.BatchSize)
        {
            cancellation.ThrowIfCancellationRequested();

            ConsumeResult<string, byte[]>? result;

            try
            {
                result = _consumer.Consume(_consumeTimeout);
            }
            catch (ConsumeException exception)
            {
                throw new InvalidOperationException($"Kafka consume error: {exception.Error.Reason}", exception);
            }

            if (result is not null)
            {
                batch.Add(CreateMessage(result));
            }

            if (batch.Count > 0)
            {
                var elapsed = DateTimeOffset.UtcNow - batchStart;
                if (elapsed.TotalMilliseconds >= _options.BatchMaxWaitMs)
                {
                    break;
                }
            }

            if (result is null)
            {
                if ((DateTimeOffset.UtcNow - batchStart).TotalMilliseconds >= _options.BatchMaxWaitMs)
                {
                    break;
                }

                await Task.Yield();
            }
        }

        return batch;
    }

    public void Commit(IReadOnlyCollection<KafkaConsumedMessage> messages)
    {
        if (messages.Count == 0)
        {
            return;
        }

        var maxOffsets = new Dictionary<int, long>();

        foreach (var consumedMessage in messages)
        {
            if (maxOffsets.TryGetValue(consumedMessage.Partition, out var current))
            {
                if (consumedMessage.Offset > current)
                {
                    maxOffsets[consumedMessage.Partition] = consumedMessage.Offset;
                }
            }
            else
            {
                maxOffsets[consumedMessage.Partition] = consumedMessage.Offset;
            }
        }

        var offsets = maxOffsets
            .Select(
                kv => new TopicPartitionOffset(
                    new TopicPartition(messages.First().Topic, new Partition(kv.Key)),
                    new Offset(kv.Value + 1)))
            .ToArray();

        _consumer.Commit(offsets);
    }

    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
    }

    private static KafkaConsumedMessage CreateMessage(ConsumeResult<string, byte[]> result)
    {
        var headers = new KafkaHeaders();

        if (result.Message.Headers is not null)
        {
            foreach (var header in result.Message.Headers)
            {
                headers[header.Key] = header.GetValueBytes() ?? [];
            }
        }

        var timeStamp = result.Message.Timestamp.UtcDateTime == default
            ? DateTimeOffset.UtcNow
            : new DateTimeOffset(result.Message.Timestamp.UtcDateTime, TimeSpan.Zero);

        return new KafkaConsumedMessage(
            Topic: result.Topic,
            Partition: result.Partition.Value,
            Offset: result.Offset.Value,
            Key: result.Message.Key,
            Value: result.Message.Value ?? [],
            Headers: headers,
            Timestamp: timeStamp);
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string v)
    {
        if (string.Equals(v, "earliest", StringComparison.OrdinalIgnoreCase))
        {
            return AutoOffsetReset.Earliest;
        }

        if (string.Equals(v, "latest", StringComparison.OrdinalIgnoreCase))
        {
            return AutoOffsetReset.Latest;
        }

        return AutoOffsetReset.Earliest;
    }
}