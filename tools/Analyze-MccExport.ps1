[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-MccBars {
    param([string] $FilePath)

    $payloadPattern = 'MCC_OHLCV\|v1\|([^|]+)\|(1m|5m)\|([0-9,]+)\|([^|]+)\|([^|]+)\|([^|]+)\|([^|]+)\|([^\)]+)\)'
    $culture = [Globalization.CultureInfo]::InvariantCulture
    $bars = [System.Collections.Generic.List[object]]::new()
    $transportCount = 0

    foreach ($line in Get-Content -LiteralPath $FilePath) {
        if ($line -match 'MCC_OHLCV_TRANSPORT_CLOSE') {
            $transportCount++
        }

        $match = [regex]::Match($line, $payloadPattern)
        if (-not $match.Success) { continue }

        $epoch = [int64]($match.Groups[3].Value -replace ',', '')
        $bars.Add([pscustomobject]@{
            Symbol = $match.Groups[1].Value
            Timeframe = $match.Groups[2].Value
            Timestamp = [DateTimeOffset]::FromUnixTimeMilliseconds($epoch)
            Open = [decimal]::Parse($match.Groups[4].Value, [Globalization.NumberStyles]::Number, $culture)
            High = [decimal]::Parse($match.Groups[5].Value, [Globalization.NumberStyles]::Number, $culture)
            Low = [decimal]::Parse($match.Groups[6].Value, [Globalization.NumberStyles]::Number, $culture)
            Close = [decimal]::Parse($match.Groups[7].Value, [Globalization.NumberStyles]::Number, $culture)
            Volume = [decimal]::Parse(($match.Groups[8].Value -replace ',', ''), [Globalization.NumberStyles]::Number, $culture)
        })
    }

    [pscustomobject]@{
        Bars = @($bars | Sort-Object Timestamp)
        TransportCount = $transportCount
    }
}

if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "File not found: $Path"
}

$result = Get-MccBars -FilePath $Path
$bars = @($result.Bars)
Write-Output "File: $Path"
Write-Output "Payload rows: $($bars.Count)"
Write-Output "Transport rows: $($result.TransportCount)"

if ($bars.Count -eq 0) {
    Write-Output 'STATUS: REJECTED - no complete MCC OHLCV payloads found.'
    exit 2
}

$duplicateCount = @($bars | Group-Object { "$($_.Symbol)|$($_.Timeframe)|$($_.Timestamp.UtcDateTime.Ticks)" } | Where-Object Count -gt 1).Count
$ohlcErrors = @($bars | Where-Object {
    $_.High -lt $_.Low -or $_.High -lt $_.Open -or $_.High -lt $_.Close -or
    $_.Low -gt $_.Open -or $_.Low -gt $_.Close
}).Count
$volumeErrors = @($bars | Where-Object { $_.Volume -lt 0 }).Count
$timeframeSet = @($bars | Select-Object -ExpandProperty Timeframe -Unique) -join ', '

Write-Output "Symbol(s): $(($bars | Select-Object -ExpandProperty Symbol -Unique) -join ', ')"
Write-Output "Timeframe(s): $timeframeSet"
Write-Output "First payload: $($bars[0].Timestamp.ToUniversalTime().ToString('O'))"
Write-Output "Last payload:  $($bars[-1].Timestamp.ToUniversalTime().ToString('O'))"
Write-Output "Minimum volume: $(($bars | Measure-Object Volume -Minimum).Minimum)"
Write-Output "Maximum volume: $(($bars | Measure-Object Volume -Maximum).Maximum)"
Write-Output "Duplicate keys: $duplicateCount"
Write-Output "OHLC violations: $ohlcErrors"
Write-Output "Negative volumes: $volumeErrors"

foreach ($group in $bars | Group-Object Timeframe) {
    $periodSeconds = if ($group.Name -eq '1m') { 60 } else { 300 }
    $ordered = @($group.Group | Sort-Object Timestamp)
    $gaps = [System.Collections.Generic.List[object]]::new()
    for ($i = 1; $i -lt $ordered.Count; $i++) {
        $seconds = ($ordered[$i].Timestamp - $ordered[$i - 1].Timestamp).TotalSeconds
        if ($seconds -ne $periodSeconds) {
            $gaps.Add([pscustomobject]@{
                Previous = $ordered[$i - 1].Timestamp.ToUniversalTime().ToString('O')
                Current = $ordered[$i].Timestamp.ToUniversalTime().ToString('O')
                Seconds = [int]$seconds
            })
        }
    }
    Write-Output "$($group.Name) nonstandard intervals: $($gaps.Count)"
    $gaps | Select-Object -First 10 | Format-Table -AutoSize | Out-String | Write-Output
}

if ($duplicateCount -eq 0 -and $ohlcErrors -eq 0 -and $volumeErrors -eq 0) {
    Write-Output 'STATUS: STRUCTURALLY VALID - chart parity and timestamp semantics still require Thinkorswim comparison.'
} else {
    Write-Output 'STATUS: REJECTED - structural validation failed.'
    exit 3
}
