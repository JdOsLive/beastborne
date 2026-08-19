---
name: angled-ui-featured-card
description: Main-menu angled UI — SOLVED via "Approach A" (one transform per group). 3D perspective kept; bottom card row is ONE shared-camera plane so cards are continuous + match the featured.
metadata: 
  node_type: memory
  type: project
  originSessionId: d451fa20-5528-48d8-b237-b196569b2c27
---

Main-menu "angled UI" (Persona-style). **KEY REALIZATION (2026-06-08): the look the user wants is a UNIFORM SKEW, not a 3D perspective recede.**

**Why perspective-rotateY failed:** with `perspective() rotateY()`, the edge "dip" (slant) scales with element SIZE — a wide/tall featured card dips far more than a small card at the same angle. So the featured and the bottom cards NEVER matched no matter the angle. Spent many iterations tuning per-element angles (featured 4°/7°/10°, cards 7°/10°/18°) trying to make them look consistent — impossible by construction. The user finally articulated it: **"the dip of the line should always stay the same size"** = a skew (size-independent dip), not perspective. User chose "Uniform skew" when asked.

**Also confirmed dead-ends (s&box, no preserve-3d):** transforming the parent (`.lh-cards` or `.lh-main`) to make one shared plane → flattens children / poisons child flex-grow widths (the featured text column collapses). Per-card negative `transform-origin` to fake a shared far pivot → artifacts (card shifts up + bottom cutoff; far perspective origin is mishandled). These are why a true unified perspective plane isn't achievable here.

**BINARY-CONFIRMED LIMIT (2026-06-08): s&box has NO standalone `perspective` PROPERTY and NO `transform-style: preserve-3d`** — read directly from the shipped DLLs (`BaseStyles` property surface has only `transform`/`transform-origin(-x/-y)`/`perspective-origin(-x/-y)`; no `Perspective` member, no `TransformStyle`). Only the `perspective()` transform-FUNCTION exists (per-element). So the "shared camera = perspective on a parent + preserve-3d wrapper" fix (the standard CSS way to make panels share one vanishing point) is IMPOSSIBLE here. Per-element `perspective()` = each element its own camera = the wide featured and narrow cards CANNOT share a dip. EMPIRICALLY CONFIRMED in-engine 2026-06-08 (test commit 7984c57, reverted eaae3fd): adding `perspective: 1300px` + `transform-style: preserve-3d` logged "Generic 1300px is not valid with perspective" and "Generic preserve-3d is not valid with transform-style" — the parser rejects both outright. This is a hard engine limit, NOT a tuning problem — do not re-investigate. Method note: inspect these .NET-10 DLLs with `System.Reflection.Metadata` (not `GetTypes()`, which fails to resolve System.Runtime 10).

**RESOLUTION (2026-06-08) — "APPROACH A" (single transformed wrapper per group). WORKING, user thrilled ("YOU DID IT! FINALLY!").** The thought-experiment agents + research confirmed: the ONLY way to a true shared camera/continuous plane in s&box is to put the panels under ONE transform (you can't share a camera across separate elements — no perspective property/preserve-3d). So:
- Bottom card ROW `.lh-cards` carries ONE `perspective(2000px) rotateY(10deg)`; the two card slots are COPLANAR children of it (so they share ONE vanishing point → Community continues Roadmap as one continuous receding plane, real depth). Slots need EXPLICIT widths (`flex: 0 0 calc(50% - 13px)`) because flex-grow is poisoned under a transformed ancestor.
- The row's `transform-origin: left -374px` (NOT center) points its vanishing point UP at the FEATURED card's, so the gap between the featured and the card row stays uniform/parallel ("straight line") instead of opening up. The -374 = ~featured H/2 (695) + the gap; re-derive if sizes change. (The cursor's pill nudge uses the same -374 for idx 9/10.)
- Featured card + sidebar buttons each keep their OWN `perspective()+rotateY(10deg)` (separate planes, but all 10° + similar widths so they read consistent).
- Earlier detour (rejected): a full uniform-skew build (8693bcf) — user said "no i dont think that looks right", reverted. The per-element dip-tuning (cards 13°/770px) was superseded by Approach A.

Brief history (the journey): tried per-element angle-tuning, computed per-element perspective (770px), negative transform-origin, uniform skew — all fell short before landing on Approach A (one wrapper per group). Current angles: sidebar buttons 10°, featured 10°, cards 13° (was 18° — too dramatic). The cards will never perfectly match the featured's dip (size physics); user accepts this. Tune the card angle (`.lh-card-slot` rotateY + the pill `CursorRot` 9/10 in lockstep) if needed. Do NOT re-pitch skew unless the user asks.

**(Archived plan — the skew approach, NOT chosen): replace perspective-rotateY with `skewY(S)` everywhere** (one constant angle ~3°, same for sidebar buttons, featured card, both cards → identical dip). Skew the SHAPE, keep CONTENT upright (counter-skew, or for the featured skew only `.lh-feat-shot` so the sibling text/beast stay upright). Cursor simplifies massively: just `skewY(S)` (constant — no perspective, no per-item pivot, no visual-displacement nudge). Skew is size-independent so there's no flush/consistency problem and no width/height math. Direction: edges dip DOWN to the right (skewY positive; flip sign if wrong). Tunable angle.

**Pre-skew committed state:** everything was `perspective rotateY` (buttons 1300/10°, featured 2000/10°, cards 2000/18°) with an imperative `PanelTransform` cursor + visual-nudge (commit ~`b8b81bc`). See [[living-selection-cursor]] for the cursor internals being replaced.
