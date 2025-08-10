#!/bin/bash

# Setup script for MedicineTrack End-to-End Tests
# This script sets the correct URLs for the Aspire services based on the dashboard

echo "🔧 Setting up MedicineTrack End-to-End Test URLs..."

# Primary service endpoints (matching the dashboard)
export MEDICINE_TRACK_API_URL="http://localhost:5001"
export MEDICINE_TRACK_CONFIG_URL="http://localhost:5002"

# Alternative endpoints available from the dashboard
# API Service endpoints:
# - services__medicine-track-api__api-http__0: http://localhost:5001
# - services__medicine-track-api__http__0: http://localhost:5155  
# - services__medicine-track-api__https__0: https://localhost:7001

# Config Service endpoints:
# - services__medicine-track-config__config-http__0: http://localhost:5002
# - services__medicine-track-config__http__0: http://localhost:5111
# - services__medicine-track-config__https__0: https://localhost:7263

echo "✅ Environment variables set:"
echo "   MEDICINE_TRACK_API_URL=$MEDICINE_TRACK_API_URL"
echo "   MEDICINE_TRACK_CONFIG_URL=$MEDICINE_TRACK_CONFIG_URL"
echo ""
echo "🚀 You can now run your end-to-end tests with:"
echo "   cd src/MedicineTrack.End2EndTests"
echo "   dotnet test"
echo ""
echo "💡 To use alternative endpoints, modify the export commands above:"
echo "   For HTTPS API: export MEDICINE_TRACK_API_URL=\"https://localhost:7001\""
echo "   For HTTPS Config: export MEDICINE_TRACK_CONFIG_URL=\"https://localhost:7263\""
