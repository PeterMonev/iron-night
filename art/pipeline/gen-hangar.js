// Textures for the hangar behind the menu and the depot: SDXL text-to-image on the ComfyUI at the given port.
// node gen-hangar.js [port] [name ...]   -> ComfyUI/output/hangar/<name>_*.png (three each)
const path = require('path'); const OUT = 'D:/Tools/ComfyUI_windows_portable/ComfyUI/output';
const PORT = Number(process.argv[2]) || 8189;
const NEG = 'people, text, watermark, logo, blurry, cartoon, painting, perspective, vanishing point, horizon, sky, furniture, vehicle';
const P = {
  concrete: 'flat lay photograph of an old concrete hangar floor seen exactly from above, orthographic, worn grey concrete with fine cracks, dark oil stains, faint tyre marks, dust in the corners, evenly lit, seamless texture, no perspective',
  brick:    'straight-on photograph of an old dark red brick wall of a military workshop, weathered bricks with grey mortar, soot and damp stains, evenly lit, flat frontal view, seamless texture, no perspective',
  metal:    'straight-on photograph of a corrugated galvanised steel wall panel, dull grey zinc with rust streaks and rivets, flat frontal view, evenly lit, seamless texture, no perspective',
};
function wf(prefix, prompt, seed, batch){ return {
  '1': {class_type:'CheckpointLoaderSimple', inputs:{ckpt_name:'sd_xl_base_1.0.safetensors'}},
  '2': {class_type:'VAELoader', inputs:{vae_name:'sdxl_vae_fp16_fix.safetensors'}},
  '3': {class_type:'CLIPTextEncode', inputs:{clip:['1',1], text:prompt}},
  '4': {class_type:'CLIPTextEncode', inputs:{clip:['1',1], text:NEG}},
  '5': {class_type:'EmptyLatentImage', inputs:{width:1024, height:1024, batch_size:batch}},
  '6': {class_type:'KSampler', inputs:{model:['1',0], positive:['3',0], negative:['4',0], latent_image:['5',0], seed, steps:30, cfg:6.5, sampler_name:'dpmpp_2m', scheduler:'karras', denoise:1}},
  '7': {class_type:'VAEDecode', inputs:{samples:['6',0], vae:['2',0]}},
  '8': {class_type:'SaveImage', inputs:{images:['7',0], filename_prefix:prefix}},
}; }
async function run(w){
  const r = await fetch(`http://127.0.0.1:${PORT}/prompt`, {method:'POST', headers:{'content-type':'application/json'}, body:JSON.stringify({prompt:w})});
  const j = await r.json(); if (!j.prompt_id) throw new Error('queue failed: ' + JSON.stringify(j).slice(0, 800));
  for (;;){ await new Promise(res => setTimeout(res, 2000)); const h = await (await fetch(`http://127.0.0.1:${PORT}/history/` + j.prompt_id)).json(); const e = h[j.prompt_id];
    if (e && e.status && e.status.completed){ const files = []; for (const n in e.outputs) for (const im of (e.outputs[n].images || [])) files.push(path.join(OUT, im.subfolder || '', im.filename)); return files; }
    if (e && e.status && e.status.status_str === 'error') throw new Error('job error: ' + JSON.stringify(e.status.messages).slice(0, 800)); }
}
(async () => { const names = process.argv.slice(3); for (const k of (names.length ? names : Object.keys(P))){ const t0 = Date.now(); const files = await run(wf('hangar/' + k, P[k], 11 + k.length, 3)); console.log(k, files.length, 'files', Math.round((Date.now() - t0) / 1000) + ' s'); } })();
