"""The snowy spruce branch from ChatGPT (art/refs/batch6/fir_branch.png, on black) made into the cut-out texture the
Ardennes firs are dressed with (Resources/Textures/fir_branch.png): the black keyed out by brightness, the branch cropped
to itself with its base at the left edge, the colour bled into the clear parts so the cut-out edge has no black rim,
512 x 256. Its import settings are the hedge leaves' (leaves.png: alpha is transparency, mipmaps).
Run with the pipeline's python (PIL, numpy, scipy): python fir_branch.py"""
import os, re, uuid
import numpy as np
from PIL import Image
from scipy.ndimage import grey_dilation, binary_dilation

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, '..', 'refs', 'batch6', 'fir_branch.png')
TEX = os.path.join(HERE, '..', '..', 'unity', 'Assets', '_Game', 'Resources', 'Textures')

a = np.asarray(Image.open(SRC).convert('RGB'), np.float32) / 255.0
luma = a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114
alpha = np.clip((luma - 0.035) / 0.07, 0, 1)
ys, xs = np.where(alpha > 0.5); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
a, alpha = a[y0:y1, x0:x1], alpha[y0:y1, x0:x1]
img = Image.fromarray((np.dstack([a, alpha]) * 255).astype(np.uint8), 'RGBA').resize((512, 256), Image.LANCZOS)
rgba = np.asarray(img, np.float32) / 255.0; rgb, al = rgba[..., :3], rgba[..., 3]
# bleed: the clear pixels take the colour of the nearest leaf, so mipmaps and the cut-out edge stay green and white
solid = al > 0.3; filled = rgb.copy(); have = solid.copy()
for _ in range(24):
    grow = binary_dilation(have) & ~have
    if not grow.any(): break
    for ch in range(3):
        g = grey_dilation(np.where(have, filled[..., ch], 0), size=3); filled[..., ch] = np.where(grow, g, filled[..., ch])
    have |= grow
rgb = np.where(solid[..., None], rgb, filled)
Image.fromarray((np.dstack([rgb, al]) * 255).astype(np.uint8), 'RGBA').save(os.path.join(TEX, 'fir_branch.png'))
meta = os.path.join(TEX, 'fir_branch.png.meta')
if not os.path.exists(meta):
    t = open(os.path.join(TEX, 'leaves.png.meta'), encoding='utf-8', newline='').read()
    t = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, t, count=1).replace('leaves', 'fir_branch')
    open(meta, 'w', encoding='utf-8', newline='').write(t)
print('fir_branch.png 512x256 written')
