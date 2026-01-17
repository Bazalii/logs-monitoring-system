using System;

namespace Logly.Serialization.Serializers;

public interface ISerializer
{
    byte[] SerializeToBytes<T>(T value);
    string SerializeToString<T>(T value);
    T Deserialize<T>(ReadOnlySpan<byte> payload);
    T Deserialize<T>(string payload);
}