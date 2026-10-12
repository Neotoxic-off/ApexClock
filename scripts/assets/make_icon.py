import sys
from PIL import Image, ImageDraw
out = sys.argv[1]
S = 1024
K = 2                                   # supersample factor, downscaled at the end for clean edges

NAVY = (0x0E, 0x2A, 0x45, 255)
DEEP = (0x15, 0x7F, 0xC4, 255)
ICE = (0x4C, 0xC2, 0xFF, 255)           # app accent
GEM = (255, 255, 255, 255)
GEM_LIGHT = (0xD8, 0xF1, 0xFF, 255)
GEM_SHADE = (0xB5, 0xE3, 0xFF, 255)

big = Image.new("RGBA", (S * K, S * K), (0, 0, 0, 0))
d = ImageDraw.Draw(big)

def poly(points, fill):
    # Points are given on a 256 grid.
    scale = S * K / 256
    d.polygon([(x * scale, y * scale) for x, y in points], fill=fill)

# Ranked-style emblem: angular shield with a navy rim, side wings and a diamond gem
poly([(128, 8), (230, 52), (214, 170), (128, 248), (42, 170), (26, 52)], NAVY)
poly([(128, 24), (214, 62), (200, 162), (128, 230), (56, 162), (42, 62)], DEEP)
poly([(42, 62), (100, 96), (84, 150), (56, 162)], ICE)
poly([(214, 62), (156, 96), (172, 150), (200, 162)], ICE)
poly([(128, 66), (170, 116), (128, 196), (86, 116)], GEM)
poly([(128, 66), (170, 116), (128, 124)], GEM_LIGHT)
poly([(86, 116), (128, 124), (128, 196)], GEM_SHADE)

icon = big.resize((S, S), Image.LANCZOS)
icon.resize((256, 256), Image.LANCZOS).save(f"{out}/AppIcon.png")
icon.save(f"{out}/AppIcon.ico", sizes=[(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48), (64, 64), (128, 128), (256, 256)])

# Preview sheet (preview.png): 128, 64, 32, 16 on dark and light backgrounds, plus 256
sheet = Image.new("RGBA", (720, 300), (32, 32, 36, 255))
ImageDraw.Draw(sheet).rectangle([0, 150, 720, 300], fill=(243, 243, 243, 255))
x = 10
for s in (128, 64, 32, 16):
    im = icon.resize((s, s), Image.LANCZOS)
    sheet.paste(im, (x, 10), im); sheet.paste(im, (x, 160), im)
    x += s + 40
preview = icon.resize((256, 256), Image.LANCZOS)
sheet.paste(preview, (450, 22), preview)
if len(sys.argv) > 2:
    sheet.save(sys.argv[2])
print("ok")
