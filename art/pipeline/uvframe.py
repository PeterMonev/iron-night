"""For a point of a model's texture: which face of the model it lies on, and which way on the texture world up and the
crate's length run. python uvframe.py <obj> u v [u v ...]"""
import sys
import numpy as np
obj = sys.argv[1]; pts = [(float(sys.argv[i]), float(sys.argv[i + 1])) for i in range(2, len(sys.argv), 2)]
V, T, F = [], [], []
for l in open(obj):
    if l.startswith('v '): V.append([float(x) for x in l.split()[1:4]])
    elif l.startswith('vt '): T.append([float(x) for x in l.split()[1:3]])
    elif l.startswith('f '): F.append([[int(i) - 1 for i in c.split('/')[:2]] for c in l.split()[1:4]])
V, T, F = np.array(V), np.array(T), np.array(F)
def frame(u, v):
    p = np.array([u, v])
    for f in F:
        t0, t1, t2 = T[f[:, 1]]; m = np.array([t1 - t0, t2 - t0]).T
        if abs(np.linalg.det(m)) < 1e-12: continue
        a, b = np.linalg.solve(m, p - t0)
        if a >= 0 and b >= 0 and a + b <= 1:
            P0, P1, P2 = V[f[:, 0]]; E = np.array([P1 - P0, P2 - P0]).T; J = E @ np.linalg.inv(m)   # d(position)/d(uv)
            n = np.cross(P1 - P0, P2 - P0); n /= np.linalg.norm(n); c = P0 * (1 - a - b) + P1 * a + P2 * b
            dirs = {}
            for name, w in {'up': [0, 1, 0], '+z': [0, 0, 1], '-z': [0, 0, -1], '-x': [-1, 0, 0]}.items():
                uvd = np.linalg.pinv(J) @ np.array(w, float); img = np.array([uvd[0], -uvd[1]]); nrm = np.linalg.norm(img)
                dirs[name] = (img / nrm).round(2) if nrm > 1e-9 else None
            return c.round(3), n.round(2), dirs, np.linalg.norm(J, axis=0).round(3)
    return None
for u, v in pts: print((u, v), frame(u, v))
