# DOC-7 — ENGINE LAWS (the hard s&box constraints, indexed by symptom)

**Charter.** This lane owns what the engine will and won't do — the constraints no design
decision can override. It is a distilled INDEX, not the law book: the full rows live in the
**CLAUDE.md quirks table** (authoritative), `css-quirks.md`, `guiding-star.md` §s&box
translation layer, and dated `learnings.md` entries. Cite the row, verify against the source;
if the engine version changes (parser rewrites like 26.06.03), the sources get updated first.

## THE CHECKLIST — grouped by symptom

**"My CSS silently does nothing / console parse error"**
1. Rejected outright: `box-sizing` (any value) · `position: fixed` · `display: block`/`inline-flex` (flex/none only) · `transparent` keyword inside gradients (use `rgba(...,0)`) · radial-gradient shape keywords / `at X% Y%` / px stops (percent stops only) · `repeating-linear-gradient` · `box-shadow: inset` · `:focus-within` · `:first-of-type` family (use `:first-child`) · `scrollbar-color`/`-width` values · CSS custom properties `var(--x)` · `border-style: dashed`. [CLAUDE.md table; tokens.md spike; learnings 2026-04-15]
2. `pointer-events: auto` parses but reads back **None** — only `none`/`all`. [CLAUDE.md row; learnings 2026-08-29 PROMOTE]
3. `filter` takes ONE function only (chains drop the whole declaration); `animation: none` is rejected (use `animation-play-state: paused`); `background: #hex` shorthand-with-color no-ops — use `background-color` + `background-image: none`. [learnings 2026-04-15; 2026-05-01; 2026-05-18]
4. Unitless `line-height` is a MULTIPLIER (post-26.06.03) — px always. [CLAUDE.md first row]
5. Razor: glued interpolation `class="slip-v@Var"` emits the LITERAL text — use `@("slip-v" + Var)`; `@code`/`@if` inside comments are parsed as real directives; `@if/else` icon+label swaps leave ghost children (compute a tuple, render one element); multi-`@` text nodes squash (sibling spans). [learnings 2026-08-30 PROMOTE; CLAUDE.md rows; learnings 2026-07-13]

**"My element is invisible / in the wrong place"**
6. Declared width/height = PADDING BOX; borders add on top. [CLAUDE.md row; learnings 2026-07-05]
7. A full-cover absolute layer paints BEHIND the parent's background-color — make it an in-flow first child; small explicit-size corner-anchored absolutes are the working family. [CLAUDE.md row; learnings 2026-07-05 1B]
8. Absolutes need `top:0; left:0` explicitly and never shrink-wrap (`<iconify>` included) — explicit boxes always. [learnings 2026-04-17; 2026-08-29 arrow]
9. Panel `> *` blanket position/z rules stomp absolutely-positioned children — out-specify (`.panel > .overlay`). [learnings 2026-06-03 ×2]
10. `text-align: center` on a sized span (absolute included) is a no-op — shrink-wrap + parent `align-items: center`. [learnings 2026-07-12; 2026-08-30]
11. Flex collapse family: `overflow: hidden` on auto-sized flex children collapses them; nested flex rows in scroll columns collapse; `flex: 1` misdistributes with 3+ siblings; `margin-left: auto` eats gaps and shoves siblings past clips. [CLAUDE.md rows; learnings 2026-05-18; 2026-06-08]

**"It leaks / paints over things"**
12. Scroll containers do NOT clip descendant box-shadow, transforms, or overhanging absolutes — no colored halos or transforms on scroll-grid cards; opaque caps over unavoidable leaks; celebration overlays OUTSIDE the scroll. [CLAUDE.md rows]
13. Transformed elements partially inside a scroll viewport paint FULLY unclipped (translateY-teleport); `overflow: hidden` never clips absolute or transformed descendants (it DOES clip in-flow children — the crop recipe). [learnings 2026-07-05 wedges; 2026-08-29 crop]
14. Rotated-square corner cuts paint over LATER siblings of their parent — keep nearby text inside the cut's parent with higher z. [learnings 2026-08-29 pennant label]

**"My transform/3D broke something else"**
15. A transform on a flex parent poisons descendant flex-grow WIDTH resolution; a transform holder can't carry bg/border/overflow under 3D (surface on a direct child); `Box.Rect` is PRE-transform (ring/cursor math must compensate); there is NO `perspective` property / no `preserve-3d` — one transform per GROUP. [CLAUDE.md; guiding-star §2 ⚠; memory angled-ui]
16. `% ` widths under a transformed ancestor are derived-width poison — compute explicit px in C# (`Length.Pixels`), also for imperative progress fills on absolutes. [CLAUDE.md imperative-width row; learnings 2026-07-05 1B (3)]

**"My animation doesn't play / plays wrong"** (full doctrine → DOC-4)
17. @keyframes fire on first MOUNT only; class-swap phases on a mounted element don't replay; `animation-delay` dies on re-render (percentage-hold keyframes); animations beat transitions/statics on shared properties; animated transform clobbers static base transform. [CLAUDE.md rows; learnings 2026-06-03; 2026-07-12]

**"My image/texture looks wrong"**
18. Any `filter` OR `transform: scale()` on/above an `<img>` rasterizes through the compositor and blurs pixel art — opacity/overlays/gradient swaps instead. [CLAUDE.md row; memory feedback_sbox_pixel_compositor]
19. SVGs rasterize at their VIEWBOX units — author glyphs at 24, emit at viewBox 240; width/height hints don't sharpen. [learnings 2026-08-30 correction]
20. Overwritten texture files serve CACHED pixels — rename (`-v2`) to bust; `<img>` + `object-fit: contain` tiles at edges without `background-repeat: no-repeat`; tiled decorations seam unless baked native-size with integer geometry; `border-radius: 50%` caps at a stadium pill and wide-short radial washes render near-solid — bake ellipses/halftones as PNGs. [learnings 2026-07-10; CLAUDE.md; learnings 2026-07-05 seam laws + dais]

**"Input is dead"** (full grammar → DOC-5)
21. `AcceptsFocus = true` on a page panel kills `Input.Pressed` game-wide; UI wheel = `OnMouseWheel` override, not `Input.MouseWheel`; `onmouseenter` doesn't exist (`onmouseover` + guard); hit-test = DOM-order siblings, later wins iff `SiblingIndex + z` ≥ best, `pointer-events:none` skips self but descends children. [CLAUDE.md rows; learnings 2026-08-29 engine-source note]

**"State changes don't render"**
22. Every field that affects rendering goes in `BuildHash()` — cursor indexes, IsVisible, timestamps-made-visible; `Log.Info` truncates at the first newline (log per line); `Texture` binds via `Style.SetBackgroundImage`, never url-interpolation. [CLAUDE.md BuildHash + Log rows; learnings 2026-04-17]

## COMMON FAILURES (the expensive ones)
- **TeamPickerPopup transform-poisoned flex** — 6+ hours; one `translate(-50%,-50%)` on the modal ancestor. [CLAUDE.md row]
- **Dead PawPad** — `pointer-events: auto` everywhere, an hour of code-reading; a 40-line probe found it. Build the instrument first (`dev_phonechain` pattern). [learnings 2026-08-29]
- **Beastbook's silently truncated reveal** — ~20 delayed animations dead under GlobalFrame re-renders. [learnings 2026-07-12]
- **The corner-mask patch cascade** — four generations of overlay patches vs a 10-minute texture re-bake: the second time a patch needs its own patch, re-derive the asset. [learnings 2026-07-10 PROMOTE]
- **Whole-UI break on 26.06.03** — unitless line-height semantics changed under us; engine updates re-open settled laws. [CLAUDE.md preamble]

## WHAT THIS LANE DOES NOT OWN
Design intent — every other lane. This doc says what CANNOT work; it never says what SHOULD.
When a checklist here collides with a design ask, the engine wins and the design lane adapts
(and the collision gets recorded in `learnings.md`, promoted to CLAUDE.md when proven twice).
