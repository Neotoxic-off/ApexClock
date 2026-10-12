import math, sys
from PIL import Image, ImageDraw, ImageFilter
out = sys.argv[1]
S = 1024

def vgrad(size, top, bot):
    g = Image.new("RGBA", size)
    d = ImageDraw.Draw(g)
    for y in range(size[1]):
        t = y / (size[1] - 1)
        d.line([(0, y), (size[0], y)], fill=tuple(int(top[i] + (bot[i] - top[i]) * t) for i in range(3)) + (255,))
    return g

# Dark rounded tile with a faint top highlight edge
tile_mask = Image.new("L", (S, S), 0)
ImageDraw.Draw(tile_mask).rounded_rectangle([32, 32, S - 32, S - 32], radius=228, fill=255)
icon = Image.new("RGBA", (S, S), (0, 0, 0, 0))
icon.paste(vgrad((S, S), (0x2A, 0x2C, 0x33), (0x0D, 0x0E, 0x11)), (0, 0), tile_mask)
edge = Image.new("L", (S, S), 0)
ImageDraw.Draw(edge).rounded_rectangle([32, 32, S - 32, S - 32], radius=228, outline=255, width=6)
icon.paste(Image.new("RGBA", (S, S), (255, 255, 255, 38)), (0, 0), edge)

cx, cy, r, w = S // 2, S // 2 + 6, 290, 82
start, end = -50, 245          # arc degrees, PIL: 0 = 3 o'clock, clockwise

# Red glow behind the arc
glow = Image.new("L", (S, S), 0)
ImageDraw.Draw(glow).arc([cx - r, cy - r, cx + r, cy + r], start, end, fill=255, width=w + 8)
glow = glow.filter(ImageFilter.GaussianBlur(20))
icon.paste(Image.new("RGBA", (S, S), (0xE5, 0x2D, 0x34, 85)), (0, 0), glow)

# Arc + arrowhead in a red gradient
arc_mask = Image.new("L", (S, S), 0)
md = ImageDraw.Draw(arc_mask)
md.arc([cx - r, cy - r, cx + r, cy + r], start, end, fill=255, width=w)
a = math.radians(end)
tip_dir = a + math.pi / 2                 # tangent, clockwise
px, py = cx + r * math.cos(a), cy + r * math.sin(a)
nx, ny = math.cos(a), math.sin(a)         # outward normal
tx, ty = math.cos(tip_dir), math.sin(tip_dir)
head = 104
md.polygon([
    (px + nx * head, py + ny * head),
    (px - nx * head, py - ny * head),
    (px + tx * head * 1.25, py + ty * head * 1.25),
], fill=255)
# round the tail end of the arc
b = math.radians(start)
bx, by = cx + r * math.cos(b) - (r - (r - w / 2)) * 0, cy + r * math.sin(b)
rx, ry = cx + (r - w / 2) * math.cos(b), cy + (r - w / 2) * math.sin(b)
md.ellipse([rx - w / 2, ry - w / 2, rx + w / 2, ry + w / 2], fill=255)
icon.paste(vgrad((S, S), (0xFF, 0x5A, 0x4E), (0xC4, 0x12, 0x1E)), (0, 0), arc_mask)

# White clock hands with rounded caps + red hub
d = ImageDraw.Draw(icon)
def hand(deg, length, width):
    ang = math.radians(deg - 90)
    x, y = cx + length * math.cos(ang), cy + length * math.sin(ang)
    d.line([(cx, cy), (x, y)], fill="white", width=width)
    d.ellipse([x - width / 2, y - width / 2, x + width / 2, y + width / 2], fill="white")
hand(0, 168, 64)
hand(300, 118, 64)
d.ellipse([cx - 50, cy - 50, cx + 50, cy + 50], fill="white")
d.ellipse([cx - 22, cy - 22, cx + 22, cy + 22], fill=(0xE5, 0x2D, 0x34, 255))

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
