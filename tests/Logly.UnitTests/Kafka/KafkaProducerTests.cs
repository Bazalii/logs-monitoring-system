using Confluent.Kafka;
using Logly.Kafka.Models.Settings;
using Logly.Serialization.Serializers;
using NSubstitute;

namespace Logly.UnitTests.Kafka;

public class KafkaProducerTests
{
    private static KafkaProducerOptions DefaultOptions() => new()
    {
        BootstrapServers = "localhost:9092",
        ClientId = "client-a",
        Topic = "topic-a",
        LingerMs = 1,
        BatchSize = 1,
        Acks = Acks.All
    };

    [Fact]
    public async Task ProduceAsync_SerializesAndCallsProduceAsync_WithKeyHeadersAndCancellationToken()
    {
        var options = DefaultOptions();

        var serializer = Substitute.For<ISerializer>();
        serializer.SerializeToBytes(Arg.Any<object>()).Returns([1, 2, 3]);

        var producer = Substitute.For<IProducer<string, byte[]>>();

        string? sentTopic = null;
        Message<string, byte[]>? sentMessage = null;
        var sentToken = CancellationToken.None;

        producer
            .ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(
                ci =>
                {
                    sentTopic = ci.Arg<string>();
                    sentMessage = ci.Arg<Message<string, byte[]>>();
                    sentToken = ci.Arg<CancellationToken>();
                    return new DeliveryResult<string, byte[]>();
                });

        var sut = new TestKafkaProducer(options, serializer, producer);

        var headers = new Logly.Kafka.Models.Contracts.KafkaHeaders
        {
            ["h1"] = [9]
        };

        using var cts = new CancellationTokenSource();
        await sut.ProduceAsync(new { X = 1 }, key: "k1", headers: headers, cancellation: cts.Token);

        Assert.Equal(options.Topic, sentTopic);
        Assert.NotNull(sentMessage);
        Assert.Equal("k1", sentMessage!.Key);
        Assert.True(sentMessage.Value.SequenceEqual(new byte[] { 1, 2, 3 }));
        Assert.Equal(new byte[] { 9 }, sentMessage.Headers.GetLastBytes("h1"));
        Assert.Equal(cts.Token, sentToken);

        _ = serializer.Received(1).SerializeToBytes(Arg.Any<object>());
        await producer.Received(1).ProduceAsync(
            options.Topic, Arg.Any<Message<string, byte[]>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Dispose_FlushesAndDisposes()
    {
        var options = DefaultOptions();
        var serializer = Substitute.For<ISerializer>();
        var producer = Substitute.For<IProducer<string, byte[]>>();

        var sut = new TestKafkaProducer(options, serializer, producer);

        sut.Dispose();

        producer.Received(1).Flush(Arg.Any<TimeSpan>());
        producer.Received(1).Dispose();
    }
}