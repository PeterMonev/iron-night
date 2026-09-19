// Leaf-cluster cards for the hedges and tree crowns: four cells cut from the hawthorn photograph, each with a ragged
// noise alpha so the card edges are leaves, not a square; toned for the night. Resources/Textures/leaves.png (RGBA).
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const IN = 'D:/Codes/Projects/lightswarm/art/refs/batch3/', OUT = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Textures/';
function noise2(x, y, seed) { const s = Math.sin(x * 127.1 + y * 311.7 + seed * 74.7) * 43758.5453; return s - Math.floor(s); }
function smoothNoise(x, y, seed) { const xi = Math.floor(x), yi = Math.floor(y), fx = x - xi, fy = y - yi, u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy); const a = noise2(xi, yi, seed), b = noise2(xi + 1, yi, seed), c = noise2(xi, yi + 1, seed), d = noise2(xi + 1, yi + 1, seed); return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v; }
function fbm(x, y, seed, oct) { let s = 0, a = 0.5, f = 1, n = 0; for (let i = 0; i < oct; i++) { s += a * smoothNoise(x * f, y * f, seed + i); n += a; a *= 0.5; f *= 2; } return s / n; }
(async () => {
  const im = await loadImage(IN + 'hedge2.png'); const S = 1024, C = 512; const c = createCanvas(S, S), g = c.getContext('2d');
  const crops = [[0, 0], [im.width - 700, 0], [0, im.height - 700], [im.width - 700, im.height - 700]];
  for (let k = 0; k < 4; k++) g.drawImage(im, crops[k][0], crops[k][1], 700, 700, (k % 2) * C, Math.floor(k / 2) * C, C, C);
  const img = g.getImageData(0, 0, S, S), d = img.data;
  for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) {
    const k = (x >= C ? 1 : 0) + (y >= C ? 2 : 0); const u = (x % C) / C, v = (y % C) / C; const i = (y * S + x) * 4;
    // a round-ish cluster with a leafy edge: fbm bites into a soft disc; inside, a few holes between the clumps
    const r = Math.hypot(u - 0.5, v - 0.5) * 2; const edge = 0.62 + 0.3 * fbm(u * 5 + k * 3, v * 5, 11 + k, 3);
    let a = 1 - Math.min(1, Math.max(0, (r - edge + 0.12) / 0.12));
    const holes = fbm(u * 9, v * 9, 31 + k, 3); if (holes < 0.36) a *= Math.max(0, (holes - 0.3) / 0.06);
    // the night: darker, less colour, a touch of blue
    const yy = 0.3 * d[i] + 0.59 * d[i + 1] + 0.11 * d[i + 2]; const sat = 0.75, gain = 0.85;
    d[i] = (yy + (d[i] - yy) * sat) * gain * 0.95; d[i + 1] = (yy + (d[i + 1] - yy) * sat) * gain; d[i + 2] = (yy + (d[i + 2] - yy) * sat) * gain * 1.02; d[i + 3] = Math.round(255 * a);
  }
  g.putImageData(img, 0, 0); fs.writeFileSync(OUT + 'leaves.png', c.toBuffer('image/png')); console.log('leaves 1024');
})();
