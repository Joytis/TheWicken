<#
.SYNOPSIS
    Common entry point for Supabase analytics query operations.

.DESCRIPTION
    Dispatches to the python scripts in tools/analytics/. Add new modes by extending
    the switch below with a new script.

    Read access uses the service_role key from supabase-service-key.local.txt
    (gitignored, next to this script) or the SUPABASE_READ_KEY env var.

.EXAMPLE
    ./tools/analytics/analytics.ps1 -Mode win-rate
    ./tools/analytics/analytics.ps1 -Mode win-rate -ModVersion 1.0.0 -DaysBack 30
    ./tools/analytics/analytics.ps1 -Mode card-ascension -CardName RITUAL_SACRIFICE
    ./tools/analytics/analytics.ps1 -Mode seed -Runs 20
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('win-rate', 'card-ascension', 'card-pick-rate', 'card-win-rate', 'seed')]
    [string]$Mode,

    # Filter to one mod release (the "version" field in TheWitch.json), e.g. 1.0.0. Default: all.
    [string]$ModVersion,

    # Filter to one Slay the Spire 2 build, e.g. 1.2.3. Default: all.
    [string]$GameVersion,

    # Only look at runs uploaded in the last N days. Blank/0 = all time.
    # (String so a blanked-out VS Code task prompt doesn't fail int binding.)
    [string]$DaysBack,

    # card-ascension mode: card entry id as uploaded, e.g. RITUAL_SACRIFICE.
    [string]$CardName,

    # seed mode: how many fabricated runs to insert.
    [int]$Runs = 20
)

$scriptDir = $PSScriptRoot

function Add-CommonFilters([System.Collections.Generic.List[string]]$argList) {
    if ($ModVersion)  { $argList.Add('--mod-version');  $argList.Add($ModVersion) }
    if ($GameVersion) { $argList.Add('--game-version'); $argList.Add($GameVersion) }
    if ($DaysBack -and [int]$DaysBack -gt 0) { $argList.Add('--days-back'); $argList.Add($DaysBack) }
}

# Run a plot script, echoing its output, then open the chart it reports having written.
function Invoke-Plot([System.Collections.Generic.List[string]]$pyArgs) {
    $output = py @pyArgs
    $output | ForEach-Object { $_ }
    $written = ($output | Where-Object { $_ -like 'Wrote *' } | Select-Object -Last 1) -replace '^Wrote ', ''
    if ($written -and (Test-Path $written)) { Start-Process $written }
}

switch ($Mode) {
    'win-rate' {
        $pyArgs = [System.Collections.Generic.List[string]]::new()
        $pyArgs.Add((Join-Path $scriptDir 'plot_card_winrate.py'))
        Add-CommonFilters $pyArgs
        Invoke-Plot $pyArgs
    }
    'card-ascension' {
        if (-not $CardName) { throw "card-ascension mode requires -CardName (card entry id, e.g. RITUAL_SACRIFICE)." }
        $pyArgs = [System.Collections.Generic.List[string]]::new()
        $pyArgs.Add((Join-Path $scriptDir 'plot_card_ascension.py'))
        $pyArgs.Add('--card'); $pyArgs.Add($CardName)
        Add-CommonFilters $pyArgs
        Invoke-Plot $pyArgs
    }
    { $_ -in 'card-pick-rate', 'card-win-rate' } {
        $pyArgs = [System.Collections.Generic.List[string]]::new()
        $pyArgs.Add((Join-Path $scriptDir 'plot_card_bars.py'))
        $pyArgs.Add('--metric'); $pyArgs.Add(($Mode -replace '^card-', ''))
        Add-CommonFilters $pyArgs
        Invoke-Plot $pyArgs
    }
    'seed' {
        py (Join-Path $scriptDir 'seed_test_runs.py') --runs $Runs
    }
}
exit $LASTEXITCODE
