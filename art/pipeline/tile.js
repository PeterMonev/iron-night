// Makes a generated texture seamless: the image is offset by half its size (the seams land in the middle) and the
// original is blended back over the seams with a soft cross mask. node tile.js <in.png> <out.png> [size]
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
(async () => {
  const [inF, outF, sizeS] = process.argv.slice(2); const size = Number(sizeS || 512);
  const im = await loadImage(inF); const W = im.width, H = im.height;
  const c = createCanvas(W, H), g = c.getContext('2d');
  // offset copy: the four quadrants swapped
  g.drawImage(im, W / 2, H / 2, W / 2, H / 2, 0, 0, W / 2, H / 2); g.drawImage(im, 0, H / 2, W / 2, H / 2, W / 2, 0, W / 2, H / 2);
  g.drawImage(im, W / 2, 0, W / 2, H / 2, 0, H / 2, W / 2, H / 2); g.drawImage(im, 0, 0, W / 2, H / 2, W / 2, H / 2, W / 2, H / 2);
  // cover the seams (now a cross through the centre) with the original, feathered
  const m = createCanvas(W, H), mg = m.getContext('2d'); mg.drawImage(im, 0, 0);
  mg.globalCompositeOperation = 'destination-in';
  const band = W * 0.22; const grad = (x0, y0, x1, y1) => { const gr = mg.createLinearGradient(x0, y0, x1, y1); gr.addColorStop(0, 'rgba(0,0,0,0)'); gr.addColorStop(0.5, 'rgba(0,0,0,1)'); gr.addColorStop(1, 'rgba(0,0,0,0)'); return gr; };
  // vertical band around x = W/2 and horizontal band around y = H/2, but keep the original's own edges out (they are the new seams)
  const k = createCanvas(W, H), kg = k.getContext('2d'); kg.fillStyle = grad(W / 2 - band, 0, W / 2 + band, 0); kg.fillRect(W / 2 - band, 0, 2 * band, H);
  kg.globalCompositeOperation = 'lighter'; kg.fillStyle = grad(0, H / 2 - band, 0, H / 2 + band); kg.fillRect(0, H / 2 - band, W, 2 * band);
  mg.drawImage(k, 0, 0);
  g.drawImage(m, 0, 0);
  const o = createCanvas(size, size), og = o.getContext('2d'); og.imageSmoothingQuality = 'high'; og.drawImage(c, 0, 0, size, size);
  fs.writeFileSync(outF, o.toBuffer('image/png')); console.log('tile', outF, size);
})();
