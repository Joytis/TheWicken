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
    ./tools/analytics/analytics.ps1 -Mode seed -Runs 20
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('win-rate', 'seed')]
    [string]$Mode,

    # Filter to one mod release (the "version" field in TheWitch.json), e.g. 1.0.0. Default: all.
    [string]$ModVersion,

    # Filter to one Slay the Spire 2 build, e.g. 1.2.3. Default: all.
    [string]$GameVersion,

    # Only look at runs uploaded in the last N days. Blank/0 = all time.
    # (String so a blanked-out VS Code task prompt doesn't fail int binding.)
    [string]$DaysBack,

    # seed mode: how many fabricated runs to insert.
    [int]$Runs = 20
)

$scriptDir = $PSScriptRoot

function Add-CommonFilters([System.Collections.Generic.List[string]]$argList) {
    if ($ModVersion)  { $argList.Add('--mod-version');  $argList.Add($ModVersion) }
    if ($GameVersion) { $argList.Add('--game-version'); $argList.Add($GameVersion) }
    if ($DaysBack -and [int]$DaysBack -gt 0) { $argList.Add('--days-back'); $argList.Add($DaysBack) }
}

switch ($Mode) {
    'win-rate' {
        $pyArgs = [System.Collections.Generic.List[string]]::new()
        $pyArgs.Add((Join-Path $scriptDir 'plot_card_winrate.py'))
        Add-CommonFilters $pyArgs
        py @pyArgs
    }
    'seed' {
        py (Join-Path $scriptDir 'seed_test_runs.py') --runs $Runs
    }
}
exit $LASTEXITCODE
