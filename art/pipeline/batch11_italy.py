"""The Italian front's flat pictures from ChatGPT (art/refs/batch11): the four fields into Resources/Textures as
ground_italy_* (seamless 1024, night tone, a normal map from the brightness, as bake-photo-fields.js does them), the
ways in, the stand, convoy and raid pictures and the Anzio operation into Resources/UI, and the new map of Europe over
the old one (its meta kept). Import settings copied from their Normandy and Kursk fellows with a new guid.
Run with the pipeline's python: python batch11_italy.py"""
import os, re, uuid, shutil
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__)); REFS = os.path.join(HERE, '..', 'refs', 'batch11')
RES = os.path.join(HERE, '..', '..', 'unity', 'Assets', '_Game', 'Resources'); TEX = os.path.join(RES, 'Textures'); UI = os.path.join(RES, 'UI')


def meta_like(path, template):
    m = path + '.meta'
    if os.path.exists(m): return
    t = open(template + '.meta', encoding='utf-8', newline='').read()
    t = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, t, count=1)
    open(m, 'w', encoding='utf-8', newline='').write(t)


def seamless(a):
    """The four quadrants swapped, the original blended back over the cross of seams."""
    H, W = a.shape[:2]; s = np.roll(np.roll(a, H // 2, 0), W // 2, 1)
    x = np.abs(np.arange(W) - W / 2) / (W * 0.22); y = np.abs(np.arange(H) - H / 2) / (H * 0.22)
    k = np.clip(np.maximum(1 - x[None, :], 1 - y[:, None]), 0, 1)[..., None]
    return s * (1 - k) + a * k


def night(a, sat, gain, cool):
    y = (0.3 * a[..., 0] + 0.59 * a[..., 1] + 0.11 * a[..., 2])[..., None]; o = (y + (a - y) * sat) * gain
    o[..., 0] *= 1 - cool; o[..., 2] *= 1 + cool; return o


def normal_map(a, strength):
    h = (0.3 * a[..., 0] + 0.59 * a[..., 1] + 0.11 * a[..., 2]) / 255.0
    h = sum(np.roll(np.roll(h, dy, 0), dx, 1) for dy in (-1, 0, 1) for dx in (-1, 0, 1)) / 9.0
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength; dv = (np.roll(h, 1, 0) - np.roll(h, -1, 0)) * strength
    l = np.sqrt(dx * dx + dv * dv + 1)
    return np.dstack([128 - dx / l * 127, 128 - dv / l * 127, 128 + 127 / l]).clip(0, 255).astype(np.uint8)


DL = os.path.join(os.path.expanduser('~'), 'Downloads')
def src(name):
    p = os.path.join(REFS, name + '.png')
    if not os.path.exists(p): shutil.copy(os.path.join(DL, name + '.png'), p)
    return Image.open(p).convert('RGB')


for name, sat, gain, nstr in [('ground_italy_plough', 0.85, 0.6, 1.3), ('ground_italy_pasture', 0.7, 0.52, 2.0), ('ground_italy_mown', 0.7, 0.5, 1.6), ('ground_italy_stubble', 0.75, 0.5, 1.6)]:
    a = seamless(np.asarray(src(name).resize((1024, 1024), Image.LANCZOS), np.float32))
    out = os.path.join(TEX, name + '.png'); Image.fromarray(normal_map(a, nstr)).save(out[:-4] + '_n.png'); meta_like(out[:-4] + '_n.png', os.path.join(TEX, 'ground_kursk_plough_n.png'))
    Image.fromarray(night(a, sat, gain, 0.0).clip(0, 255).astype(np.uint8)).save(out); meta_like(out, os.path.join(TEX, 'ground_kursk_plough.png')); print(name)

for name in ['route_italy_town', 'route_italy_valley', 'route_italy_groves', 'op_anzio', 'op_anzio_1', 'op_anzio_2', 'op_anzio_3', 'op_anzio_4', 'op_anzio_5']:
    out = os.path.join(UI, name + '.png'); src(name).resize((1024, 1024), Image.LANCZOS).save(out); meta_like(out, os.path.join(UI, 'route_village.png')); print(name)
for name in ['stand_italy', 'convoy_italy', 'sneak_italy']:
    out = os.path.join(UI, name + '.png'); src(name).resize((1536, 1024), Image.LANCZOS).save(out); meta_like(out, os.path.join(UI, 'convoy_normandy.png')); print(name)

src('map_europe').resize((1536, 1024), Image.LANCZOS).save(os.path.join(UI, 'map_europe.png')); print('map_europe (meta kept)')
