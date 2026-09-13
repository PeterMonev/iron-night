// Cuts game sprites out of the ControlNet renders using the guide masks (the render follows the guide, so the silhouette is
// known exactly): <tank>_turret.png from the whole-tank render, <tank>_hull.png from the hull-only render, plus meta.json
// with the turret ring position in each sprite. node split.js sherman <whole.png> <hull.png> [scale]
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const G = 'D:/Tools/ComfyUI_windows_portable/ComfyUI/input/guides', OUT = 'D:/Codes/Projects/lightswarm/art/ai-tanks';
const meta = JSON.parse(fs.readFileSync(G + '/meta.json', 'utf8'));
function blurMask(m, W, H, r){ // box blur so the cut edge is soft, r pixels
  const o = new Float32Array(W * H), t = new Float32Array(W * H);
  for (let y = 0; y < H; y++){ let s = 0; for (let x = -r; x <= r; x++) s += m[y * W + Math.max(0, Math.min(W - 1, x))]; for (let x = 0; x < W; x++){ t[y * W + x] = s / (2 * r + 1); s += m[y * W + Math.min(W - 1, x + r + 1)] - m[y * W + Math.max(0, x - r)]; } }
  for (let x = 0; x < W; x++){ let s = 0; for (let y = -r; y <= r; y++) s += t[Math.max(0, Math.min(H - 1, y)) * W + x]; for (let y = 0; y < H; y++){ o[y * W + x] = s / (2 * r + 1); s += t[Math.min(H - 1, y + r + 1) * W + x] - t[Math.max(0, y - r) * W + x]; } }
  return o;
}
async function cut(renderFile, maskFile, outFile, pivot, scale, grow, patch){
  let im = await loadImage(renderFile); const mk = await loadImage(maskFile); const W = im.width, H = im.height;
  if (patch) im = await patchTurret(im, patch.render, patch.mask, patch.tank);
  const c = createCanvas(W, H), g = c.getContext('2d'); g.drawImage(mk, 0, 0, W, H); const md = g.getImageData(0, 0, W, H).data;
  let m = new Float32Array(W * H); for (let i = 0; i < W * H; i++) m[i] = md[i * 4] / 255;
  if (grow) { const b = blurMask(m, W, H, grow); for (let i = 0; i < W * H; i++) m[i] = Math.min(1, b[i] * 3); } // grow so bolts and stowage on the silhouette edge stay
  m = blurMask(m, W, H, 1);
  g.clearRect(0, 0, W, H); g.drawImage(im, 0, 0); const id = g.getImageData(0, 0, W, H), d = id.data;
  let minX = W, minY = H, maxX = -1, maxY = -1;
  for (let i = 0; i < W * H; i++){ const a = Math.round(m[i] * 255); d[i * 4 + 3] = a; if (a > 8){ const x = i % W, y = (i / W) | 0; if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; } }
  g.putImageData(id, 0, 0);
  const pad = 2, w = maxX - minX + 1 + 2 * pad, h = maxY - minY + 1 + 2 * pad, sw = Math.round(w * scale), sh = Math.round(h * scale);
  const o = createCanvas(sw, sh), og = o.getContext('2d'); og.imageSmoothingQuality = 'high'; og.drawImage(c, minX - pad, minY - pad, w, h, 0, 0, sw, sh);
  fs.writeFileSync(outFile, o.toBuffer('image/png'));
  return { w: sw, h: sh, px: (pivot.x - (minX - pad)) * scale, py: (pivot.y - (minY - pad)) * scale };
}
async function patchTurret(whole, hullFile, turretMaskFile, tank){ // paste the hull-only render under the turret footprint, feathered
  const B = await loadImage(hullFile), tm = await loadImage(turretMaskFile); const W = whole.width, H = whole.height;
  const c = createCanvas(W, H), g = c.getContext('2d'); g.drawImage(tm, 0, 0); const md = g.getImageData(0, 0, W, H).data;
  const m = new Float32Array(W * H); for (let i = 0; i < W * H; i++) m[i] = md[i * 4] / 255; const bl = blurMask(m, W, H, 14); for (let i = 0; i < W * H; i++) m[i] = Math.min(1, bl[i] * 1.6);
  g.clearRect(0, 0, W, H); g.drawImage(whole, 0, 0); const a = g.getImageData(0, 0, W, H); g.clearRect(0, 0, W, H); g.drawImage(B, 0, 0); const b = g.getImageData(0, 0, W, H).data;
  const gain = deckGain(a.data, b, W, H, tank);
  for (let i = 0; i < W * H; i++){ const k = m[i]; if (k <= 0) continue; for (let ch = 0; ch < 3; ch++){ const v = Math.min(255, b[i * 4 + ch] * gain[ch]); a.data[i * 4 + ch] = Math.round(a.data[i * 4 + ch] * (1 - k) + v * k); } }
  g.putImageData(a, 0, 0); return c;
}
function deckGain(a, b, W, H, tank){ // per-channel gain that matches the hull-only render's deck colour to the whole-tank render's deck
  const M = meta[tank]; const sa = [0, 0, 0], sb = [0, 0, 0]; let n = 0; const y0 = Math.round(M.ringY + M.turretL * 0.7), y1 = Math.round(M.cy + M.hullL * 0.5 - 10), x0 = Math.round(M.cx - M.hullW * 0.25), x1 = Math.round(M.cx + M.hullW * 0.25);
  for (let y = y0; y < y1; y += 2) for (let x = x0; x < x1; x += 2){ const i = (y * W + x) * 4; for (let ch = 0; ch < 3; ch++){ sa[ch] += a[i + ch]; sb[ch] += b[i + ch]; } n++; }
  return sa.map((v, ch) => Math.max(0.85, Math.min(1.2, v / Math.max(1, sb[ch]))));
}
async function main(){
  const [tank, whole, hull, scaleS] = process.argv.slice(2); const scale = Number(scaleS || 0.25); const M = meta[tank]; if (!M) throw new Error('unknown tank ' + tank);
  fs.mkdirSync(OUT, { recursive: true }); const mf = OUT + '/meta.json'; const all = fs.existsSync(mf) ? JSON.parse(fs.readFileSync(mf, 'utf8')) : {};
  const pivot = { x: M.ringX, y: M.ringY };
  if (tank === 'pak40'){ // one piece that rotates about its axle; the mockup knows it as pak_hull
    all.pak_hull = await cut(whole, `${G}/mask_pak40_all.png`, `${OUT}/pak40.png`, pivot, scale, 3);
    fs.writeFileSync(mf, JSON.stringify(all, null, 1)); console.log('pak40', JSON.stringify(all.pak_hull)); return; }
  all[tank + '_turret'] = await cut(whole, `${G}/mask_${tank}_turret.png`, `${OUT}/${tank}_turret.png`, pivot, scale, 3);
  all[tank + '_hull'] = await cut(whole, `${G}/mask_${tank}_hull.png`, `${OUT}/${tank}_hull.png`, pivot, scale, 2, { render: hull, mask: `${G}/mask_${tank}_turret.png`, tank });
  fs.writeFileSync(mf, JSON.stringify(all, null, 1)); console.log(tank, JSON.stringify(all[tank + '_turret']), JSON.stringify(all[tank + '_hull']));
}
main();
