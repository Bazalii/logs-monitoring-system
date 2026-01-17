using Logly.RawLogsHandler.Domain.Services.FilesParsers.Implementations;

namespace Logly.UnitTests.RawLogsHandler.FileParsers;

public sealed class TextFileParserTests
{
    [Fact]
    public async Task ParseFileAsync_ReadsAllLines_IncludingEmptyLines()
    {
        // Arrange
        var parser = new TextFileParser();
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");

        await File.WriteAllTextAsync(
            filePath,
            "line1\n\nline3\n");

        var lines = new List<string>();

        try
        {
            // Act
            await foreach (var line in parser.ReadLogsAsync(filePath, CancellationToken.None))
            {
                lines.Add(line);
            }

            // Assert
            Assert.Equal(2, lines.Count);
            Assert.Equal("line1", lines[0]);
            Assert.Equal("line3", lines[1]);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}