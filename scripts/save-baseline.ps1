# scripts/save-baseline.ps1
param(
    [string]$Version = "1.0.4"
)

$ErrorActionPreference = "Stop"
Write-Host "Running baseline benchmarks..." -ForegroundColor Cyan

# Run benchmark test
dotnet test tests/Core.Tests/NileCore.Raar.Core.Tests.csproj `
    --filter "FullyQualifiedName=NileCore.Raar.Core.Tests.BaselineBenchmarkRunner.Measure_Baseline_Performance_And_Save" `
    -c Release

$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$baselineFile = "benchmarks/baselines/latest.json"

if (Test-Path $baselineFile) {
    Copy-Item $baselineFile "benchmarks/baselines/history/${Version}_${timestamp}.json" -Force
    Write-Host "Baseline successfully saved for version ${Version} at ${timestamp}" -ForegroundColor Green
} else {
    Write-Error "Baseline output file not found at $baselineFile"
}
