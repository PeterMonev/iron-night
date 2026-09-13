// Image-to-3D through the ComfyUI-Trellis2 nodes on the TRELLIS instance (port 8189):
// reference image -> background removal -> sparse structure -> shape (512) -> mesh -> simplify -> texture -> GLB.
// node trellis-run.js <image in ComfyUI/input, e.g. refs/sherman34.png> <name> [faces=20000] [seed=1]
const path = require('path');
const HOST = 'http://127.0.0.1:8189', OUT = 'D:/Tools/ComfyUI_windows_portable/ComfyUI/output';
function workflow(image, name, faces, seed){ return {
  '1': {class_type:'Trellis2LoadImageWithTransparency', inputs:{image}},
  '2': {class_type:'Trellis2PreProcessImage', inputs:{image:['1',0], padding:10, remove_background:true, max_size:1024}},
  '3': {class_type:'Trellis2LoadModel', inputs:{modelname:'visualbruno/TRELLIS.2-4B-FP8', backend:'sdpa', device:'cuda', low_vram:true, keep_models_loaded:false, conv_backend:'flex_gemm', sparse_backend:'flash_attn', use_reconviagen:false, pixal3d_multiview:false}},
  '4': {class_type:'Trellis2ImageCondGenerator', inputs:{pipeline:['3',0], image:['2',0], max_views:1}},
  '5': {class_type:'Trellis2SparseGenerator', inputs:{pipeline:['4',2], image_cond:['4',0], seed, sparse_structure_steps:12, sparse_structure_guidance_strength:7.5, sparse_structure_guidance_rescale:0.01, sparse_structure_rescale_t:5, sparse_structure_sampler:'heun', sparse_structure_resolution:32, sparse_structure_guidance_interval_start:0.1, sparse_structure_guidance_interval_end:1, fill_holes:true, hole_iterations:1, verbose:false, dino_lock:0, dino_substeps:4, hole_fill_algorithm:'flood_fill', dino_foundation_cap:1, keep_only_shell:true}},
  '6': {class_type:'Trellis2ShapeGenerator', inputs:{pipeline:['5',2], image_cond:['4',0], coords:['5',0], resolution:512, shape_steps:12, shape_guidance_strength:7.5, shape_guidance_rescale:0.01, shape_rescale_t:3, shape_sampler:'heun', shape_guidance_interval_start:0.1, shape_guidance_interval_end:1, verbose:false, dino_lock:0, dino_substeps:4, dino_foundation_cap:1}},
  '7': {class_type:'Trellis2DecodeLatents', inputs:{pipeline:['6',2], shape_slat:['6',0], resolution:['6',1], use_tiled_decoder:true}},
  '8': {class_type:'Trellis2FillHolesWithCuMesh', inputs:{mesh:['7',0], max_permieters:0.03}},
  '9': {class_type:'Trellis2ReconstructMeshWithQuad', inputs:{mesh:['8',0], remesh_band:1, resolution:512, remove_floaters:true, remove_inner_faces:true}},
  '10': {class_type:'Trellis2SimplifyMesh', inputs:{mesh:['9',0], target_face_num:300000, method:'Cumesh'}},
  '11': {class_type:'Trellis2FillHolesNicelyWithMeshlib', inputs:{mesh:['10',0]}},
  '15': {class_type:'Trellis2SimplifyMesh', inputs:{mesh:['11',0], target_face_num:faces, method:'Meshlib'}}, // the low-poly pass must be Meshlib, as in the node's own low-poly example
  '12': {class_type:'Trellis2MeshWithVoxelToTrimesh', inputs:{mesh:['15',0], reorient_vertices:'90 degrees'}},
  '13': {class_type:'Trellis2MeshTexturing', inputs:{pipeline:['7',2], image:['2',0], trimesh:['12',0], seed, texture_steps:12, texture_guidance_strength:3, texture_guidance_rescale:0.2, texture_rescale_t:3, resolution:512, texture_size:2048, texture_alpha_mode:'OPAQUE', double_side_material:false, texture_guidance_interval_start:0, texture_guidance_interval_end:0.9, max_views:4, bake_on_vertices:false, use_custom_normals:false, mesh_cluster_threshold_cone_half_angle_rad:60, sampler:'euler', inpainting:'telea', verbose:false, dino_lock:0, dino_substeps:4, dino_foundation_cap:1}},
  '14': {class_type:'Trellis2ExportMesh', inputs:{trimesh:['13',0], filename_prefix:'3D/' + name, file_format:'glb'}},
}; }
async function run(wf){
  const r = await fetch(HOST + '/prompt', {method:'POST', headers:{'content-type':'application/json'}, body:JSON.stringify({prompt:wf})});
  const j = await r.json(); if (!j.prompt_id) throw new Error('queue failed: ' + JSON.stringify(j).slice(0, 1200));
  for (;;){ await new Promise(res => setTimeout(res, 3000)); const h = await (await fetch(HOST + '/history/' + j.prompt_id)).json(); const e = h[j.prompt_id];
    if (e && e.status && e.status.completed) return e.outputs;
    if (e && e.status && e.status.status_str === 'error') throw new Error('job error: ' + JSON.stringify(e.status.messages).slice(0, 1500)); }
}
module.exports = { run, workflow };
if (require.main === module) (async () => {
  const [image, name, facesS, seedS] = process.argv.slice(2); const t0 = Date.now();
  const outputs = await run(workflow(image, name || 'model', Number(facesS || 20000), Number(seedS || 1)));
  console.log(name, 'done in', Math.round((Date.now() - t0) / 1000) + 's', JSON.stringify(outputs).slice(0, 600));
})();
