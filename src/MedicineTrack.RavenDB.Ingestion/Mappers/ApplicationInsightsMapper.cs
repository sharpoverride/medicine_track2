using MedicineTrack.RavenDB.Ingestion.Models;
using System.Text.Json;

namespace MedicineTrack.RavenDB.Ingestion.Mappers;

/// <summary>
/// Maps OpenTelemetry data (from JSON export) to RavenDB document models
/// </summary>
public static class ApplicationInsightsMapper
{
    /// <summary>
    /// Map JSON log record to TraceDocument (RavenDB schema)
    /// </summary>
    public static TraceDocument? MapLogRecordFromJson(JsonElement logElement)
    {
        try
        {
            var timestamp = logElement.TryGetProperty("timeUnixNano", out var tsElement)
                ? DateTimeOffset.FromUnixTimeMilliseconds(tsElement.GetInt64() / 1_000_000).UtcDateTime
                : DateTime.UtcNow;

            var body = logElement.TryGetProperty("body", out var bodyElement)
                ? bodyElement.GetProperty("stringValue").GetString() ?? ""
                : "";

            var severityNumber = logElement.TryGetProperty("severityNumber", out var sevElement)
                ? sevElement.GetInt32()
                : 9; // INFO

            // Map OTLP severity (0-24) to Application Insights (0-4)
            var severityLevel = MapOtlpSeverityToAppInsights(severityNumber);

            var attributes = ExtractAttributes(logElement);
            var resource = ExtractResourceAttributes(logElement);

            var spanId = logElement.TryGetProperty("spanId", out var spanIdEl)
                ? spanIdEl.GetString()
                : null;

            // Note: Log records typically don't have parentSpanId in OTLP format

            return new TraceDocument
            {
                Timestamp = timestamp,
                Message = body,
                SeverityLevel = severityLevel,
                OperationName = attributes.GetValueOrDefault("operation.name")?.ToString(),
                OperationId = logElement.TryGetProperty("traceId", out var traceIdEl)
                    ? traceIdEl.GetString() ?? "unknown"
                    : "unknown",
                SpanId = spanId,
                ParentSpanId = null,
                CloudRoleName = resource.GetValueOrDefault("service.name")?.ToString() ?? "unknown",
                CloudRoleInstance = resource.GetValueOrDefault("service.instance.id")?.ToString(),
                CustomDimensions = ConvertToStringDictionary(attributes),
                ItemType = "trace"
            };
        }
        catch
        {
            return null; // Skip invalid records
        }
    }

    /// <summary>
    /// Map JSON span to RequestDocument (HTTP requests)
    /// </summary>
    public static RequestDocument? MapSpanToRequest(JsonElement spanElement)
    {
        try
        {
            // Check if this is an HTTP server span (kind = 2 = SERVER)
            if (!spanElement.TryGetProperty("kind", out var kindElement) || kindElement.GetInt32() != 2)
                return null;

            var startTime = spanElement.TryGetProperty("startTimeUnixNano", out var startElement)
                ? DateTimeOffset.FromUnixTimeMilliseconds(startElement.GetInt64() / 1_000_000).UtcDateTime
                : DateTime.UtcNow;

            var endTime = spanElement.TryGetProperty("endTimeUnixNano", out var endElement)
                ? DateTimeOffset.FromUnixTimeMilliseconds(endElement.GetInt64() / 1_000_000).UtcDateTime
                : startTime;

            var duration = (endTime - startTime).TotalMilliseconds;

            var attributes = ExtractAttributes(spanElement);
            var resource = ExtractResourceAttributes(spanElement);

            var name = spanElement.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString() ?? "unknown"
                : "unknown";

            var url = attributes.GetValueOrDefault("http.url")?.ToString()
                ?? attributes.GetValueOrDefault("url.full")?.ToString();

            var statusCode = attributes.GetValueOrDefault("http.status_code")?.ToString()
                ?? attributes.GetValueOrDefault("http.response.status_code")?.ToString();

            var success = DetermineSuccessBool(statusCode);

            var spanId = spanElement.TryGetProperty("spanId", out var spanIdEl)
                ? spanIdEl.GetString()
                : null;

            return new RequestDocument
            {
                Timestamp = startTime,
                Name = name,
                Url = url,
                Success = success,
                ResultCode = statusCode,
                DurationMs = duration,
                OperationName = name,
                OperationId = spanElement.TryGetProperty("traceId", out var traceIdEl)
                    ? traceIdEl.GetString() ?? "unknown"
                    : "unknown",
                SpanId = spanId,
                CloudRoleName = resource.GetValueOrDefault("service.name")?.ToString() ?? "unknown",
                CloudRoleInstance = resource.GetValueOrDefault("service.instance.id")?.ToString(),
                CustomDimensions = ConvertToStringDictionary(attributes),
                CustomMeasurements = null,
                ItemType = "request"
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Map JSON span to DependencyDocument (external calls)
    /// </summary>
    public static DependencyDocument? MapSpanToDependency(JsonElement spanElement)
    {
        try
        {
            // Check if this is a CLIENT or INTERNAL span (kind = 3 = CLIENT, kind = 1 = INTERNAL)
            if (!spanElement.TryGetProperty("kind", out var kindElement))
                return null;

            var kind = kindElement.GetInt32();
            if (kind != 3 && kind != 1) // Not CLIENT or INTERNAL
                return null;

            var startTime = spanElement.TryGetProperty("startTimeUnixNano", out var startElement)
                ? DateTimeOffset.FromUnixTimeMilliseconds(startElement.GetInt64() / 1_000_000).UtcDateTime
                : DateTime.UtcNow;

            var endTime = spanElement.TryGetProperty("endTimeUnixNano", out var endElement)
                ? DateTimeOffset.FromUnixTimeMilliseconds(endElement.GetInt64() / 1_000_000).UtcDateTime
                : startTime;

            var duration = (endTime - startTime).TotalMilliseconds;

            var attributes = ExtractAttributes(spanElement);
            var resource = ExtractResourceAttributes(spanElement);

            var name = spanElement.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString() ?? "unknown"
                : "unknown";

            // Determine dependency type (HTTP, SQL, etc.)
            var type = DetermineDependencyType(attributes);
            var target = ExtractTarget(attributes, type);
            var data = ExtractDependencyData(attributes, type);

            var statusCode = attributes.GetValueOrDefault("http.status_code")?.ToString()
                ?? attributes.GetValueOrDefault("db.response_status_code")?.ToString();

            var success = DetermineSuccessBool(statusCode);

            var spanId = spanElement.TryGetProperty("spanId", out var spanIdEl)
                ? spanIdEl.GetString()
                : null;

            return new DependencyDocument
            {
                Timestamp = startTime,
                Name = name,
                Type = type,
                Target = target,
                Data = data,
                Success = success,
                ResultCode = statusCode,
                DurationMs = duration,
                OperationName = name,
                OperationId = spanElement.TryGetProperty("traceId", out var traceIdEl)
                    ? traceIdEl.GetString() ?? "unknown"
                    : "unknown",
                SpanId = spanId,
                CloudRoleName = resource.GetValueOrDefault("service.name")?.ToString() ?? "unknown",
                CloudRoleInstance = resource.GetValueOrDefault("service.instance.id")?.ToString(),
                CustomDimensions = ConvertToStringDictionary(attributes),
                ItemType = "dependency"
            };
        }
        catch
        {
            return null;
        }
    }

    // Helper methods

    private static int MapOtlpSeverityToAppInsights(int otlpSeverity)
    {
        // OTLP: 0-24, Application Insights: 0-4
        // 0-4: Trace/Debug -> 0-1
        // 5-12: Info -> 1
        // 13-16: Warn -> 2
        // 17-20: Error -> 3
        // 21-24: Fatal -> 4
        return otlpSeverity switch
        {
            <= 4 => 0,      // Trace/Debug
            <= 12 => 1,     // Info
            <= 16 => 2,     // Warn
            <= 20 => 3,     // Error
            _ => 4          // Fatal
        };
    }

    private static Dictionary<string, object?> ExtractAttributes(JsonElement element)
    {
        var dict = new Dictionary<string, object?>();

        if (element.TryGetProperty("attributes", out var attrsElement) && attrsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var attr in attrsElement.EnumerateArray())
            {
                if (attr.TryGetProperty("key", out var keyEl) && attr.TryGetProperty("value", out var valueEl))
                {
                    var key = keyEl.GetString();
                    if (!string.IsNullOrEmpty(key))
                    {
                        dict[key] = ExtractValue(valueEl);
                    }
                }
            }
        }

        return dict;
    }

    private static Dictionary<string, object?> ExtractResourceAttributes(JsonElement element)
    {
        var dict = new Dictionary<string, object?>();

        if (element.TryGetProperty("resource", out var resourceElement) &&
            resourceElement.TryGetProperty("attributes", out var attrsElement) &&
            attrsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var attr in attrsElement.EnumerateArray())
            {
                if (attr.TryGetProperty("key", out var keyEl) && attr.TryGetProperty("value", out var valueEl))
                {
                    var key = keyEl.GetString();
                    if (!string.IsNullOrEmpty(key))
                    {
                        dict[key] = ExtractValue(valueEl);
                    }
                }
            }
        }

        return dict;
    }

    private static object? ExtractValue(JsonElement valueElement)
    {
        if (valueElement.TryGetProperty("stringValue", out var strVal))
            return strVal.GetString();
        if (valueElement.TryGetProperty("intValue", out var intVal))
            return intVal.GetInt64();
        if (valueElement.TryGetProperty("doubleValue", out var dblVal))
            return dblVal.GetDouble();
        if (valueElement.TryGetProperty("boolValue", out var boolVal))
            return boolVal.GetBoolean();

        return null;
    }

    private static string DetermineDependencyType(Dictionary<string, object?> attributes)
    {
        if (attributes.ContainsKey("db.system"))
            return "SQL";
        if (attributes.ContainsKey("http.method") || attributes.ContainsKey("http.request.method"))
            return "HTTP";
        if (attributes.ContainsKey("rpc.system"))
            return "RPC";
        if (attributes.ContainsKey("messaging.system"))
            return "Queue";

        return "Other";
    }

    private static string? ExtractTarget(Dictionary<string, object?> attributes, string type)
    {
        return type switch
        {
            "SQL" => attributes.GetValueOrDefault("db.name")?.ToString()
                ?? attributes.GetValueOrDefault("db.connection_string")?.ToString(),
            "HTTP" => attributes.GetValueOrDefault("net.peer.name")?.ToString()
                ?? attributes.GetValueOrDefault("server.address")?.ToString(),
            _ => null
        };
    }

    private static string? ExtractDependencyData(Dictionary<string, object?> attributes, string type)
    {
        return type switch
        {
            "SQL" => attributes.GetValueOrDefault("db.statement")?.ToString(),
            "HTTP" => attributes.GetValueOrDefault("http.url")?.ToString()
                ?? attributes.GetValueOrDefault("url.full")?.ToString(),
            _ => null
        };
    }

    private static bool DetermineSuccessBool(string? statusCode)
    {
        if (string.IsNullOrEmpty(statusCode))
            return true;

        if (int.TryParse(statusCode, out var code))
        {
            return code >= 200 && code < 400;
        }

        return true; // Default to success
    }

    /// <summary>
    /// Convert Dictionary<string, object?> to Dictionary<string, string> for RavenDB
    /// </summary>
    private static Dictionary<string, string>? ConvertToStringDictionary(Dictionary<string, object?> attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return null;

        var result = new Dictionary<string, string>();
        foreach (var kvp in attributes)
        {
            if (kvp.Value != null)
            {
                result[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
            }
        }

        return result.Count > 0 ? result : null;
    }
}
