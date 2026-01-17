using Logly.RawLogsHandler.Domain.Services.LogParsers.Facades;
using Logly.RawLogsHandler.Domain.Services.LogParsers.Implementations;

namespace Logly.UnitTests.RawLogsHandler.Facades;

public sealed class LogParsersFacadeTests
{
    [Fact]
    public void Parse_JsonLine_UsesJsonParser()
    {
        // Arrange
        var facade = new LogParsersFacade(
            new ClfParser(),
            new JsonLogParser(),
            new SyslogParser(),
            new FallbackParser());

        const string raw =
            """
            {"created_at":"2026-01-01T00:00:00Z","level":"info","source":"api-gateway","host":"prod-01","environment":"production","message":"Ok"}
            """;

        // Act
        var log = facade.Parse(raw);

        // Assert
        Assert.Equal("api-gateway", log.Source);
        Assert.Equal("prod-01", log.Host);
        Assert.Equal("production", log.Environment);
    }

    [Fact]
    public void Parse_UnknownLine_UsesFallback()
    {
        // Arrange
        var facade = new LogParsersFacade(
            new ClfParser(),
            new JsonLogParser(),
            new SyslogParser(),
            new FallbackParser());

        // Act
        var log = facade.Parse("?? not a known format ??");

        // Assert
        Assert.Equal("unknown", log.Source);
        Assert.NotNull(log.Payload);
        Assert.True(log.Payload!.ContainsKey("raw"));
    }
}