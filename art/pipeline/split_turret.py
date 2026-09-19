"""Splits a TRELLIS tank GLB into hull and turret meshes for the game.

The model is normalised (longest side = 1) with Y up and the hull's long axis along Z. The turret ring height is found
from the horizontal cross-section: going up from the deck, the footprint area drops sharply where the hull ends and the
turret begins. Faces whose centroid lies above that height, or that belong to the gun (in front of the ring, above the deck),
go to the turret; everything else is the hull. Both parts keep the original texture. Scale is set so the hull length is
real metres. Usage: python split_turret.py <in.glb> <out_dir> <name> <hull_length_m> [ring_z_fraction]
"""
import sys, os, json
import numpy as np
import trimesh

src, out_dir, name, hull_len = sys.argv[1], sys.argv[2], sys.argv[3], float(sys.argv[4])
ring_frac = float(sys.argv[5]) if len(sys.argv) > 5 and sys.argv[5] not in ("", "auto") else None
force_forward = float(sys.argv[6]) if len(sys.argv) > 6 else None   # +1/-1: which end the model's barrel is at, when the glacis rule gets it wrong

scene = trimesh.load(src)
mesh = list(scene.geometry.values())[0] if isinstance(scene, trimesh.Scene) else scene
v, f = mesh.vertices, mesh.faces
lo, hi = mesh.bounds
# the model is normalised with Y up; the long axis is Z (front = +Z or -Z; TRELLIS keeps the image's facing so we detect it below)
size = hi - lo
axis = int(np.argmax([size[0], size[2]])) * 2  # 0 = X, 2 = Z
height = size[1]
# the hull's own length, without the barrel: bins along the long axis where the model is wider than a third of its widest
lat0 = 2 if axis == 0 else 0; nb = 60; edges = np.linspace(lo[axis], hi[axis], nb + 1); widths = np.zeros(nb)
for i in range(nb):
    sel = (v[:, axis] >= edges[i]) & (v[:, axis] < edges[i + 1]); widths[i] = (v[sel, lat0].max() - v[sel, lat0].min()) if sel.sum() > 3 else 0
wide = np.where(widths > 0.33 * widths.max())[0]; hull_z0, hull_z1 = edges[wide.min()], edges[wide.max() + 1]
hull_span = hull_z1 - hull_z0; print(f'hull without the barrel: {hull_span / size[axis]:.2f} of the model length')
size[axis] = hull_span   # every fraction below is of the hull, not of the gun

# the turret ring sits on the hull deck: the deck is the height band with the most upward-facing area between 40% and 80%
# of the model height (roof of the hull, sponsons); the turret walls rise from there
lat = 2 if axis == 0 else 0
xc = (lo[lat] + hi[lat]) / 2
fn = mesh.face_normals; fc = v[f].mean(axis=1); fa = mesh.area_faces
up = (fn[:, 1] > 0.85) & (np.abs(fc[:, lat] - xc) > 0.12 * size[lat])   # flat faces away from the centre line (the turret roof is central)
bands = np.arange(0.40, 0.80, 0.02); best, best_area = None, 0.0
for fr in bands:
    y0 = lo[1] + height * fr; sel = up & (fc[:, 1] >= y0) & (fc[:, 1] < y0 + height * 0.02)
    area = fa[sel].sum()
    if area > best_area: best_area, best = area, fr
ring_y = lo[1] + height * (best + 0.015) if best is not None else lo[1] + height * 0.6
if ring_frac is not None: ring_y = lo[1] + height * ring_frac
print(f"{name}: height {height:.3f}, deck band {best}, ring height {ring_y:.3f} ({(ring_y - lo[1]) / height:.2f} of height)")

cent = v[f].mean(axis=1)
lat_v = np.abs(v[f][:, :, lat] - xc)            # lateral offset of each face's three vertices
above = cent[:, 1] > ring_y + 0.005
high = cent[:, 1] > ring_y + 0.12 * height   # the turret proper, clear of hatches and stowage on the deck
tur_c = cent[high] if high.sum() > 50 else cent[above]
ring_center = np.array([np.median(tur_c[:, 0]), ring_y, np.median(tur_c[:, 2])]) if above.any() else (lo + hi) / 2
along = cent[:, axis] - ring_center[axis]
lateral = np.abs(cent[:, lat] - ring_center[lat])
# the generated barrel is bent and too short; it is removed and the game adds a straight one of the real length.
# its axis is measured from the part that sticks out beyond the hull's nose, then everything inside a cylinder around
# that axis, from the turret face forward, goes - whichever part it would have landed in
along = cent[:, axis] - ring_center[axis]
above = cent[:, 1] > ring_y + 0.005
narrow = (lat_v.max(axis=1) - lat_v.min(axis=1)) < 0.05          # faces of something thin, like a barrel
# forward: the glacis - the big plate sloping up toward the turret - is at the front of every tank; the rear is vertical
# doors and a flat engine deck. (A roof machine gun pointing backwards fooled the "where is the thin tube" rule.)
sloped = (fn[:, 1] > 0.35) & (fn[:, 1] < 0.9) & (np.abs(fn[:, axis]) > 0.4)
mid = (hull_z0 + hull_z1) / 2
area_plus = fa[sloped & (cent[:, axis] > mid + 0.25 * size[axis])].sum(); area_minus = fa[sloped & (cent[:, axis] < mid - 0.25 * size[axis])].sum()
forward = force_forward if force_forward is not None else (1.0 if area_plus >= area_minus else -1.0)
thin_ahead = above & narrow & (along * forward > 0.2 * size[axis]) & (np.abs(cent[:, lat] - xc) < 0.08 * size[lat])
n_thin = int(thin_ahead.sum())
print(f"glacis area +{area_plus:.3f} / -{area_minus:.3f} -> front is {'+' if forward > 0 else '-'}{'x' if axis == 0 else 'z'}; {n_thin} narrow faces ahead")
# the turret's front face: the widest part of the turret body ahead of the ring (mantlet included); everything in front
# of it at turret height is gun, coaxial gun and sight tubes - the game adds a straight gun of the real length there
body = above & ~narrow & (np.abs(along) < 0.3 * size[axis])
tfront = np.percentile(along[body] * forward, 96) if body.sum() > 50 else 0.2 * size[axis]
gunzone = above & (along * forward > tfront + 0.005)
barrel = gunzone
print(f"turret front at {tfront / size[axis]:.2f} of the hull length, faces ahead of it removed: {int(gunzone.sum())}")
lateral = np.abs(cent[:, lat] - ring_center[lat])
far = np.abs(along) > 0.3 * size[axis]
turret = (above & ~far) & (lateral < 0.24 * size[axis]) & ~barrel

# above the roof: keep the cupola and hatch lids (compact, flat), drop tubes and aerials (elongated, thin)
import scipy.sparse as sp
from scipy.sparse.csgraph import connected_components
roof_faces = turret & (fn[:, 1] > 0.8) & (fa > np.percentile(fa[turret], 60))
roof = np.median(cent[roof_faces, 1]) if roof_faces.sum() > 20 else ring_y + 0.1
cand = turret & (cent[:, 1] > roof + 0.015 * height)
idx = np.where(cand)[0]; pos = {f_: i for i, f_ in enumerate(idx)}
adj = mesh.face_adjacency; keep = cand[adj[:, 0]] & cand[adj[:, 1]]
rows = [pos[x] for x in adj[keep, 0]]; cols = [pos[x] for x in adj[keep, 1]]
graph = sp.coo_matrix((np.ones(len(rows)), (rows, cols)), shape=(len(idx), len(idx)))
ncomp, labels = connected_components(graph, directed=False)
clutter = np.zeros(len(f), dtype=bool)
for c_ in range(ncomp):
    members = idx[labels == c_]; pts = v[f[members]].reshape(-1, 3)
    ext = np.sort([pts[:, 0].max() - pts[:, 0].min(), pts[:, 2].max() - pts[:, 2].min()])  # horizontal extents, small to large
    tall = pts[:, 1].max() - roof; area = fa[members].sum()
    elongated = ext[1] > 2.2 * max(ext[0], 0.01) and ext[0] < 0.07 * size[axis]     # a tube lying on the roof
    spike = tall > 0.1 * height and ext[1] < 0.08 * size[axis]                     # an aerial standing on it
    tiny = area < 0.002 * fa[turret].sum()                                          # loose shards
    if elongated or spike or tiny: clutter[members] = True
turret = turret & ~clutter
print(f"roof at {(roof - lo[1]) / height:.2f} of height, {ncomp} pieces above it, tubes/aerials/shards removed: {int(clutter.sum())}")
hull = ~turret & ~barrel & ~clutter

os.makedirs(out_dir, exist_ok=True)
scale = hull_len / hull_span
def export(mask, fname):
    part = mesh.submesh([np.where(mask)[0]], append=True)
    try:
        n0 = len(part.faces); part.fill_holes(); print(f"  {fname}: holes capped, {len(part.faces) - n0} faces added")
    except Exception as ex: print('  fill_holes failed', ex)
    part.apply_translation(-ring_center)  # pivot = turret ring centre for both parts
    part.apply_scale(scale)
    part.export(os.path.join(out_dir, fname))
    return len(part.faces)
nh = export(hull, f"{name}_hull.glb"); nt = export(turret, f"{name}_turret.glb")
meta_path = os.path.join(out_dir, 'meta.json'); meta = json.load(open(meta_path)) if os.path.exists(meta_path) else {}
meta[name] = {"hullFaces": int(nh), "turretFaces": int(nt), "ringHeight": float((ring_y - lo[1]) * scale), "longAxis": "x" if axis == 0 else "z", "forward": float(forward), "hullLength": hull_len, "ringCenter": [float(c) for c in ring_center]}
json.dump(meta, open(meta_path, 'w'), indent=1)
print(f"hull {nh} faces, turret {nt} faces, long axis {'X' if axis == 0 else 'Z'}, scale {scale:.2f}")
