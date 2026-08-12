#!/usr/bin/env node
// Generate pages/art-tracker.html — a static, self-contained art-asset tracker
// suitable for GitHub Pages. Data sources:
//   - ../card-data/cards.json      (cards; artFinal=true → Final)
//   - ./card-briefs.json           (hand-maintained card art briefs / status overrides)
//   - ./assets.json                (hand-maintained non-card asset categories)
// Thumbnails are referenced by repo-relative paths (../TheWitch/images/...) so the
// page works when the whole repo is served (Pages) or opened from Docs/ locally.
//
// Usage: node Docs/art-tracker/regen-art-tracker.js

const fs = require('fs');
const path = require('path');

const cardsData = require(path.join(__dirname, '..', 'card-data', 'cards.json'));
const briefs = require(path.join(__dirname, 'card-briefs.json')).briefs || {};
const assetCats = require(path.join(__dirname, 'assets.json')).categories;

const CARD_DIMS = '1000x760';

// sanitize card text (same rules as gen-art-tracker.js)
let cur = null;
function clean(s) {
  return String(s ?? '')
    .replace(/\{(\w+):diff\(\)\}/g, (_, v) => varValue(v))
    .replace(/\{InCombat:[^}]*\|\}/g, '')
    .replace(/\{(\w+)\}/g, (_, v) => varValue(v))
    .replace(/<see cref=""?(\w+)""? \/>/g, '$1')
    .replace(/<\/?c>/g, '')
    .replace(/\s+/g, ' ')
    .trim();
  function varValue(v) { return cur && cur.numbers && cur.numbers[v] != null ? String(cur.numbers[v]) : v; }
}

// build card rows
const root = path.join(__dirname, '..', '..');
function cardArtPath(entry) {
  const base = entry.toLowerCase() + '.png';
  for (const dir of ['TheWitch/images/card_portraits/', 'TheWitch/images/card_portraits/familiar/']) {
    if (fs.existsSync(path.join(root, dir + base))) return dir + base;
  }
  return 'TheWitch/images/card_portraits/' + base; // convention target even if missing
}
// status is derived: done → Done; artist assigned → In Progress; else Placeholder
function derive(done, artist) { return done ? 'Done' : (artist ? 'In Progress' : 'Placeholder'); }

const cardRows = cardsData.cards.map(c => {
  cur = c;
  const b = briefs[c.name] || {};
  return {
    name: c.name,
    artist: b.artist || '',
    status: derive(c.artFinal, b.artist),
    brief: b.brief || '',
    type: c.type,
    rarity: c.rarity,
    cost: c.cost === -1 ? 'X' : String(c.cost),
    text: clean(c.text),
    upgrade: clean(c.upgrade),
    mechanics: (c.mechanics || []).filter(m => m && m !== 'None').join(', '),
    path: cardArtPath(c.entry),
    dims: CARD_DIMS,
  };
});

// Powers tab: auto-enumerated from the localization file (every power must have loc, so it is the
// authoritative list — new powers appear on the next regen). Optional per-power curation (artist/done/brief)
// lives in assets.json under "powerOverrides", keyed by the loc entry (e.g. "HEX_POWER").
// .replace strips a UTF-8 BOM — editors re-save the localization JSON with one on and off.
const powerLoc = JSON.parse(fs.readFileSync(path.join(root, 'TheWitch', 'localization', 'eng', 'powers.json'), 'utf8').replace(/^﻿/, ''));
const powerOverrides = require(path.join(__dirname, 'assets.json')).powerOverrides || {};
const powerEntries = Object.keys(powerLoc)
  .filter(k => k.endsWith('.title'))
  .map(k => k.slice('THEWITCH-'.length, -'.title'.length));
const powersCat = {
  id: 'powers',
  title: 'Powers',
  dims: '64x64 (small) + 256x256 (big)',
  assets: powerEntries.map(entry => {
    const o = powerOverrides[entry] || {};
    cur = null;
    return {
      name: powerLoc[`THEWITCH-${entry}.title`] + ` (${entry.toLowerCase()})`,
      artist: o.artist || '',
      done: !!o.done,
      brief: o.brief || '',
      effect: clean(String(powerLoc[`THEWITCH-${entry}.description`] || '').replace(/\[\/?[a-z]+\]/g, '')),
      path: 'TheWitch/images/powers/big/' + entry.toLowerCase() + '.png',
    };
  }),
};
assetCats.splice(assetCats.findIndex(c => c.id === 'pets'), 0, powersCat);

// check which referenced images actually exist
function exists(p) { return p && fs.existsSync(path.join(root, p)); }
for (const r of cardRows) r.hasArt = exists(r.path);
for (const cat of assetCats) for (const a of cat.assets) a.hasArt = exists(a.path);

const data = {
  generated: new Date().toISOString().slice(0, 10),
  cards: cardRows,
  categories: assetCats.map(c => ({
    id: c.id, title: c.title, dims: c.dims,
    assets: c.assets.map(a => ({
      name: a.name, artist: a.artist || '', status: derive(a.done, a.artist), brief: a.brief || '',
      rarity: a.rarity || '', orientation: a.orientation || '',
      effect: a.effect || '', path: a.path || '', dims: a.dims || c.dims || '',
      hasArt: a.hasArt,
    })),
  })),
};

const html = `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>The Witch — Art Tracker</title>
<style>
  :root{
    --bg:#15131c; --panel:#1f1b2b; --panel2:#272234; --line:#3a3350;
    --ink:#e9e4f5; --muted:#a99fc4; --gold:#e0b24a; --accent:#9d7bff;
    --ok:#46c476; --warn:#e0b24a; --no:#5b5470;
  }
  *{box-sizing:border-box}
  body{margin:0;background:var(--bg);color:var(--ink);font:14px/1.45 "Segoe UI",system-ui,sans-serif}
  header{padding:18px 22px;border-bottom:1px solid var(--line);background:var(--panel);position:sticky;top:0;z-index:5}
  h1{margin:0 0 4px;font-size:20px;color:var(--gold);letter-spacing:.5px}
  .sub{color:var(--muted);font-size:12px}
  .tabs{display:flex;gap:6px;margin-top:12px;flex-wrap:wrap}
  .tab{background:var(--panel2);border:1px solid var(--line);color:var(--muted);padding:7px 16px;border-radius:8px 8px 0 0;cursor:pointer;font-size:13px;font-weight:600;user-select:none}
  .tab.on{background:var(--accent);border-color:var(--accent);color:#fff}
  .controls{display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin-top:10px}
  input[type=search]{background:var(--panel2);border:1px solid var(--line);color:var(--ink);padding:7px 10px;border-radius:6px;min-width:220px;font-size:13px}
  .chip{background:var(--panel2);border:1px solid var(--line);color:var(--muted);padding:6px 11px;border-radius:14px;cursor:pointer;font-size:12px;user-select:none}
  .chip.on{background:var(--accent);border-color:var(--accent);color:#fff}
  .prog{margin-left:auto;color:var(--muted);font-size:12px;text-align:right}
  .bar{width:180px;height:7px;background:var(--panel2);border-radius:4px;overflow:hidden;margin-top:4px}
  .bar > i{display:block;height:100%;background:var(--ok);width:0}
  main{padding:14px 22px 60px}
  table{width:100%;border-collapse:collapse}
  th,td{text-align:left;padding:8px 10px;border-bottom:1px solid var(--line);vertical-align:top}
  th{background:var(--panel);color:var(--muted);font-size:11px;text-transform:uppercase;letter-spacing:.6px}
  tbody tr:hover{background:var(--panel2)}
  .name{font-weight:600}
  .meta{color:var(--muted);font-size:12px;white-space:nowrap}
  .text{max-width:340px}
  .brief{max-width:420px;color:#b9a8e6;font-style:italic;font-size:12px}
  .dims{font:11px/1.4 ui-monospace,Consolas,monospace;color:var(--muted)}
  .pathcell{font:10px/1.4 ui-monospace,Consolas,monospace;color:var(--muted);max-width:220px;word-break:break-all}
  .badge{display:inline-block;padding:1px 8px;border-radius:10px;font-size:11px;font-weight:600;white-space:nowrap}
  .s-done{background:#46c47633;color:#5fe09a}
  .s-inprogress{background:#c9a82a33;color:#e6cf5a}
  .s-placeholder{background:#5b547033;color:#9b93b3}
  .artist{color:#8ec2ee;font-size:12px;white-space:nowrap}
  .thumb{width:60px}
  .thumb img{width:52px;height:auto;border-radius:4px;border:1px solid var(--line);background:#0d0b14;display:block;cursor:zoom-in}
  .thumb .none{width:52px;height:40px;border:1px dashed var(--line);border-radius:4px;display:flex;align-items:center;justify-content:center;color:var(--no);font-size:10px}
  #lightbox{position:fixed;inset:0;background:#000c;display:none;align-items:center;justify-content:center;z-index:50;cursor:zoom-out}
  #lightbox img{max-width:88vw;max-height:88vh;border-radius:8px;border:1px solid var(--line)}
  .catnote{color:var(--muted);font-size:12px;margin:8px 0 12px}
</style>
</head>
<body>
<header>
  <h1>The Witch — Art Tracker</h1>
  <div class="sub">Generated ${data.generated} from repo state · statuses reflect the current project, not artist assignments</div>
  <div class="tabs" id="tabs"></div>
  <div class="controls">
    <input type="search" id="q" placeholder="filter by name / text…">
    <span class="chip" data-f="all">All</span>
    <span class="chip" data-f="Placeholder">Placeholder</span>
    <span class="chip" data-f="In Progress">In Progress</span>
    <span class="chip" data-f="Done">Done</span>
    <div class="prog"><span id="progtext"></span><div class="bar"><i id="progbar"></i></div></div>
  </div>
</header>
<main id="main"></main>
<div id="lightbox"><img alt=""></div>
<script>
const DATA = ${JSON.stringify(data)};
const esc = s => String(s??'').replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
const scls = s => 's-' + s.toLowerCase().replace(/\\s+/g,'');
// tabs: cards + each asset category
const TABS = [{id:'cards', title:'Cards'}].concat(DATA.categories.map(c=>({id:c.id,title:c.title})));
let tab = 'cards', filter = 'all', q = '';

function rows() {
  if (tab === 'cards') return DATA.cards;
  return DATA.categories.find(c=>c.id===tab).assets;
}
function render() {
  document.querySelectorAll('.tab').forEach(t=>t.classList.toggle('on', t.dataset.t===tab));
  document.querySelectorAll('.chip').forEach(c=>c.classList.toggle('on', c.dataset.f===filter));
  const all = rows();
  const shown = all.filter(r =>
    (filter==='all' || r.status===filter) &&
    (!q || (r.name + ' ' + (r.text||r.effect||'') + ' ' + (r.brief||'') + ' ' + (r.artist||'')).toLowerCase().includes(q)));
  const fin = all.filter(r=>r.status==='Done').length;
  document.getElementById('progtext').textContent = fin + ' / ' + all.length + ' done';
  document.getElementById('progbar').style.width = (all.length ? 100*fin/all.length : 0) + '%';

  const isCards = tab === 'cards';
  const cat = isCards ? null : DATA.categories.find(c=>c.id===tab);
  let h = '';
  if (cat && cat.dims) h += '<div class="catnote">Required dimensions: ' + esc(cat.dims) + '</div>';
  h += '<table><thead><tr><th></th><th>Status</th><th>Artist</th><th>Art Brief</th><th>Name</th>';
  h += isCards ? '<th>Type</th><th>Rarity</th><th>Cost</th><th>Card Text</th><th>Upgrade</th><th>Mechanics</th>'
               : '<th>Details</th>' + (cat.assets.some(a=>a.rarity)?'<th>Rarity</th>':'') + (cat.assets.some(a=>a.orientation)?'<th>Orientation</th>':'');
  h += '<th>Required Dims</th><th>Filename</th></tr></thead><tbody>';
  for (const r of shown) {
    h += '<tr>';
    h += '<td class="thumb">' + (r.hasArt
      ? '<img loading="lazy" src="../' + esc(r.path) + '" alt="" title="click to enlarge">'
      : '<div class="none">no art</div>') + '</td>';
    h += '<td><span class="badge ' + scls(r.status) + '">' + esc(r.status) + '</span></td>';
    h += '<td class="artist">' + esc(r.artist || '—') + '</td>';
    h += '<td class="brief">' + esc(r.brief) + '</td>';
    h += '<td class="name">' + esc(r.name) + '</td>';
    if (isCards) {
      h += '<td class="meta">' + esc(r.type) + '</td><td class="meta">' + esc(r.rarity) + '</td><td class="meta">' + esc(r.cost) + '</td>';
      h += '<td class="text">' + esc(r.text) + '</td><td class="meta">' + esc(r.upgrade) + '</td><td class="meta">' + esc(r.mechanics) + '</td>';
    } else {
      h += '<td class="text">' + esc(r.effect) + '</td>';
      if (cat.assets.some(a=>a.rarity)) h += '<td class="meta">' + esc(r.rarity) + '</td>';
      if (cat.assets.some(a=>a.orientation)) h += '<td class="meta">' + esc(r.orientation) + '</td>';
    }
    h += '<td class="dims">' + esc(r.dims) + '</td>';
    h += '<td class="pathcell">' + esc(r.path ? r.path.split('/').pop() : '—') + '</td>';
    h += '</tr>';
  }
  h += '</tbody></table>';
  document.getElementById('main').innerHTML = h;
}
const tabsEl = document.getElementById('tabs');
tabsEl.innerHTML = TABS.map(t=>'<span class="tab" data-t="'+t.id+'">'+esc(t.title)+'</span>').join('');
tabsEl.addEventListener('click', e => { const t=e.target.closest('.tab'); if(t){tab=t.dataset.t; render();} });
document.querySelector('.controls').addEventListener('click', e => { const c=e.target.closest('.chip'); if(c){filter=c.dataset.f; render();} });
document.getElementById('q').addEventListener('input', e => { q=e.target.value.toLowerCase(); render(); });
const lb = document.getElementById('lightbox');
document.getElementById('main').addEventListener('click', e => {
  const img = e.target.closest('.thumb img');
  if (img) { lb.querySelector('img').src = img.src; lb.style.display='flex'; }
});
lb.addEventListener('click', () => lb.style.display='none');
render();
</script>
</body>
</html>
`;

const outPath = path.join(root, 'pages', 'art-tracker.html');
fs.writeFileSync(outPath, html);
const total = cardRows.length + assetCats.reduce((n, c) => n + c.assets.length, 0);
console.log(`art-tracker.html written: ${cardRows.length} cards + ${total - cardRows.length} other assets = ${total} rows`);
const missing = [...cardRows.filter(r => !r.hasArt).map(r => 'card: ' + r.name),
  ...assetCats.flatMap(c => c.assets.filter(a => a.path && !a.hasArt).map(a => c.title + ': ' + a.name))];
if (missing.length) console.log('MISSING IMAGE FILES:\n  ' + missing.join('\n  '));
