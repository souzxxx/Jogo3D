# Icone do jogo (Windows .exe e itch.io): quadrado escuro arredondado, aro creme, a Quintessencia no centro
# e os quatro elementos em volta, com as cores contidas do jogo (ElementColors). Usa os icones de game-icons.net (CC BY 3.0).
# uv run --with pillow python ArtSource/Icon/make_icon.py
from PIL import Image, ImageDraw, ImageFilter
import os
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ICONS = os.path.join(ROOT, "Assets/Resources/Icons")
N = 1024
def tint(name, color, size):
    g = Image.open(os.path.join(ICONS, name + ".png")).convert("RGBA").resize((size, size), Image.LANCZOS)
    a = g.split()[3]
    out = Image.new("RGBA", (size, size), color + (255,))
    out.putalpha(a)
    return out
img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
d = ImageDraw.Draw(img)
r = 180
d.rounded_rectangle((16, 16, N - 16, N - 16), r, fill=(26, 22, 34, 255))
d.rounded_rectangle((44, 44, N - 44, N - 44), r - 28, outline=(217, 204, 178, 120), width=10)
# brilho quente bem fraco atras do centro
glow = Image.new("RGBA", (N, N), (0, 0, 0, 0))
ImageDraw.Draw(glow).ellipse((262, 262, N - 262, N - 262), fill=(230, 194, 107, 70))
img = Image.alpha_composite(img, glow.filter(ImageFilter.GaussianBlur(70)))
center = tint("quintessence", (236, 214, 160), 400)
img.alpha_composite(center, ((N - 400) // 2, (N - 400) // 2))
cols = {"fire": (219, 97, 46), "water": (69, 133, 189), "earth": (161, 120, 69), "air": (153, 204, 173)}
pos = {"fire": (N // 2, 205), "water": (N - 205, N // 2), "earth": (N // 2, N - 205), "air": (205, N // 2)}
for k, (x, y) in pos.items():
    s = 140
    ImageDraw.Draw(img).ellipse((x - 86, y - 86, x + 86, y + 86), fill=(18, 15, 24, 255), outline=cols[k] + (255,), width=8)
    img.alpha_composite(tint(k, cols[k], s), (x - s // 2, y - s // 2))
out = os.path.join(ROOT, "Assets/Art/Icon")
os.makedirs(out, exist_ok=True)
img.save(os.path.join(out, "QuintessenceIcon.png"))
img.resize((256, 256), Image.LANCZOS).save(os.path.join(ROOT, "docs/img/icone.png"))
print("ok")
