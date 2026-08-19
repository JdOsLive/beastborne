# Bake animated two-strand DNA-helix WebPs for the fusion ritual (2026-07-12).
# The BIND phase's CSS "helix" (6 horizontal rungs + 12 static dots) did not
# read as a helix at all — CSS can't draw crossing sine strands. This bakes a
# real one: two glowing sine strands crossing with DEPTH (the back strand is
# dimmer/thinner at each crossing — that's what sells 3D rotation), plus thin
# dim cross-rungs. Strand A = element color (the in-code ElementHex palette,
# MonsterRosterPanel.razor), strand B = the forge accent in EVERY variant
# (fusion is same-species-only, so element × forge is the pair that always
# exists). 2026-07-12: forge accent = My Beasts violet #9b6cff (was pink).
#
# Pipeline (same as the animated button icons / element daises): Playwright
# headless Chromium renders a canvas frame-by-frame — time is STEPPED in JS
# (never wall-clock) — then Pillow stitches a lossless animated WebP.
#   canvas 450x840 (1.5x of the ~300x560 "Helix Cathedral" display size on
#   the full-column 560x800 ritual stage — 2x was 3.3MB/variant even
#   quantized; 1.5x is indistinguishable for smooth glow art), transparent
#   bg, 18 frames @ 50ms = one full rotation in 0.9s, seamless (2π == 0).
#   Strands span canvas y 60–780 (glow padding ≈ 7% top/bottom).
#
# HISTORY: v1 baked 400x680 (200x340 display on the old 400x400 stage) as
# helix-<element>.webp. When the stage grew to the full center column the
# bake was redone at this size under NEW -tall filenames — the texture-cache
# law: overwriting a webp the running game has loaded serves stale pixels.
#
# Usage: python bake-helix.py [element ...]   (no args = all 11 variants)
# Output: Assets/ui/fusion/helix-<element>-tall.webp  (+ frame-0 preview PNGs
# in %TEMP%\helix-previews — NOT inside Assets, or s&box compiles them)

import io
import math
import os
import sys

from PIL import Image
from playwright.sync_api import sync_playwright

ROOT = r"c:\Users\jscho\OneDrive\Documents\s&box projects\beastborne"
OUT_DIR = os.path.join(ROOT, "Assets", "ui", "fusion")
W, H = 450, 840
# -v2 = the 2026-07-12 violet rebake (strand B: forge pink #F472B6 →
# My Beasts violet #9b6cff — fusion adopted the beast-page identity).
# NEW suffix per the texture-cache law: the running game serves stale
# pixels if a loaded webp is overwritten in place.
SUFFIX = "-v2"
FRAMES, DURATION_MS = 18, 50
FORGE_VIOLET = "#9b6cff"
# Per-element strand-B overrides: shadow's element color (#9D7BEA) is
# nearly the forge violet — nudge strand B lighter so two strands still
# read at the crossings.
STRAND_B = {
    "shadow": "#cdb2ff",
}

# The game's live element palette — ElementHex() in MonsterRosterPanel.razor
# ("Turn-6 element palette (matches the bestiary icons + dais assets)").
ELEMENTS = {
    "neutral":  "#A8A29E",
    "fire":     "#FB923C",
    "water":    "#60A5FA",
    "earth":    "#C9974C",
    "wind":     "#2DD4BF",
    "electric": "#FBBF24",
    "ice":      "#7DD3FC",
    "nature":   "#4ADE80",
    "metal":    "#94A3B8",
    "shadow":   "#9D7BEA",
    "spirit":   "#F472B6",  # pink × pink — monochrome, but consistent
}

HTML = """<!doctype html><html><head><style>
html,body{margin:0;padding:0;background:transparent}
canvas{display:block}
</style></head><body>
<canvas id="c" width="450" height="840"></canvas>
<script>
const cv=document.getElementById('c'),ctx=cv.getContext('2d');
// Cathedral proportions: 3.5 twists (7 crossings) over the tall span.
const W=450,H=840,CX=225,AMP=144,Y0=60,Y1=780,TWISTS=3.5,STEP=5;
function hexToRgb(h){h=h.replace('#','');return[parseInt(h.substr(0,2),16),parseInt(h.substr(2,2),16),parseInt(h.substr(4,2),16)];}
function rgba(c,a){return 'rgba('+c[0]+','+c[1]+','+c[2]+','+a+')';}
// Soft taper at both ends so the helix fades out instead of being cut off.
function taper(y){
  const t=(y-Y0)/(Y1-Y0),e=0.09;
  return Math.min(1,t/e,(1-t)/e);
}
function render(phase,hexA,hexB){
  ctx.clearRect(0,0,W,H);
  const A=hexToRgb(hexA),B=hexToRgb(hexB);
  const segs=[];
  // Two strands, π apart. z=cos(θ) is the depth cue: front (z→1) renders
  // bright/thick with a hot white core, back (z→-1) dim/thin.
  for(const [col,off] of [[A,0],[B,Math.PI]]){
    for(let y=Y0;y<Y1;y+=STEP){
      const th1=phase+off+2*Math.PI*TWISTS*(y-Y0)/(Y1-Y0);
      const th2=phase+off+2*Math.PI*TWISTS*(y+STEP-Y0)/(Y1-Y0);
      const thm=(th1+th2)/2;
      segs.push({kind:'strand',col,z:Math.cos(thm),
        x1:CX+AMP*Math.sin(th1),ya:y,x2:CX+AMP*Math.sin(th2),yb:y+STEP,
        fade:taper(y+STEP/2)});
    }
  }
  // Cross-rungs: thin, dim, gradient A→B; sit just behind the mid plane so
  // the front strand passes over them. Skip rungs near a crossing (they'd
  // be zero-length specks).
  const NR=13;
  for(let i=0;i<NR;i++){
    const y=Y0+38+(Y1-Y0-76)*i/(NR-1);
    const thA=phase+2*Math.PI*TWISTS*(y-Y0)/(Y1-Y0);
    const xA=CX+AMP*Math.sin(thA),xB=CX+AMP*Math.sin(thA+Math.PI);
    if(Math.abs(xA-xB)<20)continue;
    segs.push({kind:'rung',z:-0.05,x1:xA,ya:y,x2:xB,yb:y,fade:taper(y)});
  }
  segs.sort((a,b)=>a.z-b.z); // painter's algorithm, back → front
  for(const s of segs){
    if(s.kind==='rung'){
      const g=ctx.createLinearGradient(s.x1,s.ya,s.x2,s.yb);
      g.addColorStop(0,rgba(A,0.32*s.fade));
      g.addColorStop(1,rgba(B,0.32*s.fade));
      ctx.lineCap='round';
      ctx.shadowBlur=0;
      ctx.strokeStyle=g;
      ctx.lineWidth=2.7;
      ctx.beginPath();ctx.moveTo(s.x1,s.ya);ctx.lineTo(s.x2,s.yb);ctx.stroke();
    }else{
      // Butt caps: round caps on semi-transparent per-segment strokes
      // double-cover a full disc at every joint → beaded/dashed look
      // (seen on the first bake's white core). Butt caps only overlap a
      // hairline wedge, invisible under the glow.
      ctx.lineCap='butt';
      const zn=(s.z+1)/2;
      ctx.shadowColor=rgba(s.col,0.9);
      ctx.shadowBlur=11+16*zn;               // glow baked in (CSS can't)
      ctx.strokeStyle=rgba(s.col,(0.28+0.72*zn)*s.fade);
      ctx.lineWidth=3.4+5.0*zn;
      ctx.beginPath();ctx.moveTo(s.x1,s.ya);ctx.lineTo(s.x2,s.yb);ctx.stroke();
      if(zn>0.55){                            // hot core on front arcs
        ctx.shadowBlur=0;
        ctx.strokeStyle='rgba(255,255,255,'+(0.50*(zn-0.55)/0.45*s.fade)+')';
        ctx.lineWidth=2.0;
        ctx.beginPath();ctx.moveTo(s.x1,s.ya);ctx.lineTo(s.x2,s.yb);ctx.stroke();
      }
    }
  }
}
</script></body></html>"""


def bake(page, name, hex_a):
    hex_b = STRAND_B.get(name, FORGE_VIOLET)
    frames = []
    for f in range(FRAMES):
        phase = 2 * math.pi * f / FRAMES  # stepped time — seamless by math
        page.evaluate("([p,a,b])=>render(p,a,b)", [phase, hex_a, hex_b])
        png = page.locator("#c").screenshot(omit_background=True)
        img = Image.open(io.BytesIO(png)).convert("RGBA")
        # Quantization (channels to multiples of 8): the glow's smooth
        # gradients otherwise defeat lossless WebP (5.3 MB/variant at the
        # tall size unquantized-ish; >>2 was still ~3.5). An 8-step ladder
        # is invisible on soft glow art over the dark ritual column and
        # compresses far better.
        img = img.point(lambda v: (v >> 3) << 3)
        frames.append(img)
    dst = os.path.join(OUT_DIR, f"helix-{name}{SUFFIX}.webp")
    frames[0].save(
        dst, save_all=True, append_images=frames[1:],
        duration=DURATION_MS, loop=0, lossless=True, method=6,
    )
    print(f"  helix-{name}{SUFFIX}.webp  ({os.path.getsize(dst) // 1024} KB, {len(frames)} frames)")
    return frames[0]


def main():
    wanted = [a.lower() for a in sys.argv[1:]] or list(ELEMENTS)
    for name in wanted:
        if name not in ELEMENTS:
            raise SystemExit(f"unknown element '{name}' — known: {', '.join(ELEMENTS)}")
    os.makedirs(OUT_DIR, exist_ok=True)
    preview_dir = os.path.join(os.environ.get("TEMP", OUT_DIR), "helix-previews")
    os.makedirs(preview_dir, exist_ok=True)
    with sync_playwright() as pw:
        browser = pw.chromium.launch()
        page = browser.new_page(viewport={"width": W + 20, "height": H + 20})
        page.set_content(HTML)
        for name in wanted:
            f0 = bake(page, name, ELEMENTS[name])
            f0.save(os.path.join(preview_dir, f"helix-{name}-f0.png"))
        browser.close()
    print("done.")


if __name__ == "__main__":
    main()
