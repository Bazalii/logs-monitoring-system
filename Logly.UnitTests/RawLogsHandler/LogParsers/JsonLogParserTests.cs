using Logly.RawLogsHandler.Domain.Services.LogParsers.Implementations;
using System.Text.Json;

namespace Logly.UnitTests.RawLogsHandler.LogParsers;

public sealed class JsonLogParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsLogEntryWithExpectedFields()
    {
        // Arrange
        var parser = new JsonLogParser();
        const string raw = """
                           {
                             "created_at": "2026-01-01T00:00:00Z",
                             "level": "error",
                             "source": "payment-service",
                             "host": "prod-01",
                             "environment": "production",
                             "message": "Payment failed",
                             "payload": { "error_type": "TimeoutException" }
                           }
                           """;

        // Act
        var log = parser.Parse(raw);

        // Assert
        Assert.Equal("Error", log.Level);
        Assert.Equal("payment-service", log.Source);
        Assert.Equal("prod-01", log.Host);
        Assert.Equal("production", log.Environment);
        Assert.Equal("Payment failed", log.Message);
        Assert.NotNull(log.Payload);
        Assert.True(log.Payload.ContainsKey("error_type"));
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsArgumentException()
    {
        // Arrange
        var parser = new JsonLogParser();

        // Act + Assert
        Assert.ThrowsAny<JsonException>(() => parser.Parse("{"));
    }
}