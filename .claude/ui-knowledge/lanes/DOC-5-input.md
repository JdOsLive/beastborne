# DOC-5 — INPUT (key grammar · one-cursor model · hover laws · routing patterns)

**Charter.** This lane owns how the player drives a page: the canonical key map, the
one-cursor / one-device model, ring-vs-slab cursor costumes and their geometry, hover laws,
`TickInput`/`UIModalState`/`NavManager` routing, and hit-testing. It is a LENS — canonical
truth lives in `learnings-archive.md` (§Input/keyboard + the dated one-cursor entries),
`Code/UI/UIModalState.cs`, `Code/UI/UiInput.cs`, `guiding-star.md` (§Focus ring), and memory
`project_keyboard_nav.md`. Sources win; flag drift.

## THE CHECKLIST
1. **Key grammar (final form)**: WASD/arrows navigate (seams walk into filter bars/tabs) · **Space + E** confirm (`UiInput.ConfirmPressed()`; E silent, consumed earlier where it has a job) · **Q** (`Menu`) backs out of EVERYTHING (set `GameHUD.PanelHandledBackKey` on consume) · **R** = the page's cycle/rotate where R has no bigger job (shown on the legend cap) · **Z/X** (`CyclePrev/Next`) = universal SILENT cycle pair and fallback where R is taken · M phone · number keys = slots/tabs. [learnings 2026-07-12 INPUT GRAMMAR — FINAL FORM + CYCLE-KEY GRAMMAR]
2. Never bind new `Escape` handlers (engine-reserved); confirm is `Jump`/`Enter`, never `attack1` (mouse1 double-fire). [learnings 2026-05-17 canonical map]
3. **NEVER set `AcceptsFocus = true` on a page panel** — one click kills `Input.Pressed` game-wide. Panel keyboard flows through GameHUD TickInput routing, not engine focus. [CLAUDE.md AcceptsFocus row; learnings 2026-07-12]
4. `Input.Pressed` is global + edge-triggered: every blocking popup registers in `UIModalState.TopModal` (priority order, ConfirmDialog highest) and gates its own keys on `IsTopModal("Name")`; dual-mounted panels expose a static `AnyVisible`. Paint z-order and modal-order are TWO independent ladders — an "over the phone" surface must climb both. [learnings 2026-05-17 routing; 2026-08-29 popup-over-PawPad]
5. **One-device flag, three legs**: a single `lastInputDevice` enum → root `.kbd-mode`/`.mouse-mode` class; root `onmousemove` (guarded) flips to mouse; EVERY owned `:hover` is gated under mouse-mode. Do NOT flip device on `onmouseover` (kb scroll fires synthetic hovers under a parked cursor). [learnings 2026-07-06 MENU DEVICE-MODE CONTRACT]
6. **One-cursor model — hover IS selection**: mouse hover moves the SAME indexes the keyboard walks; `.kb-focused` is the sole cursor class in both modes; cursor rows carry NO separate hover styling. Every cursor field goes in `BuildHash()`. [learnings 2026-07-06 FULL ONE-CURSOR MODEL; 2026-05-17 kbIndex pattern]
7. **Ring vs slab costumes**: the traveling violet ring needs ≥12px clearance on every side; dense rows inside scroll containers wear the SLAB cursor (`.kb-focused` border/edge recolor + fill lift, border/bg only). Handoff: ring fades 0.12s entering a slab lane; reappears SNAPPED leaving it. [online-motion-spec.html §1; learnings 2026-08-28 two-costumes]
8. Slab-cursor rows with no outline: a zero-alpha 3–4px `border-left` seam that flips to `#9b6cff` (declared width + border = occupied width; no geometry shift). On a 512px dark slab a 2px spread-shadow ring is NOT a cursor — rows need the row highlight; accent-filled controls near cursor-hue get the gap + outer ring form. Pick the recipe by the SURFACE. [learnings 2026-08-28 no-outline slab; 2026-08-30 phone playtest]
9. **Ring geometry**: real bordered child (never box-shadow), `border: 4px solid #9b6cff`, ring radius = host radius + inset (+10 standard); `L/T = host.Rect − inset + 4` (border width — Style.Left places the padding box), `W/H = rect + 2·inset`. One costume PER host radius; the ring wraps a real BUTTON inside any tile ≥ ~200px, never the tile. [guiding-star §Focus ring; learnings 2026-08-29 RING CENTERING LAW + costume entries]
10. A ring-bearing control never transforms on hover/active — feedback is brighten/dim only; hosts that pop scale must have the measured rect grown by the same factor before inset math (Box.Rect is pre-transform). [guiding-star §Focus ring; learnings 2026-08-29 ring adaptations]
11. **W/S flows continuously through zones** top-to-bottom as ONE cursor (skip empty zones; multi-row zones cross at true grid edges); Tab is reserved for escaping text inputs. Focus rules recolor EXISTING borders (reserve transparent ones) — never add border width. [learnings 2026-05-18 plaza rework + focus-border rule]
12. Reset a sub-panel's cursor on its OPEN EDGE (tracked bool in Tick), not the opening keystroke — modals open by mouse too. Conditionally-rendered button lists: TickInput builds the SAME live `@if` list in the SAME order as markup. [learnings 2026-05-18]
13. Kb scroll-follow: `UiGridScroll.ComputeIndexOffset` + per-tick 0.30 glide behind a pending flag (retry until the child has a rect); endpoints scroll fully to top/bottom so display-only content is reachable; a cursor leaving upward glides the grid to 0. [learnings 2026-08-29 portable scroll-follow; 2026-07-06 endpoint semantics]
14. Destructive actions: hold-to-confirm or two-stage; every confirm modal has a kb branch with the cursor SEEDED ON CANCEL; a stray Q must not fall through under the modal. [guiding-star §Components; learnings 2026-07-06 (c)]
15. Hover sounds: `onmouseover` + state guard (no `onmouseenter` in s&box); hover handlers IGNORE events while in kb mode. [memory feedback_sbox_hover_sound; learnings 2026-07-06]
16. `pointer-events` is `none`/`all` only — `auto` silently reads back None. Children of hover targets get `pointer-events: none` via a CLASS ON THE CHILD (bare element / `> *` selector forms fail). [CLAUDE.md pointer-events + tooltip rows]
17. Wheel = `protected override OnMouseWheel(Vector2)` (never `Input.MouseWheel`); consume by not calling base; route to the ACTIVE view's scroll ref, not a hardcoded one. [CLAUDE.md wheel row; learnings 2026-08-28 PawPad wheel audit]
18. TextEntry: `@ref` + poll in Tick (no onchange); any component that sets a global focus flag clears it on EVERY exit path (unmount, park, click-through) and blurs on backdrop taps. [CLAUDE.md TextEntry row; learnings 2026-08-30 phone-focus]
19. Routing: `NavManager.GoTo/Open(dest)` is the router; deep links use existing static latches (`PendingFocusMonsterId` — grep `Pending*Id` before inventing one); static `IsVisible` + `BuildHash()` for show/hide. [guiding-star §Navigation; learnings 2026-08-29 latch entry]
20. PawPad hit router: a parent never carries `pl-hit` if a child does (pre-order walk wins first); indexed hit families need prefix scans; scrolled-off rows must be gated by the scroller's rect; the in-app cursor is an ACTION STRING, not an index (lists change under the cursor). [learnings 2026-08-28 hit-router law; 2026-08-30 cursor build]
21. The kb graph encodes GEOMETRY — when a control moves on screen, its W/S routes move with it; a route that contradicts the screen is a bug even under "keep behavior". [learnings 2026-08-30 filters-into-chrome]

## COMMON FAILURES (seen in this codebase)
- **AcceptsFocus killing every keybind** ("none of the keybinds work", zero errors) — twice confirmed. [learnings 2026-07-12]
- **`pointer-events: auto` dead phone** — every click fell through to the page behind; probe-proven with dev_phonechain. [CLAUDE.md; learnings 2026-08-29 PROMOTE]
- **Mouse and keyboard fighting** — parked cursor keeping hovers lit; fixed only by all three device-contract legs. [learnings 2026-07-06]
- **2px spread-shadow ring invisible on wide dark slabs** — "the purple rim doesn't actually highlight it". [learnings 2026-08-30]
- **Ring 4px off-center** (padding-box placement bias) and the legacy `−8` width hack. [learnings 2026-08-29 RING CENTERING; 2026-07-12]
- **Stale cursor index after mouse-open**; **fixed 0–3 maps breaking when an `@if` hides a button**. [learnings 2026-05-18]

## WHAT THIS LANE DOES NOT OWN
The ring's glide ease / ink-lean / gold-snap character → **DOC-4**. Cursor color reservation →
**DOC-2**. Key-cap type metrics → **DOC-3** (and `.cb-key` passthrough → **DOC-6**). Zone
layout the cursor walks → **DOC-1**. Engine hit-test internals → **DOC-7**.
