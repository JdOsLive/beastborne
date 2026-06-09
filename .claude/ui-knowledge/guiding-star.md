# Beastborne UI — Guiding Star (canonical style reference)

Distilled from the design team's "UI Guiding Star" handoff, **reconciled to the live
codebase + s&box CSS reality**. This is the target every panel converges to in the
all-panel consistency pass. Where the design handoff conflicted with the shipped game,
the live value wins (noted inline). Supersedes the older notes in `style-guide.md` for
tokens; keep `css-quirks.md` as the engine-constraint companion.

> **North star:** *Make the deep feel simple.* Beastborne is genuinely complex
> (genetics, breeding, fusion, teams); the UI's whole job is to make that feel
> approachable, warm, obvious-at-a-glance. The beasts are the heroes — UI is the frame.

---

## Principles (the gut-check)
- **P1 Simple surface, deep system** — lead with one action + the few numbers that matter; tuck genetics/traits/edge stats behind progressive reveal. First read ≠ spreadsheet.
- **P2 Readable at a glance** — big type, high contrast, generous space. Parse the most important thing in <1s. If two things fight for "most important," one is wrong.
- **P3 Beasts are the heroes** — chrome stays dark + quiet so sprites/art pop. A panel never out-shouts a beast.
- **P4 Color carries meaning** — never decorate with it (see Color). One accent per view.
- **P5 Playful, not noisy** — chunky pills, rounded cards, italic energy, dry wit; discipline underneath (one accent, consistent spacing).
- **P6 The next step is always obvious** — teach *through the interface* (one first-run hint / empty-state nudge in context), never stacked tutorials. If players need a tooltip to know what to do, fix the screen.
- **★ One-line test** — finish "The player is here to ____." If the layout doesn't make that the easiest thing, simplify until it does.

---

## Color tokens (reconciled)

**Surfaces** (dark, purple-leaning canvas):
- App BG `#0A0912` · Panel `#15121F` · Raised card `#1C1830` · Empty slot `#131019`

**Semantic accents** — fixed meaning, never recolored for variety:
| Token | Hex | Means |
|---|---|---|
| Gold | `#FFCE3A` (grad `#FFD95A→#F59312`, dark text `#2A1605`) | highlight · level · "go" / primary |
| Purple | `#7B4DDB` · lite `#9B6CFF` | selection · nav · "you" |
| Orange | `#EE5421` | live · embark · alert |
| Red | `#E0414A` | destructive · release |
| Green | `#3FB45E` | success · confirm |
| Blue | `#3F8FE0` | info · neutral action |
| Text | `#F4F1EA` (dim `rgba(214,206,236,.6)`) | primary text |

**Element identity — one hue per element, EVERYWHERE that element appears.** Reconciled to the live game (these differ from the handoff in two spots — noted):
| Element | Primary / border | Light text/glyph | Fill / dark |
|---|---|---|---|
| Fire | `#FF5A1F` | `#ff7a3c` | — |
| Water | `#1F8FFF` | `#4aa8ff` | — |
| **Nature** | `#22c55e` / `#16a34a` *(live emerald — NOT the handoff's #2FD24F)* | `#4ade80` | — |
| **Wind** | `#2dd4bf` *(TEAL — shipped 0596e37; reverses old slate-gray. NOT the handoff's #1FD6C8)* | `#5eead4` | fill `#0d9488` / dark `#0f766e`, tint `rgba(45,212,191,α)` |
| Earth | `#EF9A1F` | `#cf9b54` | — |
| Electric | `#FFD60A` | `#ffd23a` | — |
| Ice | `#6FD8FF` | `#9fe6ff` | — |
| Metal | `#AEBACC` | `#b9c2d0` | — |
| Shadow | `#9A4DFF` | `#b388ff` | — |
| Spirit | `#FF4FD0` | `#ff6bd6` | — |

Dual-typed beasts show **both** element badges, **primary first** (e.g. Liliprince = Water/Spirit). Neutral fallback `#9AA0AD`.

**Rarity ladder** (card top-edge tint + soft outer glow) — *the game goes to Mythic; the handoff stopped at Legendary:*
Common grey · Uncommon green · Rare blue · Epic purple · Legendary gold (glow) · **Mythic** (top tier — confirm in-code for the exact hue).

**Stat hues** (fixed): HP green · ATK red · DEF blue · SpA purple · SpD cyan · SPD gold. *(Note SpD cyan vs Wind teal are close — keep stat-cyan slightly bluer.)*

**Currency** — Gold / Ink / Tokens *(the handoff's "gem" is wrong — the game has no gem)*.

---

## Typography — Exo 2 only
- Registered as `font-family: Exo2` (from `fonts/Exo2-Bold.ttf`). **One family, no second font.** Any `monospace` is a stray → replace.
- Display/H2/CTA/hero beast-names: **900 italic**. Card names: 800. Labels: 800 UPPERCASE, `letter-spacing 0.14–0.18em`. Body: 500. Data: 800, **tabular numbers**.
- **Italic is reserved** for headlines, hero beast names, primary CTAs — don't italicize body/dense data.
- ⚠️ **Engine reality:** only `Exo2-Bold.ttf` is in `Assets/fonts/` root, so every `font-weight` is faux-synthesized off Bold (reads a bit heavy). Until the other weights are moved into the fonts root + registered, lean on **size + italic + color** for hierarchy, not fine weight steps. (`line-height` MUST be `px` — unitless is a multiplier post-26.06.03.)

---

## Layout & spacing
- Designed at **1920×1080**, root scales to fit. Persistent **bottom HUD** anchors every screen: currency (left) · primary nav 1–6 (center) · utility (right). Never let content touch the HUD; screen padding ~32–56px.
- Detail panel **slides out from under the content grid** (layered behind, emerges right) — not a fixed third column.
- **Spacing scale (4-based):** 4 · 8 · 12 · 16 · 20 · 24 · 32 · 40 · 56. Inside a component lean small (8–16); between sections lean large (24–56).
- **Radius:** chip 6 · button 10 · card 14 · panel 18 · pill 999.

---

## Components
- **Buttons** — one primary per view. Tiers: Primary (gold fill, dark text) → Commit (red fill, rationed — Embark/start battle) → Secondary (surface fill + **diamond action icon**) → Ghost (faint) → Danger (red-tinted, confirm). Sizes: 56 / 44 / 34px; never below 34, ≥44 on main flows. States: hover lift 1px+brighten, active press 1px+dim, focus = purple ring (keep it), disabled 40%+desaturate — **hue never changes between states**. Game states: busy=spinner+lock; cost=trailing coin chip (turns red, inert when unaffordable); destructive=**hold-to-confirm** (fill red, not single-click).
- **The diamond** = Beastborne's "action shape" (45°). Battle: diamond leads, keybind+label pill juts from its bottom edge. Menus/lists: small diamond *inside* a rectangular button so rows align. Tint to function (Item blue, Fuse pink, Release red). Never on nav/Play/passive chrome.
- **Beast card** (the atom — roster/book/team/shop): dark fill, soft border, **level pill top-left**, **favorite/multi-select top-right**, **PWR bottom-right over art**, **name + element(s) at foot**. Rarity = border tint + soft outer glow (never a different shape). Selection overrides with gold ring + glow. Art is the biggest thing; never drop-shadow the sprite (the card carries elevation).
- **Detail panel** stack order: identity (art/name/badges) → lore → actions → stats → moves/traits. Glanceable up top, deepest data last.
- **Badges** — element (tinted bg + solid glyph + UPPER label), rarity ladder, battle conditions (each owns a glyph+color, persist on the plaque), chips (LV, NEW, +N delta — always signed).
- **Stat bars** — compact (detail panel): name+IV left, total+green mastery right, full-width bar base(solid)→mastery(striped). Full table for a stats screen. Gene/IV kept visually separate (it's roll quality, not a live value).
- **Nav** — global bottom bar, active tab = filled purple, orange dot = something new. **If it has a hotkey, it wears the hotkey** (key badge = small mono char in a dark rounded square, leading/top corner).

---

## Motion — the three-layer doctrine (v2, 2026-06-09)

Every motion in the game belongs to exactly ONE of three layers. If you can't name the
layer, the motion is wrong. This is what makes "flowy overall, snappy when interacting"
a system instead of a vibe — and it ports to every panel, not just the menu.

| Layer | Speed | Owner | Examples |
|---|---|---|---|
| **FLOW** (ambient) | slow, continuous, environmental | the *scene*, never controls | wave video bg, nebula drift, hero beast bob, live-dot pulse 1.6s∞ |
| **ALIVE** (the selector) | organic, perpetual, small | the violet cursor/ring ONLY | ring glide 0.27s, slow sine wave/skew, liquid stretch |
| **SNAP** (interaction) | fast, sharp, finite ≤150ms | the control the player touched | Persona pop (translateX+scale 0.14s), press dim, brighten, flash |

Rules that fall out of it:
- **Surfaces are SOLID.** Panels/buttons/cards never deform, ripple, or breathe.
  Organic motion lives in the selector and the scene. ⚠️ HARD VETO (motion sickness,
  2026-06-09): never put wave/skew motion on a large surface near the player's focus
  (PLAY's liquid gold was tried twice and vetoed). Small fills mostly hidden behind a
  label are the only tolerated exception — and even those are candidates for
  simplification if consistency questions keep coming up.
- **Player input creates the sharpest motion on screen.** Nothing ambient may move as
  fast as interaction feedback — that contrast is what makes clicking feel like *doing*.
- **NO GLOWS on/near animated backgrounds.** Soft box-shadow halos read as smudge over
  moving gradients (user call, 2026-06-09 — removed from PLAY/Play Now/View). Crisp
  edges + motion draw the eye instead. Colored glows survive only inside static, dark,
  non-scroll contexts (and sparingly).
- Old timing values still hold: hover 120ms · selection 140ms · panel-in 180ms ·
  stat fill 400ms · burst 600–900ms. Interaction ≤150ms always.
- Reserve big celebratory motion for real milestones (tier system in feel-principles).

## Surface & stroke discipline (the "vector" rule, 2026-06-09)

The menu was accumulating hairline `1px rgba(255,255,255,0.1–0.2)` borders on every chip,
pill, and card — the generic "AI dashboard" look (user flagged it). The language is
**solid color blocks, not outlines**:
- A component is a **filled slab** on one of the surface tones, separated from its
  parent by CONTRAST, not by a stroke.
- Permitted lines: the **violet ring** (selection), the **left accent bar** (cards),
  a **bottom rarity strip**, and **one optional solid panel signature strip** (e.g. a
  2px violet top strip on a modal — ONE per panel, solid not gradient; added from the
  OptionsPanel port feedback 2026-06-09). That's the budget. A border that just
  "defines an edge" → delete it and fix the fill contrast instead.
- Idle controls sit DARK and quiet (surface tones); brightness is earned by selection
  (the "make the other buttons darker" instinct — idle = quiet, focus = lit).

---

## Voice
UI copy: short, confident, imperative ("Embark", "Out now.", "Five new beasts await."). Lore copy: warm, sensory, a little mythic. Labels UPPERCASE tracked; numbers brag for themselves ("PWR 916"). Dry wit ("Genetics go brrrrr."), rare exclamation points, never explain what the player can already see.

---

## ⚠️ s&box translation layer (the design CSS is browser CSS — these DON'T port)
The handoff assumes standard browser CSS. Translate every pattern to engine-safe CSS (full table in `css-quirks.md`). The big ones:
- **No `box-sizing: border-box`** — content-box only; subtract borders/padding from explicit widths.
- **No `backdrop-filter`** ("frosted") → solid dark `rgba()` bg.
- **No `conic-gradient`** (studio mark) → solid or linear-gradient.
- **`radial-gradient`** → bare percent-stops only: `radial-gradient(rgba(...) 0%, rgba(0,0,0,0) 70%)`. No shape keyword, no `at X% Y%`, no px stops, no `transparent` keyword.
- **No `transparent` keyword in any gradient** → `rgba(...,0)`.
- **No `filter: drop-shadow` on `<img>`/sprites** (blurs pixel art) → glow via a separate behind radial-gradient div; drop shadows via `box-shadow` on a wrapper (never `inset`).
- **No CSS border-triangles** (`border-style: solid` is rejected standalone) → use an iconify glyph (`lucide:play`, `lucide:chevron-right`) or a `▶` text glyph for the diamond/arrow shapes.
- **`line-height` always `px`** (unitless = multiplier since 26.06.03).
- **Sprites** use `image-rendering: pixelated`; never anti-alias or fractional-scale them.
- **Transforms poison flex width** — never put a `transform` (even a translateY entrance) on a flex parent whose children rely on width/flex-grow; drive entrances with **opacity** instead.
- **Scroll containers** don't clip descendant `box-shadow`/`transform` — no colored glows/transforms on scroll-grid cards; celebration overlays go full-screen/outside the scroll.
- Replace all **emoji** with the iconify/in-game icon set. Keybind/element/currency glyphs = the in-game art, one set, one weight.

---

## Focus ring & button interaction (reusable — built on the menu)

The keyboard/focus cursor is a **violet ring** (`#9B6CFF` = `--bb-purple-lite`) with a small
GAP, implemented as a **real bordered element** — NOT a box-shadow (s&box distorts box-shadow
ring corners; see `css-quirks.md`). Pattern: a child at `inset:-10px; border:4px solid
#9b6cff; border-radius: hostRadius+10; background-color: rgba(0,0,0,0); opacity:0`, flipped
to `opacity:1` on the focused/selected state. If the host has `overflow:hidden`, wrap it in a
slot and make the ring a *sibling* (menu `.lh-play-slot` / `.lh-play-ring`); otherwise the
ring is a direct *child* (menu `.lh-item-ring` / `.lh-util-ring`). Scale the inset/border
down for small controls.

**The rule:** a button carrying this ring must NOT transform on hover/active — a lift/slide
breaks the even gap; feedback is **brighten (hover) / dim (active)** only. Mouse-only buttons
with NO ring follow the guide's hover-lift 1px + brighten / active press + dim. **Hue never
changes** between states. Where hover also sets the keyboard index (hover == focus), the ring
shows on hover too — one identical cursor on every control.

## Live-game corrections vs the handoff (quick list)
1. **Wind = teal** (`#2dd4bf`), not slate-gray, not the handoff's `#1FD6C8`. ✅ shipped.
2. **Nature** = `#22c55e/#16a34a`, not `#2FD24F`.
3. **Currency** = Gold/Ink/Tokens, not "gem".
4. **Rarity** includes **Mythic** above Legendary.
5. Nav is Home + 6 tabs with the live labels (Beasts/Skills/Expeditions/Online/Beastbook/Shop), not the handoff's exact list.
