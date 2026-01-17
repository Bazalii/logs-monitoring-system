using Logly.RawLogsHandler.Domain.Services.FilesParsers.Facades;
using Logly.RawLogsHandler.Domain.Services.FilesParsers.Implementations;

namespace Logly.UnitTests.RawLogsHandler.Facades;

public sealed class FileParsersFacadeTests
{
    [Fact]
    public async Task ParseFileAsync_Txt_UsesTextFileParser()
    {
        // Arrange
        var facade = new FileParsersFacade(new TextFileParser(), new JsonFileParser());
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        await File.WriteAllTextAsync(filePath, "a\nb\n");

        var result = new List<string>();

        try
        {
            // Act
            await foreach (var line in facade.ParseFileAsync(filePath, CancellationToken.None))
            {
                result.Add(line);
            }

            // Assert
            Assert.Equal(new[] { "a", "b" }, result);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ParseFileAsync_Json_UsesJsonFileParser()
    {
        // Arrange
        var facade = new FileParsersFacade(new TextFileParser(), new JsonFileParser());
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        await File.WriteAllTextAsync(filePath, """[{"a":1},{"b":2}]""");

        var result = new List<string>();

        try
        {
            // Act
            await foreach (var item in facade.ParseFileAsync(filePath, CancellationToken.None))
            {
                result.Add(item);
            }

            // Assert
            Assert.Equal(2, result.Count);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}