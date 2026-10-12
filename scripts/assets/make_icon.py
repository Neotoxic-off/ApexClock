import math, sys
from PIL import Image, ImageDraw, ImageFilter, ImageChops
out = sys.argv[1]
S = 1024
K = 2                                   # supersample factor, downscaled at the end for clean edges
N = S * K

TINT = (0x4C, 0xC2, 0xFF)               # ice blue
TINT_LIGHT = tuple(min(255, c + 110) for c in TINT)

def vgrad(top, bot):
    g = Image.new("RGBA", (N, N))
    d = ImageDraw.Draw(g)
    for y in range(N):
        t = y / (N - 1)
        d.line([(0, y), (N, y)], fill=tuple(int(top[i] + (bot[i] - top[i]) * t) for i in range(4)))
    return g

def solid(c):
    return Image.new("RGBA", (N, N), c)

def layer(base, color, mask):
    l = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    l.paste(color, (0, 0), mask)
    return Image.alpha_composite(base, l)

def rounded(inset, radius, width=0):
    m = Image.new("L", (N, N), 0)
    box = [inset * K, inset * K, N - inset * K, N - inset * K]
    if width:
        ImageDraw.Draw(m).rounded_rectangle(box, radius=radius * K, outline=255, width=width * K)
    else:
        ImageDraw.Draw(m).rounded_rectangle(box, radius=radius * K, fill=255)
    return m

def blob(box, blur, clip):
    m = Image.new("L", (N, N), 0)
    ImageDraw.Draw(m).ellipse([v * K for v in box], fill=255)
    return ImageChops.multiply(m.filter(ImageFilter.GaussianBlur(blur * K)), clip)

# Dark glass tile, tinted light bleeding in bottom-right, soft white bloom top-left
tile = rounded(40, 210)
icon = layer(Image.new("RGBA", (N, N), (0, 0, 0, 0)), vgrad((0x3A, 0x3E, 0x48, 255), (0x12, 0x14, 0x18, 255)), tile)
icon = layer(icon, solid(TINT + (110,)), blob([500, 520, 1100, 1120], 160, tile))
icon = layer(icon, solid((255, 255, 255, 40)), blob([-200, -200, 450, 450], 160, tile))

# Top sheen + rim light
sheen = Image.new("L", (N, N), 0)
ImageDraw.Draw(sheen).ellipse([-300 * K, -900 * K, 1300 * K, 470 * K], fill=255)
sheen = ImageChops.multiply(sheen.filter(ImageFilter.GaussianBlur(6 * K)), tile)
icon = layer(icon, vgrad((255, 255, 255, 46), (255, 255, 255, 6)), sheen)
icon = layer(icon, vgrad((255, 255, 255, 150), (255, 255, 255, 25)), rounded(40, 210, width=6))

# Countdown ring: frosted track + glowing progress arc (PIL: 0 = 3 o'clock, clockwise)
cx, cy, r, w = 512, 520, 290, 70
ring_box = [(cx - r) * K, (cy - r) * K, (cx + r) * K, (cy + r) * K]
track = Image.new("L", (N, N), 0)
ImageDraw.Draw(track).arc(ring_box, 0, 360, fill=255, width=w * K)
icon = layer(icon, solid((255, 255, 255, 34)), track)

arc = Image.new("L", (N, N), 0)
ad = ImageDraw.Draw(arc)
ad.arc(ring_box, -90, 150, fill=255, width=w * K)
for deg in (-90, 150):                  # round caps
    a = math.radians(deg)
    px, py = cx + (r - w / 2) * math.cos(a), cy + (r - w / 2) * math.sin(a)
    ad.ellipse([(px - w / 2) * K, (py - w / 2) * K, (px + w / 2) * K, (py + w / 2) * K], fill=255)
icon = layer(icon, solid(TINT + (170,)), arc.filter(ImageFilter.GaussianBlur(28 * K)))
icon = layer(icon, vgrad(TINT_LIGHT + (255,), TINT + (255,)), arc)

spec = Image.new("L", (N, N), 0)
ImageDraw.Draw(spec).arc([ring_box[0] + 14 * K, ring_box[1] + 14 * K, ring_box[2] - 14 * K, ring_box[3] - 14 * K],
                         -80, 20, fill=255, width=10 * K)
icon = layer(icon, solid((255, 255, 255, 120)), spec.filter(ImageFilter.GaussianBlur(3 * K)))

# Clock hands with rounded caps and a soft drop shadow
hands = Image.new("L", (N, N), 0)
hd = ImageDraw.Draw(hands)
def hand(deg, length, width):
    a = math.radians(deg - 90)
    x, y = cx + length * math.cos(a), cy + length * math.sin(a)
    hd.line([cx * K, cy * K, x * K, y * K], fill=255, width=width * K)
    for px, py in ((cx, cy), (x, y)):
        hd.ellipse([(px - width / 2) * K, (py - width / 2) * K, (px + width / 2) * K, (py + width / 2) * K], fill=255)
hand(0, 165, 58)
hand(240, 115, 58)
icon = layer(icon, solid((0, 0, 0, 120)), ImageChops.offset(hands.filter(ImageFilter.GaussianBlur(14 * K)), 0, 10 * K))
icon = layer(icon, vgrad((255, 255, 255, 255), (0xD8, 0xDE, 0xE8, 255)), hands)

icon = icon.resize((S, S), Image.LANCZOS)
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
big = icon.resize((256, 256), Image.LANCZOS)
sheet.paste(big, (450, 22), big)
if len(sys.argv) > 2:
    sheet.save(sys.argv[2])
print("ok")
