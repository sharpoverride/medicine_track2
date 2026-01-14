#!/bin/bash

# RavenDB Ingestion Validation Script
# Validates that telemetry data is flowing from services → OTEL Collector → RavenDB

set -e

echo "============================================"
echo "RavenDB Ingestion Validation"
echo "============================================"
echo ""

# Configuration
RAVENDB_URL="${RAVENDB_URL:-https://ravendb.ravendb.orb.local}"
DATABASE="telemetry"
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

# Helper function to run RQL query
run_rql() {
    local query="$1"
    local description="$2"

    echo -n "Testing: $description... "

    local response=$(curl -sk -X POST "$RAVENDB_URL/databases/$DATABASE/queries" \
        -H "Content-Type: application/json" \
        -d "{\"Query\": \"$query\"}")

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
        echo -e "${GREEN}✓ $name: $count documents${NC}"
        ((PASSED++))
        return 0
    else
        echo -e "${RED}✗ $name: 0 documents${NC}"
        ((FAILED++))
        return 1
    fi
}

echo "Step 1: Testing RavenDB connection..."
echo "------------------------------------"

# Test basic connection with statistics endpoint
STATS=$(curl -sk -X GET "$RAVENDB_URL/databases/$DATABASE/stats")
if echo "$STATS" | grep -q "CountOfDocuments"; then
    echo -e "${GREEN}✓ RavenDB is accessible${NC}"
    echo -e "${GREEN}✓ Database 'telemetry' exists${NC}"
    ((PASSED+=2))
else
    echo -e "${RED}✗ RavenDB not accessible or database missing${NC}"
    echo "Response: $STATS"
    echo ""
    echo "Troubleshooting:"
    echo "  - Ensure RavenDB is running at: $RAVENDB_URL"
    echo "  - Verify 'telemetry' database exists in RavenDB Studio"
    echo "  - Check network connectivity and certificates"
    ((FAILED+=2))
    exit 1
fi

echo ""
echo "Step 2: Checking collection schemas..."
echo "------------------------------------"

# Get collection stats
COLLECTIONS=$(curl -sk -X GET "$RAVENDB_URL/databases/$DATABASE/collections/stats")

for collection in Traces Requests Dependencies Exceptions; do
    if echo "$COLLECTIONS" | grep -q "\"Name\":\"$collection\""; then
        echo -e "${GREEN}✓ Collection '$collection' exists${NC}"
        ((PASSED++))
    else
        echo -e "${YELLOW}⚠ Collection '$collection' not yet created (will be created on first insert)${NC}"
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

# Make some API requests to generate more interesting telemetry
echo ""
echo "Making API requests to generate telemetry data..."
echo -n "- GET /medications... "
if curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/medications" | grep -q "200"; then
    echo -e "${GREEN}✓${NC}"
else
    echo -e "${YELLOW}⚠ (may fail if no auth)${NC}"
fi

echo -n "- GET /medication-logs... "
if curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/medication-logs" | grep -q "200"; then
    echo -e "${GREEN}✓${NC}"
else
    echo -e "${YELLOW}⚠ (may fail if no auth)${NC}"
fi

echo ""
echo "Waiting 15 seconds for telemetry to be ingested..."
sleep 15

echo ""
echo "Step 4: Validating data ingestion..."
echo "------------------------------------"

# Check Traces collection
TRACE_RESULT=$(run_rql "from Traces select count()" "Count traces")
TRACE_COUNT=$(echo "$TRACE_RESULT" | grep -oP '"TotalResults":\s*\K\d+' | head -1)
check_count "$TRACE_COUNT" "Traces"

# Check Requests collection
REQUEST_RESULT=$(run_rql "from Requests select count()" "Count requests")
REQUEST_COUNT=$(echo "$REQUEST_RESULT" | grep -oP '"TotalResults":\s*\K\d+' | head -1)
check_count "$REQUEST_COUNT" "Requests"

# Check Dependencies collection (may be empty if no external calls)
DEPENDENCY_RESULT=$(run_rql "from Dependencies select count()" "Count dependencies")
DEPENDENCY_COUNT=$(echo "$DEPENDENCY_RESULT" | grep -oP '"TotalResults":\s*\K\d+' | head -1)
if [ -z "$DEPENDENCY_COUNT" ] || [ "$DEPENDENCY_COUNT" = "null" ] || [ "$DEPENDENCY_COUNT" = "0" ]; then
    echo -e "${YELLOW}⚠ Dependencies: No data (expected if no database calls made)${NC}"
else
    echo -e "${GREEN}✓ Dependencies: $DEPENDENCY_COUNT documents${NC}"
    ((PASSED++))
fi

# Check Exceptions collection (may be empty)
EXCEPTION_RESULT=$(run_rql "from Exceptions select count()" "Count exceptions")
EXCEPTION_COUNT=$(echo "$EXCEPTION_RESULT" | grep -oP '"TotalResults":\s*\K\d+' | head -1)
if [ -z "$EXCEPTION_COUNT" ] || [ "$EXCEPTION_COUNT" = "null" ] || [ "$EXCEPTION_COUNT" = "0" ]; then
    echo -e "${GREEN}✓ Exceptions: 0 documents (no errors)${NC}"
    ((PASSED++))
else
    echo -e "${YELLOW}⚠ Exceptions: $EXCEPTION_COUNT documents (errors detected)${NC}"
fi

echo ""
echo "Step 5: Validating schema compliance..."
echo "------------------------------------"

# Check if traces have required fields
echo -n "- Checking Traces schema... "
TRACES_SCHEMA=$(run_rql "from Traces select Timestamp, Message, SeverityLevel, OperationId, CloudRoleName limit 1" "Traces schema")
if echo "$TRACES_SCHEMA" | grep -q "Timestamp"; then
    echo -e "${GREEN}✓${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ (missing required fields)${NC}"
    ((FAILED++))
fi

# Check if requests have required fields
echo -n "- Checking Requests schema... "
REQUESTS_SCHEMA=$(run_rql "from Requests select Timestamp, Name, DurationMs, ResultCode, CloudRoleName limit 1" "Requests schema")
if echo "$REQUESTS_SCHEMA" | grep -q "Timestamp"; then
    echo -e "${GREEN}✓${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ (missing required fields)${NC}"
    ((FAILED++))
fi

echo ""
echo "Step 6: Testing distributed tracing..."
echo "------------------------------------"

# Get a recent operation ID (trace ID)
TRACE_ID_RESULT=$(run_rql "from Requests where Timestamp > @now.AddMinutes(-5) select OperationId limit 1" "Get trace ID")
TRACE_ID=$(echo "$TRACE_ID_RESULT" | grep -oP '"OperationId":\s*"\K[^"]+' | head -1)

if [ -n "$TRACE_ID" ] && [ "$TRACE_ID" != "null" ]; then
    echo "Found trace ID: $TRACE_ID"

    # Check if we can correlate using the Telemetry_ByTraceId index
    TRACE_CORRELATION=$(run_rql "from index 'Telemetry/ByTraceId' where OperationId == '$TRACE_ID' select ItemType, CloudRoleName" "Trace correlation")

    if echo "$TRACE_CORRELATION" | grep -q "ItemType"; then
        echo -e "${GREEN}✓ Distributed tracing correlation works${NC}"
        ((PASSED++))
    else
        echo -e "${YELLOW}⚠ Could not verify correlation (index may still be building)${NC}"
    fi
else
    echo -e "${YELLOW}⚠ No recent traces found for correlation test${NC}"
fi

echo ""
echo "Step 7: Testing custom indexes..."
echo "------------------------------------"

# Test Traces_ByServiceAndTime index
echo -n "- Testing Traces/ByServiceAndTime index... "
TRACES_INDEX=$(run_rql "from index 'Traces/ByServiceAndTime' limit 1" "Traces index")
if echo "$TRACES_INDEX" | grep -q "CloudRoleName\|TotalResults"; then
    echo -e "${GREEN}✓${NC}"
    ((PASSED++))
else
    echo -e "${YELLOW}⚠ (index may still be building)${NC}"
fi

# Test Requests_ByServiceAndStatus index
echo -n "- Testing Requests/ByServiceAndStatus index... "
REQUESTS_INDEX=$(run_rql "from index 'Requests/ByServiceAndStatus' limit 1" "Requests index")
if echo "$REQUESTS_INDEX" | grep -q "CloudRoleName\|TotalResults"; then
    echo -e "${GREEN}✓${NC}"
    ((PASSED++))
else
    echo -e "${YELLOW}⚠ (index may still be building)${NC}"
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
    echo "  - View RavenDB Studio at: $RAVENDB_URL/studio/index.html"
    echo "  - Run test queries from: scripts/ravendb-test-queries.rql"
    echo "  - View Aspire dashboard for service metrics"
    echo "  - Query examples available in RavenDB Studio query editor"
    exit 0
else
    echo -e "${RED}✗ Some validation checks failed${NC}"
    echo ""
    echo "Troubleshooting:"
    echo "  - Ensure all services are running (dotnet run --project src/MedicineTrack.AppHost)"
    echo "  - Check OTEL Collector logs for export errors"
    echo "  - Verify RavenDB is accessible at: $RAVENDB_URL"
    echo "  - Check RavenDB ingestion service logs"
    echo "  - Verify 'telemetry' database exists in RavenDB Studio"
    echo "  - Wait a few moments for indexes to build (they build asynchronously)"
    exit 1
fi
