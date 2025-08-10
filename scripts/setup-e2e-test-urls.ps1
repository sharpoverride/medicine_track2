# Setup script for MedicineTrack End-to-End Tests
# This script sets the correct URLs for the Aspire services based on the dashboard

Write-Host "🔧 Setting up MedicineTrack End-to-End Test URLs..." -ForegroundColor Cyan

# Primary service endpoints (matching the dashboard)
$env:MEDICINE_TRACK_API_URL = "http://localhost:5001"
$env:MEDICINE_TRACK_CONFIG_URL = "http://localhost:5002"

# Alternative endpoints available from the dashboard
# API Service endpoints:
# - services__medicine-track-api__api-http__0: http://localhost:5001
# - services__medicine-track-api__http__0: http://localhost:5155  
# - services__medicine-track-api__https__0: https://localhost:7001

# Config Service endpoints:
# - services__medicine-track-config__config-http__0: http://localhost:5002
# - services__medicine-track-config__http__0: http://localhost:5111
# - services__medicine-track-config__https__0: https://localhost:7263

Write-Host "✅ Environment variables set:" -ForegroundColor Green
Write-Host "   MEDICINE_TRACK_API_URL=$env:MEDICINE_TRACK_API_URL" -ForegroundColor White
Write-Host "   MEDICINE_TRACK_CONFIG_URL=$env:MEDICINE_TRACK_CONFIG_URL" -ForegroundColor White
Write-Host ""
Write-Host "🚀 You can now run your end-to-end tests with:" -ForegroundColor Yellow
Write-Host "   cd src\MedicineTrack.End2EndTests" -ForegroundColor White
Write-Host "   dotnet test" -ForegroundColor White
Write-Host ""
Write-Host "💡 To use alternative endpoints, modify the `$env: commands above:" -ForegroundColor Magenta
Write-Host "   For HTTPS API: `$env:MEDICINE_TRACK_API_URL = `"https://localhost:7001`"" -ForegroundColor White
Write-Host "   For HTTPS Config: `$env:MEDICINE_TRACK_CONFIG_URL = `"https://localhost:7263`"" -ForegroundColor White
