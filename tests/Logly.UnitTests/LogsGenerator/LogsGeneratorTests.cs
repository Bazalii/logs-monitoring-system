using System.Text.Json;
using System.Text.RegularExpressions;
using Logly.LogsGenerator.Domain.MessageQueues.Logs;
using Logly.LogsGenerator.Domain.Services;

namespace Logly.UnitTests.LogsGenerator;

public sealed class LogsGeneratorServiceTests
{
    [Fact]
    public async Task SendLogsToKafkaAsync_PeriodNull_SendsExactNumberOfLogs_WithCancellationTokenNone()
    {
        // Arrange
        var producer = new CapturingLogsProducer();
        var logsGeneratorService = new LogsGeneratorService(producer);

        // Act
        await logsGeneratorService.SendLogsToKafkaAsync(
            numberOfLogs: 5,
            period: null,
            format: "json",
            cancellation: CancellationToken.None);

        // Assert
        Assert.Equal(5, producer.Lines.Count);
        Assert.All(producer.Tokens, (t) => Assert.Equal(CancellationToken.None, t));
    }

    [Theory]
    [InlineData("json")]
    [InlineData("syslog")]
    [InlineData("clf")]
    [InlineData("unknown-format")]
    public async Task SendLogsToKafkaAsync_PeriodNull_UsesExpectedFormat(string format)
    {
        // Arrange
        var producer = new CapturingLogsProducer();
        var logsGeneratorService = new LogsGeneratorService(producer);

        // Act
        await logsGeneratorService.SendLogsToKafkaAsync(
            numberOfLogs: 1,
            period: null,
            format: format,
            cancellation: CancellationToken.None);

        // Assert
        Assert.Single(producer.Lines);
        var line = producer.Lines[0];

        switch (format)
        {
            case "syslog":
                AssertSyslog(line);
                break;
            case "clf":
                AssertClf(line);
                break;
            default:
                AssertJson(line);
                break;
        }
    }

    [Fact]
    public async Task SendLogsToKafkaAsync_PeriodProvided_SendsBatchesUntilCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sent = 0;

        var producer = new CapturingLogsProducer(
            onSend: () =>
            {
                sent++;
                if (sent >= 3)
                {
                    cts.Cancel();
                }
            });

        var logsGeneratorService = new LogsGeneratorService(producer);

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
            {
                await logsGeneratorService.SendLogsToKafkaAsync(
                    numberOfLogs: 3,
                    period: TimeSpan.FromHours(1),
                    format: "json",
                    cancellation: cts.Token);
            });

        // Assert
        Assert.Equal(3, producer.Lines.Count);
        Assert.All(producer.Lines, AssertJson);
    }

    private static bool TryGetPropertyAny(
        JsonElement obj,
        out JsonElement value,
        params string[] names)
    {
        foreach (var name in names)
        {
            if (obj.TryGetProperty(name, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static void AssertJson(string line)
    {
        // Arrange
        using var doc = JsonDocument.Parse(line);

        // Act
        var root = doc.RootElement;

        // Assert
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(TryGetPropertyAny(root, out var createdAt, "created_at", "createdAt"));
        Assert.True(TryGetPropertyAny(root, out var level, "level"));
        Assert.True(TryGetPropertyAny(root, out var source, "source"));
        Assert.True(TryGetPropertyAny(root, out var host, "host"));
        Assert.True(TryGetPropertyAny(root, out var env, "environment", "env"));
        Assert.True(TryGetPropertyAny(root, out var message, "message"));
        Assert.True(TryGetPropertyAny(root, out var payload, "payload"));

        Assert.Equal(JsonValueKind.String, createdAt.ValueKind);
        Assert.True(DateTimeOffset.TryParse(createdAt.GetString(), out _));

        Assert.Equal(JsonValueKind.String, level.ValueKind);
        Assert.Equal(JsonValueKind.String, source.ValueKind);
        Assert.Equal(JsonValueKind.String, host.ValueKind);
        Assert.Equal(JsonValueKind.String, env.ValueKind);
        Assert.Equal(JsonValueKind.String, message.ValueKind);

        Assert.Equal(JsonValueKind.Object, payload.ValueKind);
        Assert.True(TryGetPropertyAny(payload, out _, "user_id", "userId"));
        Assert.True(TryGetPropertyAny(payload, out _, "duration_ms", "durationMs"));
        Assert.True(TryGetPropertyAny(payload, out _, "http_status_code", "httpStatusCode"));

        // Optional fields depending on level / serializer null-handling:
        if (TryGetPropertyAny(payload, out var errorType, "error_type", "errorType"))
        {
            Assert.True(
                errorType.ValueKind is JsonValueKind.String or JsonValueKind.Null,
                "error_type must be string or null");
        }

        if (TryGetPropertyAny(payload, out var stackTrace, "stack_trace", "stackTrace"))
        {
            Assert.True(
                stackTrace.ValueKind is JsonValueKind.String or JsonValueKind.Null,
                "stack_trace must be string or null");
        }
    }

    private static void AssertSyslog(string line)
    {
        // Arrange
        // Example:
        // <14>1 2026-01-14T21:31:18.7782570+00:00 staging-server-01 auth-service 9236 REQ [logly@32473 ...] Operation completed
        var re = new Regex(
            @"^<(?<pri>\d{1,3})>1\s+(?<ts>\S+)\s+(?<host>\S+)\s+(?<app>\S+)\s+(?<proc>\d+)\s+(?<msgid>\S+)\s+(?<sd>\[logly@32473[^\]]*\])\s+(?<msg>.+)$",
            RegexOptions.Compiled);

        // Act
        var m = re.Match(line);

        // Assert
        Assert.True(m.Success);

        var pri = int.Parse(m.Groups["pri"].Value);
        Assert.True(pri is 11 or 12 or 14 or 15);

        Assert.True(DateTimeOffset.TryParse(m.Groups["ts"].Value, out _));

        var host = m.Groups["host"].Value;
        var app = m.Groups["app"].Value;
        Assert.False(string.IsNullOrWhiteSpace(host));
        Assert.False(string.IsNullOrWhiteSpace(app));

        var sd = m.Groups["sd"].Value;

        // Custom SD fields must exist
        Assert.Contains("source=\"", sd);
        Assert.Contains("host=\"", sd);
        Assert.Contains("environment=\"", sd);
        Assert.Contains("level=\"", sd);
        Assert.Contains("user_id=\"", sd);
        Assert.Contains("duration_ms=\"", sd);
        Assert.Contains("http_status_code=\"", sd);

        // Host/app appear both in header and in SD (not strictly required, but good sanity check)
        Assert.Contains($"host=\"{host}\"", sd);
        Assert.Contains($"source=\"{app}\"", sd);

        // Message text exists
        Assert.False(string.IsNullOrWhiteSpace(m.Groups["msg"].Value));
    }

    private static void AssertClf(string line)
    {
        // Arrange
        // 1.2.3.4 - 123 [14/Jan/2026:21:31:18 +0000] "GET /api/v1/orders?... HTTP/1.1" 200 1234
        var re = new Regex(
            @"^(?<ip>\d{1,3}(\.\d{1,3}){3})\s+-\s+(?<authuser>\S+)\s+\[(?<date>[^\]]+)\]\s+""(?<request>[^""]+)""\s+(?<status>\d{3})\s+(?<bytes>\d+)$",
            RegexOptions.Compiled);

        // Act
        var m = re.Match(line);

        // Assert
        Assert.True(m.Success);
        var dateRaw = m.Groups["date"].Value;
        Assert.False(string.IsNullOrWhiteSpace(dateRaw));

        // Try to parse using invariant (English month names) and current culture (e.g. RU month names).
        var parsed =
            DateTimeOffset.TryParseExact(
                dateRaw,
                "dd/MMM/yyyy:HH:mm:ss zzz",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out _)
            || DateTimeOffset.TryParseExact(
                dateRaw,
                "dd/MMM/yyyy:HH:mm:ss zzz",
                System.Globalization.CultureInfo.CurrentCulture,
                System.Globalization.DateTimeStyles.None,
                out _)
            || DateTimeOffset.TryParse(dateRaw, out _);

        Assert.True(parsed);

        var request = m.Groups["request"].Value;
        Assert.StartsWith("GET /api/v1/orders?", request);
        Assert.EndsWith(" HTTP/1.1", request);

        Assert.Contains("source=", request);
        Assert.Contains("host=", request);
        Assert.True(request.Contains("env=") || request.Contains("environment="));
        Assert.Contains("level=", request);

        var status = int.Parse(m.Groups["status"].Value);
        Assert.InRange(status, 100, 599);
    }

    private sealed class CapturingLogsProducer(Action? onSend = null) : ILogsProducer
    {
        public List<string> Lines { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];

        public Task SendLogAsync(string logLine, CancellationToken cancellation)
        {
            Lines.Add(logLine);
            Tokens.Add(cancellation);

            onSend?.Invoke();

            return Task.CompletedTask;
        }
    }
}