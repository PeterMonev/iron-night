# blender -b --factory-startup -P decimate_obj.py -- <in.obj> <out.obj> <faces>
# A model cut down to so many faces with its UVs kept (Blender's collapse decimation), written back as OBJ with the
# same material file: for a prop that has to stand by the dozen (the olive trees of an Italian grove).
import bpy, sys
argv = sys.argv[sys.argv.index('--') + 1:]; src, dst, faces = argv[0], argv[1], int(argv[2])
for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)   # the factory cube and lamp out first
bpy.ops.wm.obj_import(filepath=src)
ob = [o for o in bpy.context.scene.objects if o.type == 'MESH'][0]; bpy.context.view_layer.objects.active = ob
n = len(ob.data.polygons); mod = ob.modifiers.new('cut', 'DECIMATE'); mod.ratio = min(1.0, faces / max(1, n)); mod.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier='cut')
print('faces', n, '->', len(ob.data.polygons))
bpy.ops.wm.obj_export(filepath=dst, export_selected_objects=False, export_materials=True, export_uv=True, export_normals=True, path_mode='STRIP')
