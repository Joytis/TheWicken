<#
.SYNOPSIS
    Bundle the built mod into a Steam Workshop workspace and (optionally) upload it
    via MegaCrit's ModUploader.

.DESCRIPTION
    Produces an upload-ready workspace under <repo>/workshop and stages the
    deployable mod files into workshop/content. By default it first runs
    `dotnet publish` so the .pck is freshly exported, then copies the build
    outputs (TheWitch.json/.dll/.pck) from the game's mods folder into the
    workspace.

    Workspace layout consumed by ModUploader:
        workshop.json   metadata (title, description, visibility, tags, ...)   [versioned]
        description.md  Workshop description source (markdown)                 [versioned]
        image.png       Steam Workshop thumbnail                               [versioned]
        mod_id.txt      published item id, written after first upload          [versioned]
        content/        the mod files that actually get uploaded               [gitignored]

    workshop.json and image.png are hand-maintained and never clobbered; this
    script only stages content/ and (optionally) patches named metadata fields.
    If description.md exists, it is the source of truth for the description:
    every staging run converts it to Steam BBCode and writes it into
    workshop.json "description" (Steam renders BBCode, not markdown).

    Staging is the default. Uploading is opt-in (-Upload) because publishing to
    the Workshop is outward-facing and not trivially reversible.

    After a successful upload, a Discord notification is posted if a webhook is
    configured via $env:DISCORD_WEBHOOK_URL or tools/discord-webhook.local.txt
    (gitignored; file contains just the webhook URL).

.PARAMETER SkipPublish
    Skip `dotnet publish`; reuse whatever is already in the mods folder.
    The script warns if the staged .pck looks older than the source tree.

.PARAMETER Upload
    After staging, invoke `ModUploader upload`. Requires Steam to be running and
    logged in. Without this flag the script only stages and prints the command.

.PARAMETER ChangeNote
    Text written to workshop.json "changeNote" before uploading.

.PARAMETER Visibility
    Override workshop.json "visibility" (private | friends | friendsonly | public).

.PARAMETER IncludePdb
    Also copy TheWitch.pdb into content/ (debug symbols). Off by default.

.PARAMETER Workspace
    Workspace directory. Defaults to <repo>/workshop.

.PARAMETER ModsPath
    The game's mods/ folder. Auto-resolved from the build (msbuild ModsPath) if omitted.

.PARAMETER Uploader
    Path to ModUploader.exe. Defaults to tools/ModUploader-win-x64/ModUploader.exe.

.EXAMPLE
    ./bundle-workshop.ps1
    # publish + stage into workshop/content, print the upload command

.EXAMPLE
    ./bundle-workshop.ps1 -SkipPublish
    # stage current mods/ outputs without rebuilding

.EXAMPLE
    ./bundle-workshop.ps1 -Upload -ChangeNote "Phase 3 cards + potion brewing"
    # publish, stage, then upload to the Workshop
#>
[CmdletBinding()]
param(
    [switch]$SkipPublish,
    [switch]$Upload,
    [string]$ChangeNote = "",
    [ValidateSet("", "private", "friends", "friendsonly", "public")]
    [string]$Visibility = "",
    [switch]$IncludePdb,
    [string]$Workspace = "",
    [string]$ModsPath = "",
    [string]$Uploader = ""
)

$ErrorActionPreference = "Stop"

# Repo root is the parent of this tools/ folder.
$repoRoot = Split-Path -Parent $PSScriptRoot
$csproj   = Join-Path $repoRoot 'TheWitch.csproj'
$manifest = Join-Path $repoRoot 'TheWitch.json'

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Warn2($msg) { Write-Host "[warn] $msg" -ForegroundColor Yellow }

# Convert a small markdown subset to Steam Workshop BBCode.
# Supported: # / ## / ### headings, - or * bullet lists, --- rules,
# **bold**, *italic*, `code` (as bold), [text](http-url) links.
function Convert-MarkdownToBBCode([string]$md) {
    $md = $md -replace "`r`n", "`n"
    $out = New-Object System.Collections.Generic.List[string]
    $inList = $false
    foreach ($line in ($md -split "`n")) {
        if ($line -match '^\s*[-*]\s+(.*)$') {
            if (-not $inList) { $out.Add('[list]'); $inList = $true }
            $out.Add('[*]' + $Matches[1])
            continue
        }
        if ($inList) { $out.Add('[/list]'); $inList = $false }
        if     ($line -match '^###\s+(.*)$') { $out.Add('[h3]' + $Matches[1] + '[/h3]') }
        elseif ($line -match '^##\s+(.*)$')  { $out.Add('[h2]' + $Matches[1] + '[/h2]') }
        elseif ($line -match '^#\s+(.*)$')   { $out.Add('[h1]' + $Matches[1] + '[/h1]') }
        elseif ($line -match '^\s*(---+|___+)\s*$') { $out.Add('[hr][/hr]') }
        else { $out.Add($line) }
    }
    if ($inList) { $out.Add('[/list]') }
    $text = $out -join "`n"
    # Links before bold/italic so bracketed BBCode tags can't be mistaken for link text.
    $text = [regex]::Replace($text, '\[([^\]]+)\]\((https?://[^)\s]+)\)', '[url=$2]$1[/url]')
    $text = [regex]::Replace($text, '\*\*(.+?)\*\*', '[b]$1[/b]')
    $text = [regex]::Replace($text, '(?<!\*)\*([^*\n]+)\*(?!\*)', '[i]$1[/i]')
    $text = [regex]::Replace($text, '`([^`\n]+)`', '[b]$1[/b]')
    return $text.Trim()
}

# --- Read the mod manifest -------------------------------------------------
if (-not (Test-Path $manifest)) { throw "Mod manifest not found: $manifest" }
$mod = Get-Content $manifest -Raw | ConvertFrom-Json
$modId = $mod.id
if (-not $modId) { throw "Manifest has no 'id': $manifest" }

# --- Resolve the ModUploader executable ------------------------------------
if (-not $Uploader) {
    $Uploader = Join-Path $PSScriptRoot 'ModUploader-win-x64/ModUploader.exe'
}
if (-not (Test-Path $Uploader)) { throw "ModUploader.exe not found: $Uploader" }
$uploaderDir = Split-Path -Parent $Uploader

# --- Resolve the workspace -------------------------------------------------
if (-not $Workspace) { $Workspace = Join-Path $repoRoot 'workshop' }

# --- Step 1: publish (default) ---------------------------------------------
if ($SkipPublish) {
    Write-Step "Skipping publish (-SkipPublish); reusing existing mods/ outputs."
} else {
    Write-Step "Publishing ($csproj) -- this exports the .pck via headless Godot..."
    & dotnet publish $csproj
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)." }
}

# --- Resolve the mods source folder ----------------------------------------
if (-not $ModsPath) {
    Write-Step "Resolving mods path from build..."
    $ModsPath = (& dotnet msbuild $csproj -getProperty:ModsPath -nologo 2>$null | Select-Object -Last 1)
    if ($ModsPath) { $ModsPath = $ModsPath.Trim() }
}
if (-not $ModsPath) {
    throw "Could not resolve the mods folder. Pass -ModsPath '<Sts2>/mods'."
}
$modSrc = Join-Path $ModsPath $modId
if (-not (Test-Path $modSrc)) {
    throw "Built mod folder not found: $modSrc (build the mod first, or omit -SkipPublish)."
}

# --- Step 2: scaffold the workspace if missing -----------------------------
$seededNew = $false
if (-not (Test-Path $Workspace)) {
    Write-Step "Creating workspace via ModUploader new -w '$Workspace'..."
    & $Uploader new -w $Workspace
    if ($LASTEXITCODE -ne 0) { throw "ModUploader new failed (exit $LASTEXITCODE)." }
    $seededNew = $true
}

$workshopJson = Join-Path $Workspace 'workshop.json'
$contentDir   = Join-Path $Workspace 'content'
$modIdFile    = Join-Path $Workspace 'mod_id.txt'

# Seed workshop.json from the manifest only on first scaffold.
if ($seededNew -and (Test-Path $workshopJson)) {
    Write-Step "Seeding workshop.json from manifest..."
    $deps = @()
    if ($mod.dependencies) { $deps = @($mod.dependencies | ForEach-Object { $_.id }) }
    $ws = [ordered]@{
        title        = $mod.name
        description  = $mod.description
        visibility   = "private"
        changeNote   = "Initial upload"
        tags         = @()
        dependencies = $deps
    }
    $json = $ws | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($workshopJson, $json, (New-Object System.Text.UTF8Encoding($false)))
    Write-Warn2 "Edit $workshopJson (title/description/tags) and replace image.png before your first public upload."
}

# --- Step 3 & 4: stage content/ --------------------------------------------
Write-Step "Staging content/ from $modSrc ..."
$pck = Join-Path $modSrc "$modId.pck"
$dll = Join-Path $modSrc "$modId.dll"
$json = Join-Path $modSrc "$modId.json"
foreach ($f in @($json, $dll, $pck)) {
    if (-not (Test-Path $f)) {
        throw "Required mod file missing: $f`nRun a full 'dotnet publish' first (the .pck only comes from publish, not build)."
    }
}

# Stale check (only meaningful when reusing outputs).
if ($SkipPublish) {
    $pckTime = (Get-Item $pck).LastWriteTimeUtc
    $newestSrc = Get-ChildItem -Path (Join-Path $repoRoot 'TheWitchCode'), (Join-Path $repoRoot 'TheWitch') -Recurse -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if ($newestSrc -and $newestSrc.LastWriteTimeUtc -gt $pckTime) {
        Write-Warn2 "Staged .pck ($pckTime UTC) is older than $($newestSrc.Name). Outputs may be stale -- drop -SkipPublish to rebuild."
    }
}

# Rebuild content/ clean.
if (Test-Path $contentDir) { Remove-Item -Path (Join-Path $contentDir '*') -Recurse -Force }
else { New-Item -ItemType Directory -Path $contentDir | Out-Null }

Copy-Item $json -Destination $contentDir
Copy-Item $dll  -Destination $contentDir
Copy-Item $pck  -Destination $contentDir

# Keep the staged manifest's version in sync with the repo manifest (matters
# for -SkipPublish -Upload, where the mods-folder copy predates the bump).
$stagedJson = Join-Path $contentDir "$modId.json"
$staged = Get-Content $stagedJson -Raw | ConvertFrom-Json
if ($staged.version -ne $mod.version) {
    Write-Step "Syncing staged manifest version -> $($mod.version)"
    $raw = Get-Content $stagedJson -Raw
    $raw = $raw -replace ('"version":\s*"' + [regex]::Escape($staged.version) + '"'), ('"version": "' + $mod.version + '"')
    [System.IO.File]::WriteAllText($stagedJson, $raw, (New-Object System.Text.UTF8Encoding($false)))
}
if ($IncludePdb) {
    $pdb = Join-Path $modSrc "$modId.pdb"
    if (Test-Path $pdb) { Copy-Item $pdb -Destination $contentDir }
    else { Write-Warn2 "-IncludePdb set but $pdb not found; skipping." }
}
Write-Host "    staged:" (Get-ChildItem $contentDir | ForEach-Object { $_.Name }) -ForegroundColor DarkGray

# --- Step 5: patch workshop.json metadata ----------------------------------
# changeNote is always rewritten on upload runs: workshop.json is versioned, so
# leaving it untouched would resend the PREVIOUS upload's note as this update's
# patch notes. No -ChangeNote => blank note (no patch-note text on Steam).
# description.md (if present) is the description's source of truth: converted
# to Steam BBCode and synced into workshop.json on every staging run.
$descriptionMd = Join-Path $Workspace 'description.md'
$hasDescMd = Test-Path $descriptionMd
if (($ChangeNote -ne "") -or ($Visibility -ne "") -or $Upload -or $hasDescMd) {
    if (-not (Test-Path $workshopJson)) { throw "workshop.json missing: $workshopJson" }
    $ws = Get-Content $workshopJson -Raw | ConvertFrom-Json
    if ($hasDescMd) {
        $bbDesc = Convert-MarkdownToBBCode (Get-Content $descriptionMd -Raw)
        if ($ws.description -ne $bbDesc) {
            $ws.description = $bbDesc
            Write-Step "description synced from description.md (converted to BBCode)"
        }
    }
    if ($ChangeNote -ne "") { $ws.changeNote = $ChangeNote; Write-Step "changeNote = '$ChangeNote'" }
    elseif ($Upload -and $ws.changeNote -ne "") {
        $ws.changeNote = ""
        Write-Warn2 "No -ChangeNote given; clearing stale changeNote so it isn't resent as this update's patch notes."
    }
    if ($Visibility -ne "") { $ws.visibility = $Visibility; Write-Step "visibility = '$Visibility'" }
    $json = $ws | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($workshopJson, $json, (New-Object System.Text.UTF8Encoding($false)))
}

# --- Step 6: sanity checks -------------------------------------------------
$templateImg = Join-Path $uploaderDir 'template/image.png'
$wsImg = Join-Path $Workspace 'image.png'
if ((Test-Path $templateImg) -and (Test-Path $wsImg)) {
    if ((Get-Item $templateImg).Length -eq (Get-Item $wsImg).Length) {
        Write-Warn2 "image.png is still the ModUploader placeholder -- replace $wsImg before a public release."
    }
}
$hasModId = Test-Path $modIdFile

# --- Step 7: upload or print next step -------------------------------------
if ($Upload) {
    if (-not (Get-Process -Name steam -ErrorAction SilentlyContinue)) {
        throw "Steam is not running. Start Steam (logged in) before uploading."
    }
    if (-not $hasModId) {
        Write-Warn2 "No mod_id.txt found -- this will CREATE A NEW Workshop item. Commit the resulting mod_id.txt afterward."
    }
    Write-Step "Uploading to Steam Workshop..."
    $proc = Start-Process -FilePath $Uploader `
        -ArgumentList @('upload', '-w', "`"$Workspace`"") `
        -WorkingDirectory $uploaderDir -NoNewWindow -Wait -PassThru
    if ($proc.ExitCode -ne 0) { throw "ModUploader upload failed (exit $($proc.ExitCode))." }
    Write-Step "Upload complete."
    if ((-not $hasModId) -and (Test-Path $modIdFile)) {
        Write-Host "    new workshop item id: $(Get-Content $modIdFile -Raw)" -ForegroundColor Green
    }

    # --- Step 8: Discord notification ----------------------------------------
    # Webhook URL comes from $env:DISCORD_WEBHOOK_URL or tools/discord-webhook.local.txt
    # (gitignored). If neither is set, skip silently; a failed POST warns but
    # never fails the upload.
    $webhookUrl = $env:DISCORD_WEBHOOK_URL
    $webhookFile = Join-Path $PSScriptRoot 'discord-webhook.local.txt'
    if ((-not $webhookUrl) -and (Test-Path $webhookFile)) {
        $webhookUrl = (Get-Content $webhookFile -Raw).Trim()
    }
    if ($webhookUrl) {
        Write-Step "Posting update notification to Discord..."
        $ws = Get-Content $workshopJson -Raw | ConvertFrom-Json
        $note = if ($ws.changeNote) { $ws.changeNote } else { "(no change note)" }
        $desc = "**Change note:** $note`n**Version:** $($mod.version)"
        if (Test-Path $modIdFile) {
            $itemId = (Get-Content $modIdFile -Raw).Trim()
            $desc += "`n[View on Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=$itemId)"
        }
        $payload = @{
            embeds = @(@{
                title       = "$($ws.title) updated on the Steam Workshop"
                description = $desc
                color       = 9534463
            })
        } | ConvertTo-Json -Depth 10
        try {
            Invoke-RestMethod -Uri $webhookUrl -Method Post -ContentType 'application/json' -Body $payload | Out-Null
            Write-Host "    Discord notified." -ForegroundColor Green
        } catch {
            Write-Warn2 "Discord notification failed: $($_.Exception.Message)"
        }
    } else {
        Write-Warn2 "No Discord webhook configured (set DISCORD_WEBHOOK_URL or create tools/discord-webhook.local.txt); skipping notification."
    }
} else {
    Write-Step "Staged. To upload, run:"
    Write-Host "    ./tools/bundle-workshop.ps1 -Upload" -ForegroundColor Green
    Write-Host "    (or directly: `"$Uploader`" upload -w `"$Workspace`")" -ForegroundColor DarkGray
    if (-not $hasModId) { Write-Warn2 "First upload will create a new Workshop item and write mod_id.txt." }
}
