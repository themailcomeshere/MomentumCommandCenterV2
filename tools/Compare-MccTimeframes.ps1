[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $OneMinutePath,

    [Parameter(Mandatory = $true)]
    [string] $FiveMinutePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-MccBars {
    param([string] $FilePath)

    $payloadPattern = 'MCC_OHLCV\|v1\|([^|]+)\|(1m|5m)\|([0-9,]+)\|([^|]+)\|([^|]+)\|([^|]+)\|([^|]+)\|([^\)]+)\)'
    $culture = [Globalization.CultureInfo]::InvariantCulture
    $bars = [System.Collections.Generic.List[object]]::new()

    foreach ($line in Get-Content -LiteralPath $FilePath) {
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
    @($bars | Sort-Object Timestamp)
}

foreach ($path in @($OneMinutePath, $FiveMinutePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "File not found: $path" }
}

$one = @(Get-MccBars $OneMinutePath)
$five = @(Get-MccBars $FiveMinutePath)
$complete = 0
$incomplete = [System.Collections.Generic.List[object]]::new()
$mismatches = [System.Collections.Generic.List[object]]::new()

foreach ($bar5 in $five) {
    $group = @($one | Where-Object {
        $_.Timestamp -ge $bar5.Timestamp -and $_.Timestamp -lt $bar5.Timestamp.AddMinutes(5)
    } | Sort-Object Timestamp)

    if ($group.Count -ne 5) {
        $incomplete.Add([pscustomobject]@{
            Timestamp = $bar5.Timestamp.ToUniversalTime().ToString('O')
            OneMinuteRows = $group.Count
        })
        continue
    }

    $complete++
    $expected = @{
        Open = $group[0].Open
        High = ($group | Measure-Object High -Maximum).Maximum
        Low = ($group | Measure-Object Low -Minimum).Minimum
        Close = $group[-1].Close
        Volume = ($group | Measure-Object Volume -Sum).Sum
    }
    $different = @(@('Open','High','Low','Close','Volume') | Where-Object {
        [decimal]$expected[$_] -ne [decimal]$bar5.$_
    })
    if ($different.Count -gt 0) {
        $mismatches.Add([pscustomobject]@{
            Timestamp = $bar5.Timestamp.ToUniversalTime().ToString('O')
            Fields = $different -join ', '
            OneMinuteOpen = $expected.Open
            FiveMinuteOpen = $bar5.Open
            OneMinuteHigh = $expected.High
            FiveMinuteHigh = $bar5.High
            OneMinuteLow = $expected.Low
            FiveMinuteLow = $bar5.Low
            OneMinuteClose = $expected.Close
            FiveMinuteClose = $bar5.Close
            OneMinuteVolume = $expected.Volume
            FiveMinuteVolume = $bar5.Volume
        })
    }
}

Write-Output "1M payload rows: $($one.Count)"
Write-Output "5M payload rows: $($five.Count)"
Write-Output "Complete 5M windows represented by five 1M rows: $complete"
Write-Output "Incomplete 5M windows: $($incomplete.Count)"
Write-Output "OHLCV mismatches in complete windows: $($mismatches.Count)"

if ($incomplete.Count -gt 0) {
    Write-Output 'First incomplete windows:'
    $incomplete | Select-Object -First 10 | Format-Table -AutoSize | Out-String | Write-Output
}
if ($mismatches.Count -gt 0) {
    Write-Output 'First mismatches:'
    $mismatches | Select-Object -First 10 | Format-Table -AutoSize | Out-String | Write-Output
}

Write-Output 'STATUS: This compares exported 1M and 5M files only; it is not independent Thinkorswim chart parity.'
