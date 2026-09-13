# Tank art pipeline (ComfyUI + SDXL + ControlNet)

Every vehicle sprite is generated on the local GPU from a plan-view schematic drawn to the real dimensions,
so the proportions are right and only the material comes from the model.

1. `node depth.js` — draws depth maps, edge maps and silhouette masks for each vehicle into
   `D:/Tools/ComfyUI_windows_portable/ComfyUI/input/guides/` (+ `meta.json` with the turret ring position).
2. `node gen3.js sherman s=0.7 e=0.3 n=4 seed=2024` — ControlNet Union (depth + canny) text-to-image, 4 candidates;
   add `hull` for the turret-less render used under the turret. Output: `ComfyUI/output/cn/`.
3. `node sheet.js out.png 300 <files>` — contact sheet to pick the best candidate.
4. `node split.js sherman <whole.png> <hull.png> 0.25` — cuts `art/ai-tanks/sherman_hull.png` + `sherman_turret.png`
   and writes the pivots into `art/ai-tanks/meta.json`. `node split.js pak40 <whole.png> x 0.25` for the gun.
5. `node gen-ground.js meadow track` + `node tile.js <in> <out> 512` — ground textures made seamless.
6. `node tanks-build.js && node frame-tanks.js` — embeds the sprites into the HTML mockup and renders check frames.

Models: SDXL base 1.0 (CreativeML OpenRAIL++), sdxl_vae_fp16_fix, xinsir/controlnet-union-sdxl-1.0 promax (Apache-2.0).
ComfyUI runs headless: `D:/Tools/ComfyUI_windows_portable/run_nvidia_gpu.bat` (API on 127.0.0.1:8188).
Scripts need `@napi-rs/canvas` (`npm i @napi-rs/canvas`).

## 3D models (TRELLIS 2, image-to-3D)

Second ComfyUI instance with its own Python (torch 2.10.0+cu130 to match the prebuilt CUDA wheels of ComfyUI-Trellis2):
`D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe -s ComfyUI/main.py --windows-standalone-build --port 8189 --listen 127.0.0.1 --disable-auto-launch`
(run from D:/Tools/ComfyUI_windows_portable). Models: `ComfyUI/models/visualbruno/TRELLIS.2-4B-FP8` (512 pipeline only, 12 GB GPU),
`ComfyUI/models/facebook/dinov3-vitl16-pretrain-lvd1689m`, `ComfyUI/models/microsoft/TRELLIS-image-large/ckpts/ss_dec_*`.
1. Reference image: one vehicle, 3/4 view from the front-left, plain background (ChatGPT renders in `art/refs/`). Put it in `ComfyUI/input/refs/`.
2. `node trellis-run.js refs/tiger_gpt.png tiger 20000 1` -> `ComfyUI/output/3D/tiger_00001_.glb` (~3 min).
3. `python_trellis/python.exe split_turret.py tiger.glb art/models/parts tiger 6.3` -> `tiger_hull.glb` + `tiger_turret.glb`,
   pivot at the turret ring, real metres, the generated barrel removed (the game adds a straight one). `meta.json` records ring height and forward axis.
4. `serve.js` + `viewer.html` / `viewer3.html` / `scene.html` show the result in a browser (node serve.js, port 8787).
Licenses: TRELLIS 2 and ComfyUI-Trellis2 MIT; DINOv3 (Meta) commercial OK, "Built with DINOv3" in the credits; MoGe MIT.
