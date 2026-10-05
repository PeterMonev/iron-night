"""The photographed flames and fireballs from ChatGPT (art/refs/batch8: flame_1..4, fireball_1..2, on black) into
Resources/Fx as additive sprites: alpha from brightness, the colour divided back out of it, cropped to the fire with a
margin, 512 x 512, the flame's foot at the bottom of the picture. Missing ones are skipped. Import settings: Fx/fx_smoke's.
Run with the pipeline's python: python batch8_fire.py"""
import os, re, uuid
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__)); REFS = os.path.join(HERE, '..', 'refs', 'batch8')
FX = os.path.join(HERE, '..', '..', 'unity', 'Assets', '_Game', 'Resources', 'Fx'); TEMPLATE = os.path.join(FX, 'fx_smoke.png')


def meta_like(path):
    m = path + '.meta'
    if os.path.exists(m): return
    t = open(TEMPLATE + '.meta', encoding='utf-8', newline='').read()
    t = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, t, count=1).replace('fx_smoke', os.path.splitext(os.path.basename(path))[0])
    open(m, 'w', encoding='utf-8', newline='').write(t)


for name in ['flame_1', 'flame_2', 'flame_3', 'flame_4', 'fireball_1', 'fireball_2']:
    src = os.path.join(REFS, name + '.png')
    if not os.path.exists(src): print(name, 'not there'); continue
    a = np.asarray(Image.open(src).convert('RGB'), np.float32) / 255.0
    alpha = np.clip((a.max(axis=2) - 0.03) / 0.6, 0, 1); rgb = np.clip(a / np.maximum(alpha[..., None], 1e-3), 0, 1); rgb[alpha < 0.01] = 0
    ys, xs = np.where(alpha > 0.04); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    side = int(max(y1 - y0, x1 - x0) * 1.06); cx = (x0 + x1) // 2
    top = y1 - side if name.startswith('flame') else (y0 + y1) // 2 - side // 2   # a flame stands on the bottom edge, a fireball in the middle
    pad = side; rgb = np.pad(rgb, ((pad, pad), (pad, pad), (0, 0))); alpha = np.pad(alpha, pad)
    top += pad; left = cx - side // 2 + pad
    rgb, alpha = rgb[top:top + side, left:left + side], alpha[top:top + side, left:left + side]
    out = os.path.join(FX, name + '.png')
    Image.fromarray((np.dstack([rgb, alpha]) * 255).astype(np.uint8), 'RGBA').resize((512, 512), Image.LANCZOS).save(out)
    meta_like(out); print(name + '.png')
