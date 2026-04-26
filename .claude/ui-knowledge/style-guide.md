# Beastborne Style Guide

**Source of truth: `Code/UI/Panels/MonsterRosterPanel.razor` + `.razor.scss`.** This is the production-quality reference panel. When designing any UI, **read roster first, copy its patterns, only diverge with explicit reason**.

This doc names the patterns and tells you where to find them in roster. It does NOT redefine what's already canonical — go read the source.

---

## When in doubt, copy roster

The roster panel embodies Beastborne's visual identity better than any document. If a question comes up like "how should this pill look?" or "what's the section header treatment?" — open `MonsterRosterPanel.razor.scss` and search for the analogous pattern. Pull it; don't invent.

This rule applies even when you're working on a new panel that has no obvious roster analog. Example: when building Beastbook, we ported `.section-title`, `.level-badge`, `.power-badge`, `.action-v2`, `.move-diamond-slot`, `.horiz-rarity-edge`, `.mini-rarity-edge` directly from roster. Result: Beastbook reads as a sibling of roster, not a foreign panel.

---

## Color palette (canonical)

### Primary chrome
| Role | Hex | Notes |
|------|-----|-------|
| Modal/page bg | `#0c0a18` | Outer containers. Roster + Beastbook + popups. |
| Card bg | `rgba(20, 20, 35, 0.95)` | Standard card. Matches MonsterCard.razor.scss:21. |
| Card elevated | `rgba(10, 8, 20, 0.92)` | Slightly raised, e.g. portrait frames. |
| Section bg subtle | `rgba(255, 255, 255, 0.03)` | Lore rows, info blocks. |

### Purple accents (interaction)
| Role | Value |
|------|-------|
| Primary purple | `#8b5cf6` / `rgba(139, 92, 246, 1)` |
| Light purple text | `#c4b5fd` |
| Section accent border | `rgba(139, 92, 246, 0.5)` (3px left bar) |
| Hover purple bg | `rgba(139, 92, 246, 0.18)` |
| Active purple border | `rgba(139, 92, 246, 0.45)` |

### Element color map (CANONICAL — match exactly)

**Canonical source: `Code/UI/Components/FilterBar.razor.scss` filter pill active state (lines 86-95).** Use these across every panel for consistency — element pills, grid card badges, detail badges, anywhere an element color appears as a solid fill.

All backgrounds are dark/saturated. White text works for every element — no dark-text-on-light exception needed.

| Element | Background | Border (companion) | Text on solid pill |
|---------|-----------|--------------------|--------------------|
| Fire | `#b91c1c` | `#dc2626` | white |
| Water | `#1d4ed8` | `#2563eb` | white |
| Earth | `#78350f` | `#92400e` | white |
| Wind | `#15803d` | `#16a34a` | white |
| Electric | `#a16207` | `#ca8a04` | white |
| Ice | `#0284c7` | `#0ea5e9` | white |
| Nature | `#166534` | `#15803d` | white |
| Metal | `#475569` | `#64748b` | white |
| Shadow | `#4c1d95` | `#5b21b6` | white |
| Spirit | `#9d174d` | `#be185d` | white |
| Neutral | `#525252` | `#737373` | white (extrapolated — not in filter pills) |

**Allowed exception:** subtle rim-light accents (e.g. Beastbook `.bb-move-row` borders, `.bb-evo-stage.current` portrait outlines) may use the lighter "game palette" values at 0.65 alpha against a dark neutral background. These are ambient glows, not identity pills — the canonical palette above applies to every solid-fill element surface.

### Rarity color map (CANONICAL)
Solid pills use these at `0.95` alpha. Rarity strips use them at full opacity with a left→right fade.

| Rarity | Hex | Pill text |
|--------|-----|----------|
| Common | `#9ca3af` | white |
| Uncommon | `#22c55e` | white |
| Rare | `#3b82f6` | white |
| Epic | `#a855f7` | white |
| Legendary | `#fbbf24` | dark `#1a1a1a` |
| Mythic | `#ec4899` | white |

### Mastery tier ramp (CANONICAL)
For per-tier text colors in mastery sections (Lv0 unbound → Lv6 grandmaster):

| Tier | Color | Title word |
|------|-------|-----------|
| Lv0 | `#b8b8b8` | Unbound |
| Lv1 | `#9ca3af` | Novice |
| Lv2 | `#4ade80` | Adept |
| Lv3 | `#60a5fa` | Veteran |
| Lv4 | `#a855f7` | Elite |
| Lv5 | `#fbbf24` | Master |
| Lv6 | `#ec4899` | Grandmaster |

### Reward + status
| Role | Hex |
|------|-----|
| Gold (reward) | `#fbbf24` |
| Gold deep | `#c0962a` |
| Gold accent | `#f59e0b` |
| Success green | `#34d399` |

---

## Typography

| Element | Size | Weight | Line-height |
|---------|------|--------|-------------|
| Page title (panel header) | 22px | 700 | 22 |
| Hero name (detail pane) | 22px | 800 | 24 |
| Section header | 11px | 700 | uppercase, ls 1.5 |
| Card name | 12-14px | 700-800 | default |
| Body text | 13px | 500 | default |
| Flavor (italic) | 13px | 500 | italic, opacity 0.65 |
| Pill text | 10-11px | 800 | uppercase, ls 1-1.2 |
| Stat label | 13px | 700 | default |
| Stat value | 14px | 700 | default |
| Number/registry pill | 11px | 800 | monospace-feel, ls 1.2 |
| Tiny meta (timestamps, counts) | 9-10px | 600-700 | uppercase optional |

**s&box quirk:** large fonts (30px+) need `line-height` numerically ≥ font-size + 2. See `css-quirks.md`.

---

## Design system primitives

### Solid pill (`.bb-pill` family — Beastbook canonical, but apply everywhere)
- Height **22px**
- Padding `4-6px / 8-10px`
- Border radius `6px`
- Border `1px solid rgba(0, 0, 0, 0.35)` (subtle dark edge for definition)
- Font `10-11px / 800 / uppercase / ls 1-1.2`
- Filled background at 0.85-0.95 alpha
- White text by default; dark text only on light element pills
- Hover: `filter: brightness(1.1); transition: filter 0.15s ease-out;` (single filter only — never chain)

Rules:
- **All pills look like variants of the same component.** Only color changes per variant; dimensions stay identical.
- No outlined pills. Always solid filled.
- No per-personality colors on personality pills — neutral white-on-translucent for personality.

### Diamond action button
Iconify-in-diamond pattern. Used for actions throughout (Beastbook diamonds, roster move slots).

- Outer container: `transform: rotate(45deg)`, ~36×36 inside ~52×52 bounding box
- Border: 1.5px in element/action color
- Inner iconify: counter-rotated `transform: rotate(-45deg)` so the icon stays upright
- Active state: border brightens, `transform: rotate(45deg) scale(1.06)` retained
- Hover: `transform: rotate(45deg) scale(1.1)` with snappy `cubic-bezier(0.2, 0.8, 0.3, 1.3)` spring
- Press: `scale(0.93)`

Reference: `Code/UI/Panels/BeastiaryPanel.razor.scss` `.bb-detail-diamond` family.

### Section accent bar
Standard section header treatment used in roster + Beastbook detail panes.

```scss
.section-title {
    border-left: 3px solid rgba(139, 92, 246, 0.5);
    border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    padding: 6px 10px;
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 1.5px;
    text-transform: uppercase;
    color: rgba(255, 255, 255, 0.45);
}
```

`border-left: 3px solid rgba(...)` DOES work in s&box (despite older claims). Verified in roster + Beastbook.

### Card hover lift (standard interactive feedback)
```scss
transition: transform 0.15s ease-out, border-color 0.15s ease-out;
&:hover {
    transform: translateY(-2px);
    border-color: rgba(139, 92, 246, 0.4);
}
```

### Bg-pattern layer
For subtle iconify lattice backgrounds in panels (chat, effects, notifications, radio, beastbook):
- **Place as plain in-flow flex child**, FIRST child of the panel container
- **NO `position: absolute`** — that causes the layer to render BEHIND the parent's `background-color` in s&box
- **NO `.bg-scroll` SCSS rules** at all (matches the working RadioWidget pattern)
- The `<img>` inside renders at natural size; clipped by panel's `border-radius: 16px; overflow: hidden`

### Per-species CardScale + CardOffset (sprite positioning)
For grid card sprites that have varying native art positioning. See `MonsterSpecies.cs`:
- `CardScale` (default 1.0) — multiplicative scale on the sprite
- `CardOffsetX` (default 0) — horizontal pixel offset
- `CardOffsetY` (default 0) — vertical pixel offset
- `transform-origin: center bottom` keeps feet grounded when scaling
- Apply via inline `style` with `InvariantCulture` formatting (s&box CSS needs `.` decimal)
- For evolution chain or other smaller frames, scale offsets proportionally (e.g. 0.65× for 100px frame vs 156px grid card)

---

## Spacing rhythm (4 / 8 / 12 / 16 / 24)

Stick to this scale. No 6/7/11/14/18 oddballs.

| Token | Value | Use |
|-------|-------|-----|
| xs | 4px | Tight inline gaps, icon padding |
| sm | 8px | Within-section gaps, item gaps |
| md | 12px | Card content padding |
| lg | 16px | Major section gaps, panel padding |
| xl | 24px | Top-level layout breaks |

---

## Border radii

| Token | Value | Use |
|-------|-------|-----|
| sm | 4-6px | Pills, small buttons |
| md | 8-10px | Cards, frames, chips |
| lg | 12-14px | Standard content cards |
| xl | 16px | Modal containers, large panels |

---

## Shadows

Outer drop only — `inset` is unsupported. Glow halos sparingly (we removed most of them this session as too "AI-looking").

| Tier | Value | Use |
|------|-------|-----|
| Subtle | `0 2px 12px rgba(0,0,0,0.3)` | Floating chips |
| Medium | `0 4px 16px rgba(0,0,0,0.5)` | Cards |
| Strong | `0 40px 100px rgba(0,0,0,0.85)` | Modal/popup frames |

**No purple halo glows** on popup frames. Clean drop shadow only. (Removed across all HUD popups + inventory in 2026-04 polish pass.)

---

## What NOT to do (stylistic discipline)

- **No outlined pills** — solid filled, always
- **No purple glow halos** on popup frames — clean drop shadow only
- **No element-color floods** on grid cards — keep cards uniformly dark, element belongs in small badges or pills
- **No rarity-colored borders** on grid cards — too noisy. Rarity reads via small icon, badge, or bottom strip
- **No infinite pulse/breathing animations** on routine UI — they read AI-generated and "twitchy"
- **No `@keyframes` for state reveals** — they only play once on mount, won't replay on panel re-open. Use `transition:` instead
- **No per-personality colors** on personality pills — neutral
- **No flex-wrap inside scroll containers** — collapses width
- **No `text-overflow: ellipsis` + `overflow: hidden` on flex children** — collapses

---

## Signature moves (use intentionally)

These are Beastborne's visual fingerprints. Reuse when appropriate.

- **Escalating heights** — Daily streak track (90→165px). Pattern for "build toward something" sequences.
- **Rotating silhouette** — Day 7 cycles legendaries. Pattern for "mystery prize" teasers.
- **Purple lift on hover** — `translateY(-2px)` + purple border intensification. Standard interactive feedback.
- **Diamond action buttons** — iconify counter-rotation in 45deg-rotated diamonds. Used for roster actions, Beastbook diamonds.
- **Section accent bars** — 3px purple left border + uppercase title. Used everywhere for section structure.

---

## Critical s&box quirks (always honor)

See `css-quirks.md` for the full list. The high-impact ones:
- `line-height` ≥ font-size for 30px+ fonts
- `overflow: hidden` collapses flex children — avoid on flex
- Scroll containers need direct flat children (no nested flex wrappers)
- `flex-wrap: wrap` miscalculates height — use explicit row divs
- `filter` accepts only ONE function — never chain
- `object-position` rejects percentage pairs — use keywords
- `scrollbar-color` / `scrollbar-width` non-functional — remove
- Absolute bg-layers render BEHIND parent `background-color` — use in-flow flex child
- `@keyframes` play once on mount — use `transition:` for state changes
- Razor `@variable` in text needs `@(...)` parens to disambiguate
- Inline-style floats need `InvariantCulture` formatting
