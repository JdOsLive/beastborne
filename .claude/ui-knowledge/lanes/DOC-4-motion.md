# DOC-4 — MOTION (three layers · transitions vs keyframes · ladders · pops · entrances)

**Charter.** This lane owns everything that moves: the FLOW/ALIVE/SNAP doctrine, the
transitions-vs-keyframes decision tree, stagger ladders, the smash pop, the living ring's
motion character, entrance grammar, celebration setpieces, and the list of things that never
move. It is a LENS — canonical truth lives in `guiding-star.md` (§Motion),
`.claude/design-system/components/online-motion-spec.html`, `tokens/motion.html`, and the
dated `learnings-archive.md` animation laws cited below. Sources win; flag drift.

## THE CHECKLIST
1. Every motion belongs to exactly ONE layer — **FLOW** (slow ambient, the scene, never controls) / **ALIVE** (the violet ring ONLY, glide 0.27s `cubic-bezier(0.22,1,0.36,1)` + ink-lean wave) / **SNAP** (finite ≤150ms on the control you touched). Can't name the layer → the motion is wrong. [guiding-star §Motion table]
2. **Surfaces are SOLID** — panels/buttons/cards never deform, ripple, or breathe. HARD VETO on wave/skew motion on large surfaces near focus (motion sickness; PLAY's liquid gold vetoed twice — its fill is static by design). [guiding-star §Motion rules; learnings 2026-07-12 liquid-ring entry]
3. Player input creates the sharpest motion on screen — nothing ambient moves as fast as interaction feedback. [guiding-star §Motion]
4. **State changes use `transition:`** (re-fires on every class toggle); `@keyframes` only for (a) genuine infinite ambient loops and (b) FRESH-MOUNT one-shots. A re-opened persistent panel never replays keyframes. [CLAUDE.md @keyframes row; learnings 2026-06-03 multi-phase setpieces]
5. Branch-swap views (`@if/else`) animate FREE via mount keyframes; same-element replays need a **render-key class** (`.pop-@version`, `.slam-@key`) — and motion classes for dynamic rows are CACHED per row id, because a class-string change IS a remount. [learnings 2026-07-05 BRANCH-SWAP; 2026-08-28 LIFE pass]
6. **`animation-delay` is murdered by re-renders** — on any panel that re-renders (especially `GlobalFrame` in BuildHash: audit target = ZERO `animation-delay` in the file), bake staggers as full-length t=0 keyframes with percentage holds (`hold% = delay/(delay+dur)`), one keyframe block per ladder step. New work = percentages ALWAYS; verified-live legacy delays = don't churn. [CLAUDE.md animation-delay row; learnings 2026-07-12 fr-* conversion + Beastbook delay sweep]
7. A `transition` SHORTHAND resets `transition-delay` to 0 — use longhands when the delay comes from an inline style. [learnings 2026-06-03 delay-clobber]
8. Entrances: staggered class-toggle transitions; drive with **opacity** when a transform would poison child flex widths; entrance translateY only small + downward + resolving to 0 under an opaque header. Persistent layers need a stage-0 **priming frame** so the first class flip is a real change. [guiding-star §Motion; learnings 2026-06-03 entrance-transform + Day-7 priming]
9. **Smash pop** (hover/kb-land): motion is OF THE GLYPH — `scale(1.16) rotate(-6deg)` on 0.18s `cubic-bezier(0.34,1.56,0.64,1)`, pre-rotated diamonds → `rotate(39deg)`, circles scale-only; TEXT STAYS PUT; `:active` → `scale(0.94)` 0.08s. Non-scroll ring hosts only. [learnings 2026-08-28 SMASH POP + "hover is OF the icon" amendment]
10. Hover-gated infinite loops fire on `:hover` ONLY — never also on `.kb-lit` (the cursor always rests somewhere; that's a second ambient loop in disguise). [learnings 2026-08-29 v3.3 ring-costume entry (b)]
11. Scroll-grid cards: hover/selected = border + background ONLY — no transforms, no box-shadow (scroll containers don't clip either); overhanging badges sit inside card bounds. [CLAUDE.md scroll-clip rows]
12. **Animations beat transitions/statics on a shared property**: an ambient box-shadow loop hides state box-shadows; a static `opacity` never applies under an opacity keyframe (intensity knobs live INSIDE the keyframe steps). Split parent/child or animate different properties. [learnings 2026-06-05 lh-play; 2026-07-05 dais]
13. A keyframe animating `transform` CLOBBERS the element's static base transform — bake the base (skew/rotate/translate offsets) into EVERY keyframe step, and author transitioning transform states with the same-SHAPED function list (identity values in the base). [learnings 2026-06-03 clobber; 2026-06-09 same-shape]
14. Volatile-class hosts (`.kb-focused`, armed states) carry NO mount animations — the entrance would replay on every cursor hop; scope pops to a RARE sibling state class instead (`.pb-new`, `.slotted`). [learnings 2026-07-05 BRANCH-SWAP trap; 2026-07-12 pb-tile]
15. Asymmetric enter/exit without delays: put a different `transition` on each STATE rule (the destination rule's transition runs); later starts come from Time-stamp beats in `Tick()`. [learnings 2026-08-30 bloom disc]
16. **The gold snap is RATIONED** — the ring's gold flip + 0.09s slam fires only on consequential commits (PLAY, fuse, release, contract); everyday activations keep the quiet press. [learnings 2026-07-12 USER RULING]
17. Big celebratory setpieces are reserved for real milestones; spend the time UNEVENLY, put payoff sounds on the REVEAL (silence builds suspense), and prefer one wrapper-level variant animation over per-child rules. [guiding-star §Motion; learnings 2026-07-06 LOOM-HELIX]
18. What never moves: phone OS chrome (the PawPad is FLAT — no skews/tilts on device elements; seam bars are the one angle), the PLAY fill, panel surfaces, anything ambient at SNAP speed. [learnings 2026-08-28 FLAT-PHONE ruling; guiding-star §Motion]
19. Ambient animated decorations near a scroll live in a NON-SCROLLING band between cap and scroll, never inside the scroll (transformed children escape clips). [learnings 2026-07-05 Batch 2]
20. Sub-second choreography can't be verified over MCP screenshots — build `dev_*` freeze ConCmds (dev_fusefx pattern) and verify END STATES (everything present, console clean). [CLAUDE.md debug-commands row; learnings 2026-07-12 MCP latency]

## COMMON FAILURES (seen in this codebase)
- **Stagger ladders playing 1–2 steps then dying** (fusion weave 2 of 6 rungs; Beastbook's entire T3 reveal silently truncated) — animation-delay murder. [CLAUDE.md; learnings 2026-07-12]
- **Class-swap phases on a still-mounted element playing only the first phase** — Day-7 setpiece; transitions for persistent elements, mount/unmount for one-shots. [learnings 2026-06-03]
- **Hover wiggles running forever beside the resting cursor** (kb-lit gated loop). [learnings 2026-08-29]
- **Label shoves rejected** — "the hover should be OF the icon, not the hover moving things". [learnings 2026-08-28]
- **`filter: brightness()` hover on tiles containing pixel art** softening sprites — swap gradients instead (0ms is legal SNAP). [learnings 2026-08-28 quickplay tile]
- **Shared keyframe + per-element static transform** = all particles animate in place; one keyframe per mote, travel baked in. [learnings 2026-06-03 sparkPop]

## WHAT THIS LANE DOES NOT OWN
Which element is the cursor and where it may go → **DOC-5**. Hover brightness POLARITY values
→ **DOC-2**. Zone/slab geometry the motion plays on → **DOC-1**. The primitives that carry
canonical motion (BbStamp tilt, BbHeaderWaves drift) → **DOC-6**. Parse/replay engine laws → **DOC-7**.
