# The small IRON NIGHT stamped in the corner of every photo: Cinzel Bold in spaced capitals, white, over a soft dark
# glow so it reads on fire and on snow. Drawn at 4x, shrunk to 640 wide; saved as PNG bytes (Resources/UI/photo_mark.bytes)
# so the game reads it straight, whatever the texture diet does to pictures.
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import sys

FONT = r'D:\Codes\Projects\lightswarm\unity\Assets\_Game\Resources\Fonts\Cinzel-Bold.ttf'
OUT = sys.argv[1] if len(sys.argv) > 1 else 'photo_mark.png'
S = 4
text = 'IRON NIGHT'
font = ImageFont.truetype(FONT, 88 * S)
track = 0.22 * 88 * S   # letter spacing

widths = [font.getlength(ch) for ch in text]
total = sum(widths) + track * (len(text) - 1)
asc, desc = font.getmetrics()
pad = 30 * S
W, H = int(total + pad * 2), int(asc + desc * 0.3 + pad * 2)

glyphs = Image.new('L', (W, H), 0)
d = ImageDraw.Draw(glyphs)
x = pad
for ch, w in zip(text, widths):
    d.text((x, pad), ch, font=font, fill=255)
    x += w + track

# the glow: the letters thickened and blurred, dark, under the white
glow = glyphs.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(14 * S / 2))
out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
shade = Image.new('RGBA', (W, H), (0, 0, 0, 255))
shade.putalpha(glow.point(lambda v: int(v * 0.55)))
out = Image.alpha_composite(out, shade)
white = Image.new('RGBA', (W, H), (255, 255, 255, 255))
white.putalpha(glyphs)
out = Image.alpha_composite(out, white)

bbox = out.getbbox()
out = out.crop(bbox)
w = 640
out = out.resize((w, round(out.height * w / out.width)), Image.LANCZOS)
out.save(OUT)
print(OUT, out.size)
