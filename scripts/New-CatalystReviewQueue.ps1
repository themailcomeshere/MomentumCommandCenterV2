<#
.SYNOPSIS
    Builds a catalyst human-review worksheet (CSV) from MCC's Alpaca news triage for many symbols at once.

.DESCRIPTION
    For each symbol, calls GET /api/market-data/catalyst-assessment and writes one row per article with the
    saved triage prediction (severity, categories, ruleset version). Articles already reviewed for that
    symbol (GET /api/market-data/catalyst-reviews) are skipped. The Label and Notes columns are left blank
    for the reviewer. Nothing is submitted; use Submit-CatalystReviews.ps1 after filling the sheet.

    A symbol with zero articles produces no row: zero news is UNKNOWN, not a labelable article.

.EXAMPLE
    .\scripts\New-CatalystReviewQueue.ps1 -Symbols CRBG,FG,HDB,INGM
    .\scripts\New-CatalystReviewQueue.ps1 -SymbolFile .\my-symbols.txt
#>
[CmdletBinding()]
param(
    [string[]]$Symbols,
    # Text file with one symbol per line (or comma/space separated).
    [string]$SymbolFile,
    [int]$Port = 53360,
    [string]$OutFile = (Join-Path ([Environment]::GetFolderPath("MyDocuments")) ("MCC-Reviews\catalyst-queue-" + (Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmss") + ".csv")),
    # Pause between symbols to stay well under provider rate limits.
    [int]$DelayMilliseconds = 400,

    # News lookback per symbol (1-720 hours). The live candidate check uses 24; samples need longer.
    [ValidateRange(1, 720)]
    [int]$LookbackHours = 336,

    # Stratified sample size (0 = keep every article). Selection: every RISK article first, then MATERIAL and
    # UNCLASSIFIED alternately, round-robin across symbols, newest first, at most MaxPerSymbol per symbol.
    [int]$SampleSize = 60,
    [int]$MaxPerSymbol = 3
)

$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:$Port"

$list = @()
if ($Symbols) { $list += $Symbols }
if ($SymbolFile) { $list += (Get-Content $SymbolFile -Raw) -split '[\s,;]+' }
$list = @($list | Where-Object { $_ } | ForEach-Object { $_.Trim().ToUpperInvariant() } | Sort-Object -Unique)
if ($list.Count -eq 0) { Write-Host "Provide -Symbols or -SymbolFile." -ForegroundColor Red; exit 1 }

try { Invoke-RestMethod "$baseUrl/api/health" -TimeoutSec 3 | Out-Null }
catch { Write-Host "MCC API is not running on port $Port. Run .\scripts\MCC.ps1 start first." -ForegroundColor Red; exit 1 }

$rows = New-Object System.Collections.Generic.List[object]
$summary = New-Object System.Collections.Generic.List[string]
foreach ($symbol in $list) {
    try {
        $assessment = Invoke-RestMethod "$baseUrl/api/market-data/catalyst-assessment?symbol=$([uri]::EscapeDataString($symbol))&lookbackHours=$LookbackHours" -TimeoutSec 60
        $reviewed = @{}
        try {
            $existing = Invoke-RestMethod "$baseUrl/api/market-data/catalyst-reviews?symbol=$([uri]::EscapeDataString($symbol))&limit=500" -TimeoutSec 30
            foreach ($r in @($existing.records)) { if ($r.articleId) { $reviewed[[string]$r.articleId] = $true } }
        } catch { }
        $added = 0; $skipped = 0
        foreach ($item in @($assessment.articles)) {
            $article = $item.article
            if ($article.id -and $reviewed.ContainsKey([string]$article.id)) { $skipped++; continue }
            $rows.Add([pscustomobject]@{
                Symbol              = $symbol
                ArticleId           = $article.id
                PublishedAtUtc      = $article.publishedAtUtc
                Headline            = $article.headline
                Url                 = $article.url
                Source              = "Alpaca News"
                PredictedSeverity   = $item.severity
                PredictedCategories = (@($item.categories) -join ";")
                RuleSetVersion      = $assessment.ruleSetVersion
                Label               = ""
                Notes               = ""
            })
            $added++
        }
        $summary.Add("$symbol : $($assessment.articleCount) articles, $added queued, $skipped already reviewed, status $($assessment.status)")
    }
    catch {
        $summary.Add("$symbol : ERROR $($_.Exception.Message)")
    }
    Start-Sleep -Milliseconds $DelayMilliseconds
}

$candidateCount = $rows.Count
if ($SampleSize -gt 0 -and $rows.Count -gt $SampleSize) {
    # Deterministic stratified selection; nothing random, so the same inputs give the same sheet.
    $picked = New-Object System.Collections.Generic.List[object]
    $perSymbol = @{}
    $take = {
        param($row)
        $n = if ($perSymbol.ContainsKey($row.Symbol)) { $perSymbol[$row.Symbol] } else { 0 }
        if ($n -ge $MaxPerSymbol -or $picked.Count -ge $SampleSize) { return }
        $perSymbol[$row.Symbol] = $n + 1
        $picked.Add($row)
    }
    $ordered = @($rows | Sort-Object Symbol, @{ Expression = { $_.PublishedAtUtc }; Descending = $true })
    foreach ($row in @($ordered | Where-Object { $_.PredictedSeverity -eq "RISK" })) { & $take $row }
    $queues = @{}
    foreach ($sev in "MATERIAL", "UNCLASSIFIED") {
        $queues[$sev] = @{}
        foreach ($g in @($ordered | Where-Object { $_.PredictedSeverity -eq $sev } | Group-Object Symbol)) {
            $queues[$sev][$g.Name] = New-Object System.Collections.Generic.Queue[object] (, @($g.Group))
        }
    }
    # Round-robin over symbols; each pick goes to whichever of MATERIAL / UNCLASSIFIED has fewer picks so far,
    # so the predicted-positive and predicted-negative sides stay balanced when both are available.
    $sevCount = @{ MATERIAL = 0; UNCLASSIFIED = 0 }
    $allSymbols = @(@($queues["MATERIAL"].Keys) + @($queues["UNCLASSIFIED"].Keys) | Sort-Object -Unique)
    $progress = $true
    while ($picked.Count -lt $SampleSize -and $progress) {
        $progress = $false
        foreach ($symbolKey in $allSymbols) {
            $order = if ($sevCount["MATERIAL"] -le $sevCount["UNCLASSIFIED"]) { "MATERIAL", "UNCLASSIFIED" } else { "UNCLASSIFIED", "MATERIAL" }
            foreach ($sev in $order) {
                $queue = $queues[$sev][$symbolKey]
                if ($null -eq $queue -or $queue.Count -eq 0) { continue }
                $before = $picked.Count
                & $take ($queue.Dequeue())
                if ($picked.Count -gt $before) { $sevCount[$sev]++; $progress = $true; break }
                $queue.Clear()   # symbol is at its cap
            }
            if ($picked.Count -ge $SampleSize) { break }
        }
    }
    $rows = $picked
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutFile) | Out-Null
$rows | Export-Csv -Path $OutFile -NoTypeInformation -Encoding UTF8
Write-Host "Candidates: $candidateCount article(s); sheet: $($rows.Count) (SampleSize $SampleSize, MaxPerSymbol $MaxPerSymbol)."
$summary | ForEach-Object { Write-Host $_ }
Write-Host ""
Write-Host "Queue written: $OutFile ($($rows.Count) articles)" -ForegroundColor Green
Write-Host "Fill Label with MATERIAL_RISK, MATERIAL_EVENT, NON_MATERIAL, IRRELEVANT, DUPLICATE or UNCERTAIN,"
Write-Host "and Notes with at least 40 characters of article-specific facts. Leave Label blank to skip a row."
Write-Host "Then: .\scripts\Submit-CatalystReviews.ps1 -Path `"$OutFile`"   (dry run; add -Submit to save)"
exit 0
