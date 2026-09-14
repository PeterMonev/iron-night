// Turns the chosen SDXL ground pictures (gen-fields.js) into game textures: seamless (the tile.js offset-and-blend),
// toned for the night, plus a normal map from the brightness for the moonlight. Also the decals with their alpha
// masks (lane, yard, crater), the hedge foliage and the low-frequency variation map. node bake-photo-fields.js
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const IN = 'D:/Tools/ComfyUI_windows_portable/ComfyUI/output/fields/', OUT = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Textures/';

const PICK = {   // which picture of each batch
  ground_plough: 'plough_00002_', ground_pasture: 'pasture_00005_', ground_mown: 'mown_00003_', ground_stubble: 'stubble_00003_',
  ground_snow_plough: 'snow_plough_00001_', ground_snow_pasture: 'snow_pasture_00002_', ground_snow_mown: 'snow_mown_00002_', ground_snow_stubble: 'snow_stubble_00002_',
  mud: 'pasture2_00002_',   // bare earth patches worked into the pasture
  hedge: 'hedge2_00001_', lane: 'lane2_00002_', yard: 'yard2_00001_', crater: 'crater_00001_', yard_snow: 'snow_yard_00002_',
};
const args = process.argv.slice(2); for (const a of args) { const [k, v] = a.split('='); if (v) PICK[k] = v; }

function canvasOf(im, size) { const c = createCanvas(size, size), g = c.getContext('2d'); g.imageSmoothingQuality = 'high'; g.drawImage(im, 0, 0, size, size); return c; }
// the four quadrants swapped, the original blended back over the cross of seams (tile.js)
function seamless(src) {
  const W = src.width, H = src.height; const c = createCanvas(W, H), g = c.getContext('2d');
  g.drawImage(src, W / 2, H / 2, W / 2, H / 2, 0, 0, W / 2, H / 2); g.drawImage(src, 0, H / 2, W / 2, H / 2, W / 2, 0, W / 2, H / 2);
  g.drawImage(src, W / 2, 0, W / 2, H / 2, 0, H / 2, W / 2, H / 2); g.drawImage(src, 0, 0, W / 2, H / 2, W / 2, H / 2, W / 2, H / 2);
  const m = createCanvas(W, H), mg = m.getContext('2d'); mg.drawImage(src, 0, 0); mg.globalCompositeOperation = 'destination-in';
  const band = W * 0.22; const grad = (x0, y0, x1, y1) => { const gr = mg.createLinearGradient(x0, y0, x1, y1); gr.addColorStop(0, 'rgba(0,0,0,0)'); gr.addColorStop(0.5, 'rgba(0,0,0,1)'); gr.addColorStop(1, 'rgba(0,0,0,0)'); return gr; };
  const k = createCanvas(W, H), kg = k.getContext('2d'); kg.fillStyle = grad(W / 2 - band, 0, W / 2 + band, 0); kg.fillRect(W / 2 - band, 0, 2 * band, H);
  kg.globalCompositeOperation = 'lighter'; kg.fillStyle = grad(0, H / 2 - band, 0, H / 2 + band); kg.fillRect(0, H / 2 - band, W, 2 * band);
  mg.drawImage(k, 0, 0); g.drawImage(m, 0, 0); return c;
}
// seamless along v only: the top and bottom halves swapped, the original blended back over the horizontal seam
function seamlessV(src) {
  const W = src.width, H = src.height; const c = createCanvas(W, H), g = c.getContext('2d');
  g.drawImage(src, 0, H / 2, W, H / 2, 0, 0, W, H / 2); g.drawImage(src, 0, 0, W, H / 2, 0, H / 2, W, H / 2);
  const m = createCanvas(W, H), mg = m.getContext('2d'); mg.drawImage(src, 0, 0); mg.globalCompositeOperation = 'destination-in';
  const band = H * 0.22; const gr = mg.createLinearGradient(0, H / 2 - band, 0, H / 2 + band); gr.addColorStop(0, 'rgba(0,0,0,0)'); gr.addColorStop(0.5, 'rgba(0,0,0,1)'); gr.addColorStop(1, 'rgba(0,0,0,0)'); mg.fillStyle = gr; mg.fillRect(0, H / 2 - band, W, 2 * band);
  g.drawImage(m, 0, 0); return c;
}
function night(c, sat, gain, cool) {
  const g = c.getContext('2d'), img = g.getImageData(0, 0, c.width, c.height), d = img.data;
  for (let i = 0; i < d.length; i += 4) { const y = 0.3 * d[i] + 0.59 * d[i + 1] + 0.11 * d[i + 2]; d[i] = (y + (d[i] - y) * sat) * gain * (1 - cool); d[i + 1] = (y + (d[i + 1] - y) * sat) * gain; d[i + 2] = (y + (d[i + 2] - y) * sat) * gain * (1 + cool); }
  g.putImageData(img, 0, 0);
}
// a normal map from the brightness: bright = high. Red is +u (right), green is +v (up in the picture), OpenGL style
function normalMap(c, strength, blur) {
  const W = c.width, H = c.height, d = c.getContext('2d').getImageData(0, 0, W, H).data; let h = new Float32Array(W * H);
  for (let i = 0; i < W * H; i++) h[i] = (0.3 * d[i * 4] + 0.59 * d[i * 4 + 1] + 0.11 * d[i * 4 + 2]) / 255;
  for (let pass = 0; pass < blur; pass++) { const t = new Float32Array(W * H); for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) { let s = 0; for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) s += h[((y + dy + H) % H) * W + (x + dx + W) % W]; t[y * W + x] = s / 9; } h = t; }
  const o = createCanvas(W, H), og = o.getContext('2d'), img = og.createImageData(W, H), p = img.data;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const dx = (h[y * W + (x + 1) % W] - h[y * W + (x - 1 + W) % W]) * strength, dv = (h[((y - 1 + H) % H) * W + x] - h[((y + 1) % H) * W + x]) * strength;
    const l = Math.sqrt(dx * dx + dv * dv + 1); const i = (y * W + x) * 4; p[i] = 128 + (-dx / l) * 127; p[i + 1] = 128 + (-dv / l) * 127; p[i + 2] = 128 + (1 / l) * 127; p[i + 3] = 255;
  }
  og.putImageData(img, 0, 0); return o;
}
// bare patches: the second picture shows through where the slow noise is high
function patchy(grass, mud) { const W = grass.width, g = grass.getContext('2d'), a = g.getImageData(0, 0, W, W), b = mud.getContext('2d').getImageData(0, 0, W, W); for (let y = 0; y < W; y++) for (let x = 0; x < W; x++) { const n = fbm(x / W * 5, y / W * 5, 7, 4) + 0.25 * (fbm(x / W * 23, y / W * 23, 9, 2) - 0.5); const t = 0.75 * Math.min(1, Math.max(0, (n - 0.6) / 0.08)); const i = (y * W + x) * 4; for (let k = 0; k < 3; k++) a.data[i + k] = a.data[i + k] * (1 - t) + b.data[i + k] * 0.8 * t; } g.putImageData(a, 0, 0); return grass; }
// winter from summer: the light ground goes under snow, the dark ruts stay mud
function snowify(c) { const g = c.getContext('2d'), img = g.getImageData(0, 0, c.width, c.height), d = img.data; for (let i = 0; i < d.length; i += 4) { const y = (0.3 * d[i] + 0.59 * d[i + 1] + 0.11 * d[i + 2]) / 255; const t = Math.min(1, Math.max(0, (y - 0.3) / 0.25)); d[i] = d[i] * (1 - t) + 225 * t; d[i + 1] = d[i + 1] * (1 - t) + 230 * t; d[i + 2] = d[i + 2] * (1 - t) + 242 * t; } g.putImageData(img, 0, 0); }
// less contrast: every pixel pulled toward the mean (the snow furrows were corrugated iron)
function flatten(c, k) { const g = c.getContext('2d'), img = g.getImageData(0, 0, c.width, c.height), d = img.data; const m = [0, 0, 0]; for (let i = 0; i < d.length; i += 4) { m[0] += d[i]; m[1] += d[i + 1]; m[2] += d[i + 2]; } const n = d.length / 4; for (let i = 0; i < d.length; i += 4) for (let j = 0; j < 3; j++) d[i + j] = d[i + j] * (1 - k) + (m[j] / n) * k; g.putImageData(img, 0, 0); }
function alphaMask(c, fn) { const g = c.getContext('2d'), img = g.getImageData(0, 0, c.width, c.height), d = img.data, W = c.width, H = c.height; for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) d[(y * W + x) * 4 + 3] = Math.round(255 * Math.max(0, Math.min(1, fn(x / W, y / H)))); g.putImageData(img, 0, 0); }
function save(c, name) { fs.writeFileSync(OUT + name + '.png', c.toBuffer('image/png')); console.log(name, c.width); }
// value noise for the masks and the variation map
function noise2(x, y, seed) { const s = Math.sin(x * 127.1 + y * 311.7 + seed * 74.7) * 43758.5453; return s - Math.floor(s); }
function smoothNoise(x, y, seed) { const xi = Math.floor(x), yi = Math.floor(y), fx = x - xi, fy = y - yi, u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy); const a = noise2(xi, yi, seed), b = noise2(xi + 1, yi, seed), c = noise2(xi, yi + 1, seed), d = noise2(xi + 1, yi + 1, seed); return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v; }
function fbm(x, y, seed, oct) { let s = 0, a = 0.5, f = 1, n = 0; for (let i = 0; i < oct; i++) { s += a * smoothNoise(x * f, y * f, seed + i); n += a; a *= 0.5; f *= 2; } return s / n; }

(async () => {
  const load = k => loadImage(IN + PICK[k] + '.png');
  // the four fields, summer and winter: seamless 1024, night tone, normal map
  for (const [name, sat, gain, cool, nstr, flat] of [['ground_plough', 0.9, 0.62, -0.04, 1.3], ['ground_pasture', 0.7, 0.5, 0.0, 2.0], ['ground_mown', 0.65, 0.5, 0.0, 1.6], ['ground_stubble', 0.7, 0.46, -0.02, 1.6],
                                               ['ground_snow_plough', 0.45, 0.56, 0.04, 1.0, 0.55], ['ground_snow_pasture', 0.4, 0.56, 0.04, 1.2, 0.35], ['ground_snow_mown', 0.4, 0.56, 0.04, 0.8, 0.3], ['ground_snow_stubble', 0.4, 0.56, 0.04, 1.0, 0.5]]) {
    if (!fs.existsSync(IN + PICK[name] + '.png')) { console.log('skip', name); continue; }
    let base = canvasOf(await load(name), 1024);
    if (name === 'ground_pasture' && fs.existsSync(IN + PICK.mud + '.png')) base = patchy(base, canvasOf(await load('mud'), 1024));
    if (flat) flatten(base, flat);
    const c = seamless(base); save(normalMap(c, nstr, 1), name + '_n'); night(c, sat, gain, cool); save(c, name);
  }
  // the hedge foliage on the blobs and tree crowns
  if (fs.existsSync(IN + PICK.hedge + '.png')) { const c = seamless(canvasOf(await load('hedge'), 512)); save(normalMap(c, 3, 1), 'hedge_n'); night(c, 0.55, 0.62, 0.05); save(c, 'hedge'); }
  // the lane: seamless along its length, soft edges across, so it lies on any field
  for (const [name, src, sat, gain, snow] of [['lane', 'lane', 0.55, 0.55, false], ['lane_snow', 'lane', 0.4, 0.56, true]]) {
    if (!fs.existsSync(IN + PICK[src] + '.png')) { console.log('skip', name); continue; }
    const ruts = await load(src), verge = seamless(canvasOf(await load(snow ? 'ground_snow_pasture' : 'ground_pasture'), 1024)); const c = createCanvas(1024, 1024), g = c.getContext('2d'); g.imageSmoothingQuality = 'high';
    g.drawImage(verge, 0, 0); g.drawImage(ruts, ruts.width * 0.3, 0, ruts.width * 0.46, ruts.height, 184, 0, 656, 1024);
    // the join between verge and track feathered
    const j = g.getImageData(0, 0, 1024, 1024), v = verge.getContext('2d').getImageData(0, 0, 1024, 1024); for (let y = 0; y < 1024; y++) for (let x = 0; x < 1024; x++) { const e = Math.min(Math.abs(x - 184), Math.abs(x - 840)); const t = Math.min(1, e / 70) ; if (x >= 184 && x <= 840) { const i = (y * 1024 + x) * 4; for (let k = 0; k < 3; k++) j.data[i + k] = v.data[i + k] * (1 - t) + j.data[i + k] * t; } } g.putImageData(j, 0, 0);
    const c2 = seamlessV(c); if (snow) snowify(c2); save(normalMap(c2, 2.4, 1), name + '_n'); night(c2, sat, gain, 0.03);
    const cc = c2;
    alphaMask(cc, (u, v) => { const e = Math.min(u, 1 - u); return Math.min(1, e / 0.16) * (0.85 + 0.15 * fbm(u * 6, v * 24, 3, 2)); }); save(cc, name);
  }
  // the yard: the mud tiled three by three inside a soft ragged disc
  for (const [name, src, sat, gain] of [['yard', 'yard', 0.55, 0.55], ['yard_snow', 'yard_snow', 0.4, 0.56]]) {
    if (!fs.existsSync(IN + PICK[src] + '.png')) { console.log('skip', name); continue; }
    const t = seamless(canvasOf(await load(src), 512)); const c = createCanvas(1024, 1024), g = c.getContext('2d'); for (let y = 0; y < 3; y++) for (let x = 0; x < 3; x++) g.drawImage(t, x * 342, y * 342, 342, 342);
    { const img = g.getImageData(0, 0, 1024, 1024), d = img.data; for (let y = 0; y < 1024; y++) for (let x = 0; x < 1024; x++) { const u = x / 1024, v = y / 1024; const k = (0.86 + 0.28 * fbm(u * 4, v * 4, 17, 3)) * (1 - 0.12 * Math.max(0, 1 - Math.hypot(u - 0.5, v - 0.5) * 3)); const i = (y * 1024 + x) * 4; d[i] *= k; d[i + 1] *= k; d[i + 2] *= k; } g.putImageData(img, 0, 0); }   // slow blotches, the middle trodden darker
    save(normalMap(c, 2.4, 1), name + '_n'); night(c, sat, gain, 0.03);
    alphaMask(c, (u, v) => { const r = Math.hypot(u - 0.5, v - 0.5) * 2; const edge = 0.62 + 0.3 * fbm(u * 5, v * 5, 11, 3); return 1 - Math.min(1, Math.max(0, (r - edge + 0.18) / 0.18)); }); save(c, name);
  }
  // the crater: one picture, the grass around it fading out
  if (fs.existsSync(IN + PICK.crater + '.png')) { const c = canvasOf(await load('crater'), 512); save(normalMap(c, 2.6, 1), 'crater_n'); night(c, 0.55, 0.5, 0.03); alphaMask(c, (u, v) => { const r = Math.hypot(u - 0.5, v - 0.5) * 2; return 1 - Math.min(1, Math.max(0, (r - 0.6) / 0.32)); }); save(c, 'crater'); }
  // the variation map: slow blotches of light and dark, the light ones a touch warmer; mid grey is neutral (linear data)
  { const S = 512, c = createCanvas(S, S), g = c.getContext('2d'), img = g.createImageData(S, S), d = img.data;
    for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) { const u = x / S, v = y / S; const n = fbm(u * 3, v * 3, 21, 4) - 0.5, m = fbm(u * 9, v * 9, 33, 3) - 0.5; const val = 0.5 + n * 0.3 + m * 0.1; const i = (y * S + x) * 4; d[i] = 255 * Math.min(1, Math.max(0, val + n * 0.06)); d[i + 1] = 255 * Math.min(1, Math.max(0, val)); d[i + 2] = 255 * Math.min(1, Math.max(0, val - n * 0.08)); d[i + 3] = 255; }
    // seamless by construction? no: blend the edges the same way as the pictures
    g.putImageData(img, 0, 0); save(seamless(c), 'ground_variation'); }
})();
