# blender -b --factory-startup -P fir_cards_render.py -- <out_dir> <obj>:<turn_deg> [<obj>:<turn_deg> ...]
# The forest's fir cards: each fir model (from TRELLIS) rendered orthographically along the battle camera's fixed line
# of sight (looking north and 47.7 degrees down, as Battle.PlaceCamera), on a transparent background, one PNG per
# model and turn. Prints for each the card's size in metres and where the tree's foot falls on it, as the game needs them.
# OBJ +Z (the game's north) comes into Blender as -Y; the camera looks towards -Y and down.
import sys, math, json, bpy
from mathutils import Vector

a = sys.argv[sys.argv.index('--') + 1:]; out = a[0]; jobs = a[1:]
PITCH = math.atan2(44.0, 40.0)   # the camera's look: (0, -44, 40) in the game
PX = 512   # half the pixels along a card's longer side

info = []
for n, job in enumerate(jobs):
    path, turn = job.rsplit(':', 1)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.obj_import(filepath=path)
    obj = [o for o in bpy.context.scene.objects if o.type == 'MESH'][0]
    obj.rotation_euler = (math.radians(90), 0, math.radians(float(turn)))   # the importer's own turn, then ours about the vertical
    bpy.context.view_layer.update()
    # the view in Blender's axes: forward f, right r, up u (the card's upright: up and away from the camera)
    f = Vector((0, -math.cos(PITCH), -math.sin(PITCH)))   # towards -Y (north) and down
    r = Vector((1, 0, 0)); u = r.cross(f).normalized() * -1 if r.cross(f).z < 0 else r.cross(f).normalized()
    pts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    xs = [p.dot(r) for p in pts]; ys = [p.dot(u) for p in pts]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys); pad = 0.03 * (y1 - y0)
    x0 -= pad; x1 += pad; y0 -= pad; y1 += pad
    w, h = x1 - x0, y1 - y0
    scn = bpy.context.scene
    cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')); scn.collection.objects.link(cam); scn.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = max(w, h)
    centre = r * ((x0 + x1) / 2) + u * ((y0 + y1) / 2)
    cam.location = centre - f * 60; cam.rotation_euler = f.to_track_quat('-Z', 'Y').to_euler()
    cam.data.sensor_fit = 'VERTICAL' if h >= w else 'HORIZONTAL'
    scn.render.resolution_x = int(round(PX * 2 * w / h)) if h >= w else PX * 2
    scn.render.resolution_y = PX * 2 if h >= w else int(round(PX * 2 * h / w))
    # the light: a soft moon-like key from above and the south, a dim sky all round, so the snow reads on the tops
    w_ = bpy.data.worlds.new('W'); scn.world = w_; w_.use_nodes = True; w_.node_tree.nodes['Background'].inputs[0].default_value = (0.55, 0.58, 0.62, 1); w_.node_tree.nodes['Background'].inputs[1].default_value = 0.7
    sun = bpy.data.objects.new('Sun', bpy.data.lights.new('Sun', 'SUN')); sun.data.energy = 3.2; sun.rotation_euler = (math.radians(35), 0, math.radians(200)); scn.collection.objects.link(sun)
    scn.render.film_transparent = True; scn.render.image_settings.file_format = 'PNG'; scn.render.image_settings.color_mode = 'RGBA'
    scn.render.engine = 'CYCLES'; scn.cycles.samples = 48; scn.view_settings.view_transform = 'Standard'
    name = '%s/card_%d.png' % (out, n); scn.render.filepath = name; bpy.ops.render.render(write_still=True)
    foot = (-x0 / w, -y0 / h)   # the tree's foot (the origin) on the card, from its left and its bottom
    info.append({'png': name, 'w': w, 'h': h, 'foot_u': foot[0], 'foot_v': foot[1]})
json.dump(info, open(out + '/cards.json', 'w'), indent=1)
print('CARDS', json.dumps(info))
