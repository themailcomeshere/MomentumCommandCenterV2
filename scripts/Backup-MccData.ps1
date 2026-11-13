<#
.SYNOPSIS
    Manifest- and hash-backed backup of every durable MCC data store.

.DESCRIPTION
    Copies each store in scripts\lib\MccDataInventory.ps1 into a new timestamped folder and writes
    manifest.json (source path, size, last write time, SHA-256 of the copied file) next to it.
    Refuses to run while the MCC API is running unless -AllowRunning is given: append-only JSONL files
    copied mid-write can end in a partial last line, which the manifest then flags.
    Secrets (user-secrets, API keys) and browser localStorage are not backed up; see the manifest notes.

.EXAMPLE
    .\scripts\MCC.ps1 stop
    .\scripts\Backup-MccData.ps1
    .\scripts\Backup-MccData.ps1 -Destination D:\MCC-Backups
#>
[CmdletBinding()]
param(
    [string]$Destination = (Join-Path ([Environment]::GetFolderPath("MyDocuments")) "MCC-Backups"),
    [int]$Port = 53360,
    [switch]$AllowRunning
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\MccDataInventory.ps1")
$repoRoot = Split-Path -Parent $PSScriptRoot

$listening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if ($listening -and -not $AllowRunning) {
    Write-Host "The MCC API is listening on port $Port. Stop it first (.\scripts\MCC.ps1 stop) or pass -AllowRunning." -ForegroundColor Red
    exit 2
}

$stamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmss")
$target = Join-Path $Destination "mcc-backup-$stamp"
New-Item -ItemType Directory -Force -Path (Join-Path $target "data") | Out-Null

$files = @()
foreach ($item in Get-MccDataInventory -RepoRoot $repoRoot) {
    if (-not $item.Exists) {
        $files += [pscustomobject]@{ name = $item.Name; sourcePath = $item.Path; overridden = $item.Overridden; status = "NOT_PRESENT" }
        continue
    }
    $fileName = Split-Path $item.Path -Leaf
    $copyPath = Join-Path (Join-Path $target "data") $fileName
    Copy-Item -LiteralPath $item.Path -Destination $copyPath
    $info = Get-Item -LiteralPath $copyPath
    $hash = (Get-FileHash -LiteralPath $copyPath -Algorithm SHA256).Hash
    $partialLastLine = $false
    if ($item.Kind -like "append-only*" -and $info.Length -gt 0) {
        $stream = [System.IO.File]::OpenRead($copyPath)
        try { $stream.Seek(-1, [System.IO.SeekOrigin]::End) | Out-Null; $partialLastLine = ($stream.ReadByte() -ne 10) } finally { $stream.Dispose() }
    }
    $files += [pscustomobject]@{
        name               = $item.Name
        sourcePath         = $item.Path
        overridden         = $item.Overridden
        kind               = $item.Kind
        backupFile         = "data/$fileName"
        bytes              = $info.Length
        sourceLastWriteUtc = (Get-Item -LiteralPath $item.Path).LastWriteTimeUtc.ToString("o")
        sha256             = $hash
        partialLastLine    = $partialLastLine
        status             = "COPIED"
    }
}

$sourceSha = "UNKNOWN"
try { $sourceSha = (git -C $repoRoot rev-parse HEAD).Trim() } catch { }
$manifest = [pscustomobject]@{
    manifestVersion = 1
    createdAtUtc    = (Get-Date).ToUniversalTime().ToString("o")
    machine         = $env:COMPUTERNAME
    sourceSha       = $sourceSha
    apiWasRunning   = [bool]$listening
    files           = $files
    notes           = @(
        "Secrets are NOT backed up: user-secrets id MomentumCommandCenterV2-API-Local (%APPDATA%\Microsoft\UserSecrets) and ALPACA_*/MASSIVE_* variables. Re-enter them after a machine rebuild.",
        "Browser localStorage (mcc.scannerImport.v1, mcc.redZoneState.v1, mcc.workspaceMode.v1, mcc.watchlist, mcc.dashboardFeedback.v1, mcc.overnightShortlists.v1) is per-browser and not backed up; re-import the scanner CSV.",
        "Logs under %LOCALAPPDATA%\MomentumCommandCenterV2\run are diagnostic and not backed up."
    )
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $target "manifest.json") -Encoding utf8

$copied = @($files | Where-Object status -eq "COPIED")
$totalBytes = ($copied | Measure-Object bytes -Sum).Sum
Write-Host "Backup written: $target" -ForegroundColor Green
Write-Host "  Copied $($copied.Count) files, $([math]::Round($totalBytes / 1MB, 1)) MB; not present: $(@($files | Where-Object status -eq 'NOT_PRESENT').Count)."
$partial = @($copied | Where-Object partialLastLine)
if ($partial.Count -gt 0) {
    Write-Host "  WARNING: last line is incomplete (no trailing newline) in $($partial.name -join ', '). Usually means the file was being written during the copy." -ForegroundColor Yellow
}
Write-Host "  Verify or restore with: .\scripts\Restore-MccData.ps1 -BackupPath `"$target`""
exit 0
