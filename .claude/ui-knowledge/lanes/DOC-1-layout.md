# DOC-1 — LAYOUT (zones · slabs · radii · headers · spacing · box math)

**Charter.** This lane owns how a page is carved up and measured: zone placement and sizing,
the slab construction of components, the radius ladder, page-header anatomy, the spacing
scale, and every pixel of width/height math under s&box's padding-box model. It is a LENS,
not a fork — canonical truth lives in `guiding-star.md` (§Layout, §Navigation, §2 angled
planes), `learnings.md` (dated entries cited below), and the CLAUDE.md quirks table. If this
doc and a source disagree, the source wins; flag the drift.

## THE CHECKLIST
1. Design at **1920×1080**; root scales to fit. [guiding-star §Layout]
2. Spacing is the 4-based scale (4·8·12·16·20·24·32·40·56): small inside a component (8–16), large between sections (24–56), screen padding 32–56. [guiding-star §Layout]
3. Radii ladder: chip 10–11 · button **16** · card **22** · hero **26** · scene/modal **30** · pill 999. Dense content grids may stay 14 — the one exception. [guiding-star §Layout radii table]
4. **PADDING-BOX math everywhere**: declared `width`/`height` INCLUDES padding; only borders add on top. Row budget: `sum(children declared + their borders) + gaps = row width`. [CLAUDE.md row "box-sizing is rejected — PADDING-BOX"; learnings 2026-07-05 "THE BOX MODEL, CORRECTED"]
5. Never `width: 100%` on a child of a padded parent — it resolves against the OUTER width and overflows by 2×padding. Explicit px or no width at all. [learnings 2026-08-29 "Online hub v3.1"]
6. Page chrome = **wave header** (kicker → italic-900 title → skewed seam bars → BbHeaderWaves band); the Skill-Tree **sub-band** exists ONLY on pages with view tabs — tab-less pages get no dead slab. [learnings 2026-08-28 "Header law for the hub"; SkillTreePanel.razor:91-127]
7. A header on a tinted surface INHERITS the surface — no base slab darker than the room ("is there a slab under the title darker than the room? then it's a box, not a header"). [learnings 2026-08-29 "HEADER NO-BOX ruling"]
8. Section filters/toolbars live in the page header's RIGHT slot (`.rh-toolbar` position), never in the body under a section title. [learnings 2026-08-30 "filters into the chrome band"]
9. **BIG THINGS BIG**: the primary action is the largest surface on the page; a LOCKED primary stays big and dims — never shrinks into a chip. [learnings 2026-08-28 "BIG THINGS BIG / NO NOTHING"]
10. **NO NOTHING**: every zone is sized by column math to fill the page and has a visible action even when its data is empty — but that action is a NORMAL-sized button, not a zone-filling bar. [same entry + learnings 2026-08-29 "v3.3 revision" ruling 2]
11. Components are **filled slabs separated by CONTRAST**, not outlines. An approved mock licenses zone placement/size only — surface treatment always comes from the house recipes. [guiding-star §Surface & stroke; learnings 2026-08-28 "house-dialect fix pass"]
12. Scroll containers: parent `display:flex; flex-direction:column; overflow:hidden` + scroll child `flex:1 1 0; min-height:0; overflow-y:scroll` with items as DIRECT children — no wrapper divs. A wrap grid that is the scroll child also pins `width` AND `max-width` to the slab. [CLAUDE.md "Scroll containers need flat children"; learnings 2026-08-29 "Trade Board card grid"]
13. Cards in flex rows declare `width` + `min-width` + `flex: 0 0 Npx` — all three — or s&box shrinks them to zero. [CLAUDE.md "Flex cards need explicit width"]
14. Rows inside a scroll list get a PINNED width; spacer lanes get a real flex-basis (`flex: 1 1 <remainder>px`), not `flex: 1 1 0`. [learnings 2026-08-29 "Rows inside overflow-y"]
15. Angled-plane dialect (game pages only, NOT the phone): rotateY per GROUP, `transform-origin: left center`; a 2× wider element halves its angle (`θ_wide ≈ θ_narrow × w_narrow/w_wide`); the transform holder carries NO bg/border/overflow — surface goes on a direct child. [guiding-star §2; learnings 2026-06-08 angled-UI entry]
16. Corner cuts force radius 0 on their slab; rotated-square cuts are RETIRED on flat pages (hub ruling) — use them only where a page is explicitly angled dialect. [learnings 2026-08-30 "Three hub rulings" (2)]
17. Pointed slabs (pennants/tabs/tails) are built ADDITIVELY (same-fill rotated square hung off the edge, z0 first child), never subtractively — subtractive cuts always leak into neighbors. [learnings 2026-08-29 ".gz-banner-point"]
18. Every `position: absolute` full-fill layer declares `top:0; left:0` in addition to width/height, or it can collapse to zero. Absolutes never shrink-wrap — give them explicit boxes. [learnings 2026-04-17; CLAUDE.md "width: auto does NOT shrink-wrap"]
19. Growing one zone means re-deriving EVERY sibling pinned height in the column so bottoms still align; surplus goes into a rhythm value (gaps), never a void under a foot. [learnings 2026-08-30 "Growing one mid-row zone"]
20. No new panel designs around the retired persistent bottom bar or its 96px `::after` reserve. [guiding-star §Navigation]

## COMMON FAILURES (seen in this codebase)
- **Content-box math on a padding-box engine** — every padded box renders exactly `padding` too small; telltale: dead slack after the last tile of a "full-width" row. [learnings 2026-07-05]
- **Transform-poisoned flex**: `translate(-50%,-50%)` on a modal ancestor breaks all descendant `flex:1 1 0` width resolution — center with flex on the root instead. [CLAUDE.md; memory feedback_sbox_transform_breaks_flex]
- **Nested flex row inside a scroll column collapses** to zero width / zero layout height (even with explicit heights split across two classes — make every row rule SELF-CONTAINED). [CLAUDE.md; learnings 2026-07-06 fr-ledger]
- **`flex-wrap` grid under `flex:1 1 0` packs an extra card** per row and clips it — pin max-width. [memory feedback_sbox_flex_wrap_overflow_clip]
- **`margin-left: auto` absorbs the parent gap** and can push the pinned child past an `overflow:hidden` edge — give the text sibling `flex:1 1 auto; min-width:0`. [learnings 2026-04-28, 2026-06-08]
- **`text-align: center` on a sized span is a silent no-op** (absolute form included) — shrink-wrap the leaf + `align-items:center` on an explicit-px column host. [learnings 2026-07-12, 2026-08-30 seats]

## WHAT THIS LANE DOES NOT OWN
Accent/element/rarity color choices → **DOC-2**. Type sizes and copy → **DOC-3**. Entrance
choreography and hover motion → **DOC-4**. Cursor/ring geometry and key routing → **DOC-5**.
Which primitive to mount → **DOC-6**. Raw engine parse laws → **DOC-7**.
