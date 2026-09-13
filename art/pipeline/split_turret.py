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

# footprint length along the long axis at increasing heights, ignoring the thin gun barrel near the centre line:
# the hull is long, the turret is short, so the ring is where the length collapses
lat = 2 if axis == 0 else 0
xc = (lo[lat] + hi[lat]) / 2
def length_at(y):
    sel = v[(v[:, 1] > y - 0.008) & (v[:, 1] < y + 0.008) & (np.abs(v[:, lat] - xc) > 0.04)]
    if len(sel) < 20: return 0.0
    return np.percentile(sel[:, axis], 97) - np.percentile(sel[:, axis], 3)
hull_length = max(length_at(lo[1] + height * fr) for fr in (0.35, 0.4, 0.45))
ring_y = None
for fr in np.arange(0.45, 0.95, 0.01):
    y = lo[1] + height * fr; L = length_at(y)
    if 0 < L < 0.75 * hull_length: ring_y = y; break
if ring_frac is not None: ring_y = lo[1] + height * ring_frac
if ring_y is None: ring_y = lo[1] + height * 0.62
ring_y += 0.015  # a hair above the deck so the deck slab stays with the hull
print(f"{name}: height {height:.3f}, ring height {ring_y:.3f} ({(ring_y - lo[1]) / height:.2f} of height)")

cent = v[f].mean(axis=1)
lat_v = np.abs(v[f][:, :, lat] - xc)            # lateral offset of each face's three vertices
above = cent[:, 1] > ring_y + 0.005
tur_c = cent[above]
ring_center = np.array([np.median(tur_c[:, 0]), ring_y, np.median(tur_c[:, 2])]) if above.any() else (lo + hi) / 2
along = cent[:, axis] - ring_center[axis]
lateral = np.abs(cent[:, lat] - ring_center[lat])
# the generated barrel is thin, bent and too short; it is removed here and the game adds a straight one of the real length.
# barrel faces: all three vertices close to the centre line, around ring height, well away from the ring along the long axis
thin = (lat_v.max(axis=1) < 0.06) & (cent[:, 1] > ring_y - 0.08) & (cent[:, 1] < ring_y + 0.14) & (np.abs(along) > 0.22 * size[axis])
forward = 1.0 if (thin & (along > 0)).sum() >= (thin & (along < 0)).sum() else -1.0
barrel = thin & (along * forward > 0)
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
