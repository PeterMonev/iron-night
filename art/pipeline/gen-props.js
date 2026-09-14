// 3/4-view reference renders of field props for image-to-3D, until the ChatGPT ones arrive. Main ComfyUI (SDXL).
const { run } = require('./gen2.js');
const NEG = 'top view, cropped, cut off, multiple objects, people, vehicles in background, text, watermark, logo, blurry, low quality, frame, border, cartoon, drawing, ground, terrain, landscape, shadow, background scenery';
const VIEW = ', 3/4 view from the front-left slightly above, fully visible and centered, clean game asset render, isolated on a plain light gray background, even soft lighting, no cast shadow, sharp focus, photorealistic, high detail';
const P = {
  barn:     'a single old wooden barn with a dark tile roof and large double doors, World War II Normandy countryside, weathered planks' + VIEW,
  wall:     'a single straight section of an old dry stone wall about 6 meters long, partly crumbled at one end, moss and lichen' + VIEW,
  truck:    'a single burnt-out wrecked German Opel Blitz truck, World War II, rusted, canvas gone, one side collapsed' + VIEW,
  haystack: 'a single large round haystack, weathered, some straw fallen at the base' + VIEW,
  deadtree: 'a single dead bare oak tree with a shattered top, thick trunk, no leaves, World War II battlefield' + VIEW,
  sandbags: 'a single semicircular sandbag emplacement three sandbags high with a few wooden ammunition crates inside, World War II' + VIEW,
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
(async () => { for (const k of (process.argv.slice(2).length ? process.argv.slice(2) : Object.keys(P))){ const t0 = Date.now(); const files = await run(wf('props/' + k, P[k], 4711, 3)); console.log(k, files.length, 'files', Math.round((Date.now() - t0) / 1000) + 's'); } })();
