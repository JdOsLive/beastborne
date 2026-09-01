# DOC-6 — COMPONENTS (Bb primitives · house recipes · when to use what)

**Charter.** This lane owns the shared building blocks: the `Code/UI/Primitives/Bb*`
components, FilterBar and the chip recipes, the beast card, the scroll-grid and framed-tile
recipes, and the "copy the canonical, don't invent" discipline. It is a LENS — canonical
truth is the PRIMITIVE SOURCE FILES themselves (each carries its contract in a header
comment), `style-guide.md` (component anatomy — wins on anatomy), `guiding-star.md`
(§Components), and `.claude/design-system/components/*.html` (rendered twins with ENGINE
NOTES). Shared primitives ARE the token mechanism (tokens.md Path B) — reuse, never re-paste.

## THE CHECKLIST
1. **BbButton** (`Code/UI/Primitives/BbButton.razor`) is THE button: tiers primary (gold, one per view) / commit (red, rationed) / secondary (slab + diamond slot) / ghost / danger (hold-to-confirm); sizes 56/44/34, main flows ≥44; no transforms; `Focused=true` lights the concentric ring. MEASURE the label before fitting two CTAs in a lane (~10px/char italic-900 @16); when the primitive doesn't fit, port the nearest IN-PANEL recipe rather than shrinking the label. [BbButton header; learnings 2026-08-29 two-CTA entry]
2. **BbDiamond** — the 45° action shape: tint to FUNCTION (Item blue, Fuse pink, Release red), counter-rotated glyph, root bounding box = Size × 1.45; never on nav/Play/passive chrome; in lists the diamond sits INSIDE a rectangular button so rows align. [BbDiamond header; guiding-star §Components]
3. **BbHeaderWaves** — the page-header wave band. Host recipe is MANDATORY: mount as first child, host DROPS its own background-color (paint-behind law), `.my-header > .bhw-root { position:absolute; inset 0; z-index:0 }` + `> *` lift FROM THE HOST SHEET. New accents = just bake `Assets/ui/wave-<accent>-a/b.png` (make-waves.ps1) — zero component code. Seam laws: native-size tiling, edge-interior bake, integer geometry; a wave tile must be baked at the PERIOD of the surface that shows it. [BbHeaderWaves header; learnings 2026-07-05 7A; 2026-08-28 accent + phone-period entries]
4. **BbIconScroll** — the living panel background (app glyph drifting): first child of a panel whose root paints NO fill; zero-footprint root, imperative drift. [BbIconScroll header]
5. **BbSectionHeader** — kicker + italic title + accent flare rule for SECTION heads. The PAGE title is NOT this component — pages use the hand `.bh-*` markup (kicker / 38px italic-900 cream title / skewed seam bars `42×12 skewX(-24°)` + dim tail). [BbSectionHeader header; learnings 2026-08-28 header law (1)]
6. **BbStamp** — tilted status pill: ±3° ONLY, straightens on hover; ONE hero stamp per surface (singular-accent law); `Dot=true` for the 1.6s live-dot. [BbStamp header; learnings 2026-04-28]
7. **FilterBar** (`Code/UI/Components/FilterBar.razor`) is a DROPDOWN cluster (element/rarity/sort + `@ref`-polled search) — it has no segmented mode. Segmented filter rows port the **skew-slab chip recipe**: 36px, `skewX(-8deg)` slab, counter-skewed pointer-inert children, content-sized; active = two-tone with the PAGE accent rim. [learnings 2026-08-29 "Make the filters look like My Beasts'"]
8. **Beast card** anatomy is fixed: level pill top-left · fav/multi-select top-right · PWR bottom-right over art · name + element badge(s) at foot · rarity = top-edge tint + bottom strip; art is the biggest thing; never drop-shadow the sprite. [guiding-star §Components]
9. **Scroll-grid recipe** (the roster-grid pattern, verbatim): parent column `overflow:hidden` + child `flex:1 1 0; min-height:0; overflow-y:scroll; overflow-x:hidden; flex-wrap:wrap; align-content:flex-start; align-items:flex-start` + `width` AND `max-width` pinned; items as direct children; empty state = an `.empty` class flipping the grid to a centered column (no wrapper div). [CLAUDE.md scroll row; learnings 2026-08-29 hb-grid]
10. A list of things WITH ART is a **card grid** (roster/bag/shop), never a full-width row — full-width rows strand the art from the action. [learnings 2026-08-29 Trade Board FEEDBACK]
11. **Framed beast tiles** = the pennant recipe: fill `BlendTowardSlab(pageAccent, 0.70)`, no rim, radius = host radius, whole beast centred (`.ts-tile.framed`, OnlineHubPanel). Portraits/crops use `SpriteCrop`/`SpriteCropRect` — a native-px window with negative margins (overflow:hidden clips in-flow children), bias 0.45 horizontal (sprites face left) / 0.22 vertical (head band); NO object-fit, NO filter, NO transform on the img. [learnings 2026-08-30 framed tiles; 2026-08-29 VIEWPORT CROP]
12. **People are ROUND, beasts are SQUARE** — tamer heads are circles (2px rim, gold on YOU); beast portraits are square tiles. [learnings 2026-08-29 v3.3 ruling 1]
13. Sprites: `image-rendering: pixelated`, INTEGER scale computed from `Texture.Load` dims (sources are 80–256px, not uniform); explicit px inline. Downscaled decorative art (<1×) drops `pixelated` and uses low opacity, never a filter. [learnings 2026-08-28 sprite sizes; 2026-08-29 art-fade]
14. Detail panels stack: identity → lore → actions → stats → moves/traits — glanceable up top, deepest data last. Trait/nature TOOLTIPS ARE RETIRED game-wide: write effect text INLINE (register rows) or icon+name chips only. [guiding-star §Components; learnings 2026-07-13 ruling]
15. Section heads inside dossier columns = the chip grammar (`.bbf-head` accent chip + `.bbf-cut` corner cuts where the angled dialect applies); overlay scrollbar = the imperative track/thumb recipe (scrollbar-color/-width don't parse). [learnings 2026-07-12 handoff; 2026-07-05 overlay scrollbar]
16. The gold key-cap `.cb-key` (GameHUD.razor.scss:860) is reused ONLY as a native passthrough — never resize, never change its line-height; panel-scoped class may adjust opacity/tracking/upright only. [learnings 2026-05-18 cb-key]
17. Iconify: lucide preferred; NEW icon names need the user to refresh the s&box iconify addon; verify names via `api.iconify.design` before shipping (two 404s shipped in trait data). [memory feedback_iconify_addon_refresh; learnings 2026-07-13]
18. Discipline: copy the canonical implementation, don't invent; reusable parts get CAPTURED into shared components, not pasted; when a shipped pattern changes, re-sync its design-system card (record the baseline commit hash in the card). [guiding-star §sweep table row 4; learnings 2026-08-28 card-sync law]

## COMMON FAILURES (seen in this codebase)
- **Re-implemented buttons/pills drifting per panel** — the root cause of "different UI style everywhere". [guiding-star §sweep]
- **BbHeaderWaves host keeping its own background-color** → invisible waves (paint-behind law); component-root absolute from its own scss unreliable. [BbHeaderWaves header]
- **Retrofit tells**: porting old subtrees into a "rebuilt" column is detectable — fresh class namespace, parallel skeleton, port data one section at a time, orphan-grep. [learnings 2026-07-12 RETROFIT-TELL LAW]
- **Themed skeuomorphism** — "museum is the CONCEPT, Beastborne is the MATERIAL": take a theme's structure, render it in the game's own vocabulary. [learnings 2026-07-12 USER LAW]
- **Scaled sprite tiles** blurring pixels (object-fit contain at 0.875×) — crop, don't scale. [learnings 2026-08-30]
- **Element icons mushy at chip size** — SVG rasterizes at VIEWBOX units: author at 24, emit at viewBox 240 (`scale(10)` group); width/height hints don't fix it. [learnings 2026-08-30 viewBox correction]

## WHAT THIS LANE DOES NOT OWN
Zone math and slab construction → **DOC-1**. Token values the primitives paint → **DOC-2**.
Label copy on the components → **DOC-3**. The motion the primitives carry → **DOC-4**. The
cursor that visits them → **DOC-5**. Why the engine forces these shapes → **DOC-7**.
- **ACTIONS NEVER SKEW (user-affirmed 2026-08-31):** every pressable is a ROUNDED BbButton-family slab (r16 / r11 small); skew (−12° chips / −24° seams / ±3° stamps) is reserved for NON-interactive identity marks (kickers, seams, stamps). The hub's skewed OPEN GUILD chip was the violation that minted this rule — it also forced a special ring costume, the retrofit tell. [learnings 2026-08-31 entry lands with the hub fix]
- **DOC SYNC (audit 2026-09-01):** people avatars are ROUNDED SQUARES r9 (user ruling 2026-08-29 — any older 'people are round' phrasing in this doc is superseded); the skewed filter-chip recipe is grandfathered for EXISTING FilterBar-family chips only — no NEW pressable ever skews (the §actions-never-skew law wins on conflict). Guild emblem hero stop = 112×112 r12 everywhere.
