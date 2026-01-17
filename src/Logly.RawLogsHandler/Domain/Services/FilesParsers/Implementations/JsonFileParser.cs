using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Logly.RawLogsHandler.Domain.Services.FilesParsers.Implementations;

public sealed class JsonFileParser : IFileParser
{
    private readonly int _bufferSize;

    public JsonFileParser(int bufferSize = 64 * 1024)
    {
        if (bufferSize < 4 * 1024)
        {
            bufferSize = 4 * 1024;
        }

        _bufferSize = bufferSize;
    }

    public async IAsyncEnumerable<string> ReadLogsAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("JSON file not found.", filePath);
        }

        await using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: _bufferSize, useAsync: true);

        var options = new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        var state = new JsonReaderState(options);

        var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        var bytesInBuffer = 0;

        var rootArrayStarted = false;
        var rootArrayEnded = false;

        try
        {
            while (rootArrayEnded is false)
            {
                cancellation.ThrowIfCancellationRequested();

                if (bytesInBuffer == buffer.Length)
                {
                    throw new InvalidDataException("JSON is too large or malformed (buffer overflow).");
                }

                var read = await stream.ReadAsync(
                    buffer.AsMemory(bytesInBuffer, buffer.Length - bytesInBuffer),
                    cancellation);

                var isFinalBlock = read == 0;
                bytesInBuffer += read;

                // It is not possible to do `yield return` while a `Utf8JsonReader` (ref struct) is in scope.
                // So the current buffer chunk is parsed, after that items are collected, and then yielded after the reader goes out of scope.
                var itemsToYield = new List<string>();
                int consumed;

                try
                {
                    var localReader = new Utf8JsonReader(buffer.AsSpan(0, bytesInBuffer), isFinalBlock, state);

                    while (localReader.Read())
                    {
                        cancellation.ThrowIfCancellationRequested();

                        if (!rootArrayStarted)
                        {
                            // First meaningful token must be StartArray
                            if (localReader.TokenType != JsonTokenType.StartArray)
                            {
                                throw new InvalidDataException(
                                    $"JSON file must contain a root array ([]). Actual root token: {localReader.TokenType}");
                            }

                            rootArrayStarted = true;
                            continue;
                        }

                        // Inside root array
                        if (localReader.TokenType == JsonTokenType.EndArray)
                        {
                            rootArrayEnded = true;
                            break;
                        }

                        // Each item is a JSON value (object/array/string/number/true/false/null)
                        // localReader is currently positioned on the first token of that value.
                        using var doc = JsonDocument.ParseValue(ref localReader);
                        itemsToYield.Add(doc.RootElement.GetRawText());

                        // After ParseValue, localReader is positioned at the END token of that value.
                        // Next localReader.Read() will continue from the next token (comma / next item / EndArray).
                    }

                    state = localReader.CurrentState;
                    consumed = (int)localReader.BytesConsumed;
                }
                catch (JsonException ex)
                {
                    throw new InvalidDataException($"Invalid JSON in file: {filePath}", ex);
                }

                // Yield after the reader is out of scope
                foreach (var json in itemsToYield)
                {
                    cancellation.ThrowIfCancellationRequested();

                    yield return json;
                }

                // `consumed` was captured from the local reader above
                if (consumed > 0)
                {
                    Buffer.BlockCopy(buffer, consumed, buffer, 0, bytesInBuffer - consumed);
                    bytesInBuffer -= consumed;
                }

                if (isFinalBlock)
                {
                    // EOF reached
                    if (rootArrayStarted is false)
                    {
                        throw new InvalidDataException("JSON file is empty or does not contain a root array.");
                    }

                    if (rootArrayEnded is false)
                    {
                        throw new InvalidDataException("JSON root array was not closed (missing ']').");
                    }

                    break;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}