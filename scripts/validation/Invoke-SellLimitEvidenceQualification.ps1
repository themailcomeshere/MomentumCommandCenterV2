[CmdletBinding()]
param(
    [string]$BaseUri = "http://localhost:53360",
    [string[]]$Symbols = @("EVR", "CBOE"),
    [ValidateRange(1, 99)]
    [int]$TargetQuantilePct = 75,
    [string]$OutputRoot = "artifacts/sell-limit-evidence"
)

$ErrorActionPreference = "Stop"

function Save-Json {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)]$Value)
    $Value | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-UtcQueryValue {
    param([Parameter(Mandatory)][DateTimeOffset]$Value)
    return [uri]::EscapeDataString($Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"))
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
    $resolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
}
else {
    $resolvedOutputRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputRoot))
}

# Preflight before creating evidence output or issuing provider requests.
try {
    $health = Invoke-RestMethod -Uri "$BaseUri/api/health" -TimeoutSec 10
}
catch {
    throw "MCC API is unavailable at $BaseUri. Start it with: dotnet run --project .\src\MomentumCommandCenterV2.API\MomentumCommandCenterV2.API.csproj --launch-profile MomentumCommandCenterV2.API. Original error: $($_.Exception.Message)"
}

if ($health.status -ne "OK" -or $health.mode -ne "decision-support-only" -or $health.orderSubmission -ne "NOT IMPLEMENTED") {
    throw "Unexpected API health/mode response. Refusing qualification run."
}

$providerStatuses = Invoke-RestMethod -Uri "$BaseUri/api/providers/status" -TimeoutSec 15
$alpaca = @($providerStatuses | Where-Object { $_.provider -eq "Alpaca" }) | Select-Object -First 1
if ($null -eq $alpaca -or -not $alpaca.configured) {
    throw "Alpaca is not configured in the running API. No provider requests were made."
}

$timestamp = Get-Date -Format "yyyyMMddTHHmmss"
$outputDirectory = Join-Path $resolvedOutputRoot $timestamp
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

Save-Json -Path (Join-Path $outputDirectory "health.json") -Value $health
Save-Json -Path (Join-Path $outputDirectory "provider-status.json") -Value $providerStatuses

$effectiveFeed = ([string]$alpaca.configuredFeed).Trim().ToLowerInvariant()
Write-Host "API: $($health.status); Alpaca feed: $effectiveFeed; provider validated: $($alpaca.validated)"
Write-Host "Evidence output: $outputDirectory"

# Once the operator has explicitly switched the effective configuration to SIP,
# perform one small SPY 1m smoke request before any symbol-level parity or 40-session capture.
if ($effectiveFeed -eq "sip") {
    $smokeTo = [DateTimeOffset]::UtcNow
    $smokeFrom = $smokeTo.AddDays(-7)
    $smokeUri = "$BaseUri/api/market-data/bars?symbol=SPY&timeframe=1m&from=$(Get-UtcQueryValue -Value $smokeFrom)&to=$(Get-UtcQueryValue -Value $smokeTo)&providerName=Alpaca"
    try {
        $smoke = Invoke-RestMethod -Uri $smokeUri -TimeoutSec 60
        Save-Json -Path (Join-Path $outputDirectory "sip-spy-smoke.json") -Value $smoke
        if ([string]$smoke.feed -ne "sip" -or [int]$smoke.barCount -lt 1) {
            throw "SPY SIP smoke did not return at least one bar from the SIP feed. Returned feed='$($smoke.feed)', bars=$($smoke.barCount)."
        }
        Write-Host "SPY SIP smoke PASS: $($smoke.barCount) 1m bars from feed '$($smoke.feed)'."
    }
    catch {
        Save-Json -Path (Join-Path $outputDirectory "sip-spy-smoke-error.json") -Value ([pscustomobject]@{
            error = $_.Exception.Message
            note = "No 40-session capture was attempted because the preliminary SPY SIP smoke test did not pass."
        })
        throw "SIP smoke test failed. No 40-session capture was attempted. $($_.Exception.Message)"
    }
}
else {
    Write-Warning "Effective feed is '$effectiveFeed'. SIP smoke and 40-session qualification will be skipped; provider comparison remains diagnostic only."
}

$summary = [System.Collections.Generic.List[object]]::new()

foreach ($symbolValue in $Symbols) {
    $symbol = $symbolValue.Trim().ToUpperInvariant()
    if ($symbol -notmatch '^[A-Z0-9.-]{1,8}$') {
        throw "Invalid symbol '$symbolValue'."
    }

    Write-Host "===== $($symbol): TOS parity and provider OHLCV comparison ====="

    $parityUri = "$BaseUri/api/strategy-report/atr-parity?symbol=$symbol&timeframe=5m&warmupBars=99&windowBars=119"
    $parity = Invoke-RestMethod -Uri $parityUri -TimeoutSec 30
    Save-Json -Path (Join-Path $outputDirectory "$symbol-atr-parity.json") -Value $parity

    $observationsUri = "$BaseUri/api/strategy-report/atr-observations?symbol=$symbol&timeframe=5m&limit=10000"
    $observationResponse = Invoke-RestMethod -Uri $observationsUri -TimeoutSec 30
    Save-Json -Path (Join-Path $outputDirectory "$symbol-tos-observations.json") -Value $observationResponse

    $series = @($observationResponse.observations |
        Where-Object { $_.metadata.studyVersion -eq "MCC_OHLCV_ATR_V2.1" } |
        Sort-Object { [DateTimeOffset]::Parse($_.observation.sourceTimestampUtc).UtcDateTime.Ticks })

    $comparisonMode = "DIAGNOSTIC_ONLY"
    $diagnosticOnlyReason = "Effective provider feed is '$effectiveFeed'; IEX/other non-SIP comparisons cannot qualify the sell-limit gate."
    if ($effectiveFeed -eq "sip") {
        $comparisonMode = "QUALIFICATION_CANDIDATE"
        $diagnosticOnlyReason = $null
    }

    $providerComparison = [ordered]@{
        symbol = $symbol
        configuredFeed = $effectiveFeed
        qualificationFeed = "sip"
        qualificationFeedRequired = "sip"
        comparisonMode = $comparisonMode
        diagnosticOnlyReason = $diagnosticOnlyReason
        feedIdentityMatch = $false
        providerResponseFeed = $null
        providerBarFeedMismatchCount = 0
        mismatchBreakdown = @{}
        tosObservationCount = $series.Count
        requestedWindowCount = 0
        providerBarCount = 0
        exactOhlcvMatchCount = 0
        mismatchCount = 0
        exactOhlcvMatch = $false
        qualificationUsableFeed = $false
        mismatches = @()
        status = "INSUFFICIENT_TOS_HISTORY"
    }

    if ($series.Count -ge 119) {
        $window = @($series | Select-Object -Last 119)
        $firstTimestamp = [DateTimeOffset]::Parse($window[0].observation.sourceTimestampUtc).ToUniversalTime()
        $lastTimestamp = [DateTimeOffset]::Parse($window[-1].observation.sourceTimestampUtc).ToUniversalTime()
        $toTimestamp = $lastTimestamp.AddMinutes(5)

        $fromQuery = Get-UtcQueryValue -Value $firstTimestamp
        $toQuery = Get-UtcQueryValue -Value $toTimestamp
        $barsUri = "$BaseUri/api/market-data/bars?symbol=$symbol&timeframe=5m&from=$fromQuery&to=$toQuery&providerName=Alpaca"
        $providerResponse = Invoke-RestMethod -Uri $barsUri -TimeoutSec 90
        Save-Json -Path (Join-Path $outputDirectory "$symbol-provider-5m-bars.json") -Value $providerResponse

        $providerByTimestamp = @{}
        foreach ($bar in @($providerResponse.bars)) {
            $key = [DateTimeOffset]::Parse($bar.timestamp).ToUniversalTime().ToString("o")
            $providerByTimestamp[$key] = $bar
        }

        $mismatches = [System.Collections.Generic.List[object]]::new()
        $matches = 0
        $providerBarFeedMismatchCount = 0
        $mismatchBreakdown = [ordered]@{
            providerBarMissing = 0
            feed = 0
            open = 0
            high = 0
            low = 0
            close = 0
            volume = 0
        }
        foreach ($stored in $window) {
            $tos = $stored.observation
            $key = [DateTimeOffset]::Parse($tos.sourceTimestampUtc).ToUniversalTime().ToString("o")
            if (-not $providerByTimestamp.ContainsKey($key)) {
                $mismatchBreakdown.providerBarMissing++
                $mismatches.Add([pscustomobject]@{ timestampUtc = $key; reason = "PROVIDER_BAR_MISSING" })
                continue
            }

            $providerBar = $providerByTimestamp[$key]
            $fields = @("open", "high", "low", "close")
            $differences = @()
            if ([string]$providerBar.feed -ne $effectiveFeed) {
                $providerBarFeedMismatchCount++
                $mismatchBreakdown.feed++
                $differences += "feed provider=$($providerBar.feed) expected=$effectiveFeed"
            }
            foreach ($field in $fields) {
                if ([decimal]$providerBar.$field -ne [decimal]$tos.$field) {
                    $mismatchBreakdown[$field]++
                    $differences += "$field provider=$($providerBar.$field) tos=$($tos.$field)"
                }
            }
            if ([decimal]$providerBar.volume -ne [decimal]$tos.volume) {
                $mismatchBreakdown.volume++
                $differences += "volume provider=$($providerBar.volume) tos=$($tos.volume)"
            }

            if ($differences.Count -eq 0) {
                $matches++
            }
            else {
                $mismatches.Add([pscustomobject]@{
                    timestampUtc = $key
                    reason = ($differences -join "; ")
                })
            }
        }

        $providerComparison.requestedWindowCount = $window.Count
        $providerComparison.providerBarCount = @($providerResponse.bars).Count
        $providerComparison.exactOhlcvMatchCount = $matches
        $providerComparison.mismatchCount = $mismatches.Count
        $providerComparison.exactOhlcvMatch = ($window.Count -eq 119 -and $matches -eq 119 -and $mismatches.Count -eq 0)
        $providerComparison.mismatches = @($mismatches)
        $providerComparison.firstTosTimestampUtc = $firstTimestamp.ToString("o")
        $providerComparison.lastTosTimestampUtc = $lastTimestamp.ToString("o")
        $providerComparison.providerResponseFeed = [string]$providerResponse.feed
        $providerComparison.providerBarFeedMismatchCount = $providerBarFeedMismatchCount
        $providerComparison.feedIdentityMatch = (
            $effectiveFeed -eq [string]$providerResponse.feed -and
            $providerBarFeedMismatchCount -eq 0
        )
        $providerComparison.mismatchBreakdown = $mismatchBreakdown
        $providerComparison.qualificationUsableFeed = (
            $effectiveFeed -eq "sip" -and
            [string]$providerResponse.feed -eq "sip" -and
            $providerBarFeedMismatchCount -eq 0
        )

        if (-not $providerComparison.feedIdentityMatch) {
            $providerComparison.status = "FEED_IDENTITY_MISMATCH_BLOCKED"
            $providerComparison.diagnosticOnlyReason = "Configured feed, response feed, or one or more row-level feed identities do not agree."
        }
        elseif (-not $providerComparison.qualificationUsableFeed) {
            if ($providerComparison.exactOhlcvMatch) {
                $providerComparison.status = "EXACT_MATCH_DIAGNOSTIC_ONLY_NON_SIP"
            }
            else {
                $providerComparison.status = "DIAGNOSTIC_MISMATCH_NON_SIP"
            }
        }
        elseif ($providerComparison.exactOhlcvMatch) {
            $providerComparison.status = "EXACT_SIP_MATCH"
        }
        else {
            $providerComparison.status = "SIP_MISMATCH_OR_MISSING_PROVIDER_BARS"
        }
    }

    Save-Json -Path (Join-Path $outputDirectory "$symbol-provider-tos-comparison.json") -Value $providerComparison

    $item = [ordered]@{
        symbol = $symbol
        parityStatus = $parity.status
        parityMeanAbsoluteError = $parity.meanAbsoluteError
        parityMaximumAbsoluteError = $parity.maximumAbsoluteError
        parityGapCount = $parity.timestampGapCount
        providerComparisonStatus = $providerComparison.status
        exactProviderTosOhlcvMatch = $providerComparison.exactOhlcvMatch
        feedIdentityMatch = $providerComparison.feedIdentityMatch
        providerResponseFeed = $providerComparison.providerResponseFeed
        effectiveFeed = $effectiveFeed
        sipCaptureStatus = "SKIPPED_NOT_CONFIGURED"
        walkForwardStatus = "NOT_RUN"
        sellLimitStatus = "NOT_RUN"
    }

    # Never attempt a SIP request while the running API is configured for IEX or another feed.
    if ($effectiveFeed -eq "sip") {
        try {
            $captureBody = @{ symbol = $symbol; providerName = "Alpaca"; feed = "sip" } | ConvertTo-Json
            $capture = Invoke-RestMethod -Method Post -Uri "$BaseUri/api/boring-overnight/capture-sell-window-history" -ContentType "application/json" -Body $captureBody -TimeoutSec 300
            Save-Json -Path (Join-Path $outputDirectory "$symbol-sip-40-session-capture.json") -Value $capture
            $item.sipCaptureStatus = if ($capture.qualificationUsable -and $capture.completeWindowCount -eq 40) { "40_OF_40_COMPLETE" } else { "INCOMPLETE_OR_NOT_QUALIFICATION_USABLE" }

            $sellLimitBody = @{
                symbol = $symbol
                providerName = "Alpaca"
                feed = "sip"
                targetQuantilePct = $TargetQuantilePct
            } | ConvertTo-Json
            $sellLimit = Invoke-RestMethod -Method Post -Uri "$BaseUri/api/boring-overnight/sell-limit" -ContentType "application/json" -Body $sellLimitBody -TimeoutSec 90
            Save-Json -Path (Join-Path $outputDirectory "$symbol-sell-limit-gates.json") -Value $sellLimit
            $item.walkForwardStatus = $sellLimit.walkForward.status
            $item.sellLimitStatus = $sellLimit.status
        }
        catch {
            $item.sipCaptureStatus = "REQUEST_FAILED"
            $item.requestError = $_.Exception.Message
            Save-Json -Path (Join-Path $outputDirectory "$symbol-sip-or-sell-limit-error.json") -Value ([pscustomobject]@{
                symbol = $symbol
                error = $_.Exception.Message
                note = "A failed provider request is not proof of entitlement. Inspect the HTTP response and account access before retrying."
            })
        }
    }
    else {
        Write-Warning "Skipping SIP 40-session capture and sell-limit qualification for $symbol. Effective Alpaca feed is '$effectiveFeed', not SIP. No SIP request was made."
    }

    $summary.Add([pscustomobject]$item)
}

Save-Json -Path (Join-Path $outputDirectory "qualification-summary.json") -Value @($summary)
Write-Host ""
Write-Host "===== Qualification summary ====="
$summary | Format-Table -AutoSize
Write-Host "Evidence saved to: $outputDirectory"
Write-Host "This script never submits orders and never treats OHLCV touches as broker fills."
