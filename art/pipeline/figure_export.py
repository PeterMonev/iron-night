"""A TRELLIS figure GLB into the game: standing on the ground, centred on its legs, facing +Z, scaled to a real height,
written as Props/<name>.obj + <name>_tex.png.
Usage: python figure_export.py <in.glb> <name> <height_m> [stand|run] [raw] [turn=<deg>] [cut=<m>]

The facing is read from the body. Standing, the feet reach ahead of the shins (the toes forward, the calves behind);
running, the head is ahead of the hips (the body leans into the run). turn=<deg> turns it further when the guess is
wrong. cut=<m> keeps only what is above that height of the full figure and sets it on the ground there: the commander
in his hatch is the man from the waist up.
"""
import sys, os, math
import numpy as np
import trimesh

src, name, height = sys.argv[1], sys.argv[2], float(sys.argv[3])
mode = sys.argv[4] if len(sys.argv) > 4 and sys.argv[4] in ('stand', 'run') else 'stand'
opts = dict(a.split('=') for a in sys.argv[4:] if '=' in a)
turn, cut = float(opts.get('turn', 0)), float(opts.get('cut', 0))
out = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Props'

s = trimesh.load(src); m = list(s.geometry.values())[0] if isinstance(s, trimesh.Scene) else s
lo, hi = m.bounds; H = hi[1] - lo[1]; v = m.vertices; y = (v[:, 1] - lo[1]) / H


def band(a, b):
    sel = (y >= a) & (y < b); return v[sel][:, [0, 2]].mean(axis=0)


d = band(0.0, 0.06) - band(0.12, 0.30) if mode == 'stand' else band(0.88, 1.0) - band(0.45, 0.56)
face = 0.0 if 'raw' in sys.argv[4:] else math.degrees(math.atan2(d[0], d[1]))   # the way the figure looks, from +Z towards +X; raw: as TRELLIS left it
rot = math.radians(-face + turn)
m.apply_transform(trimesh.transformations.rotation_matrix(rot, [0, 1, 0]))

# on the ground, centred on the legs, at its real height
lo, hi = m.bounds; H = hi[1] - lo[1]; v = m.vertices; y = (v[:, 1] - lo[1]) / H
legs = v[(y < 0.3)][:, [0, 2]].mean(axis=0)
m.apply_translation([-legs[0], -lo[1], -legs[1]]); m.apply_scale(height / H)
if cut > 0:
    keep = m.triangles_center[:, 1] > cut; m = m.submesh([np.where(keep)[0]], append=True); m.apply_translation([0, -cut, 0])
lo, hi = m.bounds

v, f, uv = m.vertices, m.faces, m.visual.uv
with open(os.path.join(out, f'{name}.obj'), 'w') as o:
    o.write(f'mtllib {name}.mtl\no {name}\n')
    for p in v: o.write(f'v {p[0]:.4f} {p[1]:.4f} {p[2]:.4f}\n')
    for t in uv: o.write(f'vt {t[0]:.5f} {t[1]:.5f}\n')
    o.write(f'usemtl {name}\ns 1\n')
    for a, b, c in f + 1: o.write(f'f {a}/{a} {b}/{b} {c}/{c}\n')
with open(os.path.join(out, f'{name}.mtl'), 'w') as o: o.write(f'newmtl {name}\nKd 1 1 1\nmap_Kd {name}_tex.png\n')
img = m.visual.material.baseColorTexture.convert('RGB')
lut = [int(255 * ((i / 255) ** 0.72)) for i in range(256)]; img = img.point(lut * 3)
img.save(os.path.join(out, f'{name}_tex.png'))
print(f"{name}: {len(f)} faces, faced {face:.0f} deg, size {(hi - lo).round(2).tolist()} m")
