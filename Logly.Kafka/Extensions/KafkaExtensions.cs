using System;
using Logly.Kafka.BackgroundServices;
using Logly.Kafka.Consumers;
using Logly.Kafka.Handlers;
using Logly.Kafka.Models.Settings;
using Logly.Kafka.Producers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Logly.Kafka.Extensions;

public static class KafkaExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterKafkaProducer<TService, TImplementation>(
            IConfiguration configuration)
            where TService : class
            where TImplementation : class, IKafkaProducer, TService
        {
            const string producersSectionName = "Kafka:Producers";
            var consumerSectionName = typeof(TImplementation).Name;
            var options = BindAndValidateProducerOptions(
                configuration, $"{producersSectionName}:{consumerSectionName}");

            services.AddSingleton<TService, TImplementation>(
                serviceProvider => ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider, options));

            return services;
        }

        public IServiceCollection RegisterKafkaConsumer<TConsumer, THandler>(
            IConfiguration configuration)
            where TConsumer : class, IKafkaConsumer
            where THandler : class, IKafkaMessageBatchHandler
        {
            const string consumersSectionName = "Kafka:Consumers";
            var consumerSectionName = typeof(TConsumer).Name;
            var options = BindAndValidateConsumerOptions(
                configuration, $"{consumersSectionName}:{consumerSectionName}");

            services.AddSingleton<THandler>();

            services.AddSingleton<TConsumer>(
                serviceProvider => ActivatorUtilities.CreateInstance<TConsumer>(serviceProvider, options));

            services.AddHostedService<KafkaConsumerBackgroundService<TConsumer, THandler>>();

            return services;
        }
    }

    private static KafkaConsumerOptions BindAndValidateConsumerOptions(
        IConfiguration configuration,
        string section)
    {
        var options = new KafkaConsumerOptions();
        configuration.GetSection(section).Bind(options);

        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            throw new InvalidOperationException("BootstrapServers is required.");
        }

        if (string.IsNullOrWhiteSpace(options.GroupId))
        {
            throw new InvalidOperationException("GroupId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Topic))
        {
            throw new InvalidOperationException("Topic is required.");
        }

        return options;
    }

    private static KafkaProducerOptions BindAndValidateProducerOptions(
        IConfiguration configuration,
        string section)
    {
        var options = new KafkaProducerOptions();
        configuration.GetSection(section).Bind(options);

        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            throw new InvalidOperationException("BootstrapServers is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            throw new InvalidOperationException("ClientId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Topic))
        {
            throw new InvalidOperationException("Topic is required.");
        }

        return options;
    }
}