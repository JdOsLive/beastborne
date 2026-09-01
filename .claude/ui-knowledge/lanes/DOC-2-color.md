# DOC-2 — COLOR (accents · reserved colors · element/rarity tokens · two-tone)

**Charter.** This lane owns every hue decision: the one-accent-per-page rule, the reserved
semantic colors (violet = cursor, gold = primary), the element and rarity token systems, the
two-tone badge recipe, surface tones, and the stroke/glow budget's COLOR half. It is a LENS —
canonical values live in `guiding-star.md` (§Color tokens, §Surface & stroke),
`Code/UI/BbTokens.cs`, `.claude/design-system/tokens/colors.html`, and dated `learnings.md`
rulings. Sources win over this summary; flag drift.

## THE CHECKLIST
1. **One saturated accent per page**, and it equals the page's PawPad app tile color — app button, bloom expansion, page chrome, all one token. [guiding-star §3; learnings 2026-07-12 "PAWPAD APP-COLOR RULE"]
2. **Violet `#9b6cff` is the cursor and nothing else** — selection, focus, kb ring. On an accented page even HOVER takes the page accent; only `.kb-focused` stays violet ("hover ≠ cursor color"). [guiding-star §3; learnings 2026-08-30 "Three hub rulings" (1)]
3. **Gold `#ffce3a`** (grad `#ffd95a→#ffb330→#f59312`, dark ink `#2a1605`) = primary / "go" / level / kicker. One primary per view. Exception on the forge: fusion's CTA is pink, gold survives only as global convention. [guiding-star §Color; learnings 2026-07-12 drift item 5]
4. Semantic accents are FIXED meanings, never recolored for variety: orange `#ee5421` live/embark, red `#E0414A` destructive, green `#3FB45E` success, blue `#3F8FE0` info, purple-deep `#7B4DDB` secondary chrome. [guiding-star §Color tokens table]
5. Surfaces: root `#04060f` · page `#0A0912`/`#0c0a18` · slab `#15121F` · raised `#1C1830` · empty slot `#131019`. Header bands sit one tone DARKER than their slab (`#110d1a` under `#15121f`) — contrast steps, not outlines. [guiding-star §Color; learnings 2026-08-28 house-dialect ledger]
6. **Element identity = two-tone**: dark saturated FILL + bright RIM, rendered as a BADGE — never a card-wide flood, never rarity-colored card borders. One hue per element everywhere it appears; dual-typed shows both badges, primary first; neutral fallback `#9AA0AD`. [guiding-star §Element identity; BbTokens.cs Element v1]
7. At CARD size (14–22px chips) show the bare element LOGO (rim-colored glyph, `transparent/` set); the rimmed badge is for FilterBar dropdown / battle results / help. [learnings 2026-08-30 SVG-raster correction, user ruling]
8. Rarity ladder goes to **Mythic**: `#9ca3af`/`#22c55e`/`#3b82f6`/`#a855f7`/`#fbbf24`(dark ink)/`#ec4899` — expressed as card top-edge tint + bottom strip ONLY. [guiding-star §Rarity]
9. Stat hues fixed: HP green · ATK red · DEF blue · SpA purple · SpD cyan · SPD gold; stat-cyan stays bluer than Wind teal. [guiding-star §Stat hues]
10. Text = warm cream `#F4F1EA`, dim `rgba(214,206,236,.6)`. Titles are CREAM; the accent lives in kicker + seams + waves, never a per-page accent title. [guiding-star §Color; learnings 2026-08-28 SURFACE & HEADER ruling 1]
11. **Stroke budget** (permitted lines only): violet cursor ring · left accent bar · bottom rarity strip · ONE optional 2px solid panel-signature strip. No hairline `rgba(255,255,255,0.1)` borders — delete and fix fill contrast. [guiding-star §Surface & stroke]
12. **No colored glow halos** — hover/selection feedback is elevation + brightness; colored glows only in static, dark, non-scroll contexts, sparingly. Dark drop shadows (`0 4px 16px rgba(0,0,0,0.5)`) are fine; never `inset`. [guiding-star §4 no-glow]
13. Brightness ladder: idle DARK and quiet, dark slabs LIGHTEN toward hover/focus, gold/bright fills DARKEN slightly (brightness .96 hover / .88 press — but never `filter` over pixel-art), press is always darkest, **hue never changes between states**. [design-system/components/online-motion-spec.html §2; learnings 2026-08-28 doc-conflict entry]
14. Active/selected chip = the two-tone recipe with the PAGE accent as rim: dark saturated fill + bright rim + white label (e.g. hub `#1c3f72` fill + `#3f8fe0` rim). [learnings 2026-08-29 skew-slab chip recipe]
15. Data-driven colors (guild emblems, user content) are blended ~70% toward the slab tone in C# (`BlendTowardSlab`) — full saturation stays on text — so a hand-sized fill can't impersonate the cursor. [learnings 2026-08-29 BuildAvatarByName]
16. There is NO scss variable layer and `var(--x)` does not parse: `BbTokens.cs` statics (inline styles) + shared primitives ARE the token mechanism. Reuse values from tokens; flag any new literal. [tokens.md SPIKE VERDICT / Path B]
17. Currency = Gold / Ink / Tokens — no "gem". [guiding-star §Currency]
18. Solid-color overrides = `background-color:` + `background-image: none` — never `background:` shorthand with a color, and a child `background-color` cannot beat an inherited gradient without killing the image. [learnings 2026-05-18 gradient-recolor entry]

## COMMON FAILURES (seen in this codebase)
- **Element hues fragmenting per panel** — ~12,400 hardcoded literals; four divergent element sets found in one audit. Always source from BbTokens/tokens, never re-derive. [learnings 2026-07-12 drift item 3]
- **Violet used decoratively** (default `#7c3aed` emblem slab, purple progress bars) reading as a stray cursor. [learnings 2026-08-29; 2026-08-28 house-dialect]
- **Mock strokes shipped verbatim** — 1px zone outlines + outlined pills = "feels AI-generated". [learnings 2026-08-28 house-dialect fix pass]
- **Near-value tones for depth** — a diagonal/fold between `#171221` and `#120e1c` reads flat; depth needs clearly different VALUES (slab vs frame). [learnings 2026-07-05 diagonal ledger v3]
- **Border-color transitions across distant hues** interpolate through mud — same-hue pairs only, or drop border-color from the transition list so the stroke steps. [learnings 2026-07-06 BORDER-MUD LAW]

## WHAT THIS LANE DOES NOT OWN
Slab construction and zone math → **DOC-1**. Type color pairings' sizes/casing → **DOC-3**.
When feedback animates → **DOC-4**. What the violet ring wraps and its geometry → **DOC-5**.
Badge/chip anatomy and which component renders them → **DOC-6**. Gradient/color parse laws → **DOC-7**.
- **HDR GLOW RULES (engine 26.09.01, user-tuned live 2026-09-01):** the scene has Bloom (threshold 1) and the HUD renders before post-process, so any colour > 1.0 blooms. (a) Bloom acts on the BLENDED pixel — for translucent layers alpha × multiplier × channel must clear ~1.2 (e.g. `rgba( #2dd4bf * 8, 0.20 )` at a gradient's centre stop only); (b) thin strokes need hot values, fills/text bloom easily; (c) the cursor ring is ONE stroke (`4px solid #9b6cff * 5`) — outline halos read as a second ring; (d) ×8 on a saturated colour tone-maps toward white ("the inside is too bright") — keep cores ×3–5, put heat in halos/centres; (e) HDR works in `linear-gradient()`/`radial-gradient()`; images/PNGs cannot be multiplied — add a gradient glow layer beneath them; (f) RATION: glow marks the object of attention (cursor, the beast's stage light/dais, a primary on press, presence dots, hero numbers) — never chrome, never chips, never body text. [learnings 2026-09-01 HDR entries]
