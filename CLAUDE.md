# Beastborne - Claude Guidelines

s&box (Razor + SCSS + C#) monster-taming game. Project file is `megarougelite.sbproj` (a relic name — the game is Beastborne). Game/systems overview: `DEV_ONBOARDING.md`.

## Keep context lean — how the knowledge files work

This file loads on EVERY turn, so it holds only rules that apply to nearly every task. Everything else loads on demand:

| Need | Read |
|---|---|
| UI engine laws + conventions (short, always read for UI work) | `.claude/ui-knowledge/laws.md` |
| Canonical style spec | `.claude/ui-knowledge/guiding-star.md` |
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

s&box has its own CSS engine. **26.06.03 rewrote the parser.** Now working (stop working around these): unitless `line-height` is a multiplier (see below), one bad rule no longer kills the whole stylesheet, `filter: none` / `transform: none` / `background: none` override base classes, `inset` shorthand, `word-break: break-word`, `transition-*` longhands, colors in the `background:` shorthand, `min()/max()/clamp()`, `:has()` with descendants, `currentColor`, `oklch()/lab()/hwb()`, `dvh/svh/lvh/dvw`, `overflow: auto`, opacity %, logical `margin/padding/inset-block/inline`, `flex-flow` + `font` shorthands, `image-rendering: crisp-edges`, `inherit/initial/unset/revert` (so `flex: unset` probably works — unverified; `flex: 0 0 auto` is the safe default), animation `ms` units.

**Still NOT supported:** `box-sizing` (any value) · `position: fixed` · `display: block` / `inline-flex` (only `flex`/`none`) · `transparent` inside gradients · `radial-gradient` shape keywords / px stops · `object-position` % pairs · `box-shadow: inset` · `:focus-within` · `:first-of-type`/`:last-of-type`/`:nth-of-type` (use `-child`) · `repeating-linear-gradient()` · quotes in `url()` (use `url(@var)`) · `onmouseenter` (use `onmouseover`).

### Box model & sizing
- **⚠️ `line-height`:** unitless = font-size MULTIPLIER since 26.06.03 (`line-height: 24` on 24px text = 576px — this broke the whole UI on that update). Use `px` or a small multiplier (`1.3`). Fonts 30px+ need a line box ≥ font-size in px or text clips.
- **⚠️ Box model is PADDING-BOX:** declared `width`/`height` INCLUDE padding; only borders add on top. A 200px card with 2px borders = `width: 196px` (padding already inside). Never subtract padding. Row math: sum(children + borders) + gaps = row width. Telltales of getting it wrong: dead slack after the last tile; overlapping text in a height-pinned tile.
- **Flex cards need `width` + `min-width` + `flex: 0 0 Npx`** (all three), and pin their visual children too — otherwise they shrink to a strip and text reflows one char per line.
- **`flex: 1` can fail with 3+ siblings** — use explicit widths.
- **`flex-wrap: wrap` miscalculates container height** — use explicit row containers.
- **`overflow: hidden` on a flex child can collapse it to zero**; with `text-overflow: ellipsis` it collapses text too. Avoid both on flex children.
- **`width: auto` doesn't shrink-wrap `position: absolute`** — give it a fixed-size box and put content in a `flex: 0 0 auto` child.
- **`transform: translate(-50%,-50%)` centering poisons flex-grow in every descendant** (`flex: 1 1 0` falls back to content width). Center modals with flex on the root (`display:flex; align-items/justify-content: center`) and `position: relative` on the modal.
- **Imperative `Style.Width = Length.Percent(x)` on an absolute child paints full width** — compute pixels (`parent.Box.Rect.Width * ScaleFromScreen * fraction`) and write `Length.Pixels`.

### Scroll containers
- **Items must be DIRECT children of the `overflow-y: scroll` element** — no intermediate wrapper. Pattern: parent `display:flex; flex-direction:column; overflow:hidden; height:Xpx;` → scroll child `flex:1 1 0; min-height:0; overflow-y:scroll;`.
- **Nested flex-row inside a scroll container collapses to zero width** — use a plain `width: 100%` block with margins instead.
- **Scroll clipping does NOT clip descendant `box-shadow`, `transform`, or overhanging absolute children.** On scroll-grid cards: hover/selected = border + border-color + background only (no transform, no colored glow); badges sit inside card bounds (`top: 4px`, not `-7px`); headers above grids must be opaque (≥0.95 alpha) with `gap: 0` to the grid. Colored glows are fine outside scroll areas.

### Rendering & images
- **`filter` on `<img>` blurs pixel art** (bypasses `image-rendering: pixelated`). Use `opacity`, or a colored overlay on the wrapper. Same cause as `transform: scale()` blur.
- **`<img>` + `object-fit: contain` tiles at the edges on aspect mismatch** — always add `background-repeat: no-repeat`.
- **`object-position`:** keyword pairs (`center top`) work; % pairs don't.
- **`radial-gradient`:** no shape word, percent stops only — `radial-gradient(rgba(...) 0%, rgba(0,0,0,0) 70%)`. No CSS halftone possible; ship a PNG/WebP.
- **Gradients:** use `rgba(...,0)` not `transparent`; prefer `to bottom`/`to right` over degree forms (`180deg` can render horizontal on narrow elements).
- **Absolutely-positioned bg layers paint BEHIND the parent's `background-color`** — make the bg layer the first in-flow flex child instead (`.bg-scroll` pattern on Chat/Effects/Notifications/Radio popups).
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
- **⚠️ Never set `AcceptsFocus = true` on page panels** — after a click it suppresses `Input.Pressed` game-wide (all keybinds die). Panel keyboard input routes through GameHUD's TickInput.
- **Custom fonts must sit directly in `Assets/fonts/`** (no subfolders); register `Exo2 { font-family: url("fonts/Exo2-Bold.ttf"); }`, use the embedded family name.
- **`Log.Info(multiLineString)` prints only the first line** — log each line separately.
- **Build `dev_*` ConCmds that freeze UI states** for inspection (e.g. `dev_fusefx gather|bind|cocoon|play|reveal|off`) — MCP round-trips are too slow to catch fast animations mid-flight.
