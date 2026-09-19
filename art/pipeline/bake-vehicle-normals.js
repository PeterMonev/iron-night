// A detail normal map for every vehicle texture, from its own brightness (rivets, hatches, plate edges catch the
// moon): Resources/Models/<name>_n.png next to <name>.png. node bake-vehicle-normals.js
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const M = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Models/';
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
(async () => {
  for (const f of fs.readdirSync(M)) {
    if (!f.endsWith('.png') || f.endsWith('_n.png')) continue; const name = f.slice(0, -4);
    const im = await loadImage(M + f); const c = createCanvas(1024, 1024), g = c.getContext('2d'); g.imageSmoothingQuality = 'high'; g.drawImage(im, 0, 0, 1024, 1024);
    fs.writeFileSync(M + name + '_n.png', normalMap(c, 3.0, 1).toBuffer('image/png')); console.log(name + '_n');
  }
})();
