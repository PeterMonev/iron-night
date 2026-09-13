// Plan-view depth maps (white = closest to the camera), edge maps and silhouette masks of WWII tanks to real proportions,
// for ControlNet. Everything is drawn from real dimensions so SDXL cannot change the shape; it only adds material and detail.
// Files: depth_<tank>.png (whole tank), depth_<tank>_hull.png (no turret, for the hull sprite), edge_*.png, mask_<tank>_{all,hull,turret}.png, meta.json
const { createCanvas } = require('@napi-rs/canvas'); const fs = require('fs');
const OUT = 'D:/Tools/ComfyUI_windows_portable/ComfyUI/input/guides';
// metres: hull length/width, track width, turret ring centre (0 front..1 rear), turret length/width, gun length past the turret front,
// barrel width, turret shape: 'cast' (rounded, Sherman), 'box' (angular with bustle, Pz IV), 'horseshoe' (Tiger), 'hex' (T-34-85)
const TANKS = {
  sherman: {L: 5.9, W: 2.6, track: 0.42, ring: 0.47, tL: 2.4, tW: 1.9, gun: 2.2, bore: 0.17, turret: 'cast', brake: false, skirts: false, fuel: false, glacis: 0.22},
  pz4:     {L: 5.9, W: 2.9, track: 0.40, ring: 0.44, tL: 2.5, tW: 2.0, gun: 3.0, bore: 0.17, turret: 'box', brake: true, skirts: true, fuel: false, glacis: 0.12},
  tiger:   {L: 6.3, W: 3.7, track: 0.72, ring: 0.5, tL: 3.2, tW: 2.6, gun: 3.6, bore: 0.20, turret: 'horseshoe', brake: true, skirts: false, fuel: false, glacis: 0.10},
  t34:     {L: 6.1, W: 3.0, track: 0.50, ring: 0.40, tL: 2.5, tW: 2.1, gun: 2.9, bore: 0.18, turret: 'hex', brake: false, skirts: false, fuel: true, glacis: 0.30},
};
const FLOOR = 0.16; // depth of the ground plane; black would read as an abyss and SDXL paints a dark backdrop
function gray(v){ const k = Math.round(Math.max(0, Math.min(1, v)) * 255); return `rgb(${k},${k},${k})`; }
function turretPath(g, kind, tW, tL){ // turret outline centred on 0,0, front at -tL/2
  g.beginPath();
  if (kind === 'cast') g.roundRect(-tW / 2, -tL / 2, tW, tL, [tW * 0.42, tW * 0.42, tW * 0.5, tW * 0.5]);
  else if (kind === 'box') g.roundRect(-tW / 2, -tL / 2, tW, tL, [tW * 0.18, tW * 0.18, tW * 0.12, tW * 0.12]);
  else if (kind === 'horseshoe') { g.moveTo(-tW / 2, -tL / 2); g.lineTo(tW / 2, -tL / 2); g.lineTo(tW / 2, tL * 0.05); g.arc(0, tL * 0.05, tW / 2, 0, Math.PI); g.closePath(); }
  else { const w = tW / 2, l = tL / 2; g.moveTo(-w * 0.55, -l); g.lineTo(w * 0.55, -l); g.lineTo(w, -l * 0.2); g.lineTo(w * 0.85, l); g.lineTo(-w * 0.85, l); g.lineTo(-w, -l * 0.2); g.closePath(); }
}
function geometry(t){ const px = 1024 * 0.86 / (t.L * (1 - t.ring) + t.tL / 2 + t.gun); const L = t.L * px, tL = t.tL * px;
  return { px, L, W: t.W * px, tr: t.track * px, ry: -L / 2 + t.ring * L, tL, tW: t.tW * px, cx: 512, cy: 512 + (t.gun * px + tL / 2 - t.ring * L) / 2 }; }
// mode: 'depth' | 'edge' | 'mask'; parts: 'all' | 'hull' | 'turret'
function draw(name, t, mode, parts = 'all'){
  const { px, L, W, tr, ry, tL, tW, cx, cy } = geometry(t);
  const c = createCanvas(1024, 1024), g = c.getContext('2d'); g.fillStyle = mode === 'depth' ? gray(FLOOR) : '#000000'; g.fillRect(0, 0, 1024, 1024);
  g.translate(cx, cy);
  const D = mode === 'depth', E = mode === 'edge', M = mode === 'mask';
  const fill = v => { g.fillStyle = D ? gray(v) : M ? '#fff' : '#000'; }, edge = () => { g.strokeStyle = '#fff'; g.lineWidth = 3; };
  const hullParts = parts !== 'turret', turretParts = parts !== 'hull';
  if (hullParts){
    // tracks with links (1 link is about 0.15 m)
    for (const s of [-1, 1]) { const x = s * (W / 2 - tr / 2); fill(0.34); g.fillRect(x - tr / 2, -L / 2 - 4, tr, L + 8);
      if (E) { edge(); g.strokeRect(x - tr / 2, -L / 2 - 4, tr, L + 8); }
      if (!M) for (let y = -L / 2; y < L / 2; y += 0.15 * px) { if (E) { g.strokeStyle = '#999'; g.lineWidth = 1.5; g.beginPath(); g.moveTo(x - tr / 2, y); g.lineTo(x + tr / 2, y); g.stroke(); }
        else { fill(0.38); g.fillRect(x - tr / 2 + 2, y + 1, tr - 4, 0.15 * px * 0.55); } } }
    if (t.skirts) for (const s of [-1, 1]) { const x = s * (W / 2 + 0.06 * px); fill(0.42); g.fillRect(x - 0.05 * px, -L * 0.42, 0.1 * px, L * 0.84); if (E) { edge(); g.strokeRect(x - 0.05 * px, -L * 0.42, 0.1 * px, L * 0.84); } }
    // hull deck
    const hx = -W / 2 + tr, hw = W - 2 * tr;
    fill(0.55); g.fillRect(hx, -L / 2, hw, L); if (E) { edge(); g.strokeRect(hx, -L / 2, hw, L); }
    if (!M){
      // sloped glacis at the front: depth falls toward the nose
      if (D) { const gr = g.createLinearGradient(0, -L / 2, 0, -L / 2 + L * t.glacis); gr.addColorStop(0, gray(0.40)); gr.addColorStop(1, gray(0.55)); g.fillStyle = gr; g.fillRect(hx, -L / 2, hw, L * t.glacis); }
      else { edge(); g.beginPath(); g.moveTo(hx, -L / 2 + L * t.glacis); g.lineTo(hx + hw, -L / 2 + L * t.glacis); g.stroke(); }
      // engine deck with grilles (slightly lower), rear
      const ey = L / 2 - L * 0.30, eh = L * 0.26; fill(0.50); g.fillRect(hx + 6, ey, hw - 12, eh); if (E) { edge(); g.strokeRect(hx + 6, ey, hw - 12, eh); }
      for (let i = 1; i < 6; i++) { const y = ey + i * eh / 6; if (E) { g.strokeStyle = '#aaa'; g.lineWidth = 1.5; } else { g.strokeStyle = gray(0.45); g.lineWidth = 3; } g.beginPath(); g.moveTo(hx + 14, y); g.lineTo(hx + hw - 14, y); g.stroke(); }
      // driver / radio operator hatches at the front (raised)
      for (const s of [-1, 1]) { const hcx = s * hw * 0.26, hcy = -L / 2 + L * (t.glacis + 0.07), r = 0.28 * px; fill(0.62); g.beginPath(); g.arc(hcx, hcy, r, 0, 7); g.fill(); if (E) { edge(); g.stroke(); } }
      // the turret ring shows when the turret is drawn separately
      if (parts === 'hull') { fill(0.57); g.beginPath(); g.arc(0, ry, tW * 0.36, 0, 7); g.fill(); if (E) { edge(); g.stroke(); } }
    }
    // external fuel drums along the sides (T-34)
    if (t.fuel) for (const s of [-1, 1]) for (const yy of [L * 0.05, L * 0.28]) { const x = s * (W / 2 + 0.16 * px), w = 0.3 * px, h = 0.9 * px;
      if (D) { const gr = g.createLinearGradient(x - w / 2, 0, x + w / 2, 0); gr.addColorStop(0, gray(0.35)); gr.addColorStop(0.5, gray(0.6)); gr.addColorStop(1, gray(0.35)); g.fillStyle = gr; } else fill(0);
      g.beginPath(); g.roundRect(x - w / 2, yy - h / 2, w, h, w / 2); g.fill(); if (E) { edge(); g.stroke(); } }
  }
  if (turretParts){
    // gun: round barrel (shaded across its width), muzzle brake
    const gw = t.bore * px, gy0 = ry - tL / 2 - t.gun * px, gy1 = ry - tL * 0.1;
    if (D) { const gr = g.createLinearGradient(-gw / 2, 0, gw / 2, 0); gr.addColorStop(0, gray(0.45)); gr.addColorStop(0.5, gray(0.78)); gr.addColorStop(1, gray(0.45)); g.fillStyle = gr; } else fill(0);
    g.fillRect(-gw / 2, gy0, gw, gy1 - gy0); if (E) { edge(); g.strokeRect(-gw / 2, gy0, gw, gy1 - gy0); }
    if (t.brake) { fill(0.7); g.fillRect(-gw * 0.9, gy0, gw * 1.8, 0.35 * px); if (E) { edge(); g.strokeRect(-gw * 0.9, gy0, gw * 1.8, 0.35 * px); } }
    // turret: rounded top (depth gradient from edge to centre), mantlet, cupola, hatch, stowage bin
    g.save(); g.translate(0, ry);
    turretPath(g, t.turret, tW, tL);
    if (D) { const gr = g.createRadialGradient(0, 0, tW * 0.1, 0, 0, tW * 0.62); gr.addColorStop(0, gray(0.86)); gr.addColorStop(1, gray(t.turret === 'cast' ? 0.66 : 0.78)); g.fillStyle = gr; } else fill(0);
    g.fill(); if (E) { edge(); g.stroke(); }
    fill(0.82); g.fillRect(-tW * 0.28, -tL / 2 - 0.15 * px, tW * 0.56, 0.45 * px); if (E) { edge(); g.strokeRect(-tW * 0.28, -tL / 2 - 0.15 * px, tW * 0.56, 0.45 * px); }
    if (!M){
      fill(0.94); g.beginPath(); g.arc(tW * 0.22, tL * 0.15, 0.32 * px, 0, 7); g.fill(); if (E) { edge(); g.stroke(); }
      fill(0.9); g.beginPath(); g.arc(-tW * 0.2, tL * 0.05, 0.26 * px, 0, 7); g.fill(); if (E) { edge(); g.stroke(); }
      if (t.turret === 'box') { fill(0.72); g.fillRect(-tW * 0.42, tL / 2 - 0.35 * px, tW * 0.84, 0.35 * px); if (E) { edge(); g.strokeRect(-tW * 0.42, tL / 2 - 0.35 * px, tW * 0.84, 0.35 * px); } }
    }
    g.restore();
  }
  return c;
}
function drawPak(mode){
  const px = 1024 * 0.86 / 6.6; // barrel 3.7 m in front of the shield, trails 2.9 m behind
  const c = createCanvas(1024, 1024), g = c.getContext('2d'); g.fillStyle = mode === 'depth' ? gray(FLOOR) : '#000000'; g.fillRect(0, 0, 1024, 1024);
  const D = mode === 'depth', E = mode === 'edge', M = mode === 'mask'; const fill = v => { g.fillStyle = D ? gray(v) : M ? '#fff' : '#000'; }, edge = () => { g.strokeStyle = '#fff'; g.lineWidth = 3; };
  g.translate(512, 512 + 0.43 * px); const sy = 0; // shield line at y = 0
  // split trails: two box beams from the axle spreading back to spades
  for (const s of [-1, 1]) { g.save(); g.translate(s * 0.35 * px, sy + 0.2 * px); g.rotate(s * 0.42); fill(0.42); g.fillRect(-0.09 * px, 0, 0.18 * px, 2.9 * px); if (E) { edge(); g.strokeRect(-0.09 * px, 0, 0.18 * px, 2.9 * px); }
    fill(0.46); g.fillRect(-0.2 * px, 2.6 * px, 0.4 * px, 0.3 * px); if (E) { edge(); g.strokeRect(-0.2 * px, 2.6 * px, 0.4 * px, 0.3 * px); } g.restore(); }
  // axle and wheels (seen from above: narrow rectangles with rounded ends)
  fill(0.4); g.fillRect(-1.0 * px, sy - 0.06 * px, 2.0 * px, 0.12 * px);
  for (const s of [-1, 1]) { fill(0.5); g.beginPath(); g.roundRect(s * 0.98 * px - 0.14 * px, sy - 0.5 * px, 0.28 * px, 1.0 * px, 0.14 * px); g.fill(); if (E) { edge(); g.stroke(); } }
  // gun shield: two plates, the outer one wider, angled back at the sides (from above it is a shallow V with a notch for the barrel)
  fill(0.62); g.beginPath(); g.moveTo(-0.95 * px, sy + 0.25 * px); g.lineTo(-0.2 * px, sy - 0.1 * px); g.lineTo(0.2 * px, sy - 0.1 * px); g.lineTo(0.95 * px, sy + 0.25 * px); g.lineTo(0.95 * px, sy + 0.4 * px); g.lineTo(0.2 * px, sy + 0.05 * px); g.lineTo(-0.2 * px, sy + 0.05 * px); g.lineTo(-0.95 * px, sy + 0.4 * px); g.closePath(); g.fill(); if (E) { edge(); g.stroke(); }
  // cradle and breech behind the shield
  fill(0.66); g.fillRect(-0.18 * px, sy - 0.05 * px, 0.36 * px, 0.9 * px); if (E) { edge(); g.strokeRect(-0.18 * px, sy - 0.05 * px, 0.36 * px, 0.9 * px); }
  // barrel with muzzle brake
  const gw = 0.16 * px; if (D) { const gr = g.createLinearGradient(-gw / 2, 0, gw / 2, 0); gr.addColorStop(0, gray(0.5)); gr.addColorStop(0.5, gray(0.8)); gr.addColorStop(1, gray(0.5)); g.fillStyle = gr; } else fill(0);
  g.fillRect(-gw / 2, sy - 3.7 * px, gw, 3.7 * px); if (E) { edge(); g.strokeRect(-gw / 2, sy - 3.7 * px, gw, 3.7 * px); }
  fill(0.72); g.fillRect(-gw * 0.9, sy - 3.7 * px, gw * 1.8, 0.3 * px); if (E) { edge(); g.strokeRect(-gw * 0.9, sy - 3.7 * px, gw * 1.8, 0.3 * px); }
  return c;
}
const meta = {};
for (const mode of ['depth', 'edge']) fs.writeFileSync(`${OUT}/${mode}_pak40.png`, drawPak(mode).toBuffer('image/png'));
fs.writeFileSync(`${OUT}/mask_pak40_all.png`, drawPak('mask').toBuffer('image/png'));
meta.pak40 = { px: 1024 * 0.86 / 6.6, cx: 512, cy: 512 + 0.43 * 1024 * 0.86 / 6.6, ringX: 512, ringY: 512 + 0.43 * 1024 * 0.86 / 6.6 };
for (const [name, t] of Object.entries(TANKS)) {
  fs.writeFileSync(`${OUT}/depth_${name}.png`, draw(name, t, 'depth').toBuffer('image/png'));
  fs.writeFileSync(`${OUT}/depth_${name}_hull.png`, draw(name, t, 'depth', 'hull').toBuffer('image/png'));
  fs.writeFileSync(`${OUT}/edge_${name}.png`, draw(name, t, 'edge').toBuffer('image/png'));
  fs.writeFileSync(`${OUT}/edge_${name}_hull.png`, draw(name, t, 'edge', 'hull').toBuffer('image/png'));
  for (const p of ['all', 'hull', 'turret']) fs.writeFileSync(`${OUT}/mask_${name}_${p}.png`, draw(name, t, 'mask', p).toBuffer('image/png'));
  const gm = geometry(t); meta[name] = { px: gm.px, cx: gm.cx, cy: gm.cy, ringX: gm.cx, ringY: gm.cy + gm.ry, hullL: gm.L, hullW: gm.W, turretL: gm.tL, turretW: gm.tW, gunTip: gm.cy + gm.ry - gm.tL / 2 - t.gun * gm.px };
}
fs.writeFileSync(`${OUT}/meta.json`, JSON.stringify(meta, null, 1)); console.log('guides written');
