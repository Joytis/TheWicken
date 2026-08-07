# Creates NTFS junctions at the repo root pointing into gamedata/ so the Godot
# editor can resolve base-game res:// paths referenced by mod scenes.
# Idempotent: skips links that already exist, errors on real dirs in the way.
# Requires gamedata/ (the local decompiled game source) to be present.
# No admin needed — junctions don't require Developer Mode.

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path (Join-Path $repoRoot 'gamedata'))) {
    Write-Error "gamedata/ not found at $repoRoot. Decompile the game there first (see CLAUDE.md)."
}

foreach ($name in 'scenes', 'materials', 'shaders', 'images') {
    $link = Join-Path $repoRoot $name
    $target = Join-Path $repoRoot "gamedata\$name"

    if (Test-Path $link) {
        $item = Get-Item $link -Force
        if ($item.LinkType -eq 'Junction') {
            Write-Host "ok:      $name -> $($item.Target)"
        } else {
            Write-Error "$link exists and is not a junction. Remove it manually, then re-run."
        }
        continue
    }

    New-Item -ItemType Junction -Path $link -Target $target | Out-Null
    Write-Host "created: $name -> $target"
}
