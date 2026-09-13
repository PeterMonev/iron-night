// Contact sheet of generated images: node sheet.js <out.png> <cell> <file...>
const {createCanvas, loadImage} = require('@napi-rs/canvas'); const fs = require('fs'); const path = require('path');
(async () => { const [out, cellS, ...files] = process.argv.slice(2); const cell = +cellS; const cols = Math.min(4, files.length), rows = Math.ceil(files.length / cols);
  const c = createCanvas(cols * cell, rows * (cell + 18)), g = c.getContext('2d'); g.fillStyle = '#1a1c20'; g.fillRect(0, 0, c.width, c.height);
  for (let i = 0; i < files.length; i++){ const im = await loadImage(files[i]); const x = (i % cols) * cell, y = Math.floor(i / cols) * (cell + 18); const s = Math.min(cell / im.width, cell / im.height); g.drawImage(im, x, y, im.width * s, im.height * s); g.fillStyle = '#ddd'; g.font = '11px sans-serif'; g.fillText(path.basename(files[i]), x + 4, y + cell + 13); }
  fs.writeFileSync(out, c.toBuffer('image/png')); console.log('sheet', out); })();
