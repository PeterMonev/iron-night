"""Cuts a Unity prop OBJ down to a face budget with MeshLab's texture-preserving quadric decimation, in place.
python decimate.py <name> <faces>   (Resources/Props/<name>.obj)"""
import sys, os
import pymeshlab
name, faces = sys.argv[1], int(sys.argv[2])
P = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Props'
ms = pymeshlab.MeshSet(); ms.load_new_mesh(os.path.join(P, name + '.obj'))
before = ms.current_mesh().face_number()
ms.meshing_decimation_quadric_edge_collapse_with_texture(targetfacenum=faces, qualitythr=0.5, preserveboundary=True, planarquadric=True)
ms.save_current_mesh(os.path.join(P, name + '.obj'), save_textures=False)
# MeshLab writes its own mtl; put ours back
with open(os.path.join(P, name + '.mtl'), 'w') as o: o.write(f'newmtl {name}\nKd 1 1 1\nmap_Kd {name}_tex.png\n')
print(f'{name}: {before} -> {ms.current_mesh().face_number()} faces')
