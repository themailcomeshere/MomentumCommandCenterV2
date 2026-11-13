<#
.SYNOPSIS
    Interactive Windows launcher for Momentum Command Center: start, status, stop, open.

.DESCRIPTION
    MCC runs only while the operator is logged in (decision 2026-11-11). This script:
      start  - single-instance start of the Release API build, waits for /api/health, opens the Dashboard.
               A second start reports the running instance and does not launch another.
      status - reports PID, health, source SHA and log paths.
      stop   - graceful stop through the token-protected loopback endpoint; hosted workers get their
               normal shutdown. Force-kills only after the timeout, and says so.
      open   - opens the Dashboard in the default browser.

    No Windows Service and no unattended-after-sign-out mode is created. No order is ever submitted.
    Build first with .\scripts\Get-Latest-And-Test.ps1 (start uses the existing Release build output).

.EXAMPLE
    .\scripts\MCC.ps1 start
    .\scripts\MCC.ps1 status
    .\scripts\MCC.ps1 stop
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet("start", "status", "stop", "open")]
    [string]$Command = "status",

    [int]$Port = 53360,

    # Development loads Alpaca keys from user secrets, matching the existing launch profile.
    [ValidateSet("Development", "Production")]
    [string]$Environment = "Development",

    [int]$StartupTimeoutSeconds = 90,
    [int]$StopTimeoutSeconds = 30,
    [switch]$NoBrowser,

    # Directory for the PID file, shutdown token and logs. Defaults to the MCC data folder.
    [string]$RunDirectory = (Join-Path $env:LOCALAPPDATA "MomentumCommandCenterV2\run"),

    [int]$KeepLogFiles = 10
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProjectDir = Join-Path $repoRoot "src\MomentumCommandCenterV2.API"
$apiDll = Join-Path $apiProjectDir "bin\Release\net8.0\MomentumCommandCenterV2.API.dll"
$pidFile = Join-Path $RunDirectory "api.pid.json"
$tokenFile = Join-Path $RunDirectory "shutdown.token"
$baseUrl = "http://localhost:$Port"

function Get-Health {
    try { return Invoke-RestMethod -Uri "$baseUrl/api/health" -TimeoutSec 3 -ErrorAction Stop }
    catch { return $null }
}

function Get-RecordedInstance {
    if (-not (Test-Path $pidFile)) { return $null }
    try { $record = Get-Content $pidFile -Raw | ConvertFrom-Json } catch { return $null }
    $process = Get-Process -Id $record.processId -ErrorAction SilentlyContinue
    if ($null -eq $process) { return $null }
    # Guard against PID reuse: the recorded start time must match the live process.
    if ($process.StartTime.ToUniversalTime().ToString("o") -ne $record.processStartTimeUtc) { return $null }
    return [pscustomobject]@{ Record = $record; Process = $process }
}

function Get-PortOwner {
    $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $connection) { return $null }
    return Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
}

function Open-Dashboard {
    if ($NoBrowser) { return }
    Start-Process "$baseUrl/"
}

function Remove-OldLogs {
    Get-ChildItem $RunDirectory -Filter "api-*.log" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -Skip ($KeepLogFiles * 2) |
        Remove-Item -Force -ErrorAction SilentlyContinue
}

function Write-Status {
    $instance = Get-RecordedInstance
    $health = Get-Health
    if ($null -eq $instance -and $null -eq $health) {
        Write-Host "MCC API: NOT RUNNING (port $Port free)." -ForegroundColor Yellow
        return 1
    }
    if ($null -eq $instance) {
        $owner = Get-PortOwner
        Write-Host "MCC API: RUNNING but not started by this launcher (PID $($owner.Id) $($owner.ProcessName))." -ForegroundColor Yellow
        Write-Host "  Health: $($health.status) - source SHA $($health.sourceSha) - graceful stop $($health.gracefulShutdownEnabled)"
        return 0
    }
    Write-Host "MCC API: RUNNING" -ForegroundColor Green
    Write-Host "  PID:          $($instance.Process.Id) (started $($instance.Record.processStartTimeUtc))"
    Write-Host "  URL:          $baseUrl/"
    Write-Host "  Source SHA:   $($instance.Record.sourceSha)"
    Write-Host "  Environment:  $($instance.Record.environment)"
    Write-Host "  Health:       $(if ($health) { "$($health.status) - API reports SHA $($health.sourceSha)" } else { 'NOT RESPONDING' })"
    Write-Host "  Logs:         $($instance.Record.stdoutLog)"
    Write-Host "                $($instance.Record.stderrLog)"
    return 0
}

switch ($Command) {
    "status" { exit (Write-Status) }

    "open" { Open-Dashboard; exit 0 }

    "start" {
        New-Item -ItemType Directory -Force -Path $RunDirectory | Out-Null

        $existing = Get-RecordedInstance
        if ($null -ne $existing) {
            Write-Host "MCC is already running (PID $($existing.Process.Id)). Not starting a second instance." -ForegroundColor Yellow
            Open-Dashboard
            exit 0
        }
        $owner = Get-PortOwner
        if ($null -ne $owner) {
            Write-Host "Port $Port is already in use by PID $($owner.Id) ($($owner.ProcessName)). Stop it first; MCC was not started." -ForegroundColor Red
            exit 2
        }
        if (-not (Test-Path $apiDll)) {
            Write-Host "Release build not found: $apiDll" -ForegroundColor Red
            Write-Host "Run .\scripts\Get-Latest-And-Test.ps1 first." -ForegroundColor Red
            exit 3
        }

        $sourceSha = "UNKNOWN"
        try { $sourceSha = (git -C $repoRoot rev-parse HEAD).Trim() } catch { }
        $dirty = @(git -C $repoRoot status --porcelain 2>$null)
        if ($dirty.Count -gt 0) { $sourceSha = "$sourceSha+DIRTY" }

        $tokenBytes = New-Object byte[] 32
        $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        try { $rng.GetBytes($tokenBytes) } finally { $rng.Dispose() }
        $token = ([System.BitConverter]::ToString($tokenBytes)).Replace("-", "")
        Set-Content -Path $tokenFile -Value $token -NoNewline -Encoding ascii
        $stamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmss")
        $stdoutLog = Join-Path $RunDirectory "api-$stamp.out.log"
        $stderrLog = Join-Path $RunDirectory "api-$stamp.err.log"
        Remove-OldLogs

        # Environment for the child process only; the caller's values are restored afterwards.
        $savedEnvironment = @{}
        foreach ($name in "ASPNETCORE_ENVIRONMENT", "ASPNETCORE_URLS", "MCC_SHUTDOWN_TOKEN", "MCC_SOURCE_SHA", "MCC_LAUNCHED_BY") {
            $savedEnvironment[$name] = [System.Environment]::GetEnvironmentVariable($name, "Process")
        }
        $env:ASPNETCORE_ENVIRONMENT = $Environment
        $env:ASPNETCORE_URLS = $baseUrl
        $env:MCC_SHUTDOWN_TOKEN = $token
        $env:MCC_SOURCE_SHA = $sourceSha
        $env:MCC_LAUNCHED_BY = "scripts/MCC.ps1"
        $process = Start-Process -FilePath "dotnet" `
            -ArgumentList @("`"$apiDll`"", "--contentRoot", "`"$apiProjectDir`"") `
            -WorkingDirectory $apiProjectDir -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog
        foreach ($name in $savedEnvironment.Keys) {
            [System.Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], "Process")
        }

        Write-Host "Starting MCC API (PID $($process.Id), source $sourceSha)..."
        $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
        $health = $null
        while ((Get-Date) -lt $deadline) {
            if ($process.HasExited) { break }
            $health = Get-Health
            if ($null -ne $health) { break }
            Start-Sleep -Milliseconds 500
        }

        if ($null -eq $health) {
            Write-Host "MCC API FAILED TO START." -ForegroundColor Red
            if ($process.HasExited) { Write-Host "Process exited with code $($process.ExitCode)." -ForegroundColor Red }
            else { Write-Host "No health response within $StartupTimeoutSeconds s; stopping the process." -ForegroundColor Red; Stop-Process -Id $process.Id -Force }
            Write-Host "--- last stderr lines ($stderrLog) ---"
            Get-Content $stderrLog -Tail 30 -ErrorAction SilentlyContinue
            Write-Host "--- last stdout lines ($stdoutLog) ---"
            Get-Content $stdoutLog -Tail 30 -ErrorAction SilentlyContinue
            Remove-Item $tokenFile -Force -ErrorAction SilentlyContinue
            exit 4
        }

        [pscustomobject]@{
            processId           = $process.Id
            processStartTimeUtc = $process.StartTime.ToUniversalTime().ToString("o")
            port                = $Port
            environment         = $Environment
            sourceSha           = $sourceSha
            startedBy           = $env:USERNAME
            stdoutLog           = $stdoutLog
            stderrLog           = $stderrLog
        } | ConvertTo-Json | Set-Content -Path $pidFile -Encoding utf8

        Write-Host "MCC API is running: $baseUrl/ (PID $($process.Id))." -ForegroundColor Green
        Write-Host "Health: $($health.status) - reported source SHA $($health.sourceSha) - orders $($health.orderSubmission)"
        Write-Host "Stop with: .\scripts\MCC.ps1 stop   (signing out of Windows also ends the process)"
        Open-Dashboard
        exit 0
    }

    "stop" {
        $instance = Get-RecordedInstance
        if ($null -eq $instance) {
            $owner = Get-PortOwner
            if ($null -ne $owner) {
                Write-Host "Port $Port is owned by PID $($owner.Id) ($($owner.ProcessName)), which this launcher did not start. Not stopping it." -ForegroundColor Yellow
                exit 2
            }
            Write-Host "MCC API is not running." -ForegroundColor Yellow
            Remove-Item $pidFile, $tokenFile -Force -ErrorAction SilentlyContinue
            exit 0
        }

        $token = Get-Content $tokenFile -Raw -ErrorAction SilentlyContinue
        $graceful = $false
        if ($token) {
            try {
                Invoke-RestMethod -Method Post -Uri "$baseUrl/api/admin/shutdown" -Headers @{ "X-MCC-Shutdown-Token" = $token.Trim() } -TimeoutSec 5 | Out-Null
                $graceful = $instance.Process.WaitForExit($StopTimeoutSeconds * 1000)
            }
            catch { Write-Host "Graceful stop request failed: $($_.Exception.Message)" -ForegroundColor Yellow }
        }
        if (-not $graceful) {
            Write-Host "Graceful stop did not finish within $StopTimeoutSeconds s; force-stopping PID $($instance.Process.Id). In-flight writes may be cut off." -ForegroundColor Red
            Stop-Process -Id $instance.Process.Id -Force
        }
        else {
            Write-Host "MCC API stopped gracefully (PID $($instance.Process.Id))." -ForegroundColor Green
        }
        Remove-Item $pidFile, $tokenFile -Force -ErrorAction SilentlyContinue
        exit 0
    }
}
