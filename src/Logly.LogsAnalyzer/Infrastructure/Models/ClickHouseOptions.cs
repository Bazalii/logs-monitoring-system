namespace Logly.LogsAnalyzer.Infrastructure.Models;

public sealed class ClickHouseOptions
{
    public string ConnectionString { get; init; }
    public string Table { get; init; }
    public int BatchSize { get; init; }
}