using MedicineTrack.RavenDB.Ingestion.Models;
using MedicineTrack.RavenDB.Ingestion.Models.Otlp;

namespace MedicineTrack.RavenDB.Ingestion.Services;

/// <summary>
/// Maps OTLP format to RavenDB document models
/// </summary>
public static class OtlpMapper
{
    /// <summary>
    /// Convert OTLP trace request to TraceDocuments and RequestDocuments
    /// </summary>
    public static (List<TraceDocument> Traces, List<RequestDocument> Requests) MapTraces(OtlpTraceRequest otlpRequest)
    {
        var traces = new List<TraceDocument>();
        var requests = new List<RequestDocument>();

        foreach (var resourceSpan in otlpRequest.ResourceSpans)
        {
            var serviceName = GetAttributeValue(resourceSpan.Resource?.Attributes, "service.name") ?? "unknown";
            var serviceInstance = GetAttributeValue(resourceSpan.Resource?.Attributes, "service.instance.id");

            foreach (var scopeSpan in resourceSpan.ScopeSpans)
            {
                foreach (var span in scopeSpan.Spans)
                {
                    var startTime = UnixNanoToDateTime(span.StartTimeUnixNano);
                    var endTime = UnixNanoToDateTime(span.EndTimeUnixNano);
                    var durationMs = (endTime - startTime).TotalMilliseconds;

                    var customDimensions = span.Attributes
                        .Where(a => a.Value != null)
                        .ToDictionary(
                            a => a.Key,
                            a => a.Value!.StringValue ?? a.Value.IntValue?.ToString() ??
                                 a.Value.DoubleValue?.ToString() ?? a.Value.BoolValue?.ToString() ?? ""
                        );

                    // Span kind 2 = SERVER (requests), others are traces/dependencies
                    if (span.Kind == 2)
                    {
                        // This is an HTTP request span
                        var request = new RequestDocument
                        {
                            Timestamp = startTime,
                            Name = span.Name,
                            Url = GetAttributeValue(span.Attributes, "http.url"),
                            Success = span.Status?.Code != 2, // 2 = ERROR
                            ResultCode = GetAttributeValue(span.Attributes, "http.status_code") ?? "200",
                            DurationMs = durationMs,
                            OperationName = span.Name,
                            OperationId = span.TraceId,
                            SpanId = span.SpanId,
                            CloudRoleName = serviceName,
                            CloudRoleInstance = serviceInstance,
                            CustomDimensions = customDimensions,
                            CustomMeasurements = new Dictionary<string, double>
                            {
                                { "duration_ms", durationMs }
                            },
                            ItemType = "request"
                        };
                        requests.Add(request);
                    }
                    else
                    {
                        // This is an internal span/trace
                        var trace = new TraceDocument
                        {
                            Timestamp = startTime,
                            Message = $"{span.Name} (duration: {durationMs:F2}ms)",
                            SeverityLevel = span.Status?.Code == 2 ? 3 : 1, // ERROR = 3, INFO = 1
                            OperationName = span.Name,
                            OperationId = span.TraceId,
                            SpanId = span.SpanId,
                            ParentSpanId = span.ParentSpanId,
                            CloudRoleName = serviceName,
                            CloudRoleInstance = serviceInstance,
                            CustomDimensions = customDimensions,
                            ItemType = "trace"
                        };
                        traces.Add(trace);
                    }
                }
            }
        }

        return (traces, requests);
    }

    /// <summary>
    /// Convert OTLP logs request to TraceDocuments
    /// </summary>
    public static List<TraceDocument> MapLogs(OtlpLogsRequest otlpRequest)
    {
        var traces = new List<TraceDocument>();

        foreach (var resourceLog in otlpRequest.ResourceLogs)
        {
            var serviceName = GetAttributeValue(resourceLog.Resource?.Attributes, "service.name") ?? "unknown";
            var serviceInstance = GetAttributeValue(resourceLog.Resource?.Attributes, "service.instance.id");

            foreach (var scopeLog in resourceLog.ScopeLogs)
            {
                foreach (var logRecord in scopeLog.LogRecords)
                {
                    var customDimensions = logRecord.Attributes
                        .Where(a => a.Value != null)
                        .ToDictionary(
                            a => a.Key,
                            a => a.Value!.StringValue ?? a.Value.IntValue?.ToString() ??
                                 a.Value.DoubleValue?.ToString() ?? a.Value.BoolValue?.ToString() ?? ""
                        );

                    var trace = new TraceDocument
                    {
                        Timestamp = UnixNanoToDateTime(logRecord.TimeUnixNano),
                        Message = logRecord.Body?.StringValue ?? "",
                        SeverityLevel = MapSeverityLevel(logRecord.SeverityNumber),
                        OperationName = scopeLog.Scope?.Name,
                        OperationId = logRecord.TraceId ?? "",
                        SpanId = logRecord.SpanId,
                        CloudRoleName = serviceName,
                        CloudRoleInstance = serviceInstance,
                        CustomDimensions = customDimensions,
                        ItemType = "trace"
                    };
                    traces.Add(trace);
                }
            }
        }

        return traces;
    }

    private static DateTime UnixNanoToDateTime(string unixNano)
    {
        if (string.IsNullOrEmpty(unixNano) || !long.TryParse(unixNano, out var nanos))
            return DateTime.UtcNow;

        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return epoch.AddTicks(nanos / 100); // Convert nanoseconds to ticks (100ns)
    }

    private static int MapSeverityLevel(int severityNumber)
    {
        // OTLP severity: 1-4=TRACE, 5-8=DEBUG, 9-12=INFO, 13-16=WARN, 17-20=ERROR, 21-24=FATAL
        // Our levels: 0=Verbose, 1=Info, 2=Warning, 3=Error, 4=Critical
        return severityNumber switch
        {
            <= 8 => 0,   // TRACE/DEBUG -> Verbose
            <= 12 => 1,  // INFO -> Info
            <= 16 => 2,  // WARN -> Warning
            <= 20 => 3,  // ERROR -> Error
            _ => 4       // FATAL -> Critical
        };
    }

    private static string? GetAttributeValue(List<KeyValue>? attributes, string key)
    {
        var attr = attributes?.FirstOrDefault(a => a.Key == key);
        return attr?.Value?.StringValue ?? attr?.Value?.IntValue?.ToString();
    }
}
