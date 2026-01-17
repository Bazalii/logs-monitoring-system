namespace Logly.RawLogsHandler.Domain.Services.FilesParsers;

public interface IFileParser
{
    IAsyncEnumerable<string> ReadLogsAsync(
        string filePath,
        CancellationToken cancellation);
}