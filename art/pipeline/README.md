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

## Props and the field

- Props (farmhouse, barn, truck, haystack, dead tree, sandbags): same TRELLIS run, then `python_trellis/python.exe prop_export.py <glb> <name> <length_m>`
  -> `Assets/_Game/Resources/Props/<name>.obj` + texture, long axis on Z, base on the ground.
- Ground: `node bake-fields.js` bakes the four seamless 40 m field tiles (plough, pasture, mown, stubble), the lane strip, the yard and crater
  decals and the hedge foliage from the SDXL tiles in `art/ai-ground` into `Assets/_Game/Resources/Textures`. Hedges, trees and the searchlight
  are built in code (`Props.cs`), the layout is a hash of the 40 m cell coordinates.

## Up close (the 3D garage), 2026-09-19
The raw TRELLIS model with its own texture holds up at close range; what broke the tanks was our processing:
brightness-baked normal maps (every atlas island edge became a crack - `bake-vehicle-normals.js` is gone, no `_n.png` for
vehicles), the flat repaint (`tankpaint.js`/`unstar.js`, no longer used), and a split that counted connectivity by vertex
index while the atlas duplicates vertices at seams. Now: `split_turret.py` (position-merged adjacency, loose pieces < 4% of
the turret dropped) -> `vehicle_export.py parts <name> <xscale> --raw` (raw-tex with gamma 0.8) -> `tone.py <name> r g b [k]`
(US olive drab 84 88 64 k 0.9, Soviet 4BO 66 75 46 k 0.9, sherman_turret2 like the hull). The painted stars stay
(`VehicleSpec.painted`). `bake_markings.py` paints a star/cross into the texture on the surface, for a model whose
reference had none.

## Artist models from Sketchfab (2026-09-19, evening)
Generated tanks never look like the real thing up close; artist models do. Free ones with CC BY 4.0 (credit in
THIRD_PARTY.md and on the title screen; never a model ripped from World of Tanks / War Thunder / Call of Duty, whatever
licence the uploader picked). Download the glTF into `art/models/sketchfab/<name>/`, then
`sketchfab_export.py <folder> <name> <hull_length_m> [ring=<frac>] [reach=0.25] [forward=+|-] [casemate]`: one OBJ per
material (`<name>_hull_m<i>`, `<name>_turret_m<i>`, textures `<name>_m<i>.png` + `_n`), the ring at the origin, X
mirrored for Unity, the model's own gun kept (spec gunLength 0, muzzle from the printout). `Vehicle.Assemble` builds
the parts; `VehicleSpec.painted`. Ring fractions used: KV-1 0.68, Panzer IV 0.70, Chaffee 0.47, the rest auto.
Check a split with `viewer10.html?n=<name>` after `obj2view_sf.py <name>` (scratchpad).

## The hangar (2026-09-22)
`gen-hangar.js 8189 concrete brick metal` (SDXL on the TRELLIS ComfyUI) -> `bake-hangar.js` (seamless, toned, normal
maps) -> `Resources/Textures/hangar_*.png`. Run IronNightSetup.Setup after adding textures: `*_n.png` must import as
normal maps or the surface goes black. Unity's Quad faces -Z: a wall at +z keeps identity, the left wall (x<0, facing +x)
turns -90 about Y, the right +90. Lights fall off with the square of the distance: walls need washers of their own within
a few metres; the URP asset's per-object light limit is 8 now.
