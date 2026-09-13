// Bakes one seamless 2048px ground tile (about 40 m) for the 3D game: mud base, soft grass patches, a dirt track with
// ruts, craters and bush rows. Everything wraps at the edges so the tile repeats without seams.
const { createCanvas, loadImage } = require('@napi-rs/canvas'); const fs = require('fs');
const S = 2048, A = 'D:/Codes/Projects/lightswarm/art/ai-ground';
(async () => {
  const mud = await loadImage(A + '/mud2.png'), grass = await loadImage(A + '/grass2.png');
  const c = createCanvas(S, S), g = c.getContext('2d');
  let seed = 23; const rnd = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
  // draw with wrap: every shape is stamped at the 4 mirrored offsets so it continues across the edges
  const wrap = fn => { for (const [dx, dy] of [[0, 0], [S, 0], [-S, 0], [0, S], [0, -S], [S, S], [-S, -S], [S, -S], [-S, S]]) { g.save(); g.translate(dx, dy); fn(); g.restore(); } };
  for (let y = 0; y < S; y += S / 2) for (let x = 0; x < S; x += S / 2) g.drawImage(mud, x, y, S / 2, S / 2);
  // grass: the grass tile masked by soft blobs
  const pc = createCanvas(S, S), pg = pc.getContext('2d'); for (let y = 0; y < S; y += S / 2) for (let x = 0; x < S; x += S / 2) pg.drawImage(grass, x, y, S / 2, S / 2);
  const mc = createCanvas(S, S), mg = mc.getContext('2d');
  for (let i = 0; i < 26; i++){ const x = rnd() * S, y = rnd() * S, r = 180 + rnd() * 380; for (const [dx, dy] of [[0, 0], [S, 0], [-S, 0], [0, S], [0, -S]]){ const gr = mg.createRadialGradient(x + dx, y + dy, r * 0.25, x + dx, y + dy, r); gr.addColorStop(0, 'rgba(0,0,0,1)'); gr.addColorStop(1, 'rgba(0,0,0,0)'); mg.fillStyle = gr; mg.fillRect(x + dx - r, y + dy - r, 2 * r, 2 * r); } }
  pg.globalCompositeOperation = 'destination-in'; pg.drawImage(mc, 0, 0); g.drawImage(pc, 0, 0);
  // a dirt track that enters and leaves at the same height so it tiles
  const track = () => { g.beginPath(); g.moveTo(0, 900); g.bezierCurveTo(600, 760, 1300, 1120, S, 900); };
  wrap(() => { g.strokeStyle = 'rgba(112,96,72,0.7)'; g.lineWidth = 92; track(); g.stroke(); g.strokeStyle = 'rgba(48,38,26,0.65)'; g.lineWidth = 10; g.save(); g.translate(0, -22); track(); g.stroke(); g.translate(0, 44); track(); g.stroke(); g.restore(); });
  // craters: dark rim, lighter thrown earth
  for (let i = 0; i < 7; i++){ const x = rnd() * S, y = rnd() * S, r = 50 + rnd() * 80; wrap(() => { const gr = g.createRadialGradient(x, y, 0, x, y, r); gr.addColorStop(0, 'rgba(16,12,8,0.6)'); gr.addColorStop(0.55, 'rgba(30,24,16,0.4)'); gr.addColorStop(0.8, 'rgba(120,104,80,0.3)'); gr.addColorStop(1, 'rgba(120,104,80,0)'); g.fillStyle = gr; g.beginPath(); g.arc(x, y, r, 0, 7); g.fill(); }); }
  // bush rows along field edges
  for (let i = 0; i < 5; i++){ const x = rnd() * S, y = rnd() * S, vert = rnd() < 0.5, len = 400 + rnd() * 700; wrap(() => { for (let d = 0; d < len; d += 26){ const bx = vert ? x + (rnd() - 0.5) * 18 : x + d, by = vert ? y + d : y + (rnd() - 0.5) * 18, r = 14 + rnd() * 16; const gr = g.createRadialGradient(bx, by, 0, bx, by, r); gr.addColorStop(0, 'rgba(38,52,24,0.95)'); gr.addColorStop(0.6, 'rgba(24,34,14,0.85)'); gr.addColorStop(1, 'rgba(16,22,10,0)'); g.fillStyle = gr; g.beginPath(); g.arc(bx, by, r, 0, 7); g.fill(); } }); }
  const out = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Textures'; fs.mkdirSync(out, { recursive: true });
  fs.writeFileSync(out + '/ground.png', c.toBuffer('image/png'));
  // a quick 2x2 preview to check the seams
  const p = createCanvas(1024, 1024), pg2 = p.getContext('2d'); for (let y = 0; y < 2; y++) for (let x = 0; x < 2; x++) pg2.drawImage(c, x * 512, y * 512, 512, 512);
  fs.writeFileSync('ground-tile-check.png', p.toBuffer('image/png')); console.log('ground baked');
})();
