using Logly.RawLogsHandler.Domain.Services.LogParsers.Implementations;

namespace Logly.UnitTests.RawLogsHandler.LogParsers;

public sealed class SyslogParserTests
{
    [Fact]
    public void Parse_Rfc5424_UsesStructuredDataSourceHostEnvAndDoesNotDuplicateHostLevelInPayload()
    {
        // Arrange
        var parser = new SyslogParser();

        // APP is different from SD source on purpose
        const string raw = """
                           <14>1 2026-01-14T21:31:18.7782570+00:00 staging-server-01 kafka-app 9236 REQ [logly@32473 source="auth-service" host="staging-server-01" environment="staging" level="info" user_id="42"] Operation completed
                           """;

        // Act
        var log = parser.Parse(raw);

        // Assert
        // Expect SD values to win:
        Assert.Equal(
            "auth-service",
            log.Source);
        Assert.Equal("staging-server-01", log.Host);
        Assert.Equal("staging", log.Environment);
        Assert.Equal("Info", log.Level);

        // Payload should NOT contain host/level/source/environment duplicates:
        Assert.NotNull(log.Payload);
        Assert.False(log.Payload.ContainsKey("host"));
        Assert.False(log.Payload.ContainsKey("level"));
        Assert.False(log.Payload.ContainsKey("source"));
        Assert.False(log.Payload.ContainsKey("environment"));

        // Custom payload should exist:
        Assert.True(log.Payload.ContainsKey("user_id"));
    }
}