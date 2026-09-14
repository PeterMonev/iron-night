"""Splits the TRELLIS searchlight into the trailer and the drum so the game can turn the drum on its yoke.
The model is scaled to a real height, turned so the tow bar (and the lens with it) points +Z, cut at the yoke height;
the drum is exported around its own centre with its elevation removed, so the game's pitch starts level.
Usage: python split_lamp.py <in.glb> <height_m> [cut_m]  -> Resources/Props/searchlight_base.obj, searchlight_drum.obj, searchlight_tex.png"""
import sys, os
import numpy as np
import trimesh

src, height = sys.argv[1], float(sys.argv[2]); cut = float(sys.argv[3]) if len(sys.argv) > 3 else 1.6
OUT = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Props'
s = trimesh.load(src); m = list(s.geometry.values())[0] if isinstance(s, trimesh.Scene) else s
lo, hi = m.bounds; m.apply_translation([-(lo[0] + hi[0]) / 2, -lo[1], -(lo[2] + hi[2]) / 2]); m.apply_scale(height / (hi[1] - lo[1]))
v, f = m.vertices, m.faces; fc = v[f].mean(axis=1)
# the drum is everything above the cut; its lens is the biggest flat patch that looks upward (the back plate looks down)
drum_faces = np.where(fc[:, 1] > cut)[0]; base_faces = np.where(fc[:, 1] <= cut)[0]
dn = m.face_normals[drum_faces]; da = m.area_faces[drum_faces]
keys = {}
for n, a in zip(dn, da):
    if n[1] < 0.15: continue
    k = tuple(np.round(n * 5).astype(int)); keys[k] = keys.get(k, 0) + a
best = max(keys.items(), key=lambda kv: kv[1])[0]; lens = np.array(best, dtype=float) / 5; lens /= np.linalg.norm(lens)
yaw = np.arctan2(lens[0], lens[2]); m.apply_transform(trimesh.transformations.rotation_matrix(-yaw, [0, 1, 0]))
lens = trimesh.transformations.rotation_matrix(-yaw, [0, 1, 0])[:3, :3] @ lens
print(f'lens normal {lens.round(2).tolist()}, model turned by {np.degrees(-yaw):.0f} deg')
v, f = m.vertices, m.faces
drum = m.submesh([drum_faces], append=True); base = m.submesh([base_faces], append=True)
dl, dh = drum.bounds; pivot = (dl + dh) / 2
tilt = np.arctan2(lens[1], lens[2]); print(f'drum pivot {pivot.round(2).tolist()}, elevation {np.degrees(tilt):.0f} deg')
drum.apply_translation(-pivot); drum.apply_transform(trimesh.transformations.rotation_matrix(tilt, [1, 0, 0]))   # positive about X pitches +Z down
img = m.visual.material.baseColorTexture.convert('RGB'); lut = [int(255 * ((i / 255) ** 0.72)) for i in range(256)]; img = img.point(lut * 3); img.save(os.path.join(OUT, 'searchlight_tex.png'))
for name, part in (('searchlight_base', base), ('searchlight_drum', drum)):
    pv, pf, uv = part.vertices, part.faces, part.visual.uv
    with open(os.path.join(OUT, name + '.obj'), 'w') as o:
        o.write(f'mtllib {name}.mtl\no {name}\n')
        for p in pv: o.write(f'v {p[0]:.4f} {p[1]:.4f} {p[2]:.4f}\n')
        for t in uv: o.write(f'vt {t[0]:.5f} {t[1]:.5f}\n')
        o.write(f'usemtl {name}\ns 1\n')
        for a, b, c in pf + 1: o.write(f'f {a}/{a} {b}/{b} {c}/{c}\n')
    with open(os.path.join(OUT, name + '.mtl'), 'w') as o: o.write(f'newmtl {name}\nKd 1 1 1\nmap_Kd searchlight_tex.png\n')
    print(f'{name}: {len(pf)} faces, bounds {part.bounds.round(2).tolist()}')
print(f'LampPivotY = {pivot[1]:.2f}, drum radius ~ {(dh[0] - dl[0]) / 2:.2f}')
