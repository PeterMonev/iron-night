"""A turret generated on its own (the Sherman's): scaled to a real width, the barrel cut off ahead of the mantlet (the
game adds a straight one), the ring centre at the origin: the body's centre across, the base on y = 0. The texture is
written as <name>.png so the hull keeps its own.  python turret_export.py <in.glb> <name> <width_m>"""
import sys, os
import numpy as np
import trimesh
from PIL import Image, ImageEnhance

OUT = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Models'
src, name, width = sys.argv[1], sys.argv[2], float(sys.argv[3])
s = trimesh.load(src); m = list(s.geometry.values())[0] if isinstance(s, trimesh.Scene) else s
lo, hi = m.bounds; size = hi - lo
# the barrel is what sticks out farthest from the body: its tip points the model, then it is cut off ahead of the mantlet
v = m.vertices; cen = np.median(v, axis=0); r = np.hypot(v[:, 0] - cen[0], v[:, 2] - cen[2]); tip = v[np.argmax(r)]
yaw = np.arctan2(tip[0] - cen[0], tip[2] - cen[2]); m.apply_transform(trimesh.transformations.rotation_matrix(-yaw, [0, 1, 0]))
v, f = m.vertices, m.faces; fc = v[f].mean(axis=1); lo, hi = m.bounds; size = hi - lo
print(f'barrel azimuth {np.degrees(yaw):.1f} deg')
# the width of the mesh along z, in 40 bins: the barrel is where it goes thin; the mantlet front is the last wide bin
bins = 40; edges = np.linspace(lo[2], hi[2], bins + 1); widths = np.zeros(bins)
for i in range(bins):
    sel = (v[:, 2] >= edges[i]) & (v[:, 2] < edges[i + 1]); widths[i] = (v[sel, 0].max() - v[sel, 0].min()) if sel.sum() > 3 else 0
wide = np.where(widths > 0.45 * widths.max())[0]; zfront = edges[wide.max() + 1]
keep = fc[:, 2] <= zfront + 0.01 * size[2]
m = m.submesh([np.where(keep)[0]], append=True)
# the display disc the reference stood on: the flat bottom slab goes
v, f = m.vertices, m.faces; fc = v[f].mean(axis=1); lo, hi = m.bounds
m = m.submesh([np.where(fc[:, 1] > lo[1] + 0.07 * (hi[1] - lo[1]))[0]], append=True)
lo, hi = m.bounds
# scale by the body's width, centre the body, base on the ground
scale = width / (hi[0] - lo[0]); m.apply_scale(scale); m.apply_scale([1.0, 0.82, 1.0]); lo, hi = m.bounds   # the generated turret is a touch tall
m.apply_translation([-(lo[0] + hi[0]) / 2, -lo[1], -(lo[2] + hi[2]) / 2 - 0.12 * (hi[2] - lo[2])])   # the ring sits ahead of the mesh centre: the bustle hangs out behind it
lo, hi = m.bounds
img = m.visual.material.baseColorTexture.convert('RGB'); lut = [int(255 * ((i / 255) ** 0.62)) for i in range(256)]; img = img.point(lut * 3)
img = ImageEnhance.Contrast(img).enhance(1.18); img = ImageEnhance.Color(img).enhance(1.25); img.save(os.path.join(OUT, f'{name}.png'))
vv, ff, uv = m.vertices, m.faces, m.visual.uv
with open(os.path.join(OUT, f'{name}.obj'), 'w') as o:
    o.write(f'mtllib {name}.mtl\no {name}\n')
    for p in vv: o.write(f'v {p[0]:.5f} {p[1]:.5f} {p[2]:.5f}\n')
    for t in uv: o.write(f'vt {t[0]:.5f} {t[1]:.5f}\n')
    o.write(f'usemtl {name}\ns 1\n')
    for a, b, c in ff + 1: o.write(f'f {a}/{a} {b}/{b} {c}/{c}\n')
with open(os.path.join(OUT, f'{name}.mtl'), 'w') as o: o.write(f'newmtl {name}\nKd 1 1 1\nmap_Kd {name}.png\n')
print(f'{name}: {len(ff)} faces, size {(hi - lo).round(2).tolist()}, mantlet front z {hi[2]:.2f}, roof y {hi[1]:.2f}')
