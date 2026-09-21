// The hangar textures: the SDXL pictures made seamless, toned for lamp light, with normal maps from their brightness
// (fine here: no uv islands, one continuous surface). node bake-hangar.js  -> Resources/Textures/hangar_*.png
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const SRC = 'D:/Tools/ComfyUI_windows_portable/ComfyUI/output/hangar/', OUT = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Textures/';
function seamless(im, size) {
  // offset by half, blend the original back over the seams with a soft cross mask
  const c = createCanvas(size, size), g = c.getContext('2d'); const h = size / 2;
  g.drawImage(im, 0, 0, im.width, im.height, -h, -h, size, size); g.drawImage(im, 0, 0, im.width, im.height, h, -h, size, size); g.drawImage(im, 0, 0, im.width, im.height, -h, h, size, size); g.drawImage(im, 0, 0, im.width, im.height, h, h, size, size);
  const o = createCanvas(size, size), og = o.getContext('2d'); og.drawImage(im, 0, 0, size, size);
  const a = g.getImageData(0, 0, size, size), b = og.getImageData(0, 0, size, size), d = a.data, e = b.data;
  for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) { const dx = Math.abs(x - h) / h, dy = Math.abs(y - h) / h; const w = Math.min(1, Math.max(0, (1 - Math.max(dx, dy)) * 2.2)); const i = (y * size + x) * 4; for (let k = 0; k < 3; k++) d[i + k] = d[i + k] * w + e[i + k] * (1 - w); }
  g.putImageData(a, 0, 0); return c;
}
function tone(c, gain, sat) { const g = c.getContext('2d'), im = g.getImageData(0, 0, c.width, c.height), d = im.data; for (let i = 0; i < d.length; i += 4) { const y = 0.3 * d[i] + 0.59 * d[i + 1] + 0.11 * d[i + 2]; for (let k = 0; k < 3; k++) d[i + k] = Math.min(255, (y + (d[i + k] - y) * sat) * gain); } g.putImageData(im, 0, 0); return c; }
function normalMap(c, strength) {
  const W = c.width, H = c.height, d = c.getContext('2d').getImageData(0, 0, W, H).data; const h = new Float32Array(W * H);
  for (let i = 0; i < W * H; i++) h[i] = (0.3 * d[i * 4] + 0.59 * d[i * 4 + 1] + 0.11 * d[i * 4 + 2]) / 255;
  const o = createCanvas(W, H), og = o.getContext('2d'), img = og.createImageData(W, H), p = img.data;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) { const dx = (h[y * W + (x + 1) % W] - h[y * W + (x - 1 + W) % W]) * strength, dv = (h[((y - 1 + H) % H) * W + x] - h[((y + 1) % H) * W + x]) * strength; const l = Math.sqrt(dx * dx + dv * dv + 1); const i = (y * W + x) * 4; p[i] = 128 + (-dx / l) * 127; p[i + 1] = 128 + (-dv / l) * 127; p[i + 2] = 128 + (1 / l) * 127; p[i + 3] = 255; }
  og.putImageData(img, 0, 0); return o;
}
(async () => {
  for (const [name, file, gain, sat, nstr] of [['hangar_floor', 'concrete_00001_.png', 0.72, 0.8, 2.0], ['hangar_brick', 'brick_00003_.png', 0.7, 0.85, 2.6], ['hangar_metal', 'metal_00001_.png', 0.75, 0.8, 3.0]]) {
    const im = await loadImage(SRC + file); const c = seamless(im, 1024); fs.writeFileSync(OUT + name + '_n.png', normalMap(c, nstr).toBuffer('image/png')); tone(c, gain, sat); fs.writeFileSync(OUT + name + '.png', c.toBuffer('image/png')); console.log(name);
  }
})();
