# Beasts Panel — Center Stage Rebuild (blueprint)

**User mandate 2026-07-02:** "we could also start fresh on the beast panel" — fresh
VISUAL SKELETON, chosen concept: **Center Stage** (three zones). The C# logic layer
(selection, fusion, pickers, kb nav, multi-release, drag-select) is battle-tested and
CARRIES OVER UNTOUCHED — this is a markup + SCSS rebuild that rebinds to existing
handlers/state, not a logic rewrite.

Read with: `sweep-brief-v2.md` (dialect contract) + `learnings.md` (engine laws).

## The three zones (at ~2560×1440 reference; flex so 1920 works)

```
┌──────────────────────────────────────────────────────────────┐
│ HEADER: COLLECTION / MY BEASTS — FilterBar — FUSE·MULTI·12/50 │
├────────────┬──────────────────────────────┬──────────────────┤
│ COLLECTION │           STAGE              │      FACTS       │
│  (~30%)    │          (~44%)              │      (~26%)      │
│ grid, 4-5  │  sprite 2.5–3× over the      │ STATS gauges     │
│ cols of    │  glyph-drift, slate floor    │ QUALITY/NATURE   │
│ MonsterCard│  nameplate + LV/PWR/XP       │ TRAITS           │
│ v2 mini,   │  element chips               │ MOVES 2×2 slabs  │
│ scroll     │  ACTIONS slabs (evolve;      │ INFO rows        │
│            │  ITEM / FUSE / RELEASE)      │ (scrolls)        │
└────────────┴──────────────────────────────┴──────────────────┘
```

- **COLLECTION (left)** — dense grid of `<MonsterCard IsMini>` (v2 redesign, in
  flight). Direct-child-of-scroll law. Multi-release checkboxes, fusion filtering,
  drag-select all stay grid-side. Empty slots: quiet, on-dialect (v2 chrome).
- **STAGE (center)** — the pixel sprite is the star: 2.5–3× via explicit img
  width/height (NEVER transform scale — compositor blur law), `image-rendering:
  pixelated`, `background-repeat: no-repeat`. A slate ellipse/wedge grounds it.
  Under: species+nickname (+pencil), LV keycap, PWR, XP gold gauge, element chips,
  favorite star. Then ACTIONS: conditional evolve slab full-width; ITEM/FUSE/RELEASE
  tier row. **Ring clearance ≥14px around every host is a LAYOUT INPUT here, not a
  retrofit** (DetailRingInset 6 + 4px border ≈ ring extends ~12px past each host).
- **FACTS (right)** — slim scroll column: STATS (6 gauges), QUALITY/NATURE two-tone
  tiles, TRAITS pills, MOVES 2×2 element-edged slabs, INFO rows, journal/show-off
  glyphs. Item/move pickers swap INTO this column (existing showItemPicker /
  showMoveSwap logic; same inline-picker classes so ring + kb keep working).

## Modes (all existing logic, new homes)

- **Fusion** — the stage becomes the altar: Parent1/Parent2 slots flank a result
  silhouette; grid filters to compatible (existing). Result reveal on the stage.
- **Multi-release** — grid checkboxes + a confirm bar docked under the stage.
- **No selection / empty roster** — stage shows a quiet prompt (pick-a-beast).

## Systems that carry unchanged

- `BbIconScroll Icon="beasts"` page background (first child, root paints no fill).
- The ONE imperative liquid ring: grid path targets `.monster-card-wrapper`;
  detail path (WrapDetailRing/FindHoveredRingHost, kb + NEW mouse-hover) targets
  `RingHostClasses` — **extend that array** with any new stage/facts host classes,
  and point `detailScrollRef` coverage at BOTH stage and facts (either one shared
  ancestor ref or a second ref + second walk).
- Keyboard nav: zones become grid ↔ stage ↔ facts (Tab cycles; Q returns). Rebuild
  the `DetailFocusItem` band table to the new 2D geometry. Scroll-follow law stays.
- FilterBar (already reswept), header chrome, all confirm dialogs.

## Execution order

1. MonsterCard v2 agent lands (its mini card is the grid unit; chrome selectors
   .card-fav-qt/.select-checkbox/.parent-indicator/.card-ring/.empty-slot.mini carry).
2. Skeleton rebuild of MonsterRosterPanel.razor markup + fresh SCSS (legacy dossier
   selectors deleted, not shadowed). Rebind — do not rewrite — every handler.
3. Compile + capture-drive: selection, hover-ring on stage/facts hosts, kb Tab
   handoff, fusion, pickers, multi-release, empty states. FPS check (drift bg).
4. Patchnotes + commit.
