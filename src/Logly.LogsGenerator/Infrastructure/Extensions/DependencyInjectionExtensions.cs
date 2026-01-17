using System.Text.Json;
using System.Text.Json.Serialization;
using Logly.Kafka.Extensions;
using Logly.LogsGenerator.Domain.MessageQueues.Logs;
using Logly.LogsGenerator.Domain.Services;

namespace Logly.LogsGenerator.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static void AddDependencies(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        builder.Services.AddHealthChecks();

        builder.Services
            .AddServices()
            .AddKafka(configuration)
            .AddOpenApi()
            .AddControllers()
            .AddJsonOptions(
                configurator =>
                {
                    var options = configurator.JsonSerializerOptions;

                    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                });
    }

    extension(IServiceCollection services)
    {
        private IServiceCollection AddServices()
        {
            services.AddSingleton<ILogsGeneratorService, LogsGeneratorService>();

            return services;
        }

        private IServiceCollection AddKafka(IConfiguration configuration)
        {
            services.RegisterKafkaProducer<ILogsProducer, LogsProducer>(configuration);

            return services;
        }
    }
}