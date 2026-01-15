using System.Text.Json;
using System.Text.Json.Serialization;
using Logly.RawLogsHandler.Domain.MessageQueues.Logs;
using Logly.RawLogsHandler.Domain.Services.FilesParsers.Facades;
using Logly.RawLogsHandler.Domain.Services.FilesParsers.Implementations;
using Logly.RawLogsHandler.Domain.Services.LogParsers.Facades;
using Logly.RawLogsHandler.Domain.Services.LogParsers.Implementations;
using Logly.RawLogsHandler.Domain.Services.Logs;
using Logly.RawLogsHandler.MessageQueues;
using static Logly.Kafka.Extensions.KafkaExtensions;

namespace Logly.RawLogsHandler.Infrastructure.Extensions;

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
            services.AddSingleton<ClfParser>();
            services.AddSingleton<JsonLogParser>();
            services.AddSingleton<SyslogParser>();
            services.AddSingleton<FallbackParser>();
            services.AddSingleton<LogParsersFacade>();

            services.AddSingleton<JsonFileParser>();
            services.AddSingleton<TextFileParser>();
            services.AddSingleton<FileParserFacade>();

            services.AddSingleton<ILogsService, LogsService>();

            return services;
        }

        private IServiceCollection AddKafka(IConfiguration configuration)
        {
            services.RegisterKafkaConsumer<LogsConsumer, LogsMessagesHandler>(configuration);

            services.RegisterKafkaProducer<ILogsProducer, LogsProducer>(configuration);

            return services;
        }
    }
}