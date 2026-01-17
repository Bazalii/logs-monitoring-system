using System.Text.Json;
using System.Text.Json.Serialization;

namespace Logly.Serialization.Options;

public static class SerializationOptions
{
    public static readonly JsonSerializerOptions SnakeCase = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}