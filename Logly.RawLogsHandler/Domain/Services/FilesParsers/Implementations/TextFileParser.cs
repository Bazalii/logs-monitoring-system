using System.Runtime.CompilerServices;

namespace Logly.RawLogsHandler.Domain.Services.FilesParsers.Implementations;

public sealed class TextFileParser : IFileParser
{
    public async IAsyncEnumerable<string> ReadLogsAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellation)
    {
        await foreach (var line in File.ReadLinesAsync(filePath, cancellation))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            yield return line;
        }
    }
}