// Runs the squadron mockup headlessly with a real canvas and saves frames, so the composition can be inspected.
const fs = require('fs');
const { createCanvas, loadImage, DOMMatrix } = require('@napi-rs/canvas');
const html = fs.readFileSync('tanks-mockup.html', 'utf8');
const src = html.slice(html.indexOf('<script>') + 8, html.lastIndexOf('</script>'));
const W = 390, H = 845;
const game = createCanvas(W, H);
const stub = (tag) => new Proxy(function(){}, { get(t, p){ if (p === Symbol.toPrimitive) return () => 0; if (p === 'then') return undefined; if (p === 'style') return {}; if (p === 'classList') return {add(){}, remove(){}, toggle(){}}; if (p === 'checked') return false; if (p === 'textContent' || p === 'innerHTML') return ''; if (p === 'length') return 0; if (p === 'getBoundingClientRect') return () => ({width: W, height: H, left: 0, top: 0}); if (p === 'addEventListener') return () => {}; return stub(p); }, set(){ return true; }, apply(t, th, args){ return stub(args[0]); }, construct(){ return stub(); } });
global.document = new Proxy({}, { get(t, p){ if (p === 'createElement') return (tag) => tag === 'canvas' ? createCanvas(1, 1) : stub(tag); if (p === 'getElementById') return (id) => id === 'game' ? game : stub(id); if (p === 'querySelectorAll') return () => ({forEach(){}}); return stub(p); } });
game.getBoundingClientRect = () => ({width: W, height: H, left: 0, top: 0}); game.style = {}; game.addEventListener = () => {}; game.setPointerCapture = () => {};
global.window = {devicePixelRatio: 1, __DAY: process.argv.includes('day')}; global.ResizeObserver = class { observe(){} }; global.DOMMatrix = DOMMatrix;
let now = 0, pending = null; global.performance = {now: () => now}; global.requestAnimationFrame = cb => { pending = cb; };
(async () => {
  // pre-decode every embedded sprite, then hand the page decoded images instead of letting it create Image objects
  const st = src.indexOf('const IMG = ') + 12; const imgSrc = src.slice(st, src.indexOf('};', st) + 1);
  const IMG = JSON.parse(imgSrc); const decoded = {};
  for (const k in IMG) decoded[k] = await loadImage(Buffer.from(IMG[k].split(',')[1], 'base64'));
  global.__DECODED = decoded;
  const patched = src.replace('for (const k in IMG) PIC[k] = img(IMG[k]);', 'for (const k in IMG) PIC[k] = global.__DECODED[k];');
  eval(patched);
  const P = process.argv.includes('day') ? 'day-' : ''; const shots = {180: P + 'tank-frame-3s.png', 600: P + 'tank-frame-10s.png', 1500: P + 'tank-frame-25s.png'};
  for (let f = 1; f <= 1500 && pending; f++){ const cb = pending; pending = null; now += 16.7; cb(now); if (shots[f]) fs.writeFileSync(shots[f], game.toBuffer('image/png')); }
  const c = createCanvas(3 * W, H), g = c.getContext('2d');
  for (let i = 0; i < 3; i++) g.drawImage(await loadImage(Object.values(shots)[i]), i * W, 0);
  fs.writeFileSync(P + 'tank-frames.png', c.toBuffer('image/png')); console.log('frames done');
})();
