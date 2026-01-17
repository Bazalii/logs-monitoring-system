using System.Reflection;
using Confluent.Kafka;
using Logly.Kafka.Consumers;
using Logly.Kafka.Models.Settings;

namespace Logly.UnitTests.Kafka;

internal sealed class TestKafkaConsumer : KafkaConsumer
{
    public TestKafkaConsumer(
        KafkaConsumerOptions options,
        IConsumer<string, byte[]> consumer)
        : base(options)
    {
        var field = typeof(KafkaConsumer)
            .GetField("_consumer", BindingFlags.Instance | BindingFlags.NonPublic)!;

        if (field.GetValue(this) is IConsumer<string, byte[]> original)
        {
            try
            {
                original.Close();
            }
            catch
            {
                /* best-effort */
            }

            original.Dispose();
        }

        field.SetValue(this, consumer);

        consumer.Subscribe(options.Topic);
    }
}