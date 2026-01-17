using Logly.RawLogsHandler.Domain.Services.FilesParsers.Implementations;

namespace Logly.RawLogsHandler.Domain.Services.FilesParsers.Facades;

public sealed class FileParsersFacade(
    TextFileParser textFileParser,
    JsonFileParser jsonFileParser)
{
    public IAsyncEnumerable<string> ParseFileAsync(
        string filePath,
        CancellationToken cancellation)
    {
        var extension = Path
            .GetExtension(filePath)
            .ToLowerInvariant();

        if (string.IsNullOrEmpty(extension))
        {
            throw new ArgumentException($"File '{filePath}' does not have a valid extension.", nameof(filePath));
        }

        IFileParser parser = extension switch
        {
            ".log" or ".txt" or ".csv" => textFileParser,
            ".json" => jsonFileParser,
            _ => throw new ArgumentException($"'{filePath}' files are not supported.", nameof(filePath))
        };

        return parser.ReadLogsAsync(filePath, cancellation);
    }
}