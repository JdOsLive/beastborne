# Animated Icon Workflow (SVG → WebP)

(Moved out of CLAUDE.md — only needed when making animated icons.)

s&box can't play animated SVGs (the SVG's internal CSS `@keyframes` don't run). To ship an animated icon:

1. **Create animated SVGs** with CSS `@keyframes` animations (user does this manually or with a tool)
2. **Place animated SVGs** in `Assets/ui/icons/animated/` with `-animated.svg` suffix
3. **Convert to animated WebP** using Playwright (headless Chromium renders CSS animations frame-by-frame):
   - Install: `pip install playwright Pillow` (Chromium is usually already available — see `tools/convert-pawpad-icons.py` / `generate_animated_icons.py` for working converters)
   - Load each SVG inline in a headless browser page
   - Screenshot each frame at 50ms intervals (20fps) with transparent background
   - Stitch frames into lossless animated WebP using Pillow
   - Output at **128x128px** resolution for quality when scaled down
4. **Reference the `.webp` files** in Razor, NOT the `.svg` files
5. **CSS hover swap pattern**: Both static SVG and animated WebP `<img>` tags sit in the same container. The animated one is `position: absolute; opacity: 0;` and becomes `opacity: 1;` on `:hover`. No state management needed — pure CSS.

Note (2026-09): the old "7 bottom-bar button icons still need animated SVGs" item is moot — the bottom bar was retired for the PawPad phone launcher (icons in `Assets/ui/icons/pawpad/`).
