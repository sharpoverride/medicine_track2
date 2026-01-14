#!/bin/bash

# Kusto Ingestion Validation Script
# Validates that telemetry data is flowing from services → OTEL Collector → Kusto

set -e

echo "============================================"
echo "Kusto Ingestion Validation"
echo "============================================"
echo ""

# Configuration
KUSTO_URL="${KUSTO_URL:-http://localhost:8080}"
DATABASE="medicinetrack"
GATEWAY_URL="${GATEWAY_URL:-http://localhost:5000}"
API_URL="${API_URL:-http://localhost:5001}"
CONFIG_URL="${CONFIG_URL:-http://localhost:5002}"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
PASSED=0
FAILED=0

# Helper function to run KQL query
run_kql() {
    local query="$1"
    local description="$2"

    echo -n "Testing: $description... "

    local payload=$(cat <<EOF
{
  "db": "$DATABASE",
  "csl": "$query"
}
EOF
)

    local response=$(curl -s -X POST "$KUSTO_URL/v1/rest/query" \
        -H "Content-Type: application/json" \
        -d "$payload")

    echo "$response"
}

# Helper function to check if number is greater than zero
check_count() {
    local count="$1"
    local name="$2"

    if [ -z "$count" ] || [ "$count" = "null" ]; then
        echo -e "${RED}✗ $name: No data found${NC}"
        ((FAILED++))
        return 1
    elif [ "$count" -gt 0 ]; then
        echo -e "${GREEN}✓ $name: $count rows${NC}"
        ((PASSED++))
        return 0
    else
        echo -e "${RED}✗ $name: 0 rows${NC}"
        ((FAILED++))
        return 1
    fi
}

echo "Step 1: Testing Kusto connection..."
echo "------------------------------------"

DATABASES=$(run_kql ".show databases" "Show databases")
if echo "$DATABASES" | grep -q "medicinetrack"; then
    echo -e "${GREEN}✓ Kusto emulator is accessible${NC}"
    echo -e "${GREEN}✓ Database 'medicinetrack' exists${NC}"
    ((PASSED+=2))
else
    echo -e "${RED}✗ Kusto emulator not accessible or database missing${NC}"
    echo "Response: $DATABASES"
    ((FAILED+=2))
    exit 1
fi

echo ""
echo "Step 2: Checking table schemas..."
echo "------------------------------------"

TABLES=$(run_kql ".show tables" "Show tables")
for table in traces requests dependencies exceptions; do
    if echo "$TABLES" | grep -q "$table"; then
        echo -e "${GREEN}✓ Table '$table' exists${NC}"
        ((PASSED++))
    else
        echo -e "${RED}✗ Table '$table' missing${NC}"
        ((FAILED++))
    fi
done

echo ""
echo "Step 3: Generating test traffic..."
echo "------------------------------------"

echo "Making requests to services to generate telemetry..."

# Health checks to generate telemetry
echo -n "- Gateway health check... "
if curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/health" | grep -q "200"; then
    echo -e "${GREEN}✓${NC}"
else
    echo -e "${YELLOW}⚠ (service may not be running)${NC}"
fi

echo -n "- API health check... "
if curl -s -o /dev/null -w "%{http_code}" "$API_URL/health" | grep -q "200"; then
    echo -e "${GREEN}✓${NC}"
else
    echo -e "${YELLOW}⚠ (service may not be running)${NC}"
fi

echo -n "- Config health check... "
if curl -s -o /dev/null -w "%{http_code}" "$CONFIG_URL/health" | grep -q "200"; then
    echo -e "${GREEN}✓${NC}"
else
    echo -e "${YELLOW}⚠ (service may not be running)${NC}"
fi

echo ""
echo "Waiting 10 seconds for telemetry to be ingested..."
sleep 10

echo ""
echo "Step 4: Validating data ingestion..."
echo "------------------------------------"

# Check traces table
TRACE_COUNT=$(run_kql "traces | count" "Count traces" | grep -oP '"Count":\s*\K\d+' | head -1)
check_count "$TRACE_COUNT" "Traces"

# Check requests table
REQUEST_COUNT=$(run_kql "requests | count" "Count requests" | grep -oP '"Count":\s*\K\d+' | head -1)
check_count "$REQUEST_COUNT" "Requests"

# Check dependencies table (may be empty if no external calls)
DEPENDENCY_COUNT=$(run_kql "dependencies | count" "Count dependencies" | grep -oP '"Count":\s*\K\d+' | head -1)
if [ -z "$DEPENDENCY_COUNT" ] || [ "$DEPENDENCY_COUNT" = "null" ]; then
    echo -e "${YELLOW}⚠ Dependencies: No data (expected if no external calls made)${NC}"
else
    echo -e "${GREEN}✓ Dependencies: $DEPENDENCY_COUNT rows${NC}"
    ((PASSED++))
fi

# Check exceptions table (may be empty)
EXCEPTION_COUNT=$(run_kql "exceptions | count" "Count exceptions" | grep -oP '"Count":\s*\K\d+' | head -1)
if [ -z "$EXCEPTION_COUNT" ] || [ "$EXCEPTION_COUNT" = "null" ]; then
    echo -e "${GREEN}✓ Exceptions: 0 rows (no errors)${NC}"
    ((PASSED++))
else
    echo -e "${YELLOW}⚠ Exceptions: $EXCEPTION_COUNT rows${NC}"
fi

echo ""
echo "Step 5: Validating schema compliance..."
echo "------------------------------------"

# Check if traces have required columns
echo -n "- Checking traces schema... "
TRACES_SCHEMA=$(run_kql "traces | take 1 | project timestamp, message, severityLevel, operation_Id, cloud_RoleName" "Traces schema")
if echo "$TRACES_SCHEMA" | grep -q "timestamp"; then
    echo -e "${GREEN}✓${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ (missing required columns)${NC}"
    ((FAILED++))
fi

# Check if requests have required columns
echo -n "- Checking requests schema... "
REQUESTS_SCHEMA=$(run_kql "requests | take 1 | project timestamp, name, duration, resultCode, cloud_RoleName" "Requests schema")
if echo "$REQUESTS_SCHEMA" | grep -q "timestamp"; then
    echo -e "${GREEN}✓${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ (missing required columns)${NC}"
    ((FAILED++))
fi

echo ""
echo "Step 6: Testing distributed tracing..."
echo "------------------------------------"

# Get a recent trace ID
TRACE_ID=$(run_kql "requests | where timestamp > ago(5m) | take 1 | project operation_Id" "Get trace ID" | grep -oP '"operation_Id":\s*"\K[^"]+' | head -1)

if [ -n "$TRACE_ID" ] && [ "$TRACE_ID" != "null" ]; then
    echo "Found trace ID: $TRACE_ID"

    # Check if we can correlate across tables
    TRACE_CORRELATION=$(run_kql "union requests, dependencies, traces | where operation_Id == '$TRACE_ID' | summarize count() by itemType" "Trace correlation")

    if echo "$TRACE_CORRELATION" | grep -q "itemType"; then
        echo -e "${GREEN}✓ Distributed tracing correlation works${NC}"
        ((PASSED++))
    else
        echo -e "${YELLOW}⚠ Could not verify correlation${NC}"
    fi
else
    echo -e "${YELLOW}⚠ No recent traces found for correlation test${NC}"
fi

echo ""
echo "============================================"
echo "Validation Results"
echo "============================================"
echo -e "Passed: ${GREEN}$PASSED${NC}"
echo -e "Failed: ${RED}$FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ All validation checks passed!${NC}"
    echo ""
    echo "Next steps:"
    echo "  - Query Kusto at: $KUSTO_URL"
    echo "  - Run test queries from: scripts/kusto-test-queries.kql"
    echo "  - View Aspire dashboard for service metrics"
    exit 0
else
    echo -e "${RED}✗ Some validation checks failed${NC}"
    echo ""
    echo "Troubleshooting:"
    echo "  - Ensure all services are running (dotnet run --project src/MedicineTrack.AppHost)"
    echo "  - Check OTEL Collector logs for export errors"
    echo "  - Verify Kusto emulator is healthy"
    echo "  - Check Kusto ingestion service logs"
    exit 1
fi
