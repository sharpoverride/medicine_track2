#!/usr/bin/env bash
# ClickHouse Telemetry Validation Script
# Validates that telemetry data is flowing from services -> OTEL Collector -> ClickHouse

set -euo pipefail

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

CLICKHOUSE_URL="${CLICKHOUSE_URL:-http://localhost:8123}"
CLICKHOUSE_DATABASE="${CLICKHOUSE_DATABASE:-telemetry}"

echo "ClickHouse Telemetry Validation"
echo "==============================="
echo ""
echo "Configuration:"
echo "  ClickHouse URL: $CLICKHOUSE_URL"
echo "  Database:       $CLICKHOUSE_DATABASE"
echo ""

query() {
    local encoded_query
    encoded_query=$(printf '%s' "$1" | python3 -c 'import sys, urllib.parse; print(urllib.parse.quote(sys.stdin.read(), safe=""))')
    curl -sS "$CLICKHOUSE_URL/?database=$CLICKHOUSE_DATABASE&query=$encoded_query"
}

# Step 1: Test ClickHouse connection
echo "Step 1: Testing ClickHouse connection..."
if curl -sS "$CLICKHOUSE_URL/ping" > /dev/null; then
    echo -e "${GREEN}ClickHouse is accessible${NC}"
else
    echo -e "${RED}ClickHouse not accessible at $CLICKHOUSE_URL${NC}"
    echo ""
    echo "Troubleshooting:"
    echo "  - Ensure the Aspire AppHost is running"
    echo "  - Verify the clickhouse container is healthy"
    exit 1
fi

# Step 2: Verify tables exist
echo ""
echo "Step 2: Verifying telemetry tables..."
tables=$(query "SHOW TABLES" || true)
for table in otel_traces otel_logs otel_metrics_sum otel_metrics_histogram otel_metrics_gauge; do
    if echo "$tables" | grep -q "$table"; then
        echo -e "${GREEN}  Table $table exists${NC}"
    else
        echo -e "${RED}  Table $table is missing${NC}"
    fi
done

# Step 3: Check for recent trace data
echo ""
echo "Step 3: Checking for recent trace data..."
trace_count=$(query "SELECT count() FROM otel_traces WHERE Timestamp > now() - INTERVAL 5 MINUTE" | tr -d '\n' || echo "0")
if [ "$trace_count" -gt 0 ] 2>/dev/null; then
    echo -e "${GREEN}  Found $trace_count trace(s) in the last 5 minutes${NC}"
else
    echo -e "${YELLOW}  No traces found in the last 5 minutes${NC}"
    echo "    Services may not have emitted traces yet."
fi

# Step 4: Check for recent log data
echo ""
echo "Step 4: Checking for recent log data..."
log_count=$(query "SELECT count() FROM otel_logs WHERE Timestamp > now() - INTERVAL 5 MINUTE" | tr -d '\n' || echo "0")
if [ "$log_count" -gt 0 ] 2>/dev/null; then
    echo -e "${GREEN}  Found $log_count log(s) in the last 5 minutes${NC}"
else
    echo -e "${YELLOW}  No logs found in the last 5 minutes${NC}"
    echo "    Services may not have emitted traces yet."
fi

# Step 5: Check for recent metric data
echo ""
echo "Step 5: Checking for recent metric data..."
metric_count=$(query "SELECT count() FROM otel_metrics_sum WHERE TimeUnix > now() - INTERVAL 5 MINUTE" | tr -d '\n' || echo "0")
if [ "$metric_count" -gt 0 ] 2>/dev/null; then
    echo -e "${GREEN}  Found $metric_count metric sum point(s) in the last 5 minutes${NC}"
else
    echo -e "${YELLOW}  No metric sum points found in the last 5 minutes${NC}"
    echo "    Services may not have emitted metrics yet."
fi

# Step 6: Sample data
echo ""
echo "Step 6: Sample trace data (last 5 minutes)..."
query "SELECT
    Timestamp,
    TraceId,
    SpanId,
    ServiceName,
    SpanName,
    StatusCode,
    Duration
FROM otel_traces
WHERE Timestamp > now() - INTERVAL 5 MINUTE
ORDER BY Timestamp DESC
LIMIT 5
FORMAT PrettyCompact"

echo ""
echo "Validation complete."
echo ""
echo "Useful queries are available in: scripts/clickhouse-test-queries.sql"
