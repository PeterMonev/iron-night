# The hangar's new pictures (ChatGPT, art/refs/hangar) made into game textures:
#   hangar_floor(_n).png   the concrete: its edges blended so it tiles without a seam, levelled to the old floor's
#                          brightness, a normal map from its height (oil stains flat, cracks and grit raised)
#   hangar_brick(_n).png   the back wall: its soot evened out row by row (the wall's own grime is laid over its foot in
#                          the game), its seams blended in a narrow band so the joints do not double, levelled a little
#                          above the old brick's brightness; a normal map that sinks the grey joints between red bricks
#   turntable(_n).png      the turntable's diamond plate seen from above, a normal map that lifts the diamonds and rivets
#   door_night.png         the night through the door, cut to the opening's shape (6 x 8.4 m)
#   poster_rolling.png     KEEP 'EM ROLLING for the wall
# New files get the import settings of the hangar's other textures (a plain texture with mipmaps, ASTC 6x6 at 1024 on
# Android; normal maps as normal maps), written before Unity first sees them.
# Run with the pipeline's python (PIL, numpy): python hangar_textures.py
import os, re, uuid
import cv2
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REFS = os.path.join(HERE, '..', 'refs', 'hangar')
TEX = os.path.join(HERE, '..', '..', 'unity', 'Assets', '_Game', 'Resources', 'Textures')
OLD_FLOOR_LUMA = 0.3263   # the mean brightness of the floor the hangar's lamps were set for
BRICK_LUMA = 0.21         # the old brick's was 0.195: a little more light for the new one's detail


def load(name):
    return np.asarray(Image.open(os.path.join(REFS, name + '.png')).convert('RGB'), np.float32) / 255.0


def save(a, name, mode='RGB'):
    Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), mode).save(os.path.join(TEX, name + '.png'))


def seamless(a, band=0.18):
    """Rolled half a tile so the seams cross the middle, then the middle taken back from the unrolled picture under a
    soft cross: the edges now meet themselves."""
    h, w, _ = a.shape; b = np.roll(np.roll(a, h // 2, 0), w // 2, 1)
    yy, xx = np.mgrid[0:h, 0:w]
    dx = np.abs(xx - w / 2) / (w * band); dy = np.abs(yy - h / 2) / (h * band)
    m = np.clip(1.0 - np.minimum(dx, dy), 0, 1) ** 1.5
    return b * (1 - m[..., None]) + a * m[..., None]


def normal_map(height, strength, wrap=True):
    """A tangent-space normal map (OpenGL convention, as Unity's) from a height field in 0..1."""
    if wrap:
        gx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) * 0.5; gy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) * 0.5
    else:
        gx = np.gradient(height, axis=1); gy = np.gradient(height, axis=0)
    n = np.stack([-gx * strength, gy * strength, np.ones_like(height)], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return n * 0.5 + 0.5


def luma(a):
    return a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114


def blur(x, r, wrap=False):
    """A gaussian blur; wrapped round the edges for a tiling texture, so its normal map tiles too."""
    x = x.astype(np.float32)
    if not wrap:
        return cv2.GaussianBlur(x, (0, 0), r, borderType=cv2.BORDER_REFLECT)
    p = int(r * 4) + 1
    return cv2.GaussianBlur(np.pad(x, p, mode='wrap'), (0, 0), r)[p:-p, p:-p]


def floor():
    a = load('hangar_floor2')
    a = np.asarray(Image.fromarray((a * 255).astype(np.uint8)).resize((1024, 1024), Image.LANCZOS), np.float32) / 255.0
    a = seamless(a)
    a = a * (OLD_FLOOR_LUMA / max(1e-3, luma(a).mean()))              # the lamps were set for the old floor's brightness
    save(a, 'hangar_floor')
    h = luma(a); h = h - blur(h, 6, wrap=True)                        # the fine relief only: cracks, grit, the edges of stains
    save(normal_map(np.clip(h * 2.5 + 0.5, 0, 1), 3.0), 'hangar_floor_n')


def brick():
    a = load('hangar_brick2')
    a = np.asarray(Image.fromarray((a * 255).astype(np.uint8)).resize((1024, 1024), Image.LANCZOS), np.float32) / 255.0
    rows = blur(luma(a).mean(1, keepdims=True).repeat(8, 1), 40)[:, :1]   # each row's brightness, smoothed down the wall
    a = a * (luma(a).mean() / np.maximum(rows, 1e-3))[..., None]          # the soot at the foot evened out: it tiles upward
    a = seamless(a, band=0.035)
    a = a * (BRICK_LUMA / max(1e-3, luma(a).mean()))
    save(a, 'hangar_brick')
    red = a[..., 0] - 0.5 * (a[..., 1] + a[..., 2])                        # brick is red, the joints grey
    h = blur(red, 1.2, wrap=True); h = (h - h.min()) / max(1e-3, h.max() - h.min())
    fine = luma(a) - blur(luma(a), 3, wrap=True)                           # the grain of each brick's face
    save(normal_map(np.clip(h * 0.8 + fine * 1.5, 0, 1), 6.0), 'hangar_brick_n')


def turntable():
    a = load('turntable_plate')
    a = np.asarray(Image.fromarray((a * 255).astype(np.uint8)).resize((1024, 1024), Image.LANCZOS), np.float32) / 255.0
    save(a, 'turntable')
    h = luma(a); h = blur(h, 0.8) - blur(h, 5)                        # the diamonds and rivets stand up from the plate
    save(normal_map(np.clip(h * 3 + 0.5, 0, 1), 5.0, wrap=False), 'turntable_n')


def door():
    # the night, and the morning, the day and the evening the hangar shows by the player's clock (GarageTime.cs); the
    # day's and the evening's pictures came with the roof's edge along their top, so theirs is cut off the sky
    for name, top in (('door_night', 0.25), ('door_morning', 0.25), ('door_day', 1.0), ('door_evening', 1.0)):
        a = load(name); h, w, _ = a.shape
        want = int(round(w * 8.4 / 6.0)); cut = h - want                # the opening is 6 m wide, 8.4 m high
        a = a[int(cut * top): h - (cut - int(cut * top))]              # the share of the cut off the sky, the rest off the near concrete
        save(a, name)


def poster():
    save(load('poster_rolling'), 'poster_rolling')


def meta(name, normal=False):
    """The .meta a new texture starts with: the hangar brick's (or its normal map's), under a fresh guid."""
    path = os.path.join(TEX, name + '.png.meta')
    if os.path.exists(path):
        return
    src = os.path.join(TEX, 'hangar_brick_n.png.meta' if normal else 'hangar_brick.png.meta')
    text = open(src, encoding='utf-8', newline='').read()
    text = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, text, count=1)
    text = text.replace('hangar_brick_n', name).replace('hangar_brick', name)
    open(path, 'w', encoding='utf-8', newline='').write(text)


if __name__ == '__main__':
    floor(); brick(); turntable(); door(); poster()
    for n, nm in (('turntable', False), ('turntable_n', True), ('door_night', False), ('door_morning', False), ('door_day', False), ('door_evening', False), ('poster_rolling', False)):
        meta(n, nm)
    print('hangar textures written to', os.path.abspath(TEX))
