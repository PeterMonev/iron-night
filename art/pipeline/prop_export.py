"""Turns a TRELLIS prop GLB into a game asset: long axis along Z, base on the ground, centred, scaled to a real length in
metres, exported as OBJ + texture into the Unity project. Usage: python prop_export.py <in.glb> <name> <size_m> [y]
With "y" the size is the height (poles, trees, a standing figure) and the model is not turned."""
import sys, os
import numpy as np
import trimesh

src, name, length = sys.argv[1], sys.argv[2], float(sys.argv[3]); by_height = len(sys.argv) > 4 and sys.argv[4] == 'y'
out = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Props'
os.makedirs(out, exist_ok=True)
s = trimesh.load(src); m = list(s.geometry.values())[0] if isinstance(s, trimesh.Scene) else s
lo, hi = m.bounds; size = hi - lo
if not by_height and size[0] > size[2]:  # long axis to Z
    m.apply_transform(trimesh.transformations.rotation_matrix(np.pi / 2, [0, 1, 0]))
    lo, hi = m.bounds; size = hi - lo
scale = length / (size[1] if by_height else size[2])
m.apply_translation([-(lo[0] + hi[0]) / 2, -lo[1], -(lo[2] + hi[2]) / 2])
m.apply_scale(scale)
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
print(f"{name}: {len(f)} faces, size {(hi - lo).round(2).tolist()} m")
