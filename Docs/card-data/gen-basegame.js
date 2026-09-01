#!/usr/bin/env node
/*
 * gen-basegame.js — build Docs/card-data/<class>.json reference files for base-game
 * classes (Silent, Necrobinder) by parsing the decompiled game source under gamedata/.
 *
 *   node Docs/card-data/gen-basegame.js            (rebuild all configured classes)
 *   node Docs/card-data/gen-basegame.js silent     (rebuild one)
 *
 * These power the per-class tabs on Docs/card-designs.html, used to study the makeup of
 * each character's pool when designing the Witch. The mechanical fields (cost/type/rarity/
 * target/numbers/upgrade/text) are parsed fresh from gamedata; the curated `mechanics` and
 * `role` categorization tags are PRESERVED across runs (keyed by entry), exactly like
 * regen.js preserves `tested`/`note`. gamedata/ is gitignored, so the generated JSON is the
 * committed snapshot — rerun this only when refreshing against a new game version.
 *
 * Each class lists which CardPool to read; the pool's `ModelDb.Card<X>()` calls are the
 * authoritative card roster.
 */
const fs = require("fs");
const path = require("path");

const HERE = __dirname;                                  // Docs/card-data
const ROOT = path.resolve(HERE, "..", "..");             // repo root
const GD = path.join(ROOT, "gamedata", "src", "Core", "Models", "Cards");
const POOL = path.join(ROOT, "gamedata", "src", "Core", "Models", "CardPools");
const LOC_PATH = path.join(ROOT, "gamedata", "localization", "eng", "cards.json");

if (!fs.existsSync(GD)) {
  console.error("gamedata/ decompile not present (it is gitignored, local-only) — nothing to parse.");
  process.exit(1);
}

// className of the source CardPool -> output basename
const CLASSES = {
  silent: "SilentCardPool",
  necrobinder: "NecrobinderCardPool",
  ironclad: "IroncladCardPool",
  defect: "DefectCardPool",
  regent: "RegentCardPool",
};

const pascalToSnake = (s) =>
  s.replace(/([a-z0-9])([A-Z])/g, "$1_$2").replace(/([A-Z]+)([A-Z][a-z])/g, "$1_$2").toUpperCase();
const stripPower = (s) => s.replace(/Power$/, "");
const num = (s) => Number(String(s).replace(/m$/, ""));
const RAR_MAP = { Basic: "Starter" };
const RAR_ORDER = ["Starter", "Common", "Uncommon", "Rare", "Special", "Ancient", "Token"];

function parseCanonicalVars(src) {
  const m = src.match(/CanonicalVars\s*=>([\s\S]*?);/);
  const vars = [];
  if (!m) return vars;
  const body = m[1];
  for (const pm of body.matchAll(/new\s+PowerVar<\s*(\w+)\s*>\s*\(\s*(-?\d+)m?/g))
    vars.push({ name: stripPower(pm[1]), token: pm[1], value: num(pm[2]) });
  for (const vm of body.matchAll(/new\s+(\w+)Var\s*\(\s*(-?\d+)m?/g)) {
    if (vm[1] === "Power") continue;
    vars.push({ name: vm[1], token: vm[1], value: num(vm[2]) });
  }
  return vars;
}
function parseUpgrade(src) {
  let body = null;
  const arrow = src.match(/OnUpgrade\s*\(\s*\)\s*=>\s*([^;]+);/);
  if (arrow) body = arrow[1];
  else { const b = src.match(/OnUpgrade\s*\(\s*\)\s*\{([\s\S]*?)\n\s*\}/); if (b) body = b[1]; }
  if (body == null) return "none";
  const parts = [];
  for (const m of body.matchAll(/DynamicVars\.(\w+)(?:\(\))?\.UpgradeValueBy\(\s*(-?\d+)m?\s*\)/g))
    parts.push(`${m[1]} ${m[2] >= 0 ? "+" : ""}${num(m[2])}`);
  for (const m of body.matchAll(/EnergyCost\.UpgradeBy\(\s*(-?\d+)\s*\)/g))
    parts.push(`Cost ${num(m[1])}`);
  return parts.length ? parts.join(", ") : "custom";
}
function renderText(desc, vars) {
  if (!desc) return "";
  const byKey = {}; for (const v of vars) { byKey[v.name] = v.value; if (v.token) byKey[v.token] = v.value; }
  let t = desc;
  t = t.replace(/\{(\w+):plural:([^|}]*)\|([^}]*)\}/g, (_, k, a, b) => (byKey[k] === 1 ? a : b));
  t = t.replace(/\{IfUpgraded:show:[^|}]*\|([^}]*)\}/g, "$1");
  t = t.replace(/\{(\w+)(?::[^}]*)?\}/g, (whole, k) => (k in byKey ? byKey[k] : whole));
  t = t.replace(/\[[^\]]*\]/g, "");
  return t.replace(/\s+/g, " ").trim();
}
function parseCard(className, loc) {
  const file = path.join(GD, className + ".cs");
  if (!fs.existsSync(file)) return { className, entry: pascalToSnake(className), missing: true };
  const src = fs.readFileSync(file, "utf8");
  const ctor = src.match(/:\s*base\(\s*([^,]+?)\s*,\s*CardType\.(\w+)\s*,\s*CardRarity\.(\w+)\s*,\s*TargetType\.(\w+)\s*\)/);
  const entry = pascalToSnake(className);
  const vars = parseCanonicalVars(src);
  const hasX = /HasEnergyCostX\s*=>\s*true/.test(src);
  const rarRaw = ctor ? ctor[3] : "?";
  return {
    name: loc[entry + ".title"] || className,
    entry,
    file: "gamedata/src/Core/Models/Cards/" + className + ".cs",
    cost: ctor ? (hasX ? "X" : (/^\d+$/.test(ctor[1].trim()) ? Number(ctor[1].trim()) : ctor[1].trim())) : "?",
    type: ctor ? ctor[2] : "?",
    rarity: RAR_MAP[rarRaw] || rarRaw,
    target: ctor ? ctor[4] : "?",
    text: renderText(loc[entry + ".description"] || "", vars),
    numbers: Object.fromEntries(vars.map((v) => [v.name, v.value])),
    upgrade: parseUpgrade(src),
    multiplayer: /MultiplayerConstraint\s*=>\s*CardMultiplayerConstraint\.MultiplayerOnly/.test(src),
    mechanics: [],
    role: [],
    sub: [],
    threads: [],
  };
}

function buildOne(outName, poolClass, loc) {
  const poolSrc = fs.readFileSync(path.join(POOL, poolClass + ".cs"), "utf8");
  const names = [...poolSrc.matchAll(/ModelDb\.Card<(\w+)>\(\)/g)].map((m) => m[1]);
  const cards = names.map((n) => parseCard(n, loc));
  const missing = cards.filter((c) => c.missing).map((c) => c.className);

  // preserve curated categorization tags from the existing file (keyed by entry)
  const outPath = path.join(HERE, outName + ".json");
  let prevByEntry = {};
  if (fs.existsSync(outPath)) {
    try { prevByEntry = Object.fromEntries(JSON.parse(fs.readFileSync(outPath, "utf8")).cards.map((c) => [c.entry, c])); } catch {}
  }
  for (const c of cards) {
    const prev = prevByEntry[c.entry];
    if (prev && prev.mechanics) c.mechanics = prev.mechanics;
    if (prev && prev.role) c.role = prev.role;
    if (prev && prev.sub) c.sub = prev.sub;
    if (prev && prev.threads) c.threads = prev.threads;
    delete c.missing;
  }

  const ri = (r) => { const i = RAR_ORDER.indexOf(r); return i < 0 ? 99 : i; };
  cards.sort((a, b) => ri(a.rarity) - ri(b.rarity) || a.name.localeCompare(b.name));
  fs.writeFileSync(outPath, JSON.stringify({ generated: "from gamedata", class: outName, cards }, null, 2) + "\n");

  const untagged = cards.filter((c) => !c.mechanics.length).map((c) => c.entry);
  const byR = {}; cards.forEach((c) => byR[c.rarity] = (byR[c.rarity] || 0) + 1);
  console.log(`${outName}: ${cards.length} cards · ${RAR_ORDER.filter((r) => byR[r]).map((r) => `${r} ${byR[r]}`).join(", ")}`);
  if (missing.length) console.log("  ! missing source:", missing.join(", "));
  if (untagged.length) console.log("  ! untagged (mechanics/role empty):", untagged.join(", "));
}

function main() {
  if (!fs.existsSync(LOC_PATH)) {
    console.error("gamedata/ not found — this reference generator needs the decompiled game source (gitignored, local-only).");
    process.exit(1);
  }
  const loc = JSON.parse(fs.readFileSync(LOC_PATH, "utf8"));
  const only = process.argv[2];
  const targets = only ? { [only]: CLASSES[only] } : CLASSES;
  if (only && !CLASSES[only]) { console.error(`unknown class '${only}'. known: ${Object.keys(CLASSES).join(", ")}`); process.exit(1); }
  for (const [outName, poolClass] of Object.entries(targets)) buildOne(outName, poolClass, loc);
}

main();
