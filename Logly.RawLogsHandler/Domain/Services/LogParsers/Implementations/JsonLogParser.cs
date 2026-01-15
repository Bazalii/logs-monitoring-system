using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using Logly.RawLogsHandler.Domain.Models.Parsing;
using Logly.RawLogsHandler.Infrastructure.Helpers;

namespace Logly.RawLogsHandler.Domain.Services.LogParsers.Implementations;

public sealed class JsonLogParser : IParser
{
    private static readonly HashSet<string> Standard = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "created_at", "timestamp", "time", "received_at", "level",
        "source", "host", "environment", "env", "message", "msg", "payload"
    };

    public LogEntry Parse(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Object)
        {
            return ParseOneObject(root, raw);
        }

        throw new ArgumentException("Invalid JSON format", nameof(raw));
    }

    private static LogEntry ParseOneObject(JsonElement obj, string rawForId)
    {
        var receivedAt = DateTimeOffset.UtcNow;

        var createdAt =
            TryGetDateTimeOffset(obj, "created_at") ??
            TryGetDateTimeOffset(obj, "timestamp") ??
            TryGetDateTimeOffset(obj, "time") ??
            receivedAt;

        var level = TryGetString(obj, "level") ?? "Info";
        level = NormalizeLevel(level);

        var source = TryGetString(obj, "source") ?? "unknown";
        var host = TryGetString(obj, "host") ?? "unknown";
        var env = TryGetString(obj, "environment") ?? TryGetString(obj, "env") ?? "unknown";

        var message = TryGetString(obj, "message") ?? TryGetString(obj, "msg") ?? obj.GetRawText();
        var payload = BuildPayloadFromJson(obj);

        var id = TryGetUInt64(obj, "id")
                 ?? LogIdGenerator.GenerateStableUInt64($"{source}|{host}|{env}|{createdAt:O}|{message}|{rawForId}");

        return new LogEntry(
            id, createdAt, receivedAt, level,
            source, host, env, message, payload);
    }

    private static ReadOnlyDictionary<string, object?> BuildPayloadFromJson(JsonElement obj)
    {
        var payload = new Dictionary<string, object?>();

        if (obj.TryGetProperty("payload", out var payloadEl) && payloadEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in payloadEl.EnumerateObject())
            {
                payload[p.Name] = JsonToPlainObject(p.Value);
            }
        }

        foreach (var p in obj.EnumerateObject())
        {
            if (Standard.Contains(p.Name))
            {
                continue;
            }

            payload[p.Name] = JsonToPlainObject(p.Value);
        }

        return payload.AsReadOnly();
    }

    private static object? JsonToPlainObject(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? l :
                el.TryGetDouble(out var d) ? d : el.GetRawText(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => el.EnumerateObject().ToDictionary(p => p.Name, p => JsonToPlainObject(p.Value)),
            JsonValueKind.Array => el.EnumerateArray().Select(JsonToPlainObject).ToList(),
            _ => el.GetRawText()
        };

    private static string? TryGetString(JsonElement obj, string name)
    {
        if (obj.TryGetProperty(name, out var el) is false)
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.String)
        {
            return el.GetString();
        }

        return el.ToString();
    }

    private static DateTimeOffset? TryGetDateTimeOffset(JsonElement obj, string name)
    {
        if (obj.TryGetProperty(name, out var el) is false)
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.String)
        {
            var s = el.GetString();
            if (string.IsNullOrWhiteSpace(s))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(
                    s, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
            {
                return dto;
            }

            if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
            {
                return ConvertUnixToDto(l);
            }
        }
        else if (el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var n))
        {
            return ConvertUnixToDto(n);
        }

        return null;
    }

    private static DateTimeOffset ConvertUnixToDto(long value)
    {
        return value >= 1_000_000_000_000L
            ? DateTimeOffset.FromUnixTimeMilliseconds(value).ToUniversalTime()
            : DateTimeOffset.FromUnixTimeSeconds(value).ToUniversalTime();
    }

    private static ulong? TryGetUInt64(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el))
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetUInt64(out var v))
        {
            return v;
        }

        if (el.ValueKind == JsonValueKind.String && ulong.TryParse(el.GetString(), out var vs))
        {
            return vs;
        }

        return null;
    }

    private static string NormalizeLevel(string level)
    {
        var s = level.Trim().ToLowerInvariant();

        return s switch
        {
            "critical" or "crit" or "fatal" => "Critical",
            "error" or "err" => "Error",
            "warning" or "warn" => "Warning",
            "debug" => "Debug",
            "info" or "information" => "Info",
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s)
        };
    }
}