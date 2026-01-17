using Logly.RawLogsHandler.Domain.MessageQueues.Logs;
using Logly.RawLogsHandler.Domain.Services.FilesParsers.Facades;
using Logly.RawLogsHandler.Domain.Services.FilesParsers.Implementations;
using Logly.RawLogsHandler.Domain.Services.LogParsers.Facades;
using Logly.RawLogsHandler.Domain.Services.LogParsers.Implementations;
using Logly.RawLogsHandler.Domain.Services.Logs;
using NSubstitute;

namespace Logly.UnitTests.RawLogsHandler.Services;

public sealed class LogsServiceTests
{
    [Fact]
    public async Task SendRawLogAsync_ParsesAndSendsToProducer()
    {
        // Arrange
        var logsProducer = Substitute.For<ILogsProducer>();

        var fileParsersFacade = new FileParsersFacade(new TextFileParser(), new JsonFileParser());
        var logParsersFacade = new LogParsersFacade(
            new ClfParser(),
            new JsonLogParser(),
            new SyslogParser(),
            new FallbackParser());

        var logsService = new LogsService(fileParsersFacade, logParsersFacade, logsProducer);

        const string raw = """
                           {"created_at":"2026-01-01T00:00:00Z","level":"info","source":"api-gateway","host":"prod-01","environment":"production","message":"Ok"}
                           """;

        // Act
        await logsService.SendRawLogAsync(raw, CancellationToken.None);

        // Assert
        await logsProducer
            .Received(1)
            .SendLogAsync(
                Arg.Any<Logly.RawLogsHandler.Domain.Models.Parsing.LogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendLogsFromFileAsync_ReadsEachLine_Parses_AndSendsAllToProducer()
    {
        // Arrange
        var logsProducer = Substitute.For<ILogsProducer>();

        var fileParsersFacade = new FileParsersFacade(new TextFileParser(), new JsonFileParser());
        var logParsersFacade = new LogParsersFacade(
            new ClfParser(),
            new JsonLogParser(),
            new SyslogParser(),
            new FallbackParser());

        var logsService = new LogsService(fileParsersFacade, logParsersFacade, logsProducer);

        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");

        await File.WriteAllTextAsync(
            filePath,
            """
            {"created_at":"2026-01-01T00:00:00Z","level":"info","source":"api-gateway","host":"prod-01","environment":"production","message":"Ok"}
            {"created_at":"2026-01-01T00:01:00Z","level":"error","source":"payment-service","host":"prod-02","environment":"production","message":"Fail","payload":{"error_type":"TimeoutException"}}
            """);

        try
        {
            // Act
            await logsService.SendLogsFromFileAsync(filePath, CancellationToken.None);

            // Assert
            await logsProducer
                .Received(2)
                .SendLogAsync(
                    Arg.Any<Logly.RawLogsHandler.Domain.Models.Parsing.LogEntry>(),
                    Arg.Any<CancellationToken>());
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}