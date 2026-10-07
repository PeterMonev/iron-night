"""A TRELLIS figure in a T-pose made ready for Mixamo's auto-rigger: the GLB turned to stand on its feet at the origin,
facing +Z, scaled to its real height in metres (Mixamo reads OBJ in centimetres: written x100), its texture beside it,
and the OBJ, MTL and PNG zipped together as Mixamo takes them. Writes art/rig/<name>.zip.
Usage: python rig_export.py <in.glb> <name> <height_m>"""
import sys, os, zipfile, io
import numpy as np
import trimesh

src, name, height = sys.argv[1], sys.argv[2], float(sys.argv[3])
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'rig'); os.makedirs(out, exist_ok=True)
s = trimesh.load(src); m = list(s.geometry.values())[0] if isinstance(s, trimesh.Scene) else s
lo, hi = m.bounds; size = hi - lo
k = height / size[1]
m.apply_translation([-(lo[0] + hi[0]) / 2, -lo[1], -(lo[2] + hi[2]) / 2]); m.apply_scale(k * 100.0)   # centimetres for Mixamo
tex = m.visual.material.baseColorTexture if hasattr(m.visual.material, 'baseColorTexture') else m.visual.material.image
buf = io.BytesIO(); tex.convert('RGB').save(buf, 'PNG')
obj = trimesh.exchange.obj.export_obj(m, include_texture=False, mtl_name=name + '.mtl')
mtl = 'newmtl %s\nKa 1 1 1\nKd 1 1 1\nKs 0 0 0\nmap_Kd %s_tex.png\n' % (name, name)
obj = obj.replace('usemtl', 'usemtl') if 'usemtl' in obj else obj.replace('\nv ', '\nusemtl %s\nv ' % name, 1)
with zipfile.ZipFile(os.path.join(out, name + '.zip'), 'w', zipfile.ZIP_DEFLATED) as z:
    z.writestr(name + '.obj', 'mtllib %s.mtl\n' % name + obj if 'mtllib' not in obj else obj)
    z.writestr(name + '.mtl', mtl); z.writestr(name + '_tex.png', buf.getvalue())
print(name, 'faces', len(m.faces), 'height', round(height, 2), 'm')
