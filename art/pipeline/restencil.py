"""The new officer's crate's markings, made right: the garbled lid text and the two four-pointed stars painted off the
wood (filled along the grain), then a five-pointed white star on each long side, its top point to world up on that
face, and U.S. ARMY on the lid, reading from the front. The paint lets a little of the grain through."""
import sys, math
import numpy as np, cv2
from PIL import Image, ImageDraw, ImageFont
src, out = sys.argv[1], sys.argv[2]
rgb = np.asarray(Image.open(src).convert('RGB')).astype(np.float32); H, W = rgb.shape[:2]
detail = rgb - cv2.GaussianBlur(rgb, (0, 0), 3.0)

def fill(x0, x1, y0, y1, along, feather=12, src_off=(0, 0), src_at=None, turn=False):
    """Paint a box off: each line along the grain runs from the wood on one side to the wood on the other."""
    s = 12
    if along == 'x':
        a = rgb[y0:y1, x0 - s:x0].mean(axis=1); b = rgb[y0:y1, x1:x1 + s].mean(axis=1)
        t = np.linspace(0, 1, x1 - x0)[None, :, None]; f = a[:, None, :] * (1 - t) + b[:, None, :] * t
    else:
        a = rgb[y0 - s:y0, x0:x1].mean(axis=0); b = rgb[y1:y1 + s, x0:x1].mean(axis=0)
        t = np.linspace(0, 1, y1 - y0)[:, None, None]; f = a[None, :, :] * (1 - t) + b[None, :, :] * t
    if src_at is None: dx, dy = src_off; f = f + detail[y0 + dy:y1 + dy, x0 + dx:x1 + dx]
    else:   # the grain of a clean panel, turned when this face's grain runs the other way
        sx, sy = src_at; hh, ww = (x1 - x0, y1 - y0) if turn else (y1 - y0, x1 - x0); d = detail[sy:sy + hh, sx:sx + ww]; f = f + (d.transpose(1, 0, 2) if turn else d)
    m = np.zeros((H, W), np.float32); m[y0 + feather:y1 - feather, x0 + feather:x1 - feather] = 1
    m = cv2.GaussianBlur(m, (0, 0), feather / 2)[y0:y1, x0:x1, None]
    rgb[y0:y1, x0:x1] = rgb[y0:y1, x0:x1] * (1 - m) + f * m

def paint(mask):
    """Off-white stencil paint where the mask is, with the wood's grain faintly through it."""
    lum = cv2.cvtColor(np.clip(rgb, 0, 255).astype(np.uint8), cv2.COLOR_RGB2GRAY).astype(np.float32) / 255
    rng = np.random.default_rng(7); noise = cv2.GaussianBlur(rng.random((H, W)).astype(np.float32), (0, 0), 2.0)
    a = mask * np.clip(0.8 + 0.35 * (lum - lum.mean()) + 0.25 * (noise - 0.5), 0.55, 0.95)
    paint_col = np.array([236, 233, 222], np.float32)
    rgb[:] = rgb * (1 - a[..., None]) + paint_col * a[..., None]

def star_mask(cx, cy, r, up):
    """A five-pointed star, its top point along `up` (a direction on the image), drawn four times over and scaled down."""
    k = 4; img = Image.new('L', (W * 1, H * 1), 0)
    big = Image.new('L', (int(r * 2.4) * k, int(r * 2.4) * k), 0); d = ImageDraw.Draw(big); c = big.width / 2
    ang0 = math.atan2(up[1], up[0]); pts = []
    for i in range(10):
        rr = r * k if i % 2 == 0 else r * k * 0.382; a = ang0 + i * math.pi / 5
        pts.append((c + rr * math.cos(a), c + rr * math.sin(a)))
    d.polygon(pts, fill=255); small = big.resize((big.width // k, big.height // k), Image.LANCZOS)
    img.paste(small, (int(cx - small.width / 2), int(cy - small.height / 2)))
    return np.asarray(img).astype(np.float32) / 255

def text_mask(cx, cy, text, height):
    font = ImageFont.truetype('D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Fonts/BarlowCondensed-Bold.ttf', int(height * 1.38))
    img = Image.new('L', (W, H), 0); d = ImageDraw.Draw(img); bb = d.textbbox((0, 0), text, font=font)
    d.text((cx - (bb[0] + bb[2]) / 2, cy - (bb[1] + bb[3]) / 2), text, font=font, fill=255)
    return cv2.GaussianBlur(np.asarray(img).astype(np.float32) / 255, (0, 0), 0.8)

S = W / 768.0   # the boxes were read off a 768 px view
fill(int(184 * S), int(446 * S), int(308 * S), int(358 * S), 'x', src_off=(0, int(-120 * S)))   # the garbled lid text
fill(int(198 * S), int(314 * S), int(438 * S), int(578 * S), 'x', src_at=(int(198 * S), int(40 * S)))  # the front's four-pointed star
fill(int(578 * S), int(702 * S), int(238 * S), int(362 * S), 'y', src_at=(int(60 * S), int(40 * S)), turn=True)  # the back's
paint(star_mask(255 * S, 510 * S, 44 * S, (0, -1)))    # the front: world up is up the image
paint(star_mask(640 * S, 300 * S, 44 * S, (1, 0)))     # the back: world up is to the image's right
paint(text_mask(318 * S, 333 * S, 'U.S. ARMY', 34 * S))
Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8), 'RGB').save(out); print('saved', out)
