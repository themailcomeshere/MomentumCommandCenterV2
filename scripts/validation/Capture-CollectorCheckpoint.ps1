param(
    [string]$BaseUri = "http://localhost:53360",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"
$BaseUri = $BaseUri.TrimEnd("/")
$baseAddress = [uri]$BaseUri
if ($baseAddress.Port -eq 53359 -and $baseAddress.Scheme -eq "http") {
    throw "Port 53359 is HTTPS in the MCC Development launch profile. Use https://localhost:53359 or http://localhost:53360."
}
if ($baseAddress.Port -eq 53360 -and $baseAddress.Scheme -eq "https") {
    throw "Port 53360 is HTTP in the MCC Development launch profile. Use http://localhost:53360 or https://localhost:53359."
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $stamp = [DateTimeOffset]::UtcNow.ToString("yyyyMMddTHHmmssZ")
    $OutputDirectory = Join-Path (Get-Location) "artifacts/collector-checkpoint/$stamp"
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

try {
    $health = Invoke-RestMethod -Method Get -Uri "$BaseUri/api/health" -TimeoutSec 30
} catch {
    throw "MCC API health check failed at $BaseUri/api/health. Verify the API is running and the URL scheme/port match launchSettings.json. Original error: $($_.Exception.Message)"
}
if ($health.status -ne "OK") { throw "MCC API health returned '$($health.status)', expected OK." }

$status = Invoke-RestMethod -Method Get -Uri "$BaseUri/api/market-data/collector/status" -TimeoutSec 30
$statusPath = Join-Path $OutputDirectory "collector-status.json"
$status | ConvertTo-Json -Depth 100 | Set-Content -Encoding utf8 $statusPath

$checkpointPath = [string]$status.checkpointPath
$manifest = [ordered]@{
    capturedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    baseUri = $BaseUri
    configuredSymbols = @($status.configuredSymbols)
    configuredProviders = @($status.configuredProviders)
    intervalSeconds = $status.intervalSeconds
    overlapMinutes = $status.overlapMinutes
    cycles = $status.cycles
    lastCycleErrors = $status.lastCycleErrors
    lastCycleStartedUtc = $status.lastCycleStartedUtc
    lastCycleCompletedUtc = $status.lastCycleCompletedUtc
    recentErrors = @($status.recentErrors)
    checkpointPath = $checkpointPath
    checkpointFileStatus = "NOT_FOUND"
    checkpointCopy = $null
    checkpointLengthBytes = $null
    checkpointLastWriteTimeUtc = $null
    checkpointSha256 = $null
    watermarksFromApi = @($status.watermarks)
}

if (-not [string]::IsNullOrWhiteSpace($checkpointPath) -and (Test-Path -LiteralPath $checkpointPath -PathType Leaf)) {
    $file = Get-Item -LiteralPath $checkpointPath
    $copyPath = Join-Path $OutputDirectory "collector-watermarks.json"
    Copy-Item -LiteralPath $checkpointPath -Destination $copyPath -Force
    $hash = (Get-FileHash -LiteralPath $copyPath -Algorithm SHA256).Hash
    $manifest.checkpointFileStatus = "COPIED"
    $manifest.checkpointCopy = $copyPath
    $manifest.checkpointLengthBytes = $file.Length
    $manifest.checkpointLastWriteTimeUtc = $file.LastWriteTimeUtc.ToString("o")
    $manifest.checkpointSha256 = $hash
}

$manifestPath = Join-Path $OutputDirectory "checkpoint-manifest.json"
[pscustomobject]$manifest | ConvertTo-Json -Depth 100 | Set-Content -Encoding utf8 $manifestPath

Write-Host "Collector evidence saved to: $OutputDirectory"
Write-Host "Checkpoint path: $checkpointPath"
Write-Host "Checkpoint file: $($manifest.checkpointFileStatus)"
Write-Host "SHA-256: $($manifest.checkpointSha256)"
Write-Host "Cycles: $($status.cycles) · last-cycle errors: $($status.lastCycleErrors)"
