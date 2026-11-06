[CmdletBinding()]
param(
    [string]$BaseUri = "http://localhost:53360",
    [string[]]$Symbols = @("SPY", "RKLX", "ASTX", "XRPN"),
    [ValidateRange(5, 60)][int]$PollIntervalSeconds = 15,
    [ValidateRange(2, 30)][int]$DurationMinutes = 5,
    [switch]$ConfirmRegularSession,
    [string]$OutputDirectory = ""
)
$ErrorActionPreference = "Stop"
if (-not $ConfirmRegularSession) { throw "Run only during a confirmed US-equity regular session and pass -ConfirmRegularSession. This script does not infer holidays or early closes." }
$BaseUri = $BaseUri.TrimEnd("/")
$eastern = [TimeZoneInfo]::FindSystemTimeZoneById("Eastern Standard Time")
$nowEastern = [TimeZoneInfo]::ConvertTime([DateTimeOffset]::Now, $eastern)
if ($nowEastern.DayOfWeek -in @([DayOfWeek]::Saturday, [DayOfWeek]::Sunday) -or $nowEastern.TimeOfDay -lt ([TimeSpan]::Parse("09:30:00")) -or $nowEastern.TimeOfDay -ge ([TimeSpan]::Parse("16:00:00"))) {
    throw "Current Eastern time $($nowEastern.ToString('yyyy-MM-dd HH:mm:ss zzz')) is outside 09:30-16:00 ET. Run during the actual regular session; holidays/early closes still require operator confirmation."
}
$stamp = [DateTimeOffset]::UtcNow.ToString("yyyyMMddTHHmmssZ")
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
    $OutputDirectory = Join-Path $repoRoot "artifacts/runtime-validation/stream-collector-$stamp"
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
function Get-ApiJson([string]$Path) { Invoke-RestMethod -Method Get -Uri "$BaseUri$Path" -TimeoutSec 30 }
function Get-PropertyValue([object]$Object, [string]$Name, $Default = $null) {
    if ($null -eq $Object) { return $Default }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $Default }
    return $property.Value
}
function Get-SymbolStream([object]$Stream, [string]$Symbol) {
    $map = Get-PropertyValue $Stream "barsBySymbol"
    return Get-PropertyValue $map $Symbol
}
function Get-Counter([object]$Object, [string]$Name) {
    $value = Get-PropertyValue $Object $Name 0
    if ($null -eq $value) { return 0L }
    try { return [long]$value } catch { return 0L }
}
function Save-Json([string]$Name, [object]$Value) { $Value | ConvertTo-Json -Depth 100 | Set-Content -Encoding utf8 (Join-Path $OutputDirectory $Name) }

$health = Get-ApiJson "/api/health"
if ($health.status -ne "OK") { throw "API health is not OK." }
$provider = Get-ApiJson "/api/providers/status"
$samples = [System.Collections.Generic.List[object]]::new()
$sampleCount = [math]::Ceiling(($DurationMinutes * 60) / $PollIntervalSeconds) + 1
Write-Host "Polling stream and collector status every $PollIntervalSeconds seconds for $DurationMinutes minutes."
Write-Host "Symbols: $($Symbols -join ', '). Feed remains whatever /api/providers/status reports; this script never changes it."
for ($i = 0; $i -lt $sampleCount; $i++) {
    $capturedAt = [DateTimeOffset]::UtcNow
    $samples.Add([pscustomobject]@{
        sample = $i
        capturedAtUtc = $capturedAt.ToString("o")
        stream = Get-ApiJson "/api/market-data/stream/status"
        collector = Get-ApiJson "/api/market-data/collector/status"
        providers = Get-ApiJson "/api/providers/status"
    })
    Write-Progress -Activity "MCC regular-session stream/collector capture" -Status ("Sample {0}/{1} at {2}" -f ($i + 1), $sampleCount, $capturedAt.ToString("HH:mm:ss 'UTC'")) -PercentComplete ([int](100 * ($i + 1) / $sampleCount))
    if ($i -lt ($sampleCount - 1)) { Start-Sleep -Seconds $PollIntervalSeconds }
}
Write-Progress -Activity "MCC regular-session stream/collector capture" -Completed
Save-Json "api-health.json" $health
Save-Json "provider-status-initial.json" $provider
Save-Json "status-samples.json" @($samples)
$streamStart = $samples[0].stream
$streamEnd = $samples[$samples.Count - 1].stream
$collectorStart = $samples[0].collector
$collectorEnd = $samples[$samples.Count - 1].collector
$perSymbol = foreach ($symbol in $Symbols) {
    $start = Get-SymbolStream $streamStart $symbol
    $end = Get-SymbolStream $streamEnd $symbol
    $startCount = Get-Counter $start "barCount"
    $endCount = Get-Counter $end "barCount"
    $startLast = Get-PropertyValue $start "lastBarUtc"
    $endLast = Get-PropertyValue $end "lastBarUtc"
    [pscustomobject]@{
        symbol = $symbol
        acknowledgedAtStart = @($streamStart.subscribedSymbols) -contains $symbol
        acknowledgedAtEnd = @($streamEnd.subscribedSymbols) -contains $symbol
        barCountStart = $startCount
        barCountEnd = $endCount
        completedBarCountDelta = ($endCount - $startCount)
        lastBarStartUtc = $startLast
        lastBarEndUtc = $endLast
        lastBarAdvanced = ($null -ne $startLast -and $null -ne $endLast -and ([DateTimeOffset]::Parse([string]$endLast) -gt [DateTimeOffset]::Parse([string]$startLast)))
        receivedBarsDelta = (Get-Counter $streamEnd "receivedBars") - (Get-Counter $streamStart "receivedBars")
        persistedBarsDelta = (Get-Counter $streamEnd "persistedBars") - (Get-Counter $streamStart "persistedBars")
    }
}
$collectorDelta = [pscustomobject]@{
    enabledAtStart = Get-PropertyValue $collectorStart "enabled"
    enabledAtEnd = Get-PropertyValue $collectorEnd "enabled"
    configuredSymbols = @(Get-PropertyValue $collectorEnd "configuredSymbols" @())
    cyclesStart = Get-Counter $collectorStart "cycles"
    cyclesEnd = Get-Counter $collectorEnd "cycles"
    cyclesDelta = (Get-Counter $collectorEnd "cycles") - (Get-Counter $collectorStart "cycles")
    lastCycleStartedUtc = Get-PropertyValue $collectorEnd "lastCycleStartedUtc"
    lastCycleCompletedUtc = Get-PropertyValue $collectorEnd "lastCycleCompletedUtc"
    lastCycleErrors = Get-PropertyValue $collectorEnd "lastCycleErrors"
    recentErrors = @(Get-PropertyValue $collectorEnd "recentErrors" @())
    watermarks = @(Get-PropertyValue $collectorEnd "watermarks" @())
}
Save-Json "stream-delta.json" @($perSymbol)
Save-Json "collector-delta.json" $collectorDelta
$assessments = foreach ($symbol in $Symbols) {
    try { [pscustomobject]@{ symbol=$symbol; status="OK"; assessment=(Get-ApiJson "/api/market-data/live-assessment?symbol=$([uri]::EscapeDataString($symbol))&providerName=Alpaca") } }
    catch { [pscustomobject]@{ symbol=$symbol; status="ERROR"; error=$_.Exception.Message } }
}
Save-Json "live-assessments-after.json" @($assessments)
$sourceCommit = $null
try { $repoRootForGit = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path; $sourceCommit = (& git -C $repoRootForGit rev-parse HEAD 2>$null).Trim() } catch {}
$manifest = [ordered]@{
    sourceCommit = $sourceCommit
    capturedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    baseUri = $BaseUri
    initialEasternTime = $nowEastern.ToString("o")
    durationMinutes = $DurationMinutes
    pollIntervalSeconds = $PollIntervalSeconds
    sampleCount = $samples.Count
    symbols = @($Symbols)
    configuredProviderStatus = $provider
    streamStateAtStart = $streamStart.state
    streamStateAtEnd = $streamEnd.state
    streamFeedAtStart = $streamStart.feed
    streamFeedAtEnd = $streamEnd.feed
    streamReceivedBarsDelta = (Get-Counter $streamEnd "receivedBars") - (Get-Counter $streamStart "receivedBars")
    streamPersistedBarsDelta = (Get-Counter $streamEnd "persistedBars") - (Get-Counter $streamStart "persistedBars")
    collectorCyclesDelta = $collectorDelta.cyclesDelta
    note = "SUBSCRIBED is acknowledgement only. Per-symbol delivery requires acknowledged subscription plus advancing completed-bar count and lastBarUtc during this RTH window. Collector acceptance separately requires enabled state, advancing cycles/watermarks and no unexplained errors. Sparse IEX symbols are not automatically global stream failures."
}
[pscustomobject]$manifest | ConvertTo-Json -Depth 50 | Set-Content -Encoding utf8 (Join-Path $OutputDirectory "manifest.json")
Write-Host ""
Write-Host "Evidence saved to: $OutputDirectory"
Write-Host "Stream state: $($streamStart.state) -> $($streamEnd.state); feed: $($streamStart.feed) -> $($streamEnd.feed)"
$perSymbol | Format-Table -AutoSize
Write-Host "Collector cycles: $($collectorDelta.cyclesStart) -> $($collectorDelta.cyclesEnd); delta=$($collectorDelta.cyclesDelta); enabled=$($collectorDelta.enabledAtEnd)"
Write-Host "Do not call this a pass unless subscription, completed-bar advancement, timestamp advancement and persistence evidence satisfy the runbook."
