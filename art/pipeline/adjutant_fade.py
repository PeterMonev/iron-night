# The adjutant's pictures (art/refs/adjutant, painted by ChatGPT on black with a blue glow on the left and an amber one
# on the right) made ready for the menu: no cut-out, the edges fade to nothing instead, so the picture melts into the
# dark sheet it stands on and the glows read as light behind her. The sides fade over their outer 10%, the bottom
# (where the picture cuts through her legs) over its last 16%, the top barely, since the caps reach almost to it.
# python adjutant_fade.py  ->  unity/Assets/_Game/Resources/UI/adjutant_<nation>[_offduty].png
import os
import numpy as np
from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), '..', '..')
SRC = os.path.join(ROOT, 'art', 'refs', 'adjutant')
DST = os.path.join(ROOT, 'unity', 'Assets', '_Game', 'Resources', 'UI')


def smooth(x):
    x = np.clip(x, 0.0, 1.0)
    return x * x * (3.0 - 2.0 * x)


for name in ('adjutant_us', 'adjutant_su', 'adjutant_us_offduty', 'adjutant_su_offduty'):
    im = Image.open(os.path.join(SRC, name + '.png')).convert('RGBA')
    w, h = im.size
    x = (np.arange(w) + 0.5) / w
    y = (np.arange(h) + 0.5) / h
    side = smooth(np.minimum(x / 0.10, (1.0 - x) / 0.10))
    vert = smooth(np.minimum(y / 0.012, (1.0 - y) / 0.16))
    fade = np.outer(vert, side)
    a = np.asarray(im).astype(np.float32)
    a[..., 3] *= fade
    Image.fromarray(a.clip(0, 255).astype(np.uint8), 'RGBA').save(os.path.join(DST, name + '.png'))
    print(name, w, h)
