using Logly.Kafka.Consumers;
using Logly.Kafka.Models.Settings;

namespace Logly.ParsedLogsHandler.MessageQueues;

public sealed class LogsConsumer(KafkaConsumerOptions options) : KafkaConsumer(options);