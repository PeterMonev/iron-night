// Paints the reference's white stars out of a tank texture (the game draws its own, centred, crisp): every
// bright low-saturation blob of a star's size is filled with the olive around it. node unstar.js [name ...] (default sherman)
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const M = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Models/', RAW = 'D:/Codes/Projects/lightswarm/art/models/raw-tex/';
(async () => { for (const name of (process.argv.length > 2 ? process.argv.slice(2) : ['sherman'])) {
  if (!fs.existsSync(RAW + name + '.png')) fs.copyFileSync(M + name + '.png', RAW + name + '.png');
  const im = await loadImage(RAW + name + '.png'); const W = im.width, H = im.height; const c = createCanvas(W, H), g = c.getContext('2d'); g.drawImage(im, 0, 0);
  const img = g.getImageData(0, 0, W, H), d = img.data; const white = new Uint8Array(W * H);
  for (let i = 0; i < W * H; i++) { const r = d[i * 4], gg = d[i * 4 + 1], b = d[i * 4 + 2]; const mx = Math.max(r, gg, b), mn = Math.min(r, gg, b); if (mx > 165 && (mx - mn) < 0.22 * mx) white[i] = 1; }
  // connected blobs (4-neighbour flood fill)
  const label = new Int32Array(W * H); let n = 0; const blobs = []; const stack = [];
  for (let s = 0; s < W * H; s++) { if (!white[s] || label[s]) continue; n++; const px = []; stack.push(s); label[s] = n;
    while (stack.length) { const p = stack.pop(); px.push(p); const x = p % W, y = (p - x) / W; for (const [dx, dy] of [[1,0],[-1,0],[0,1],[0,-1]]) { const nx = x + dx, ny = y + dy; if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue; const q = ny * W + nx; if (white[q] && !label[q]) { label[q] = n; stack.push(q); } } }
    blobs.push(px); }
  let painted = 0;
  for (const px of blobs) {
    if (px.length < 25 || px.length > 6000) continue;   // specks and big light plates stay
    // the ring round the blob: dilate 5 px, take the median colour of the non-white pixels there
    const ring = new Map(); for (const p of px) { const x = p % W, y = (p - x) / W; for (let dy = -7; dy <= 7; dy++) for (let dx = -7; dx <= 7; dx++) { if (Math.abs(dx) < 3 && Math.abs(dy) < 3) continue; const nx = x + dx, ny = y + dy; if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue; const q = ny * W + nx; if (!white[q]) ring.set(q, 1); } }
    if (ring.size < 10) continue; const rs = [], gs = [], bs = []; for (const q of ring.keys()) { rs.push(d[q * 4]); gs.push(d[q * 4 + 1]); bs.push(d[q * 4 + 2]); }
    const med = a => { a.sort((u, v) => u - v); return a[a.length >> 1]; }; const mr = med(rs), mg = med(gs), mb = med(bs);
    // fill the blob and a 2 px rim round it (the anti-aliased edge of the star is not white enough to be in the blob)
    const fill = new Set(px); for (const p of px) { const x = p % W, y = (p - x) / W; for (let dy = -2; dy <= 2; dy++) for (let dx = -2; dx <= 2; dx++) { const nx = x + dx, ny = y + dy; if (nx >= 0 && ny >= 0 && nx < W && ny < H) fill.add(ny * W + nx); } }
    for (const p of fill) { const x = p % W, y = (p - x) / W; const jitter = ((x * 7 + y * 13) % 9) - 4; d[p * 4] = mr + jitter; d[p * 4 + 1] = mg + jitter; d[p * 4 + 2] = mb + jitter; }
    painted++;
  }
  g.putImageData(img, 0, 0); fs.writeFileSync(M + name + '.png', c.toBuffer('image/png')); console.log(name, 'blobs', blobs.length, 'painted out', painted);
} })();
