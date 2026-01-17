using Logly.RawLogsHandler.Domain.Services.LogParsers.Implementations;

namespace Logly.UnitTests.RawLogsHandler.LogParsers;

public sealed class ClfParserTests
{
    [Fact]
    public void Parse_ValidClf_ExtractsHostAndStatusAndQueryStringFields()
    {
        // Arrange
        var parser = new ClfParser();
        const string raw = """
                           127.0.0.1 - 123 [14/Jan/2026:21:31:18 +0000] "GET /api/v1/orders?source=payment-service&host=prod-01&env=production&level=error HTTP/1.1" 504 123
                           """;

        // Act
        var log = parser.Parse(raw);

        // Assert
        Assert.Equal("web-access", log.Source);
        Assert.Equal("127.0.0.1", log.Host);
        Assert.Equal("unknown", log.Environment);
        Assert.Equal("Error", log.Level);
        Assert.NotNull(log.Payload);
        Assert.True(log.Payload.ContainsKey("http_status_code"));
    }
}