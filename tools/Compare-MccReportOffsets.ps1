[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Path,

    [int] $ReportOffsetHours = -7
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "File not found: $Path"
}

$payloadPattern = 'MCC_OHLCV\|v1\|([^|]+)\|(1m|5m)\|([0-9,]+)\|'
$reportOffset = [TimeSpan]::FromHours($ReportOffsetHours)
$rows = [System.Collections.Generic.List[object]]::new()

foreach ($line in Get-Content -LiteralPath $Path) {
    if ($line -notmatch 'MCC_OHLCV\|v1\|') { continue }
    $match = [regex]::Match($line, $payloadPattern)
    if (-not $match.Success) { continue }

    $fields = $line.Split(';')
    if ($fields.Count -lt 6) { continue }
    $reportLocal = [DateTime]::ParseExact($fields[5].Trim(), 'M/d/yy h:mm tt', [Globalization.CultureInfo]::GetCultureInfo('en-US'))
    $reportTimestamp = [DateTimeOffset]::new($reportLocal, $reportOffset)
    $epoch = [int64]($match.Groups[3].Value -replace ',', '')
    $payloadTimestamp = [DateTimeOffset]::FromUnixTimeMilliseconds($epoch)

    $rows.Add([pscustomobject]@{
        Timeframe = $match.Groups[2].Value
        ReportTime = $reportTimestamp
        PayloadTime = $payloadTimestamp
        OffsetMinutes = [math]::Round(($reportTimestamp.ToUniversalTime() - $payloadTimestamp.ToUniversalTime()).TotalMinutes, 3)
    })
}

if ($rows.Count -eq 0) {
    throw 'No MCC payload rows with report timestamps were found.'
}

Write-Output "File: $Path"
Write-Output "Report offset assumption: UTC$($ReportOffsetHours.ToString('+00;-00;00'))"
Write-Output "Rows: $($rows.Count)"
Write-Output "First row: report=$($rows[0].ReportTime.ToString('O')); payload=$($rows[0].PayloadTime.ToString('O')); offset=$($rows[0].OffsetMinutes) minutes"
Write-Output "Last row:  report=$($rows[-1].ReportTime.ToString('O')); payload=$($rows[-1].PayloadTime.ToString('O')); offset=$($rows[-1].OffsetMinutes) minutes"

$groups = @($rows | Group-Object Timeframe)
foreach ($group in $groups) {
    $offsets = @($group.Group | Select-Object -ExpandProperty OffsetMinutes -Unique)
    Write-Output "$($group.Name) distinct offsets: $($offsets -join ', ') minutes"
}

$rows | Select-Object -First 10 | Format-Table -AutoSize | Out-String | Write-Output
Write-Output 'STATUS: The report Date/Time is transport metadata. Use the embedded payload timestamp for normalized bars until chart parity establishes its bar-open/availability meaning.'
