[CmdletBinding()]
param(
    [string]$BaseUri = "http://localhost:53360",
    [string]$OutputRoot = "artifacts/atr-parity-evidence",
    [string[]]$Symbols = @("EVR", "CBOE"),
    [int]$WarmupBars = 99,
    [int]$WindowBars = 119
)

$ErrorActionPreference = "Stop"
$BaseUri = $BaseUri.TrimEnd("/")
if ($WarmupBars -lt 15 -or $WindowBars -le $WarmupBars) {
    throw "WarmupBars must be >= 15 and WindowBars must exceed WarmupBars."
}

# Verify the API before creating an evidence directory. A connection failure here
# means the API is stopped or BaseUri does not match its active HTTP listener.
try {
    $health = Invoke-RestMethod -Uri "$BaseUri/api/health" -Method Get -TimeoutSec 10 -ErrorAction Stop
}
catch {
    $detail = $_.Exception.Message
    throw @"
Cannot reach the MCC API health endpoint: $BaseUri/api/health
Reason: $detail

Start the API from the repository root in a separate PowerShell window:
  dotnet run --project .\src\MomentumCommandCenterV2.API\MomentumCommandCenterV2.API.csproj --launch-profile MomentumCommandCenterV2.API

Keep that window running, then verify in this window:
  Invoke-RestMethod 'http://localhost:53360/api/health' | ConvertTo-Json -Depth 8

The launch profile uses HTTP http://localhost:53360 and HTTPS https://localhost:53359.
Use the matching scheme and port. If your API is listening elsewhere, rerun this script
with -BaseUri 'http://localhost:<actual-http-port>'.
No evidence files were collected.
"@
}

if ($health.status -and $health.status -ne "OK") {
    throw "MCC API responded at $BaseUri but health status is '$($health.status)', not 'OK'. No evidence files were collected."
}

$stamp = (Get-Date).ToString("yyyyMMddTHHmmss")
$outputDirectory = Join-Path $OutputRoot $stamp
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

function Save-Json {
    param([string]$Path, [object]$Value)
    $json = $Value | ConvertTo-Json -Depth 50
    [System.IO.File]::WriteAllText(
        $Path,
        $json,
        [System.Text.UTF8Encoding]::new($false)
    )
}

$localSha = $null
try {
    $localSha = (git rev-parse HEAD 2>$null | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($localSha)) { $localSha = $null }
}
catch { $localSha = $null }

Save-Json (Join-Path $outputDirectory "health.json") $health

$providers = Invoke-RestMethod "$BaseUri/api/providers/status"
Save-Json (Join-Path $outputDirectory "providers-status.json") $providers

$queries = @()
foreach ($symbolValue in $Symbols) {
    $symbol = $symbolValue.Trim().ToUpperInvariant()
    if ($symbol -notmatch '^[A-Z0-9.-]{1,8}$') {
        throw "Invalid symbol '$symbolValue'."
    }

    foreach ($timeframe in @("1m", "5m")) {
        $query = "symbol=$([uri]::EscapeDataString($symbol))&timeframe=$timeframe"
        $parityUri = "$BaseUri/api/strategy-report/atr-parity?$query&warmupBars=$WarmupBars&windowBars=$WindowBars"
        $observationsUri = "$BaseUri/api/strategy-report/atr-observations?$query&limit=10000"

        $parity = Invoke-RestMethod $parityUri
        $observations = Invoke-RestMethod $observationsUri

        $stem = "$symbol-$timeframe"
        Save-Json (Join-Path $outputDirectory "$stem-parity.json") $parity
        Save-Json (Join-Path $outputDirectory "$stem-tos-observations.json") $observations

        $queries += [pscustomobject]@{
            Symbol = $symbol
            Timeframe = $timeframe
            ParityUri = $parityUri
            ObservationsUri = $observationsUri
            ParityStatus = $parity.status
            SourceObservationCount = $parity.sourceObservationCount
            ComparedBarCount = $parity.comparedBarCount
            TimestampGapCount = $parity.timestampGapCount
            ObservationRowsReturned = $observations.count
        }
    }
}

$manifest = [pscustomobject]@{
    CapturedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    CapturedAtLocal = (Get-Date).ToString("O")
    BaseUri = $BaseUri
    LocalGitSha = $localSha
    ApiHealth = $health
    ProviderStatus = $providers
    WarmupBars = $WarmupBars
    ComparisonWindowBars = $WindowBars
    Queries = $queries
    ImportantBoundary = "TOS ATR observations and parity are exported separately from provider OHLCV. This script does not prove provider/TOS bar equality, classify missing timestamps, or infer broker fills."
}
Save-Json (Join-Path $outputDirectory "manifest.json") $manifest

$hashes = Get-ChildItem -File $outputDirectory |
    Where-Object { $_.Name -ne "sha256.txt" } |
    Sort-Object Name |
    ForEach-Object {
        $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName
        "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $_.Name
    }
[System.IO.File]::WriteAllLines(
    (Join-Path $outputDirectory "sha256.txt"),
    $hashes,
    [System.Text.UTF8Encoding]::new($false)
)

Write-Host "ATR parity evidence saved to: $outputDirectory"
Write-Host "Local Git SHA: $(if ($localSha) { $localSha } else { 'not available (run inside the repository)' })"
$queries | Format-Table -AutoSize
