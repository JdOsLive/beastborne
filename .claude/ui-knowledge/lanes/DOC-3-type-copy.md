# DOC-3 — TYPE & COPY (Exo 2 · sizes/weights · 12px floor · voice · casing)

**Charter.** This lane owns everything written and how it is set: the Exo 2 family rules,
the size/weight ladder, the italic doctrine, the 12px player-facing floor, casing/tracking,
and the plain-words copy voice. It is a LENS — canonical truth lives in `guiding-star.md`
(§Typography, §Voice), `learnings-archive.md` dated rulings, and the shipped font registrations in
`GameHUD.razor.scss:1-34`. Where guiding-star and shipped code disagree (2 cuts vs 6
registered families), THE CODE WINS. [learnings 2026-07-12 drift item 1]

## THE CHECKLIST
1. **Exo 2 only.** Italic is its OWN registered family: `font-family: Exo2Italic; font-style: italic;` — s&box doesn't honor `@font-face` style matching. Fonts live flat in `Assets/fonts/` root. [guiding-star §Typography; CLAUDE.md fonts row]
2. Build hierarchy from **size + italic + color**, not weight steps — every `font-weight` is faux-synthesized off Bold and reads heavy. [guiding-star §Typography ⚠]
3. **`line-height` is always px** (unitless = multiplier post-26.06.03); for 30px+ type, line-height ≥ font-size or text clips; italic display wants line-height ≥ font-size + 2 for slant headroom. [CLAUDE.md first quirks row; learnings 2026-05-01 punch list]
4. Menu-proven display ladder: hero 76/900 · scene title 52/900 · PLAY 38/900 · page title ~38 italic-900 UPPERCASE · nav/card 26 · body 21/500 italic · CTA 20/900 · kicker 13–16/900 UPPERCASE ls 0.2em · key-cap 11/900. Panel-internal type stays smaller (section header 11 uppercase, body 13). [guiding-star §Typography; style-guide.md]
5. **12px floor for player-facing text**; 11px survives only inside the house chip/stamp recipes. [learnings 2026-08-28 "BIG THINGS BIG / NO NOTHING"]
6. Italic is the default voice for headlines, hero names, kickers, CTAs, card titles; dense data/tables stay roman for legibility. [guiding-star §Typography]
7. **Italic/skew restraint**: one hero stamp/skew per surface; italic-stamp numerics reserved for one or two setpiece numbers — everything else upright. "We don't always need that angled box look." [learnings 2026-04-28 Persona-stamp restraint]
8. Kickers are UPPERCASE, wide-tracked, accent-colored — and on the phone, ONE word ("MESSAGES", not "TAMERLINK OS · MESSAGES"). [learnings 2026-08-29 NO-BOX ruling 1]
9. Titles start at the margin; nothing crams the title row inward. [learnings 2026-08-29 NO-BOX ruling 2]
10. Voice: short, confident, imperative ("Embark", "Out now."); lore warm and a little mythic; dry wit; rare exclamation points; never explain what the player can already see; numbers brag for themselves ("PWR 916"). [guiding-star §Voice]
11. **Plain words, no theater nouns.** Theme lives in styling, never nouns. Online-page banned list: marquee, tonight's card, promoter, ringside, broadcast, fight night → BATTLE / FIND A BATTLE / PRACTICE VS AI / LEADERBOARDS / TRADE OFFERS / GUILD / ONLINE NOW / ACTIVITY. [learnings 2026-08-28 v3 ruling; online-simple-v3.html]
12. A label doesn't restate its button ("PRACTICE", not "PRACTICE VS AI" beside a BATTLE THE AI button); prefer the one-word label at the shared size over shrinking/wrapping. [learnings 2026-08-29 v3.3 ruling 3]
13. Terminology: "**Contract**" not "catch"; no loyalty-system references; jargon (MMR/Elo/rating) never player-facing — "levels matched fair". [memory project_terminology; learnings 2026-08-28 mock rulings]
14. Ghost-set honesty copy: "took the set against X's squad", never "beat X"; results framed from the winner's client only. [learnings 2026-08-28 GHOST-SET COPY PATTERN]
15. Empty states are honest AND actionable ("Coming with the trade board"), never grey text in a void; a pressed future-feature control answers with copy, not a deny. [learnings 2026-08-28/29 POST OFFER entries]
16. No `→` in Exo2 text — the glyph is absent from the cmap and falls back ugly at display sizes; use a `lucide:arrow-right` iconify. `·` middle-dot is safe. `‹` is unverified. [learnings 2026-08-29 U+2192 entry]
17. Wrap ALL text in `<span>`s (bare text in flex renders vertically), and split any text node mixing multiple `@` interpolations with literal separators into sibling spans + flex gap. [CLAUDE.md bare-text row; learnings 2026-07-13 whitespace-collapse]
18. UI teaches through the interface: one first-run hint / empty-state nudge in context — never stacked tutorials or explainer paragraphs. [guiding-star P6]

## COMMON FAILURES (seen in this codebase)
- **Unitless line-height ballooning layouts** after 26.06.03 — the change that broke the whole UI once. [CLAUDE.md first row]
- **Every stamp italicized/skewed** until the page reads as a tic — restraint pass required on ProfilePanel. [learnings 2026-04-28]
- **Two-label kickers and crammed title rows** on the phone headers (rev 5 → rev 6 rulings). [learnings 2026-08-29]
- **Interpolation squash**: `<span>LV @tier · @title</span>` rendered "·LV 1Novice". [learnings 2026-07-13]
- **Centered text via `text-align: center` on a sized span** — silent no-op in-engine; shrink-wrap + parent centering instead (full law in DOC-7). [learnings 2026-07-12]
- **Key-cap restyling**: enlarging `.cb-key` or changing its line-height floats the glyph — native passthrough only. [learnings 2026-05-18 cb-key saga]

## WHAT THIS LANE DOES NOT OWN
Where the header sits and its band anatomy → **DOC-1**. Which color the kicker takes →
**DOC-2**. Text entrance/stagger animation → **DOC-4**. Key legends and cap placement logic →
**DOC-5**. The `.bh-*`/BbSectionHeader markup itself → **DOC-6**. Font/parse engine laws → **DOC-7**.
