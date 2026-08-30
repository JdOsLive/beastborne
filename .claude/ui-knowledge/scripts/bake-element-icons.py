# bake-element-icons.py — the ELEMENT ICONS from the design system card
# (Claude Design "Beastborne UI" → components/element-icons.html, 2026-08-30)
# baked into the three SVG sets the game already consumes by filename:
#   background/<el>.svg     BADGE  — dark Fill, bright Rim border, glyph in Rim (the card's tile badge)
#   element_white/<el>.svg  INK    — bare glyph in parchment #f4f1ea (on dark / colored surfaces)
#   transparent/<el>.svg    BARE   — bare glyph in the Rim color on nothing (HUD overlays)
# Colors = BbTokens.Element v1 (Code/UI/BbTokens.cs) — identical to the card.
# Glyphs are 24×24. Wind / Ice / Metal are LINE glyphs (stroke 3); the rest are
# filled silhouettes with a .6 self-stroke. Nature's vein is the ONE sanctioned
# second color: the element's dark tone (knocked to the surface tone on ink).
import os
OUT = r"c:\users\jscho\documents\s&box projects\megarougelite\Assets\ui\icons\elements"
VEIN = "#14532d"  # nature's dark tone as written in the card's path
EL = [
    ("neutral",  "#44403c", "#a8a29e", '<circle cx="12" cy="12" r="8"/>'),
    ("fire",     "#7c2410", "#ff6a3d", '<path d="M8.5 14.5A2.5 2.5 0 0 0 11 12c0-1.38-.5-2-1-3-1.072-2.143-.224-4.054 2-6 .5 2.5 2 4.9 4 6.5 2 1.6 3 3.5 3 5.5a7 7 0 1 1-14 0c0-1.153.433-2.294 1-3a2.5 2.5 0 0 0 2.5 2.5z"/>'),
    ("water",    "#1e3a8a", "#4aa8ff", '<path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z"/>'),
    ("nature",   "#14532d", "#4ade80", '<path d="M11 20A7 7 0 0 1 9.8 6.1C15.5 5 17 4.48 19 2c1 2 2 4.18 2 8 0 5.5-4.78 10-10 10z"/><path d="M4 21c1.5-5.5 5-9.5 10-12" fill="none" stroke="__VEIN__" stroke-width="1.7" stroke-linecap="round"/>'),
    ("wind",     "#0f766e", "#2dd4bf", '<path d="M17.7 7.7a2.5 2.5 0 1 1 1.8 4.3H2" fill="none"/><path d="M9.6 4.6A2 2 0 1 1 11 8H2" fill="none"/><path d="M12.6 19.4A2 2 0 1 0 14 16H2" fill="none"/>'),
    ("electric", "#806c00", "#f7e024", '<path d="M13 2 3 14h9l-1 8 10-12h-9l1-8z"/>'),
    ("earth",    "#6b3f14", "#d9a054", '<path d="M9 5.5 13.6 12.8 16 9.4 22 19.5H2z"/>'),
    ("ice",      "#155e75", "#9fe8ff", '<path d="M12 3v18M4.2 7.5l15.6 9M19.8 7.5l-15.6 9" fill="none"/>'),
    ("metal",    "#3f4b5c", "#c3cdd9", '<path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" fill="none"/>'),
    ("spirit",   "#86185d", "#ff6bd6", '<path d="M12 3l2.5 6.5L21 12l-6.5 2.5L12 21l-2.5-6.5L3 12l6.5-2.5z"/>'),
    ("shadow",   "#4a1580", "#c26bff", '<path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z"/>'),
]
LINE = {"wind", "ice", "metal"}

def glyph(name, body, color, vein):
    sw = "3" if name in LINE else ".6"
    body = body.replace("__VEIN__", vein)
    return f'<g fill="{color}" stroke="{color}" stroke-width="{sw}" stroke-linecap="round" stroke-linejoin="round">{body}</g>'

def svg(inner, w=24):
    # width/height = the RASTER hint: s&box rasterizes an SVG at its declared size and
    # stretches it, so 24 made every badge a mushy 24px texture (roster cards looked
    # "low quality", 2026-08-30). 256 gives the 14px chips and 72px heroes clean edges.
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {w}" width="256" height="256">{inner}</svg>\n'

for name, fill, rim, body in EL:
    # BADGE — the card's 40px tile: r9 corners (5.4 of 24), 1.5px rim (0.9); glyph at 0.62
    # (the card says 22/40 = .55; plumped so it still reads on the roster's 14px chips) centred.
    inner = (f'<rect x="0.45" y="0.45" width="23.1" height="23.1" rx="5.4" fill="{fill}" stroke="{rim}" stroke-width="0.9"/>'
             f'<g transform="translate(4.56 4.56) scale(0.62)">{glyph(name, body, rim, fill)}</g>')
    open(os.path.join(OUT, "background", f"{name}.svg"), "w", encoding="utf-8").write(svg(inner))
    # INK — parchment glyph; the vein knocks out to a dark surface tone.
    open(os.path.join(OUT, "element_white", f"{name}.svg"), "w", encoding="utf-8").write(svg(glyph(name, body, "#f4f1ea", "#1a1026")))
    # BARE — rim-colored glyph on nothing; the vein in the element's dark tone.
    open(os.path.join(OUT, "transparent", f"{name}.svg"), "w", encoding="utf-8").write(svg(glyph(name, body, rim, fill)))
    print("baked", name)
