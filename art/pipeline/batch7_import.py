"""The seventh batch from ChatGPT (art/refs/batch7) into the game:
  photo_<tank>.png   -> Resources/UI/photo_<tank>.jpg, 720 x 480: the depot's tank photographs, now in colour
  smoke_puff_1..4    -> Resources/Fx/smoke_puff_<n>.png, 512: white smoke on black made into white smoke with soft alpha
                        (alpha from brightness, the colour unpremultiplied so the shading stays), cropped to the puff
  paint_ring         -> Resources/Fx/paint_ring.png, 512: the painted marker ring, keyed the same way and set so its
                        stroke runs at 0.4 of the picture from the centre, as the ring the code paints (Fx.PaintedRing)
New textures take the import settings of Fx/fx_smoke.png. Run with the pipeline's python: python batch7_import.py"""
import os, re, uuid
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__)); REFS = os.path.join(HERE, '..', 'refs', 'batch7')
RES = os.path.join(HERE, '..', '..', 'unity', 'Assets', '_Game', 'Resources')
TANKS = ['sherman', 'easy8', 'hellcat', 'chaffee', 'pershing', 'm10', 'firefly', 't34_85', 'kv1', 'su100', 'is2']


def keyed(path):
    """White on black as white with alpha: alpha from brightness, the colour divided back out of it."""
    a = np.asarray(Image.open(path).convert('RGB'), np.float32) / 255.0
    luma = a.max(axis=2); alpha = np.clip((luma - 0.02) / 0.75, 0, 1)
    rgb = np.clip(a / np.maximum(alpha[..., None], 1e-3), 0, 1); rgb[alpha < 0.01] = 1.0
    return rgb, alpha


def save_rgba(rgb, alpha, path):
    Image.fromarray((np.dstack([rgb, alpha]) * 255).astype(np.uint8), 'RGBA').save(path)


def meta_like(path, template):
    m = path + '.meta'
    if os.path.exists(m): return
    t = open(template + '.meta', encoding='utf-8', newline='').read()
    t = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, t, count=1).replace(os.path.splitext(os.path.basename(template))[0], os.path.splitext(os.path.basename(path))[0])
    open(m, 'w', encoding='utf-8', newline='').write(t)


for t in TANKS:
    im = Image.open(os.path.join(REFS, 'photo_%s.png' % t)).convert('RGB')
    w, h = im.size; want = w / h
    if abs(want - 1.5) > 0.01:   # to 3:2, cut evenly
        if want > 1.5: nw = int(h * 1.5); im = im.crop(((w - nw) // 2, 0, (w - nw) // 2 + nw, h))
        else: nh = int(w / 1.5); im = im.crop((0, (h - nh) // 2, w, (h - nh) // 2 + nh))
    im.resize((720, 480), Image.LANCZOS).save(os.path.join(RES, 'UI', 'photo_%s.jpg' % t), quality=90)
    print('photo_%s.jpg' % t)

FX = os.path.join(RES, 'Fx'); TEMPLATE = os.path.join(FX, 'fx_smoke.png')
for n in range(1, 5):
    rgb, al = keyed(os.path.join(REFS, 'smoke_puff_%d.png' % n))
    ys, xs = np.where(al > 0.03); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    side = max(y1 - y0, x1 - x0); cy, cx = (y0 + y1) // 2, (x0 + x1) // 2; half = int(side * 0.56)
    pad = half + 2; rgb = np.pad(rgb, ((pad, pad), (pad, pad), (0, 0)), constant_values=1.0); al = np.pad(al, pad)
    cy += pad; cx += pad; rgb, al = rgb[cy - half:cy + half, cx - half:cx + half], al[cy - half:cy + half, cx - half:cx + half]
    out = os.path.join(FX, 'smoke_puff_%d.png' % n)
    img = Image.fromarray((np.dstack([rgb, al]) * 255).astype(np.uint8), 'RGBA').resize((512, 512), Image.LANCZOS); img.save(out)
    meta_like(out, TEMPLATE); print('smoke_puff_%d.png' % n)

rgb, al = keyed(os.path.join(REFS, 'paint_ring.png'))
ys, xs = np.where(al > 0.3); cy, cx = (ys.min() + ys.max()) / 2, (xs.min() + xs.max()) / 2
radius = np.median(np.hypot(ys - cy, xs - cx))   # the middle of the stroke
half = int(radius / 0.8)   # the stroke at 0.4 of the picture, half the picture is 0.5: half = radius / 0.8
pad = half + 2; rgb = np.pad(rgb, ((pad, pad), (pad, pad), (0, 0)), constant_values=1.0); al = np.pad(al, pad); cy, cx = int(cy) + pad, int(cx) + pad
rgb, al = rgb[cy - half:cy + half, cx - half:cx + half], al[cy - half:cy + half, cx - half:cx + half]
out = os.path.join(FX, 'paint_ring.png')
Image.fromarray((np.dstack([rgb, al]) * 255).astype(np.uint8), 'RGBA').resize((512, 512), Image.LANCZOS).save(out)
meta_like(out, TEMPLATE); print('paint_ring.png')
