// Embeds tank sprites as data URIs into the tank mockup. Keys map to files; swap the map when the AI-drawn tanks arrive.
const fs = require('fs'), path = require('path');
const K = 'D:/Codes/Projects/lightswarm/art/kenney-topdown-tanks-redux/PNG/Retina';
const AI = 'D:/Codes/Projects/lightswarm/art/ai-tanks';
const pick = {
  sherman_hull: [AI + '/sherman_hull.png', K + '/tankBody_green.png'], sherman_turret: [AI + '/sherman_turret.png', K + '/tankGreen_barrel2.png'],
  pz4_hull: [AI + '/pz4_hull.png', K + '/tankBody_dark.png'], pz4_turret: [AI + '/pz4_turret.png', K + '/tankDark_barrel2.png'],
  tiger_hull: [AI + '/tiger_hull.png', K + '/tankBody_darkLarge.png'], tiger_turret: [AI + '/tiger_turret.png', K + '/tankDark_barrel3.png'],
  pak_hull: [AI + '/pak40.png', K + '/specialBarrel1.png'],
  ground: ['D:/Codes/Projects/lightswarm/art/ai-ground/mud2.png'], ground2: ['D:/Codes/Projects/lightswarm/art/ai-ground/grass2.png'],
  t34_hull: [AI + '/t34_hull.png', K + '/tankBody_sand.png'], t34_turret: [AI + '/t34_turret.png', K + '/tankSand_barrel2.png'],
};
const IMG = {}; let ai = 0;
for (const [key, cands] of Object.entries(pick)) { const f = cands.find(p => fs.existsSync(p)); if (f.startsWith(AI)) ai++; IMG[key] = 'data:image/png;base64,' + fs.readFileSync(f).toString('base64'); }
const tpl = fs.readFileSync('tanks-template.html', 'utf8');
const META = fs.existsSync(AI + '/meta.json') ? JSON.parse(fs.readFileSync(AI + '/meta.json', 'utf8')) : {};
fs.writeFileSync('tanks-mockup.html', tpl.replace('/*__IMAGES__*/', 'const IMG = ' + JSON.stringify(IMG) + ';').replace('/*__META__*/', 'const META = ' + JSON.stringify(META) + ';'));
console.log('written; AI sprites used:', ai, 'of', Object.keys(pick).length);
