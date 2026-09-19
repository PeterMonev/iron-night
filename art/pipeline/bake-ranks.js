// The rank insignia as a clean six-cell sheet with alpha: each chevron cut from the painted row, the dark ground dropped.
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
(async () => {
  const im = await loadImage('D:/Codes/Projects/lightswarm/art/refs/batch3/rank_insignia.png'); const cw = im.width / 6; const c = createCanvas(6 * 256, 256), g = c.getContext('2d'); g.imageSmoothingQuality = 'high';
  for (let i = 0; i < 6; i++) g.drawImage(im, i * cw, im.height * 0.22, cw, im.height * 0.56, i * 256, 0, 256, 256);
  const img = g.getImageData(0, 0, c.width, c.height), d = img.data;
  for (let p = 0; p < d.length; p += 4) { const y = (0.3 * d[p] + 0.59 * d[p + 1] + 0.11 * d[p + 2]) / 255; const a = Math.min(1, Math.max(0, (y - 0.14) / 0.2)); d[p + 3] = Math.round(255 * a); if (a > 0 && a < 1) { const k = 1 / Math.max(a, 0.4); d[p] = Math.min(255, d[p] * k); d[p + 1] = Math.min(255, d[p + 1] * k); d[p + 2] = Math.min(255, d[p + 2] * k); } }
  g.putImageData(img, 0, 0); fs.writeFileSync('D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/UI/rank_insignia.png', c.toBuffer('image/png')); console.log('ranks 1536x256');
})();
