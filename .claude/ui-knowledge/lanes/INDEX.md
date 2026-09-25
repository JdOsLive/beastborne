# Beastborne UI — Lane Doc Registry (DOC-1 … DOC-7)

Prebuilt reference lenses for the multi-agent page-build pipeline. Agents cite these **by
number** ("per DOC-4 §checklist 6", "DOC-7 group 3") so briefs, reviews, and commit notes can
point at a rule without pasting it. Each doc is a LENS, not a fork: it carries the lane's
charter, a testable checklist with source pointers, the lane's known failure modes, and its
boundaries. **The canonical truth stays in the sources** — `guiding-star.md`, `learnings-archive.md`,
CLAUDE.md's quirks table, `style-guide.md`, `.claude/design-system/` cards, the primitive
source files — and every checklist item names its source so drift is checkable. When a lane
doc and its source disagree, the source wins; fix the lane doc and note the drift.

| # | Lane | Charter line |
|---|------|--------------|
| **DOC-1** | [Layout](DOC-1-layout.md) | Zones, slabs, radii, page-header anatomy, spacing, and all pixel math under the padding-box model. |
| **DOC-2** | [Color](DOC-2-color.md) | One accent per page, reserved colors (violet = cursor, gold = primary), element/rarity tokens, the two-tone recipe, stroke/glow budget. |
| **DOC-3** | [Type & Copy](DOC-3-type-copy.md) | Exo 2 rules, the size ladder, the 12px floor, casing/tracking, and the plain-words copy voice. |
| **DOC-4** | [Motion](DOC-4-motion.md) | FLOW/ALIVE/SNAP tiers, transitions-vs-keyframes, stagger ladders, smash pop, the living ring's character, entrances, what never moves. |
| **DOC-5** | [Input](DOC-5-input.md) | The key grammar (WASD/Space/Q/R/Z-X/E-silent), one-cursor + one-device model, ring/slab cursor costumes, hover laws, TickInput/UIModalState/NavManager routing. |
| **DOC-6** | [Components](DOC-6-components.md) | The Bb primitives (Button/Diamond/HeaderWaves/IconScroll/SectionHeader/Stamp), FilterBar + chip recipes, beast card, scroll-grid and framed-tile recipes, when-to-use. |
| **DOC-7** | [Engine Laws](DOC-7-engine-laws.md) | The hard s&box constraints no design overrides — a symptom-grouped index into the CLAUDE.md quirks table and learnings-archive.md. |

**Reading order for a page build:** DOC-7 first (what can't work), then DOC-1 (carve the
page), DOC-2/DOC-3 (paint and write it), DOC-6 (assemble from primitives), DOC-4/DOC-5
(make it move and drive). A reviewer works the same list backwards.
