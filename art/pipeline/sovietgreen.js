// The Soviet tanks in 4BO green: the lifted TRELLIS textures come out lime; pulled down to the dull dark olive-green
// of the real paint. Originals kept in art/models/raw-tex. node sovietgreen.js [name ...]
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const M = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Models/', RAW = 'D:/Codes/Projects/lightswarm/art/models/raw-tex/';
fs.mkdirSync(RAW, { recursive: true });
(async () => {
  for (const name of (process.argv.length > 2 ? process.argv.slice(2) : ['t34_85', 'kv85', 'su100', 'is2'])) {
    if (!fs.existsSync(RAW + name + '.png')) fs.copyFileSync(M + name + '.png', RAW + name + '.png');
    const im = await loadImage(RAW + name + '.png'); const c = createCanvas(im.width, im.height), g = c.getContext('2d'); g.drawImage(im, 0, 0);
    const img = g.getImageData(0, 0, c.width, c.height), d = img.data;
    for (let i = 0; i < d.length; i += 4) {
      const y = (0.3 * d[i] + 0.59 * d[i + 1] + 0.11 * d[i + 2]) / 255;
      // desaturate hard, darken, then a whisper of olive: the paint is dull, the tracks and stowage keep their tone
      const k = 0.35; const grey = y * 0.72;
      d[i] = 255 * Math.min(1, (grey + (d[i] / 255 * 0.72 - grey) * k) * 1.0);
      d[i + 1] = 255 * Math.min(1, (grey + (d[i + 1] / 255 * 0.72 - grey) * k) * 1.04);
      d[i + 2] = 255 * Math.min(1, (grey + (d[i + 2] / 255 * 0.72 - grey) * k) * 0.88);
    }
    g.putImageData(img, 0, 0); fs.writeFileSync(M + name + '.png', c.toBuffer('image/png')); console.log(name, im.width);
  }
})();
