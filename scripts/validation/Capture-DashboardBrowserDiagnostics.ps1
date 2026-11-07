[CmdletBinding()]
param(
    [string]$BaseUri = "http://localhost:53360/",
    [ValidateRange(1024, 65535)][int]$DebugPort = 9222,
    [ValidateRange(15, 600)][int]$CaptureSeconds = 90,
    [string]$BrowserPath = "",
    [string]$OutputDirectory = ""
)
$ErrorActionPreference = "Stop"
$BaseUri = $BaseUri.TrimEnd("/") + "/"
$stamp = [DateTimeOffset]::UtcNow.ToString("yyyyMMddTHHmmssZ")
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
    $OutputDirectory = Join-Path $repoRoot "artifacts/browser-diagnostics/$stamp"
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

function Get-SafeUrl([string]$Value) {
    try {
        $u = [uri]$Value
        $queryKeys = @()
        if (-not [string]::IsNullOrWhiteSpace($u.Query)) {
            $queryKeys = @($u.Query.TrimStart("?").Split("&") | ForEach-Object { ($_ -split "=", 2)[0] } | Where-Object { $_ })
        }
        $safe = "$($u.Scheme)://$($u.Authority)$($u.AbsolutePath)"
        if ($queryKeys.Count -gt 0) { $safe += "?" + ($queryKeys -join "&") }
        return $safe
    } catch { return "[non-http-url]" }
}
function Get-EdgePath {
    if (-not [string]::IsNullOrWhiteSpace($BrowserPath)) {
        if (-not (Test-Path -LiteralPath $BrowserPath -PathType Leaf)) {
            throw "The supplied -BrowserPath is not an existing file: '$BrowserPath'. Supply the full path to msedge.exe."
        }
        $explicit = (Resolve-Path -LiteralPath $BrowserPath).Path
        if ([IO.Path]::GetFileName($explicit) -ine "msedge.exe") {
            throw "The supplied -BrowserPath must point to msedge.exe, not '$explicit'."
        }
        return $explicit
    }

    $candidates = [System.Collections.Generic.List[string]]::new()
    foreach ($root in @(
        [Environment]::GetEnvironmentVariable("ProgramFiles(x86)"),
        $env:ProgramFiles,
        $env:LOCALAPPDATA
    )) {
        if (-not [string]::IsNullOrWhiteSpace([string]$root)) {
            $candidates.Add((Join-Path $root "Microsoft/Edge/Application/msedge.exe"))
        }
    }

    # App Paths covers non-default installs and machine/user-specific Edge registrations.
    foreach ($key in @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe",
        "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe"
    )) {
        try {
            $registered = (Get-Item -LiteralPath $key -ErrorAction Stop).GetValue("")
            if (-not [string]::IsNullOrWhiteSpace([string]$registered)) {
                $candidates.Add([string]$registered)
            }
        } catch {}
    }

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $resolved = (Resolve-Path -LiteralPath $candidate).Path
            if ([IO.Path]::GetFileName($resolved) -ieq "msedge.exe") { return $resolved }
        }
    }

    foreach ($command in @(Get-Command msedge.exe -All -ErrorAction SilentlyContinue)) {
        $source = if ($command.Source) { $command.Source } else { $command.Definition }
        if (-not [string]::IsNullOrWhiteSpace([string]$source) -and (Test-Path -LiteralPath $source -PathType Leaf)) {
            $resolved = (Resolve-Path -LiteralPath $source).Path
            if ([IO.Path]::GetFileName($resolved) -ieq "msedge.exe") { return $resolved }
        }
    }

    $checked = @($candidates | Select-Object -Unique) -join "; "
    throw "Microsoft Edge executable was not found. Checked: $checked. Supply -BrowserPath with the full existing path to msedge.exe."
}
$debugRoot = "http://127.0.0.1:$DebugPort"
$debugReady = $false
try { $null = Invoke-RestMethod "$debugRoot/json/version" -TimeoutSec 2; $debugReady = $true } catch {}
if (-not $debugReady) {
    $edge = Get-EdgePath
    if (-not (Test-Path -LiteralPath $edge -PathType Leaf)) {
        throw "Resolved Edge executable disappeared before launch: '$edge'."
    }
    $profile = Join-Path $env:TEMP "MCC-CDP-$stamp"
    New-Item -ItemType Directory -Force -Path $profile | Out-Null
    $edgeArguments = @("--remote-debugging-port=$DebugPort","--user-data-dir=`"$profile`"","--no-first-run","--disable-default-apps",$BaseUri)
    Write-Host "Launching Microsoft Edge executable: $edge"
    try {
        $browserProcess = Start-Process -FilePath $edge -ArgumentList $edgeArguments -WorkingDirectory $env:TEMP -PassThru -ErrorAction Stop
    } catch {
        throw "Microsoft Edge launch failed. Resolved executable: '$edge'. Confirm that this exact file exists and is executable. Start-Process error: $($_.Exception.Message)"
    }
    if (-not $browserProcess) { throw "Start-Process did not return a Microsoft Edge process for '$edge'." }
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    do {
        Start-Sleep -Milliseconds 500
        try { $null = Invoke-RestMethod "$debugRoot/json/version" -TimeoutSec 2; $debugReady = $true } catch {}
    } while (-not $debugReady -and [DateTimeOffset]::UtcNow -lt $deadline)
}
if (-not $debugReady) { throw "Browser DevTools endpoint did not start at $debugRoot." }
$targets = @(Invoke-RestMethod "$debugRoot/json/list" -TimeoutSec 10)
$target = $targets | Where-Object { $_.type -eq "page" -and $_.url -like "$BaseUri*" } | Select-Object -First 1
if (-not $target) { $target = $targets | Where-Object { $_.type -eq "page" } | Select-Object -First 1 }
if (-not $target -or [string]::IsNullOrWhiteSpace([string]$target.webSocketDebuggerUrl)) { throw "No browser page target with a DevTools WebSocket URL was found." }

$script:socket = [System.Net.WebSockets.ClientWebSocket]::new()
$script:nextId = 0
$script:events = [System.Collections.Generic.List[object]]::new()
$script:socket.ConnectAsync([uri]$target.webSocketDebuggerUrl, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
function Receive-CdpMessage([int]$TimeoutMilliseconds = 10000) {
    $buffer = New-Object byte[] 65536
    $memory = [System.IO.MemoryStream]::new()
    $cts = [Threading.CancellationTokenSource]::new($TimeoutMilliseconds)
    try {
        do {
            $segment = [ArraySegment[byte]]::new($buffer)
            $result = $script:socket.ReceiveAsync($segment, $cts.Token).GetAwaiter().GetResult()
            if ($result.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) { return $null }
            $memory.Write($buffer, 0, $result.Count)
        } while (-not $result.EndOfMessage)
        return ([Text.Encoding]::UTF8.GetString($memory.ToArray()) | ConvertFrom-Json)
    } finally { $cts.Dispose(); $memory.Dispose() }
}
function Send-Cdp([string]$Method, [hashtable]$Params = @{}) {
    $script:nextId++
    $id = $script:nextId
    $payload = @{ id = $id; method = $Method; params = $Params } | ConvertTo-Json -Depth 30 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($payload)
    $segment = [ArraySegment[byte]]::new($bytes)
    $script:socket.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(15)
    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        $message = Receive-CdpMessage 15000
        if ($null -eq $message) { continue }
        if ($message.id -eq $id) { return $message }
        if ($message.method) { $script:events.Add($message) }
    }
    throw "Timed out waiting for Chrome DevTools command '$Method'."
}
$null = Send-Cdp "Runtime.enable"
$null = Send-Cdp "Log.enable"
$null = Send-Cdp "Network.enable" @{ maxTotalBufferSize = 10485760; maxResourceBufferSize = 1048576 }
$null = Send-Cdp "Page.enable"
$navigation = Send-Cdp "Page.navigate" @{ url = $BaseUri }
$startedAt = [DateTimeOffset]::UtcNow
$deadline = $startedAt.AddSeconds($CaptureSeconds)
Write-Host "Capturing Dashboard console/network diagnostics for $CaptureSeconds seconds: $BaseUri"
while ([DateTimeOffset]::UtcNow -lt $deadline) {
    try {
        $message = Receive-CdpMessage 1000
        if ($message -and $message.method) { $script:events.Add($message) }
    } catch [System.OperationCanceledException] { continue }
}
$screenshot = $null
try {
    $shot = Send-Cdp "Page.captureScreenshot" @{ format = "png"; captureBeyondViewport = $false }
    if ($shot.result.data) {
        $screenshot = Join-Path $OutputDirectory "dashboard.png"
        [IO.File]::WriteAllBytes($screenshot, [Convert]::FromBase64String([string]$shot.result.data))
    }
} catch { Write-Warning "Dashboard screenshot capture failed: $($_.Exception.Message)" }

$console = [System.Collections.Generic.List[object]]::new()
$network = [System.Collections.Generic.List[object]]::new()
foreach ($event in $script:events) {
    switch ($event.method) {
        "Runtime.consoleAPICalled" {
            $argsText = @($event.params.args | ForEach-Object {
                if ($_.value -ne $null) { [string]$_.value } elseif ($_.description) { [string]$_.description } else { [string]$_.type }
            }) -join " "
            $console.Add([pscustomobject]@{ kind="console"; level=[string]$event.params.type; timestamp=$event.params.timestamp; text=$argsText })
        }
        "Runtime.exceptionThrown" {
            $details = $event.params.exceptionDetails
            $description = if ($details.exception.description) { $details.exception.description } else { $details.text }
            $console.Add([pscustomobject]@{ kind="uncaught-exception"; level="error"; timestamp=$event.params.timestamp; text=[string]$description; url=(Get-SafeUrl ([string]$details.url)); lineNumber=$details.lineNumber; columnNumber=$details.columnNumber })
        }
        "Log.entryAdded" {
            $entry = $event.params.entry
            $console.Add([pscustomobject]@{ kind="browser-log"; level=[string]$entry.level; timestamp=$entry.timestamp; text=[string]$entry.text; url=(Get-SafeUrl ([string]$entry.url)); source=[string]$entry.source })
        }
        "Network.requestWillBeSent" {
            $request = $event.params.request
            $network.Add([pscustomobject]@{ kind="request"; requestId=[string]$event.params.requestId; timestamp=$event.params.timestamp; method=[string]$request.method; url=(Get-SafeUrl ([string]$request.url)); resourceType=[string]$event.params.type })
        }
        "Network.responseReceived" {
            $response = $event.params.response
            $network.Add([pscustomobject]@{ kind="response"; requestId=[string]$event.params.requestId; timestamp=$event.params.timestamp; url=(Get-SafeUrl ([string]$response.url)); status=[int]$response.status; mimeType=[string]$response.mimeType; resourceType=[string]$event.params.type; fromDiskCache=[bool]$response.fromDiskCache })
        }
        "Network.loadingFailed" {
            $network.Add([pscustomobject]@{ kind="failed"; requestId=[string]$event.params.requestId; timestamp=$event.params.timestamp; errorText=[string]$event.params.errorText; canceled=[bool]$event.params.canceled; blockedReason=[string]$event.params.blockedReason })
        }
    }
}
$consolePath = Join-Path $OutputDirectory "browser-console.json"
$networkPath = Join-Path $OutputDirectory "browser-network.json"
$manifestPath = Join-Path $OutputDirectory "browser-diagnostics-manifest.json"
@($console) | ConvertTo-Json -Depth 30 | Set-Content -Encoding utf8 $consolePath
@($network) | ConvertTo-Json -Depth 30 | Set-Content -Encoding utf8 $networkPath
$sourceCommit = $null
try { $repoRootForGit = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path; $sourceCommit = (& git -C $repoRootForGit rev-parse HEAD 2>$null).Trim() } catch {}
$manifest = [ordered]@{
    sourceCommit = $sourceCommit
    capturedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    startedAtUtc = $startedAt.ToString("o")
    baseUri = $BaseUri
    browserTarget = Get-SafeUrl ([string]$target.url)
    browserTargetId = [string]$target.id
    debugPort = $DebugPort
    captureSeconds = $CaptureSeconds
    navigationError = $navigation.error
    consoleEntryCount = $console.Count
    consoleErrorCount = @($console | Where-Object { $_.level -eq "error" -or $_.kind -eq "uncaught-exception" }).Count
    networkRequestCount = @($network | Where-Object kind -eq "request").Count
    httpErrorResponseCount = @($network | Where-Object { $_.kind -eq "response" -and $_.status -ge 400 }).Count
    networkFailureCount = @($network | Where-Object kind -eq "failed").Count
    screenshot = $screenshot
    consoleFile = $consolePath
    networkFile = $networkPath
    note = "URLs are sanitized to origin/path/query-key names. Request headers, cookies, response bodies, credentials and query values are not captured."
}
[pscustomobject]$manifest | ConvertTo-Json -Depth 30 | Set-Content -Encoding utf8 $manifestPath
try { $script:socket.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "capture complete", [Threading.CancellationToken]::None).GetAwaiter().GetResult() } catch {}
$script:socket.Dispose()
Write-Host "Browser diagnostics saved: $OutputDirectory"
Write-Host "Console errors: $($manifest.consoleErrorCount); HTTP error responses: $($manifest.httpErrorResponseCount); network failures: $($manifest.networkFailureCount)"
Write-Host "Inspect raw events; 4xx/failed requests are not automatically defects until expected endpoint behavior is considered."
