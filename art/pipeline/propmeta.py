# A new prop's texture takes the import settings the other props' have (Props/crate_tex: a plain texture with mipmaps,
# ASTC 6x6 at 1024 on Android) instead of the 2D sprite Unity gives a new PNG in this project. Its guid is kept when it
# already has one. Run before or after Unity first imports the prop:  python propmeta.py mechanic weldcart ...
import os, re, sys, uuid
PROPS = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'unity', 'Assets', '_Game', 'Resources', 'Props')
template = open(os.path.join(PROPS, 'crate_tex.png.meta'), encoding='utf-8', newline='').read()
for name in sys.argv[1:]:
    path = os.path.join(PROPS, name + '_tex.png.meta')
    guid = uuid.uuid4().hex
    if os.path.exists(path):
        m = re.search(r'guid: ([0-9a-f]{32})', open(path, encoding='utf-8').read())
        if m: guid = m.group(1)
    text = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + guid, template, count=1).replace('crate_tex', name + '_tex')
    open(path, 'w', encoding='utf-8', newline='').write(text)
    print(name + '_tex.png.meta', 'guid', guid)
