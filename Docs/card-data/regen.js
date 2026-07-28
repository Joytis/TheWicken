#!/usr/bin/env node
/*
 * regen.js — rebuild Docs/card-data/cards.json from the card source.
 *
 *   node Docs/card-data/regen.js          (rewrites cards.json + prints a report)
 *   node Docs/card-data/regen.js --check  (no write; exits 1 if drift found — for CI/pre-commit)
 *
 * It parses every card class in TheWitchCode/Cards (+ Familiar/) for the mechanical fields
 * (cost / type / rarity / target / numbers / upgrade) and the localization JSON for name + text.
 *
 * PRESERVED across runs (keyed by entry): `tested` flag and any curated `note`.
 * If a card's mechanical fingerprint CHANGES, its `tested` flag is auto-reset to false and the
 * change is reported — that is the "clear TESTED when the design changes" rule, automated.
 */
const fs = require("fs");
const path = require("path");

const ROOT = path.resolve(__dirname, "..", "..");                 // repo root
const CARDS_DIR = path.join(ROOT, "TheWitchCode", "Cards");
const LOC_PATH = path.join(ROOT, "TheWitch", "localization", "eng", "cards.json");
const OUT_PATH = path.join(__dirname, "cards.json");
const MOD_PREFIX = "THEWITCH-";

// Base classes / interfaces / registries — not real cards.
const SKIP = new Set(["WitchCard", "WitchFamiliarCard", "IFamiliarSummon", "FamiliarCardRegistry"]);
const RAR_ORDER = ["Starter", "Common", "Uncommon", "Rare", "Special", "Token"];
const RAR_MAP = { Basic: "Starter" }; // CardRarity.Basic is the starter rarity

// ---------- helpers ----------
const pascalToSnake = (s) =>
  s.replace(/([a-z0-9])([A-Z])/g, "$1_$2").replace(/([A-Z]+)([A-Z][a-z])/g, "$1_$2").toUpperCase();
const stripPower = (s) => s.replace(/Power$/, "");
const num = (s) => Number(String(s).replace(/m$/, ""));

function listCardFiles(dir) {
  const out = [];
  for (const name of fs.readdirSync(dir)) {
    const full = path.join(dir, name);
    if (fs.statSync(full).isDirectory()) { out.push(...listCardFiles(full)); continue; }
    if (name.endsWith(".cs")) out.push(full);
  }
  return out;
}

// ---------- C# parsing ----------
function parseCanonicalVars(src) {
  // Grab the CanonicalVars => [ ... ]; block.
  const m = src.match(/CanonicalVars\s*=>\s*\[([\s\S]*?)\]\s*;/);
  const vars = [];
  if (!m) return vars;
  const body = m[1];
  // PowerVar<FooPower>(6m)
  for (const pm of body.matchAll(/new\s+PowerVar<\s*(\w+)\s*>\s*\(\s*(-?\d+)m?/g)) {
    vars.push({ token: pm[1], name: stripPower(pm[1]), value: num(pm[2]) });
  }
  // DamageVar/BlockVar/CardsVar/RepeatVar/etc(6m, ...)  — skip PowerVar (handled above)
  for (const vm of body.matchAll(/new\s+(\w+)Var\s*\(\s*(-?\d+)m?/g)) {
    if (vm[1] === "Power") continue;
    vars.push({ token: vm[1], name: vm[1], value: num(vm[2]) });
  }
  return vars;
}

function parseUpgrade(src) {
  // Capture OnUpgrade body, either `=> expr;` or `{ ... }`.
  let body = null;
  const arrow = src.match(/OnUpgrade\s*\(\s*\)\s*=>\s*([^;]+);/);
  if (arrow) body = arrow[1];
  else {
    const block = src.match(/OnUpgrade\s*\(\s*\)\s*\{([\s\S]*?)\n\s*\}/);
    if (block) body = block[1];
  }
  if (body == null) return "none";

  const parts = [];
  // DynamicVars.Name.UpgradeValueBy(n)  /  DynamicVars.Name().UpgradeValueBy(n)
  for (const m of body.matchAll(/DynamicVars\.(\w+)(?:\(\))?\.UpgradeValueBy\(\s*(-?\d+)m?\s*\)/g))
    parts.push(`${stripPower(m[1])} ${m[2] >= 0 ? "+" : ""}${num(m[2])}`);
  // DynamicVars["Name"].UpgradeValueBy(n) — stripPower keeps names consistent with `numbers`
  // regardless of whether the code uses a string key or a typed accessor.
  for (const m of body.matchAll(/DynamicVars\["(\w+)"\]\.UpgradeValueBy\(\s*(-?\d+)m?\s*\)/g))
    parts.push(`${stripPower(m[1])} ${m[2] >= 0 ? "+" : ""}${num(m[2])}`);
  // EnergyCost.UpgradeBy(n)
  for (const m of body.matchAll(/EnergyCost\.UpgradeBy\(\s*(-?\d+)\s*\)/g))
    parts.push(`Cost ${num(m[1])}`);

  return parts.length ? parts.join(", ") : "custom (see code)";
}

function parseSummary(src) {
  const m = src.match(/\/\/\/\s*<summary>([\s\S]*?)<\/summary>/); // single-line summaries have no /// before the close tag
  if (!m) return "";
  const txt = m[1].replace(/\/\/\/?/g, " ").replace(/\s+/g, " ").trim();
  const sentence = txt.split(/(?<=[.!?])\s/)[0] || txt;
  return sentence.trim();
}

const splitArgs = (s) => s.split(",").map((a) => a.trim()).filter(Boolean);

// Resolve the card ctor's ultimate base(cost, CardType.X, CardRarity.X, TargetType.X) args,
// following pass-through ctor params on intermediate bases (e.g. the Brew trio :
// OrientationBrewCard, whose ctor forwards `energyCost`/`rarity` with defaults).
function resolveCtorArgs(childSrc, parent, srcByClass) {
  const isResolved = (a) => a && a.length === 4 && a[1].startsWith("CardType.");
  const call = childSrc.match(/:\s*base\(([^)]*)\)/);
  let args = call ? splitArgs(call[1]) : null; // null = child has no ctor; use parent defaults
  for (let hops = 0; hops < 5 && !isResolved(args); hops++) {
    const p = srcByClass && srcByClass[parent];
    if (!p) return null;
    const ctor = p.src.match(
      new RegExp(String.raw`(?:protected|public)\s+${parent}\s*\(([^)]*)\)\s*:\s*base\(([^)]*)\)`)
    );
    if (!ctor) return null;
    // param map: declared defaults, overridden positionally by the child's base(...) args
    const map = {};
    splitArgs(ctor[1]).forEach((decl, i) => {
      const m = decl.match(/(\w+)\s*(?:=\s*(.+))?$/);
      if (!m) return;
      map[m[1]] = args && args[i] !== undefined ? args[i] : (m[2] || "").trim();
    });
    args = splitArgs(ctor[2]).map((a) => (a in map ? map[a] : a));
    parent = p.parent;
  }
  return isResolved(args) ? args : null;
}

function parseCard(file, srcByClass) {
  let src = fs.readFileSync(file, "utf8");
  const cls = src.match(/public\s+sealed\s+class\s+(\w+)\s*:\s*(\w+)/) || src.match(/public\s+class\s+(\w+)\s*:\s*(\w+)/);
  if (!cls) return null;
  const className = cls[1];
  if (SKIP.has(className)) return null;

  const ctorArgs = resolveCtorArgs(src, cls[2], srcByClass);
  if (!ctorArgs) return null; // not a card model (no card ctor)

  // A card may inherit its vars/OnUpgrade/summary from an intermediate abstract base
  // (e.g. the Brew trio : OrientationBrewCard). Append base-class source so the
  // field regexes below can fall through to it; child declarations match first.
  let parent = cls[2];
  while (parent && !SKIP.has(parent) && srcByClass && srcByClass[parent]) {
    src += "\n" + srcByClass[parent].src;
    parent = srcByClass[parent].parent;
  }

  const hasX = /HasEnergyCostX\s*=>\s*true/.test(src);
  const rarityRaw = ctorArgs[2].replace("CardRarity.", "");
  const vars = parseCanonicalVars(src);

  return {
    className,
    entry: pascalToSnake(className),
    file: path.relative(ROOT, file).split(path.sep).join("/"),
    cost: hasX ? "X" : ctorArgs[0],
    type: ctorArgs[1].replace("CardType.", ""),
    rarity: RAR_MAP[rarityRaw] || rarityRaw,
    target: ctorArgs[3].replace("TargetType.", ""),
    vars,
    numbers: Object.fromEntries(vars.map((v) => [v.name, v.value])),
    upgrade: parseUpgrade(src),
    summary: parseSummary(src),
    multiplayer: /MultiplayerConstraint\s*=>\s*CardMultiplayerConstraint\.MultiplayerOnly/.test(src),
  };
}

// ---------- localization ----------
function loadLoc() {
  const raw = JSON.parse(fs.readFileSync(LOC_PATH, "utf8"));
  return raw; // flat map of "THEWITCH-ENTRY.field" -> string
}

function renderText(desc, vars) {
  if (!desc) return "";
  const byKey = {};
  for (const v of vars) { byKey[v.token] = v.value; byKey[v.name] = v.value; }
  let t = desc;
  // {Token:plural:a|b}
  t = t.replace(/\{(\w+):plural:([^|}]*)\|([^}]*)\}/g, (_, k, a, b) => (byKey[k] === 1 ? a : b));
  // {IfUpgraded:show:A|B} -> base (B)
  t = t.replace(/\{IfUpgraded:show:[^|}]*\|([^}]*)\}/g, "$1");
  // {Token:diff()} / {Token:anything} / {Token}
  t = t.replace(/\{(\w+)(?::[^}]*)?\}/g, (whole, k) => (k in byKey ? byKey[k] : whole));
  // strip [gold]...[/gold] and any [..] markup tags
  t = t.replace(/\[[^\]]*\]/g, "");
  return t.replace(/\s+/g, " ").trim();
}

// ---------- build ----------
function build() {
  const loc = loadLoc();
  const files = listCardFiles(CARDS_DIR);
  // index abstract/intermediate bases by class name for ctor-inheritance fallthrough
  const srcByClass = {};
  for (const f of files) {
    const s = fs.readFileSync(f, "utf8");
    const m = s.match(/public\s+(?:abstract\s+|sealed\s+)?class\s+(\w+)\s*:\s*(\w+)/);
    if (m) srcByClass[m[1]] = { src: s, parent: m[2] };
  }
  const parsed = files.map((f) => parseCard(f, srcByClass)).filter(Boolean);

  const cards = parsed.map((p) => {
    const title = loc[`${MOD_PREFIX}${p.entry}.title`] || p.className;
    const desc = loc[`${MOD_PREFIX}${p.entry}.description`] || "";
    const note = p.multiplayer ? `Multiplayer-only. ${p.summary}`.trim() : p.summary;
    return {
      name: title,
      entry: p.entry,
      file: p.file,
      cost: /^\d+$/.test(p.cost) ? Number(p.cost) : p.cost,
      type: p.type,
      rarity: p.rarity,
      target: p.target,
      text: renderText(desc, p.vars),
      numbers: p.numbers,
      upgrade: p.upgrade,
      note,
      tested: false,
      artFinal: false,
      mechanics: [],
      role: [],
      sub: [],
      threads: [],
      _hasLoc: !!loc[`${MOD_PREFIX}${p.entry}.title`],
    };
  });

  const ri = (r) => { const i = RAR_ORDER.indexOf(r); return i < 0 ? 99 : i; };
  cards.sort((a, b) => ri(a.rarity) - ri(b.rarity) || a.name.localeCompare(b.name));
  return cards;
}

// fingerprint = the mechanical fields that, when changed, should clear TESTED.
const fingerprint = (c) =>
  JSON.stringify([c.cost, c.type, c.rarity, c.target, c.text, c.numbers, c.upgrade]);

function main() {
  const check = process.argv.includes("--check");
  const fresh = build();

  let old = { cards: [] };
  if (fs.existsSync(OUT_PATH)) { try { old = JSON.parse(fs.readFileSync(OUT_PATH, "utf8")); } catch {} }
  const oldByEntry = Object.fromEntries((old.cards || []).map((c) => [c.entry, c]));

  const added = [], changed = [], missingLoc = [];
  for (const c of fresh) {
    const prev = oldByEntry[c.entry];
    if (!c._hasLoc) missingLoc.push(c.entry);
    c.artFinal = !!prev && prev.artFinal; // art-final flag is independent of mechanics; always preserved
    // curated categorization tags — independent of mechanical fingerprint; always preserved
    if (prev && prev.mechanics) c.mechanics = prev.mechanics;
    if (prev && prev.role) c.role = prev.role;
    if (prev && prev.sub) c.sub = prev.sub;
    if (prev && prev.threads) c.threads = prev.threads;
    if (!prev) { added.push(c.name); continue; }
    // preserve curated note if the source has no summary
    if (!c.note && prev.note) c.note = prev.note;
    if (fingerprint(c) !== fingerprint(prev)) {
      changed.push(c.name);
      c.tested = false; // design changed -> clear TESTED
    } else {
      c.tested = !!prev.tested; // unchanged -> keep flag
    }
  }
  const freshEntries = new Set(fresh.map((c) => c.entry));
  const removed = (old.cards || []).filter((c) => !freshEntries.has(c.entry)).map((c) => c.name);

  for (const c of fresh) delete c._hasLoc;
  const out = { generated: new Date().toISOString().slice(0, 10), cards: fresh };

  // report
  const tested = fresh.filter((c) => c.tested).length;
  console.log(`Cards: ${fresh.length}  (tested ${tested}/${fresh.length})`);
  const byRar = {};
  for (const c of fresh) byRar[c.rarity] = (byRar[c.rarity] || 0) + 1;
  console.log("Rarity:", RAR_ORDER.filter((r) => byRar[r]).map((r) => `${r} ${byRar[r]}`).join(", "),
    Object.keys(byRar).filter((r) => !RAR_ORDER.includes(r)).map((r) => `${r} ${byRar[r]}`).join(", "));
  if (added.length) console.log("  + added:", added.join(", "));
  if (removed.length) console.log("  - removed:", removed.join(", "));
  if (changed.length) console.log("  ~ changed (TESTED cleared):", changed.join(", "));
  if (missingLoc.length) console.log("  ! missing localization title:", missingLoc.join(", "));
  if (!added.length && !removed.length && !changed.length) console.log("  up to date.");

  const drift = added.length || removed.length || changed.length;
  if (check) { process.exit(drift ? 1 : 0); }

  fs.writeFileSync(OUT_PATH, JSON.stringify(out, null, 2) + "\n");
  console.log(`Wrote ${path.relative(ROOT, OUT_PATH).split(path.sep).join("/")}`);

  // keep the static art tracker page in sync with the fresh card data
  require("child_process").execFileSync(process.execPath,
    [path.join(ROOT, "Docs", "art-tracker", "regen-art-tracker.js")], { stdio: "inherit" });
  // ...and the pool-comparison report (Docs/pool-comparison.md)
  require("child_process").execFileSync(process.execPath,
    [path.join(__dirname, "compare.js")], { stdio: "inherit" });
}

main();
