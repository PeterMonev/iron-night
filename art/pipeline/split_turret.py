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
ring_frac = float(sys.argv[5]) if len(sys.argv) > 5 else None

scene = trimesh.load(src)
mesh = list(scene.geometry.values())[0] if isinstance(scene, trimesh.Scene) else scene
v, f = mesh.vertices, mesh.faces
lo, hi = mesh.bounds
# the model is normalised with Y up; the long axis is Z (front = +Z or -Z; TRELLIS keeps the image's facing so we detect it below)
size = hi - lo
axis = int(np.argmax([size[0], size[2]])) * 2  # 0 = X, 2 = Z
height = size[1]

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
tur_c = cent[above]
ring_center = np.array([np.median(tur_c[:, 0]), ring_y, np.median(tur_c[:, 2])]) if above.any() else (lo + hi) / 2
along = cent[:, axis] - ring_center[axis]
lateral = np.abs(cent[:, lat] - ring_center[lat])
# the generated barrel is bent and too short; it is removed and the game adds a straight one of the real length.
# its axis is measured from the part that sticks out beyond the hull's nose, then everything inside a cylinder around
# that axis, from the turret face forward, goes - whichever part it would have landed in
along = cent[:, axis] - ring_center[axis]
above = cent[:, 1] > ring_y + 0.005
narrow = (lat_v.max(axis=1) - lat_v.min(axis=1)) < 0.05          # faces of something thin, like a barrel
# forward: the glacis - a large plate sloping up toward the turret - is at the front of every tank; the rear is vertical
# doors and a flat engine deck. Compare the sloped area at the two ends.
sloped = (fn[:, 1] > 0.35) & (fn[:, 1] < 0.9) & (np.abs(fn[:, axis]) > 0.4)
mid = (lo[axis] + hi[axis]) / 2
area_plus = fa[sloped & (cent[:, axis] > mid + 0.25 * size[axis])].sum(); area_minus = fa[sloped & (cent[:, axis] < mid - 0.25 * size[axis])].sum()
forward = 1.0 if area_plus >= area_minus else -1.0
print(f"glacis area +{area_plus:.3f} / -{area_minus:.3f}")
# the barrel: narrow faces near the centre line ahead of the turret
thin_ahead = above & narrow & (along * forward > 0.2 * size[axis]) & (np.abs(cent[:, lat] - xc) < 0.08 * size[lat])
n_thin = int(thin_ahead.sum()); print(f"barrel candidates ahead: {n_thin} faces")
if n_thin > 15:
    bx = np.median(cent[thin_ahead, lat]); by = np.median(cent[thin_ahead, 1])
    rb = float(np.clip(1.7 * np.percentile(np.abs(cent[thin_ahead, lat] - bx), 80), 0.02, 0.036))
    body = above & (np.abs(cent[:, lat] - bx) > rb * 1.3) & (np.abs(along) < 0.3 * size[axis])
    tfront = np.percentile(along[body] * forward, 97) if body.sum() > 50 else 0.2 * size[axis]
    barrel = (np.abs(cent[:, lat] - bx) < rb) & (np.abs(cent[:, 1] - by) < rb * 1.2) & (along * forward > tfront + 0.005)
    print(f"barrel axis lateral {bx - xc:+.3f}, radius {rb:.3f}, turret front at {tfront / size[axis]:.2f} of the hull length")
else:
    barrel = np.zeros(len(f), dtype=bool); print("no barrel found")
lateral = np.abs(cent[:, lat] - ring_center[lat])
# turret: above the ring and not far along the hull (stowage on the deck corners stays with the hull); the mantlet stub near the centre stays
far = np.abs(along) > 0.3 * size[axis]
turret = (above & ~far) | (above & (lateral < 0.08) & (along * forward > 0) & (along * forward < 0.3 * size[axis]))
turret = turret & ~barrel
hull = ~turret & ~barrel
print(f"forward is {'+' if forward > 0 else '-'}{'x' if axis == 0 else 'z'}, barrel faces removed: {int(barrel.sum())}")

os.makedirs(out_dir, exist_ok=True)
scale = hull_len / size[axis]
def export(mask, fname):
    part = mesh.submesh([np.where(mask)[0]], append=True)
    part.apply_translation(-ring_center)  # pivot = turret ring centre for both parts
    part.apply_scale(scale)
    part.export(os.path.join(out_dir, fname))
    return len(part.faces)
nh = export(hull, f"{name}_hull.glb"); nt = export(turret, f"{name}_turret.glb")
meta_path = os.path.join(out_dir, 'meta.json'); meta = json.load(open(meta_path)) if os.path.exists(meta_path) else {}
meta[name] = {"hullFaces": int(nh), "turretFaces": int(nt), "ringHeight": float((ring_y - lo[1]) * scale), "longAxis": "x" if axis == 0 else "z", "forward": float(forward), "hullLength": hull_len, "ringCenter": [float(c) for c in ring_center]}
json.dump(meta, open(meta_path, 'w'), indent=1)
print(f"hull {nh} faces, turret {nt} faces, long axis {'X' if axis == 0 else 'Z'}, scale {scale:.2f}")
