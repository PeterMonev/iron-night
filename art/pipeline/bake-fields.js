// Bakes the ground set for the 3D field: four seamless 2048 px field tiles (40 m each: plough, pasture, mown hay,
// stubble), a dirt lane strip, a farmyard decal, a shell crater decal and the hedge foliage texture. Sources are the
// SDXL seamless 512 px tiles in art/ai-ground; everything wraps at the edges.
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const A = 'D:/Codes/Projects/lightswarm/art/ai-ground', OUT = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Textures';
let seed = 71; const rnd = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
const save = (c, name) => fs.writeFileSync(`${OUT}/${name}.png`, c.toBuffer('image/png'));
// moonlight takes the colour out of everything: pull the saturation down and the level with it
function night(c, sat, gain) { const g = c.getContext('2d'), img = g.getImageData(0, 0, c.width, c.height), d = img.data; for (let i = 0; i < d.length; i += 4) { const y = 0.3 * d[i] + 0.59 * d[i + 1] + 0.11 * d[i + 2]; d[i] = (y + (d[i] - y) * sat) * gain; d[i + 1] = (y + (d[i + 1] - y) * sat) * gain; d[i + 2] = (y + (d[i + 2] - y) * sat) * gain; } g.putImageData(img, 0, 0); }
const WRAP = [[0, 0], [1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [-1, -1], [1, -1], [-1, 1]];

// tiles a source across the canvas (n x n repeats) so the seamless source stays seamless
function tile(g, img, S, n) { const s = S / n; for (let y = 0; y < n; y++) for (let x = 0; x < n; x++) g.drawImage(img, x * s, y * s, s, s); }
// soft blobs of another tile: masks the tiled source with radial gradients, stamped with wrap
function blobs(g, img, S, count, rmin, rmax, alpha, n = 4) {
  const pc = createCanvas(S, S), pg = pc.getContext('2d'); tile(pg, img, S, n);
  const mc = createCanvas(S, S), mg = mc.getContext('2d');
  for (let i = 0; i < count; i++) {
    const x = rnd() * S, y = rnd() * S, r = rmin + rnd() * (rmax - rmin);
    for (const [wx, wy] of WRAP) { const dx = wx * S, dy = wy * S; const gr = mg.createRadialGradient(x + dx, y + dy, r * 0.2, x + dx, y + dy, r); gr.addColorStop(0, `rgba(0,0,0,${alpha})`); gr.addColorStop(1, 'rgba(0,0,0,0)'); mg.fillStyle = gr; mg.fillRect(x + dx - r, y + dy - r, 2 * r, 2 * r); }
  }
  pg.globalCompositeOperation = 'destination-in'; pg.drawImage(mc, 0, 0); g.drawImage(pc, 0, 0);
}
// large slow tone variation so the repeat is less obvious: a few huge soft dark or light blobs
function tone(g, S, count, dark, strength = 1) {
  for (let i = 0; i < count; i++) {
    const x = rnd() * S, y = rnd() * S, r = 300 + rnd() * 500;
    for (const [wx, wy] of WRAP) { const dx = wx * S, dy = wy * S; const gr = g.createRadialGradient(x + dx, y + dy, 0, x + dx, y + dy, r); gr.addColorStop(0, dark ? 'rgba(10,8,4,' + 0.22 * strength + ')' : 'rgba(190,175,140,' + 0.14 * strength + ')'); gr.addColorStop(1, 'rgba(0,0,0,0)'); g.fillStyle = gr; g.fillRect(x + dx - r, y + dy - r, 2 * r, 2 * r); }
  }
}

(async () => {
  const S = 2048;
  const field = await loadImage(A + '/field.png'), grass = await loadImage(A + '/grass.png'), grass2 = await loadImage(A + '/grass2.png'), mud = await loadImage(A + '/mud.png'), mud2 = await loadImage(A + '/mud2.png');
  // plough: dark furrows with drier lighter streaks
  { const c = createCanvas(S, S), g = c.getContext('2d'); tile(g, mud2, S, 4); blobs(g, field, S, 14, 200, 500, 0.7); blobs(g, mud, S, 6, 150, 350, 0.35); tone(g, S, 5, false); tone(g, S, 4, true); night(c, 0.6, 0.85); save(c, 'field_plough'); }
  // pasture: tufty grass with worn earth patches
  { const c = createCanvas(S, S), g = c.getContext('2d'); tile(g, grass, S, 4); blobs(g, mud, S, 9, 120, 320, 0.55); blobs(g, grass2, S, 5, 250, 500, 0.5); tone(g, S, 5, true); tone(g, S, 3, false); night(c, 0.55, 0.8); save(c, 'field_pasture'); }
  // mown hay: striped field with a few patches
  { const c = createCanvas(S, S), g = c.getContext('2d'); tile(g, grass2, S, 4); blobs(g, grass, S, 7, 150, 380, 0.5); blobs(g, mud, S, 4, 100, 240, 0.4); tone(g, S, 5, true); tone(g, S, 3, false); night(c, 0.5, 0.72); save(c, 'field_mown'); }
  // stubble: pale dry earth with yellowed tufts
  { const c = createCanvas(S, S), g = c.getContext('2d'); tile(g, mud, S, 4); blobs(g, grass, S, 12, 150, 400, 0.6); g.fillStyle = 'rgba(150,120,60,0.12)'; g.fillRect(0, 0, S, S); tone(g, S, 5, true); tone(g, S, 3, false); night(c, 0.6, 0.85); save(c, 'field_stubble'); }

  // lane: 512 x 2048 strip, wraps along its length; dry earth, two dark ruts, a grass crown in the middle, soft edges
  {
    const W = 512, H = 2048, c = createCanvas(W, H), g = c.getContext('2d');
    for (let y = 0; y < H; y += 512) g.drawImage(mud, 0, y, 512, 512);
    const pc = createCanvas(W, H), pg = pc.getContext('2d'); for (let y = 0; y < H; y += 512) pg.drawImage(grass, 0, y, 512, 512);
    const mc = createCanvas(W, H), mg = mc.getContext('2d'); const gr = mg.createLinearGradient(0, 0, W, 0); gr.addColorStop(0.36, 'rgba(0,0,0,0)'); gr.addColorStop(0.5, 'rgba(0,0,0,0.75)'); gr.addColorStop(0.64, 'rgba(0,0,0,0)'); mg.fillStyle = gr; mg.fillRect(0, 0, W, H);
    pg.globalCompositeOperation = 'destination-in'; pg.drawImage(mc, 0, 0); g.drawImage(pc, 0, 0);
    for (const cx of [150, 362]) {
      g.strokeStyle = 'rgba(40,30,18,0.7)'; g.lineWidth = 26; g.beginPath();
      for (let y = -64; y <= H + 64; y += 32) { const x = cx + Math.sin(y / H * Math.PI * 4) * 9 + Math.sin(y / H * Math.PI * 10) * 4; if (y === -64) g.moveTo(x, y); else g.lineTo(x, y); }
      g.stroke();
    }
    g.globalCompositeOperation = 'destination-in'; const eg = g.createLinearGradient(0, 0, W, 0); eg.addColorStop(0, 'rgba(0,0,0,0)'); eg.addColorStop(0.16, 'rgba(0,0,0,1)'); eg.addColorStop(0.84, 'rgba(0,0,0,1)'); eg.addColorStop(1, 'rgba(0,0,0,0)'); g.fillStyle = eg; g.fillRect(0, 0, W, H);
    night(c, 0.55, 0.8); save(c, 'lane');
  }

  // farmyard: trodden earth disc with a soft edge
  {
    const S2 = 1024, c = createCanvas(S2, S2), g = c.getContext('2d'); tile(g, mud, S2, 2); blobs(g, mud2, S2, 6, 100, 260, 0.4, 2);
    g.globalCompositeOperation = 'destination-in'; const gr = g.createRadialGradient(S2 / 2, S2 / 2, S2 * 0.3, S2 / 2, S2 / 2, S2 * 0.5); gr.addColorStop(0, 'rgba(0,0,0,1)'); gr.addColorStop(1, 'rgba(0,0,0,0)'); g.fillStyle = gr; g.fillRect(0, 0, S2, S2);
    night(c, 0.55, 0.8); save(c, 'yard');
  }
  // crater: scorched bowl, dark rim, thrown pale earth
  {
    const S2 = 256, c = createCanvas(S2, S2), g = c.getContext('2d'); const m = S2 / 2;
    const gr = g.createRadialGradient(m, m, 0, m, m, m); gr.addColorStop(0, 'rgba(14,10,6,1)'); gr.addColorStop(0.45, 'rgba(26,20,12,0.95)'); gr.addColorStop(0.62, 'rgba(60,48,30,0.8)'); gr.addColorStop(0.8, 'rgba(120,104,78,0.45)'); gr.addColorStop(1, 'rgba(120,104,78,0)'); g.fillStyle = gr; g.fillRect(0, 0, S2, S2);
    for (let i = 0; i < 40; i++) { const a = rnd() * 6.283, d = S2 * (0.3 + rnd() * 0.2), r = 2 + rnd() * 5; g.fillStyle = `rgba(${90 + rnd() * 50 | 0},${75 + rnd() * 40 | 0},${50 + rnd() * 30 | 0},${0.3 + rnd() * 0.4})`; g.beginPath(); g.arc(m + Math.cos(a) * d, m + Math.sin(a) * d, r, 0, 7); g.fill(); }
    save(c, 'crater');
  }

  // hedge foliage: periodic value noise in greens, darker towards the bottom (V is height on the bushes)
  {
    const S2 = 256, c = createCanvas(S2, S2), g = c.getContext('2d'); const img = g.createImageData(S2, S2), d = img.data;
    const lat = n => { const a = []; for (let i = 0; i < n * n; i++) a.push(rnd()); return (x, y) => a[((y % n + n) % n) * n + ((x % n + n) % n)]; };
    const octs = [[8, 1], [16, 0.6], [32, 0.45], [64, 0.35]].map(([n, w]) => [n, w, lat(n)]);
    const sm = t => t * t * (3 - 2 * t);
    for (let y = 0; y < S2; y++) for (let x = 0; x < S2; x++) {
      let v = 0, wsum = 0;
      for (const [n, w, L] of octs) { const fx = x / S2 * n, fy = y / S2 * n, ix = Math.floor(fx), iy = Math.floor(fy), tx = sm(fx - ix), ty = sm(fy - iy); const a = L(ix, iy), b = L(ix + 1, iy), c2 = L(ix, iy + 1), e = L(ix + 1, iy + 1); v += w * ((a * (1 - tx) + b * tx) * (1 - ty) + (c2 * (1 - tx) + e * tx) * ty); wsum += w; }
      v /= wsum; const h = 1 - y / S2; const shade = 0.45 + 0.55 * Math.pow(h, 0.8); const leaf = 0.6 + 1.3 * (v - 0.5);
      const i = (y * S2 + x) * 4; d[i] = 72 * leaf * shade + 18; d[i + 1] = 118 * leaf * shade + 26; d[i + 2] = 48 * leaf * shade + 12; d[i + 3] = 255;
    }
    g.putImageData(img, 0, 0); night(c, 0.6, 0.55); save(c, 'hedge');
  }
  // the Ardennes: the same four fields under snow. The ground shows through where a noise mask says so (ragged
  // patches, not circles), a snow lane with dark ruts, a snow yard.
  const noiseMask = (S, cells, threshold, soft) => {
    const mc = createCanvas(S, S), mg = mc.getContext('2d'); const img = mg.createImageData(S, S), d = img.data;
    const lat = n => { const a = []; for (let i = 0; i < n * n; i++) a.push(rnd()); return (x, y) => a[((y % n + n) % n) * n + ((x % n + n) % n)]; };
    const octs = [[cells, 1], [cells * 2, 0.5], [cells * 4, 0.25]].map(([n, w]) => [n, w, lat(n)]); const sm = t => t * t * (3 - 2 * t); const step = 4;
    for (let y = 0; y < S; y += step) for (let x = 0; x < S; x += step) {
      let v = 0, ws = 0; for (const [n, w, L] of octs) { const fx = x / S * n, fy = y / S * n, ix = Math.floor(fx), iy = Math.floor(fy), tx = sm(fx - ix), ty = sm(fy - iy); v += w * ((L(ix, iy) * (1 - tx) + L(ix + 1, iy) * tx) * (1 - ty) + (L(ix, iy + 1) * (1 - tx) + L(ix + 1, iy + 1) * tx) * ty); ws += w; }
      v /= ws; const a = Math.max(0, Math.min(1, (v - threshold) / soft)) * 255;
      for (let yy = 0; yy < step; yy++) for (let xx = 0; xx < step; xx++) { const i = ((y + yy) * S + x + xx) * 4; d[i + 3] = a; }
    }
    mg.putImageData(img, 0, 0); return mc;
  };
  const snowBase = (g, S) => { g.fillStyle = 'rgb(214,218,226)'; g.fillRect(0, 0, S, S); const img = g.getImageData(0, 0, S, S), d = img.data; let sd = 5; const r2 = () => (sd = (sd * 16807) % 2147483647) / 2147483647; for (let i = 0; i < d.length; i += 4) { const v = (r2() - 0.5) * 14; d[i] += v; d[i + 1] += v; d[i + 2] += v; } g.putImageData(img, 0, 0); };
  const through = (g, img, S, cells, threshold, soft, alpha) => { const pc = createCanvas(S, S), pg = pc.getContext('2d'); tile(pg, img, S, 4); pg.globalCompositeOperation = 'destination-in'; pg.drawImage(noiseMask(S, cells, threshold, soft), 0, 0); g.globalAlpha = alpha; g.drawImage(pc, 0, 0); g.globalAlpha = 1; };
  { const c = createCanvas(S, S), g = c.getContext('2d'); snowBase(g, S); g.globalAlpha = 0.3; tile(g, mud2, S, 4); g.globalAlpha = 1; through(g, field, S, 6, 0.56, 0.12, 0.8); tone(g, S, 5, true, 0.3); night(c, 0.5, 0.62); save(c, 'field_snow_plough'); }
  { const c = createCanvas(S, S), g = c.getContext('2d'); snowBase(g, S); through(g, grass, S, 8, 0.55, 0.1, 0.85); through(g, mud, S, 5, 0.62, 0.1, 0.6); tone(g, S, 5, true, 0.3); night(c, 0.45, 0.62); save(c, 'field_snow_pasture'); }
  { const c = createCanvas(S, S), g = c.getContext('2d'); snowBase(g, S); g.globalAlpha = 0.2; tile(g, grass2, S, 4); g.globalAlpha = 1; through(g, grass2, S, 7, 0.6, 0.1, 0.7); tone(g, S, 5, true, 0.3); night(c, 0.45, 0.62); save(c, 'field_snow_mown'); }
  { const c = createCanvas(S, S), g = c.getContext('2d'); snowBase(g, S); through(g, mud, S, 7, 0.5, 0.12, 0.85); through(g, grass, S, 9, 0.62, 0.08, 0.6); tone(g, S, 5, true, 0.3); night(c, 0.5, 0.62); save(c, 'field_snow_stubble'); }
  {
    const W = 512, H = 2048, c = createCanvas(W, H), g = c.getContext('2d'); g.fillStyle = 'rgb(190,192,198)'; g.fillRect(0, 0, W, H); g.globalAlpha = 0.5; for (let y = 0; y < H; y += 512) g.drawImage(mud, 0, y, 512, 512); g.globalAlpha = 1;
    for (const cx of [150, 362]) { g.strokeStyle = 'rgba(50,42,34,0.75)'; g.lineWidth = 30; g.beginPath(); for (let y = -64; y <= H + 64; y += 32) { const x = cx + Math.sin(y / H * Math.PI * 4) * 9 + Math.sin(y / H * Math.PI * 10) * 4; if (y === -64) g.moveTo(x, y); else g.lineTo(x, y); } g.stroke(); }
    g.globalCompositeOperation = 'destination-in'; const eg = g.createLinearGradient(0, 0, W, 0); eg.addColorStop(0, 'rgba(0,0,0,0)'); eg.addColorStop(0.16, 'rgba(0,0,0,1)'); eg.addColorStop(0.84, 'rgba(0,0,0,1)'); eg.addColorStop(1, 'rgba(0,0,0,0)'); g.fillStyle = eg; g.fillRect(0, 0, W, H);
    night(c, 0.5, 0.62); save(c, 'lane_snow');
  }
  {
    const S2 = 1024, c = createCanvas(S2, S2), g = c.getContext('2d'); g.fillStyle = 'rgb(180,178,176)'; g.fillRect(0, 0, S2, S2); g.globalAlpha = 0.6; tile(g, mud, S2, 2); g.globalAlpha = 1; blobs(g, mud2, S2, 6, 100, 260, 0.4, 2);
    g.globalCompositeOperation = 'destination-in'; const gr = g.createRadialGradient(S2 / 2, S2 / 2, S2 * 0.3, S2 / 2, S2 / 2, S2 * 0.5); gr.addColorStop(0, 'rgba(0,0,0,1)'); gr.addColorStop(1, 'rgba(0,0,0,0)'); g.fillStyle = gr; g.fillRect(0, 0, S2, S2);
    night(c, 0.5, 0.62); save(c, 'yard_snow');
  }
  console.log('fields baked');
})();
