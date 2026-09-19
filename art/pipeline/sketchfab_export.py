"""An artist's model from Sketchfab (the glTF download) into the game: one OBJ per material for the hull and one per
material for the turret, the textures capped at 2048 with their normal maps, the long axis on Z, the nose at +Z, the
turret ring at the origin, scaled to the real hull length. The model keeps its own gun (no cut, no game barrel).
  python sketchfab_export.py <folder> <name> <hull_length_m> [ring=auto|<fraction of height>] [forward=+|-] [reach=0.25] [casemate]
Prints the numbers the VehicleSpec needs: ringHeight, the muzzle, the turret bounds.
Unity mirrors X when it imports an OBJ, so X is mirrored here first and the numbers printed are as the game sees them."""
import sys, os, json
import numpy as np
import trimesh
from PIL import Image

OUT = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Models'
folder, name, hull_len = sys.argv[1], sys.argv[2], float(sys.argv[3])
opts = dict(a.split('=') if '=' in a else (a, '1') for a in sys.argv[4:])
ring_opt = opts.get('ring', 'auto'); fwd_opt = opts.get('forward'); casemate = 'casemate' in opts

# ---- the scene, flattened: one mesh per material, in world space
scene = trimesh.load(os.path.join(folder, 'scene.gltf'))
by_mat = {}
for node in scene.graph.nodes_geometry:
    T, gname = scene.graph[node]; g = scene.geometry[gname]
    if not hasattr(g, 'faces'): continue
    m = g.copy(); m.apply_transform(T)
    mat = m.visual.material; key = getattr(mat, 'name', None) or gname
    by_mat.setdefault(key, {'meshes': [], 'mat': mat})['meshes'].append(m)
mats = list(by_mat.keys()); parts = []
for key in mats:
    m = trimesh.util.concatenate(by_mat[key]['meshes']); parts.append(m)
whole = trimesh.util.concatenate(parts); mat_of_face = np.concatenate([np.full(len(p.faces), i) for i, p in enumerate(parts)])
print(f'{name}: {len(mats)} materials, {len(whole.faces)} faces, bounds {(whole.bounds[1] - whole.bounds[0]).round(2).tolist()}')

# ---- orientation: long axis on Z; the nose is the end the gun sticks out of (thin overhang beyond the hull)
lo, hi = whole.bounds; size = hi - lo
R = np.eye(4)
if size[0] > size[2]: R = trimesh.transformations.rotation_matrix(np.pi / 2, [0, 1, 0])
whole.apply_transform(R); [p.apply_transform(R) for p in parts]
v = whole.vertices; lo, hi = whole.bounds; size = hi - lo
nb = 80; edges = np.linspace(lo[2], hi[2], nb + 1); widths = np.zeros(nb)
for i in range(nb):
    sel = (v[:, 2] >= edges[i]) & (v[:, 2] < edges[i + 1]); widths[i] = (v[sel, 0].max() - v[sel, 0].min()) if sel.sum() > 3 else 0
wide = np.where(widths > 0.33 * widths.max())[0]; z0, z1 = edges[wide.min()], edges[wide.max() + 1]; span = z1 - z0
over_plus, over_minus = hi[2] - z1, z0 - lo[2]
# the nose: the gun is the farthest thing from the middle on the centre line above the deck (the bustle is shorter)
xm = (np.percentile(v[:, 0], 1) + np.percentile(v[:, 0], 99)) / 2; zm = (z0 + z1) / 2
online = (np.abs(v[:, 0] - xm) < 0.06 * span) & (v[:, 1] > lo[1] + 0.55 * size[1]); far = v[online, 2] - zm
auto_fwd = 1.0 if far[np.argmax(np.abs(far))] > 0 else -1.0
forward = auto_fwd if fwd_opt is None else (1.0 if fwd_opt == '+' else -1.0)
print(f'hull span {span:.2f} of {size[2]:.2f}; gun reaches {far.max():.2f} ahead / {-far.min():.2f} behind the middle -> nose at {"+" if forward > 0 else "-"}z')
if forward < 0:
    R2 = trimesh.transformations.rotation_matrix(np.pi, [0, 1, 0]); whole.apply_transform(R2); [p.apply_transform(R2) for p in parts]
    v = whole.vertices; lo, hi = whole.bounds; z0, z1 = -z1, -z0
scale = hull_len / span; height = size[1]

# ---- the turret ring: scanning up from 45% of the height, where the width of the central third drops under 0.72 of the hull's
f = whole.faces; fc = v[f].mean(axis=1); fa = whole.area_faces
core = np.abs(fc[:, 2] - (z0 + z1) / 2) < 0.3 * span
def width_at(fr):
    y0 = lo[1] + height * fr; sel = core & (fc[:, 1] >= y0) & (fc[:, 1] < y0 + height * 0.03)
    return (np.percentile(fc[sel, 0], 97) - np.percentile(fc[sel, 0], 3)) if sel.sum() > 20 else 0
wmax = max(width_at(fr) for fr in np.arange(0.2, 0.6, 0.03))
if ring_opt == 'auto' and not casemate:
    ring_fr = next((fr for fr in np.arange(0.42, 0.9, 0.02) if width_at(fr) < 0.72 * wmax), 0.6)
else: ring_fr = float(ring_opt) if ring_opt != 'auto' else 1.0
ring_y = lo[1] + height * ring_fr
above = fc[:, 1] > ring_y
high = fc[:, 1] > ring_y + 0.15 * height   # the turret proper: stowage and logs on the hull sides sit just above the ring
tur_c = fc[high & core] if (high & core).sum() > 50 else fc[above]
xmid = (np.percentile(v[:, 0], 1) + np.percentile(v[:, 0], 99)) / 2   # tanks are symmetric: the turret sits on the hull's centre line
ring_c = np.array([xmid, ring_y, np.median(tur_c[:, 2])]) if not casemate else np.array([xmid, lo[1], (z0 + z1) / 2])
print(f'ring at {ring_fr:.2f} of the height ({(ring_y - lo[1]) * scale:.2f} m), centre x {ring_c[0] * scale:.2f} z {(ring_c[2] - (z0 + z1) / 2) * scale:.2f} from the hull middle')

# ---- turret: above the ring within reach of its centre, plus the gun ahead of it (narrow, on the centre line, above the deck)
if casemate: turret = np.zeros(len(f), dtype=bool)
else:
    dxz = np.hypot(fc[:, 0] - ring_c[0], fc[:, 2] - ring_c[2]); along = fc[:, 2] - ring_c[2]
    reach = float(opts.get('reach', 0.25)) * span   # how far from the ring centre the turret (bustle) goes
    body = above & (dxz < reach)
    gun = (fc[:, 1] > ring_y) & (np.abs(fc[:, 0] - ring_c[0]) < 0.07 * span) & (along > 0.2 * span)
    turret = body | gun
    print(f'turret {int(turret.sum())} faces ({int(gun.sum())} of the gun), hull {int((~turret).sum())}')

# ---- write: mirror X for Unity, ring centre at the origin, metres
def write_part(mask, fname, mi):
    idx = np.where(mask & (mat_of_face == mi))[0]
    if len(idx) == 0: return None
    part = whole.submesh([idx], append=True); part.apply_translation(-ring_c); part.apply_scale(scale)
    pv, pf, uv = part.vertices.copy(), part.faces, part.visual.uv; pv[:, 0] *= -1; pf = pf[:, ::-1]
    with open(os.path.join(OUT, f'{fname}.obj'), 'w') as o:
        o.write(f'mtllib {fname}.mtl\no {fname}\n')
        for p in pv: o.write(f'v {p[0]:.5f} {p[1]:.5f} {p[2]:.5f}\n')
        for t in uv: o.write(f'vt {t[0]:.5f} {t[1]:.5f}\n')
        o.write(f'usemtl {fname}\ns 1\n')
        for a, b, c in pf + 1: o.write(f'f {a}/{a} {b}/{b} {c}/{c}\n')
    with open(os.path.join(OUT, f'{fname}.mtl'), 'w') as o: o.write(f'newmtl {fname}\nKd 1 1 1\nmap_Kd {name}_m{mi}.png\n')
    return part
hull_parts, tur_parts = [], []
for mi, key in enumerate(mats):
    hp = write_part(~turret, f'{name}_hull_m{mi}', mi); tp = write_part(turret, f'{name}_turret_m{mi}', mi)
    if hp is not None: hull_parts.append(hp)
    if tp is not None: tur_parts.append(tp)
    mat = by_mat[key]['mat']; img = getattr(mat, 'baseColorTexture', None)
    if img is None:
        bcf = getattr(mat, 'baseColorFactor', None); bcf = [140, 140, 130] if bcf is None else [int(c) for c in np.asarray(bcf)[:3]]   # an untextured material: its flat colour
        img = Image.new('RGB', (64, 64), tuple(bcf))
    if img.mode == 'RGBA': img = Image.alpha_composite(Image.new('RGBA', img.size, (72, 76, 60, 255)), img)   # a decal sheet: its empty parts go dark, not white
    img = img.convert('RGB'); img.thumbnail((2048, 2048)); img.save(os.path.join(OUT, f'{name}_m{mi}.png'))
    nrm = getattr(mat, 'normalTexture', None)
    if nrm is not None: nrm = nrm.convert('RGB'); nrm.thumbnail((2048, 2048)); nrm.save(os.path.join(OUT, f'{name}_m{mi}_n.png'))
    print(f'  m{mi} {key[:30]:30s} hull {0 if hp is None else len(hp.faces):6d}  turret {0 if tp is None else len(tp.faces):6d}  tex {img.size}{" +n" if nrm is not None else ""}')

allh = trimesh.util.concatenate(hull_parts); hb = allh.bounds
print(f'hull bounds x {hb[0][0]:.2f}..{hb[1][0]:.2f}  y {hb[0][1]:.2f}..{hb[1][1]:.2f}  z {hb[0][2]:.2f}..{hb[1][2]:.2f}  -> ringHeight = {-hb[0][1]:.2f}f')
if tur_parts:
    allt = trimesh.util.concatenate(tur_parts); tb = allt.bounds; tv = allt.vertices
    # the muzzle: the farthest vertices ahead on the centre line
    tip = tv[tv[:, 2] > tb[1][2] - 0.05]; tipc = tip.mean(axis=0)
    print(f'turret bounds x {tb[0][0]:.2f}..{tb[1][0]:.2f}  y {tb[0][1]:.2f}..{tb[1][1]:.2f}  z {tb[0][2]:.2f}..{tb[1][2]:.2f}; muzzle = new Vector3({tipc[0]:.2f}f, {tipc[1]:.2f}f, {tipc[2]:.2f}f)')
else:
    tip = allh.vertices[allh.vertices[:, 2] > hb[1][2] - 0.05]; tipc = tip.mean(axis=0); print(f'muzzle = new Vector3({tipc[0]:.2f}f, {tipc[1]:.2f}f, {tipc[2]:.2f}f)')
meta_path = os.path.join('D:/Codes/Projects/lightswarm/art/models/sketchfab', 'meta.json'); meta = json.load(open(meta_path)) if os.path.exists(meta_path) else {}
meta[name] = {'materials': len(mats), 'ringHeight': float(-hb[0][1]), 'muzzle': [float(x) for x in tipc], 'hullLength': hull_len, 'ringFrac': float(ring_fr)}
json.dump(meta, open(meta_path, 'w'), indent=1)
