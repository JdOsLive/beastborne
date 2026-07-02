"""PawPad animated icon conversion: animated SVG -> lossless animated WebP.

Deterministic frame stepping: pause all CSS animations and drive them with a
negative animation-delay per frame — every frame is exact, no realtime flake.
128x128 transparent, 20fps (50ms/frame), loops seamlessly (loop=0).
"""
import re
import sys
from pathlib import Path

from PIL import Image
from playwright.sync_api import sync_playwright

SRC = Path(r"C:\Users\jscho\AppData\Local\Temp\claude\c--Users-jscho-OneDrive-Documents-s-box-projects-beastborne\d199b3ba-a286-42bc-aea4-5d0540e2de3d\scratchpad\pawpad-mockup\icons\v2")
OUT = Path(r"C:\Users\jscho\OneDrive\Documents\s&box projects\beastborne\Assets\ui\icons\pawpad\anim")
FRAMES_DIR = Path(r"C:\Users\jscho\AppData\Local\Temp\claude\c--Users-jscho-OneDrive-Documents-s-box-projects-beastborne\d199b3ba-a286-42bc-aea4-5d0540e2de3d\scratchpad\frames")
FRAME_MS = 16  # ~60fps (user call after seeing 20fps live)

OUT.mkdir(parents=True, exist_ok=True)
FRAMES_DIR.mkdir(parents=True, exist_ok=True)


def loop_seconds(svg_text: str) -> float:
    """Longest animation duration in the SVG = the loop length."""
    durs = [float(m) for m in re.findall(r"animation\s*:[^;]*?(\d+(?:\.\d+)?)s", svg_text)]
    return max(durs) if durs else 1.5


def main() -> None:
    svgs = sorted(SRC.glob("*.anim.svg"))
    if not svgs:
        print("no anim svgs found", file=sys.stderr)
        sys.exit(1)

    with sync_playwright() as pw:
        browser = pw.chromium.launch()
        page = browser.new_page(viewport={"width": 128, "height": 128})

        for svg_path in svgs:
            name = svg_path.name.replace(".anim.svg", "")
            text = svg_path.read_text(encoding="utf-8")
            secs = loop_seconds(text)
            n_frames = max(2, round(secs * 1000 / FRAME_MS))

            # Scale the 48-viewBox SVG to fill 128px; transparent page.
            scaled = re.sub(r'width="48" height="48"', 'width="128" height="128"', text)
            page.set_content(
                "<!doctype html><html><head><style>"
                "html,body{margin:0;padding:0;background:transparent;}"
                "svg *{animation-play-state:paused !important;}"
                "</style></head><body>" + scaled + "</body></html>"
            )

            frames = []
            for f in range(n_frames):
                t = f * FRAME_MS / 1000.0
                page.evaluate(
                    "t => document.querySelectorAll('svg *').forEach("
                    "el => { el.style.animationDelay = -t + 's'; })",
                    t,
                )
                shot = FRAMES_DIR / f"{name}_{f:03d}.png"
                page.screenshot(path=str(shot), omit_background=True)
                frames.append(Image.open(shot).convert("RGBA"))

            first, rest = frames[0], frames[1:]
            out_path = OUT / f"{name}.webp"
            first.save(
                out_path,
                save_all=True,
                append_images=rest,
                duration=FRAME_MS,
                loop=0,
                lossless=True,
            )
            print(f"{name}: {n_frames} frames @ {secs}s -> {out_path.name}")

        browser.close()

    print("done")


if __name__ == "__main__":
    main()
