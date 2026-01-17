using Confluent.Kafka;
using Logly.Kafka.Models.Contracts;
using Logly.Kafka.Models.Settings;
using NSubstitute;

namespace Logly.UnitTests.Kafka;

public class KafkaConsumerTests
{
    private static KafkaConsumerOptions DefaultOptions(int batchSize = 3) => new()
    {
        BootstrapServers = "localhost:9092",
        Topic = "topic-a",
        GroupId = "group-a",
        EnableAutoCommit = false,
        AutoOffsetReset = "earliest",
        PollTimeoutMs = 1,
        BatchSize = batchSize,
        BatchMaxWaitMs = 50
    };

    private static ConsumeResult<string, byte[]> MakeResult(
        string topic,
        int partition,
        long offset,
        string? key = null,
        byte[]? value = null,
        Headers? headers = null,
        Timestamp? timestamp = null)
    {
        return new ConsumeResult<string, byte[]>
        {
            Topic = topic,
            Partition = new Partition(partition),
            Offset = new Offset(offset),
            Message = new Message<string, byte[]>
            {
                Key = key,
                Value = value,
                Headers = headers,
                Timestamp = timestamp ?? new Timestamp(DateTime.UtcNow)
            }
        };
    }

    [Fact]
    public async Task ConsumeBatchAsync_CollectsUpToBatchSize()
    {
        var options = DefaultOptions();

        var consumer = Substitute.For<IConsumer<string, byte[]>>();
        consumer.Consume(Arg.Any<TimeSpan>()).Returns(
            MakeResult(options.Topic, 0, 10, key: "k1", value: [1]),
            MakeResult(options.Topic, 0, 11, key: "k2", value: [2]),
            MakeResult(options.Topic, 1, 5, key: null, value: [3])
        );

        var sut = new TestKafkaConsumer(options, consumer);

        var batch = await sut.ConsumeBatchAsync(CancellationToken.None);

        Assert.Equal(3, batch.Count);
        Assert.Equal([10L, 11L, 5L], batch.Select(x => x.Offset));
        Assert.Equal([0, 0, 1], batch.Select(x => x.Partition));
        Assert.Equal(["k1", "k2", null], batch.Select(x => x.Key));

        consumer.Received(3).Consume(Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task ConsumeBatchAsync_MapsHeaders_AndDefaultsValueToEmpty_WhenNull()
    {
        var options = DefaultOptions(batchSize: 1);

        var headers = new Headers { { "h1", "\t\t"u8.ToArray() } };

        var consumer = Substitute.For<IConsumer<string, byte[]>>();
        consumer.Consume(Arg.Any<TimeSpan>())
            .Returns(MakeResult(options.Topic, 0, 1, value: null, headers: headers));

        var sut = new TestKafkaConsumer(options, consumer);

        var msg = (await sut.ConsumeBatchAsync(CancellationToken.None)).Single();

        Assert.Equal(Array.Empty<byte>(), msg.Value);
        Assert.True(msg.Headers.ContainsKey("h1"));
        Assert.Equal("\t\t"u8.ToArray(), msg.Headers["h1"]);
    }

    [Fact]
    public async Task ConsumeBatchAsync_ThrowsInvalidOperationException_OnConsumeException()
    {
        var options = DefaultOptions();

        var consumer = Substitute.For<IConsumer<string, byte[]>>();
        consumer
            .When(c => c.Consume(Arg.Any<TimeSpan>()))
            .Do(
                _ => throw new ConsumeException(
                    new ConsumeResult<byte[], byte[]>(),
                    new Error(ErrorCode.Unknown, "boom")));

        var sut = new TestKafkaConsumer(options, consumer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ConsumeBatchAsync(CancellationToken.None));

        Assert.Contains("Kafka consume error: boom", ex.Message);
    }

    [Fact]
    public void Commit_DoesNothing_WhenEmpty()
    {
        var options = DefaultOptions();
        var consumer = Substitute.For<IConsumer<string, byte[]>>();

        var sut = new TestKafkaConsumer(options, consumer);

        sut.Commit([]);

        consumer.DidNotReceiveWithAnyArgs().Commit(default(IEnumerable<TopicPartitionOffset>)!);
    }

    [Fact]
    public void Commit_CommitsMaxOffsetPlusOne_PerPartition()
    {
        var options = DefaultOptions();
        var consumer = Substitute.For<IConsumer<string, byte[]>>();

        IEnumerable<TopicPartitionOffset>? committed = null;
        consumer
            .When(c => c.Commit(Arg.Any<IEnumerable<TopicPartitionOffset>>()))
            .Do(ci => committed = ci.Arg<IEnumerable<TopicPartitionOffset>>());

        var sut = new TestKafkaConsumer(options, consumer);

        var messages = new[]
        {
            new KafkaConsumedMessage(
                options.Topic, 0, 10, null, [1], new KafkaHeaders(), DateTimeOffset.UtcNow),
            new KafkaConsumedMessage(
                options.Topic, 0, 12, null, [2], new KafkaHeaders(), DateTimeOffset.UtcNow),
            new KafkaConsumedMessage(
                options.Topic, 1, 5, null, [3], new KafkaHeaders(), DateTimeOffset.UtcNow),
            new KafkaConsumedMessage(
                options.Topic, 1, 4, null, [4], new KafkaHeaders(), DateTimeOffset.UtcNow),
        };

        sut.Commit(messages);

        Assert.NotNull(committed);
        var arr = committed!.ToArray();
        
        Assert.Contains(arr, x => x.Topic == options.Topic && x.Partition.Value == 0 && x.Offset.Value == 13);
        Assert.Contains(arr, x => x.Topic == options.Topic && x.Partition.Value == 1 && x.Offset.Value == 6);

        consumer.Received(1).Commit(Arg.Any<IEnumerable<TopicPartitionOffset>>());
    }

    [Fact]
    public void Dispose_ClosesAndDisposesConsumer()
    {
        var options = DefaultOptions();
        var consumer = Substitute.For<IConsumer<string, byte[]>>();

        var sut = new TestKafkaConsumer(options, consumer);

        sut.Dispose();

        consumer.Received(1).Close();
        consumer.Received(1).Dispose();
    }
}