using System;
using System.Text.Json;
using Logly.Serialization.Options;

namespace Logly.Serialization.Serializers.Implementations;

public sealed class JsonSerializer(
    JsonSerializerOptions options)
    : ISerializer
{
    public static readonly ISerializer SnakeCase = new JsonSerializer(SerializationOptions.SnakeCase);

    public byte[] SerializeToBytes<T>(T value)
    {
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value, options);
    }

    public string SerializeToString<T>(T value)
    {
        return System.Text.Json.JsonSerializer.Serialize(value, options);
    }

    public T Deserialize<T>(ReadOnlySpan<byte> payload)
    {
        var result = System.Text.Json.JsonSerializer.Deserialize<T>(payload, options);

        return result ?? throw new InvalidOperationException("Json deserialization returned null.");
    }

    public T Deserialize<T>(string payload)
    {
        var result = System.Text.Json.JsonSerializer.Deserialize<T>(payload, options);

        return result ?? throw new InvalidOperationException("Json deserialization returned null.");
    }
}