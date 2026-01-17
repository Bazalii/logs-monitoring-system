using Confluent.Kafka;
using Logly.Kafka.Models.Settings;
using Logly.Kafka.Producers;
using Logly.Serialization.Serializers;

namespace Logly.UnitTests.Kafka;

internal sealed class TestKafkaProducer : KafkaProducer
{
    public TestKafkaProducer(
        KafkaProducerOptions options,
        ISerializer serializer,
        IProducer<string, byte[]> producer)
        : base(options, serializer)
    {
        typeof(KafkaProducer)
            .GetField("_producer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(this, producer);
    }
}