# Beastborne UI — Guiding Star (canonical style reference + Fable 5 sweep brief)

The single source of truth for the all-panel UI sweep. Every panel — main menu, tab
screens, floating widgets, sub-panels, popups — converges here. The new visual language
was **proven first on the main menu** (Concept C launcher, purple liquid ring, angled
planes, italic display type); this doc lifts that language off the menu and makes it the
target for the entire game. Where an older doc or a shipped panel conflicts, **this doc
wins on tokens/color/motion**; `style-guide.md` wins on component anatomy/recipes.

> **North star:** *Make the deep feel simple.* Beastborne is genuinely complex (genetics,
> breeding, fusion, teams); the UI's whole job is to make that feel approachable, warm,
> obvious-at-a-glance. **The beasts are the heroes — UI is the frame.**

**THE MIX (each doc owns its lane):**
- **guiding-star.md (this doc)** — principles, the new visual language, color/type/space
  tokens, motion doctrine, stroke discipline, navigation model. WINS on any token/color/
  motion conflict.
- **style-guide.md** — the COMPONENT COOKBOOK (buttons, pills, badges, diamonds, section
  chrome) with `MonsterRosterPanel` as the living reference. WINS on component anatomy.
- **css-quirks.md** — the engine-constraint companion (what parses, what breaks).
- **scene-swap-spec.md** — the transition system for sibling screen changes.

> **⚠️ Reinvention scope (user directive, 2026-07-01):** the **main menu (title/launcher
> screen, `MainMenu.razor`) is LIKED — keep its structure.** It's the reference: its visual
> vocabulary (motion doctrine, angled planes, italic display, engine-safe patterns) AND its
> overall composition both work. The reinvention target is the **in-game menus/panels** — the
> tabs, floating widgets, sub-panels, and popups — whose **layout and especially FORMATTING**
> the user is unhappy with. Rethink how each of those is structured and laid out; be
> innovative, don't just reskin. Carry the main menu's vocabulary AND its compositional
> confidence into them so every screen feels like the same game — that's what "sibling of the
> menu" means.

---

## Principles (the gut-check)
- **P1 Simple surface, deep system** — lead with one action + the few numbers that matter; tuck genetics/traits/edge stats behind progressive reveal. First read ≠ spreadsheet.
- **P2 Readable at a glance** — big type, high contrast, generous space. Parse the most important thing in <1s. If two things fight for "most important," one is wrong.
- **P3 Beasts are the heroes** — chrome stays dark + quiet so sprites/art pop. A panel never out-shouts a beast.
- **P4 Color carries meaning** — never decorate with it. One accent per view.
- **P5 Playful, not noisy** — chunky pills, rounded cards, italic energy, dry wit; discipline underneath (one accent, consistent spacing).
- **P6 The next step is always obvious** — teach *through the interface* (one first-run hint / empty-state nudge in context), never stacked tutorials. If players need a tooltip to know what to do, fix the screen.
- **★ One-line test** — finish "The player is here to ____." If the layout doesn't make that the easiest thing, simplify until it does.

---

## What the sweep fixes (the 5 problems, and their mandates)

The current game has five named problems. Every panel in the sweep is judged against these:

| Problem today | Root cause | The mandate |
|---|---|---|
| **Not much movement / feels static** | Panels are flat and dead; motion is either absent or sprinkled thinly everywhere so nothing reads as alive. | **Concentrate motion.** Every screen has exactly ONE alive element (the violet selection ring) + a quiet ambient scene layer + hard SNAP feedback on the control you touch. Dead-static panels are a bug. See Motion. |
| **No angles / flat grid look** | Everything sits on a rectangular grid, axis-aligned. | **Adopt the angled-plane dialect** — `rotateY(13°)` sidebar buttons, `rotateY(10°)` cards, signed `±7°` scene backdrops, 45° diamonds, ±3° tilted stamps. Nothing sits perfectly flat. See The New Visual Language §2. |
| **Colors are messy** | ~12,400 hardcoded color literals, no token layer, element hues drift between panels, hairline borders everywhere read "generic AI dashboard." | **Token discipline + one accent per view + the vector rule.** Wind = teal is locked; the rest of the element palette is Fable 5's to redesign as ONE set. Kill decorative hairline borders. See Color + Surface & Stroke. |
| **Different UI style everywhere** | No shared primitives; every panel re-implements button/pill/card/modal/header by copy-paste, then drifts. | **Every panel is a sibling of the menu.** Header, section chrome, card, pill, diamond, focus ring all come from ONE source. Copy the canonical, don't invent. Reusable parts get captured into shared components, not pasted. |
| **Players get confused** | The always-on 3-island bottom bar spreads attention across ~13 targets; too many entry points, no clear "what do I do here." | **P1/P2/P6 + the phone launcher.** One primary action per view; replace the persistent bar with a single menu button that opens a clear launcher. See Navigation. |

---

## The new visual language (sourced from the main menu)

Four signatures make the look *new* vs a generic dark dashboard. These are the DNA every
panel inherits. All values below are real, shipped, engine-proven on the menu.

### 1. One alive element — the living violet ring
The selection cursor is the **only** perpetually-moving thing on any screen. It is a real
bordered `<div>` (`border: 4px solid #9b6cff`, transparent fill) — **not** a box-shadow
ring (s&box distorts box-shadow corners) — whose position/size/radius are written from
code and glide on the signature ease:
```
transition: left/top/width/height 0.27s cubic-bezier(0.22, 1, 0.36, 1);
```
It carries a continuous, per-frame multi-sine "ink-lean" wave (small skew/rotate, applied
imperatively so it never gets low-passed) and a 3D tilt that copies its target's plane.
The selected item's solid fill sits *behind* its label with a literal gap of moving
background between fill and ring (the "View button" look). On PLAY it snaps to gold
`#ffce3a` and slams shut (`0.09s`). **Every panel that has a selection/keyboard cursor
uses this exact ring.** See Focus ring & interaction.

### 2. The angled-plane dialect
Nothing sits on a flat axis-aligned grid. The vocabulary:
- **Receding planes** — sidebar buttons `perspective(1300px) rotateY(13deg)` (origin left center); half-width cards `perspective(2000px) rotateY(10deg)` (a full-width element's far edge recedes ~2× a half-width one at the same angle, so wide = smaller angle). One shared plane per GROUP (can't share a camera across separate elements in s&box — no `preserve-3d`).
- **Persona pop** — selecting an item lunges it OUT of the lean toward the player: `translateX(9px) rotateY(13deg) scale(1.04)` (nav), `scale(1.05–1.06)` (cards/CTAs). Hard SNAP, ≤150ms.
- **Signed scene lean as wayfinding** — Roadmap `+7°`, Patch Notes `−7°` (mirrored — the angle tells you where you are), Options `+5°`, all `perspective(2000px)`.
- **45° diamonds** — icon frames / gems: `rotate(45deg)` with inner icon counter-rotated `rotate(-45deg)`. The diamond is Beastborne's action shape.
- **Tilted stamps** — badges rest at `rotate(-3deg)` / `rotate(3deg)` and straighten to `0deg` on hover ("snaps to attention"). OUT NOW badge idle-wiggles 2°→5°.
- **⚠ Engine rule:** a transform-holder can't also carry bg/border/overflow (renders flat under 3D) → put the surface on a direct CHILD. And never put a transform on a flex parent whose children rely on flex-grow width (poisons width resolution) — drive entrances with opacity.

### 3. Three-color identity
- **Violet ring `#9b6cff`** = selection / "you" / focus. Everywhere, one color.
- **Gold hero `#ffce3a`** (on dark brown text `#2a1605`) = PLAY / primary / "go" / kickers.
- **One saturated accent per destination** — each screen owns a single accent (menu scenes: Roadmap gold, Patch Notes violet, Options teal). This is what the phone-launcher's per-app color and its liquid-expansion background build on (see Navigation).

### 4. Italic-900 display type + the vector rule + no-glow
- **Editorial voice**: large italic 900 display (76px featured title, 52px scene title, 38px PLAY), wide-tracked uppercase kickers. Persona "stamp-and-slab," not neutral sans UI.
- **The vector rule**: NO decorative hairline borders. A component is a **filled slab** separated from its parent by CONTRAST, not a stroke. Permitted lines only: the violet ring, a left accent bar, a bottom rarity strip, one optional 2px solid panel-signature strip. A border that just "defines an edge" → delete it, fix the fill contrast.
- **No-glow**: colored box-shadow halos read as smudge over moving/dark backgrounds. Hover feedback is dark elevation + brighten, never a colored glow. (Colored glows survive only in static, dark, non-scroll contexts, sparingly.)

---

## Color tokens

**Surfaces** (near-black, purple-leaning — the menu's actual darks):
- Root base `#04060f` · App/page bg `#0A0912`/`#0c0a18` · Panel slab `#15121F` · Raised card `#1C1830` · Card fill `rgba(20,20,35,0.95)` · Empty slot `#131019`
- Sidebar/slab gradient reference: `rgba(24,17,48,1) → rgba(11,7,28,1)`.

**Semantic accents** — fixed meaning, never recolored for variety:
| Token | Value | Means |
|---|---|---|
| Gold | `#ffce3a` (grad `#ffd95a→#ffb330→#f59312`, dark text `#2a1605`) | highlight · level · primary · "go" |
| Violet ring | `#9b6cff` | selection · focus · nav · "you" |
| Purple deep | `#7B4DDB` | secondary purple chrome |
| Orange | `#ee5421` | live · embark · alert |
| Red | `#E0414A` | destructive · release |
| Green | `#3FB45E` | success · confirm |
| Blue | `#3F8FE0` | info · neutral action |
| Discord blurple | `#5865F2` | Discord only |
| Text | `#F4F1EA` warm cream (dim `rgba(214,206,236,.6)`) | primary text |

### Element identity — **the full palette (incl. Wind) is OPEN — Fable 5 to design**
The exact hues are Fable 5's to design as ONE cohesive 11-element set. Only the *rendering
system* is a constant.

**THE ONE CONSTANT (keep):**
- **Two-tone rendering** — each element = a **dark saturated FILL + a bright RIM** (e.g. shipped Fire `#b91c1c` fill / `#dc2626` rim). This keeps cards uniformly dark so beasts pop; the element reads via a rimmed BADGE, **never a card-wide color flood**. No rarity-colored card borders either.
- **One hue per element**, used everywhere that element appears (badge, filter pill, detail, dual-type). Dual-typed beasts show BOTH badges, primary first. Neutral fallback `#9AA0AD`.

**OPEN — design a full 11-element palette (10 elements + Neutral):**
Fire · Water · Earth · **Wind** · Electric · Ice · Nature · Metal · Shadow · Spirit · Neutral.
*Wind is currently teal (`#0d9488`/`#2dd4bf`) and that's a fine anchor, but it is NO LONGER
locked — change it if the cohesive set calls for it.* Constraints for the new set: every hue
must (a) render legibly as a dark-fill + bright-rim badge on the `#0c0a18`/`#1C1830`
surfaces, (b) stay distinct from the semantic accents — **especially keep Shadow clear of the
`#9b6cff` selection violet** and Fire clear of the destructive-red — and (c) pass a contrast
check at badge size. Deliver as a single table (fill + rim per element); it then replaces the
current per-panel copies in ONE token pass. *Current shipped values live in
`Code/UI/Panels/BeastiaryPanel.razor.scss:797-807` + `FilterBar.razor.scss:102-112` — the
baseline to iterate from, not gospel.*

### Rarity ladder (card top-edge tint + soft strip; goes to Mythic)
Common `#9ca3af` · Uncommon `#22c55e` · Rare `#3b82f6` · Epic `#a855f7` · Legendary `#fbbf24` (dark text) · **Mythic `#ec4899`**.

### Stat hues (fixed)
HP green · ATK red · DEF blue · SpA purple · SpD cyan · SPD gold. *(Keep stat-cyan slightly bluer than Wind teal.)*

### Currency
Gold / Ink / Tokens. *(No "gem" — the game has none.)*

---

## Typography — Exo 2 only
- Two registered families: **`Exo2`** (Exo2-Bold.ttf) and **`Exo2Italic`** (Exo2-BoldItalic.ttf). s&box doesn't honor `@font-face` style-matching, so **italic is its own named family** — set `font-family: Exo2Italic; font-style: italic;`. One typeface, no second font.
- **The new look leans heavily italic**: display, labels, and even body copy on the menu use Exo2Italic. Italic is the default voice for headlines, hero beast names, kickers, CTAs, and card titles. Keep dense data/tables roman for legibility.
- Scale (menu-proven): hero/featured title **76px/900**, scene title **52px/900**, PLAY **38px/900**, nav labels **26px/800**, card title **26px/900**, body/desc **21px/500 italic**, CTA **20px/900**, kicker **13–16px/900 uppercase ls 0.2em**, key-cap **11px/900**. Panel-internal type stays smaller (page title 22, section header 11 uppercase, body 13) — see style-guide.md.
- ⚠ **Engine reality:** only Bold weights are in `Assets/fonts/` root, so every `font-weight` is faux-synthesized off Bold (reads heavy). Build hierarchy from **size + italic + color**, not fine weight steps. **`line-height` MUST be `px`** (unitless = multiplier post-26.06.03); for 30px+ type, line-height ≥ font-size.

---

## Layout, spacing & radii

- Designed at **1920×1080**, root scales to fit.
- **Spacing scale (4-based):** 4 · 8 · 12 · 16 · 20 · 24 · 32 · 40 · 56. Inside a component lean small (8–16); between sections lean large (24–56). Screen padding ~32–56px.
- **Radii — the new scale is ROUNDER than the old panels** (menu-derived):
  | Token | Value | Use |
  |---|---|---|
  | chip / small pill | 10–11px | key-caps, small pills, tiny CTAs (13px) |
  | button | **16px** | primary buttons, nav items, posters (menu standard) |
  | card | **22px** | content cards |
  | large surface / hero | **26px** | featured cards, big panels |
  | scene / modal slab | **30px** | full backdrops, large modals |
  | fully round | 999px | pills, dots |
  - **Concentric focus ring**: ring radius = host radius + inset (menu uses **+10px** → 26px ring on a 16px button, 32px ring on a 22px card). Keep the ring a fixed even gap around its host.
  - Dense content grids (roster cards) may stay tighter (14px) for information density — that's the one allowed exception; everything chrome-level uses the rounder scale.

---

## Components
- **Buttons** — one primary per view. Tiers: Primary (gold fill, dark text) → Commit (red fill, rationed — Embark/start battle) → Secondary (surface fill + diamond action icon) → Ghost (faint) → Danger (red-tinted, hold-to-confirm). Sizes 56/44/34px; ≥44 on main flows. States: hover brighten, active dim — **hue never changes**, and a ring-bearing button never transforms on hover (breaks the gap). Cost = trailing coin chip (red + inert when unaffordable); destructive = hold-to-confirm, not single click.
- **The diamond** (45°) = the action shape. Battle: diamond leads, keybind+label pill juts from its bottom edge. Menus/lists: small diamond *inside* a rectangular button so rows align. Tint to function (Item blue, Fuse pink, Release red). Never on nav/Play/passive chrome.
- **Beast card** (the atom): dark fill, soft separation (contrast, not stroke), **level pill top-left**, **favorite/multi-select top-right**, **PWR bottom-right over art**, **name + element badge(s) at foot**. Rarity = top-edge tint + bottom strip (never a card-wide border or flood). Selection = gold ring + fill. Art is the biggest thing; never drop-shadow the sprite (the card carries elevation).
- **Detail panel** stack: identity (art/name/badges) → lore → actions → stats → moves/traits. Glanceable up top, deepest data last. Slides out from under the content grid.
- **Badges** — element (dark-fill + bright-rim + solid glyph + UPPER label), rarity ladder, chips (LV, NEW, +N signed delta).
- **Nav** — see Navigation (the persistent bottom bar is being retired).

---

## Motion — the three-layer doctrine

Every motion belongs to exactly ONE layer. If you can't name the layer, the motion is wrong.

| Layer | Speed | Owner | Examples (menu durations) |
|---|---|---|---|
| **FLOW** (ambient) | slow, continuous, environmental | the *scene*, never controls | wave video bg, nebula drift 34s, hero bob 6s, gold sheen 4.8s, pulse dots 1.6s ∞ |
| **ALIVE** (the selector) | organic, perpetual, small | the violet ring ONLY | ring glide 0.27s `cubic-bezier(0.22,1,0.36,1)`, continuous ink-lean wave |
| **SNAP** (interaction) | fast, sharp, finite ≤150ms | the control you touched | Persona pop 0.14s, hover brighten, press dim, card hover 0.16s |

Rules that fall out:
- **Surfaces are SOLID.** Panels/buttons/cards never deform, ripple, or breathe. Organic motion lives in the ring + the scene. ⚠ HARD VETO: never put wave/skew motion on a large surface near the player's focus (motion sickness — PLAY's liquid gold was vetoed twice).
- **Player input creates the sharpest motion on screen.** Nothing ambient moves as fast as interaction feedback — that contrast is what makes clicking feel like *doing*.
- **Entrances** are staggered class-toggle transitions (`transition-delay` works post-26.06.03): sidebar slide-in 0.6s overshoot, nav rows cascade 0/0.06/0.12/0.18s. Drive entrances with **opacity** when a transform would poison child flex widths.
- **@keyframes only play once on mount** — fine for infinite ambient loops, WRONG for state reveals (won't replay on re-open). Use `transition:` for state changes.
- Reserve big celebratory setpieces for real milestones (the PLAY "detonation" — five-layer acknowledge into the tilted two-tone Persona wipe — is the template; don't spend it on routine actions).

---

## Surface & stroke discipline (the vector rule)
- Component = **filled slab** on a surface tone, separated by CONTRAST. Idle controls sit DARK and quiet; brightness is earned by selection (idle = quiet, focus = lit).
- Permitted lines only: violet selection ring · left accent bar (cards) · bottom rarity strip · one optional 2px solid panel-signature strip (ONE per panel, solid not gradient). That's the budget.
- No hairline `rgba(255,255,255,0.1)` borders on chips/pills/cards — that's the "AI dashboard" look. Delete and fix fill contrast.
- No colored glow halos (see no-glow). Drop shadows for elevation are fine (`0 4px 16px rgba(0,0,0,0.5)`); never `inset`.

---

## Navigation — retire the persistent bar, adopt the phone launcher

**The change:** remove the always-shown 3-island bottom command bar. Replace it with a
**single menu button** that opens a **phone/device overlay acting as a LAUNCHER** — a grid
of themed "app" buttons, one per destination. The phone is a *guide/router*, not where
content lives. This declutters every screen (P6, fixes "players get confused") and is the
exact abstraction the future **2D pixel-art map** needs (a map + a menu button, no
persistent chrome).

**The signature transition (the reason it's cool):** tapping an app plays a **liquid
expansion** — a solid circle in that app's accent color scales up from the button's
position to fill the screen, *becoming that menu's background*, with the menu fading in
behind it. Each destination owns an accent token; that token drives the app button, the
expansion, and the menu's background — one color, defined once.
- ⚠ Implement as a **scaling solid `<div>` circle** (`transform: scale()` on a `border-radius: 50%` element), NOT a `radial-gradient` (s&box rejects gradient shape keywords + px stops). This is proven-safe.
- The menu itself renders **full-screen or as a large popup**, per its own layout — the phone never holds real content.

**What to build to enable it (the decoupling):**
- A small **`NavManager` router** (static state + `Open(dest)`) so nav triggers are separate from whatever chrome renders them. Panels and chrome both call the router.
- Each destination declares its **accent token** (app color = expansion = bg).
- Remove the **96px bottom padding reserve** (`::after` spacer) currently on every tab panel — it exists only to clear the old bar.
- Remap the number-key routing / `IsNavigatingTabs` state to the launcher (keep hotkeys; they open the launcher or jump to an app).
- **Preserve** the rock-solid patterns: static `IsVisible` + `BuildHash()` for show/hide, `UIModalState` for input blocking, the concentric focus ring for keyboard nav inside the launcher.

---

## s&box translation layer (design CSS is browser CSS — these DON'T port)
Full table in `css-quirks.md`. The high-impact ones for the sweep:
- **No `box-sizing: border-box`** — content-box only; subtract borders/padding from explicit widths.
- **No `backdrop-filter`** → solid dark `rgba()`. **No `conic-gradient`** → linear/solid.
- **`radial-gradient`** → bare percent-stops only (`rgba(...) 0%, rgba(0,0,0,0) 70%`). No shape keyword, no `at X% Y%`, no px stops, no `transparent` keyword. For circles/halftone/liquid-expansion use a **solid scaling div**, not a gradient.
- **No `transparent` keyword in any gradient** → `rgba(...,0)`.
- **No `filter: drop-shadow` on `<img>`/sprites** (blurs pixel art) → glow via a behind div; drop shadow via `box-shadow` on a wrapper (never `inset`).
- **`filter` accepts only ONE function** — never chain.
- **No CSS border-triangles** → iconify glyph or a text `▶`/`◆` glyph for diamonds/arrows.
- **`line-height` always `px`.** **Sprites** use `image-rendering: pixelated` — never anti-alias or fractional-scale.
- **Transforms poison flex width** — no transform on a flex parent whose children rely on flex-grow; entrances via opacity. Transform-holders can't carry bg/border/overflow (flat under 3D) → surface on a direct child.
- **Scroll containers** don't clip descendant `box-shadow`/`transform` — no colored glows/transforms on scroll-grid cards; overhanging badges sit inside card bounds; celebration overlays go full-screen/outside the scroll.
- **`display: block` / `inline-flex` / `position: fixed`** rejected — use `display: flex`, `position: absolute` against a fullscreen root.
- Replace all **emoji** with the iconify/in-game icon set — one set, one weight.

---

## Focus ring & button interaction (the reusable cursor)
The keyboard/focus cursor is a **violet ring** (`#9b6cff`) with a small even GAP,
implemented as a **real bordered element** (NOT box-shadow — s&box distorts corners):
a child at `inset:-10px; border:4px solid #9b6cff; border-radius: hostRadius+10;
background-color: rgba(0,0,0,0); opacity:0`, flipped to `opacity:1` on focus/selection.
If the host has `overflow:hidden`, make the ring a *sibling* in a slot; otherwise a direct
*child*. Scale inset/border down for small controls.

**The rule:** a ring-bearing button must NOT transform on hover/active (a lift breaks the
even gap) — feedback is **brighten (hover) / dim (active)** only. Mouse-only buttons with
no ring may use hover-lift 1px + brighten. **Hue never changes between states.** Where
hover also sets the keyboard index (hover == focus), the ring shows on hover too — one
identical cursor on every control, in every panel.

---

## Voice
UI copy: short, confident, imperative ("Embark", "Out now.", "Five new beasts await."). Lore copy: warm, sensory, a little mythic. Labels UPPERCASE tracked; numbers brag for themselves ("PWR 916"). Dry wit ("Genetics go brrrrr."), rare exclamation points, never explain what the player can already see.

---

## Live-game corrections vs the old handoff (quick list)
1. **Wind is currently teal** (`#0d9488`/`#2dd4bf`) — a good anchor, but NOT locked; the whole element palette is open to Fable 5's redesign.
2. **The full element palette is being REDESIGNED by Fable 5** as one cohesive set — don't re-canonize the old per-panel values; iterate from the shipped baseline, keep the two-tone rendering system.
3. **Currency** = Gold/Ink/Tokens, not "gem".
4. **Rarity** includes **Mythic** above Legendary.
5. **The persistent bottom command bar is being retired** for the phone launcher (see Navigation) — don't design new panels around a permanent bottom bar or its 96px reserve.
6. **Radii are rounder now** (16/22/26/30) than the old 10/14/18 panel scale.
