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

## Values and properties

- **`background: none` unsupported.** Use `background-color: transparent`.
- **`flex: unset` unsupported.** Use `flex: 0 0 auto`. Parser throws "expected a float or length".
- **`border-style: dashed` unsupported.** Use `solid`.
- **`text-overflow: ellipsis` + `overflow: hidden` on text inside flex containers** collapses the element. Avoid this combo inside flex.
- **URL quotes in `background-image` inline styles** not supported. Use `url(@variable)` not `url('@variable')`.

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
- **CSS `@keyframes` in SCSS files DO work** — use these liberally for UI particles, pulses, transitions. This is the preferred juice tool.

## When in doubt

- **s&box has an LLM documentation index at `https://sbox.game/llms.txt`.** WebFetch it when stuck on s&box-specific APIs or CSS behavior. Docs may be outdated — verify against actual project code when something feels off.
- **Test empirically.** s&box CSS behavior is not a 1:1 match with browsers. If a rule behaves unexpectedly, try alternatives and add findings to `learnings.md`.
