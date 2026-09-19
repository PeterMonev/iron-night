"""Writes a vehicle into the Unity project: hull (+ turret) OBJs from art/models/parts and one brightened texture.
  python vehicle_export.py parts <name> [xscale]        -> <name>_hull.obj, <name>_turret.obj, <name>.png (split_turret output)
  python vehicle_export.py whole <in.glb> <name> <len_m> -> <name>_hull.obj, <name>.png: a turretless vehicle or a gun, long
                                                            axis on Z, base on the ground, centred, scaled to the length
The texture gets the same lift as the first tanks (gamma 0.62, contrast 1.18, colour 1.25) so it reads under moonlight."""
import sys, os
import numpy as np
import trimesh
from PIL import Image, ImageEnhance

OUT = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Models'
PARTS = 'D:/Codes/Projects/lightswarm/art/models/parts'

def lift(img):
    img = img.convert('RGB'); lut = [int(255 * ((i / 255) ** 0.62)) for i in range(256)]; img = img.point(lut * 3)
    img = ImageEnhance.Contrast(img).enhance(1.18); return ImageEnhance.Color(img).enhance(1.25)

def outward(m):
    # the generator leaves patches wound inside out (lit as their own underside: flat dark blue under the moon); a hull is
    # near enough convex that a face's normal should point away from the centroid - flip the ones that do not
    v, f = m.vertices, m.faces; fn = m.face_normals; fc = v[f].mean(axis=1); lo, hi = m.bounds
    flip = (fc[:, 1] > lo[1] + 0.55 * (hi[1] - lo[1])) & (fn[:, 1] < -0.3)   # the upper half never faces down: a deck wound inside out
    f = f.copy(); f[flip] = f[flip][:, ::-1]; m.faces = f; print(f'  {int(flip.sum())} faces turned outward')

def write(m, name, tex):
    outward(m); v, f, uv = m.vertices, m.faces, m.visual.uv
    with open(os.path.join(OUT, f'{name}.obj'), 'w') as o:
        o.write(f'mtllib {name}.mtl\no {name}\n')
        for p in v: o.write(f'v {p[0]:.5f} {p[1]:.5f} {p[2]:.5f}\n')
        for t in uv: o.write(f'vt {t[0]:.5f} {t[1]:.5f}\n')
        o.write(f'usemtl {name}\ns 1\n')
        for a, b, c in f + 1: o.write(f'f {a}/{a} {b}/{b} {c}/{c}\n')
    with open(os.path.join(OUT, f'{name}.mtl'), 'w') as o: o.write(f'newmtl {name}\nKd 1 1 1\nmap_Kd {tex}\n')
    print(f'{name}: {len(v)} verts, {len(f)} faces')

def load(path):
    s = trimesh.load(path); return list(s.geometry.values())[0] if isinstance(s, trimesh.Scene) else s

mode = sys.argv[1]
if mode == 'parts':
    name = sys.argv[2]; xscale = float(sys.argv[3]) if len(sys.argv) > 3 else 1.0   # the generated Soviet tanks come out too wide: squeeze across
    zwarp = [float(x) for x in sys.argv[4].split(':')] if len(sys.argv) > 4 else None   # rear:front stretch about the ring - the generator hides the deck behind the turret
    hull = load(os.path.join(PARTS, f'{name}_hull.glb')); tur = load(os.path.join(PARTS, f'{name}_turret.glb'))
    if xscale != 1.0: hull.apply_scale([xscale, 1, 1]); tur.apply_scale([xscale, 1, 1])
    if zwarp:
        z = hull.vertices[:, 2]; hull.vertices[:, 2] = np.where(z < 0, z * zwarp[0], z * zwarp[1])
    if not os.path.exists(os.path.join(OUT, f'{name}.png')): lift(hull.visual.material.baseColorTexture).save(os.path.join(OUT, f'{name}.png'))   # a repainted texture stays
    write(hull, f'{name}_hull', f'{name}.png'); write(tur, f'{name}_turret', f'{name}.png')
else:
    src, name, length = sys.argv[2], sys.argv[3], float(sys.argv[4]); xscale = float(sys.argv[5]) if len(sys.argv) > 5 else 1.0
    m = load(src); lo, hi = m.bounds; size = hi - lo
    if size[0] > size[2]: m.apply_transform(trimesh.transformations.rotation_matrix(np.pi / 2, [0, 1, 0])); lo, hi = m.bounds; size = hi - lo
    m.apply_translation([-(lo[0] + hi[0]) / 2, -lo[1], -(lo[2] + hi[2]) / 2]); m.apply_scale(length / size[2]); m.apply_scale([xscale, 1, 1])
    if not os.path.exists(os.path.join(OUT, f'{name}.png')): lift(m.visual.material.baseColorTexture).save(os.path.join(OUT, f'{name}.png'))   # a repainted texture stays
    write(m, f'{name}_hull', f'{name}.png'); lo, hi = m.bounds; print(f'{name}: size {(hi - lo).round(2).tolist()} m')
