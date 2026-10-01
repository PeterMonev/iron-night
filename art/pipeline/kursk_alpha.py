# The Kursk lane and yard decals came without an alpha channel, so on the steppe the farmyards and searchlight posts
# sat on hard-edged dark squares and the lanes on hard strips. They take the soft edges of the Normandy ones (yard.png,
# lane.png: the same size), the colour staying their own.
# Run with the pipeline's python (PIL): python kursk_alpha.py
import os
from PIL import Image

TEX = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'unity', 'Assets', '_Game', 'Resources', 'Textures')
for name in ('yard', 'lane'):
    edge = Image.open(os.path.join(TEX, name + '.png')).convert('RGBA').getchannel('A')
    path = os.path.join(TEX, name + '_kursk.png'); img = Image.open(path).convert('RGB')
    if edge.size != img.size: edge = edge.resize(img.size, Image.LANCZOS)
    img.putalpha(edge); img.save(path)
    print(name + '_kursk.png: soft edges from', name + '.png')
