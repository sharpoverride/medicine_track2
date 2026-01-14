namespace MedicineTrack.RavenDB.Ingestion.Models.Otlp;

/// <summary>
/// OTLP logs request model
/// </summary>
public record OtlpLogsRequest
{
    public List<ResourceLogs> ResourceLogs { get; init; } = [];
}

public record ResourceLogs
{
    public Resource? Resource { get; init; }
    public List<ScopeLogs> ScopeLogs { get; init; } = [];
}

public record ScopeLogs
{
    public Scope? Scope { get; init; }
    public List<LogRecord> LogRecords { get; init; } = [];
}

public record LogRecord
{
    public string TimeUnixNano { get; init; } = string.Empty;
    public int SeverityNumber { get; init; }
    public string? SeverityText { get; init; }
    public AnyValue? Body { get; init; }
    public List<KeyValue> Attributes { get; init; } = [];
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
