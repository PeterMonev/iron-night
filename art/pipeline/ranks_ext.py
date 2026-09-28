"""Extends the rank sheet from six cells to eleven: a lieutenant colonel's silver oak leaf (the major's gold one turned
silver) and the generals' one to four silver stars, faceted like pressed metal."""
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
UI = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/UI/'
old = Image.open(UI + 'rank_insignia.png').convert('RGBA'); C = old.height
assert old.width == 6 * C, old.size
sheet = Image.new('RGBA', (11 * C, C), (0, 0, 0, 0)); sheet.alpha_composite(old, (0, 0))

# the lieutenant colonel: the major's oak leaf in silver
leaf = old.crop((5 * C, 0, 6 * C, C)); a = np.asarray(leaf).astype(np.float32)
lum = (0.3 * a[..., 0] + 0.59 * a[..., 1] + 0.11 * a[..., 2]) / 255.0
lum = np.clip((lum - 0.2) * 1.35 + 0.28, 0, 1) ** 0.85
silver = np.stack([lum * 222 + 18, lum * 226 + 18, lum * 234 + 20, a[..., 3]], axis=-1)
sheet.alpha_composite(Image.fromarray(np.clip(silver, 0, 255).astype(np.uint8), 'RGBA'), (6 * C, 0))

def star(size):
    """A five-pointed star of pressed silver: ten facets, lit from the upper left, a dark rim, drawn four times over."""
    k = 4; S = size * k; img = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img); c = S / 2; R = S * 0.48; r = R * 0.4
    pts = [(c + (R if i % 2 == 0 else r) * math.cos(-math.pi / 2 + i * math.pi / 5), c + (R if i % 2 == 0 else r) * math.sin(-math.pi / 2 + i * math.pi / 5)) for i in range(10)]
    d.polygon(pts, fill=(70, 72, 78, 255))                         # the rim
    inner = [(c + (x - c) * 0.9, c + (y - c) * 0.9) for x, y in pts]
    light = (-0.6, -0.8)
    for i in range(10):
        p, q = inner[i], inner[(i + 1) % 10]
        mx, my = (p[0] + q[0]) / 2 - c, (p[1] + q[1]) / 2 - c; n = math.hypot(mx, my) or 1
        shade = 0.5 + 0.5 * ((mx / n) * light[0] + (my / n) * light[1]) * (1 if i % 2 == 0 else -1)
        v = int(120 + 120 * shade); d.polygon([(c, c), p, q], fill=(v, v + 2, v + 8, 255))
    img = img.resize((size, size), Image.LANCZOS)
    glow = img.split()[3].filter(ImageFilter.GaussianBlur(size * 0.04)); halo = Image.new('RGBA', img.size, (0, 0, 0, 0)); halo.putalpha(glow.point(lambda x: int(x * 0.35)))
    out = Image.new('RGBA', img.size, (0, 0, 0, 0)); out.alpha_composite(halo, (0, 2)); out.alpha_composite(img); return out

for n in range(1, 5):   # brigadier, major, lieutenant general, general
    size = {1: 150, 2: 108, 3: 80, 4: 60}[n]; gap = int(size * 0.08); total = n * size + (n - 1) * gap
    x0 = (7 + n - 1) * C + (C - total) // 2; y0 = (C - size) // 2
    s = star(size)
    for i in range(n): sheet.alpha_composite(s, (x0 + i * (size + gap), y0))
sheet.save(UI + 'rank_insignia.png'); print('rank sheet', sheet.size)
prev = Image.new('RGBA', sheet.size, (30, 32, 38, 255)); prev.alpha_composite(sheet); prev.convert('RGB').resize((1408, 128), Image.LANCZOS).save('ranks-ext.png')
