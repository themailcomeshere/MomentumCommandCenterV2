# Durable MCC data inventory (source-verified 2026-11-13 against src/**/*.cs).
# Dot-source this file: . "$PSScriptRoot\lib\MccDataInventory.ps1"
# Each entry resolves its environment-variable override first, exactly as the API does.
# Secrets (user-secrets secrets.json, ALPACA_*/MASSIVE_* variables) are deliberately NOT included.

function Get-MccDataInventory {
    param([string]$RepoRoot = (Split-Path -Parent (Split-Path -Parent $PSCommandPath)))

    $defaultRoot = Join-Path $env:LOCALAPPDATA "MomentumCommandCenterV2"
    $apiRoot = Join-Path $RepoRoot "src\MomentumCommandCenterV2.API"
    $entries = @(
        @{ Name = "market-bars";               Override = "MCC_MARKET_BAR_STORE_PATH";            Default = "market-bars.jsonl";               Kind = "append-only JSONL"; Owner = "JsonlMarketBarObservationStore" },
        @{ Name = "scanner-discoveries";       Override = "MCC_SCANNER_DISCOVERY_PATH";           Default = "scanner-discoveries.jsonl";       Kind = "append-only JSONL"; Owner = "JsonlScannerDiscoveryEventStore" },
        @{ Name = "boring-overnight-snapshots"; Override = "MCC_BORING_OVERNIGHT_SNAPSHOT_PATH";  Default = "boring-overnight-snapshots.jsonl"; Kind = "append-only JSONL"; Owner = "BoringOvernightCandidateSnapshotStore" },
        @{ Name = "morning-brief-snapshots";   Override = "MCC_MORNING_BRIEF_SNAPSHOT_PATH";      Default = "morning-brief-snapshots.jsonl";   Kind = "append-only JSONL"; Owner = "MorningBriefSnapshotStore" },
        @{ Name = "catalyst-human-reviews";    Override = "MCC_CATALYST_REVIEW_PATH";             Default = "catalyst-human-reviews.jsonl";    Kind = "append-only JSONL"; Owner = "CatalystHumanReviewStore" },
        @{ Name = "thinkorswim-atr-observations"; Override = "MCC_TOS_ATR_STORE_PATH";            Default = "thinkorswim-atr-observations.jsonl"; Kind = "append-only JSONL"; Owner = "JsonlThinkorswimAtrObservationStore" },
        @{ Name = "tos-daily-gap-reference";   Override = "MCC_TOS_DAILY_GAP_PATH";               Default = "tos-daily-gap-reference.jsonl";   Kind = "append-only JSONL"; Owner = "ThinkorswimDailyGapCalculator store" },
        @{ Name = "collector-watermarks";      Override = "MCC_MARKET_COLLECTOR_CHECKPOINT_PATH"; Default = "collector-watermarks.json";       Kind = "rewritten JSON";    Owner = "MarketDataCollectionWorker" }
    )

    foreach ($entry in $entries) {
        $override = [System.Environment]::GetEnvironmentVariable($entry.Override)
        $path = if ([string]::IsNullOrWhiteSpace($override)) { Join-Path $defaultRoot $entry.Default } else { $override }
        [pscustomobject]@{
            Name        = $entry.Name
            Path        = $path
            Override    = $entry.Override
            Overridden  = -not [string]::IsNullOrWhiteSpace($override)
            Kind        = $entry.Kind
            Owner       = $entry.Owner
            Exists      = Test-Path $path
        }
    }

    # Exclusion watchlist lives under the API content root, not LocalApplicationData. It has no override.
    $exclusions = Join-Path $apiRoot "App_Data\exclusions-watchlist.json"
    [pscustomobject]@{
        Name = "exclusions-watchlist"; Path = $exclusions; Override = $null; Overridden = $false
        Kind = "rewritten JSON (tracked in git)"; Owner = "ExclusionWatchlistStore"; Exists = Test-Path $exclusions
    }
}
