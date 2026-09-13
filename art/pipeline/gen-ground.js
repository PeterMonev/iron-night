// Ground textures for the tank mockup: plain SDXL text-to-image, later made tileable by tile.js
const { run } = require('./gen2.js');
const NEG = 'horizon, sky, perspective view, angled view, buildings, vehicles, tanks, people, animals, text, watermark, logo, blurry, low quality, frame, border, cartoon, drawing';
const P = {
  meadow: 'seamless tileable texture, top-down drone photograph straight down of a wet autumn meadow at the edge of a farm field, patchy green and brown grass, bare mud, tyre ruts, small puddles, overcast diffuse light, photorealistic, orthographic, evenly lit, high detail',
  track:  'seamless tileable texture, top-down drone photograph straight down of muddy farmland with a dirt track crossing it, deep tank track ruts in wet brown mud, tufts of grass, autumn, overcast diffuse light, photorealistic, orthographic, evenly lit, high detail',
  mud:   'seamless tileable texture, top-down aerial photograph of a muddy World War II battlefield field in autumn, wet dark dirt, tank track ruts, patches of dead yellow grass, a few small shell craters, puddles, overcast diffuse light, photorealistic, orthographic, evenly lit, high detail',
  field: 'seamless tileable texture, top-down aerial photograph of a ploughed farm field in autumn with furrows, dark soil, sparse stubble, a dirt path crossing it, overcast diffuse light, photorealistic, orthographic, evenly lit, high detail',
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
(async () => { for (const k of (process.argv.slice(2).length ? process.argv.slice(2) : Object.keys(P))){ const t0 = Date.now(); const files = await run(wf('ground/' + k, P[k], 99, 4)); console.log(k, files.length, 'files', Math.round((Date.now() - t0) / 1000) + 's'); } })();
