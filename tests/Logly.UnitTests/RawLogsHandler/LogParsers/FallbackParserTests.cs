using Logly.RawLogsHandler.Domain.Services.LogParsers.Implementations;

namespace Logly.UnitTests.RawLogsHandler.LogParsers;

public sealed class FallbackParserTests
{
    [Fact]
    public void Parse_AlwaysReturnsUnknownWithRawInPayload()
    {
        // Arrange
        var parser = new FallbackParser();
        const string raw = "some totally unknown format";

        // Act
        var log = parser.Parse(raw);

        // Assert
        Assert.Equal("unknown", log.Source);
        Assert.Equal("unknown", log.Host);
        Assert.Equal("unknown", log.Environment);
        Assert.Equal("Info", log.Level);
        Assert.NotNull(log.Payload);
        Assert.True(log.Payload.ContainsKey("raw"));
        Assert.Equal(raw, log.Payload["raw"]);
    }
}