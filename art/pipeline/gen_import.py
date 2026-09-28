"""Brings Petar's ChatGPT pictures into the game: the three currency icons trimmed to their outline, the officers'
insignia into the rank sheet, the launcher icon from its sky and its tank. Run once on 2026-09-28: the rank step
expects the old sheet (the enlisted chevrons in cells 0-2, the captain's bars in cell 5 as the measure of size)."""
import numpy as np
from PIL import Image
D = 'D:/Codes/Projects/lightswarm/art/refs/batch4/'   # ChatGPT's pictures, as Petar made them
UI = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/UI/'
IC = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Icon/'

def bbox(im, thr=8):
    a = np.asarray(im.convert('RGBA'))[..., 3]; ys, xs = np.where(a > thr)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1

def trimmed(im, pad=0.03):
    x0, y0, x1, y1 = bbox(im); p = int(max(x1 - x0, y1 - y0) * pad)
    return im.crop((max(0, x0 - p), max(0, y0 - p), min(im.width, x1 + p), min(im.height, y1 + p)))

# the currencies: trimmed, 320 px on the long side
for f in ['icon_points', 'icon_crewxp', 'icon_gold']:
    im = trimmed(Image.open(D + f + '.png').convert('RGBA')); s = 320 / max(im.size)
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS); im.save(UI + f + '.png'); print(f, im.size)

# the rank sheet: the enlisted chevrons stay (cells 0-2), the officers' pins take cells 3-5 (first lieutenant, captain,
# major), each as tall as the old captain's bars stood in their cell
old = Image.open(UI + 'rank_insignia.png').convert('RGBA'); cell = old.height
ref = old.crop((5 * cell, 0, 6 * cell, cell)); rx0, ry0, rx1, ry1 = bbox(ref); ref_h = ry1 - ry0; print('old captain bars height', ref_h, 'of', cell)
off = Image.open(D + 'rank_officers.png').convert('RGBA'); a = np.asarray(off)[..., 3]
cols = (a > 8).any(axis=0); runs = []; start = None
for x, on in enumerate(cols):
    if on and start is None: start = x
    if not on and start is not None: runs.append((start, x)); start = None
if start is not None: runs.append((start, len(cols)))
runs = [r for r in runs if r[1] - r[0] > 20]
print('pieces', runs)
# the captain's two bars are joined, so there should be three pieces; if the gap between the captain's bars shows,
# merge the two closest middle runs
while len(runs) > 3:
    gaps = [(runs[i + 1][0] - runs[i][1], i) for i in range(len(runs) - 1)]; g, i = min(gaps); runs[i:i + 2] = [(runs[i][0], runs[i + 1][1])]
sheet = Image.new('RGBA', old.size, (0, 0, 0, 0)); sheet.alpha_composite(old.crop((0, 0, 3 * cell, cell)), (0, 0))
for k, (x0, x1) in enumerate(runs):
    piece = trimmed(off.crop((x0, 0, x1, off.height)), pad=0.0)
    s = (ref_h * (1.0 if k < 2 else 1.08)) / piece.height; piece = piece.resize((max(1, round(piece.width * s)), max(1, round(piece.height * s))), Image.LANCZOS)
    cx = (3 + k) * cell + (cell - piece.width) // 2; cy = (cell - piece.height) // 2
    sheet.alpha_composite(piece, (cx, cy))
sheet.save(UI + 'rank_insignia.png'); print('rank sheet', sheet.size)

# the launcher icon: the sky, the tank on its own layer inside the adaptive icon's safe circle, and the two together
bg = Image.open(D + 'icon_bg.png').convert('RGBA').resize((1024, 1024), Image.LANCZOS)
tank = trimmed(Image.open(D + 'icon_fg.png').convert('RGBA'), pad=0.0)
def placed(width_frac, drop):
    s = 1024 * width_frac / tank.width; t = tank.resize((round(tank.width * s), round(tank.height * s)), Image.LANCZOS)
    layer = Image.new('RGBA', (1024, 1024), (0, 0, 0, 0)); layer.alpha_composite(t, ((1024 - t.width) // 2, (1024 - t.height) // 2 + drop)); return layer
bg.save(IC + 'icon_bg.png')
placed(0.52, 16).save(IC + 'icon_fg.png')
full = bg.copy(); full.alpha_composite(placed(0.78, 60)); full.convert('RGB').save(IC + 'icon.png')
print('icons done')
