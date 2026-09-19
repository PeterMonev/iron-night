"""Shifts a vehicle texture's mean colour to a target while keeping its shading and detail (the generator paints what the
reference shows: the Soviet tanks came out mint, the standalone Sherman turret sand). Runs after vehicle_export.py.
python tone.py <name> <r> <g> <b> [contrast=1.0]"""
import sys
import numpy as np
from PIL import Image
M = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Models/'
name = sys.argv[1]; target = np.array([float(x) for x in sys.argv[2:5]]); k = float(sys.argv[5]) if len(sys.argv) > 5 else 1.0
img = Image.open(M + name + '.png').convert('RGB'); a = np.asarray(img).astype(float)
mean = a.reshape(-1, 3).mean(axis=0); out = np.clip((a - mean) * k + target, 0, 255).astype(np.uint8)
Image.fromarray(out).save(M + name + '.png'); print(f'{name}: mean {mean.round(0)} -> {target}')
