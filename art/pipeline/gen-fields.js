// Photographic ground textures for the bocage field: SDXL text-to-image straight down, later made seamless by
// tile.js and turned into night albedo + normal maps by bake-photo-fields.js. Runs on whichever ComfyUI is up.
// node gen-fields.js [port] [name ...]
const path = require('path'); const OUT = 'D:/Tools/ComfyUI_windows_portable/ComfyUI/output';
const PORT = Number(process.argv[2]) || 8188;
const NEG = 'berries, flowers, fruit, paving, cobblestones, flagstones, bricks, wall, lawn, turf, aerial photograph, satellite image, map, landscape, fields pattern, hedgerows pattern, horizon, sky, perspective, angled view, buildings, vehicles, people, animals, text, watermark, logo, blurry, low quality, frame, border, cartoon, drawing, painting, tilt-shift, shadows of trees';
const TAIL = ', photographed looking straight down from about eight metres above the ground so that single tufts of grass, stones and footprints are visible, orthographic top view, evenly lit under an overcast sky, no shadows, seamless tileable ground texture, photorealistic, sharp, high detail';
const P = {
  pasture: 'a rough green pasture meadow in Normandy in summer, uneven tufted grass with clumps, a few bare trodden mud patches and cow paths' + TAIL,
  mown:    'a freshly mown hay meadow, short pale green grass with parallel mowing lines and thin windrows of cut dry grass' + TAIL,
  stubble: 'a harvested wheat field in August, golden straw stubble in straight parallel rows, loose straw and chaff between the rows, some dry earth showing' + TAIL,
  plough:  'a freshly ploughed farm field, dark brown moist soil in straight parallel furrows with clods and a few stones' + TAIL,
  lane:    'a narrow unpaved farm track running exactly vertically through the middle of the picture, two pale worn wheel ruts with a strip of grass between them, muddy puddles in the ruts, grassy verges with weeds on both sides' + TAIL,
  yard:    'a trodden farmyard, wet brown mud mixed with straw, cart wheel tracks, small puddles, chicken-scratched dirt' + TAIL,
  hedge:   'the top of a dense hawthorn hedge, thick small dark green leaves and twigs, bramble, a few red berries, no gaps' + TAIL,
  crater:  'a single fresh artillery shell crater in the centre of a green grass field, a round hole with dark upturned earth and scorched soil thrown around it in a ring, grass beyond' + TAIL.replace(', seamless tileable texture', ''),
  snow_pasture: 'a snow-covered rough pasture in winter, dry grass tufts and weeds poking through thin snow, muddy patches where the snow has melted' + TAIL,
  snow_mown:    'a snow-covered mown meadow in winter, an even blanket of snow with faint parallel lines of grass showing through' + TAIL,
  snow_stubble: 'a snow-covered harvested wheat field in winter, rows of straw stubble poking through the snow in straight lines' + TAIL,
  snow_plough:  'a ploughed field in winter, dark furrows with snow lying in the troughs in straight parallel lines' + TAIL,
  snow_lane:    'a narrow unpaved farm track in winter running exactly vertically through the middle of the picture, two dark muddy wheel ruts through the snow, snowy verges on both sides' + TAIL,
  pasture2: 'rough uneven pasture ground, tufts of grass of different heights, patches of yellowed dry grass and clover, a few small bare earth patches and hoof prints, wild and untidy' + TAIL,
  yard2:    'a muddy trodden farmyard ground, wet brown earth churned by cart wheels and hooves, loose straw scattered in the mud, small puddles, no stones' + TAIL,
  hedge2:   'the flat top of a dense clipped hawthorn hedge, thousands of small dark green leaves and thin twigs, a few dry leaves, no gaps' + TAIL,
  lane2:    'flat lay of a dirt farm track photographed exactly from above with no perspective, two straight parallel muddy wheel ruts running from the top edge to the bottom edge, a strip of grass between the ruts and grass verges at both sides, puddles in the ruts' + TAIL,
  snow_yard:    'a farmyard in winter trodden to brown slush, footprints and cart tracks in dirty snow, straw and mud' + TAIL,
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
(async () => { const names = process.argv.slice(3); for (const k of (names.length ? names : Object.keys(P))){ const t0 = Date.now(); const files = await run(wf('fields/' + k, P[k], 7 + k.length, 3)); console.log(k, files.length, 'files', Math.round((Date.now() - t0) / 1000) + 's'); } })();
