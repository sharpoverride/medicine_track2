namespace MedicineTrack.RavenDB.Ingestion.Models.Otlp;

/// <summary>
/// OTLP trace request model
/// </summary>
public record OtlpTraceRequest
{
    public List<ResourceSpans> ResourceSpans { get; init; } = [];
}

public record ResourceSpans
{
    public Resource? Resource { get; init; }
    public List<ScopeSpans> ScopeSpans { get; init; } = [];
}

public record Resource
{
    public List<KeyValue> Attributes { get; init; } = [];
}

public record ScopeSpans
{
    public Scope? Scope { get; init; }
    public List<Span> Spans { get; init; } = [];
}

public record Scope
{
    public string? Name { get; init; }
    public string? Version { get; init; }
    public List<KeyValue> Attributes { get; init; } = [];
}

public record Span
{
    public string TraceId { get; init; } = string.Empty;
    public string SpanId { get; init; } = string.Empty;
    public string? ParentSpanId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string StartTimeUnixNano { get; init; } = string.Empty;
    public string EndTimeUnixNano { get; init; } = string.Empty;
    public int Kind { get; init; }
    public List<KeyValue> Attributes { get; init; } = [];
    public SpanStatus? Status { get; init; }
}

public record SpanStatus
{
    public string? Message { get; init; }
    public int Code { get; init; }
}

public record KeyValue
{
    public string Key { get; init; } = string.Empty;
    public AnyValue? Value { get; init; }
}

public record AnyValue
{
    public string? StringValue { get; init; }
    public long? IntValue { get; init; }
    public double? DoubleValue { get; init; }
    public bool? BoolValue { get; init; }
}
