// Renders every split tank (hull + turret at several turret angles) on a dark ground at game scale and at 2.5x.
const {createCanvas,loadImage}=require('@napi-rs/canvas');const fs=require('fs');
(async()=>{const A='D:/Codes/Projects/lightswarm/art/ai-tanks';const meta=JSON.parse(fs.readFileSync(A+'/meta.json','utf8'));
const names=['sherman','pz4','tiger','t34'];const c=createCanvas(1000,640),g=c.getContext('2d');g.fillStyle='#2e3a26';g.fillRect(0,0,1000,640);
for(let n=0;n<names.length;n++){const nm=names[n];const hull=await loadImage(`${A}/${nm}_hull.png`),tur=await loadImage(`${A}/${nm}_turret.png`);const mh=meta[nm+'_hull'],mt=meta[nm+'_turret'];
  const s=170/hull.height;const y=150+ (n%2)*300, x0=130+Math.floor(n/2)*500;
  for(let i=0;i<3;i++){const x=x0+i*150;g.save();g.translate(x,y);g.scale(s,s);g.rotate(i===2?-0.6:0);g.drawImage(hull,-mh.px,-mh.py);g.rotate(i*Math.PI/3);g.drawImage(tur,-mt.px,-mt.py);g.restore();}
  const s2=64/hull.height;for(let i=0;i<4;i++){const x=x0-60+i*40;g.save();g.translate(x,y+120);g.scale(s2,s2);g.drawImage(hull,-mh.px,-mh.py);g.rotate(i*Math.PI/4);g.drawImage(tur,-mt.px,-mt.py);g.restore();}
  g.fillStyle='#ddd';g.font='14px sans-serif';g.fillText(nm,x0-100,y-110);}
fs.writeFileSync('comp-check.png',c.toBuffer('image/png'));})()
