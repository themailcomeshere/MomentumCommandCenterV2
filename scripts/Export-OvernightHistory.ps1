<#
.SYNOPSIS
    Research: exports overnight-return history and weekday statistics for the symbols in a Thinkorswim
    Overnight scanner CSV (or a symbol list), and archives the scanner CSV itself.

.DESCRIPTION
    For each symbol it calls MCC's GET /api/research/overnight-history (completed Daily bars, delayed
    historical SIP when DailyFeed=sip_delayed, split-adjusted). Overnight return = next regular-session
    open / buy-session close - 1, grouped by the buy session's weekday. Missing sessions are never bridged.

    Output (outside the repository, because Alpaca's terms for storing SIP data are not yet reviewed):
      <OutRoot>\scanner-exports\<scanner file>           archived copy of the scanner CSV (daily candidate snapshot)
      <OutRoot>\overnight-history\<timestamp>\pairs.csv   every overnight pair, every symbol
      <OutRoot>\overnight-history\<timestamp>\weekday-by-symbol.csv
      <OutRoot>\overnight-history\<timestamp>\weekday-pooled.csv   all symbols combined, by weekday
      <OutRoot>\overnight-history\<timestamp>\manifest.json        source SHA, feed, missing sessions, errors

    RESEARCH ONLY. With 50 symbols x 5 weekdays, about 1 group in 20 shows |t| near 2 by chance.
    Treat weekday effects as hypotheses to confirm on later data, never as a trading signal.
    MCC must be running (scripts\MCC.ps1 start). No order is ever sent.

.EXAMPLE
    .\scripts\Export-OvernightHistory.ps1
    .\scripts\Export-OvernightHistory.ps1 -ScannerCsv "$env:USERPROFILE\OneDrive\Documents\2026-11-16-WatchListScanner_1311.csv"
    .\scripts\Export-OvernightHistory.ps1 -Symbols PRU,AFL,HQY -Days 1095
#>
[CmdletBinding()]
param(
    # Thinkorswim scanner export. Default: the newest *WatchListScanner*.csv in Documents.
    [string]$ScannerCsv,
    # Symbols to use instead of a scanner CSV.
    [string[]]$Symbols,
    # Calendar days of history (30-1460). 730 = about 2 years, about 100 nights per weekday.
    [ValidateRange(30, 1460)]
    [int]$Days = 730,
    [int]$Port = 53360,
    # Default: the MCC-Data folder next to the repository folder (D:\source\repos\MCC-Data).
    [string]$OutRoot = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) "MCC-Data"),
    # Pause between symbols to stay well under provider rate limits.
    [int]$DelayMilliseconds = 300
)

$ErrorActionPreference = "Stop"
$base = "http://localhost:$Port"

function Read-ScannerSymbols([string]$Path) {
    # TOS exports start with a title and a "Results" line; the header row begins with "Symbol,".
    $lines = Get-Content -LiteralPath $Path
    $headerIndex = -1
    for ($i = 0; $i -lt [Math]::Min($lines.Count, 25); $i++) {
        if ($lines[$i] -match '^\s*"?Symbol"?\s*,') { $headerIndex = $i; break }
    }
    if ($headerIndex -lt 0) { throw "No 'Symbol,' header row found in the first 25 lines of $Path." }
    $rows = $lines[$headerIndex..($lines.Count - 1)] | Where-Object { $_.Trim() -ne "" } | ConvertFrom-Csv
    return @($rows | ForEach-Object { ($_.Symbol -replace '"', '').Trim().ToUpperInvariant() } |
        Where-Object { $_ -match '^[A-Z0-9.\-]{1,8}$' } | Select-Object -Unique)
}

function Get-PooledStats([string]$Label, [double[]]$Values) {
    $n = $Values.Count
    if ($n -eq 0) { return [pscustomobject]@{ BuyWeekday = $Label; Count = 0; WinRatePct = $null; MeanPct = $null; MedianPct = $null; StdDevPct = $null; TStat = $null } }
    $mean = ($Values | Measure-Object -Average).Average
    $sorted = $Values | Sort-Object
    $median = if ($n % 2 -eq 0) { ($sorted[$n / 2 - 1] + $sorted[$n / 2]) / 2 } else { $sorted[[Math]::Floor($n / 2)] }
    $sd = $null; $t = $null
    if ($n -gt 1) {
        $sum = 0.0; foreach ($v in $Values) { $sum += ($v - $mean) * ($v - $mean) }
        $sd = [Math]::Sqrt($sum / ($n - 1))
        if ($sd -gt 0) { $t = $mean / ($sd / [Math]::Sqrt($n)) }
    }
    [pscustomobject]@{
        BuyWeekday = $Label; Count = $n
        WinRatePct = [Math]::Round((@($Values | Where-Object { $_ -gt 0 }).Count * 100.0 / $n), 2)
        MeanPct = [Math]::Round($mean, 4); MedianPct = [Math]::Round($median, 4)
        StdDevPct = if ($null -ne $sd) { [Math]::Round($sd, 4) } else { $null }
        TStat = if ($null -ne $t) { [Math]::Round($t, 2) } else { $null }
    }
}

# 1. Symbols
if (-not $Symbols -or $Symbols.Count -eq 0) {
    if (-not $ScannerCsv) {
        $docs = [Environment]::GetFolderPath("MyDocuments")
        $latest = Get-ChildItem -LiteralPath $docs -Filter "*WatchListScanner*.csv" -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if (-not $latest) { throw "No -ScannerCsv given and no *WatchListScanner*.csv found in $docs. Pass -ScannerCsv or -Symbols." }
        $ScannerCsv = $latest.FullName
    }
    if (-not (Test-Path -LiteralPath $ScannerCsv)) { throw "Scanner CSV not found: $ScannerCsv" }
    $Symbols = Read-ScannerSymbols $ScannerCsv
}
else {
    $Symbols = @($Symbols | ForEach-Object { $_ -split '[,\s;]+' } | Where-Object { $_ } |
        ForEach-Object { $_.Trim().ToUpperInvariant() } | Select-Object -Unique)
}
if ($Symbols.Count -eq 0) { throw "No valid symbols." }
Write-Host ("Symbols: {0} ({1})" -f $Symbols.Count, ($Symbols -join ", "))

# 2. MCC must be running and must have the research endpoint
try { $health = Invoke-RestMethod -Uri "$base/api/health" -TimeoutSec 5 }
catch { throw "MCC is not reachable at $base. Start it with .\scripts\MCC.ps1 start." }
Write-Host ("MCC source SHA: {0}" -f $health.sourceSha)

# 3. Output folders and scanner archive
$stamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmss")
$runDir = Join-Path $OutRoot ("overnight-history\" + $stamp)
New-Item -ItemType Directory -Force -Path $runDir | Out-Null
$archived = $null
if ($ScannerCsv) {
    $archiveDir = Join-Path $OutRoot "scanner-exports"
    New-Item -ItemType Directory -Force -Path $archiveDir | Out-Null
    $archived = Join-Path $archiveDir (Split-Path $ScannerCsv -Leaf)
    if (-not (Test-Path -LiteralPath $archived)) { Copy-Item -LiteralPath $ScannerCsv -Destination $archived }
}

# 4. Per-symbol history
$pairs = New-Object System.Collections.Generic.List[object]
$weekdayRows = New-Object System.Collections.Generic.List[object]
$perSymbol = New-Object System.Collections.Generic.List[object]
$errors = New-Object System.Collections.Generic.List[string]
foreach ($symbol in $Symbols) {
    try {
        $r = Invoke-RestMethod -Uri ("$base/api/research/overnight-history?symbol={0}&days={1}" -f [Uri]::EscapeDataString($symbol), $Days) -TimeoutSec 60
    }
    catch {
        $status = $null
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        if ($status -eq 404) { throw "MCC does not have /api/research/overnight-history. Restart MCC on the latest version (MCC.ps1 stop, Get-Latest-And-Test.ps1, MCC.ps1 start)." }
        $errors.Add("${symbol}: $($_.Exception.Message)"); Write-Warning "$symbol failed: $($_.Exception.Message)"; continue
    }
    foreach ($p in $r.pairs) {
        $pairs.Add([pscustomobject]@{
            Symbol = $r.symbol; BuySessionDate = $p.buySessionDate; NextSessionDate = $p.nextSessionDate
            BuyWeekday = $p.buyWeekday; CalendarGapDays = $p.calendarGapDays; BuyClose = $p.buyClose
            NextOpen = $p.nextOpen; ReturnPct = $p.returnPct; Feed = $r.feed })
    }
    foreach ($w in @($r.byWeekday) + @($r.allNights)) {
        $weekdayRows.Add([pscustomobject]@{
            Symbol = $r.symbol; BuyWeekday = $w.buyWeekday; Count = $w.count; WinRatePct = $w.winRatePct
            MeanPct = $w.meanPct; MedianPct = $w.medianPct; StdDevPct = $w.stdDevPct; TStat = $w.tStat
            Feed = $r.feed; FeedCoverage = $r.feedCoverage })
    }
    $perSymbol.Add([pscustomobject]@{
        symbol = $r.symbol; feed = $r.feed; feedCoverage = $r.feedCoverage; requestStatus = $r.requestStatus
        expectedSessions = $r.expectedSessionCount; sessions = $r.sessionCount; pairs = @($r.pairs).Count
        missingSessionDates = @($r.missingSessionDates); pairsSkippedForMissingSessions = $r.pairsSkippedForMissingSessions
        firstObservedSessionDate = $r.firstObservedSessionDate; preListingSessions = $r.preListingSessionCount })
    $listed = if ($r.preListingSessionCount -gt 0) { " (listed {0}; {1} earlier sessions not yet listed)" -f $r.firstObservedSessionDate, $r.preListingSessionCount } else { "" }
    Write-Host ("  {0,-6} {1} pairs, {2} missing sessions, feed {3}{4}" -f $r.symbol, @($r.pairs).Count, @($r.missingSessionDates).Count, $r.feed, $listed)
    Start-Sleep -Milliseconds $DelayMilliseconds
}

# 5. Pooled weekday statistics (consolidated sources only)
$consolidated = @($perSymbol | Where-Object { $_.feedCoverage -eq "CONSOLIDATED_SIP" } | ForEach-Object { $_.symbol })
$pooled = foreach ($day in "Monday", "Tuesday", "Wednesday", "Thursday", "Friday") {
    Get-PooledStats $day ([double[]]@($pairs | Where-Object { $_.BuyWeekday -eq $day -and $consolidated -contains $_.Symbol } | ForEach-Object { [double]$_.ReturnPct }))
}
$pooled = @($pooled) + (Get-PooledStats "All" ([double[]]@($pairs | Where-Object { $consolidated -contains $_.Symbol } | ForEach-Object { [double]$_.ReturnPct })))

$pairs | Export-Csv -LiteralPath (Join-Path $runDir "pairs.csv") -NoTypeInformation -Encoding UTF8
$weekdayRows | Export-Csv -LiteralPath (Join-Path $runDir "weekday-by-symbol.csv") -NoTypeInformation -Encoding UTF8
$pooled | Export-Csv -LiteralPath (Join-Path $runDir "weekday-pooled.csv") -NoTypeInformation -Encoding UTF8
[pscustomobject]@{
    createdAtUtc = (Get-Date).ToUniversalTime().ToString("o"); mccSourceSha = $health.sourceSha
    scannerCsv = $ScannerCsv; scannerArchive = $archived; days = $Days; symbolsRequested = $Symbols.Count
    symbolsSucceeded = $perSymbol.Count; consolidatedSymbols = $consolidated.Count; pairRows = $pairs.Count
    # .ToArray(): PowerShell 5.1 throws "Argument types do not match" for @(List[T]) inside a hashtable literal.
    errors = $errors.ToArray(); perSymbol = $perSymbol.ToArray()
    note = "RESEARCH ONLY. Overnight return = next open / buy close - 1 (completed Daily bars, split-adjusted). Pooled stats use consolidated (SIP) symbols only. Weekday effects are hypotheses, not signals. Data stored outside the repository until Alpaca's storage terms are reviewed."
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $runDir "manifest.json") -Encoding UTF8

Write-Host ""
Write-Host "Pooled weekday statistics (consolidated symbols only):"
$pooled | Format-Table BuyWeekday, Count, WinRatePct, MeanPct, MedianPct, StdDevPct, TStat -AutoSize | Out-String | Write-Host
Write-Host ("Written to {0}" -f $runDir)
if ($errors.Count -gt 0) { Write-Warning ("{0} symbol(s) failed; see manifest.json." -f $errors.Count) }
