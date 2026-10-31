param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUri,

    [string]$Symbol = "RKLX",
    [string]$Provider = "Alpaca",
    [string]$Feed = "iex",
    [Parameter(Mandatory = $true)]
    [DateTimeOffset]$FromUtc,
    [Parameter(Mandatory = $true)]
    [DateTimeOffset]$ToUtc,

    [string[]]$StreamSymbols = @("SPY", "RKLX", "ASTX", "XRPN"),
    [ValidateRange(60, 1800)]
    [int]$WaitSeconds = 300,
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

if ($FromUtc.Offset -ne [TimeSpan]::Zero -or $ToUtc.Offset -ne [TimeSpan]::Zero -or $FromUtc -gt $ToUtc) {
    throw "FromUtc and ToUtc must be UTC timestamps, and FromUtc must not exceed ToUtc."
}
if ([string]::IsNullOrWhiteSpace($BaseUri)) {
    throw "BaseUri is required, for example http://localhost:5080."
}

$BaseUri = $BaseUri.TrimEnd("/")

$baseAddress = [uri]$BaseUri
if ($baseAddress.Port -eq 53359 -and $baseAddress.Scheme -eq "http") {
    throw "The MCC Development launch profile maps port 53359 to HTTPS, not HTTP. Use -BaseUri 'https://localhost:53359' or -BaseUri 'http://localhost:53360'."
}
if ($baseAddress.Port -eq 53360 -and $baseAddress.Scheme -eq "https") {
    throw "The MCC Development launch profile maps port 53360 to HTTP, not HTTPS. Use -BaseUri 'http://localhost:53360' or -BaseUri 'https://localhost:53359'."
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $stamp = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ")
    $OutputDirectory = Join-Path (Get-Location) "artifacts/runtime-validation/$stamp"
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

function Save-Json {
    param([string]$Name, [object]$Value)
    $path = Join-Path $OutputDirectory $Name
    $Value | ConvertTo-Json -Depth 100 | Set-Content -Encoding utf8 $path
    return $path
}

function Get-ApiJson {
    param([string]$Path)
    return Invoke-RestMethod -Method Get -Uri "$BaseUri$Path" -TimeoutSec 30
}

function Get-StreamBarCount {
    param([object]$Status, [string]$Ticker)
    $entry = $Status.barsBySymbol.PSObject.Properties[$Ticker]
    if ($null -eq $entry -or $null -eq $entry.Value) { return 0 }
    return [long]$entry.Value.barCount
}

function Get-StreamLastBarUtc {
    param([object]$Status, [string]$Ticker)
    $entry = $Status.barsBySymbol.PSObject.Properties[$Ticker]
    if ($null -eq $entry -or $null -eq $entry.Value) { return $null }
    return $entry.Value.lastBarUtc
}

$encodedSymbol = [uri]::EscapeDataString($Symbol)
$fromText = $FromUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
$toText = $ToUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

Write-Host "Checking API health at $BaseUri/api/health..."
$health = Get-ApiJson "/api/health"
if ($health.status -ne "OK") {
    throw "The configured BaseUri did not return API health status OK. Returned status: $($health.status)"
}

Write-Host "Capturing stored RKLX bars (provider=$Provider, feed=$Feed, window=$fromText to $toText)..."
$stored = @{}
foreach ($timeframe in @("1m", "5m")) {
    $query = "symbol=$encodedSymbol&fromUtc=$([uri]::EscapeDataString($fromText))&toUtc=$([uri]::EscapeDataString($toText))&provider=$([uri]::EscapeDataString($Provider))&timeframe=$timeframe&limit=10000"
    $response = Get-ApiJson "/api/market-data/stored-bars?$query"
    $matching = @($response.observations | Where-Object {
        $_.bar.feed -eq $Feed -and $_.bar.timeframe -eq $timeframe
    })
    $stored[$timeframe] = [pscustomobject]@{
        symbol = $Symbol
        provider = $Provider
        feed = $Feed
        timeframe = $timeframe
        fromUtc = $fromText
        toUtc = $toText
        responseCount = $response.count
        matchingFeedCount = $matching.Count
        firstSourceTimestampUtc = if ($matching.Count) { ($matching | Sort-Object sourceTimestamp | Select-Object -First 1).sourceTimestamp } else { $null }
        latestSourceTimestampUtc = if ($matching.Count) { ($matching | Sort-Object sourceTimestamp | Select-Object -Last 1).sourceTimestamp } else { $null }
        observations = $matching
    }
}
Save-Json "stored-bars.json" $stored | Out-Null

Write-Host "Capturing stream and collector status before the wait..."
$before = [pscustomobject]@{
    capturedAtUtc = [DateTimeOffset]::UtcNow
    stream = Get-ApiJson "/api/market-data/stream/status"
    collector = Get-ApiJson "/api/market-data/collector/status"
    providers = Get-ApiJson "/api/providers/status"
}
Save-Json "runtime-before.json" $before | Out-Null

Write-Host "Waiting $WaitSeconds seconds. Keep the API process running and run during regular market hours."
Start-Sleep -Seconds $WaitSeconds

Write-Host "Capturing stream and collector status after the wait..."
$after = [pscustomobject]@{
    capturedAtUtc = [DateTimeOffset]::UtcNow
    stream = Get-ApiJson "/api/market-data/stream/status"
    collector = Get-ApiJson "/api/market-data/collector/status"
    providers = Get-ApiJson "/api/providers/status"
}
Save-Json "runtime-after.json" $after | Out-Null

$assessmentResults = @()
foreach ($ticker in $StreamSymbols) {
    $assessmentResults += [pscustomobject]@{
        symbol = $ticker
        assessment = Get-ApiJson "/api/market-data/live-assessment?symbol=$([uri]::EscapeDataString($ticker))&providerName=$([uri]::EscapeDataString($Provider))"
    }
}
Save-Json "live-assessments.json" $assessmentResults | Out-Null

$streamDelta = foreach ($ticker in $StreamSymbols) {
    $beforeCount = Get-StreamBarCount $before.stream $ticker
    $afterCount = Get-StreamBarCount $after.stream $ticker
    [pscustomobject]@{
        symbol = $ticker
        acknowledgedBefore = $before.stream.subscribedSymbols -contains $ticker
        acknowledgedAfter = $after.stream.subscribedSymbols -contains $ticker
        barCountBefore = $beforeCount
        barCountAfter = $afterCount
        newBarsObserved = ($afterCount - $beforeCount)
        lastBarBeforeUtc = Get-StreamLastBarUtc $before.stream $ticker
        lastBarAfterUtc = Get-StreamLastBarUtc $after.stream $ticker
        advanced = ($afterCount -gt $beforeCount)
    }
}
Save-Json "stream-delta.json" $streamDelta | Out-Null

Write-Host ""
Write-Host "Runtime validation artifacts saved to: $OutputDirectory"
Write-Host "Stored-bar counts for $Symbol ($Provider/$Feed): 1m=$($stored['1m'].matchingFeedCount), 5m=$($stored['5m'].matchingFeedCount)"
$streamDelta | Format-Table -AutoSize
Write-Host "Interpretation:"
Write-Host "- A subscribed symbol with no new bars is not automatically a failure unless the test window is during regular market hours and the feed should have eligible prints."
Write-Host "- Compare assessment.oneMinute.sourceTransport and sourceTimestampUtc; require WebSocket provenance, >=50 completed bars, and freshness before expecting ALPACA_WEBSOCKET_LATEST_BAR."
Write-Host "- 5m permission remains REST-backed. Do not lower freshness thresholds to hide provider gaps."
Write-Host "- Preserve all JSON files with the branch SHA and the exact local test window."
