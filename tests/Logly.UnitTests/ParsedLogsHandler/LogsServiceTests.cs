using System.Collections.ObjectModel;
using Logly.ParsedLogsHandler.Domain.Models.Logs;
using Logly.ParsedLogsHandler.Domain.Repositories.Logs;
using Logly.ParsedLogsHandler.Domain.Services.Logs;
using NSubstitute;

namespace Logly.UnitTests.ParsedLogsHandler;

public sealed class LogsServiceTests
{
    [Fact]
    public async Task InsertAsync_CallsRepositoryInsertAsync_WithSameArguments()
    {
        // Arrange
        var repository = Substitute.For<ILogsRepository>();
        var logsService = new LogsService(repository);

        var logs = new ILog[]
        {
            new FakeLog(),
            new FakeLog()
        };

        var cancellation = new CancellationTokenSource().Token;

        // Act
        await logsService.InsertAsync(logs, cancellation);

        // Assert
        await repository
            .Received(1)
            .InsertAsync(logs, cancellation);
    }

    private sealed class FakeLog : ILog
    {
        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ReceivedAt { get; } = DateTimeOffset.UtcNow;
        public string Level => "Info";
        public string Source => "service";
        public string Host => "host";
        public string Environment => "dev";
        public string Message => "message";
        public ReadOnlyDictionary<string, object?>? Payload => null;
    }
}