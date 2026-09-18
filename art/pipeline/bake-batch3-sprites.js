// The effect sprites and the UI pictures of the third batch (art/refs/batch3) into the Unity project:
// flipbook sheets and single sprites on black (additive, or alpha from brightness for the blended ones), the parachute
// fabric, the formation icons, the cards, medals, commander portraits, flags, ranks and the key art.
// node bake-batch3-sprites.js
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const IN = 'D:/Codes/Projects/lightswarm/art/refs/batch3/', RES = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/';
const FX = RES + 'Fx/', UI = RES + 'UI/'; for (const d of [FX, UI]) fs.mkdirSync(d, { recursive: true });
const load = n => loadImage(IN + n + '.png');
function save(c, dir, name) { fs.writeFileSync(dir + name + '.png', c.toBuffer('image/png')); console.log(name, c.width + 'x' + c.height); }
function fit(im, w, h) { const c = createCanvas(w, h), g = c.getContext('2d'); g.imageSmoothingQuality = 'high'; g.drawImage(im, 0, 0, w, h); return c; }
function cropSquare(im, size) { const s = Math.min(im.width, im.height); const c = createCanvas(size, size), g = c.getContext('2d'); g.imageSmoothingQuality = 'high'; g.drawImage(im, (im.width - s) / 2, (im.height - s) / 2, s, s, 0, 0, size, size); return c; }
function crop(im, x, y, w, h, outW, outH) { const c = createCanvas(outW, outH), g = c.getContext('2d'); g.imageSmoothingQuality = 'high'; g.drawImage(im, x, y, w, h, 0, 0, outW, outH); return c; }
// alpha from brightness: black is nothing. keepColour false paints the sprite a flat light grey so _BaseColor tints it
function alphaFromBlack(c, gain, keepColour = true, grey = 235, floor = 0.06) {
  const g = c.getContext('2d'), img = g.getImageData(0, 0, c.width, c.height), d = img.data;
  for (let i = 0; i < d.length; i += 4) { const y = (0.3 * d[i] + 0.59 * d[i + 1] + 0.11 * d[i + 2]) / 255; d[i + 3] = Math.round(255 * Math.min(1, Math.max(0, (y - floor) / (1 - floor)) * gain)); if (!keepColour) { d[i] = d[i + 1] = d[i + 2] = grey; } }
  g.putImageData(img, 0, 0);
}
// the black bleed at sprite edges: premultiplied look is avoided by pushing the colour up where alpha is low
function unpremultiply(c) { const g = c.getContext('2d'), img = g.getImageData(0, 0, c.width, c.height), d = img.data; for (let i = 0; i < d.length; i += 4) { const a = d[i + 3] / 255; if (a > 0.01 && a < 1) { for (let k = 0; k < 3; k++) d[i + k] = Math.min(255, d[i + k] / Math.max(a, 0.35)); } } g.putImageData(img, 0, 0); }
// a soft vignette so nothing hard sits at a sprite's border
function vignette(c, start) { const g = c.getContext('2d'), img = g.getImageData(0, 0, c.width, c.height), d = img.data, W = c.width, H = c.height; for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) { const r = Math.hypot((x / W - 0.5) * 2, (y / H - 0.5) * 2); const k = 1 - Math.min(1, Math.max(0, (r - start) / (1 - start))); const i = (y * W + x) * 4; d[i] *= k; d[i + 1] *= k; d[i + 2] *= k; if (d[i + 3] < 255) d[i + 3] *= k; } g.putImageData(img, 0, 0); }

(async () => {
  // --- flipbooks: 4 by 4, kept as sheets; the game steps the UV offset ---
  { const c = fit(await load('fx_explosion_sheet'), 1024, 1024); save(c, FX, 'fx_explosion'); }                        // additive, black stays black
  { const c = fit(await load('fx_smoke_sheet'), 1024, 1024); alphaFromBlack(c, 1.7, true, 225, 0.08); unpremultiply(c); save(c, FX, 'fx_smoke'); }   // blended grey, tinted by the game
  // --- single sprites ---
  { const c = cropSquare(await load('fx_sparks'), 512); vignette(c, 0.55); save(c, FX, 'fx_sparks'); }
  { const c = cropSquare(await load('fx_flak'), 512); alphaFromBlack(c, 2.6, true); unpremultiply(c); vignette(c, 0.5); save(c, FX, 'fx_flak'); }
  { const im = await load('fx_dust'); const c = crop(im, 0, 0, im.width, im.height, 512, 288); const sq = createCanvas(512, 512), g = sq.getContext('2d'); g.drawImage(c, 0, 112); alphaFromBlack(sq, 1.9, true); unpremultiply(sq); vignette(sq, 0.5); save(sq, FX, 'fx_dust'); }
  // the muzzle flashes: three cells, the barrel in each is dark and drops out with the alpha
  { const im = await load('fx_muzzle'); const c = createCanvas(768, 256), g = c.getContext('2d'); g.imageSmoothingQuality = 'high';
    // each flash turned to point up (+v), the way an aligned puff maps its up axis onto the barrel direction
    for (let i = 0; i < 3; i++) { g.save(); g.translate(i * 256 + 128, 128); g.rotate(-Math.PI / 2); g.drawImage(im, i * im.width / 3 + im.width / 3 * 0.4, im.height * 0.2, im.width / 3 * 0.6, im.height * 0.6, -128, -128, 256, 256); g.restore(); }
    save(c, FX, 'fx_muzzle'); }                                                                                       // additive: 3 frames across
  // the tracer: the shell flies right in the picture; the game's tracer quad points +Y, so it is turned nose up
  { const im = await load('fx_tracer'); const c = createCanvas(256, 1024), g = c.getContext('2d'); g.imageSmoothingQuality = 'high';
    g.translate(128, 512); g.rotate(-Math.PI / 2); g.drawImage(im, 0, im.height * 0.25, im.width, im.height * 0.5, -512, -128, 1024, 256); save(c, FX, 'fx_tracer'); }
  // the rain splash: eight frames in a row, a square cut out of each round the splash
  { const im = await load('fx_splash_sheet'); const cell = im.width / 8; const c = createCanvas(1024, 128), g = c.getContext('2d'); g.imageSmoothingQuality = 'high';
    for (let i = 0; i < 8; i++) g.drawImage(im, i * cell, im.height / 2 - cell / 2, cell, cell, i * 128, 0, 128, 128); save(c, FX, 'fx_splash'); }
  // the snowflakes: a 4 by 3 grid the particle system picks random cells from
  { const c = fit(await load('fx_snowflakes'), 1024, 768); alphaFromBlack(c, 1.8, false, 245, 0.16); save(c, FX, 'fx_snowflakes'); }
  // the parachute fabric: seams every 1/5 of the width, tiled three times round the canopy
  { const c = fit(await load('chute_fabric'), 1024, 1024); save(c, FX, 'chute_fabric'); }
  // --- UI ---
  { const im = await load('formation_icons'); const cell = im.width / 4; const c = createCanvas(1024, 256), g = c.getContext('2d'); g.imageSmoothingQuality = 'high';
    for (let i = 0; i < 4; i++) g.drawImage(im, i * cell, 0, cell, im.height, i * 256, 0, 256, 256); alphaFromBlack(c, 2.5, false, 255, 0.3); save(c, UI, 'formation_icons'); }
  for (const n of ['he', 'apcr', 'loader', 'optics', 'engines', 'repair', 'reinf', 'artillery', 'smoke', 'firefly', 'gunner', 'armour', 'crews', 'veteran']) { const c = fit(await load('card_' + n), 512, 512); save(c, UI, 'card_' + n); }
  for (const n of ['recruit', 'nightfighter', 'bocage', 'sharpshooter', 'tankace', 'tigerslayer', 'gunbuster', 'trenchbroom', 'pathfinder', 'aceofaces', 'oldguard', 'ironnight']) { const c = fit(await load('medal_' + n), 512, 512); save(c, UI, 'medal_' + n); }
  for (const n of ['us_sergeant', 'us_lieutenant', 'us_corporal', 'su_captain', 'su_woman', 'su_siberian']) { const im = await load('cmd_' + n); const c = crop(im, 0, 0, im.width, im.width, 512, 512); save(c, UI, 'cmd_' + n); }
  { const c = fit(await load('flags'), 1024, 512); save(c, UI, 'flags'); }
  { const c = fit(await load('rank_insignia'), 1024, 512); save(c, UI, 'rank_insignia'); }
  { const im = await load('keyart'); const c = crop(im, 0, 0, im.width, im.height, 720, 1280); save(c, UI, 'keyart'); }
  { const c = fit(await load('feature'), 1024, 500); fs.mkdirSync('D:/Codes/Projects/lightswarm/docs/store', { recursive: true }); fs.writeFileSync('D:/Codes/Projects/lightswarm/docs/store/feature.png', c.toBuffer('image/png')); console.log('feature 1024x500 -> docs/store'); }
})();
