// Removes the white studio background from a generated tank image: flood fill from the borders (so white markings inside
// the tank survive), soft alpha at the edge, crop to the content. node cutout.js <in.png> <out.png> [tolerance]
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
async function cutout(inFile, outFile, tol = 40){
  const im = await loadImage(inFile); const W = im.width, H = im.height;
  const c = createCanvas(W, H), g = c.getContext('2d'); g.drawImage(im, 0, 0); const id = g.getImageData(0, 0, W, H), d = id.data;
  // background = pixels reachable from the border whose distance from white is below the tolerance
  const bg = new Uint8Array(W * H); const stack = [];
  const isWhite = i => (255 - d[i * 4]) + (255 - d[i * 4 + 1]) + (255 - d[i * 4 + 2]) < tol * 3;
  for (let x = 0; x < W; x++){ stack.push(x, (H - 1) * W + x); } for (let y = 0; y < H; y++){ stack.push(y * W, y * W + W - 1); }
  while (stack.length){ const i = stack.pop(); if (bg[i] || !isWhite(i)) continue; bg[i] = 1; const x = i % W, y = (i / W) | 0;
    if (x > 0) stack.push(i - 1); if (x < W - 1) stack.push(i + 1); if (y > 0) stack.push(i - W); if (y < H - 1) stack.push(i + W); }
  // alpha: background 0, content 255, one-pixel soft edge based on how white the edge pixel is
  let minX = W, minY = H, maxX = -1, maxY = -1;
  for (let i = 0; i < W * H; i++){ const x = i % W, y = (i / W) | 0;
    if (bg[i]) { d[i * 4 + 3] = 0; continue; }
    const nearBg = (x > 0 && bg[i - 1]) || (x < W - 1 && bg[i + 1]) || (y > 0 && bg[i - W]) || (y < H - 1 && bg[i + W]);
    if (nearBg){ const wh = ((255 - d[i * 4]) + (255 - d[i * 4 + 1]) + (255 - d[i * 4 + 2])) / 3; d[i * 4 + 3] = Math.max(0, Math.min(255, Math.round(wh * 255 / (tol * 2)))); }
    if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; }
  g.putImageData(id, 0, 0);
  const pad = 4, w = maxX - minX + 1 + 2 * pad, h = maxY - minY + 1 + 2 * pad; const o = createCanvas(w, h);
  o.getContext('2d').drawImage(c, minX - pad, minY - pad, w, h, 0, 0, w, h);
  fs.writeFileSync(outFile, o.toBuffer('image/png')); return { w, h };
}
module.exports = { cutout };
if (require.main === module) cutout(process.argv[2], process.argv[3], Number(process.argv[4] || 40)).then(r => console.log('cutout', process.argv[3], r.w + 'x' + r.h));
