[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Path,

    [Parameter(Mandatory = $true)]
    [string] $OutputPath,

    [int] $Count = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "File not found: $Path"
}
if ($Count -lt 1) {
    throw 'Count must be at least 1.'
}

$payloadPattern = 'MCC_OHLCV\|v1\|([^|]+)\|(1m|5m)\|([0-9,]+)\|([^|]+)\|([^|]+)\|([^|]+)\|([^|]+)\|([^\)]+)\)'
$culture = [Globalization.CultureInfo]::InvariantCulture
$rows = [System.Collections.Generic.List[object]]::new()

foreach ($line in Get-Content -LiteralPath $Path) {
    $match = [regex]::Match($line, $payloadPattern)
    if (-not $match.Success) { continue }

    $epoch = [int64]($match.Groups[3].Value -replace ',', '')
    $rows.Add([pscustomobject]@{
        SourceRow = $rows.Count + 1
        Symbol = $match.Groups[1].Value
        Timeframe = $match.Groups[2].Value
        PayloadTimestampUtc = [DateTimeOffset]::FromUnixTimeMilliseconds($epoch).ToUniversalTime().ToString('O')
        PayloadOpen = [decimal]::Parse($match.Groups[4].Value, [Globalization.NumberStyles]::Number, $culture)
        PayloadHigh = [decimal]::Parse($match.Groups[5].Value, [Globalization.NumberStyles]::Number, $culture)
        PayloadLow = [decimal]::Parse($match.Groups[6].Value, [Globalization.NumberStyles]::Number, $culture)
        PayloadClose = [decimal]::Parse($match.Groups[7].Value, [Globalization.NumberStyles]::Number, $culture)
        PayloadVolume = [decimal]::Parse(($match.Groups[8].Value -replace ',', ''), [Globalization.NumberStyles]::Number, $culture)
        ChartTimestamp = ''
        ChartOpen = ''
        ChartHigh = ''
        ChartLow = ''
        ChartClose = ''
        ChartVolume = ''
        ChartTimestampMeaning = ''
        Finding = ''
    })
    if ($rows.Count -ge $Count) { break }
}

if ($rows.Count -eq 0) {
    throw 'No MCC payload rows found.'
}

$parent = Split-Path -Parent $OutputPath
if ($parent -and -not (Test-Path -LiteralPath $parent)) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}

$rows | Export-Csv -LiteralPath $OutputPath -NoTypeInformation -Encoding UTF8
Write-Output "Wrote $($rows.Count) parity rows to $OutputPath"
Write-Output 'Fill ChartTimestamp, ChartOpen, ChartHigh, ChartLow, ChartClose, ChartVolume, ChartTimestampMeaning, and Finding from Thinkorswim.'
