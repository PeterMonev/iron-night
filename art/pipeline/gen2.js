// img2img through ComfyUI: the plan-view schematic in input/guides/<tank>.png fixes the shape and proportions,
// SDXL only "dresses" it in realistic metal. One job per (tank, denoise), several seeds per job via RepeatLatentBatch.
const path = require('path');
const OUT = 'D:/Tools/ComfyUI_windows_portable/ComfyUI/output';
const NEG = 'front view, rear view, three-quarter view, side view, perspective view, angled view, isometric, flat vector, cartoon, clipart, drawing, sketch, illustration, cel shaded, ground, terrain, landscape, shadow on ground, cropped, cut off, multiple vehicles, people, text, watermark, logo, blurry, low quality, frame, border';
const STYLE = 'orthographic plan view photographed from directly above, the gun barrel pointing straight up toward the top of the image, the whole vehicle centered and fully visible, isolated on a plain pure white background, flat diffuse overcast daylight, matte paint, no specular highlights, no reflections, no cast shadow, sharp focus, photorealistic, weathered steel, rivets, welded armour plates, steel tracks with individual track links, hatches, tow cables, stowage, very high detail, 8k';
const SUBJECTS = {
  sherman: 'M4 Sherman medium tank, World War II American tank, olive drab paint, white star on the turret roof, ',
  pz4:     'Panzer IV Ausf. H German medium tank, World War II, muted sand-tan dunkelgelb with olive green camouflage stripes, long 75mm gun with muzzle brake, ',
  tiger:   'Tiger I heavy tank, World War II German tank, matte dusty desaturated sand-ochre dunkelgelb paint like a Tamiya scale model in Dark Yellow, faint olive green camouflage stripes, massive 88mm gun with muzzle brake, wide tracks, ',
  t34:     'T-34-85 Soviet medium tank, World War II, dark green paint, sloped armour, ',
  pak40:   'PaK 40 75mm German anti-tank gun, World War II artillery piece, dark yellow dunkelgelb paint, long barrel with muzzle brake, double gun shield, two wheels, split trail legs spread apart, ',
};
function workflow(prefix, guide, prompt, seed, batch, denoise){ return {
  '1': {class_type:'CheckpointLoaderSimple', inputs:{ckpt_name:'sd_xl_base_1.0.safetensors'}},
  '2': {class_type:'VAELoader', inputs:{vae_name:'sdxl_vae_fp16_fix.safetensors'}},
  '3': {class_type:'CLIPTextEncode', inputs:{clip:['1',1], text:prompt}},
  '4': {class_type:'CLIPTextEncode', inputs:{clip:['1',1], text:NEG}},
  '9': {class_type:'LoadImage', inputs:{image:'guides/' + guide + '.png'}},
  '10': {class_type:'VAEEncode', inputs:{pixels:['9',0], vae:['2',0]}},
  '11': {class_type:'RepeatLatentBatch', inputs:{samples:['10',0], amount:batch}},
  '6': {class_type:'KSampler', inputs:{model:['1',0], positive:['3',0], negative:['4',0], latent_image:['11',0], seed, steps:35, cfg:7, sampler_name:'dpmpp_2m', scheduler:'karras', denoise}},
  '7': {class_type:'VAEDecode', inputs:{samples:['6',0], vae:['2',0]}},
  '8': {class_type:'SaveImage', inputs:{images:['7',0], filename_prefix:prefix}},
}; }
async function run(wf){
  const r = await fetch('http://127.0.0.1:8188/prompt', {method:'POST', headers:{'content-type':'application/json'}, body:JSON.stringify({prompt:wf})});
  const j = await r.json(); if (!j.prompt_id) throw new Error('queue failed: ' + JSON.stringify(j).slice(0, 800));
  for (;;){ await new Promise(res => setTimeout(res, 2000)); const h = await (await fetch('http://127.0.0.1:8188/history/' + j.prompt_id)).json(); const e = h[j.prompt_id];
    if (e && e.status && e.status.completed){ const files = []; for (const n in e.outputs) for (const im of (e.outputs[n].images || [])) files.push(path.join(OUT, im.subfolder || '', im.filename)); return files; }
    if (e && e.status && e.status.status_str === 'error') throw new Error('job error: ' + JSON.stringify(e.status.messages).slice(0, 800)); }
}
module.exports = { run, workflow, SUBJECTS, STYLE, NEG };
if (require.main === module) (async () => {
  const args = process.argv.slice(2); const which = args.filter(a => SUBJECTS[a]); const den = (args.find(a => a.startsWith('d=')) || 'd=0.6').slice(2).split(',').map(Number);
  const batch = Number((args.find(a => a.startsWith('n=')) || 'n=4').slice(2));
  for (const k of (which.length ? which : Object.keys(SUBJECTS))) for (const d of den){
    const t0 = Date.now(); const files = await run(workflow('i2i/' + k + '_d' + Math.round(d * 100), k, SUBJECTS[k] + STYLE, 4242 + k.length, batch, d));
    console.log(k, 'denoise', d, files.length, 'files', Math.round((Date.now() - t0) / 1000) + 's'); }
})();
