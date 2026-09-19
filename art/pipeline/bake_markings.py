"""Paints the national markings straight into a tank's texture, on the actual surface: the white star on both hull
sides and turret sides of the Americans, the Balkenkreuz on the German turret (or hull) sides. Every face whose
normal looks sideways and whose centre lies inside the marking's disc gets the mark rasterised into its UV triangle,
so nothing floats off the lumpy generated armour. Works on the Unity OBJ/PNG pair, in place.
python bake_markings.py <name> star|cross [turret_name]"""
import sys, os
import numpy as np
import trimesh
from PIL import Image

M = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Models'
name, kind = sys.argv[1], sys.argv[2]; tur = sys.argv[3] if len(sys.argv) > 3 else None

def star_mask(u, v):
    # u, v in -1..1: a five-point star (even-odd against the pentagram outline)
    pts = [(np.cos(np.pi / 2 + i * np.pi / 5) * (0.95 if i % 2 == 0 else 0.95 * 0.382), np.sin(np.pi / 2 + i * np.pi / 5) * (0.95 if i % 2 == 0 else 0.95 * 0.382)) for i in range(10)]
    inside = False
    for i in range(10):
        x0, y0 = pts[i]; x1, y1 = pts[i - 1]
        if (y0 > v) != (y1 > v) and u < (x1 - x0) * (v - y0) / (y1 - y0 + 1e-9) + x0: inside = not inside
    return inside

def cross_mask(u, v):
    au, av = abs(u), abs(v)
    arm = (av < 0.3 and au < 0.9) or (au < 0.3 and av < 0.9)
    bar = (0.3 < av < 0.44 and 0.3 < au < 0.9) or (0.3 < au < 0.44 and 0.3 < av < 0.9)
    return 2 if arm else (1 if bar else 0)

def paint(mesh_path, tex, marks):
    m = trimesh.load(mesh_path, force='mesh', process=False); v, f, uv = m.vertices, m.faces, m.visual.uv; fn = m.face_normals; fc = v[f].mean(axis=1)
    W, H = tex.size; px = tex.load(); painted = 0
    for (side, centre, radius, colour) in marks:
        # side: +1 / -1 along x; the mark's disc lies in the (y, z) plane at that side
        sel = np.where((fn[:, 0] * side > 0.45) & (np.hypot(fc[:, 1] - centre[0], fc[:, 2] - centre[1]) < radius * 1.3))[0]
        for fi in sel:
            tri_uv = uv[f[fi]]; tri = v[f[fi]]
            # rasterise the uv triangle: every texel inside gets the 3D point by barycentrics, then the mask
            us = tri_uv[:, 0] * (W - 1); vs = (1 - tri_uv[:, 1]) * (H - 1)
            x0, x1 = int(max(0, us.min() - 1)), int(min(W - 1, us.max() + 1)); y0, y1 = int(max(0, vs.min() - 1)), int(min(H - 1, vs.max() + 1))
            if x1 - x0 > 400 or y1 - y0 > 400: continue
            (ax, ay), (bx, by), (cx, cy) = zip(us, vs); det = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
            if abs(det) < 1e-6: continue
            for y in range(y0, y1 + 1):
                for x in range(x0, x1 + 1):
                    l0 = ((by - cy) * (x - cx) + (cx - bx) * (y - cy)) / det; l1 = ((cy - ay) * (x - cx) + (ax - cx) * (y - cy)) / det; l2 = 1 - l0 - l1
                    if l0 < -0.02 or l1 < -0.02 or l2 < -0.02: continue
                    p = tri[0] * l0 + tri[1] * l1 + tri[2] * l2
                    uu = (p[2] - centre[1]) / radius * (-side); vv = (p[1] - centre[0]) / radius
                    if kind == 'star':
                        if star_mask(uu, vv): px[x, y] = colour; painted += 1
                    else:
                        k = cross_mask(uu, vv)
                        if k == 2: px[x, y] = (18, 18, 18); painted += 1
                        elif k == 1: px[x, y] = colour; painted += 1
    return painted

def side_marks(mesh_path, radius, y_frac, z_frac):
    m = trimesh.load(mesh_path, force='mesh', process=False); lo, hi = m.bounds
    cy = lo[1] + (hi[1] - lo[1]) * y_frac; cz = lo[2] + (hi[2] - lo[2]) * z_frac
    return [(1, (cy, cz), radius, (232, 228, 214)), (-1, (cy, cz), radius, (232, 228, 214))]

tex_path = os.path.join(M, name + '.png'); tex = Image.open(tex_path).convert('RGB')
hull = os.path.join(M, name + '_hull.obj')
n = 0
if kind == 'star':
    n += paint(hull, tex, side_marks(hull, 0.42, 0.72, 0.5))
else:
    if tur is None: n += paint(hull, tex, side_marks(hull, 0.3, 0.7, 0.45))
if tur:
    tpath = os.path.join(M, tur + '.obj'); ttex_path = os.path.join(M, (tur if os.path.exists(os.path.join(M, tur + '.png')) else name) + '.png')
    ttex = tex if ttex_path == tex_path else Image.open(ttex_path).convert('RGB')
    n += paint(tpath, ttex, side_marks(tpath, 0.28 if kind == 'star' else 0.3, 0.5, 0.5))
    if ttex is not tex: ttex.save(ttex_path)
tex.save(tex_path); print(f'{name}: {n} texels marked')
