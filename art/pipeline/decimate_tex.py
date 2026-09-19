"""Texture-preserving quadric decimation of a Unity prop OBJ (MeshLab); the atlas seams limit how far it goes.
python decimate_tex.py <name> <faces>   (Resources/Props/<name>.obj in place; the full mesh kept in art/models/props/<name>_full.obj)"""
import sys, os, shutil
import pymeshlab
name, faces = sys.argv[1], int(sys.argv[2])
P = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Props'; FULL = 'D:/Codes/Projects/lightswarm/art/models/props/' + name + '_full.obj'
if not os.path.exists(FULL): shutil.copy(os.path.join(P, name + '.obj'), FULL)
ms = pymeshlab.MeshSet(); ms.load_new_mesh(FULL); n0 = ms.current_mesh().face_number()
ms.meshing_decimation_quadric_edge_collapse_with_texture(targetfacenum=faces, qualitythr=0.3, extratcoordw=1.0, preserveboundary=False, optimalplacement=True, planarquadric=False)
ms.save_current_mesh(os.path.join(P, name + '.obj'), save_textures=False)
with open(os.path.join(P, name + '.mtl'), 'w') as o: o.write(f'newmtl {name}\nKd 1 1 1\nmap_Kd {name}_tex.png\n')
print(f'{name}: {n0} -> {ms.current_mesh().face_number()} faces')
