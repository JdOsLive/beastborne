# Beastborne - Claude Guidelines

## Patch Notes — Track As You Ship

**After completing any meaningful change, append a one-line player-facing entry to `Assets/data/patchnotes-pending.json`.** This is the running list for the NEXT release.

**Why:** retroactive `git log` summarization at release time misses things on big updates — terse commit messages, too many commits, lossy summarization. Tracking AT ship time per feature is the only reliable approach. The `patch-notes` skill at release rolls pending → versioned and clears it, generating Discord markdown from the structured data with no summarization step.

**What counts as meaningful:** new features, balance changes, bug fixes the player would notice, polish that visibly changes how the game feels. Skip: refactors with no behavioral change, comment-only edits, dev-only tooling, internal renames.

**How to add an entry:** open `Assets/data/patchnotes-pending.json`, append to the `entries` array:
```json
{ "category": "feature|balance|fix|polish|content", "line": "Player-facing one-liner — write what they'll notice, not what files changed" }
```

Lines should read like patch notes a player would skim — concrete and concise. "Fixed starter selection screen yellow box bug" → "Starter selection no longer renders a giant yellow rectangle when picking a beast." Lead with the player's experience, not the technical cause.

If the pending file doesn't exist yet for a fresh release cycle, create it with `target_version` set to the next planned version and an empty `entries` array.

---

## AI Art Generation (PixelLab)

When the user asks for AI art prompts for any monster, follow these guidelines:

### Core Principle: Mythology & Description First

**Every beast should be based on or inspired by real-world mythology, folklore, or legends.** When creating new beasts, research the source myth and let it inform the creature's design, description, and visual identity.

**The monster's description in MonsterManager.cs is the primary source for visuals.**
- Read the description carefully
- Extract visual cues from the text (colors, forms, effects mentioned)
- The element is secondary - don't force element colors if the description implies something different
- If the description doesn't translate well visually, propose an update first

### Art Style
- **Resolution**: 128x128 pixel art
- **Format**: Sprite sheet with 4 frames for idle animation
- **Facing**: Left (all monsters face left for consistency)
- **Tool**: PixelLab (uses Description + Animation fields)
- **Background**: Dark/transparent background for sprites

### Prompt Structure

**Description field:**
```
[Physical form from description], [key visual features], [colors implied by description], [any magical effects], facing left, 128x128 pixel art, sprite sheet 4 frames idle animation, fantasy monster game art style, dark background
```

**Animation field:**
```
[Idle movement appropriate to creature type], [any effect animations], [secondary motion like tail/wings/wisps]
```

### Element Colors (Reference Only)

These are fallback suggestions if the description doesn't imply specific colors:

| Element | Suggested Colors | Common Effects |
|---------|------------------|----------------|
| Fire | Orange, red, yellow | Flames, embers |
| Water | Blue, cyan, teal | Bubbles, droplets |
| Earth | Brown, tan, gray | Rocks, dust |
| Wind | White, pale green | Swirls, gusts |
| Electric | Yellow, blue | Sparks, arcs |
| Ice | Light blue, white | Frost, crystals |
| Nature | Green, brown, pink | Leaves, vines |
| Metal | Silver, gray, rust | Gears, shine |
| Shadow | Purple, black | Dark wisps |
| Spirit | Pink, gold, white | Halos, glow |

**Important**: These are suggestions, not rules. A Fire monster described as "black flames" should be black, not orange.

### Evolution Lines

When a monster has EvolvesFrom/EvolvesTo:
1. Check all stages' descriptions
2. Ensure visual progression makes sense narratively
3. If descriptions don't connect well, propose updates before generating prompts

**Progression pattern:**
- Base: Smaller, simpler, cuter
- Mid: Larger, more defined, element more visible
- Final: Majestic/powerful, complex details

### Description Quality Check

Before generating prompts, verify the description works visually:

**Good descriptions include:**
- Physical form hints (ghostly, bird-like, veiled, crystalline)
- Color/material cues (golden, cream, translucent, prismatic)
- Behavioral hints that suggest movement (drifts, floats, crawls)

**Bad descriptions need updating:**
- Too abstract ("keeper of the hour before existence")
- No physical form implied
- Contradicts evolution line visually

### Workflow for Any Monster

1. **Read** the description in MonsterManager.cs
2. **Check** for evolution line (EvolvesFrom/EvolvesTo)
3. **Evaluate** if description translates to visuals
4. **If poor fit**: Propose updated description, get approval, update code
5. **Generate** Description + Animation prompts based on the text
6. **Include**: 128x128, 4-frame idle, dark background

### Examples

**Haloveil** - Description drives everything:
> "When a Dawnmote gathers enough light, it condenses into a veiled spirit crowned by a golden halo."

Visual extraction:
- "veiled spirit" → flowing robes/veil
- "golden halo" → halo above head
- "condensed light" → warm glow, cream-gold colors

```
Description: A veiled ghostly spirit with flowing cream-gold robes, single golden halo floating above its head, trailing ribbon-like sash, ethereal angelic form, soft warm glow, facing left, 128x128 pixel art, sprite sheet 4 frames idle animation, fantasy monster game art style, dark background

Animation: Gentle floating drift, veil and robes billow softly, halo rotates slowly with subtle shimmer, trailing sash flows gracefully
```

**Solmara** - Description drives everything:
> "A radiant bird born from gathered dawn-light, crowned by rings of every color sunrise has ever worn."

Visual extraction:
- "radiant bird" → bird/phoenix form
- "rings of every color sunrise" → multiple colorful halos
- "dawn-light" → warm golden body with prismatic accents

```
Description: A radiant bird-phoenix spirit with elegant swan-like pose, multiple colorful halos/rings in pink orange and rainbow, prismatic wing feathers shimmering with unnamed colors, golden-cream body with luminous glow, facing left, 128x128 pixel art, sprite sheet 4 frames idle animation, fantasy monster game art style, dark background

Animation: Majestic slow wing movements, multiple halos rotate at different speeds, prismatic feathers shimmer and shift colors, radiant aura pulses gently
```

### Updating Descriptions

If the existing description doesn't work visually:
1. Propose updated description that keeps the spirit but adds visual clarity
2. Show how it connects to evolution line (if applicable)
3. Get user approval before changing MonsterManager.cs
4. Then generate prompts matching the new description

---

## s&box Razor UI — CSS Quirks & Gotchas

s&box uses a custom CSS engine that behaves differently from browsers. Keep these rules in mind:

| Issue | Details |
|-------|---------|
| **line-height must be extremely high** | For large font sizes (30px+), `line-height` needs to be 18–24+ or the text gets clipped. Normal values like 1.2 or 2 are not enough. Example: `font-size: 42px` needs `line-height: 24` to render fully. |
| **`overflow: hidden` collapses flex children** | Setting `overflow: hidden` on a flex child can cause it to shrink to zero width/height, making content invisible. Avoid it on flex children. |
| **Scroll containers need flat children** | s&box cannot correctly calculate scroll height when a scrollable container has nested flex containers. Scrollable items must be **direct children** of the `overflow-y: scroll` element — do NOT wrap them in an intermediate div. Follow the roster-grid pattern: parent with `display: flex; flex-direction: column; overflow: hidden; height: Xpx;` and scroll child with `flex: 1 1 0; min-height: 0; overflow-y: scroll;` with items as direct children. |
| **`flex-wrap: wrap` miscalculates height** | The container won't compute its height correctly when children wrap. Use explicit row containers instead (e.g., two `.stats-row` divs instead of one flex-wrap grid). |
| **Bare text renders vertically** | Text not wrapped in a `<span>` or other element inside flex containers may render character-by-character vertically. Always wrap text in elements. |
| **`flex: 1` can fail with multiple siblings** | When a flex container has 3+ children, `flex: 1` may not distribute space correctly. Use explicit `width` values instead. |
| **`Log.Info(multiLineString)` silently truncates after first newline** | `Log.Info(sb.ToString())` where `sb` was built with `.AppendLine()` ends up printing as a blank `Generic` line in the s&box console — only the first line (the empty leading newline) gets through. **Fix**: log each line separately via individual `Log.Info(...)` calls instead of building a multi-line StringBuilder. Verified 2026-05-06 — broke both BeastbookEdit and SlotEdit "save → console" output. |
| **CSS `filter` on `<img>` bypasses `image-rendering: pixelated`** | Any `filter` (`grayscale(1)`, `brightness(0)`, `blur(N)`, etc.) on an `<img>` element rasterizes the bitmap through the GPU compositor, which falls back to bilinear smoothing regardless of `image-rendering: pixelated`. Symptom: pixel-art sprites with a grayscale/dim/silhouette treatment look soft/blurry while sharp-rendered siblings look crisp. **Workarounds:** (1) use `opacity: N` alone — opacity does NOT trigger the compositor rasterize path; (2) for "tinted" looks, apply a colored overlay div or `background-color` to the wrapper instead of `filter: brightness(...)` on the img; (3) for silhouettes where the shape is the point, accept the softness (you can't tell a black bitmap is blurry). Same root cause as CSS `transform: scale()` blur on pixel art — anything that goes through the compositor smooths the source. |
| **`transform: translate(-50%, -50%)` on a flex parent BREAKS flex-grow width resolution in descendants** | A modal centered with `position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%)` poisons every flex chain inside it — `flex: 1 1 0` children fall back to content-min-width instead of growing to fill remaining space. Symptom: a `flex: 1 1 0; min-width: 0` column inside a row-flex sibling-of-fixed-width-pair sizes itself to ~content width (e.g., to whatever a FilterBar's intrinsic shrink-fit width is, ~640px) regardless of how much space the parent actually has. The `flex-wrap: wrap` grid inside it then only fits N-1 cards per row even when N would mathematically fit. Verified 2026-05-06 on TeamPickerPopup — drove 6+ hours of debugging that tried every CSS workaround imaginable; root cause was a single line on the modal ancestor. **Fix**: use FLEX CENTERING on the popup's root instead — the root becomes `display: flex; align-items: center; justify-content: center`, the modal switches from `position: absolute + transform: translate` to `position: relative` (no transform). Removes the transform from the entire ancestor chain, lets flex children resolve widths normally. |
| **`inline-flex` not supported** | Use `display: flex` only. |
| **`background: none` not supported** | Use `background-color: transparent` instead. |
| **`position: fixed` not supported** | Runtime log: "Generic fixed is not valid with position". Only `static`/`relative`/`absolute` parse. For viewport-pinned overlays (drag shields, modals), use `position: absolute` against a panel root that already covers the viewport — e.g. anchor with `top/left/right/bottom: 0` on a fullscreen-sized parent. |
| **URL quotes in `background-image` not supported** | Use `url(@variable)` not `url('@variable')` in inline styles — s&box doesn't need quotes around URLs. |
| **Empty divs render as visible panels** | Empty `<div>` elements may render as gray rectangles or scrollbar artifacts. Remove wrapper divs that have no content. |
| **`text-overflow: ellipsis` with `overflow: hidden`** | This combination can collapse the element in s&box flex layouts. Avoid using `overflow: hidden` on text elements inside flex containers. |
| **Custom fonts must be in `Assets/fonts/` root** | s&box only discovers font files placed **directly** in `Assets/fonts/` — NOT in subdirectories. Place TTF files like `Assets/fonts/Exo2-Bold.ttf`, not `Assets/fonts/Exo2/Exo2-Bold.ttf`. Register in SCSS with `Exo2 { font-family: url("fonts/Exo2-Bold.ttf"); }` and use `font-family: Exo2;` (the embedded font family name from TTF metadata, no space). Resources in `.sbproj` must include `fonts/*`. |
| **`flex: unset` not supported** | s&box CSS parser cannot parse `flex: unset` — throws "expected a float or length". Use `flex: 0 0 auto` instead. |
| **`inset` shorthand not supported** | s&box CSS parser rejects `inset: 5px` ("Generic 5px is not valid with inset"). Expand to `top/left/right/bottom` individually. |
| **`box-shadow: inset ...` not supported** | s&box rejects any box-shadow with the `inset` keyword. For recessed/embossed effects, use a darker `background-color` + border, or layer a nested absolutely-positioned div with a gradient. Outer drop shadows work fine. |
| **`:focus-within` not supported** | s&box CSS parser throws "Unsupported Pseudo Class `focus-within`". Use `:hover` as a fallback for search-input-style styling, or track focus state manually via `@ref` + code-behind. |
| **`:first-of-type` / `:last-of-type` / `:nth-of-type` not supported — and the parser fails the WHOLE STYLESHEET** | s&box logs "Unsupported Pseudo Class `first-of-type`" and **stops parsing the entire .scss file** — every panel that uses it renders unstyled (collapses to nothing in flex layouts). `:first-child` / `:last-child` / `:nth-child(N)` ARE supported. Use those, or rework the rule (e.g. swap `border-top` + `&:first-of-type { border: 0 }` for plain `border-bottom`). The "stylesheet bails entirely on one bad selector" failure mode is the most dangerous — a single `:first-of-type` in a 3000-line panel SCSS makes the whole panel invisible. |
| **`TextEntry` has no `onchange` — use `@ref` + poll** | s&box `TextEntry` doesn't accept an `onchange` handler (binding throws `CS8974 Converting method group ... to non-delegate type 'object'`). Pattern: `@ref="searchInput"` + `Text="@searchQuery"` and poll `searchInput.Text` in `Tick()` to sync the backing field. See `FilterBar.razor` / `GuildPanel.razor` for the canonical pattern. |
| **Flex cards need explicit width + `flex: 0 0 Npx`** | Cards inside flex rows MUST declare `width`, `min-width`, AND `flex: 0 0 Npx` (or `flex-shrink: 0`). Without all three, s&box shrinks them to zero, the content collapses to a thin strip, and text inside reflows one character per line. Pin the card and all its visual children (`.item-compartment`, `.item-nameplate`) explicitly. |
| **Nested flex-row inside scroll container collapses width** | A flex-row wrapper (e.g. `.section-label-row`) that is a direct child of an `overflow-y: scroll` area will collapse to zero width in s&box — children with `flex: 1 1 0` don't distribute, text inside wraps one character per line. Fix: avoid nested flex inside scroll containers. Use a plain block with `width: 100%` and lay out children with margins, not a nested flex row. |
| **`Panel` subclass needs `BuildHash()` to react to static state** | If you flip a `public static bool IsVisible` to show/hide a panel, the framework only re-renders when `BuildHash()` changes. Always override `BuildHash()` and include every field that affects rendering — `IsVisible`, selection state, current category, counts, etc. Missing BuildHash = panel never reacts to `Show()/Close()/Toggle()` calls. |
| **`repeating-linear-gradient()` not supported** | s&box logs "Unknown Image Type 'repeating-linear-gradient(...)'" and skips the background. Use a regular `linear-gradient()` or a solid color + border for hatched/diagonal patterns. |
| **CSS `@keyframes` animations only play once on first mount** | `animation: foo 0.3s ease-out` on a panel element plays when the element first enters the DOM. Re-opening the panel (toggling `IsVisible`) does NOT replay the animation because the element stays in the DOM. **Use CSS transitions instead** — set the default state (`opacity: 0; transform: scale(0.88);`) on the element and override in the `.visible` parent selector. Transitions re-fire every time the class toggles. |
| **`border-left` / `border-right` / etc. shorthand with style word WORKS** | `border-left: 3px solid rgba(...)` parses correctly in s&box — the per-side shorthand with a style keyword and rgba color is fine. Confirmed 2026-04-15; MonsterRosterPanel uses it throughout for section accent bars. Earlier entries claiming it fails were wrong. |
| **`transparent` keyword inside `linear-gradient()` fails** | s&box logs "Unrecognised part transparent in background" + "Unknown Image Type" when a gradient stop uses the `transparent` keyword (e.g. `linear-gradient(90deg, transparent, ...)`). Use `rgba(255, 255, 255, 0)` (or any zero-alpha color) explicitly instead. |
| **`background: rgba(...)` shorthand treated as image** | s&box logs "Unknown Image Type rgba(...)" when you use `background:` shorthand with an rgba value (it tries to parse as `background-image`). Always use `background-color: rgba(...)` explicitly for colors. Only use `background:` for actual image/gradient values. |
| **Absolutely-positioned bg layers render BEHIND the parent `background-color`** | An `absolute` child with `top/left/right/bottom: 0` (or explicit `width/height: 100%`) intended as a background overlay paints **behind** the parent's solid `background-color` fill in s&box, so it's invisible. Standard CSS stacks it above. Fix: make the bg layer a plain in-flow flex child (no `position: absolute`) as the first child of the panel — matches the working `RadioWidget` pattern. See the `.bg-scroll` pattern on Chat/Effects/Notifications/Radio popups. |
| **`filter: none` is rejected — use `filter: brightness(1)`** | Runtime log: "Generic none is not valid with filter". The keyword `none` does not parse as a valid filter value. When a child variant needs to CANCEL a parent's hover filter (e.g. `.bb-pill:hover { filter: brightness(1.1) }` shouldn't apply to `.personality` pills), override with the identity function `filter: brightness(1)` instead of `filter: none`. Same trick applies to cancelling `grayscale`, `blur`, etc. — use the identity value (`grayscale(0)`, `blur(0)`), never `none`. |
| **Scroll containers do NOT clip descendant `box-shadow`, `transform`, or edge-overhanging children** | `overflow-y: scroll` (and `overflow: hidden`) on a grid/list container does NOT clip: (1) descendant `box-shadow` — colored OR dark, both leak past the clip, (2) descendant `transform: translateY/translateX` — a `.selected` card with `translateY(-4px)` teleports out of the scroll clip and renders in the header area, (3) absolutely-positioned descendants that overhang the card (e.g. `.mini-fav { top: -7px }`, `.mini-badge-row { top: -7px; left: -3px }`) render past the scroll viewport. **Rules:** hover/selected states on scroll-grid cards = border-width + border-color + background only; NO transform, NO box-shadow; overhanging badges must sit INSIDE the card bounds (`top: 4px`, not `top: -7px`); headers above scroll grids must be OPAQUE (`rgba(..., 0.95+)`) to cover unavoidable leaks; use `gap: 0` (not `gap: 16px`) between the header and the grid so there's no transparent gap region for leaks to occupy. |
| **UI mouse-wheel goes through `Panel.OnMouseWheel(Vector2)`, NOT `Input.MouseWheel`** | `Input.MouseWheel` is the GAME INPUT channel (weapon switch, etc) and does not carry UI panel wheel events — polling it in `Tick()` does nothing for wheel-over-UI. The correct UI API is to override `OnMouseWheel( Vector2 delta )`. `delta.y` is the scroll amount (positive = toward user). Return without calling `base.OnMouseWheel(delta)` to CONSUME the event so it doesn't fall through to a parent scroll container. Scope to a child element by tracking `_isMouseOverFoo` via `onmouseover`/`onmouseout` handlers. **Access modifier:** on a Razor panel (`.razor`) the base member is `protected internal` and lives in another assembly — so the `internal` half is invisible here and the override MUST be declared `protected override void OnMouseWheel(...)`. `public override` fails with CS0507 ("cannot change access modifiers"). |
| **`display: block` is rejected — use `display: flex`** | Runtime error: "Generic block is not valid with display". s&box's CSS parser only accepts `flex` and `none` for `display`. When toggling visibility via class swap (e.g. `.has-item .foo { display: flex } .no-item .foo { display: none }`), use `flex` for the visible state. `flex` behaves identically to `block` for single-child wrappers, so there's no layout cost to the swap. |
| **`linear-gradient(180deg, ...)` can misparse as a left→right sweep** | For vertical gradients (especially on wide/short elements like scroll-fade bars), the `180deg` degree form can render as horizontal in s&box. Use the `to bottom` / `to top` keyword form instead — it's unambiguous. `to right` / `to left` for horizontal is also reliable. Degree forms work for most cases but fail unpredictably on narrow-vertical geometries. |
| **Razor `@if / else if / else` cascades swapping whole icon+label blocks leave ghost children** | When a button's markup uses `@if IsA { <iconify A/> <span>X</span> } else if IsB { <iconify B/> <span>Y</span> } else { <iconify C/> <span>Z</span> }`, s&box's re-render diff can leave a stale `<iconify>` in the DOM during rapid state changes — both icons render simultaneously (doubled-icon bug seen on spam-click). **Fix:** compute the icon + label in code-behind as a tuple (`(string icon, string label, string color, int size) GetContent(...)`), then render ONE `<iconify>` + ONE `<span>` with interpolated attribute values. s&box swaps attribute values cleanly; it only trips on child-slot swaps. See shop's `GetBuyButtonContent` for the canonical pattern. |
| **`radial-gradient` requires PERCENT stops only — and rejects all shape keywords** | Two distinct parser failures stack on this property: (1) shape keywords — `radial-gradient(circle, ...)` and `(ellipse, ...)` are BOTH rejected ("Generic Cannot read a color from 'circle'" / "'ellipse at X% Y%'") — the parser reads the shape word as a color and fails. Drop the shape word entirely. (2) Stop positions — only percent values are accepted ("Generic Only percent stop values are supported: 'rgba(...) 1.5px'"). Pixel-based stops (e.g. for tight halftone dot patterns: `rgba(...) 1.5px, rgba(0,0,0,0) 2px`) all fail. **Working syntax:** `radial-gradient(rgba(...) 0%, rgba(0,0,0,0) 70%)` — soft wash only. **There is no working CSS halftone dot pattern** — for actual halftone visuals, ship a PNG/WebP asset. For warm corner washes / soft glows, the percent-stop form is fine. |
| **`object-position` rejects percentage pairs — use keyword pairs or drop it** | `object-position: 50% 25%` fails ("Generic 50% 25% is not valid with object-position"). However, keyword pairs (`center top`, `left bottom`, `center center`) DO work. Prefer keyword values when you need non-default positioning. If default centering is acceptable, drop `object-position` entirely and rely on `object-fit: contain`. To bottom-anchor a contained image, use a parent `display: flex; align-items: flex-end;` with a transform-Y offset on the wrapper. |
| **`<img>` + `object-fit: contain` tiles at the edges when aspect ratios mismatch** | Against the HTML spec, s&box renders large `<img>` elements through a texture path that defaults to `repeat` when the source aspect doesn't match the container — a ghost duplicate of the image appears at the far edge (observed on the main-menu featured portrait rendering Wispryn, and in evolution-line panels that show large monster art in non-matching containers). **Fix:** add explicit `background-repeat: no-repeat;` to any `<img>` that uses `object-fit: contain` in a container with a different aspect ratio. Cheap insurance — put it on every such `<img>` preemptively. |
| **Scrollable grid cards cannot use colored outer glow halos — they leak past the scroll viewport** | `overflow-y: scroll` (and `overflow: hidden`) on a parent does NOT clip descendant `box-shadow` with large blur radii — a `.selected` card with `box-shadow: 0 0 24px rgba(139, 92, 246, 0.45)` will leak a purple blob onto the page above/below the viewport. Same for colored halos on owned/equipped/featured states. **Fix:** use border + bg tint + translateY for selection feedback. Dark drop shadows (`0 4px 14px rgba(0, 0, 0, 0.5)`) don't leak as noticeably; colored halos always do. If a card is OUTSIDE a scroll (e.g. in the detail sidebar), colored glows are fine. |
| **`word-break: break-word` rejected — just use `white-space: normal`** | Runtime log: "break-word is not valid with word-break". `break-word` is a legacy webkit value; only `normal` / `break-all` / `keep-all` parse. For most text-wrapping needs, `white-space: normal` alone wraps at word boundaries correctly. `overflow-wrap: break-word` is the standard alternative for forcing mid-word breaks on long unbreakable strings, but not verified in s&box — avoid unless needed. |
| **`transition-delay` silently dropped — use `animation-delay` or skip** | Runtime log: "Didn't handle transition style: transition-delay". The property is ignored by the parser, so staggered `transition` reveals won't work via delay. `animation-delay` on `@keyframes` DOES work but only fires on first mount — combine with a render-key class (`foo-@version`) to replay on state changes. For simple state-change staggers without remount plumbing, drop the stagger and accept simultaneous reveal. |
| **Hover tooltips flicker when cursor moves over inner text/icons — children must be `pointer-events: none`** | If a hover-target pill (e.g. trait pill, stat chip, skill node) contains inner `<span>`s, `<iconify>` icons, or a child tooltip body, moving the cursor from the pill's chrome onto its own inner text fires `onmouseout` on the pill (because the cursor is now hit-testing a "different" element from the pill chrome). Same for the cursor moving from the pill into the tooltip body — the inner divs of the tooltip catch the cursor and break the parent's `:hover` state. **Fix:** add `> * { pointer-events: none; }` to BOTH the hover-target pill AND the tooltip itself, so every child element passes the cursor through to its parent. Combine with `bottom: 100%` (or `top: 100%`) for the tooltip's edge to sit FLUSH against the pill — any visual gap is a "void" the cursor crosses, breaking the hover. Canonical pattern: `Code/UI/Panels/SkillTreePanel.razor.scss` `.hex-icon { pointer-events: none; }`. Same fix applied to BeastiaryPanel trait tooltips 2026-05-13. |
| **`box-sizing: border-box` is rejected — s&box is content-box only** | Runtime log: "Generic border-box is not valid with box-sizing". s&box's CSS parser does not accept `border-box` (nor any non-default `box-sizing` value) — the box model is always content-box. A declared `width`/`height` is the CONTENT box; borders and padding add on top. Account for borders manually: a card that must occupy exactly 200px of layout space with a 2px border on each side needs `width: 196px`. This compounds with the "flex cards need explicit width" rule — pin `width`, `min-width`, `flex: 0 0 Npx`, and subtract the horizontal border total from each. |
| **`width: auto` does NOT shrink-wrap an absolutely-positioned element** | An `position: absolute` element with no `width` (and only `right`/`top` set, or `left`+`right` both unset) does not shrink-fit its content in s&box the way browsers do — it resolves to a degenerate or full-parent width instead of hugging the content. **Fix:** give the absolute element a fixed-size container (explicit `width`/`height`) and put the real content inside as a `flex: 0 0 auto` child, which sizes to content predictably. Don't rely on `width: auto` content-hugging on anything `position: absolute`. |

---

## Discord Patch Notes Style Guide

When writing patch notes for Discord announcements, follow this format:

### Structure
```
# 🎮 BEASTBORNE [VERSION] - [UPDATE NAME]

[One-liner hook or tagline]

---

## ⚔️ [MAJOR FEATURE 1]
[Brief description of the feature]

- **[Sub-feature]** — [Description]
- **[Sub-feature]** — [Description]

## 🎒 [MAJOR FEATURE 2]
[Brief description]

- **[Sub-feature]** — [Description]

## 🔧 Improvements & Fixes
- [Fix or improvement]
- [Fix or improvement]

---

*Thank you for playing Beastborne! Join our Discord: [link]*
```

### Discord Markdown Reference
- `# Header` — Large header (only works in forum posts/announcements)
- `## Subheader` — Medium header
- `**bold**` — Bold text
- `*italic*` — Italic text
- `__underline__` — Underlined text
- `~~strikethrough~~` — Strikethrough
- `> quote` — Block quote
- `- item` — Bullet point
- `---` — Horizontal divider
- `` `code` `` — Inline code
- Use emojis liberally for visual appeal

### Tone Guidelines
- Exciting but concise
- Lead with the biggest features
- Use action verbs (Added, Improved, Fixed)
- Keep bullet points to one line when possible
- End with community call-to-action

### Emoji Conventions
| Category | Emoji |
|----------|-------|
| Combat/Battle | ⚔️ |
| Items/Inventory | 🎒 |
| Skills/Abilities | ✨ |
| Monsters/Beasts | 🐉 |
| Fixes/Polish | 🔧 |
| New Content | 🆕 |
| Balance | ⚖️ |
| UI/UX | 🎨 |
| Performance | ⚡ |
| Warning/Important | ⚠️ |

---

## Animated Icon Workflow (SVG → WebP)

s&box does NOT support animated SVGs or CSS `@keyframes`. To create animated icons:

1. **Create animated SVGs** with CSS `@keyframes` animations (user does this manually or with a tool)
2. **Place animated SVGs** in `Assets/ui/icons/animated/` with `-animated.svg` suffix
3. **Convert to animated WebP** using Playwright (headless Chromium renders CSS animations frame-by-frame):
   - Install: `pip install playwright Pillow && python -m playwright install chromium`
   - Load each SVG inline in a headless browser page
   - Screenshot each frame at 50ms intervals (20fps) with transparent background
   - Stitch frames into lossless animated WebP using Pillow
   - Output at **128x128px** resolution for quality when scaled down
4. **Reference the `.webp` files** in Razor, NOT the `.svg` files
5. **CSS hover swap pattern**: Both static SVG and animated WebP `<img>` tags sit in the same container. The animated one is `position: absolute; opacity: 0;` and becomes `opacity: 1;` on `:hover`. No state management needed — pure CSS.

### Button icons still need animated SVGs
The 7 bottom-bar button icons (menu, inventory, chat, effects, music, settings, notification) still have Pillow-generated placeholder WebPs. When ready, create animated SVGs for these and convert them using the same Playwright workflow above. Their static SVGs are in `Assets/ui/icons/buttons/`.
