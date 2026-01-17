using Logly.ParsedLogsHandler.Domain.Models.Logs;
using Logly.ParsedLogsHandler.Domain.Repositories.Logs;
using Logly.ParsedLogsHandler.Infrastructure.Models;
using Microsoft.Extensions.Options;

namespace Logly.UnitTests.ParsedLogsHandler;

public sealed class LogsRepositoryInsertTests
{
    [Fact]
    public async Task InsertAsync_EmptyLogs_DoesNothing()
    {
        // Arrange
        var logsRepository = CreateRepository();
        var logs = Array.Empty<ILog>();

        // Act
        await logsRepository.InsertAsync(logs, CancellationToken.None);

        // Assert
        // Method doesn't throw
        Assert.True(true);
    }

    private static LogsRepository CreateRepository()
    {
        var options = Options.Create(
            new ClickHouseOptions
            {
                ConnectionString = "Host=localhost;Port=9000;User=default;",
                Table = "logly.logs",
                BatchSize = 100
            });

        return new LogsRepository(options);
    }
}