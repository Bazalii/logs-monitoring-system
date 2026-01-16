using System.Text.Json;
using System.Text.Json.Serialization;
using Logly.LogsAnalyzer.Domain.Repositories.Logs;
using Logly.LogsAnalyzer.Domain.Services.Logs;
using Logly.LogsAnalyzer.Infrastructure.Models;

namespace Logly.LogsAnalyzer.Infrastructure.Extensions;

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
    }
}