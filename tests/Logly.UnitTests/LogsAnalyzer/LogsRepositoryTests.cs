using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;
using Logly.LogsAnalyzer.Domain.Models.Core.Logs;
using Logly.LogsAnalyzer.Domain.Repositories.Logs;
using Logly.LogsAnalyzer.Infrastructure.Models;
using Microsoft.Extensions.Options;

namespace Logly.UnitTests.LogsAnalyzer;

public class LogsRepositoryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Throws_WhenConnectionStringMissing(string? cs)
    {
        // Arrange
        var opts = Options.Create(
            new ClickHouseOptions
            {
                ConnectionString = cs!,
                Table = "logs",
                BatchSize = 1
            });

        // Act
        var ex = Assert.Throws<ArgumentException>(() => new LogsRepository(opts));

        // Assert
        Assert.Equal("ClickHouseOptions.ConnectionString is required.", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Throws_WhenTableMissing(string? table)
    {
        // Arrange
        var opts = Options.Create(
            new ClickHouseOptions
            {
                ConnectionString = "Host=localhost;Port=9000;",
                Table = table!,
                BatchSize = 1
            });

        // Act
        var ex = Assert.Throws<ArgumentException>(() => new LogsRepository(opts));

        // Assert
        Assert.Equal("ClickHouseOptions.Table is required.", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public void Ctor_Throws_WhenBatchSizeNotPositive(int batchSize)
    {
        // Arrange
        var opts = Options.Create(
            new ClickHouseOptions
            {
                ConnectionString = "Host=localhost;Port=9000;",
                Table = "logs",
                BatchSize = batchSize
            });

        // Act
        var ex = Assert.Throws<ArgumentException>(() => new LogsRepository(opts));

        // Assert
        Assert.Equal("ClickHouseOptions.BatchSize must be > 0.", ex.Message);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    public void DeserializePayload_ReturnsNull_WhenWhitespace(string payload)
    {
        // Arrange
        // Act
        var result = InvokePrivateStatic<ReadOnlyDictionary<string, object?>?>("DeserializePayload", payload);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeserializePayload_ReturnsDictionary_ForJsonObject_AndIsCaseInsensitive()
    {
        // Arrange
        var payload = "{\"Name\":\"Ada\",\"age\":33,\"ok\":true,\"n\":null}";

        // Act
        var dict = InvokePrivateStatic<ReadOnlyDictionary<string, object?>?>("DeserializePayload", payload);

        // Assert
        Assert.NotNull(dict);
        Assert.Equal("Ada", dict!["name"]); // case-insensitive lookup
        Assert.Equal(33L, dict["AGE"]); // numbers prefer Int64
        Assert.Equal(true, dict["Ok"]);
        Assert.Null(dict["N"]);
    }

    [Fact]
    public void DeserializePayload_ReturnsRawWrapper_WhenJsonIsNotObject()
    {
        // Arrange
        var payload = "[1,2,3]";

        // Act
        var dict = InvokePrivateStatic<ReadOnlyDictionary<string, object?>?>("DeserializePayload", payload);

        // Assert
        Assert.NotNull(dict);
        Assert.Single(dict!);
        Assert.Equal(payload, dict["raw"]);
    }

    [Fact]
    public void DeserializePayload_ReturnsRawWrapper_WhenJsonInvalid()
    {
        // Arrange
        var payload = "{ this is not json";

        // Act
        var dict = InvokePrivateStatic<ReadOnlyDictionary<string, object?>?>("DeserializePayload", payload);

        // Assert
        Assert.NotNull(dict);
        Assert.Single(dict!);
        Assert.Equal(payload, dict["raw"]);
    }

    [Fact]
    public void ConvertJsonElement_ConvertsNestedObjectsAndArrays()
    {
        // Arrange: { "obj": { "x": 1 }, "arr": [true, "s", null, 2.5] }
        var payload = "{\"obj\":{\"x\":1},\"arr\":[true,\"s\",null,2.5]}";

        // Act
        var dict = InvokePrivateStatic<ReadOnlyDictionary<string, object?>?>("DeserializePayload", payload)!;

        // Assert
        // Assert object -> ReadOnlyDictionary<string, object?>
        var obj = Assert.IsType<ReadOnlyDictionary<string, object?>>(dict["obj"]);
        Assert.Equal(1L, obj["x"]);

        // Assert array -> List<object?>
        var arr = Assert.IsType<List<object?>>(dict["arr"]);
        Assert.Equal(true, arr[0]);
        Assert.Equal("s", arr[1]);
        Assert.Null(arr[2]);

        // 2.5 should become double (TryInt64 fails; TryDouble succeeds)
        Assert.IsType<double>(arr[3]);
        Assert.Equal(2.5, (double)arr[3]!, 5);
    }

    [Fact]
    public void ReadDateTimeOffsetUtc_TreatsUnspecifiedKindAsUtc()
    {
        // Arrange
        // Unspecified => specify as UTC then ToUniversalTime
        var unspecified = new DateTime(2026, 01, 17, 10, 00, 00, DateTimeKind.Unspecified);
        var record = new FakeDataRecord(new object[] { unspecified });

        // Act
        var dto = InvokePrivateStatic<DateTimeOffset>("ReadDateTimeOffsetUtc", record, 0);

        // Assert
        Assert.Equal(TimeSpan.Zero, dto.Offset);
        Assert.Equal(DateTimeKind.Utc, dto.UtcDateTime.Kind);
        Assert.Equal(new DateTime(2026, 01, 17, 10, 00, 00, DateTimeKind.Utc), dto.UtcDateTime);
    }

    [Fact]
    public void BuildRows_SerializesNullPayloadAsEmptyObject()
    {
        // Arrange
        var log = new FakeLog
        {
            CreatedAt = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow,
            Level = "INFO",
            Source = "src",
            Host = "h",
            Environment = "env",
            Message = "msg",
            Payload = null
        };

        // Act
        var rows = InvokePrivateStatic<IEnumerable<object[]>>("BuildRows", (object?)new ILog[] { log }).ToList();

        // Assert
        var row = Assert.Single(rows);
        Assert.Equal("{}", row[7]);
    }

    [Fact]
    public void BuildRows_SerializesPayload_WithSnakeCaseSerializer()
    {
        // Arrange
        var log = new FakeLog
        {
            CreatedAt = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow,
            Level = "INFO",
            Source = "src",
            Host = "h",
            Environment = "env",
            Message = "msg",
            Payload = new Dictionary<string, object?>
            {
                ["FirstName"] = "Ada",
                ["LastName"] = "Lovelace"
            }.AsReadOnly()
        };

        // Act
        var rows = InvokePrivateStatic<IEnumerable<object[]>>("BuildRows", (object?)new ILog[] { log }).ToList();

        // Assert
        var row = Assert.Single(rows);

        var json = Assert.IsType<string>(row[7]);
        Assert.Contains("\"FirstName\"", json);
        Assert.Contains("\"LastName\"", json);
    }


    private static T InvokePrivateStatic<T>(string methodName, params object?[] args)
    {
        var t = typeof(LogsRepository);
        var mi = t.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                 ?? throw new MissingMethodException(t.FullName, methodName);

        var result = mi.Invoke(null, args);

        return (T)result!;
    }

    private sealed class FakeLog : ILog
    {
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset ReceivedAt { get; init; }
        public string Level { get; init; } = "";
        public string Source { get; init; } = "";
        public string Host { get; init; } = "";
        public string Environment { get; init; } = "";
        public string Message { get; init; } = "";
        public ReadOnlyDictionary<string, object?>? Payload { get; init; }
    }

    private sealed class FakeDataRecord(object[] values) : IDataRecord
    {
        public DateTime GetDateTime(int i) => (DateTime)values[i];

        // Only members used by ReadDateTimeOffsetUtc are required. Everything else can throw.
        public object this[int i] => values[i];
        public object this[string name] => throw new NotSupportedException();
        public int FieldCount => values.Length;

        public bool GetBoolean(int i) => throw new NotSupportedException();
        public byte GetByte(int i) => throw new NotSupportedException();

        public long GetBytes(
            int i, long fieldOffset,
            byte[]? buffer, int bufferoffset,
            int length) => throw new NotSupportedException();

        public char GetChar(int i) => throw new NotSupportedException();

        public long GetChars(
            int i, long fieldoffset,
            char[]? buffer, int bufferoffset,
            int length) => throw new NotSupportedException();

        public IDataReader GetData(int i) => throw new NotSupportedException();
        public string GetDataTypeName(int i) => throw new NotSupportedException();
        public decimal GetDecimal(int i) => throw new NotSupportedException();
        public double GetDouble(int i) => throw new NotSupportedException();
        public Type GetFieldType(int i) => throw new NotSupportedException();
        public float GetFloat(int i) => throw new NotSupportedException();
        public Guid GetGuid(int i) => throw new NotSupportedException();
        public short GetInt16(int i) => throw new NotSupportedException();
        public int GetInt32(int i) => throw new NotSupportedException();
        public long GetInt64(int i) => throw new NotSupportedException();
        public string GetName(int i) => throw new NotSupportedException();
        public int GetOrdinal(string name) => throw new NotSupportedException();
        public string GetString(int i) => throw new NotSupportedException();
        public object GetValue(int i) => values[i];

        public int GetValues(object[] values1)
        {
            Array.Copy(values, values1, values.Length);
            return values.Length;
        }

        public bool IsDBNull(int i) => values[i] is null or DBNull;
    }
}