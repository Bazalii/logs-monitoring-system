using Logly.LogsGenerator.Domain.MessageQueues.Logs;
using Logly.Serialization.Serializers.Implementations;

namespace Logly.LogsGenerator.Domain.Services;

public sealed class LogsGeneratorService(
    ILogsProducer logsProducer)
    : ILogsGeneratorService
{
    public async Task SendLogsToKafkaAsync(
        int numberOfLogs,
        TimeSpan? period,
        string format,
        CancellationToken cancellation)
    {
        if (period is null)
        {
            await SendLogs(numberOfLogs, format);

            return;
        }

        while (cancellation.IsCancellationRequested is false)
        {
            await SendLogs(numberOfLogs, format);

            await Task.Delay(period.Value, cancellation);
        }
    }

    private async Task SendLogs(int numberOfLogs, string format)
    {
        for (var i = 0; i < numberOfLogs; i++)
        {
            var logLine = format switch
            {
                "json" => GenerateJsonLogLine(),
                "syslog" => GenerateSyslogLogLine(),
                "clf" => GenerateClfLogLine(),
                _ => GenerateJsonLogLine()
            };

            await logsProducer.SendLogAsync(
                logLine,
                CancellationToken.None);
        }
    }

    private string GenerateJsonLogLine()
    {
        var now = DateTimeOffset.UtcNow;

        var level = Pick("debug", "info", "warning", "error");
        var source = Pick("payment-service", "auth-service", "orders-service", "api-gateway");
        var host = Pick("prod-server-01", "prod-server-02", "staging-server-01");
        var environment = Pick("production", "staging", "dev");

        var payload = new
        {
            user_id = Random.Shared.Next(1, 5000),
            duration_ms = Random.Shared.Next(1, 10_000),
            http_status_code = Pick(200, 201, 400, 401, 403, 404, 500, 502, 503, 504),
            error_type = level == "error"
                ? Pick("TimeoutException", "ValidationException", "NullReferenceException")
                : null,
            stack_trace = level == "error" && Random.Shared.NextDouble() < 0.2
                ? "at Logly.App.SomeMethod()\n at Logly.App.Program.Main()"
                : null
        };

        var value = new
        {
            created_at = now.ToString("O"),
            level,
            source,
            host,
            environment,
            message = level == "error" ? "Request failed" : "Request processed",
            payload
        };

        return JsonSerializer.SnakeCase.SerializeToString(value);
    }

    static string GenerateSyslogLogLine()
    {
        // RFC5424: <PRI>VERSION TIMESTAMP HOST APP PROCID MSGID [SD] MSG
        var now = DateTimeOffset.UtcNow.ToString("O");

        var level = Pick("debug", "info", "warning", "error");
        var source = Pick("payment-service", "auth-service", "orders-service", "api-gateway");
        var host = Pick("prod-server-01", "prod-server-02", "staging-server-01");
        var environment = Pick("production", "staging", "dev");

        var pri = level switch
        {
            "error" => 11,
            "warning" => 12,
            "info" => 14,
            _ => 15
        };

        var procId = Random.Shared.Next(1000, 9999);
        var msgId = Pick("REQ", "PAY", "AUTH", "ORD");

        // Custom structured data used by our parsers to fill source/host/env.
        var userId = Random.Shared.Next(1, 5000);
        var durationMs = Random.Shared.Next(1, 10_000);
        var httpStatusCode = Pick(200, 201, 400, 401, 403, 404, 500, 502, 503, 504);
        var errorType = level == "error"
            ? Pick("TimeoutException", "ValidationException", "NullReferenceException")
            : null;
        var stackTrace = level == "error" && Random.Shared.NextDouble() < 0.2
            ? "at Logly.App.SomeMethod()\\n at Logly.App.Program.Main()"
            : null;

        var sd = $"[logly@32473 " +
                 $"source=\"{source}\" " +
                 $"host=\"{host}\" " +
                 $"environment=\"{environment}\" " +
                 $"level=\"{level}\" " +
                 $"user_id=\"{userId}\" " +
                 $"duration_ms=\"{durationMs}\" " +
                 $"http_status_code=\"{httpStatusCode}\"" +
                 (errorType is not null ? $" error_type=\"{errorType}\"" : string.Empty) +
                 (stackTrace is not null ? $" stack_trace=\"{stackTrace}\"" : string.Empty) +
                 "]";

        var message = level == "error" ? "Operation failed" : "Operation completed";

        return $"<{pri}>1 {now} {host} {source} {procId} {msgId} {sd} {message}";
    }

    static string GenerateClfLogLine()
    {
        // Common Log Format: host ident authuser [date] "request" status bytes
        // Custom fields are added as query string parameters so the CLF parser can extract them.

        var ip =
            $"{Random.Shared.Next(1, 255)}.{Random.Shared.Next(0, 255)}.{Random.Shared.Next(0, 255)}.{Random.Shared.Next(1, 255)}";

        var authUser = Random.Shared.NextDouble() < 0.7
            ? $"{Random.Shared.Next(1, 5000)}"
            : "-";

        var source = Pick("payment-service", "auth-service", "orders-service", "api-gateway");
        var host = Pick("prod-server-01", "prod-server-02", "staging-server-01");
        var environment = Pick("production", "staging", "dev");
        var level = Pick("debug", "info", "warning", "error");

        var ts = DateTimeOffset.UtcNow.ToString("dd/MMM/yyyy:HH:mm:ss zzz");

        var status = Pick(200, 201, 400, 401, 403, 404, 500, 502, 503, 504);
        var bytes = Random.Shared.Next(100, 50_000);

        var path =
            $"/api/v1/orders?source={Uri.EscapeDataString(source)}&host={Uri.EscapeDataString(host)}&env={Uri.EscapeDataString(environment)}&level={Uri.EscapeDataString(level)}";
        var request = $"GET {path} HTTP/1.1";

        return $"{ip} - {authUser} [{ts}] \"{request}\" {status} {bytes}";
    }

    private static T Pick<T>(params T[] values)
    {
        return values[Random.Shared.Next(0, values.Length)];
    }
}