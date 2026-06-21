<#
.SYNOPSIS
    Launch N local Slay the Spire 2 instances for multiplayer testing:
    1 host (-fastmp host_standard) + (N-1) clients (-fastmp join).

.DESCRIPTION
    The first instance hosts. Each additional client gets a unique -clientId
    starting at 1000 (1000, 1001, 1002, ...), as required for 3+ players.

    The game is launched directly (no Steam) via the steam_appid.txt next to
    the executable. Start-Process sets the working directory to the game folder
    so the game finds steam_appid.txt and its data dir.

.PARAMETER Players
    Total number of instances to launch (host + clients). Default 2.

.PARAMETER Sts2Path
    Path to the Slay the Spire 2 install folder (containing SlayTheSpire2.exe).
    Auto-discovered from the Steam library registry if not given.

.EXAMPLE
    ./launch-mp.ps1 -Players 4
#>
param(
    [int]$Players = 2,
    [string]$Sts2Path = "",
    [switch]$Solo,
    [int]$DelayMs = 0
)

$ErrorActionPreference = "Stop"

if ($Players -lt 1) { throw "Players must be >= 1 (got $Players)." }

# --- Resolve the game install folder ---------------------------------------
function Resolve-Sts2Path {
    param([string]$Override)
    if ($Override) { return $Override }

    $candidates = @()
    try {
        $steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction Stop).SteamPath
        if ($steam) { $candidates += (Join-Path $steam 'steamapps\common\Slay the Spire 2') }
    } catch {}
    $candidates += 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'

    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c 'SlayTheSpire2.exe')) { return $c }
    }
    throw "Could not find SlayTheSpire2.exe. Pass -Sts2Path explicitly."
}

$gameDir = Resolve-Sts2Path -Override $Sts2Path
$exe     = Join-Path $gameDir 'SlayTheSpire2.exe'
$appId   = Join-Path $gameDir 'steam_appid.txt'

if (-not (Test-Path $exe))   { throw "Executable not found: $exe" }
if (-not (Test-Path $appId)) {
    Write-Warning "steam_appid.txt missing next to exe; creating it (2868840)."
    Set-Content -Path $appId -Value '2868840' -NoNewline -Encoding ascii
}

Write-Host "Game dir : $gameDir"

# --- Solo (no multiplayer) --------------------------------------------------
if ($Solo) {
    Write-Host "[solo ] launching single instance (no -fastmp)"
    Start-Process -FilePath $exe -WorkingDirectory $gameDir
    if ($DelayMs -gt 0) {
        Write-Host "Waiting ${DelayMs}ms for the runtime to come up (debugger attach)..."
        Start-Sleep -Milliseconds $DelayMs
    }
    Write-Host "Launched 1 solo instance."
    return
}

Write-Host "Players  : $Players (1 host + $($Players - 1) client(s))"

# --- Launch host ------------------------------------------------------------
Write-Host "[host ] -fastmp host_standard"
Start-Process -FilePath $exe -WorkingDirectory $gameDir -ArgumentList @('-fastmp','host_standard')

# --- Launch clients ---------------------------------------------------------
# clientId starts at 1000; the default join clientId is 1000 so the first
# client could omit it, but passing it explicitly keeps every id unique.
for ($i = 0; $i -lt ($Players - 1); $i++) {
    $cid = 1000 + $i
    Write-Host "[cli $cid] -fastmp join -clientId $cid"
    Start-Process -FilePath $exe -WorkingDirectory $gameDir -ArgumentList @('-fastmp','join','-clientId',"$cid")
    Start-Sleep -Milliseconds 750   # brief stagger so the host is up first
}

Write-Host "Launched $Players instance(s)."
