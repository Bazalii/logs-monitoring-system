using System.Text.Json;
using JsonSerializer = Logly.Serialization.Serializers.Implementations.JsonSerializer;

namespace Logly.UnitTests.Serialization;

public class JsonSerializerTests
{
    private sealed record Person(string FirstName, int Age);

    [Fact]
    public void SerializeToString_UsesProvidedOptions()
    {
        // Arrange
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var sut = new JsonSerializer(options);

        // Act
        var json = sut.SerializeToString(new Person("Ada", 33));

        // Assert
        Assert.Contains("\"firstName\"", json);
        Assert.Contains("\"age\"", json);
        Assert.Contains("\"Ada\"", json);
        Assert.Contains("33", json);
    }

    [Fact]
    public void SerializeToBytes_ProducesValidUtf8Json()
    {
        // Arrange
        var sut = new JsonSerializer(new JsonSerializerOptions());

        // Act
        var bytes = sut.SerializeToBytes(new Person("Ada", 33));

        // Assert
        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        var parsed = System.Text.Json.JsonSerializer.Deserialize<Person>(bytes);
        Assert.Equal(new Person("Ada", 33), parsed);
    }

    [Fact]
    public void Deserialize_FromBytes_RoundTrips()
    {
        // Arrange
        var sut = new JsonSerializer(new JsonSerializerOptions());
        var payload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new Person("Ada", 33));

        // Act
        var result = sut.Deserialize<Person>(payload);

        // Assert
        Assert.Equal(new Person("Ada", 33), result);
    }

    [Fact]
    public void Deserialize_FromString_RoundTrips()
    {
        // Arrange
        var sut = new JsonSerializer(new JsonSerializerOptions());
        var payload = System.Text.Json.JsonSerializer.Serialize(new Person("Ada", 33));

        // Act
        var result = sut.Deserialize<Person>(payload);

        // Assert
        Assert.Equal(new Person("Ada", 33), result);
    }

    [Fact]
    public void Deserialize_FromBytes_ThrowsInvalidOperation_WhenJsonIsNull()
    {
        // Arrange
        var sut = new JsonSerializer(new JsonSerializerOptions());
        var payload = "null"u8.ToArray();

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => sut.Deserialize<Person?>(payload));

        // Assert
        Assert.Equal("Json deserialization returned null.", ex.Message);
    }

    [Fact]
    public void Deserialize_FromString_ThrowsInvalidOperation_WhenJsonIsNull()
    {
        // Arrange
        var sut = new JsonSerializer(new JsonSerializerOptions());
        var payload = "null";

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => sut.Deserialize<Person?>(payload));

        // Assert
        Assert.Equal("Json deserialization returned null.", ex.Message);
    }

    [Fact]
    public void SnakeCase_StaticInstance_UsesSnakeCaseOptions_ForPropertyNames()
    {
        // Arrange
        var value = new { FirstName = "Ada", LastName = "Lovelace" };

        // Act
        var json = JsonSerializer.SnakeCase.SerializeToString(value);

        // Assert
        Assert.Contains("\"first_name\"", json);
        Assert.Contains("\"last_name\"", json);
    }

    [Fact]
    public void Deserialize_PropagatesJsonException_ForInvalidJson()
    {
        // Arrange
        var sut = new JsonSerializer(new JsonSerializerOptions());

        // Act + Assert
        Assert.Throws<JsonException>(() => sut.Deserialize<Person>("{ this is not json"));
    }
}