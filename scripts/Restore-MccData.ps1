<#
.SYNOPSIS
    Verifies an MCC backup and restores it, staged by default.

.DESCRIPTION
    Default (staged): verifies every SHA-256 in manifest.json, then copies the files into a separate
    folder (-StageTo, default <backup>\staged-restore-<timestamp>) without touching live data. Point the
    API at the staged copies with the MCC_*_PATH overrides to inspect them before going live.

    -ToLive: requires the API to be stopped. Each live file that would be replaced is first moved into
    <data root>\pre-restore-<timestamp>, then the backup copy is written to the path the inventory
    resolves now (honoring MCC_*_PATH overrides) and re-hashed. Nothing is deleted.

.EXAMPLE
    .\scripts\Restore-MccData.ps1 -BackupPath "$HOME\Documents\MCC-Backups\mcc-backup-20261113-180000"
    .\scripts\Restore-MccData.ps1 -BackupPath <folder> -ToLive
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BackupPath,
    [string]$StageTo,
    [switch]$ToLive,
    [int]$Port = 53360
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\MccDataInventory.ps1")
$repoRoot = Split-Path -Parent $PSScriptRoot

$manifestPath = Join-Path $BackupPath "manifest.json"
if (-not (Test-Path $manifestPath)) { Write-Host "manifest.json not found in $BackupPath" -ForegroundColor Red; exit 2 }
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$copied = @($manifest.files | Where-Object { $_.status -eq "COPIED" })

# 1. Integrity check before anything is written.
$failures = @()
foreach ($file in $copied) {
    $path = Join-Path $BackupPath $file.backupFile
    if (-not (Test-Path $path)) { $failures += "$($file.name): backup file missing"; continue }
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($hash -ne $file.sha256) { $failures += "$($file.name): SHA-256 mismatch" }
}
if ($failures.Count -gt 0) {
    Write-Host "INTEGRITY CHECK FAILED - nothing was restored:" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 3
}
Write-Host "Integrity check passed for $($copied.Count) files (backup $($manifest.createdAtUtc), source $($manifest.sourceSha))." -ForegroundColor Green
$partial = @($copied | Where-Object { $_.partialLastLine })
if ($partial.Count -gt 0) { Write-Host "WARNING: backup flagged partial last line in: $($partial.name -join ', ')" -ForegroundColor Yellow }

$stamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmss")
if (-not $ToLive) {
    if (-not $StageTo) { $StageTo = Join-Path $BackupPath "staged-restore-$stamp" }
    New-Item -ItemType Directory -Force -Path $StageTo | Out-Null
    foreach ($file in $copied) {
        $destination = Join-Path $StageTo (Split-Path $file.backupFile -Leaf)
        Copy-Item -LiteralPath (Join-Path $BackupPath $file.backupFile) -Destination $destination
        if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $file.sha256) { throw "Staged copy hash mismatch: $destination" }
        Write-Host "  staged $($file.name) -> $destination"
    }
    Write-Host "Staged restore complete: $StageTo (live data untouched). Use -ToLive to replace live data." -ForegroundColor Green
    exit 0
}

if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) {
    Write-Host "The MCC API is listening on port $Port. Stop it first (.\scripts\MCC.ps1 stop); nothing was restored." -ForegroundColor Red
    exit 4
}

$inventory = @{}
foreach ($item in Get-MccDataInventory -RepoRoot $repoRoot) { $inventory[$item.Name] = $item }
$preRestore = Join-Path (Join-Path $env:LOCALAPPDATA "MomentumCommandCenterV2") "pre-restore-$stamp"
foreach ($file in $copied) {
    $item = $inventory[$file.name]
    if ($null -eq $item) { Write-Host "  skipped $($file.name): not in the current inventory" -ForegroundColor Yellow; continue }
    if (Test-Path $item.Path) {
        New-Item -ItemType Directory -Force -Path $preRestore | Out-Null
        Move-Item -LiteralPath $item.Path -Destination (Join-Path $preRestore (Split-Path $item.Path -Leaf))
    }
    New-Item -ItemType Directory -Force -Path (Split-Path $item.Path -Parent) | Out-Null
    Copy-Item -LiteralPath (Join-Path $BackupPath $file.backupFile) -Destination $item.Path
    if ((Get-FileHash -LiteralPath $item.Path -Algorithm SHA256).Hash -ne $file.sha256) { throw "Restored file hash mismatch: $($item.Path)" }
    Write-Host "  restored $($file.name) -> $($item.Path)"
}
Write-Host "Live restore complete. Previous live files were moved to: $preRestore" -ForegroundColor Green
Write-Host "Start MCC with .\scripts\MCC.ps1 start and confirm the collector resumes from its watermarks."
exit 0
