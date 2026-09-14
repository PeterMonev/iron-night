// App icon candidates for the Play listing and the launcher: a Sherman in silhouette under searchlight beams, bold and
// readable at 48 px. Main ComfyUI (SDXL). node gen-icon.js -> ComfyUI/output/icon/*.png
const { run } = require('./gen2.js');
const NEG = 'text, letters, watermark, logo text, signature, blurry, low quality, frame, border, cartoon, cute, multiple tanks, busy background, photo, realistic photo, grass, daytime';
const PROMPTS = [
  'app icon, a WWII Sherman tank in dark silhouette on a low ridge, seen from the front three-quarter, two anti-aircraft searchlight beams crossing the deep blue night sky behind it, moon, minimal flat vector style with a few bold shapes, high contrast, orange rim light on the tank, square composition, centered, clean',
  'game icon, close-up of a WWII tank turret and gun barrel in dark green, pointing up-right, against a night sky lit by white searchlight beams, dramatic, poster style, bold simple shapes, high contrast, square, centered',
  'square emblem icon, WWII Sherman tank front view in dark olive drab under a full moon, searchlight beam behind, bold graphic illustration, thick shapes, night blue and amber palette, no text',
];
function wf(prefix, prompt, seed, batch){ return {
  '1': {class_type:'CheckpointLoaderSimple', inputs:{ckpt_name:'sd_xl_base_1.0.safetensors'}},
  '2': {class_type:'VAELoader', inputs:{vae_name:'sdxl_vae_fp16_fix.safetensors'}},
  '3': {class_type:'CLIPTextEncode', inputs:{clip:['1',1], text:prompt}},
  '4': {class_type:'CLIPTextEncode', inputs:{clip:['1',1], text:NEG}},
  '5': {class_type:'EmptyLatentImage', inputs:{width:1024, height:1024, batch_size:batch}},
  '6': {class_type:'KSampler', inputs:{model:['1',0], positive:['3',0], negative:['4',0], latent_image:['5',0], seed, steps:30, cfg:7, sampler_name:'dpmpp_2m', scheduler:'karras', denoise:1}},
  '7': {class_type:'VAEDecode', inputs:{samples:['6',0], vae:['2',0]}},
  '8': {class_type:'SaveImage', inputs:{images:['7',0], filename_prefix:prefix}},
}; }
(async () => { for (let i = 0; i < PROMPTS.length; i++) { const t0 = Date.now(); const files = await run(wf('icon/icon' + i, PROMPTS[i], 9100 + i, 2)); console.log('icon' + i, files.length, 'files', Math.round((Date.now() - t0) / 1000) + 's'); } })();
