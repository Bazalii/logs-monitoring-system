using Logly.ParsedLogsHandler.Domain.Repositories.Logs;
using Logly.ParsedLogsHandler.Domain.Services.Logs;
using Logly.ParsedLogsHandler.Infrastructure.Models;
using Logly.ParsedLogsHandler.MessageQueues;
using static Logly.Kafka.Extensions.KafkaExtensions;

namespace Logly.ParsedLogsHandler.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static void AddDependencies(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        builder.Services.AddHealthChecks();

        builder.Services
            .AddConfiguration(configuration)
            .AddServices()
            .AddRepositories()
            .AddKafka(configuration);
    }

    extension(IServiceCollection services)
    {
        private IServiceCollection AddConfiguration(IConfiguration configuration)
        {
            services.Configure<ClickHouseOptions>(configuration.GetSection("ClickHouse"));

            return services;
        }

        private IServiceCollection AddServices()
        {
            services.AddSingleton<ILogsService, LogsService>();

            return services;
        }

        private IServiceCollection AddRepositories()
        {
            services.AddSingleton<ILogsRepository, LogsRepository>();

            return services;
        }

        private IServiceCollection AddKafka(IConfiguration configuration)
        {
            services.RegisterKafkaConsumer<LogsConsumer, LogsMessagesHandler>(configuration);

            return services;
        }
    }
}