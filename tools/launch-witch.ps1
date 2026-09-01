<#
.SYNOPSIS
    Witch dev launcher for Slay the Spire 2. Default: one solo instance (with
    optional mod debug flags). Pass -Players N for multiplayer testing
    (1 host [-fastmp host_standard] + N-1 clients [-fastmp join]).

.DESCRIPTION
    In multiplayer mode the first instance hosts. Each additional client gets a
    unique -clientId starting at 1000 (1000, 1001, 1002, ...), as required for
    3+ players.

    The game is launched directly (no Steam) via the steam_appid.txt next to
    the executable. Start-Process sets the working directory to the game folder
    so the game finds steam_appid.txt and its data dir.

.PARAMETER Players
    Total number of instances to launch (host + clients). Omitted = one solo
    instance; providing it switches to multiplayer mode. (-Solo still forces
    solo and is what the VS Code tasks pass explicitly.)

.PARAMETER Sts2Path
    Path to the Slay the Spire 2 install folder (containing SlayTheSpire2.exe).
    Auto-discovered from the Steam library registry if not given.

.EXAMPLE
    ./launch-witch.ps1                # one solo instance
    ./launch-witch.ps1 -Build publish -WitchBootstrap   # publish, then straight into combat
    ./launch-witch.ps1 -TestUpdatePopup
    ./launch-witch.ps1 -Players 4    # 1 host + 3 clients
#>
param(
    [int]$Players = 2,
    [string]$Sts2Path = "",
    [switch]$Solo,
    [int]$DelayMs = 0,
    # Solo-only debug launch modes (see TheWitchCode/Debug/WitchDebug.cs):
    [switch]$WitchBootstrap,   # -witch-debug -witch-bootstrap: skip menu, enter combat with 100 energy
    [switch]$AutoSlay,         # -witch-debug -autoslay: run the smoke-test bot
    [switch]$Headless,         # solo only: pass Godot --headless (no window/GPU); waits for
                               # exit and propagates the game's exit code (AutoSlay: 0=run done, 1=fail)
    [switch]$FxLab,            # -witch-debug -witch-fxlab: open the SFX/VFX browser scene
    [switch]$IconLab,          # -witch-debug -witch-iconlab: open the relic/potion icon browser scene
    [switch]$CardTest,         # -witch-debug -witch-cardtest: headless smoke test that plays every Witch card (WitchCardTest)
    [switch]$PotionTest,       # -witch-debug -witch-potiontest: uses + discards every Witch potion
    [switch]$RelicTest,        # -witch-debug -witch-relictest: equips every Witch relic, then every card + potion
    [switch]$TestAll,          # -witch-debug -witch-testall: cards, potions, then relics in one run
    [string]$Encounter = "",   # optional encounter id for -WitchBootstrap (e.g. SLIMES_WEAK)
    [switch]$TestUpdatePopup,          # -witch-test-update-popup: show the Workshop-update restart popup (no Steam calls)
    [switch]$ForceWorkshopDownload,    # -witch-force-workshop-download=<id>: force the Workshop download path;
                                       # item id read from workshop/mod_id.txt (local builds need it)
    [switch]$TailLog,          # solo only: stream %appdata%\SlayTheSpire2\logs\godot.log to this console
    [ValidateSet('none','build','publish')]
    [string]$Build = 'none'    # run `dotnet build` (code deploy) or `dotnet publish` (code + .pck) first; abort on failure
)

$ErrorActionPreference = "Stop"

if ($Players -lt 1) { throw "Players must be >= 1 (got $Players)." }

# --- Optional build step (build = deploy dll/json; publish = also export the .pck) ---
if ($Build -ne 'none') {
    $repoRoot = Split-Path $PSScriptRoot -Parent
    $csproj = Join-Path $repoRoot 'TheWitch.csproj'
    Write-Host "[$Build] dotnet $Build $csproj"
    & dotnet $Build $csproj
    if ($LASTEXITCODE -ne 0) { throw "dotnet $Build failed (exit $LASTEXITCODE); not launching." }
}

# --- Smoke-test report: skim the tailed log, errors loud, passes routine ---------
# One harness (WitchCardTest) behind three flags; $tag picks the log prefix to parse.
$SmokeTag = if ($TestAll) { '[witch-testall]' } elseif ($RelicTest) { '[witch-relictest]' } elseif ($PotionTest) { '[witch-potiontest]' } else { '[witch-cardtest]' }
$SmokeTest = $CardTest -or $PotionTest -or $RelicTest -or $TestAll
function Write-CardTestReport {
    param([System.Collections.Generic.List[string]]$lines, [string]$tag = $SmokeTag)
    $rtag = [regex]::Escape($tag)
    $started  = @($lines | Where-Object { $_ -match "\[INFO\] \[AutoSlay\] $rtag (\w+)$" } | ForEach-Object { $Matches[1] })
    $failed   = @($lines | Where-Object { $_ -match "$rtag FAILED (\w+): (.*)$" } |
                 ForEach-Object { [pscustomobject]@{ Card = $Matches[1]; Message = $Matches[2] } })
    # Stray errors = every [ERROR] line the harness itself did not emit (game-side exceptions,
    # missing resources, ...). Keyed by the first line so a repeated stack trace counts once.
    $errors   = @($lines | Where-Object { $_ -match '^\[ERROR\]' -and $_ -notmatch [regex]::Escape($tag) })
    $errGroups = $errors | Group-Object | Sort-Object Count -Descending
    $passed = $started.Count - $failed.Count

    $red = 'Red'; $yellow = 'Yellow'; $green = 'Green'; $dim = 'DarkGray'
    Write-Host ''
    Write-Host "================ SMOKE TEST REPORT $tag ================" -ForegroundColor Cyan
    if ($errors.Count -gt 0) {
        Write-Host "!! $($errors.Count) stray [ERROR] line(s) during the run ($($errGroups.Count) distinct):" -ForegroundColor $red
        foreach ($g in $errGroups) {
            $msg = $g.Name -replace '^\[ERROR\]\s*', ''
            if ($msg.Length -gt 200) { $msg = $msg.Substring(0, 200) + '...' }
            Write-Host ("  {0,4}x  {1}" -f $g.Count, $msg) -ForegroundColor $red
        }
    }
    if ($failed.Count -gt 0) {
        Write-Host "!! $($failed.Count) item(s) FAILED:" -ForegroundColor $red
        foreach ($f in $failed) { Write-Host ("  - {0}: {1}" -f $f.Card, $f.Message) -ForegroundColor $yellow }
    }
    if ($errors.Count -eq 0 -and $failed.Count -eq 0) {
        Write-Host "No errors, no failures." -ForegroundColor $green
    }
    Write-Host ("Items: {0} run, {1} passed, {2} failed | stray errors: {3}" -f $started.Count, $passed, $failed.Count, $errors.Count) -ForegroundColor $(if ($failed.Count -or $errors.Count) { $yellow } else { $green })
    if ($started.Count -eq 0) { Write-Host "(no $tag lines seen - did the harness run? needs -witch-debug $($tag.Trim('[',']') -replace '^', '-'))" -ForegroundColor $dim }
    Write-Host '=======================================================================' -ForegroundColor Cyan
}

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
# Solo is the default; multiplayer only when -Players is given explicitly.
if ($Solo -or -not $PSBoundParameters.ContainsKey('Players')) {
    $gameArgs = @()
    if ($WitchBootstrap -or $AutoSlay -or $FxLab -or $IconLab -or $SmokeTest) {
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
    if ($SmokeTest) {
        if ('-witch-debug' -notin $gameArgs) { $gameArgs += '-witch-debug' }
        $gameArgs += '-' + $SmokeTag.Trim('[', ']')   # -witch-cardtest / -witch-potiontest / -witch-relictest / -witch-testall
    }
    if ($Headless) {
        # Godot engine flag: dummy display server, no window/render/GPU. The game is
        # headless-aware (Logger switches to console printing, graphics prefs skipped,
        # AutoSlay's UiHelper bypasses hover/focus checks).
        $gameArgs += '--headless'
    }
    # Workshop self-update debug flags (not gated on -witch-debug; handled in
    # WorkshopSelfUpdate.Initialize).
    if ($TestUpdatePopup) {
        $gameArgs += '-witch-test-update-popup'
    }
    if ($ForceWorkshopDownload) {
        $modIdFile = Join-Path $PSScriptRoot '..\workshop\mod_id.txt'
        if (Test-Path $modIdFile) {
            $itemId = (Get-Content $modIdFile -Raw).Trim()
            $gameArgs += "-witch-force-workshop-download=$itemId"
        } else {
            Write-Warning "workshop/mod_id.txt not found; passing the flag without an item id (only works for a Workshop-loaded install)."
            $gameArgs += '-witch-force-workshop-download'
        }
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

    # --- Headless without a tail: block until the game exits, report the code ---
    if ($Headless -and -not $TailLog) {
        Write-Host "[headless] waiting for game exit (AutoSlay run cap is 25 min)..."
        $proc.WaitForExit()
        Write-Host "[headless] game exited (code $($proc.ExitCode))"
        exit $proc.ExitCode
    }

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
        $captured = [System.Collections.Generic.List[string]]::new()
        try {
            while (-not $proc.HasExited) {
                $line = $sr.ReadLine()
                if ($null -ne $line) { Write-Host $line; if ($SmokeTest) { $captured.Add($line) } } else { Start-Sleep -Milliseconds 200 }
            }
            while ($null -ne ($line = $sr.ReadLine())) { Write-Host $line; if ($SmokeTest) { $captured.Add($line) } }
        } finally { $sr.Dispose() }
        Write-Host "--- game exited (code $($proc.ExitCode)) ---"
        if ($SmokeTest) { Write-CardTestReport $captured }
        if ($Headless) { exit $proc.ExitCode }
    }
    return
}

if ($Headless) { Write-Warning "-Headless is solo-only; ignoring for multiplayer launch." }

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
