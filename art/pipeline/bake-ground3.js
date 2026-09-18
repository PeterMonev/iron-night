// The ground from the ChatGPT pictures (art/refs/batch3): the same treatment as bake-photo-fields.js - seamless
// by the offset-and-blend, toned for the night, a normal map from the brightness - plus the new decals: tank track
// prints, puddles, scorch marks. node bake-ground3.js
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const IN = 'D:/Codes/Projects/lightswarm/art/refs/batch3/', OUT = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Textures/';
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


// a square crop out of the middle of a portrait picture, then scaled
function cropSquare(im, size) { const s = Math.min(im.width, im.height); const c = createCanvas(size, size), g = c.getContext('2d'); g.imageSmoothingQuality = 'high'; g.drawImage(im, (im.width - s) / 2, (im.height - s) / 2, s, s, 0, 0, size, size); return c; }
// alpha from brightness for the pictures on black (scorch, track print, ring): black is nothing
function alphaFromBlack(c, gain) { const g = c.getContext('2d'), img = g.getImageData(0, 0, c.width, c.height), d = img.data; for (let i = 0; i < d.length; i += 4) { const y = (0.3 * d[i] + 0.59 * d[i + 1] + 0.11 * d[i + 2]) / 255; d[i + 3] = Math.round(255 * Math.min(1, y * gain)); } g.putImageData(img, 0, 0); }
(async () => {
  const load = n => loadImage(IN + n + '.png');
  for (const [name, sat, gain, cool, nstr, flat] of [['ground_plough', 0.85, 0.6, -0.03, 1.4], ['ground_pasture', 0.75, 0.5, 0.0, 2.0], ['ground_mown', 0.7, 0.5, 0.0, 1.6], ['ground_stubble', 0.75, 0.46, -0.02, 1.6],
                                               ['ground_snow_plough', 0.5, 0.56, 0.04, 1.0, 0.3], ['ground_snow_pasture', 0.45, 0.56, 0.04, 1.2, 0.15], ['ground_snow_mown', 0.45, 0.58, 0.04, 0.8, 0.1], ['ground_snow_stubble', 0.45, 0.56, 0.04, 1.0, 0.25]]) {
    const base = cropSquare(await load(name), 1024); if (flat) flatten(base, flat);
    const c = seamless(base); save(normalMap(c, nstr, 1), name + '_n'); night(c, sat, gain, cool); save(c, name);
  }
  // the hedge foliage (hedge2 is the top-down picture; hedge is the model reference)
  { const c = seamless(cropSquare(await load('hedge2'), 512)); save(normalMap(c, 3, 1), 'hedge_n'); night(c, 0.6, 0.62, 0.05); save(c, 'hedge'); }
  // the lanes come with their own verges: the middle square, seamless along the ruts, soft edges across
  for (const [name, sat, gain] of [['lane', 0.6, 0.52], ['lane_snow', 0.45, 0.56]]) {
    const c = seamlessV(cropSquare(await load(name), 1024)); save(normalMap(c, 2.4, 1), name + '_n'); night(c, sat, gain, 0.03);
    alphaMask(c, (u, v) => { const e = Math.min(u, 1 - u); return Math.min(1, e / 0.16) * (0.85 + 0.15 * fbm(u * 6, v * 24, 3, 2)); }); save(c, name);
  }
  // the yards: the mud tiled three by three inside a soft ragged disc
  for (const [name, sat, gain] of [['yard', 0.6, 0.52], ['yard_snow', 0.45, 0.56]]) {
    const t = seamless(cropSquare(await load(name), 512)); const c = createCanvas(1024, 1024), g = c.getContext('2d'); for (let y = 0; y < 3; y++) for (let x = 0; x < 3; x++) g.drawImage(t, x * 342, y * 342, 342, 342);
    { const img = g.getImageData(0, 0, 1024, 1024), d = img.data; for (let y = 0; y < 1024; y++) for (let x = 0; x < 1024; x++) { const u = x / 1024, v = y / 1024; const k = (0.86 + 0.28 * fbm(u * 4, v * 4, 17, 3)) * (1 - 0.12 * Math.max(0, 1 - Math.hypot(u - 0.5, v - 0.5) * 3)); const i = (y * 1024 + x) * 4; d[i] *= k; d[i + 1] *= k; d[i + 2] *= k; } g.putImageData(img, 0, 0); }
    save(normalMap(c, 2.4, 1), name + '_n'); night(c, sat, gain, 0.03);
    alphaMask(c, (u, v) => { const r = Math.hypot(u - 0.5, v - 0.5) * 2; const edge = 0.62 + 0.3 * fbm(u * 5, v * 5, 11, 3); return 1 - Math.min(1, Math.max(0, (r - edge + 0.18) / 0.18)); }); save(c, name);
  }
  // the crater: the grass round it fading out
  { const c = cropSquare(await load('crater'), 512); save(normalMap(c, 2.6, 1), 'crater_n'); night(c, 0.6, 0.5, 0.03); alphaMask(c, (u, v) => { const r = Math.hypot(u - 0.5, v - 0.5) * 2; return 1 - Math.min(1, Math.max(0, (r - 0.55) / 0.35)); }); save(c, 'crater'); }
  // the puddle: brown water in a ragged shape; everything green round it goes transparent
  { const c = cropSquare(await load('puddle'), 512); night(c, 0.6, 0.55, 0.03); alphaMask(c, (u, v) => { const r = Math.hypot((u - 0.5) * 1.15, (v - 0.5) * 1.35) * 2; const edge = 0.5 + 0.12 * fbm(u * 6, v * 6, 5, 3); return 1 - Math.min(1, Math.max(0, (r - edge + 0.1) / 0.14)); }); save(c, 'puddle'); }
  // the track print: a vertical strip cut from the picture on black (it came out horizontal), alpha from brightness
  { const im = await load('track_print'); const c = createCanvas(256, 1024), g = c.getContext('2d'); g.translate(128, 512); g.rotate(Math.PI / 2); g.drawImage(im, -512, -128, 1024, 256); alphaFromBlack(c, 2.2); night(c, 0.6, 0.6, 0.0); save(c, 'track_print'); }
  // the scorch mark: on black, alpha from brightness, darkened so it burns the ground rather than lightening it
  { const c = cropSquare(await load('scorch'), 512); alphaFromBlack(c, 1.6); const g = c.getContext('2d'), img = g.getImageData(0, 0, 512, 512), d = img.data; for (let i = 0; i < d.length; i += 4) { d[i] *= 0.18; d[i + 1] *= 0.17; d[i + 2] *= 0.16; } g.putImageData(img, 0, 0); save(c, 'scorch'); }
  // the objective ring: additive sprite straight from the picture (black stays black)
  { const c = cropSquare(await load('ring_objective'), 512); save(c, 'ring_objective'); }
  // the variation map as before
  { const S = 512, c = createCanvas(S, S), g = c.getContext('2d'), img = g.createImageData(S, S), d = img.data;
    for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) { const u = x / S, v = y / S; const n = fbm(u * 3, v * 3, 21, 4) - 0.5, m = fbm(u * 9, v * 9, 33, 3) - 0.5; const val = 0.5 + n * 0.3 + m * 0.1; const i = (y * S + x) * 4; d[i] = 255 * Math.min(1, Math.max(0, val + n * 0.06)); d[i + 1] = 255 * Math.min(1, Math.max(0, val)); d[i + 2] = 255 * Math.min(1, Math.max(0, val - n * 0.08)); d[i + 3] = 255; }
    g.putImageData(img, 0, 0); save(seamless(c), 'ground_variation'); }
})();
