<#
.SYNOPSIS
    Validates and (with -Submit) appends the labelled rows of a catalyst review worksheet to MCC.

.DESCRIPTION
    Reads a CSV made by New-CatalystReviewQueue.ps1. Rows with a blank Label are skipped. Without -Submit
    this is a dry run: it checks labels and note length locally and prints what would be sent. With -Submit
    it POSTs each row to /api/market-data/catalyst-reviews. The review store is append-only: a submitted
    review cannot be edited, only superseded by a new review. The API applies its own rationale checks and
    may reject a row; rejected rows are listed with the server's reason.

.EXAMPLE
    .\scripts\Submit-CatalystReviews.ps1 -Path "$env:USERPROFILE\Documents\MCC-Reviews\catalyst-queue-....csv"
    .\scripts\Submit-CatalystReviews.ps1 -Path "...csv" -Submit
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Path,
    [switch]$Submit,
    [int]$Port = 53360
)

$ErrorActionPreference = "Stop"
$allowed = "MATERIAL_RISK", "MATERIAL_EVENT", "NON_MATERIAL", "IRRELEVANT", "DUPLICATE", "UNCERTAIN"
$rows = @(Import-Csv $Path)
$labelled = @($rows | Where-Object { $_.Label -and $_.Label.Trim() })
Write-Host "$($rows.Count) rows, $($labelled.Count) labelled."

$problems = 0
foreach ($row in $labelled) {
    $label = $row.Label.Trim().ToUpperInvariant()
    $notes = ([string]$row.Notes).Trim()
    $issue = $null
    if ($allowed -notcontains $label) { $issue = "label '$($row.Label)' is not one of $($allowed -join ', ')" }
    elseif ($notes.Length -lt 40) { $issue = "notes are $($notes.Length) characters; at least 40 article-specific characters are required" }
    if ($issue) { $problems++; Write-Host "  FIX  $($row.Symbol) $($row.ArticleId): $issue" -ForegroundColor Yellow }
}
if ($problems -gt 0) { Write-Host "$problems row(s) need fixing. Nothing was submitted." -ForegroundColor Red; exit 1 }
if (-not $Submit) {
    Write-Host "Dry run OK. $($labelled.Count) review(s) would be submitted. Re-run with -Submit to save them (append-only)." -ForegroundColor Green
    exit 0
}

$baseUrl = "http://localhost:$Port"
try { Invoke-RestMethod "$baseUrl/api/health" -TimeoutSec 3 | Out-Null }
catch { Write-Host "MCC API is not running on port $Port. Run .\scripts\MCC.ps1 start first." -ForegroundColor Red; exit 1 }

$ok = 0; $rejected = 0
foreach ($row in $labelled) {
    $categories = @(([string]$row.PredictedCategories) -split ';' | Where-Object { $_ })
    $body = [ordered]@{
        symbol              = $row.Symbol
        articleId           = $row.ArticleId
        headline            = $row.Headline
        url                 = $row.Url
        publishedAtUtc      = $row.PublishedAtUtc
        source              = $row.Source
        label               = $row.Label.Trim().ToUpperInvariant()
        ruleSetVersion      = $row.RuleSetVersion
        notes               = $row.Notes.Trim()
        predictedSeverity   = $row.PredictedSeverity
        predictedCategories = $categories
    } | ConvertTo-Json -Depth 3
    try {
        $result = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/market-data/catalyst-reviews" -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 30
        $ok++; Write-Host "  SAVED    $($row.Symbol) $($row.ArticleId) -> $($result.reviewId)"
    }
    catch {
        $rejected++
        $reason = if ($_.ErrorDetails.Message) { $_.ErrorDetails.Message } else { $_.Exception.Message }
        Write-Host "  REJECTED $($row.Symbol) $($row.ArticleId): $reason" -ForegroundColor Yellow
    }
}
Write-Host "$ok saved, $rejected rejected."
Write-Host "Check progress: Invoke-RestMethod http://localhost:$Port/api/market-data/catalyst-reviews/validation"
exit 0
