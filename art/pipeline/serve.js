// tiny static server for checking the mockups in the browser pane; POST /save?name=x.png stores a screenshot from the page
const http = require('http'), fs = require('fs'), path = require('path');
const types = {'.html': 'text/html; charset=utf-8', '.png': 'image/png', '.glb': 'model/gltf-binary', '.js': 'text/javascript', '.json': 'application/json'};
http.createServer((req, res) => {
  const u = new URL(req.url, 'http://x');
  if (req.method === 'POST' && u.pathname === '/save'){ const chunks = []; req.on('data', c => chunks.push(c)); req.on('end', () => { const name = path.basename(u.searchParams.get('name') || 'shot.png'); fs.writeFileSync(path.join(__dirname, name), Buffer.concat(chunks)); res.writeHead(200); res.end('saved ' + name); }); return; }
  const f = path.join(__dirname, decodeURIComponent(u.pathname).replace(/^\//, '') || 'index.html');
  if (!fs.existsSync(f) || fs.statSync(f).isDirectory()) { res.writeHead(404); return res.end('nope'); }
  res.writeHead(200, {'content-type': types[path.extname(f)] || 'application/octet-stream', 'cache-control': 'no-store'}); fs.createReadStream(f).pipe(res);
}).listen(8787, '127.0.0.1', () => console.log('serving on 8787'));
