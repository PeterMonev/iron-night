// The platoon's paint, from the raw lifted TRELLIS textures (art/models/raw-tex): the Americans in dull olive drab,
// the Soviets in 4BO green - the generator's lime pulled down to real, dirty paint. Dark blue-black holes the
// texture bake left (the underside seen through no camera) are filled with the tank's own average. A part texture
// (the Sherman's separate turret) is matched to its hull's brightness. node tankpaint.js
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const M = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Models/', RAW = 'D:/Codes/Projects/lightswarm/art/models/raw-tex/', UNSTARRED = 'D:/Codes/Projects/lightswarm/art/models/unstarred/';
fs.mkdirSync(RAW, { recursive: true });
const US = ['sherman', 'sherman_turret2', 'easy8', 'hellcat', 'chaffee', 'pershing', 'm10', 'firefly'];
const SU = ['t34_85', 'kv85', 'su100', 'is2'];
function paint(d, sat, gain, r, g, b) {
  for (let i = 0; i < d.length; i += 4) {
    const y = (0.3 * d[i] + 0.59 * d[i + 1] + 0.11 * d[i + 2]) / 255; const grey = y * gain;
    d[i] = 255 * Math.min(1, (grey + (d[i] / 255 * gain - grey) * sat) * r);
    d[i + 1] = 255 * Math.min(1, (grey + (d[i + 1] / 255 * gain - grey) * sat) * g);
    d[i + 2] = 255 * Math.min(1, (grey + (d[i + 2] / 255 * gain - grey) * sat) * b);
  }
}
function fillHoles(d, W, H) {
  // the bake's holes: the sides the reference never showed come out as flat dark blue-grey - dark, bluish, and with
  // almost no texture in a 9x9 window (the tracks are dark and bluish too, but full of detail)
  const y = new Float32Array(W * H); for (let i = 0; i < W * H; i++) y[i] = 0.3 * d[i * 4] + 0.59 * d[i * 4 + 1] + 0.11 * d[i * 4 + 2];
  // box-filtered mean and mean of squares via integral images
  const S1 = new Float64Array((W + 1) * (H + 1)), S2 = new Float64Array((W + 1) * (H + 1));
  for (let r = 1; r <= H; r++) { let a1 = 0, a2 = 0; for (let c = 1; c <= W; c++) { const v = y[(r - 1) * W + c - 1]; a1 += v; a2 += v * v; S1[r * (W + 1) + c] = S1[(r - 1) * (W + 1) + c] + a1; S2[r * (W + 1) + c] = S2[(r - 1) * (W + 1) + c] + a2; } }
  const R = 4; const hole = new Uint8Array(W * H); let mr = 0, mg = 0, mb = 0, n = 0;
  for (let r = 0; r < H; r++) for (let c = 0; c < W; c++) {
    const r0 = Math.max(0, r - R), r1 = Math.min(H, r + R + 1), c0 = Math.max(0, c - R), c1 = Math.min(W, c + R + 1); const cnt = (r1 - r0) * (c1 - c0);
    const s1 = S1[r1 * (W + 1) + c1] - S1[r0 * (W + 1) + c1] - S1[r1 * (W + 1) + c0] + S1[r0 * (W + 1) + c0], s2 = S2[r1 * (W + 1) + c1] - S2[r0 * (W + 1) + c1] - S2[r1 * (W + 1) + c0] + S2[r0 * (W + 1) + c0];
    const m = s1 / cnt, sd = Math.sqrt(Math.max(0, s2 / cnt - m * m)); const i = (r * W + c) * 4;
    if (m < 105 && sd < 7 && d[i + 2] >= d[i + 1] * 0.98 && d[i + 2] > d[i] * 1.08) hole[r * W + c] = 1; else { mr += d[i]; mg += d[i + 1]; mb += d[i + 2]; n++; }
  }
  mr /= n; mg /= n; mb /= n; let filled = 0;
  for (let p = 0; p < W * H; p++) if (hole[p]) { const i = p * 4; const j = ((p * 7919) % 23) - 11; d[i] = mr + j; d[i + 1] = mg + j; d[i + 2] = mb + j; filled++; }
  return filled / (W * H);
}
function mean(d) { let s = 0; for (let i = 0; i < d.length; i += 4) s += 0.3 * d[i] + 0.59 * d[i + 1] + 0.11 * d[i + 2]; return s / (d.length / 4) / 255; }
// the brightness of the texels a mesh actually uses: sampled at its vertex UVs (the atlas is mostly unused space)
function usedMean(d, W, H, objFile) { const lines = fs.readFileSync(objFile, 'utf8').split(/\r?\n/); let s = 0, n = 0; for (const l of lines) { if (!l.startsWith('vt ')) continue; const [u, v] = l.slice(3).split(' ').map(Number); const x = Math.min(W - 1, Math.max(0, Math.round(u * (W - 1)))), y = Math.min(H - 1, Math.max(0, Math.round((1 - v) * (H - 1)))); const i = (y * W + x) * 4; s += 0.3 * d[i] + 0.59 * d[i + 1] + 0.11 * d[i + 2]; n++; } return n ? s / n / 255 : 0.2; }
(async () => {
  const done = {};
  for (const name of [...US, ...SU]) {
    if (!fs.existsSync(M + name + '.png')) { console.log('no texture', name); continue; }
    if (!fs.existsSync(RAW + name + '.png')) fs.copyFileSync(M + name + '.png', RAW + name + '.png');
    const src = fs.existsSync(UNSTARRED + name + '.png') ? UNSTARRED + name + '.png' : RAW + name + '.png';   // unstar.js ran first on the Americans
    const im = await loadImage(src); const c = createCanvas(im.width, im.height), g = c.getContext('2d'); g.drawImage(im, 0, 0);
    const img = g.getImageData(0, 0, c.width, c.height), d = img.data;
    const holes = fillHoles(d, c.width, c.height);
    if (US.includes(name)) paint(d, 0.5, 0.62, 0.95, 1.0, 0.74);   // olive drab: dull, dark, brown-green
    else paint(d, 0.38, 0.62, 0.98, 1.05, 0.86);                   // 4BO: dull, dark, green over brown
    done[name] = { d, c, g, img }; console.log(name, 'holes', holes, 'mean', mean(d).toFixed(3));
  }
  // the separate Sherman turret at the hull's brightness
  if (done.sherman && done.sherman_turret2) { const k = usedMean(done.sherman.d, done.sherman.c.width, done.sherman.c.height, M + 'sherman_hull.obj') / usedMean(done.sherman_turret2.d, done.sherman_turret2.c.width, done.sherman_turret2.c.height, M + 'sherman_turret2.obj'); const d = done.sherman_turret2.d; for (let i = 0; i < d.length; i += 4) { d[i] = Math.min(255, d[i] * k); d[i + 1] = Math.min(255, d[i + 1] * k); d[i + 2] = Math.min(255, d[i + 2] * k); } console.log('turret matched x' + k.toFixed(2)); }
  for (const name in done) { const o = done[name]; o.g.putImageData(o.img, 0, 0); fs.writeFileSync(M + name + '.png', o.c.toBuffer('image/png')); }
})();
