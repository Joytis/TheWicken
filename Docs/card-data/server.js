// Tiny zero-dependency server for the Witch card design page.
// Serves card-designs.html + cards.json (+ live art state + thumbnails), and persists
// TESTED / Art-Final toggles back to cards.json.
//   node server.js   ->  http://localhost:7820
const http = require("http");
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const PORT = 7820;
const HERE = __dirname; // Docs/card-data
const ROOT = path.resolve(HERE, "..", ".."); // repo root
const DOCS_DIR = path.join(HERE, ".."); // Docs/
const JSON_PATH = path.join(HERE, "cards.json");
const HTML_PATH = path.join(DOCS_DIR, "card-designs.html");
const BIG_DIR = path.join(ROOT, "TheWitch", "images", "card_portraits"); // single-size source art (packed to atlases at publish)
const PLACEHOLDER = path.join(BIG_DIR, "card.png"); // the "no art" duplicate source

function send(res, code, body, type) {
  res.writeHead(code, { "Content-Type": type, "Cache-Control": "no-store" });
  res.end(body);
}
const readData = () => JSON.parse(fs.readFileSync(JSON_PATH, "utf8"));
const writeData = (d) => fs.writeFileSync(JSON_PATH, JSON.stringify(d, null, 2) + "\n");

// Artist + art-brief per card (hand-authored via the page) -> Docs/art-tracker/card-briefs.json
const BRIEFS_PATH = path.join(DOCS_DIR, "art-tracker", "card-briefs.json");
const readBriefs = () => JSON.parse(fs.readFileSync(BRIEFS_PATH, "utf8"));
const writeBriefs = (d) => fs.writeFileSync(BRIEFS_PATH, JSON.stringify(d, null, 2) + "\n");

// Regenerate the static art-tracker page (the team-facing view) after any write.
// Debounced + fire-and-forget so rapid edits don't stack processes.
let regenTimer = null;
function regenTracker() {
  clearTimeout(regenTimer);
  regenTimer = setTimeout(() => {
    const child = require("child_process").execFile(
      process.execPath, [path.join(DOCS_DIR, "art-tracker", "regen-art-tracker.js")],
      (err) => { if (err) console.error("art-tracker regen failed:", err.message); });
    child.unref?.();
  }, 400);
}

const md5 = (buf) => crypto.createHash("md5").update(buf).digest("hex");
// Source art lives at card_portraits/<entry>.png, but familiar token cards author theirs under
// familiar/<entry>.png (mirrors WitchFamiliarCard's `familiar/` atlas-key prefix).
// Check the root first, then the familiar/ subdir; fall back to the root path when neither exists.
const bigPathFor = (entry) => {
  const root = path.join(BIG_DIR, entry.toLowerCase() + ".png");
  if (fs.existsSync(root)) return root;
  const fam = path.join(BIG_DIR, "familiar", entry.toLowerCase() + ".png");
  return fs.existsSync(fam) ? fam : root;
};
const hashFile = (file) => { try { return md5(fs.readFileSync(file)); } catch { return null; } };

// Known placeholder images (the generic card backs). Any big art matching one of these is "No Art".
const PLACEHOLDER_FILES = [
  PLACEHOLDER, // card_portraits/card.png
];

// Live art state for every card's BIG portrait, recomputed from disk on each call:
//   'none'        -> file missing, OR equals a known card.png, OR is a duplicate shared by >1 card
//                    (a shared image is a placeholder, not finished unique art)
//   'placeholder' -> a real, distinct image (unique to this card), not yet flagged final
//   'final'       -> a real, distinct image AND the card is flagged artFinal
function computeArtStates(cards) {
  const placeholders = new Set(PLACEHOLDER_FILES.map(hashFile).filter(Boolean));
  const hashByEntry = {};
  const counts = {};
  for (const c of cards) {
    const h = hashFile(bigPathFor(c.entry));
    hashByEntry[c.entry] = h;
    if (h) counts[h] = (counts[h] || 0) + 1;
  }
  const states = {};
  for (const c of cards) {
    const h = hashByEntry[c.entry];
    if (!h || placeholders.has(h) || counts[h] > 1) states[c.entry] = "none";
    else states[c.entry] = c.artFinal ? "final" : "placeholder";
  }
  return states;
}

const server = http.createServer((req, res) => {
  // --- toggles ---------------------------------------------------------
  const TOGGLE_FIELDS = { "/api/tested": "tested", "/api/artfinal": "artFinal", "/api/vfxpass": "vfxPass", "/api/sfxpass": "sfxPass" };
  if (req.method === "POST" && TOGGLE_FIELDS[req.url]) {
    const field = TOGGLE_FIELDS[req.url];
    const valKey = field;
    let raw = "";
    req.on("data", (c) => (raw += c));
    req.on("end", () => {
      try {
        const body = JSON.parse(raw);
        const data = readData();
        const card = data.cards.find((c) => c.entry === body.entry);
        if (!card) return send(res, 404, JSON.stringify({ error: "no such entry" }), "application/json");
        card[field] = !!body[valKey];
        writeData(data);
        if (field === "artFinal") regenTracker(); // Done-status feeds the static tracker
        const art = computeArtStates(data.cards)[card.entry];
        send(res, 200, JSON.stringify({ ok: true, entry: card.entry, [field]: card[field], art }), "application/json");
      } catch (e) {
        send(res, 400, JSON.stringify({ error: String(e) }), "application/json");
      }
    });
    return;
  }

  // --- artist + art brief per card --------------------------------------
  if (req.method === "POST" && req.url === "/api/brief") {
    let raw = "";
    req.on("data", (c) => (raw += c));
    req.on("end", () => {
      try {
        const body = JSON.parse(raw); // { name, artist, brief }
        if (!body.name) return send(res, 400, JSON.stringify({ error: "name required" }), "application/json");
        const data = readBriefs();
        const artist = String(body.artist || "").trim();
        const brief = String(body.brief || "").trim();
        if (!artist && !brief) delete data.briefs[body.name];
        else data.briefs[body.name] = { artist, brief };
        writeBriefs(data);
        regenTracker();
        send(res, 200, JSON.stringify({ ok: true, name: body.name, artist, brief }), "application/json");
      } catch (e) {
        send(res, 400, JSON.stringify({ error: String(e) }), "application/json");
      }
    });
    return;
  }
  if (req.method === "GET" && req.url.startsWith("/briefs.json")) {
    try { return send(res, 200, fs.readFileSync(BRIEFS_PATH), "application/json; charset=utf-8"); }
    catch { return send(res, 200, JSON.stringify({ briefs: {} }), "application/json"); }
  }

  // --- thumbnails: /art/<ENTRY>.png  (falls back to placeholder) --------
  if (req.method === "GET" && req.url.startsWith("/art/")) {
    const reqPath = req.url.split("?")[0]; // drop ?v= cache-buster before parsing
    const entry = decodeURIComponent(reqPath.slice("/art/".length).replace(/\.png$/i, ""));
    let file = bigPathFor(entry);
    if (!fs.existsSync(file)) file = PLACEHOLDER;
    try {
      res.writeHead(200, { "Content-Type": "image/png", "Cache-Control": "no-store" });
      res.end(fs.readFileSync(file));
    } catch { send(res, 404, "no image", "text/plain"); }
    return;
  }

  // --- data with live art state ----------------------------------------
  if (req.method === "GET" && req.url.startsWith("/cards.json")) {
    const data = readData();
    const states = computeArtStates(data.cards);
    data.cards.forEach((c) => {
      if (typeof c.artFinal === "undefined") c.artFinal = false;
      if (typeof c.vfxPass === "undefined") c.vfxPass = false;
      if (typeof c.sfxPass === "undefined") c.sfxPass = false;
      c.art = states[c.entry];
    });
    return send(res, 200, JSON.stringify(data), "application/json; charset=utf-8");
  }

  // --- base-game reference data (read-only): /silent.json, /necrobinder.json
  if (req.method === "GET" && /^\/(silent|necrobinder|ironclad|defect|regent)\.json/.test(req.url)) {
    const name = req.url.split("?")[0].slice(1);
    try {
      return send(res, 200, fs.readFileSync(path.join(HERE, name)), "application/json; charset=utf-8");
    } catch { return send(res, 404, "Not found", "text/plain"); }
  }

  // --- page ------------------------------------------------------------
  if (req.method === "GET" && (req.url === "/" || req.url === "/index.html")) {
    return send(res, 200, fs.readFileSync(HTML_PATH), "text/html; charset=utf-8");
  }
  send(res, 404, "Not found", "text/plain");
});

server.listen(PORT, () => {
  console.log(`Witch card designs -> http://localhost:${PORT}`);
  console.log("TESTED + Art-Final + VFX/SFX-Pass flags are saved to Docs/card-data/cards.json. Ctrl+C to stop.");
});
