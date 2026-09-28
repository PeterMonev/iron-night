# The hangar's hunt board and the kill marks, baked from the aces' portraits and drawn shapes:
#   Resources/Textures/hunt_board.png   olive-painted plywood with MOST WANTED stencilled across the head
#   Resources/Textures/hunt_photos.png  the eight aces as sepia prints on cream paper (5 x 2 cells, Surnames order),
#                                       then an unknown man's print with a question mark
#   Resources/Textures/hunt_x.png       a red grease-pencil cross for a dead man's print (alpha: the strokes)
#   Resources/Textures/mark_tiger.png   a Tiger's side view in white, the turret's kill mark (alpha: the shape)
# Run with the pipeline's python (PIL, numpy): python hunt_board.py
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.join(os.path.dirname(__file__), '..', '..', 'unity', 'Assets', '_Game', 'Resources')
UI = os.path.join(ROOT, 'UI'); TEX = os.path.join(ROOT, 'Textures'); FONTS = os.path.join(ROOT, 'Fonts')
SURNAMES = ['keller', 'brandt', 'hoffmann', 'ziegler', 'vogel', 'reinhardt', 'stahl', 'neumann']
rng = np.random.default_rng(1944)


def noise(h, w, scale, octaves=4):
    """Smooth value noise in 0..1, a few octaves summed."""
    out = np.zeros((h, w), np.float32); amp, total = 1.0, 0.0
    for o in range(octaves):
        s = max(1, int(scale / (2 ** o)))
        small = rng.random((h // s + 2, w // s + 2)).astype(np.float32)
        big = np.array(Image.fromarray((small * 255).astype(np.uint8)).resize((w + 2 * s, h + 2 * s), Image.BICUBIC), np.float32) / 255.0
        out += big[s:s + h, s:s + w] * amp; total += amp; amp *= 0.5
    return out / total


def sepia_print(src, w, h):
    """A portrait as an old print: cropped to the frame, levelled (the originals are night scenes), toned warm."""
    im = Image.open(src).convert('RGB'); W, H = im.size
    cw = int(H * w / h); x0 = (W - cw) // 2
    im = im.crop((x0, 0, x0 + cw, H)).resize((w, h), Image.LANCZOS)
    a = np.asarray(im, np.float32) / 255.0
    l = a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114
    lo, hi = np.percentile(l, 2), np.percentile(l, 99.3)
    l = np.clip((l - lo) / max(1e-3, hi - lo), 0, 1) ** 0.78
    l = 0.07 + l * 0.86
    yy, xx = np.mgrid[0:h, 0:w]; d = ((xx - w / 2) / w) ** 2 + ((yy - h * 0.45) / h) ** 2
    l *= np.clip(1.1 - d * 1.9, 0.55, 1.0)                                  # the lens's fall-off
    l += (rng.random((h, w)).astype(np.float32) - 0.5) * 0.05                # grain
    tone = np.stack([l * 1.02 + 0.03, l * 0.9 + 0.02, l * 0.72 + 0.01], -1)
    return np.clip(tone, 0, 1)


def paper(w, h):
    """Cream photographic paper, yellowed toward its edges, a little uneven."""
    base = np.array([0.9, 0.86, 0.76], np.float32)
    yy, xx = np.mgrid[0:h, 0:w]; edge = np.minimum(np.minimum(xx, w - 1 - xx), np.minimum(yy, h - 1 - yy)) / 14.0
    age = 1.0 - 0.1 * np.exp(-edge) - 0.05 * noise(h, w, 24)
    p = base[None, None, :] * age[..., None]
    p[..., 2] -= 0.04 * np.exp(-edge)
    return np.clip(p, 0, 1)


def photos():
    cw, ch = 204, 256; sheet = np.zeros((512, 1024, 3), np.float32); sheet[:] = [0.9, 0.86, 0.76]
    side, top, bottom = 12, 12, 38
    for i in range(10):
        c = paper(cw, ch)
        if i < 8:
            c[top:ch - bottom, side:cw - side] = sepia_print(os.path.join(UI, 'ace_%s.png' % SURNAMES[i]), cw - 2 * side, ch - top - bottom)
        elif i == 8:
            # an unknown man: a dark print, a head and shoulders in shadow, a question mark over it
            w, h = cw - 2 * side, ch - top - bottom
            yy, xx = np.mgrid[0:h, 0:w]; u = (xx - w / 2) / w; v = yy / h
            img = np.zeros((h, w, 3), np.float32); img[:] = [0.2, 0.18, 0.15]
            img *= (0.8 + 0.4 * (1 - v))[..., None]
            head = (u ** 2) / 0.03 + ((v - 0.38) ** 2) / 0.035 < 1; shoulders = (u ** 2) / 0.2 + ((v - 1.05) ** 2) / 0.2 < 1
            img[head | shoulders] = [0.08, 0.07, 0.06]
            q = Image.new('L', (w, h), 0); dr = ImageDraw.Draw(q)
            f = ImageFont.truetype(os.path.join(FONTS, 'BarlowCondensed-Bold.ttf'), 150)
            dr.text((w / 2, h * 0.44), '?', font=f, fill=255, anchor='mm')
            qa = np.asarray(q.filter(ImageFilter.GaussianBlur(0.8)), np.float32)[..., None] / 255.0
            img = img * (1 - qa * 0.75) + np.array([0.82, 0.78, 0.68]) * qa * 0.75
            c[top:ch - bottom, side:cw - side] = img
        col, row = i % 5, i // 5
        sheet[row * ch:(row + 1) * ch, col * cw:(col + 1) * cw] = c
    Image.fromarray((sheet * 255).astype(np.uint8)).save(os.path.join(TEX, 'hunt_photos.png'))


def cross():
    """A cross in red grease pencil: two strokes a little bowed, their edges ragged, heavier where the pencil pressed."""
    S = 1024; a = np.zeros((S, S), np.float32); yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    rough = noise(S, S, 10, 3)
    for (x0, y0, x1, y1, bow) in ((120, 110, 905, 915, 26), (915, 120, 110, 900, -20)):
        dx, dy = x1 - x0, y1 - y0; L = np.hypot(dx, dy); nx, ny = -dy / L, dx / L
        t = np.clip(((xx - x0) * dx + (yy - y0) * dy) / (L * L), 0, 1)
        px = x0 + dx * t + nx * bow * np.sin(np.pi * t); py = y0 + dy * t + ny * bow * np.sin(np.pi * t)
        d = np.hypot(xx - px, yy - py)
        width = 58 * (0.75 + 0.35 * np.sin(np.pi * t) ** 0.6)                   # thinner where the stroke starts and lifts
        a = np.maximum(a, np.clip((width + (rough - 0.5) * 36 - d) / 6, 0, 1))
    img = Image.fromarray((a * 255).astype(np.uint8)).resize((256, 256), Image.LANCZOS)
    al = np.asarray(img, np.float32) / 255.0
    shade = 0.85 + 0.15 * np.asarray(Image.fromarray((noise(S, S, 6, 2) * 255).astype(np.uint8)).resize((256, 256)), np.float32) / 255.0
    rgb = np.stack([0.72 * shade, 0.07 * shade, 0.05 * shade], -1)
    out = np.concatenate([rgb, al[..., None]], -1)
    Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8), 'RGBA').save(os.path.join(TEX, 'hunt_x.png'))


def board():
    """Plywood in olive drab, worn at the edges and where hands went, MOST WANTED stencilled across its head."""
    W, H = 1024, 640; yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    grain = noise(H, W // 8, 3, 3); grain = np.array(Image.fromarray((grain * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC), np.float32) / 255.0
    base = np.array([0.155, 0.16, 0.115], np.float32)
    shade = 0.82 + 0.22 * grain + 0.1 * (noise(H, W, 90) - 0.5)
    edge = np.minimum(np.minimum(xx, W - 1 - xx), np.minimum(yy, H - 1 - yy))
    shade *= 0.78 + 0.22 * np.clip(edge / 90, 0, 1)
    img = base[None, None, :] * shade[..., None]
    wear = (noise(H, W, 18) > 0.7) & (edge < 34)                               # the paint rubbed through to the wood
    img[wear] = img[wear] * 0.75 + np.array([0.26, 0.21, 0.14]) * 0.25
    # the stencil: white paint through a card, the bridges of the letters left in, the paint thin in places
    m = Image.new('L', (W, H), 0); dr = ImageDraw.Draw(m)
    f = ImageFont.truetype(os.path.join(FONTS, 'BarlowCondensed-Bold.ttf'), 104)
    dr.text((W / 2, 70), 'M O S T   W A N T E D', font=f, fill=255, anchor='mm')
    dr.rectangle((W / 2 - 330, 128, W / 2 + 330, 134), fill=255)
    ma = np.asarray(m.filter(ImageFilter.GaussianBlur(1.1)), np.float32) / 255.0
    ma *= np.clip(0.55 + noise(H, W, 8, 3) * 0.7, 0, 1)
    img = img * (1 - ma[..., None]) + np.array([0.78, 0.76, 0.68]) * ma[..., None]
    for _ in range(40):                                                         # pin holes from the files before
        x, y = rng.integers(40, W - 40), rng.integers(170, H - 30); r = rng.uniform(1.2, 2.2)
        img[(xx - x) ** 2 + (yy - y) ** 2 < r * r] *= 0.35
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(os.path.join(TEX, 'hunt_board.png'))


def tiger():
    """A Tiger I side on, facing right, as a stencil: tracks, hull, turret with its cupola, the long 88 and its brake."""
    k = 4; mpx = 58 * k; W, H = 512 * k, 200 * k
    im = Image.new('L', (W, H), 0); dr = ImageDraw.Draw(im)
    def P(x, y): return (8 * k + x * mpx, H - 10 * k - y * mpx)
    def box(x0, y0, x1, y1): dr.polygon([P(x0, y0), P(x1, y0), P(x1, y1), P(x0, y1)], fill=255)
    r = 0.46 * mpx; a, b = P(0.15, 0.0), P(6.2, 1.0)
    dr.rounded_rectangle((a[0], b[1], b[0], a[1]), radius=r, fill=255)            # the running gear
    dr.polygon([P(0.0, 0.85), P(5.85, 0.85), P(6.3, 1.25), P(6.3, 1.95), P(0.0, 1.95)], fill=255)   # the hull
    dr.polygon([P(2.0, 1.95), P(4.6, 1.95), P(4.6, 2.85), P(2.25, 2.85), P(2.0, 2.6)], fill=255)    # the turret
    box(4.55, 2.05, 4.95, 2.78)                                                   # the mantlet
    box(1.62, 2.12, 2.05, 2.62)                                                   # the stowage bin
    dr.rounded_rectangle((P(2.25, 3.08)[0], P(2.25, 3.08)[1], P(2.85, 2.8)[0], P(2.85, 2.8)[1]), radius=0.08 * mpx, fill=255)   # the cupola
    dr.polygon([P(4.9, 2.27), P(8.2, 2.31), P(8.2, 2.53), P(4.9, 2.58)], fill=255)  # the 88, drawn heavier than it is: a stencil has to read at a glance
    box(8.12, 2.2, 8.52, 2.64)                                                     # its muzzle brake
    small = im.resize((256, 100), Image.LANCZOS)
    al = np.asarray(small, np.float32) / 255.0
    out = np.concatenate([np.ones((100, 256, 3), np.float32), al[..., None]], -1)
    Image.fromarray((out * 255).astype(np.uint8), 'RGBA').save(os.path.join(TEX, 'mark_tiger.png'))


if __name__ == '__main__':
    photos(); cross(); board(); tiger()
    print('hunt board, photos, cross and the Tiger mark written to', os.path.abspath(TEX))
