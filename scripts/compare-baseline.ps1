# scripts/compare-baseline.ps1
param(
    [double]$ThresholdPercent = 15.0,
    [string]$BaselineFile = "benchmarks/baselines/latest.json"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BaselineFile)) {
    Write-Error "Baseline file not found at $BaselineFile. Run save-baseline.ps1 first."
    exit 1
}

Write-Host "Running performance verification suite against baseline..." -ForegroundColor Cyan

# Run performance verification test
dotnet test tests/Core.Tests/NileCore.Raar.Core.Tests.csproj `
    --filter "FullyQualifiedName=NileCore.Raar.Core.Tests.BaselineBenchmarkRunner.Measure_Baseline_Performance_And_Save" `
    -c Release

$baselineRaw = Get-Content $BaselineFile -Raw | ConvertFrom-Json
$currentFile = "benchmarks/baselines/latest.json"
$currentRaw = Get-Content $currentFile -Raw | ConvertFrom-Json

$regressions = @()
$improvements = @()

foreach ($current in $currentRaw.Benchmarks) {
    $match = $baselineRaw.Benchmarks | Where-Object {
        $_.Category -eq $current.Category -and $_.Name -eq $current.Name -and $_.Target -eq $current.Target -and $_.PayloadSize -eq $current.PayloadSize
    }

    if ($match) {
        $baseMicros = [double]$match.MeanExecutionTimeMicroseconds
        $currMicros = [double]$current.MeanExecutionTimeMicroseconds

        if ($baseMicros -gt 0) {
            $diffPercent = (($currMicros - $baseMicros) / $baseMicros) * 100.0

            if ($diffPercent -gt $ThresholdPercent) {
                $regressions += [PSCustomObject]@{
                    Category   = $current.Category
                    Benchmark  = "$($current.Target) - $($current.Name) ($($current.PayloadSize))"
                    Baseline   = "$([math]::Round($baseMicros, 3)) μs"
                    Current    = "$([math]::Round($currMicros, 3)) μs"
                    Difference = "+$([math]::Round($diffPercent, 1))%"
                }
            } elseif ($diffPercent -lt -$ThresholdPercent) {
                $improvements += [PSCustomObject]@{
                    Category   = $current.Category
                    Benchmark  = "$($current.Target) - $($current.Name) ($($current.PayloadSize))"
                    Baseline   = "$([math]::Round($baseMicros, 3)) μs"
                    Current    = "$([math]::Round($currMicros, 3)) μs"
                    Difference = "$([math]::Round($diffPercent, 1))%"
                }
            }
        }
    }
}

if ($improvements.Count -gt 0) {
    Write-Host "`n🚀 PERFORMANCE IMPROVEMENTS DETECTED:" -ForegroundColor Green
    $improvements | Format-Table -AutoSize
}

if ($regressions.Count -gt 0) {
    Write-Host "`n❌ PERFORMANCE REGRESSIONS DETECTED (> $ThresholdPercent%):" -ForegroundColor Red
    $regressions | Format-Table -AutoSize
    exit 1
} else {
    Write-Host "`n✅ No performance regressions detected! All benchmarks are within acceptable thresholds." -ForegroundColor Green
}
