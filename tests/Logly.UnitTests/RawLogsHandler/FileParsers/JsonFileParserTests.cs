using System.IO;
using System.Text.Json;
using Logly.RawLogsHandler.Domain.Services.FilesParsers.Implementations;

namespace Logly.UnitTests.RawLogsHandler.FileParsers;

public sealed class JsonFileParserTests
{
    [Fact]
    public async Task ParseFileAsync_JsonArray_YieldsElementsAsJsonStrings()
    {
        // Arrange
        var parser = new JsonFileParser();
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

        await File.WriteAllTextAsync(
            filePath,
            """
            [
              {"a":1},
              {"b":"x"},
              {"c":true}
            ]
            """);

        var result = new List<string>();

        try
        {
            // Act
            await foreach (var item in parser.ReadLogsAsync(filePath, CancellationToken.None))
            {
                result.Add(item);
            }

            // Assert
            Assert.Equal(3, result.Count);

            // Validate every yielded string is valid JSON object
            foreach (var json in result)
            {
                using var doc = JsonDocument.Parse(json);
                Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
            }
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ParseFileAsync_JsonNotArray_ThrowsInvalidDataException()
    {
        // Arrange
        var parser = new JsonFileParser();
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        await File.WriteAllTextAsync(filePath, """{"a":1}""");

        try
        {
            // Act
            var ex = await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await foreach (var _ in parser.ReadLogsAsync(filePath, CancellationToken.None))
                {
                }
            });

            // Assert
            Assert.Contains("root array", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ParseFileAsync_InvalidJson_ThrowsInvalidDataException()
    {
        // Arrange
        var parser = new JsonFileParser();
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        await File.WriteAllTextAsync(filePath, """[{""");

        try
        {
            // Act + Assert
            var ex = await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await foreach (var _ in parser.ReadLogsAsync(filePath, CancellationToken.None))
                {
                }
            });
            Assert.IsAssignableFrom<JsonException>(ex.InnerException);
            Assert.Contains("Invalid JSON", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}