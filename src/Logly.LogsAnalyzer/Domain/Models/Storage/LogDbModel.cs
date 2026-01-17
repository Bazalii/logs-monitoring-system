using System.Collections.ObjectModel;
using Logly.LogsAnalyzer.Domain.Models.Core.Logs;

namespace Logly.LogsAnalyzer.Domain.Models.Storage;

public sealed record LogDbModel(
    DateTimeOffset CreatedAt,
    DateTimeOffset ReceivedAt,
    string Level,
    string Source,
    string Host,
    string Environment,
    string Message,
    ReadOnlyDictionary<string, object?>? Payload) : ILog;