-- ClickHouse SQL Test Queries for Telemetry Analysis
-- Run these via the ClickHouse HTTP interface or any ClickHouse client.
-- The Aspire AppHost exposes ClickHouse HTTP on localhost:8123 (DCP proxy).

-- Total traces per service in the last hour
SELECT
    ServiceName,
    count() AS TraceCount,
    avg(Duration) AS AvgDurationMs,
    max(Duration) AS MaxDurationMs
FROM telemetry.otel_traces
WHERE Timestamp > now() - INTERVAL 1 HOUR
GROUP BY ServiceName
ORDER BY TraceCount DESC;

-- Error rate per service in the last hour
SELECT
    ServiceName,
    count() AS Total,
    countIf(StatusCode = 'Error') AS Errors,
    round(Errors / Total * 100, 2) AS ErrorRate
FROM telemetry.otel_traces
WHERE Timestamp > now() - INTERVAL 1 HOUR
GROUP BY ServiceName
ORDER BY ErrorRate DESC;

-- Slowest spans in the last hour
SELECT
    Timestamp,
    TraceId,
    SpanId,
    ServiceName,
    SpanName,
    Duration
FROM telemetry.otel_traces
WHERE Timestamp > now() - INTERVAL 1 HOUR
ORDER BY Duration DESC
LIMIT 20;

-- Logs by severity in the last hour
SELECT
    ServiceName,
    SeverityText,
    count() AS LogCount
FROM telemetry.otel_logs
WHERE Timestamp > now() - INTERVAL 1 HOUR
GROUP BY ServiceName, SeverityText
ORDER BY ServiceName, LogCount DESC;

-- Recent error logs
SELECT
    Timestamp,
    ServiceName,
    SeverityText,
    Body
FROM telemetry.otel_logs
WHERE SeverityText IN ('Error', 'Fatal')
  AND Timestamp > now() - INTERVAL 1 HOUR
ORDER BY Timestamp DESC
LIMIT 50;

-- Metric sum points per service in the last hour
SELECT
    ServiceName,
    MetricName,
    count() AS PointCount
FROM telemetry.otel_metrics_sum
WHERE Timestamp > now() - INTERVAL 1 HOUR
GROUP BY ServiceName, MetricName
ORDER BY ServiceName, PointCount DESC;

-- Trace details for a specific TraceId
-- Replace '<trace-id>' with an actual value
SELECT
    Timestamp,
    SpanId,
    ParentSpanId,
    ServiceName,
    SpanName,
    StatusCode,
    Duration,
    Attributes,
    ResourceAttributes
FROM telemetry.otel_traces
WHERE TraceId = '<trace-id>'
ORDER BY Timestamp;
