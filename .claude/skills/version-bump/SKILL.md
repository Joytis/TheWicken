---
name: version-bump
description: Bump the mod version and author player-facing patch notes for the next Steam Workshop release. Use when the user says "version bump", "bump the version", "cut a release", "write patch notes", or is preparing a Workshop upload.
---

# Version Bump + Patch Notes

Prepares a Workshop release: pick the new version, bump it where it lives, and write legible player-facing patch notes covering everything since the last release.

## Version source of truth

`TheWitch.json` → `"version"` (format `vX.Y.Z`). This is the ONLY place to edit.
Derived/do-not-touch: `workshop/workshop.json` `changeNote` (rewritten by `tools/bundle-workshop.ps1` on upload), staged `workshop/content/TheWitch.json` (synced by the bundle script), `pages/analytics-data/*` (data).

## Protocol

### 1. Find the last release point

In priority order:
1. `git tag --sort=-creatordate` — if a `vX.Y.Z` tag exists, the newest one is the last release ref. (Tags are the convention going forward; created in step 5.)
2. No tag: read the tail + `LastWriteTime` of `tools/ModUploader-win-x64/mod-uploader.log` (last successful upload timestamp), then match it against `git log --format="%h %ci %s"` — the newest commit BEFORE the upload time is the released commit.

Report the found version, ref, and date to the user.

### 2. Choose the new version

If the user didn't specify, ask (AskUserQuestion) with suggested options: patch bump (`Z+1`) for balance/fixes, minor bump (`Y+1`, Z→0) for new content/mechanics. Versions always carry the `v` prefix.

### 3. Bump

Edit `TheWitch.json` `"version"`. Nothing else.

### 4. Author patch notes

Diff the release ref against the working tree (committed + uncommitted): `git diff <ref> -- TheWitchCode TheWitch/localization TheWitch/data`. Write `Docs/patch-notes/vX.Y.Z.md`.

Style rules:
- **Player-facing only.** Skip refactors, art-pipeline changes, internal renames, doc/tooling work. A localization text change matters only if it reflects a mechanic change (or is funny flavor worth calling out).
- Group under `## Reworks` / `## Buffs` / `## Nerfs` / `## New` / `## Other` (omit empty sections). One bullet per card/relic/potion, **bold name**, en-dash, then the change.
- Numbers as `old → new` (e.g. `Damage 9 → 7`). Upgrade changes in parentheses.
- Classify buffs/nerfs by *player power*, not by direction of the number. When a change is genuinely mixed, put it under Reworks.
- Lead the file with a one-line theme summary of the release.
- Uncertain intent on a change? Ask the user rather than guessing which bucket.

### 5. Finish

- `node Docs/card-data/regen.js --check` — if stale, run the full regen (card mechanics changed without a regen).
- Show the user the notes, then remind them of the release flow:
  1. Commit everything (notes + bump).
  2. Tag: `git tag vX.Y.Z` (+ `git push --tags`) — this is what step 1 relies on next time.
  3. Upload: `./tools/bundle-workshop.ps1 -Upload -ChangeNote "<notes>"` — Steam change notes render BBCode, not markdown; either paste a short plain-text summary or hand-convert (`[b]`, `[list]`/`[*]`). Do NOT run the upload yourself; it is outward-facing.

## Notes

- Patch-notes history lives in `Docs/patch-notes/` — one file per release, never rewrite old ones.
- If a `Docs/patch-notes/vX.Y.Z.md` already exists for the chosen version (drafted mid-cycle), update it against the latest diff instead of starting over.
