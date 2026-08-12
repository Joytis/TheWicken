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
    [int]$DelayMs = 0,
    # Solo-only debug launch modes (see TheWitchCode/Debug/WitchDebug.cs):
    [switch]$WitchBootstrap,   # -witch-debug -witch-bootstrap: skip menu, enter combat with 100 energy
    [switch]$AutoSlay,         # -witch-debug -autoslay: run the smoke-test bot
    [switch]$FxLab,            # -witch-debug -witch-fxlab: open the SFX/VFX browser scene
    [switch]$IconLab,          # -witch-debug -witch-iconlab: open the relic/potion icon browser scene
    [string]$Encounter = "",   # optional encounter id for -WitchBootstrap (e.g. SLIMES_WEAK)
    [switch]$TailLog           # solo only: stream %appdata%\SlayTheSpire2\logs\godot.log to this console
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
    $gameArgs = @()
    if ($WitchBootstrap -or $AutoSlay -or $FxLab -or $IconLab) {
        # Game-native dev switch: skips the intro logo (checked once at startup).
        # Child processes inherit the environment, so set it just for this launch.
        $env:STS2_DEV_SKIP = '1'
    }
    if ($WitchBootstrap) {
        $bootstrapArg = if ($Encounter) { "-witch-bootstrap=$Encounter" } else { '-witch-bootstrap' }
        $gameArgs += @('-witch-debug', $bootstrapArg)
    }
    if ($AutoSlay) {
        if ('-witch-debug' -notin $gameArgs) { $gameArgs += '-witch-debug' }
        $gameArgs += '-autoslay'
    }
    if ($FxLab) {
        if ('-witch-debug' -notin $gameArgs) { $gameArgs += '-witch-debug' }
        $gameArgs += '-witch-fxlab'
    }
    if ($IconLab) {
        if ('-witch-debug' -notin $gameArgs) { $gameArgs += '-witch-debug' }
        $gameArgs += '-witch-iconlab'
    }
    $launchTime = Get-Date
    if ($gameArgs.Count -gt 0) {
        Write-Host "[solo ] launching single instance: $($gameArgs -join ' ')"
        $proc = Start-Process -FilePath $exe -WorkingDirectory $gameDir -ArgumentList $gameArgs -PassThru
    } else {
        Write-Host "[solo ] launching single instance (no -fastmp)"
        $proc = Start-Process -FilePath $exe -WorkingDirectory $gameDir -PassThru
    }
    if ($DelayMs -gt 0) {
        Write-Host "Waiting ${DelayMs}ms for the runtime to come up (debugger attach)..."
        Start-Sleep -Milliseconds $DelayMs
    }
    Write-Host "Launched 1 solo instance."

    # --- Live log tail (the game is a GUI app; its output only goes to godot.log) ---
    if ($TailLog) {
        $logFile = Join-Path $env:APPDATA 'SlayTheSpire2\logs\godot.log'
        # The game rotates the previous godot.log on startup; wait for the fresh one.
        while (-not $proc.HasExited -and
               (-not (Test-Path $logFile) -or (Get-Item $logFile).LastWriteTime -lt $launchTime)) {
            Start-Sleep -Milliseconds 250
        }
        if ($proc.HasExited) { Write-Host "Game exited before writing a log."; return }
        Write-Host "--- tailing $logFile (closes when the game exits) ---"
        # Share Read/Write/Delete so the game can keep writing and rotate freely.
        $fs = [System.IO.FileStream]::new($logFile, 'Open', 'Read', [System.IO.FileShare]'ReadWrite,Delete')
        $sr = [System.IO.StreamReader]::new($fs)
        try {
            while (-not $proc.HasExited) {
                $line = $sr.ReadLine()
                if ($null -ne $line) { Write-Host $line } else { Start-Sleep -Milliseconds 200 }
            }
            while ($null -ne ($line = $sr.ReadLine())) { Write-Host $line }
        } finally { $sr.Dispose() }
        Write-Host "--- game exited (code $($proc.ExitCode)) ---"
    }
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
