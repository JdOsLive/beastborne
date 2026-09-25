# Beastborne - Claude Guidelines

s&box (Razor + SCSS + C#) monster-taming game. Project file is `megarougelite.sbproj` (a relic name — the game is Beastborne). Game/systems overview: `DEV_ONBOARDING.md`.

## Keep context lean — how the knowledge files work

This file loads on EVERY turn, so it holds only rules that apply to nearly every task. Everything else loads on demand:

| Need | Read |
|---|---|
| UI engine laws + conventions (short, always read for UI work) | `.claude/ui-knowledge/laws.md` |
| Canonical style spec | `.claude/ui-knowledge/guiding-star.md` |
| Active UI overhaul brief (the standing prompt) | `.claude/ui-knowledge/ui-overhaul-brief.md` |
| Past UI history / "why is it like this?" | **grep** `.claude/ui-knowledge/learnings-archive.md` — never read it whole (560 KB) |
| A specific panel's notes | **grep** `.claude/ui-knowledge/panel-inventory.md` for the panel name |
| Settled balance decisions | `.claude/balance-knowledge/decisions-summary.md`; **grep** `decisions-log.md` for full reasoning |
| PixelLab art prompts | `/monster-prompt` skill (`.claude/commands/monster-prompt.md`) |
| Discord patch notes | `/patch-notes` skill |
| Animated icons (SVG → WebP) | `.claude/ui-knowledge/animated-icons.md` |

**Growth caps (enforce when you write):** `laws.md` ≤ ~120 rules / 40 KB — a new law must replace or merge with an old one when at the cap; session narration and per-panel changelogs go to `learnings-archive.md` (append-only, grep-only), never into `laws.md`. `decisions-summary.md` ≤ 15 KB. This file: only add a rule here if it applies to most tasks — otherwise it goes in `laws.md`.

**Environment:** the s&box editor + `sbox` MCP (`compile_status`, `read_console`, `camera_screenshot`) only exist on the user's Windows desktop. In a cloud session, edit + commit + push and say what needs verifying in-editor; don't claim compile/visual verification you couldn't run.

---

## Patch Notes — Track As You Ship

**After completing any meaningful change, append a one-line player-facing entry to `Assets/data/patchnotes-pending.json`** (the running list for the NEXT release; the `patch-notes` skill builds release notes from it).

- **Meaningful:** new features, balance changes, bug fixes the player would notice, visible polish. **Skip:** no-behavior refactors, comment edits, dev-only tooling, internal renames.
- **Format:** append to `entries`: `{ "category": "feature|balance|fix|polish|content", "line": "..." }`
- **Voice:** what the player notices, not what files changed. "Fixed starter selection yellow box bug" → "Starter selection no longer renders a giant yellow rectangle when picking a beast."
- If the file doesn't exist for a fresh cycle, create it with `target_version` = next planned version and empty `entries`.

---

## s&box Razor UI — CSS Quirks & Gotchas

> **⚠️ ENGINE MOVED AGAIN (Aug–Sep 2026) — rows below were measured on the OLD engine.** 26.09.01 rewrote UI rendering (shader gradients, gamma blending, real blur/drop-shadow, one rounded-rect path, big perf pass); **26.09.08 replaced Yoga with `Sandbox.Layout`** (new flex engine, `display: block/grid/inline/contents`, `position: fixed`, scrollbars, Tab focus traversal, `ScrollIntoView`); 26.09.15 switched to GPU text + the Painter renderer; 26.09.22 fixed shadow/filter layer clipping. Findings below marked **(26.09 — verify)** come from reading Facepunch's engine source (sbox-public @ 2026-09-24) and the news posts — **not yet confirmed in our editor.** Until a row is verified in-editor, keep writing the proven-safe way; when you verify one, update the row and drop the tag. Full research notes: `.claude/ui-knowledge/sbox-26-09-changes.md`.
>
> **Test these first (likely live regressions):**
> 1. **`<button>` now takes keyboard focus** (26.09.08, `Button` ctor sets `AcceptsFocus = true`; ~300 `<button>` tags in our razor). In-game, a focused panel with default `ButtonInput` routes keys to UI, so **clicking any button may kill game keybinds** (the AcceptsFocus row below) — and Space/Enter now "click" the focused button, which can double-fire with our `UiInput.ConfirmPressed()`. **Fix applied 2026-09-25 (verify):** `Code/UI/UiFocusGuard.cs` clears any non-text focus every frame (called from `GameHUD` + `MainMenu` `OnUpdate`); TextEntry keeps focus. Test: click a button, then press M / 1–6.
> 2. **Box model:** `Sandbox.Layout` sizes border-box internally — declared width may now INCLUDE borders, contradicting the 2026-07-05 padding-box measurement. Re-measure one bordered row.
> 3. **Scrollbars are opt-in** (`scrollbar-width` default draws nothing) — check scroll areas still show whatever scroll affordance we expect.

s&box has its own CSS engine. **26.06.03 rewrote the parser.** Now working (stop working around these): unitless `line-height` is a multiplier (see below), one bad rule no longer kills the whole stylesheet, `filter: none` / `transform: none` / `background: none` override base classes, `inset` shorthand, `word-break: break-word`, `transition-*` longhands, colors in the `background:` shorthand, `min()/max()/clamp()`, `:has()` with descendants, `currentColor`, `oklch()/lab()/hwb()`, `dvh/svh/lvh/dvw`, `overflow: auto`, opacity %, logical `margin/padding/inset-block/inline`, `flex-flow` + `font` shorthands, `image-rendering: crisp-edges`, `inherit/initial/unset/revert` (so `flex: unset` probably works — unverified; `flex: 0 0 auto` is the safe default), animation `ms` units.

**Reportedly supported now (26.09 — verify before relying on it):** `position: fixed` · `display: block/grid/inline/contents` · `transparent` in gradients · `radial-gradient` `circle`/`ellipse` + px stops · `conic-gradient` · `backdrop-filter` · `box-shadow: inset` · `::before`/`::after` · `isolation` · `background-clip: text` · `overscroll-behavior` · `scrollbar-width/-color` · `Panel.ScrollIntoView` · `Panel.TabIndex` / `FocusNext()`.

**Still NOT supported (confirmed in engine source):** `box-sizing` as a CSS property · `inline-flex` · **`object-position` in any form** (the property doesn't exist — keyword pairs "working" was the default centering) · `:focus-within` · `:first-of-type`/`:last-of-type`/`:nth-of-type` (use `-child`) · `repeating-linear-gradient()` · CSS `var()` / `--x` (SCSS `$vars` only) · `color-mix()` · `transform-style: preserve-3d` / `perspective` property (the `perspective()` transform function works; panels don't depth-sort) · `opacity()` inside `filter` (kills the whole declaration; other filter functions chain but apply in a FIXED order, not source order) · quotes in `url()` (use `url(@var)`) · `onmouseenter` (use `onmouseover`).

### Box model & sizing
- **⚠️ `line-height`:** unitless = font-size MULTIPLIER since 26.06.03 (`line-height: 24` on 24px text = 576px — this broke the whole UI on that update). Use `px` or a small multiplier (`1.3`). Fonts 30px+ need a line box ≥ font-size in px or text clips.
- **⚠️ Box model is PADDING-BOX (measured on Yoga, 2026-07-05 — 26.09 `Sandbox.Layout` is border-box internally; RE-MEASURE):** declared `width`/`height` INCLUDE padding; only borders add on top. A 200px card with 2px borders = `width: 196px` (padding already inside). Never subtract padding. Row math: sum(children + borders) + gaps = row width. Telltales of getting it wrong: dead slack after the last tile; overlapping text in a height-pinned tile.
- *The layout rows below were all found on Yoga (replaced 26.09.08). They're still the safe way to write layout, but may no longer be necessary — re-test before removing a workaround.*
- **Flex cards need `width` + `min-width` + `flex: 0 0 Npx`** (all three), and pin their visual children too — otherwise they shrink to a strip and text reflows one char per line.
- **`flex: 1` can fail with 3+ siblings** — use explicit widths.
- **`flex-wrap: wrap` miscalculates container height** — use explicit row containers.
- **`overflow: hidden` on a flex child can collapse it to zero**; with `text-overflow: ellipsis` it collapses text too. Avoid both on flex children.
- **`width: auto` doesn't shrink-wrap `position: absolute`** (26.09 — the new engine shrink-fits; verify) — give it a fixed-size box and put content in a `flex: 0 0 auto` child.
- **`transform: translate(-50%,-50%)` centering poisons flex-grow in every descendant** (`flex: 1 1 0` falls back to content width). Center modals with flex on the root (`display:flex; align-items/justify-content: center`) and `position: relative` on the modal.
- **Imperative `Style.Width = Length.Percent(x)` on an absolute child paints full width** — compute pixels (`parent.Box.Rect.Width * ScaleFromScreen * fraction`) and write `Length.Pixels`.

### Scroll containers
- **Items must be DIRECT children of the `overflow-y: scroll` element** — no intermediate wrapper. Pattern: parent `display:flex; flex-direction:column; overflow:hidden; height:Xpx;` → scroll child `flex:1 1 0; min-height:0; overflow-y:scroll;`.
- **Nested flex-row inside a scroll container collapses to zero width** — use a plain `width: 100%` block with margins instead.
- **Scroll clipping does NOT clip descendant `box-shadow`, `transform`, or overhanging absolute children** (26.09 — the Painter renderer draws children inside the parent's clip, so this is likely fixed; verify before using lifts/glows in scrolls). On scroll-grid cards: hover/selected = border + border-color + background only (no transform, no colored glow); badges sit inside card bounds (`top: 4px`, not `-7px`); headers above grids must be opaque (≥0.95 alpha) with `gap: 0` to the grid. Colored glows are fine outside scroll areas.

### Rendering & images
- **`filter` on `<img>` blurs pixel art** (bypasses `image-rendering: pixelated`). Use `opacity`, or a colored overlay on the wrapper. Same cause as `transform: scale()` blur.
- **`<img>` + `object-fit: contain` tiles at the edges on aspect mismatch** — always add `background-repeat: no-repeat`.
- **`object-position` doesn't exist in s&box** (confirmed in engine source) — drop it; to anchor an image, position its wrapper with flex alignment.
- **`radial-gradient`:** no shape word, percent stops only — `radial-gradient(rgba(...) 0%, rgba(0,0,0,0) 70%)`. (26.09 — shape keywords + px stops reportedly work now; verify before relying on them.)
- **Gradients:** use `rgba(...,0)` not `transparent` (26.09 — `transparent` reportedly fine now); prefer `to bottom`/`to right` over degree forms (`180deg` can render horizontal on narrow elements).
- **Absolutely-positioned bg layers paint BEHIND the parent's `background-color`** (old renderer — re-test under 26.09) — make the bg layer the first in-flow flex child instead (`.bg-scroll` pattern on Chat/Effects/Notifications/Radio popups).
- **Empty `<div>`s render as gray rectangles / scrollbar artifacts** — remove them.
- **Bare text in flex containers renders vertically** — always wrap text in an element.

### Animation
- **`@keyframes` play once on first mount** — re-showing a still-mounted panel won't replay. Use transitions toggled by a class (default hidden state + `.visible` override).
- **⚠️ `animation-delay` is killed by re-renders** — a delayed animation that hasn't started gets cancelled when the panel re-renders (busy panels re-render constantly). Bake staggers into keyframe percentages on full-length animations starting at t=0 (canonical: `lmRung0–5`/`lmRail0–5` in `MonsterRosterPanel.razor.scss`). Delayed transitions likely suffer the same.

### Razor / panel behavior
- **`Panel` subclasses need `BuildHash()`** including every field that affects rendering (`IsVisible`, selection, counts, `kbIndex`...) or the panel never re-renders.
- **`@if / else if` swapping whole icon+label blocks leaves ghost children** — compute `(icon, label, ...)` in code-behind and render ONE `<iconify>` + ONE `<span>` with interpolated attributes (see shop's `GetBuyButtonContent`).
- **`TextEntry` has no `onchange`** — `@ref` + `Text="@field"`, poll `.Text` in `Tick()` (see `FilterBar.razor`).
- **Hover tooltips flicker over inner elements** — `> * { pointer-events: none; }` on BOTH the hover target and the tooltip, and make the tooltip flush (`bottom: 100%`) with no gap.
- **UI mouse wheel = `protected override void OnMouseWheel(Vector2 delta)`** (not `Input.MouseWheel`; must be `protected`, `public` fails CS0507). Don't call base to consume the event.
- **⚠️ Never set `AcceptsFocus = true` on page panels** — after a click it suppresses `Input.Pressed` game-wide (all keybinds die). Panel keyboard input routes through GameHUD's TickInput. **26.09.08: `<button>` sets `AcceptsFocus = true` itself** — see "Test these first" above; handled globally by `UiFocusGuard` (clears non-text focus each frame) — any new top-level UI host must call `UiFocusGuard.Tick()` too.
- **Custom fonts must sit directly in `Assets/fonts/`** (no subfolders); register `Exo2 { font-family: url("fonts/Exo2-Bold.ttf"); }`, use the embedded family name.
- **`Log.Info(multiLineString)` prints only the first line** — log each line separately.
- **Build `dev_*` ConCmds that freeze UI states** for inspection (e.g. `dev_fusefx gather|bind|cocoon|play|reveal|off`) — MCP round-trips are too slow to catch fast animations mid-flight.
