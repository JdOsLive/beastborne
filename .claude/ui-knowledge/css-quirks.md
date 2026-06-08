# s&box CSS Quirks (agent-owned mirror)

This mirrors the quirks table in `CLAUDE.md`. Keep in sync — if a new quirk is learned, add it here AND flag for promotion to CLAUDE.md in `learnings.md` with a `[PROMOTE]` tag.

Do not treat this file as authoritative — CLAUDE.md is the source of truth. This file exists so the agent can load quirks without re-reading CLAUDE.md from scratch each invocation.

## Layout & flexbox

- **`line-height` must be extremely high for large font sizes.** 30px+ font needs line-height numerically ≥ the font-size, often higher. 42px font-size with line-height:42 works. Lower values clip text.
- **`overflow: hidden` collapses flex children.** Setting it on a flex child can shrink to 0×0. Avoid on flex children.
- **Scroll containers need flat children.** s&box can't calculate scroll height when a scrollable container has nested flex wrappers. Scrollable items must be DIRECT children of the `overflow-y: scroll` element. Pattern:
  ```scss
  .parent {
    display: flex;
    flex-direction: column;
    overflow: hidden;
    height: Xpx;
  }
  .scroll {
    flex: 1 1 0;
    min-height: 0;
    overflow-y: scroll;
    // items directly inside here
  }
  ```
- **`flex-wrap: wrap` miscalculates height.** Container won't compute height correctly. Use explicit row divs (two `.row` divs instead of one wrapping flex).
- **`flex: 1` can fail with 3+ siblings.** Use explicit `width: Xpx` values instead.
- **`inline-flex` not supported.** Use `display: flex` only.
- **`position: fixed` not supported.** Runtime log: "Generic fixed is not valid with position". Only `absolute`, `relative`, and the default (`static`) are accepted. For full-viewport overlays (drag shields, backdrops), use `position: absolute` with oversized negative offsets like `top: -4000px; left: -4000px; width: 8000px; height: 8000px;` — this covers any realistic viewport from any anchor point without needing the fixed-to-viewport behavior.

## Values and properties

- **`background: none` unsupported.** Use `background-color: transparent`.
- **`background: rgba(...)` shorthand treated as image.** s&box parses the shorthand as `background-image` and rejects rgba. Always use `background-color: rgba(...)` explicitly. Only use `background:` shorthand for actual gradients/images.
- **`flex: unset` unsupported.** Use `flex: 0 0 auto`. Parser throws "expected a float or length".
- **`border-style: dashed` unsupported.** Use `solid`.
- **`text-overflow: ellipsis` + `overflow: hidden` on text inside flex containers** collapses the element. Avoid this combo inside flex.
- **URL quotes in `background-image` inline styles** not supported. Use `url(@variable)` not `url('@variable')`.
- **`filter` accepts only ONE function.** `filter: brightness(0) opacity(0.5)` silently drops the entire declaration. Split into `filter: brightness(0); opacity: 0.5;`. Same for `grayscale(1) brightness(0.8)` etc — only one filter function per declaration. Discovered repeatedly during silhouette work.
- **`object-position` rejects percentage pairs.** `object-position: 50% 25%` throws "is not valid with object-position". Only keyword values work (`center top`, `left bottom`). Workaround: use keywords or drop the property and rely on default centering with `object-fit: contain`.
- **`scrollbar-color` and `scrollbar-width` are non-functional.** Two-value `scrollbar-color: rgba(a) rgba(b)` and pixel `scrollbar-width: 6px` both rejected. Remove both — scrollbars use s&box defaults.
- **`inset` shorthand not supported.** `inset: 5px` throws "Generic 5px is not valid with inset". Expand to top/left/right/bottom individually.
- **`box-shadow: inset ...` unsupported.** Any `inset` keyword in box-shadow rejected. For recessed effects, use a darker `background-color` + border, or layer a nested absolutely-positioned div with a gradient. Outer drop shadows work fine.
- **`transparent` keyword inside `linear-gradient()` fails.** `linear-gradient(90deg, transparent, color)` rejected. Use `rgba(0, 0, 0, 0)` (zero-alpha) instead.
- **`repeating-linear-gradient()` unsupported.** Use regular `linear-gradient()` or solid color + border for hatched patterns.
- **`radial-gradient()` with ANY shape keyword fails — drop the shape word entirely.** Both `circle` and `ellipse` are rejected; parser logs `Cannot read a color from 'circle'` / `Cannot read a color from 'ellipse'` and drops the gradient. Positional qualifier `at X% Y%` also fails even with `circle`. **Working form:** `radial-gradient(stop1, stop2, ...)` — no shape, no position. Default shape is ellipse; on a square-ish container the result is visually indistinguishable from a circle. For off-center blooms, size + position the ELEMENT so its geometric center sits where the bloom should be, then paint a default radial gradient inside. Confirmed 2026-04-17 PM after runtime logs showed `[8] Cannot read a color from 'circle'` errors on MainMenu; 9 rules converted. Reference pattern: `Code/UI/Panels/SkillTreePanel.razor.scss:550-557` (documents this failure + working form inline).
- **`border-left: 3px solid rgba(...)` DOES work.** Despite an old quirks claim, per-side border shorthand WITH color WITH style word does parse correctly. Roster uses this widely for section accent bars. Confirmed 2026-04-15. (The original failure mode was a different combination; this one is fine.)

## Content rendering

- **Bare text renders vertically.** Text not wrapped in `<span>` or other element inside flex containers may render character-by-character vertically. Always wrap text.
- **Empty `<div>` elements render as visible panels.** Gray rectangles / scrollbar artifacts. Remove empty wrappers.

## Assets

- **Custom fonts must be at `Assets/fonts/` root.** NOT in subdirs. Place TTF as `Assets/fonts/Exo2-Bold.ttf`, not `Assets/fonts/Exo2/Exo2-Bold.ttf`.
- Register fonts: `Exo2 { font-family: url("fonts/Exo2-Bold.ttf"); }` then use `font-family: Exo2;`
- Resources in `.sbproj` must include `fonts/*`.

## Duplicate UI

- Some components (move-picker, confirm dialogs) exist duplicated in `MonsterRosterPanel` AND `MonsterDetailPanel`. When fixing styles, check BOTH `.razor.scss` files. Roster is primary.

## Event quirks

- **`onmouseenter` not supported.** Use `onmouseover` with a state guard to prevent repeated firing for hover sounds. See `feedback_sbox_hover_sound.md` in user memory.

## Animation

- **Animated SVGs (CSS `@keyframes` inside SVG) not supported.** Use animated WebP instead. Workflow: create SVG with animations → convert via Playwright headless Chromium frame-by-frame capture → stitch to animated WebP with Pillow. See CLAUDE.md "Animated Icon Workflow".
- **CSS `@keyframes` only play ONCE on first DOM mount.** Re-rendering the same element does NOT replay them — toggling `IsVisible` on a panel won't re-fire keyframes because the element stays in the DOM. **Use `transition:` for state-driven animations** — transitions re-fire every time a property/class toggles. Reserve `@keyframes` for true ambient infinite loops.
- **CSS `@keyframes` in SCSS files DO work** for ambient/loop animations — use them for idle motion, infinite pulses, particle bursts on FIRST mount. Not for state reveals.

## Stacking and rendering order

- **Absolute-positioned bg-layer children render BEHIND parent `background-color`.** A `.bg-pattern { position: absolute; top/left/right/bottom: 0 }` child inside a panel with `background-color: #0c0a18` paints UNDER the parent fill — invisible. Standard CSS would stack it ABOVE. Workaround: make the bg layer a plain in-flow flex child (first child, no `position: absolute`, no SCSS rules). Matches the `RadioWidget` pattern. Confirmed 2026-04-14.

## Razor interpolation

- **`@variableName` inside string text needs parens to disambiguate.** `Reach Lv@loreIdx to unlock` renders LITERALLY because Razor can't tell where the identifier ends. Use `@(loreIdx + 1)` form OR hoist to a local variable: `var n = loreIdx; ... Lv@n`. Common gotcha when interpolating numbers into UI strings.
- **Inline style transforms with culture-sensitive floats need InvariantCulture.** s&box CSS expects `1.30` not `1,30`. Format floats with `value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)` when building inline `transform: scale(...)` or `translate(...)` strings.

## box-shadow — two confirmed gotchas (menu focus-ring debugging, 2026-06) [PROMOTE]

- **s&box paints box-shadows LAST-on-top (opposite of browsers).** With multiple shadows
  the LAST-listed wins the overlap, not the first. For a gapped ring
  (`0 0 0 Rpx <ring>, 0 0 0 Gpx <gap>`) the GAP must be listed **last** to knock through
  the ring's inner edge — the reverse of what you'd write for a browser.
- **box-shadow spread distorts ring corners.** s&box doesn't add the spread to the inner
  shadow's corner radius, so a thick box-shadow ring reads "stretched"/uneven at the
  corners. For a clean concentric focus ring, use a **real bordered element**, not
  box-shadow: a child at `inset:-10px; border:4px solid <color>; border-radius:
  hostRadius+10` gives a 6px gap + 4px ring with smooth corners. If the host has
  `overflow:hidden` (e.g. a button clipping a sheen), put the ring in a wrapper *slot*
  sibling instead of a child. Canonical: menu `.lh-play-ring` / `.lh-item-ring`.

## 3D transforms ARE supported (confirmed 2026-06) [PROMOTE]

- **`perspective()`, `rotateY()`, `transform-origin` work in s&box** — confirmed in-engine
  on the main menu's angled buttons. Despite zero prior usage anywhere in the project,
  s&box's CSS DOES render 3D transforms. `transform: perspective(1300px) rotateY(10deg)`
  with `transform-origin: left center` tilts an element so its far edge recedes (a real
  trapezoid/depth effect — NOT a shear). Use the `perspective()` transform FUNCTION on the
  element itself (self-perspective) so you don't depend on a `perspective` property on a
  specific ancestor (which breaks across nesting/intermediate wrappers).
- **`skewX`/`skewY` only shear (parallelogram), no depth.** For "receding/going back" use
  perspective+rotateY, not skew. skewY tilts top/bottom edges but they stay parallel.
- **BUT an element's OWN `background`, `border`, AND `box-shadow` render AXIS-ALIGNED on a
  3D-rotated element** — they do NOT follow the rotation, so they float as a flat mismatched
  rectangle at the un-rotated box (the "purple box" bug on the menu featured card). Only the
  element's CHILDREN rotate correctly. So a 3D-rotated card with its own bg/border/shadow
  leaks all three flat. Fix: move the visible surface to a full-cover CHILD (e.g. an absolute
  bg layer that rotates) and make the rotated element's own bg/border/shadow transparent/none;
  OR rotate an outer wrapper and keep the styled element flat inside it. Fix: drop the border/shadow
  on any 3D-rotated element (define the edge via its background/gradient), OR rotate an outer
  WRAPPER and keep the bordered element un-transformed inside it. Elements with no visible
  border/shadow (e.g. the menu's PLAY/nav buttons) are unaffected.
- **To CLIP content inside a 3D-rotated element, use `mask`, NOT `overflow:hidden`** (confirmed
  2026-06 on the menu featured card). `overflow:hidden` computes a clip rect in flat space then
  projects it → the receding corner SQUARES + the clip can short the receding side. A `mask`
  (`mask-image: linear-gradient(rgba(0,0,0,1) 0%, rgba(0,0,0,1) 100%); mask-mode: alpha;`)
  composites WITH the element, so it clips correctly AND projects with the 3D rotation. Caveat:
  the mask is a RECTANGLE (no rounding) — full-cover overlay children that should be rounded need
  their OWN `border-radius` (the rect mask sits outside the rounded content, so it doesn't square
  them, but un-rounded overlays will fill the corners up to the rect). Canonical: `.lh-feat-shot`.
- **The clean recipe for a 3D-receding bordered/rounded card with bleeding content:** transform-
  holder = transparent, no border, no overflow (those render flat); a DIRECT-child "surface" carries
  the bg + border + `border-radius` + the `mask` clip; bleeding content (e.g. a beast) lives inside
  the surface and is masked. Canonical: menu `.lh-featured` (holder) + `.lh-feat-shot` (surface).
- Not yet tested: `translateZ`, `rotateX`, `preserve-3d`, hover `translateZ` pop. Try them.
- Canonical: menu `.lh-play` / `.lh-item` + the living cursor's matching transform.

## When in doubt

- **s&box has an LLM documentation index at `https://sbox.game/llms.txt`.** WebFetch it when stuck on s&box-specific APIs or CSS behavior. Docs may be outdated — verify against actual project code when something feels off.
- **Test empirically.** s&box CSS behavior is not a 1:1 match with browsers. If a rule behaves unexpectedly, try alternatives and add findings to `learnings.md`.
