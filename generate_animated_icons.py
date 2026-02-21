"""
Render animated SVGs to seamlessly looping animated WebP using Playwright.

Workflow:
1. Load each *-animated.svg in headless Chromium at 128x128
2. Use Web Animations API to seek CSS animations to precise frame times
3. Use SVG.setCurrentTime() for SMIL animations (e.g. <animate>)
4. Capture transparent PNG screenshots per frame
5. Stitch into animated WebP with infinite loop (loop=0)

Install: pip install playwright Pillow && python -m playwright install chromium
Run:     python generate_animated_icons.py
"""

import asyncio
import re
import math
import io
from pathlib import Path
from PIL import Image
from playwright.async_api import async_playwright

ICONS_DIR = Path(r"c:/users/jscho/documents/s&box projects/megarougelite/Assets/ui/icons/animated")
SIZE = 128
FPS = 20
FRAME_MS = 1000 / FPS  # 50ms per frame


def gcd(a, b):
    while b:
        a, b = b, a % b
    return a


def lcm(a, b):
    return abs(a * b) // gcd(a, b)


def parse_animation_properties(svg_content):
    """Extract animation durations and delays from SVG CSS and SMIL."""
    durations = []
    delays = []
    time_re = r'(\d+\.?\d*)(s|ms)'

    # CSS animation shorthand: animation: name duration timing delay iteration ...
    for m in re.finditer(r'animation\s*:\s*([^;}]+)', svg_content):
        shorthand = m.group(1)
        times = re.findall(time_re, shorthand)
        if times:
            d = float(times[0][0]) * (1000 if times[0][1] == 's' else 1)
            durations.append(int(d))
        if len(times) >= 2:
            dl = float(times[1][0]) * (1000 if times[1][1] == 's' else 1)
            delays.append(int(dl))

    # CSS animation-duration property
    for m in re.finditer(r'animation-duration\s*:\s*(\d+\.?\d*)(s|ms)', svg_content):
        d = float(m.group(1)) * (1000 if m.group(2) == 's' else 1)
        durations.append(int(d))

    # SMIL <animate> dur attribute
    for m in re.finditer(r'<animate[^>]*dur="(\d+\.?\d*)(s|ms)"', svg_content):
        d = float(m.group(1)) * (1000 if m.group(2) == 's' else 1)
        durations.append(int(d))

    return durations, delays


def compute_cycle_ms(durations):
    """Compute cycle duration as LCM of all animation durations."""
    if not durations:
        return 2000
    # Deduplicate
    unique = list(set(max(1, d) for d in durations))
    result = unique[0]
    for d in unique[1:]:
        result = lcm(result, d)
    # Cap at 10 seconds (200 frames at 20fps)
    return min(result, 10000)


def build_html(svg_content, size):
    """Build HTML page with SVG rendered at target size on transparent bg."""
    return f"""<!DOCTYPE html>
<html>
<head><style>
    html, body {{
        margin: 0; padding: 0;
        background: transparent;
        overflow: hidden;
        width: {size}px; height: {size}px;
    }}
    body {{
        display: flex;
        align-items: center;
        justify-content: center;
    }}
    svg {{
        width: {size}px;
        height: {size}px;
    }}
</style></head>
<body>{svg_content}</body>
</html>"""


async def render_svg_to_webp(page, svg_path, output_path):
    """Render an animated SVG to a looping animated WebP."""
    svg_content = svg_path.read_text(encoding='utf-8')

    durations, delays = parse_animation_properties(svg_content)
    cycle_ms = compute_cycle_ms(durations)
    max_delay = max(delays) if delays else 0

    # Start capturing after all delays have elapsed, at a clean cycle boundary.
    # This ensures all animations are in their repeating steady-state.
    start_ms = cycle_ms * max(1, math.ceil((max_delay + 1) / cycle_ms))

    num_frames = int(cycle_ms / FRAME_MS)
    name = svg_path.stem.replace('-animated', '')
    print(f"  cycle={cycle_ms}ms, start_offset={start_ms}ms, frames={num_frames}")

    html = build_html(svg_content, SIZE)
    await page.set_content(html)
    await page.set_viewport_size({"width": SIZE, "height": SIZE})

    # Wait for initial render
    await page.wait_for_timeout(300)

    frames = []
    for i in range(num_frames):
        t_ms = start_ms + i * FRAME_MS
        t_sec = t_ms / 1000.0

        # Seek all animations to the exact time
        await page.evaluate(f"""() => {{
            // CSS animations (Web Animations API)
            document.getAnimations().forEach(a => {{
                a.pause();
                a.currentTime = {t_ms};
            }});
            // SMIL animations (<animate>, <animateTransform>, etc.)
            const svg = document.querySelector('svg');
            if (svg && svg.pauseAnimations) {{
                svg.pauseAnimations();
                svg.setCurrentTime({t_sec});
            }}
        }}""")

        # Brief wait for render to complete
        await page.wait_for_timeout(30)

        screenshot = await page.screenshot(type='png', omit_background=True)
        img = Image.open(io.BytesIO(screenshot)).convert('RGBA')
        frames.append(img)

    # Save as animated WebP with infinite loop
    if frames:
        frames[0].save(
            str(output_path),
            save_all=True,
            append_images=frames[1:],
            duration=int(FRAME_MS),
            loop=0,  # 0 = infinite loop
            lossless=True,
        )
        size_kb = output_path.stat().st_size / 1024
        print(f"  -> {output_path.name} ({len(frames)} frames, {size_kb:.1f} KB)")


async def main():
    svg_files = sorted(ICONS_DIR.glob('*-animated.svg'))
    print(f"Found {len(svg_files)} animated SVGs to convert\n")

    async with async_playwright() as p:
        browser = await p.chromium.launch()
        page = await browser.new_page()

        for svg_path in svg_files:
            name = svg_path.stem.replace('-animated', '')
            output = ICONS_DIR / f"{name}.webp"
            print(f"[{name}] Converting {svg_path.name}...")
            try:
                await render_svg_to_webp(page, svg_path, output)
            except Exception as e:
                print(f"  ERROR: {e}")
            print()

        await browser.close()

    print("Done! All animated WebPs generated.")


if __name__ == '__main__':
    asyncio.run(main())
