-- ClickHouse telemetry database for OpenTelemetry data
-- Mounted into the container at /docker-entrypoint-initdb.d and executed on first startup.
-- The OpenTelemetry Collector clickhouseexporter creates the otel_traces, otel_logs,
-- and otel_metrics tables (and their dependent tables) automatically, so we only
-- ensure the target database exists here.

CREATE DATABASE IF NOT EXISTS telemetry;
