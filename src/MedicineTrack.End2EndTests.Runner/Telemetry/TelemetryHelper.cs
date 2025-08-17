using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MedicineTrack.End2EndTests.Runner.Telemetry;

/// <summary>
/// Helper class for managing telemetry and tracing in E2E tests
/// </summary>
public static class TelemetryHelper
{
    private static readonly ActivitySource ActivitySource = new("MedicineTrack.End2EndTests.Runner", "1.0.0");

    /// <summary>
    /// Starts a new root trace for an E2E test operation
    /// </summary>
    public static Activity? StartE2ETrace(
        string operationName,
        string testSuite,
        string testName,
        Dictionary<string, object?>? additionalTags = null,
        [CallerMemberName] string callerName = "")
    {
        var tags = new Dictionary<string, object?>
        {
            ["test.suite"] = testSuite,
            ["test.name"] = testName,
            ["test.caller"] = callerName,
            ["test.timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["service.name"] = "MedicineTrack.End2EndTests.Runner",
            ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        };

        if (additionalTags != null)
        {
            foreach (var tag in additionalTags)
            {
                tags[tag.Key] = tag.Value;
            }
        }

        // Start a new root activity (new trace)
        var activity = ActivitySource.StartActivity(
            operationName,
            ActivityKind.Client,
            parentContext: default, // No parent = new trace
            tags: tags);

        // Add correlation ID as baggage for downstream propagation
        activity?.AddBaggage("correlation.id", Guid.NewGuid().ToString("N"));
        activity?.AddBaggage("test.runner", "e2e");
        
        return activity;
    }

    /// <summary>
    /// Records a test event in the current activity
    /// </summary>
    public static void RecordTestEvent(Activity? activity, string eventName, Dictionary<string, object?>? attributes = null)
    {
        if (activity == null) return;

        var eventAttributes = new ActivityTagsCollection();
        if (attributes != null)
        {
            foreach (var attr in attributes)
            {
                eventAttributes[attr.Key] = attr.Value;
            }
        }

        activity.AddEvent(new ActivityEvent(eventName, DateTimeOffset.UtcNow, eventAttributes));
    }

    /// <summary>
    /// Sets the activity status based on success/failure
    /// </summary>
    public static void SetActivityStatus(Activity? activity, bool success, string? description = null)
    {
        if (activity == null) return;

        if (success)
        {
            activity.SetStatus(ActivityStatusCode.Ok, description ?? "Operation completed successfully");
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Error, description ?? "Operation failed");
        }
    }

    /// <summary>
    /// Enriches activity with HTTP response information
    /// </summary>
    public static void EnrichWithHttpResponse(Activity? activity, HttpResponseMessage response, TimeSpan duration)
    {
        if (activity == null) return;

        activity.SetTag("http.status_code", (int)response.StatusCode);
        activity.SetTag("http.status_text", response.StatusCode.ToString());
        activity.SetTag("http.response.content_length", response.Content.Headers.ContentLength);
        activity.SetTag("http.duration_ms", duration.TotalMilliseconds);
        
        if (!response.IsSuccessStatusCode)
        {
            activity.SetTag("http.error", true);
        }

        // Add response headers as events for debugging
        if (response.Headers.Any())
        {
            var headers = new ActivityTagsCollection();
            foreach (var header in response.Headers.Take(10)) // Limit to first 10 headers
            {
                headers[$"header.{header.Key.ToLower()}"] = string.Join(", ", header.Value);
            }
            activity.AddEvent(new ActivityEvent("http.response.headers", DateTimeOffset.UtcNow, headers));
        }
    }

    /// <summary>
    /// Creates a child activity for a sub-operation within an E2E test
    /// </summary>
    public static Activity? StartChildActivity(string operationName, Activity? parent, Dictionary<string, object?>? tags = null)
    {
        var activity = ActivitySource.StartActivity(
            operationName,
            ActivityKind.Internal,
            parent?.Context ?? default,
            tags: tags);

        return activity;
    }

    /// <summary>
    /// Logs activity details with trace information
    /// </summary>
    public static void LogWithTrace(ILogger logger, LogLevel level, Activity? activity, string message, params object[] args)
    {
        if (activity != null)
        {
            var traceId = activity.TraceId.ToString();
            var spanId = activity.SpanId.ToString();
            
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["TraceId"] = traceId,
                ["SpanId"] = spanId,
                ["ParentSpanId"] = activity.ParentSpanId.ToString()
            }))
            {
                logger.Log(level, $"[{traceId}] {message}", args);
            }
        }
        else
        {
            logger.Log(level, message, args);
        }
    }
}
