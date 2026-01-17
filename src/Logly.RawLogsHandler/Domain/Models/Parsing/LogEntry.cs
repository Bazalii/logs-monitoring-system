using System.Collections.ObjectModel;

namespace Logly.RawLogsHandler.Domain.Models.Parsing;

public sealed record LogEntry(
    DateTimeOffset CreatedAt,
    DateTimeOffset ReceivedAt,
    string Level,
    string Source,
    string Host,
    string Environment,
    string Message,
    ReadOnlyDictionary<string, object?> Payload);