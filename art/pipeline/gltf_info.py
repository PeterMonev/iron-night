"""What is in a Sketchfab glTF: node tree with mesh face counts and bounds, materials and texture sizes.
python gltf_info.py <folder> [...]"""
import sys, os, json, numpy as np, trimesh
for folder in sys.argv[1:]:
    p = os.path.join(folder, 'scene.gltf'); s = trimesh.load(p); print(f'== {folder}: {open(os.path.join(folder, "license.txt"), encoding="utf-8", errors="replace").read().splitlines()[0][:100]}')
    lo, hi = s.bounds; print(f'  bounds {(hi - lo).round(2).tolist()} (units as in file), {len(s.geometry)} meshes, faces total {sum(len(g.faces) for g in s.geometry.values() if hasattr(g, 'faces'))}')
    j = json.load(open(p, encoding='utf-8'))
    names = [n.get('name', '?') for n in j.get('nodes', [])]; print('  nodes:', ', '.join(names[:40])[:600])
    for name, g in [(k, v) for k, v in s.geometry.items() if hasattr(v, 'faces')][:25]:
        b = g.bounds; print(f'   mesh {name[:40]:40s} {len(g.faces):7d} f  size {(b[1] - b[0]).round(2).tolist()}  centre {((b[0] + b[1]) / 2).round(2).tolist()}')
    for im in j.get('images', []): print('  image', im.get('uri'))
