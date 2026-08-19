# bake-beastbook.py — THE SPECIMEN HALL texture kit (2026-07-12).
# Bakes the Beastbook stage's museum hardware as PNGs under
# Assets/ui/beastbook/. Everything CSS can't draw in s&box:
#   - field-specimen-v1.png  480x480  the display FIELD behind a
#       discovered beast: dark glass disc + crisp 1px teal ring +
#       sharp dial ticks + base scale ruler. Beastborne material
#       (dais family — modern, luminous), museum only as concept.
#   - case-brackets-v1.png   480x480  empty display case corner
#       brackets + base shelf (the UNDISCOVERED exhibit)
#
# RETIRED same-day (user verdict: "the plate feels not in the style
# of beastborne" — antique/etched/bronze skeuomorphism clashes with
# the dark-slab/teal-hairline dialect): plate-specimen-v1 (etched
# graphite+bronze plate), tag-brass-v1 (brass plaque — replaced by a
# skewed CSS nameplate chip), medal-*-v1 (metallic medal renders —
# replaced by flat two-tone CSS chips). Bake functions kept below,
# unreferenced, as the record of what was tried.
#
# LAWS honored:
#   - 4x SUPERSAMPLE then LANCZOS downscale (the corner-mask lesson:
#     1:1 anti-aliased hairlines read fuzzy; supersampled+downscaled
#     hairlines are engine-crisp).
#   - versioned -v1 filenames (texture-cache law: overwritten textures
#     serve cached pixels to a running game — iterate via -v2, -v3...).
#   - shipped at EXACT display size (no runtime scaling of hairlines).
import math
import os
from PIL import Image, ImageDraw

SS = 4  # supersample factor
OUT = r"c:\Users\jscho\OneDrive\Documents\s&box projects\beastborne\Assets\ui\beastbook"
os.makedirs(OUT, exist_ok=True)


def canvas(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def ship(img, w, h, name):
    out = img.resize((w, h), Image.LANCZOS)
    path = os.path.join(OUT, name)
    out.save(path)
    print("wrote", path)


# ─────────────────────────────────────────────────────────────────────
# 1. THE DISPLAY FIELD — 480x480 display (v2 direction, ON-brand).
# Dark glass disc + crisp thin teal ring + sharp 1px dial ticks +
# base scale ruler. No engraving, no bronze — Beastborne material.
# ─────────────────────────────────────────────────────────────────────
def bake_field():
    W = H = 480
    img = canvas(W, H)
    d = ImageDraw.Draw(img)
    cx = cy = W * SS / 2.0

    def ring(r_disp, w_disp, col):
        r = r_disp * SS
        w = max(1, int(w_disp * SS))
        d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=col, width=w)

    # Dark glass backing disc — one flat slab tone, semi-transparent so
    # the page's drift layer breathes through (glass, not plate). A
    # step lighter than the #0a0912 page so the disc actually reads.
    R = 226 * SS
    d.ellipse([cx - R, cy - R, cx + R, cy + R], fill=(20, 24, 33, 175))
    ring(226, 1.0, (5, 7, 10, 220))            # crisp dark edge

    # The RING — one confident thin teal line + a faint inner echo.
    ring(222, 1.5, (45, 212, 191, 135))
    ring(176, 1.0, (45, 212, 191, 38))

    # Sharp dial ticks between the echo band and the ring — 1px, teal-
    # white, majors every 30 degrees.
    for i in range(72):
        a = math.radians(i * 5.0)
        major = (i % 6 == 0)
        r0 = (208 if major else 213) * SS
        r1 = 219 * SS
        alpha = 130 if major else 55
        x0, y0 = cx + r0 * math.cos(a), cy + r0 * math.sin(a)
        x1, y1 = cx + r1 * math.cos(a), cy + r1 * math.sin(a)
        d.line([x0, y0, x1, y1], fill=(150, 232, 218, alpha), width=max(1, int(1.0 * SS)))

    # Base scale ruler — sharp teal ticks under where the beast stands.
    ry = cy + 156 * SS
    rx0, rx1 = cx - 130 * SS, cx + 130 * SS
    d.line([rx0, ry, rx1, ry], fill=(45, 212, 191, 85), width=max(1, int(1.0 * SS)))
    n_ticks = 12
    for i in range(n_ticks + 1):
        x = rx0 + (rx1 - rx0) * i / n_ticks
        major = (i % 3 == 0)
        h = (9 if major else 5) * SS
        d.line([x, ry, x, ry - h], fill=(150, 232, 218, 95 if major else 50), width=max(1, int(1.0 * SS)))

    ship(img, W, H, "field-specimen-v1.png")


# ─────────────────────────────────────────────────────────────────────
# [RETIRED] SPECIMEN PLATE — the off-brand etched/bronze version.
# ─────────────────────────────────────────────────────────────────────
def bake_plate():
    W = H = 480
    img = canvas(W, H)
    d = ImageDraw.Draw(img)
    cx = cy = W * SS / 2.0
    R = 232 * SS  # plate radius (display 232 → 16px margin all round)

    # Plate body — concentric interpolated fill (center slightly lighter,
    # teal-undertone graphite). 90 steps reads as a smooth radial.
    c_in = (27, 33, 34, 255)    # center  #1b2122
    c_out = (17, 21, 22, 255)   # edge    #111516
    steps = 90
    for i in range(steps):
        t = i / (steps - 1)
        r = R * (1.0 - t)  # draw big→small so small (light) paints last
        col = tuple(int(c_out[k] + (c_in[k] - c_out[k]) * (1.0 - t)) for k in range(3)) + (255,)
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=col)

    def ring(r_disp, w_disp, col):
        r = r_disp * SS
        w = max(1, int(w_disp * SS))
        d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=col, width=w)

    # Outer edge: near-black drop line, then the dark-bronze rim, then a
    # faint inner bronze echo — the mounted-hardware read.
    ring(232, 1.5, (5, 6, 8, 230))
    ring(228, 2.0, (122, 94, 54, 175))     # bronze rim
    ring(224, 1.0, (122, 94, 54, 70))

    # Etched engraving rings — dark groove + light catch (etch = pair).
    for r_disp in (150, 186, 212):
        ring(r_disp, 1.0, (4, 5, 7, 150))                  # groove
        ring(r_disp + 1.5, 1.0, (168, 214, 204, 34))       # light catch (teal-white)

    # Dial scale notches around the outer band (between r 214 and 224).
    for i in range(72):
        a = math.radians(i * 5.0)
        major = (i % 6 == 0)
        r0 = (213 if major else 217) * SS
        r1 = 223 * SS
        alpha = 66 if major else 34
        x0, y0 = cx + r0 * math.cos(a), cy + r0 * math.sin(a)
        x1, y1 = cx + r1 * math.cos(a), cy + r1 * math.sin(a)
        d.line([x0, y0, x1, y1], fill=(176, 218, 208, alpha), width=max(1, int(1.0 * SS)))

    # Base ruler — the specimen-scale strip under where the beast stands.
    ry = cy + 158 * SS
    rx0, rx1 = cx - 132 * SS, cx + 132 * SS
    d.line([rx0, ry, rx1, ry], fill=(150, 190, 182, 60), width=max(1, int(1.0 * SS)))
    n_ticks = 12
    for i in range(n_ticks + 1):
        x = rx0 + (rx1 - rx0) * i / n_ticks
        major = (i % 3 == 0)
        h = (9 if major else 5) * SS
        d.line([x, ry, x, ry - h], fill=(150, 190, 182, 66 if major else 40), width=max(1, int(1.0 * SS)))

    ship(img, W, H, "plate-specimen-v1.png")


# ─────────────────────────────────────────────────────────────────────
# 2. EMPTY DISPLAY CASE BRACKETS — 480x480 display
# ─────────────────────────────────────────────────────────────────────
def bake_case():
    W = H = 480
    img = canvas(W, H)
    d = ImageDraw.Draw(img)
    m = 34 * SS           # outer margin
    arm = 86 * SS         # bracket arm length
    t_out = max(1, int(2.0 * SS))
    t_in = max(1, int(1.0 * SS))
    glass = (45, 212, 191, 105)      # outer bracket — teal glass line
    glass_dim = (45, 212, 191, 42)   # inner echo

    Wp, Hp = W * SS, H * SS
    corners = [
        (m, m, 1, 1),                # top-left    (arms go +x, +y)
        (Wp - m, m, -1, 1),          # top-right
        (m, Hp - m, 1, -1),          # bottom-left
        (Wp - m, Hp - m, -1, -1),    # bottom-right
    ]
    for x, y, sx, sy in corners:
        # outer bracket
        d.line([x, y, x + sx * arm, y], fill=glass, width=t_out)
        d.line([x, y, x, y + sy * arm], fill=glass, width=t_out)
        # inner echo bracket, inset 9px, shorter arms
        i = 9 * SS
        a2 = arm * 0.62
        d.line([x + sx * i, y + sy * i, x + sx * (i + a2), y + sy * i], fill=glass_dim, width=t_in)
        d.line([x + sx * i, y + sy * i, x + sx * i, y + sy * (i + a2)], fill=glass_dim, width=t_in)

    # Base shelf — where the specimen WOULD stand. Slightly warmer line.
    sy_ = Hp - 44 * SS
    d.line([70 * SS, sy_, Wp - 70 * SS, sy_], fill=(45, 212, 191, 64), width=t_out)
    d.line([70 * SS, sy_ + 3 * SS, Wp - 70 * SS, sy_ + 3 * SS], fill=(45, 212, 191, 26), width=t_in)

    ship(img, W, H, "case-brackets-v1.png")


# ─────────────────────────────────────────────────────────────────────
# 3. BRASS REGISTRY TAG — 280x54 display, skew baked into the shape
# ─────────────────────────────────────────────────────────────────────
def bake_tag():
    W, H = 280, 54
    img = canvas(W, H)
    d = ImageDraw.Draw(img)
    sk = math.tan(math.radians(12)) * H * SS  # ≈ 11.5px display

    def para(inset):
        i = inset * SS
        # parallelogram leaning right at the top (skewX(-12°) silhouette)
        return [
            (sk + i, i),
            (W * SS - i, i),
            (W * SS - sk - i, H * SS - i),
            (i, H * SS - i),
        ]

    # Brass body — vertical gradient painted as horizontal slices clipped
    # to the parallelogram via a mask.
    mask = Image.new("L", img.size, 0)
    md = ImageDraw.Draw(mask)
    md.polygon(para(0), fill=255)
    grad = Image.new("RGBA", img.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(grad)
    top = (206, 168, 104)    # lit brass
    mid = (150, 114, 62)
    bot = (96, 70, 38)       # shadowed brass
    for y in range(img.size[1]):
        t = y / (img.size[1] - 1)
        if t < 0.42:
            u = t / 0.42
            col = tuple(int(top[k] + (mid[k] - top[k]) * u) for k in range(3))
        else:
            u = (t - 0.42) / 0.58
            col = tuple(int(mid[k] + (bot[k] - mid[k]) * u) for k in range(3))
        gd.line([0, y, img.size[0], y], fill=col + (255,))
    img.paste(grad, (0, 0), mask)

    # Edges: dark rim + top inner highlight.
    d.polygon(para(0), outline=(52, 36, 18, 255), width=max(1, int(1.5 * SS)))
    d.line([para(0)[0], para(0)[1]], fill=(240, 214, 158, 130), width=max(1, int(1.2 * SS)))

    # Engraved face — inset parallelogram, slightly darker (recessed).
    # alpha_composite (NOT paste-with-mask — paste REPLACES pixels and
    # turned the face near-black on the first bake).
    overlay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(overlay)
    od.polygon(para(7), fill=(40, 28, 12, 46))
    img.alpha_composite(overlay)
    d = ImageDraw.Draw(img)
    d.polygon(para(7), outline=(58, 40, 18, 120), width=max(1, int(1.0 * SS)))

    # Screws — one per end, small dark disc + light catch.
    for fx in (0.075, 0.925):
        # x measured inside the slanted body at mid-height
        x = (sk / 2 + (W * SS - sk / 2 - sk / 2) * fx)
        y = H * SS / 2
        r = 3.4 * SS
        d.ellipse([x - r, y - r, x + r, y + r], fill=(70, 50, 24, 255), outline=(34, 24, 10, 255), width=max(1, int(1.0 * SS)))
        d.line([x - r * 0.55, y - r * 0.55, x + r * 0.55, y + r * 0.55], fill=(228, 198, 140, 190), width=max(1, int(1.0 * SS)))

    ship(img, W, H, "tag-brass-v1.png")


# ─────────────────────────────────────────────────────────────────────
# 4. MASTERY MEDALS — 48x48 display, bronze / silver / gold
# ─────────────────────────────────────────────────────────────────────
def bake_medal(name, light, mid, dark):
    W = H = 48
    img = canvas(W, H)
    d = ImageDraw.Draw(img)
    cx = cy = W * SS / 2.0
    R = 21 * SS

    # Metal body — diagonal light: interpolate rings offset toward top-left.
    steps = 40
    for i in range(steps):
        t = i / (steps - 1)
        r = R * (1.0 - t * 0.96)
        off = (1.0 - t) * 2.2 * SS
        col = tuple(int(dark[k] + (light[k] - dark[k]) * t) for k in range(3)) + (255,)
        d.ellipse([cx - r - off * 0.4, cy - r - off * 0.4, cx + r - off * 0.4 + 0, cy + r - off * 0.4], fill=col)

    # Outer rim (dark), inner engraved ring, center disc.
    d.ellipse([cx - R, cy - R, cx + R, cy + R], outline=(20, 14, 8, 235), width=max(1, int(1.6 * SS)))
    r2 = 15 * SS
    d.ellipse([cx - r2, cy - r2, cx + r2, cy + r2], outline=tuple(dark) + (220,), width=max(1, int(1.3 * SS)))
    r3 = 13.4 * SS
    d.ellipse([cx - r3, cy - r3, cx + r3, cy + r3], fill=tuple(mid) + (255,))

    # Engraved notches between rim and inner ring.
    for i in range(16):
        a = math.radians(i * 22.5)
        r0, r1 = 16.4 * SS, 19.6 * SS
        d.line([cx + r0 * math.cos(a), cy + r0 * math.sin(a),
                cx + r1 * math.cos(a), cy + r1 * math.sin(a)],
               fill=tuple(dark) + (200,), width=max(1, int(1.0 * SS)))

    # Top-left specular catch on the center disc.
    d.arc([cx - r3 * 0.82, cy - r3 * 0.82, cx + r3 * 0.82, cy + r3 * 0.82],
          200, 300, fill=tuple(light) + (200,), width=max(1, int(1.2 * SS)))

    ship(img, W, H, f"medal-{name}-v1.png")


if __name__ == "__main__":
    bake_field()
    bake_case()
    # RETIRED (off-brand skeuomorphism — see banner): bake_plate(),
    # bake_tag(), bake_medal(...). Kept as code for the record.
