[CmdletBinding()]
param(
    [string]$BaseUri = "http://localhost:53360",
    [string[]]$Symbols = @("RTO", "EVR"),
    [string]$ReferenceDirectory,
    [string]$ReferenceProvider,
    [string]$ReferenceAdjustment = "split-adjusted",
    [switch]$ConfirmWindowAdjustmentEquivalence,
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
$BaseUri = $BaseUri.TrimEnd("/")
$runId = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ")
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
    $OutputDirectory = Join-Path $repoRoot "artifacts/daily-data-qualification/$runId"
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

function Assert-Near([double]$Actual, [double]$Expected, [double]$Tolerance, [string]$Label) {
    if ([math]::Abs($Actual - $Expected) -gt $Tolerance) {
        throw "$Label mismatch. API=$Actual independently recalculated=$Expected tolerance=$Tolerance"
    }
}

$health = Invoke-RestMethod -Uri "$BaseUri/api/health" -Method Get
if ($health.status -ne "OK") { throw "API health is not OK." }
$providerStatus = Invoke-RestMethod -Uri "$BaseUri/api/providers/status" -Method Get
$alpaca = @($providerStatus | Where-Object { $_.provider -eq "Alpaca" } | Select-Object -First 1)
$configuredFeed = if ($alpaca.Count -gt 0) { [string]$alpaca[0].configuredFeed } else { "UNKNOWN" }

$results = @()
foreach ($symbolInput in $Symbols) {
    $symbol = $symbolInput.Trim().ToUpperInvariant()
    if ($symbol -notmatch '^[A-Z0-9.-]{1,8}$') { throw "Invalid symbol '$symbolInput'." }
    $uri = "$BaseUri/api/market-data/daily?symbol=$([uri]::EscapeDataString($symbol))"
    $payload = Invoke-RestMethod -Uri $uri -Method Get
    $rawPath = Join-Path $OutputDirectory "$symbol-daily-api.json"
    $payload | ConvertTo-Json -Depth 30 | Set-Content -Path $rawPath -Encoding UTF8

    $expected = @($payload.expectedSessionDates | ForEach-Object { ([string]$_).Substring(0,10) })
    $missing = @($payload.missingSessionDates | ForEach-Object { ([string]$_).Substring(0,10) })
    $sessions = @($payload.sessions)
    $sessionDates = @($sessions | ForEach-Object { ([string]$_.sessionDate).Substring(0,10) })
    $distinctDates = @($sessionDates | Sort-Object -Unique)
    $internalErrors = @()

    if ($expected.Count -ne 20) { $internalErrors += "ExpectedSessionDates count is $($expected.Count), not 20." }
    if ($ConfirmWindowAdjustmentEquivalence -and
        (($expected.Count -ne 20) -or $expected[0] -ne "2026-10-12" -or $expected[-1] -ne "2026-11-06")) {
        $internalErrors += "The split-only/all-adjusted equivalence switch is authorized only for the 2026-10-12 through 2026-11-06 20-session sample."
    }
    if ($sessions.Count -ne 20) { $internalErrors += "Returned session count is $($sessions.Count), not 20." }
    if ($missing.Count -ne 0) { $internalErrors += "Missing expected sessions: $($missing -join ', ')." }
    if ($distinctDates.Count -ne 20) { $internalErrors += "Session dates are not 20 distinct dates." }
    if (($sessionDates -join "|") -ne ($expected -join "|")) { $internalErrors += "Returned sessions do not exactly match ordered expected session dates." }
    if ($payload.validSessionCount -ne 20) { $internalErrors += "validSessionCount is $($payload.validSessionCount), not 20." }
    if ($payload.adjustment -ne "all (split/dividend adjusted)") { $internalErrors += "Unexpected adjustment basis '$($payload.adjustment)'." }

    $recalculatedSma = $null
    $recalculatedVolume = $null
    $recalculatedClose = $null
    if ($sessions.Count -eq 20) {
        $recalculatedSma = (@($sessions | ForEach-Object { [double]$_.close } | Measure-Object -Average).Average)
        $recalculatedVolume = (@($sessions | ForEach-Object { [double]$_.volume } | Measure-Object -Average).Average)
        $recalculatedClose = [double]$sessions[-1].close
        foreach ($row in $sessions) {
            if ([double]$row.high -lt [double]$row.low -or [double]$row.high -lt [double]$row.open -or [double]$row.high -lt [double]$row.close -or
                [double]$row.low -gt [double]$row.open -or [double]$row.low -gt [double]$row.close -or [long]$row.volume -lt 0) {
                $internalErrors += "Invalid OHLCV bounds on $($row.sessionDate)."
            }
        }
        if ($null -ne $payload.observedSma20) {
            try { Assert-Near ([double]$payload.observedSma20) $recalculatedSma 0.00000001 "Observed SMA20" }
            catch { $internalErrors += $_.Exception.Message }
        } else { $internalErrors += "ObservedSma20 is null despite 20 returned sessions." }
        if ($null -ne $payload.observedAverageDailyVolume20) {
            try { Assert-Near ([double]$payload.observedAverageDailyVolume20) $recalculatedVolume 0.00000001 "Observed average daily volume" }
            catch { $internalErrors += $_.Exception.Message }
        } else { $internalErrors += "ObservedAverageDailyVolume20 is null despite 20 returned sessions." }
        if ($null -ne $payload.observedClose) {
            try { Assert-Near ([double]$payload.observedClose) $recalculatedClose 0.00000001 "Observed close" }
            catch { $internalErrors += $_.Exception.Message }
        } else { $internalErrors += "ObservedClose is null despite 20 returned sessions." }
    }

    $feed = [string]$payload.feed
    if ($payload.provider -ne "Alpaca") { $internalErrors += "Unexpected Daily provider '$($payload.provider)'." }
    if ($feed -ne $configuredFeed) { $internalErrors += "Daily response feed '$feed' does not match configured provider feed '$configuredFeed'." }
    if ([string]::IsNullOrWhiteSpace([string]$payload.retrievedAtUtc)) { $internalErrors += "retrievedAtUtc is missing." }
    if ($null -eq $payload.latestCompletedSessionTimestampUtc) { $internalErrors += "Latest completed session timestamp is missing." }
    if ($feed -eq "sip") {
        if ($payload.qualificationUsable -ne $true) { $internalErrors += "SIP feed but qualificationUsable is not true despite a complete/fresh 20-session response." }
        foreach ($pair in @(
            @{ Field = "price"; Observed = "observedClose" },
            @{ Field = "sma20"; Observed = "observedSma20" },
            @{ Field = "averageDailyVolume20"; Observed = "observedAverageDailyVolume20" }
        )) {
            $qualificationValue = $payload.PSObject.Properties[$pair.Field].Value
            $observedValue = $payload.PSObject.Properties[$pair.Observed].Value
            if ($null -eq $qualificationValue -or $null -eq $observedValue) {
                $internalErrors += "$($pair.Field) qualification value is null on SIP."
            } else {
                try { Assert-Near ([double]$qualificationValue) ([double]$observedValue) 0.00000001 $pair.Field }
                catch { $internalErrors += $_.Exception.Message }
            }
        }
    } else {
        if ($payload.qualificationUsable -ne $false) { $internalErrors += "Non-SIP feed must never be qualificationUsable." }
        foreach ($field in @("price","sma20","averageDailyVolume20")) {
            if ($null -ne $payload.$field) { $internalErrors += "Non-SIP response exposed qualification field '$field'." }
        }
    }
    if ($payload.dataFresh -ne $true) { $internalErrors += "Latest expected completed session is not fresh." }

    $independentStatus = "NOT_RUN_REFERENCE_CSV_NOT_PROVIDED"
    $independentMismatches = @()
    $referencePath = if ([string]::IsNullOrWhiteSpace($ReferenceDirectory)) { $null } else { Join-Path $ReferenceDirectory "$symbol.csv" }
    if ($referencePath -and (Test-Path $referencePath)) {
        if ([string]::IsNullOrWhiteSpace($ReferenceProvider)) {
            $independentMismatches += "ReferenceProvider is required to identify the independent source."
        } else {
            Copy-Item -LiteralPath $referencePath -Destination (Join-Path $OutputDirectory "$symbol-independent-reference.csv") -Force
        $referenceRows = @(Import-Csv -Path $referencePath)
            $requiredColumns = @("SessionDate","Open","High","Low","Close","Volume","Adjustment")
            if ($referenceRows.Count -ne 20) { $independentMismatches += "Reference CSV has $($referenceRows.Count) rows, expected 20." }
            if ($referenceRows.Count -gt 0) {
                foreach ($column in $requiredColumns) {
                    if (-not ($referenceRows[0].PSObject.Properties.Name -contains $column)) {
                        $independentMismatches += "Reference CSV is missing column '$column'."
                    }
                }
            }
            if ($independentMismatches.Count -eq 0) {
                $refDates = @($referenceRows | ForEach-Object { ([string]$_.SessionDate).Substring(0,10) })
                if (@($refDates | Sort-Object -Unique).Count -ne 20) { $independentMismatches += "Reference CSV has duplicate session dates." }
                if (($refDates -join "|") -ne ($expected -join "|")) { $independentMismatches += "Reference dates do not exactly match the API's 20 expected sessions." }
                foreach ($ref in $referenceRows) {
                    $sameAdjustment = $ref.Adjustment -eq $payload.adjustment
                    $windowEquivalentAdjustment = $ConfirmWindowAdjustmentEquivalence -and
                        $ref.Adjustment -eq "split-adjusted" -and
                        $payload.adjustment -eq "all (split/dividend adjusted)"
                    if ($ref.Adjustment -ne $ReferenceAdjustment -or (-not $sameAdjustment -and -not $windowEquivalentAdjustment)) {
                        $independentMismatches += "Adjustment basis mismatch on $($ref.SessionDate): reference='$($ref.Adjustment)' API='$($payload.adjustment)'."
                        continue
                    }
                    $apiRow = @($sessions | Where-Object { ([string]$_.sessionDate).Substring(0,10) -eq ([string]$ref.SessionDate).Substring(0,10) }) | Select-Object -First 1
                    if (-not $apiRow) { $independentMismatches += "No API session for reference date $($ref.SessionDate)."; continue }
                    foreach ($field in @("Open","High","Low","Close")) {
                        try {
                            $apiValue = $apiRow.PSObject.Properties[$field.ToLowerInvariant()].Value
                            $referenceValue = $ref.PSObject.Properties[$field].Value
                            Assert-Near ([double]$apiValue) ([double]$referenceValue) 0.0001 "$symbol $($ref.SessionDate) $field"
                        } catch { $independentMismatches += $_.Exception.Message }
                    }
                    if ([long]$apiRow.volume -ne [long]$ref.Volume) {
                        $independentMismatches += "$symbol $($ref.SessionDate) Volume mismatch: API=$($apiRow.volume) reference=$($ref.Volume)."
                    }
                }
            }
        }
        $independentStatus = if ($independentMismatches.Count -eq 0) { "PASS_EXACT_DATES_OHLCV_WITHIN_PRICE_TOLERANCE" } else { "FAIL" }
    } elseif ($referencePath) {
        $independentStatus = "REFERENCE_FILE_NOT_FOUND"
        $independentMismatches += "Expected reference file $referencePath."
    }

    $result = [ordered]@{
        symbol = $symbol
        provider = $payload.provider
        feed = $feed
        configuredFeed = $configuredFeed
        adjustment = $payload.adjustment
        retrievedAtUtc = $payload.retrievedAtUtc
        latestExpectedSessionDate = $payload.latestExpectedSessionDate
        latestCompletedSessionTimestampUtc = $payload.latestCompletedSessionTimestampUtc
        expectedSessionCount = $expected.Count
        returnedSessionCount = $sessions.Count
        missingSessionDates = $missing
        dataFresh = $payload.dataFresh
        observedClose = $payload.observedClose
        recalculatedClose = $recalculatedClose
        observedSma20 = $payload.observedSma20
        recalculatedSma20 = $recalculatedSma
        observedAverageDailyVolume20 = $payload.observedAverageDailyVolume20
        recalculatedAverageDailyVolume20 = $recalculatedVolume
        qualificationUsable = $payload.qualificationUsable
        internalStatus = if ($internalErrors.Count -eq 0) { "PASS_INTERNAL_CONSISTENCY" } else { "FAIL" }
        internalErrors = $internalErrors
        independentProvider = $ReferenceProvider
        independentComparisonStatus = $independentStatus
        independentMismatches = $independentMismatches
        rawResponseFile = $rawPath
    }
    $results += $result
    $result | ConvertTo-Json -Depth 20 | Set-Content -Path (Join-Path $OutputDirectory "$symbol-summary.json") -Encoding UTF8
    Write-Host ("{0}: internal={1}; independent={2}; feed={3}; sessions={4}/20; missing={5}" -f
        $symbol,$result.internalStatus,$result.independentComparisonStatus,$feed,$sessions.Count,$missing.Count)
}

$overall = [ordered]@{
    runId = $runId
    baseUri = $BaseUri
    apiHealth = $health
    configuredAlpacaFeed = $configuredFeed
    referenceProvider = $ReferenceProvider
    referenceAdjustment = $ReferenceAdjustment
    windowAdjustmentEquivalenceConfirmed = [bool]$ConfirmWindowAdjustmentEquivalence
    symbols = $results
    note = "Internal arithmetic consistency is not independent market-data parity. A separate source CSV is required for independent OHLCV verification. Non-SIP values are diagnostic only."
}
$overall | ConvertTo-Json -Depth 30 | Set-Content -Path (Join-Path $OutputDirectory "daily-qualification-summary.json") -Encoding UTF8
Write-Host ""
Write-Host "Evidence saved to: $OutputDirectory"
if (@($results | Where-Object { $_.internalStatus -ne "PASS_INTERNAL_CONSISTENCY" }).Count -gt 0) {
    throw "One or more Daily API responses failed internal consistency checks. Review the evidence directory."
}
if (@($results | Where-Object { $_.independentComparisonStatus -ne "PASS_EXACT_DATES_OHLCV_WITHIN_PRICE_TOLERANCE" }).Count -gt 0) {
    Write-Warning "Independent-source parity is NOT VERIFIED. Internal consistency is not an independent validation."
}
