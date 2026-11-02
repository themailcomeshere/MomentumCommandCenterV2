[CmdletBinding()]
param(
    [string]$Branch = "feature/opportunity-quality-classification"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    Write-Host "`n> $Command $($Arguments -join ' ')" -ForegroundColor Cyan
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command failed with exit code $LASTEXITCODE."
    }
}

try {
    if (-not (Test-Path (Join-Path $repoRoot "MomentumCommandCenterV2.sln"))) {
        throw "Run this script from a MomentumCommandCenterV2 repository checkout."
    }

    $dirty = @(git status --porcelain)
    if ($LASTEXITCODE -ne 0) { throw "Unable to read Git working-tree status." }
    if ($dirty.Count -gt 0) {
        $dirty | ForEach-Object { Write-Host $_ -ForegroundColor Yellow }
        throw "Working tree is not clean. Commit or safely stash your changes before updating; this script will not overwrite them."
    }

    Invoke-Checked -Command "git" -Arguments @("fetch", "origin", $Branch)

    $currentBranch = (git branch --show-current).Trim()
    if ($LASTEXITCODE -ne 0) { throw "Unable to determine current Git branch." }

    if ($currentBranch -ne $Branch) {
        $localBranch = git branch --list $Branch
        if ($LASTEXITCODE -ne 0) { throw "Unable to inspect local branches." }
        if ([string]::IsNullOrWhiteSpace(($localBranch -join ""))) {
            Invoke-Checked -Command "git" -Arguments @("switch", "--track", "-c", $Branch, "origin/$Branch")
        }
        else {
            Invoke-Checked -Command "git" -Arguments @("switch", $Branch)
        }
    }

    Invoke-Checked -Command "git" -Arguments @("pull", "--ff-only", "origin", $Branch)
    $head = (git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw "Unable to read the updated commit SHA." }
    Write-Host "`nUpdated branch: $Branch" -ForegroundColor Green
    Write-Host "Commit: $head" -ForegroundColor Green

    Invoke-Checked -Command "dotnet" -Arguments @("restore", ".\MomentumCommandCenterV2.sln")
    Invoke-Checked -Command "dotnet" -Arguments @("build", ".\MomentumCommandCenterV2.sln", "-c", "Release", "--no-restore")
    Invoke-Checked -Command "dotnet" -Arguments @("test", ".\MomentumCommandCenterV2.sln", "-c", "Release", "--no-build", "--logger", "console;verbosity=normal")

    Write-Host "`nGET LATEST + RELEASE BUILD + TESTS: PASSED" -ForegroundColor Green
    Write-Host "Verified local source SHA: $head" -ForegroundColor Green
}
finally {
    Pop-Location
}
