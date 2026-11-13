<#
.SYNOPSIS
    Writes an immutable per-release evidence bundle for the current commit.

.DESCRIPTION
    Collects, for the exact HEAD SHA:
      - branch, SHA, clean/dirty working tree
      - GitHub Actions run for that SHA (via gh, if installed and authenticated)
      - local Release test totals (runs dotnet test --no-build unless -SkipTests)
      - running API process SHA from /api/health (if the API is up)
      - operator-supplied evidence files (browser diagnostics, TOS compile record, data provenance,
        backup manifest), each recorded with its SHA-256
      - open blockers you pass with -Blocker
    Output: docs/Validation/Release-Evidence/<sha>.json and .md. An existing bundle for the same SHA is
    never overwritten; a new numbered bundle is written instead.
    Every gate is reported separately. A missing gate is NOT_PROVIDED, never PASS.

.EXAMPLE
    .\scripts\New-ReleaseEvidence.ps1
    .\scripts\New-ReleaseEvidence.ps1 -BrowserDiagnostics .\artifacts\cdp.json -Blocker "SIP entitlement 403"
#>
[CmdletBinding()]
param(
    [string]$BrowserDiagnostics,
    [string]$TosCompileRecord,
    [string]$DataProvenance,
    [string]$BackupManifest,
    [string[]]$Blocker = @(),
    [int]$Port = 53360,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    $sha = (git rev-parse HEAD).Trim()
    $branch = (git branch --show-current).Trim()
    $dirty = @(git status --porcelain).Count -gt 0

    # GitHub Actions for this exact SHA.
    $ci = [ordered]@{ status = "NOT_PROVIDED"; detail = "gh CLI unavailable or not authenticated" }
    if (Get-Command gh -ErrorAction SilentlyContinue) {
        try {
            $runs = gh run list --commit $sha --json databaseId,status,conclusion,url,workflowName 2>$null | ConvertFrom-Json
            if ($runs -and @($runs).Count -gt 0) {
                $run = @($runs)[0]
                $ci = [ordered]@{
                    status = if ($run.status -ne "completed") { "PENDING" } elseif ($run.conclusion -eq "success") { "PASS" } else { "FAIL" }
                    runId = $run.databaseId; conclusion = $run.conclusion; url = $run.url; workflow = $run.workflowName
                }
            }
            else { $ci = [ordered]@{ status = "NOT_FOUND"; detail = "No workflow run found for $sha (not pushed yet?)" } }
        }
        catch { $ci = [ordered]@{ status = "UNKNOWN"; detail = $_.Exception.Message } }
    }

    # Local Release tests.
    $tests = [ordered]@{ status = "NOT_PROVIDED"; detail = "Skipped (-SkipTests)" }
    if (-not $SkipTests) {
        $output = & dotnet test .\MomentumCommandCenterV2.sln -c Release --no-build 2>&1 | Out-String
        $match = [regex]::Match($output, "Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)")
        if ($match.Success) {
            $failed = [int]$match.Groups[1].Value
            $tests = [ordered]@{
                status = if ($failed -eq 0 -and $LASTEXITCODE -eq 0) { "PASS" } else { "FAIL" }
                failed = $failed; passed = [int]$match.Groups[2].Value; skipped = [int]$match.Groups[3].Value; total = [int]$match.Groups[4].Value
                note = "Ran against the existing Release build (--no-build). Build first with Get-Latest-And-Test.ps1."
            }
        }
        else { $tests = [ordered]@{ status = "FAIL"; detail = "Could not parse test totals; exit code $LASTEXITCODE" } }
    }

    # Running API process.
    $runtime = [ordered]@{ status = "NOT_RUNNING" }
    try {
        $health = Invoke-RestMethod -Uri "http://localhost:$Port/api/health" -TimeoutSec 3
        $runtime = [ordered]@{
            status = if ($health.sourceSha -eq $sha) { "MATCHES_HEAD" } elseif ($health.sourceSha -like "$sha+DIRTY") { "HEAD_PLUS_LOCAL_CHANGES" } else { "DIFFERENT_OR_UNKNOWN" }
            reportedSha = $health.sourceSha; processId = $health.processId; startedAtUtc = $health.processStartedAtUtc; launchedBy = $health.launchedBy
        }
    }
    catch { }

    function Get-EvidenceFile([string]$path) {
        if ([string]::IsNullOrWhiteSpace($path)) { return [ordered]@{ status = "NOT_PROVIDED" } }
        if (-not (Test-Path $path)) { return [ordered]@{ status = "MISSING_FILE"; path = $path } }
        $item = Get-Item $path
        return [ordered]@{ status = "PROVIDED"; path = $item.FullName; bytes = $item.Length; sha256 = (Get-FileHash $item.FullName -Algorithm SHA256).Hash; note = "Provided by operator; content not validated by this script." }
    }

    $bundle = [ordered]@{
        bundleVersion   = 1
        createdAtUtc    = (Get-Date).ToUniversalTime().ToString("o")
        branch          = $branch
        sha             = $sha
        workingTree     = if ($dirty) { "DIRTY" } else { "CLEAN" }
        gates           = [ordered]@{
            exactHeadCi        = $ci
            localReleaseTests  = $tests
            apiRuntimeSha      = $runtime
            browserAcceptance  = Get-EvidenceFile $BrowserDiagnostics
            thinkorswimCompile = Get-EvidenceFile $TosCompileRecord
            dataProvenance     = Get-EvidenceFile $DataProvenance
            backupRestore      = Get-EvidenceFile $BackupManifest
        }
        blockers        = $Blocker
        rule            = "Each gate is independent. NOT_PROVIDED is not PASS. Operator, Thinkorswim and data-source gates cannot be closed by CI or tests."
    }

    $outDir = Join-Path $repoRoot "docs\Validation\Release-Evidence"
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $base = Join-Path $outDir $sha
    $suffix = 1
    while (Test-Path "$base.json") { $suffix++; $base = Join-Path $outDir "$sha-$suffix" }
    $bundle | ConvertTo-Json -Depth 6 | Set-Content "$base.json" -Encoding utf8

    $lines = @("# Release evidence - $sha", "", "- Branch: ``$branch``", "- Working tree: $($bundle.workingTree)", "- Created (UTC): $($bundle.createdAtUtc)", "", "| Gate | Status | Detail |", "|---|---|---|")
    foreach ($gate in $bundle.gates.GetEnumerator()) {
        $detail = ($gate.Value.GetEnumerator() | Where-Object Key -ne "status" | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join "; "
        $lines += "| $($gate.Key) | $($gate.Value.status) | $detail |"
    }
    $lines += ""
    $lines += "## Blockers"
    $lines += if ($Blocker.Count) { $Blocker | ForEach-Object { "- $_" } } else { "- None supplied (this does not mean none exist; see docs/Master-To-Do.md)." }
    $lines += ""
    $lines += "_$($bundle.rule)_"
    $lines | Set-Content "$base.md" -Encoding utf8

    Write-Host "Release evidence written: $base.json / .md" -ForegroundColor Green
    foreach ($gate in $bundle.gates.GetEnumerator()) { Write-Host ("  {0,-20} {1}" -f $gate.Key, $gate.Value.status) }
}
finally { Pop-Location }
exit 0
