"""Cuts a Unity prop OBJ down to a face budget (quadric decimation, no UV constraints), then gives every new vertex the
texture coordinate of the nearest original vertex - fine for foliage, where the atlas is noise anyway.
python decimate_uv.py <name> <faces>   (Resources/Props/<name>.obj, in place)"""
import sys, os
import numpy as np
import trimesh
from scipy.spatial import cKDTree
name, faces = sys.argv[1], int(sys.argv[2])
P = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Props'
m = trimesh.load(os.path.join(P, name + '.obj'), force='mesh', process=False)
v0, uv0 = m.vertices.copy(), m.visual.uv.copy()
s = m.simplify_quadric_decimation(face_count=faces)
tree = cKDTree(v0); _, idx = tree.query(s.vertices); uv = uv0[idx]
with open(os.path.join(P, name + '.obj'), 'w') as o:
    o.write(f'mtllib {name}.mtl\no {name}\n')
    for p in s.vertices: o.write(f'v {p[0]:.4f} {p[1]:.4f} {p[2]:.4f}\n')
    for t in uv: o.write(f'vt {t[0]:.5f} {t[1]:.5f}\n')
    o.write(f'usemtl {name}\ns 1\n')
    for a, b, c in s.faces + 1: o.write(f'f {a}/{a} {b}/{b} {c}/{c}\n')
print(f'{name}: {len(m.faces)} -> {len(s.faces)} faces')
