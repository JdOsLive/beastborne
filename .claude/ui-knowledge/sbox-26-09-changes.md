# s&box UI engine changes, June → Sep 2026 (research notes, 2026-09-25)

Sources: sbox.game/news update posts (via search excerpts — direct fetch was blocked by the
sandbox proxy) + Facepunch `sbox-public` source at commit 5ddfb7b5 (2026-09-24). Update
attribution by commit date. **Everything here is source-/changelog-derived, NOT yet
verified in our editor.** When you verify an item in-game, update the matching row in
`CLAUDE.md` and note it here.

## ⚠️ Likely live impact on Beastborne
1. **`<button>` takes keyboard focus** (commit 9a16c2c, 2026-09-04 → 26.09.08). `Button()`
   sets `AcceptsFocus = true` (also Checkbox, Tab). Mousedown → `Focus()` on the nearest
   focusable ancestor; in-game, `UISystem` (~l.363) sets the button state to UI when the
   focused panel's `ButtonInput` is the default (`UI`), which blocks game presses — the
   same failure as our `AcceptsFocus` law. Verified in source by the main session
   (`Controls/Button.cs:101`, `UISystem.cs:363`). We have ~300 `<button>` tags. Focused
   buttons also get `:focus`, Tab/Shift+Tab move focus, Enter/Space click the focused
   control (possible double-fire with `UiInput.ConfirmPressed()`).
   Candidate fixes to test: set `ButtonInput = PanelInputType.Game` on focusable controls
   (keys then flow to the game), or clear UI focus after clicks.
2. **Yoga replaced by `Sandbox.Layout`** (a7b5147, 2026-09-05 → 26.09.08). Defaults
   preserved (flex, row, shrink 1, align-content flex-start, static). Internal sizing is
   `BoxSizing.BorderBox` (LayoutStyle.cs:273), not reachable from CSS — our 2026-07-05
   padding-box measurement needs redoing. All Yoga-era layout workarounds should be
   re-tested (flex-wrap height, `flex:1` with 3+ siblings, translate(-50%) poisoning,
   nested flex in scroll). Ships 24 flex-wrap conformance tests.
3. **Scrollbars opt-in** (547dc51 → 26.09.08): default `scrollbar-width` draws nothing.

## Timeline
| Update | UI-relevant changes |
|---|---|
| 26.06.03 | Parser rewrite (known). Perf: rule matching 3–5× faster, fewer transition allocs, finished animations stop re-laying out, less GC in selectors, faster Yoga wrapper, `!important` fixed. |
| 26.06.24 | Engine-wide alloc cuts (~100 KB/frame). |
| ~26.07.08 | Label rich text: `style` attr + inherited styles; HtmlPanel additions. |
| 26.08.05 | CSS `isolation`. |
| 26.09.01 | "Major UI rendering improvements": shader gradients (web-matching; px stops, circle/ellipse, transparent, corner directions), gamma-space blending (alpha looks like the web now — translucent colors may look different), HDR colors (`rgba(white * 4, .5)`), text batched with boxes, unified rounded-rect for boxes/borders/clips/shadows + elliptical radii, real gaussian `blur`/`drop-shadow`, CSS-matrix color filters, `background-clip` (incl. `text`), `border-shape` polygon/circle. Perf: no per-panel matrix invert, layout behind dirty flag, scrolling doesn't invalidate layout, Razor skips `UpdateBinds` with no binds. Fixes: `text-decoration: none`, `aspect-ratio: none`, spaced `hsl()`, nested `>`. |
| 26.09.01–08 | Absolute panels measure against nearest positioned ancestor again; inline styles inherit again; focus change repaints both panels. |
| 26.09.08 | `Sandbox.Layout` (block/grid/inline/contents, `position: fixed`, `place-*`), inline text with styled spans/wrapping/selection, scrollbars + `Panel.ScrollIntoView` + `overflow-x`, Tab focus traversal (`TabIndex`, `FocusNext/Previous`), keyboard-navigable menus, `Panel.StyleParent`, TreeView, PanelWindow, color picker, font faces load-order independent, stylesheet change rebuilds rules, inline-style change schedules layout. |
| 26.09.15 | GPU text (sharp under transforms; main menu 79→90 fps), Painter replaces the panel renderer (`HudPainter` obsolete — unused by us), `overscroll-behavior`, Razor child content survives a parent's `:outro`. |
| 26.09.22 | Box-shadow no longer clipped on panels with `filter`; transparent `background-image` no longer tints; TabBar/TabPanel, Toolbar, StatusBar, CanvasPanel, GraphPanel, CurveEditor; panel docking; Painter animated sprites. |
| post-09-22 | Text composites correctly inside masks/filters. |

## Limitation status (source verdicts)
| Limitation | Verdict |
|---|---|
| `box-sizing` | Still not a CSS property; layout is border-box internally. Re-measure. |
| `position: fixed` | Supported 26.09.08 (anchors to root viewport, renders above). |
| `display: block`/`grid` | Supported 26.09.08 (`block`, `flow-root`, `grid`, `inline`, `contents`). |
| `inline-flex` | Still unsupported. |
| `transparent` in gradients | Supported 26.09.01 (premultiplied). |
| `radial-gradient` shapes / px stops | Supported 26.09.01. |
| `conic-gradient` | Supported (shader-evaluated since 26.09.01). |
| `backdrop-filter` | Supported (pre-June). |
| `object-position` | Not implemented at all. |
| `box-shadow: inset` | Supported (leading-`inset` parse fixed 2026-05-18). |
| `:focus-within` | Unsupported. |
| `:*-of-type` | Unsupported. |
| `repeating-linear-gradient` | Unsupported. |
| Chained `filter` | Supported functions chain but apply in a fixed order; `opacity()` is not a filter function → whole declaration fails. |
| `var()` / custom props | Unsupported (SCSS `$vars` only). |
| `color-mix()` | Unsupported (non-standard `mix()`/`lerp()` exist). |
| `perspective` / `preserve-3d` | Only the `perspective()` transform function; no depth sorting. |
| `::before`/`::after` | Supported. |
| `@keyframes` replay on class change | No change: restarts only when the resolved keyframes object changes. |
| `animation-delay` vs re-renders | No change found. |
| flex-wrap height | Unknown — new engine; re-test. |
| Scroll clipping shadows/transforms | Painter draws children inside parent clip; only `fixed` escapes. Likely fixed — verify. |
| `overflow: hidden` clipping absolute children | Likely clips (gallery OverflowTests). `width: auto` on absolute now shrink-fits. |
| `filter` on `<img>` blurring pixel art | Mechanism (off-screen layer composite) still present — assume still blurs. |
| BuildHash cost | No API change; layout/scroll/binds cheaper. |
| Focus/keyboard API | New: Tab traversal, `TabIndex`, `FocusNext/Previous`, Enter/Space click, `ScrollIntoView`, menu keyboard nav. |

Unverified extras: `mix-blend-mode`/`background-blend-mode` parse; UI textures sample with
−1.5 mip bias again (09-03); failed font loads now log an error.
