# blender -b --factory-startup -P view_figure.py -- <out_prefix> <model.obj> [<model.obj> ...]
# Renders exported figures side by side from the game's front (OBJ +Z) and from their right: a figure that looks at
# the camera in the front row faces +Z as the game wants.
import sys, math, bpy
from mathutils import Vector
argv = sys.argv[sys.argv.index('--') + 1:]
out, srcs = argv[0], argv[1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
for i, src in enumerate(srcs):
    before = set(bpy.data.objects); bpy.ops.wm.obj_import(filepath=src)
    for o in set(bpy.data.objects) - before: o.location.x += i * 1.4           # OBJ +Z comes in as Blender -Y
scn = bpy.context.scene
w = bpy.data.worlds.new('W'); scn.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (0.75, 0.77, 0.8, 1)
sun = bpy.data.objects.new('Sun', bpy.data.lights.new('Sun', 'SUN')); sun.data.energy = 3.5; sun.rotation_euler = (math.radians(50), 0, math.radians(-20)); scn.collection.objects.link(sun)
cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')); scn.collection.objects.link(cam); scn.camera = cam
cam.data.type = 'ORTHO'; n = len(srcs); cam.data.ortho_scale = max(2.4, n * 1.4 + 0.4)
scn.render.engine = 'CYCLES'; scn.cycles.samples = 24; scn.view_settings.view_transform = 'Standard'
scn.render.resolution_x = int(260 * max(1.8, n * 1.4 + 0.4)); scn.render.resolution_y = 520
mid = Vector(((n - 1) * 1.4 / 2, 0, 0.95))
for name, off in (('front', Vector((0, -30, 0))), ('right', Vector((30, 0, 0)))):
    if name == 'right' and n > 1: continue
    cam.location = mid + off; d = mid - cam.location; cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    scn.render.filepath = f'{out}_{name}.png'; bpy.ops.render.render(write_still=True)
print('rendered')
