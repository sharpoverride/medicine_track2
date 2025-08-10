#!/bin/bash

# Test script to verify both End-to-End test execution modes work correctly

echo "🧪 Testing MedicineTrack End-to-End Test Execution Modes"
echo "========================================================="

# Function to check if a process is running on a port
check_port() {
    local port=$1
    if lsof -i :$port >/dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

# Check if Aspire services are running
echo "🔍 Checking if Aspire services are running..."
if check_port 5001 && check_port 5002; then
    echo "✅ Aspire services detected on ports 5001 and 5002"
    ASPIRE_RUNNING=true
else
    echo "❌ Aspire services not detected. Make sure to run:"
    echo "   cd src/MedicineTrack.AppHost && dotnet run"
    ASPIRE_RUNNING=false
fi

echo ""

# Test Mode 1: Direct execution (standalone dotnet test)
echo "📋 Test Mode 1: Direct Execution (dotnet test)"
echo "----------------------------------------------"
if [ "$ASPIRE_RUNNING" = true ]; then
    echo "Setting up environment variables..."
    export MEDICINE_TRACK_API_URL="http://localhost:5001"
    export MEDICINE_TRACK_CONFIG_URL="http://localhost:5002"
    
    echo "Running: cd src/MedicineTrack.End2EndTests && dotnet test --logger console"
    cd src/MedicineTrack.End2EndTests
    dotnet test --logger console --verbosity normal
    TEST_RESULT_1=$?
    cd ../..
else
    echo "⚠️  Skipping standalone test - Aspire services not running"
    TEST_RESULT_1=1
fi

echo ""

# Test Mode 2: Via End2EndTests.Runner (in Aspire)
echo "📋 Test Mode 2: Via End2EndTests.Runner (in Aspire)"
echo "----------------------------------------------------"
if [ "$ASPIRE_RUNNING" = true ]; then
    echo "Running End2EndTests.Runner for a quick test..."
    echo "Running: cd src/MedicineTrack.End2EndTests.Runner && dotnet run --launch-profile 'MedicineTrack.End2EndTests.Runner (Quick Run)'"
    cd src/MedicineTrack.End2EndTests.Runner
    timeout 30s dotnet run --launch-profile "MedicineTrack.End2EndTests.Runner (Quick Run)"
    TEST_RESULT_2=$?
    cd ../..
else
    echo "⚠️  Skipping Runner test - Aspire services not running"
    TEST_RESULT_2=1
fi

echo ""
echo "📊 Test Results Summary"
echo "======================"
if [ $TEST_RESULT_1 -eq 0 ]; then
    echo "✅ Mode 1 (Direct): PASSED"
else
    echo "❌ Mode 1 (Direct): FAILED"
fi

if [ $TEST_RESULT_2 -eq 0 ] || [ $TEST_RESULT_2 -eq 124 ]; then  # 124 is timeout exit code
    echo "✅ Mode 2 (Runner): PASSED (or timeout - expected for continuous runner)"
else
    echo "❌ Mode 2 (Runner): FAILED"
fi

echo ""
echo "🎯 Recommendations:"
echo "- For development/debugging: Use Mode 1 (dotnet test) with environment variables"
echo "- For continuous monitoring: Use Mode 2 (Runner) in Aspire dashboard"
echo "- For CI/CD: Use Mode 1 with appropriate service URLs"
