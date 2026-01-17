using System;
using System.Threading;
using System.Threading.Tasks;
using Logly.Kafka.Consumers;
using Logly.Kafka.Handlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Logly.Kafka.BackgroundServices;

public sealed class KafkaConsumerBackgroundService<TConsumer, THandler>(
    TConsumer consumer,
    THandler handler,
    ILogger<KafkaConsumerBackgroundService<TConsumer, THandler>> logger)
    : BackgroundService
    where TConsumer : class, IKafkaConsumer
    where THandler : class, IKafkaMessageBatchHandler
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested is false)
        {
            try
            {
                var batch = await consumer.ConsumeBatchAsync(stoppingToken);

                if (batch.Count == 0)
                {
                    continue;
                }

                await handler.HandleAsync(batch, stoppingToken);

                consumer.Commit(batch);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Kafka consumer loop error.");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        if (consumer is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.Dispose();
    }
}