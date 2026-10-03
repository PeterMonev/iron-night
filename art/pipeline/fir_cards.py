"""The four fir cards (rendered by fir_cards_render.py from the TRELLIS spruces along the battle camera's line of sight)
into one atlas, Resources/Textures/fir_cards.png, 2048 x 1024, a card to every 512-pixel column: the yellow the model left
at its very top turned to the dark green of the rest, the colour bled into the clear parts (so the cut-out edge and the
mipmaps keep no dark rim). Prints each card's size and foot for Props.FirCard. Import settings: the hedge leaves'.
Usage: python fir_cards.py <dir with card_0..3.png and cards.json>"""
import os, re, sys, json, uuid
import numpy as np
from PIL import Image
from scipy.ndimage import grey_dilation, binary_dilation

SRC = sys.argv[1]; TEX = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'unity', 'Assets', '_Game', 'Resources', 'Textures')
info = json.load(open(os.path.join(SRC, 'cards.json')))
atlas = np.zeros((1024, 2048, 4), np.float32)
for i, c in enumerate(info):
    im = np.asarray(Image.open(c['png']).convert('RGBA').resize((512, 1024), Image.LANCZOS), np.float32) / 255.0
    rgb, al = im[..., :3], im[..., 3]
    top = np.zeros(al.shape, bool); ys = np.where(al.max(1) > 0.5)[0]; top[: ys.min() + int(0.12 * 1024)] = True   # the top eighth of the tree
    yellow = top & (rgb[..., 0] > rgb[..., 2] + 0.06) & (rgb[..., 1] > rgb[..., 2] + 0.04)
    rgb[yellow] = rgb[yellow] * np.array([0.35, 0.55, 0.4])
    solid = al > 0.3; filled = rgb.copy(); have = solid.copy()
    for _ in range(16):
        grow = binary_dilation(have) & ~have
        if not grow.any(): break
        for ch in range(3): filled[..., ch] = np.where(grow, grey_dilation(np.where(have, filled[..., ch], 0), size=3), filled[..., ch])
        have |= grow
    atlas[:, i * 512:(i + 1) * 512, :3] = np.where(solid[..., None], rgb, filled); atlas[:, i * 512:(i + 1) * 512, 3] = al
Image.fromarray((np.clip(atlas, 0, 1) * 255).astype(np.uint8), 'RGBA').save(os.path.join(TEX, 'fir_cards.png'))
meta = os.path.join(TEX, 'fir_cards.png.meta')
if not os.path.exists(meta):
    t = open(os.path.join(TEX, 'leaves.png.meta'), encoding='utf-8', newline='').read()
    t = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, t, count=1).replace('leaves', 'fir_cards')
    open(meta, 'w', encoding='utf-8', newline='').write(t)
for c in info: print('new Vector4(%.2ff, %.2ff, %.3ff, %.3ff),' % (c['w'], c['h'], c['foot_u'], c['foot_v']))
