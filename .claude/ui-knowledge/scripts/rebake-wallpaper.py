# Root-cause fix for the PawPad screen corners (2026-07-10).
# The shipped wallpaper.webp is a 506x702 bake with the OLD shell graphite
# composited into r30 corners, tuned for the retired leaned phone. Displayed
# at 538x692 the corners stretch and their baked tone mismatches the current
# shell -> every corner artifact the user reported. This rebuilds the TEXTURE:
#   1. decode every animation frame (durations preserved)
#   2. resize field to 538x692 (the exact stretch the engine was applying,
#      so the visible field is pixel-identical to today)
#   3. redraw the corners: composite a supersampled r30 rounded-rect mask
#      against the CURRENT shell gradient tone at each corner's height
#   4. re-encode LOSSLESS animated webp (lossy drops alpha in-engine; corners
#      here are opaque-composited anyway, lossless keeps them crisp)
from PIL import Image, ImageDraw

SRC = r"c:\Users\jscho\OneDrive\Documents\s&box projects\beastborne\Assets\ui\pawpad\wallpaper.webp"
DST = r"c:\Users\jscho\OneDrive\Documents\s&box projects\beastborne\Assets\ui\pawpad\wallpaper-538.webp"
W, H, R, SS = 538, 692, 30, 4  # target size, corner radius, supersample

# Shell gradient #3f434e (top) -> #24262e (bottom) across the shell's ~836px;
# the surface's corners sit at shell-y ~58 (top) and ~750 (bottom).
def shell_tone(y_frac):
    a, b = (0x3F, 0x43, 0x4E), (0x24, 0x26, 0x2E)
    return tuple(round(a[i] + (b[i] - a[i]) * y_frac) for i in range(3))

TONE_TOP = shell_tone(58 / 836)      # surface top corners' height
TONE_BOT = shell_tone(750 / 836)     # surface bottom corners' height

# Supersampled alpha mask of the rounded rect (255 inside, 0 outside)
mask = Image.new("L", (W * SS, H * SS), 0)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, W * SS - 1, H * SS - 1], radius=R * SS, fill=255)
mask = mask.resize((W, H), Image.LANCZOS)

# Corner backdrop: shell tone, top corners vs bottom corners
backdrop = Image.new("RGB", (W, H), TONE_BOT)
bd = ImageDraw.Draw(backdrop)
bd.rectangle([0, 0, W, H // 2], fill=TONE_TOP)  # only corners matter; split coarse

src = Image.open(SRC)
frames, durations = [], []
try:
    i = 0
    while True:
        src.seek(i)
        durations.append(src.info.get("duration", 100))
        f = src.convert("RGB").resize((W, H), Image.LANCZOS)
        out = backdrop.copy()
        out.paste(f, (0, 0), mask)  # field inside the rounded rect, shell tone outside
        frames.append(out)
        i += 1
except EOFError:
    pass

print(f"frames: {len(frames)}, durations[0]: {durations[0] if durations else '?'}ms")
frames[0].save(
    DST, save_all=True, append_images=frames[1:], duration=durations,
    loop=0, lossless=True, method=4,
)
import os
print(f"wrote {DST} ({os.path.getsize(DST)//1024} KB)")
