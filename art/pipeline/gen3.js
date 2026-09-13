// ControlNet (union SDXL) text-to-image: the depth map from depth.js pins the shape and proportions, optionally the edge
// map pins the panel lines too; SDXL is free to paint the material. node gen3.js sherman tiger s=0.7 e=0.3 n=4
const path = require('path');
const { run, SUBJECTS, STYLE, NEG } = require('./gen2.js');
function workflow(prefix, guide, prompt, seed, batch, depthStrength, edgeStrength, suffix = ''){
  const wf = {
    '1': {class_type:'CheckpointLoaderSimple', inputs:{ckpt_name:'sd_xl_base_1.0.safetensors'}},
    '2': {class_type:'VAELoader', inputs:{vae_name:'sdxl_vae_fp16_fix.safetensors'}},
    '3': {class_type:'CLIPTextEncode', inputs:{clip:['1',1], text:prompt}},
    '4': {class_type:'CLIPTextEncode', inputs:{clip:['1',1], text:NEG}},
    '5': {class_type:'EmptyLatentImage', inputs:{width:1024, height:1024, batch_size:batch}},
    '12': {class_type:'ControlNetLoader', inputs:{control_net_name:'controlnet-union-sdxl-promax.safetensors'}},
    '13': {class_type:'SetUnionControlNetType', inputs:{control_net:['12',0], type:'depth'}},
    '14': {class_type:'LoadImage', inputs:{image:'guides/depth_' + guide + suffix + '.png'}},
    '15': {class_type:'ControlNetApplyAdvanced', inputs:{positive:['3',0], negative:['4',0], control_net:['13',0], image:['14',0], strength:depthStrength, start_percent:0, end_percent:0.85, vae:['2',0]}},
    '7': {class_type:'VAEDecode', inputs:{samples:['6',0], vae:['2',0]}},
    '8': {class_type:'SaveImage', inputs:{images:['7',0], filename_prefix:prefix}},
  };
  let cond = '15';
  if (edgeStrength > 0){
    wf['16'] = {class_type:'SetUnionControlNetType', inputs:{control_net:['12',0], type:'canny/lineart/anime_lineart/mlsd'}};
    wf['17'] = {class_type:'LoadImage', inputs:{image:'guides/edge_' + guide + suffix + '.png'}};
    wf['18'] = {class_type:'ControlNetApplyAdvanced', inputs:{positive:['15',0], negative:['15',1], control_net:['16',0], image:['17',0], strength:edgeStrength, start_percent:0, end_percent:0.6, vae:['2',0]}};
    cond = '18';
  }
  wf['6'] = {class_type:'KSampler', inputs:{model:['1',0], positive:[cond,0], negative:[cond,1], latent_image:['5',0], seed, steps:35, cfg:6.5, sampler_name:'dpmpp_2m', scheduler:'karras', denoise:1}};
  return wf;
}
module.exports = { workflow };
if (require.main === module) (async () => {
  const args = process.argv.slice(2); const which = args.filter(a => SUBJECTS[a]);
  const num = (k, d) => Number((args.find(a => a.startsWith(k + '=')) || k + '=' + d).slice(k.length + 1));
  const s = num('s', 0.7), e = num('e', 0), n = num('n', 4), seed = num('seed', 777), suffix = args.includes('hull') ? '_hull' : '';
  for (const k of (which.length ? which : Object.keys(SUBJECTS))){
    const t0 = Date.now(); const tag = 'cn/' + k + suffix + '_s' + Math.round(s * 100) + '_e' + Math.round(e * 100);
    const files = await run(workflow(tag, k, (suffix ? SUBJECTS[k].replace(/^/, 'the hull of a ') + 'turret removed, open turret ring, ' : SUBJECTS[k]) + STYLE, seed + k.length, n, s, e, suffix));
    console.log(k, 'depth', s, 'edge', e, files.length, 'files', Math.round((Date.now() - t0) / 1000) + 's'); }
})();
