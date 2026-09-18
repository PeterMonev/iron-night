// German vehicles in Panzergrau: the dunkelgelb TRELLIS textures go to a dark blue-grey, the camouflage staying as a
// faint tone difference. The raw (yellow) copies are kept in art/models/raw-tex. node panzergrau.js
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const M = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Models/', RAW = 'D:/Codes/Projects/lightswarm/art/models/raw-tex/';
fs.mkdirSync(RAW, { recursive: true });
(async () => {
  for (const name of (process.argv.length > 2 ? process.argv.slice(2) : ['pz4', 'tiger', 'panther', 'stug', 'halftrack', 'pak40'])) {
    if (!fs.existsSync(RAW + name + '.png')) fs.copyFileSync(M + name + '.png', RAW + name + '.png');
    const im = await loadImage(RAW + name + '.png'); const c = createCanvas(im.width, im.height), g = c.getContext('2d'); g.drawImage(im, 0, 0);
    const img = g.getImageData(0, 0, c.width, c.height), d = img.data;
    for (let i = 0; i < d.length; i += 4) {
      const y = (0.3 * d[i] + 0.59 * d[i + 1] + 0.11 * d[i + 2]) / 255;
      // yellow paint sits around 0.55-0.75 luminance: map the whole range down to Panzergrau, keep the darks (tracks, shadows)
      const grey = Math.pow(y, 1.25) * 0.62 + 0.06;
      d[i] = 255 * grey * 0.96; d[i + 1] = 255 * grey * 0.99; d[i + 2] = 255 * grey * 1.06;
    }
    g.putImageData(img, 0, 0); fs.writeFileSync(M + name + '.png', c.toBuffer('image/png')); console.log(name, im.width);
  }
})();
