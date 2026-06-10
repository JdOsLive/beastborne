# Scene-Swap Spec — "Deal-Out / Deal-In" (2026-06-09)

The canonical transition system for ALL sibling screen changes, game-wide. Researched
from Persona 5 (CEDEC 2017, Masayoshi Suto) + Splatoon (GDC 2018) mechanics; designed
against Beastborne's existing deal-in machinery. User mandate: full scene swap —
"all the elements move off and then the new elements come on" — NOT drawers/scrims.

## The distilled P5/Splatoon rules
1. **Depart fast + together (≈160ms, ease-in, NO stagger); arrive staggered + eased.**
   Spend nothing on exits, everything on entrances. Overlap the timelines — arrival
   starts while departure opacity tails finish (the perceived-speed trick).
2. **The clicked control dies LAST** (holds + fades with ~60ms delay — your input
   visibly causes the scene to shatter).
3. **No cover for sibling swaps.** The branded diagonal wipe is ONLY for context
   changes (menu→game, battle entry, expedition start). Two tools, never mixed.
4. **Exit along your own entrance vector, over-rotated** — the angled planes are rails.
5. **Stagger is micro** (group starts 0/70/140ms; within-group rows 0/50/100/150ms).
6. **Input outruns animation**: sound on INPUT; cursor live before settle; arrival
   interruptible (mid-flight poses become the next departure's start poses — never
   make the player wait for choreography twice).
7. (Suto) Angle = wayfinding: screens at different depths may own different rest
   angles — the tilt itself says where you are.

## Departure (menu → out), total 160ms, all at t=0
| Group | Mechanism | Motion |
|---|---|---|
| Clicked control | `.depart-hold` | holds pop, fades 0.10s ease-in w/ 0.06s delay (dies last) |
| Featured plane | imperative lerp, rate 0.28/frame | x 0→+90, deg 10→17 (over-rotate out right) + `.departing` opacity 0.16s ease-in |
| Cards row | imperative, same | same, t=0 |
| Sidebar slab | class-toggle | translateX(-60px) + opacity, 0.14s ease-in |
| .lh-main shell / pill | opacity ONLY (flex-poison rule) | 0.14s ease-in |
- pointer-events: none at t=0. No exit sound (sound fired on input).
- t=170ms TIMER (never "wait for the lerp") → swap frame.

## Swap frame
Old scene wrapper `display: none`; new wrapper `display: flex` mounted CLOSED for one
rendered frame (the proven mount-then-open rule), then staggered arrival begins.

## Arrival (destination), interactive ~+250ms after swap
| Slot | Mechanism | Start |
|---|---|---|
| A — identity/header | SlabLeft: translateX(-40px)→0 + opacity 0.30s decelerating | +0ms |
| B — hero content plane | imperative deal-in x 70→0, deg 15→10 (or →0, see decisions), rate 0.16/frame; opacity class-toggle 0.30s | +70ms |
| C — secondary/footer | FadeOnly or SlabLeft | +140ms |
| Within-group rows | transition-delay 0/0.05/0.10/0.15s + `.settled` zeroing | rides B |
Interactive when A+B settle (~420ms from input); C is invisible tail.

## State machine (in MainMenu.razor root; both scenes are siblings)
`SceneId {Menu, Roadmap, Options, PatchNotes}` · `SwapState {Idle, Departing, Arriving}`
· `_pending` input buffer (destination input during Departing queues; during Arriving
INTERRUPTS — tweener poses reverse in place). Generalize UpdateDealIn into a
list-driven `PlaneTween` tweener (el, x, deg, targets, rate; null inline transform on
settle). BuildHash adds `_scene/_swap/_pending/stage`.

## Cursor handoff (ALIVE-layer signature)
Cursor STAYS VISIBLE through the swap and glides to the new scene's first focusable
(retarget at swap+1 frame when Box.Rect is valid; its 0.27s left/top transition does
the trip; lands as the scene becomes interactive). New scenes register CursorRot
entries per focusable. Fallback if it reads chaotic: hide at t=0, 0.12s
opacity+scale "ring lands" at settle.

## Return trip (Escape): mirror, but cheaper
Panel departs 140ms; menu re-arrives with HALVED staggers (0/40/80) + rate 0.20.
Menu stays mounted (display flip only) — no splash re-stamp on returns.

## Budget
Input→interactive ≤ ~420ms forward, ~350ms back. Hard bar: 450ms.

## Recipe — what any screen declares to join the system
1. 2–4 groups, each a @ref + PlaneKind:
   - `Angled` — owns perspective(2000) rotateY steady CSS, explicit-width internals
   - `SlabLeft` — fixed-width chrome, class-toggle translateX+opacity
   - `FadeOnly` — anything with flex-grow descendants (opacity only; optional
     decorative angled backdrop slab behind it carries the plane motion)
2. Stagger order in (exit is always all-at-once)
3. First focusable + CursorRot entries
4. Hero designation (which control gets `.depart-hold`)
GameHUD becomes a second host later (extract tweener+state machine to a shared
helper when the rollout starts). Battle/expedition/menu→game keep the diagonal wipe.

## DECISIONS (2026-06-09, user-confirmed)
1. **Backgrounds angled, text flat** (user call): content scenes rest with ANGLED
   decorative backdrop slabs (the FadeOnly-+-angled-backdrop pattern) while the
   text/content itself sits FLAT for reading. Content may still ARRIVE through the
   angle (deg 14→0). Hero-art scenes may rest fully tilted.
2. **Cursor cross-glide ON** (user: "better than teleport"; fallback documented).
3. **Arrival sound**: reuse `SoundManager.PlayStamp` at low volume for the Group-B
   land beat for now; dedicated `ui_scene_land` cue next sound pass.
4. **Options keeps its dual life** (Docked scene here / centered modal in-game) —
   AND gets a REINVENTION pass (user): implement the settings that don't work yet,
   remove the dead ones. Audit in flight; implementation follows the scene-swap.
5. **Bottom cards react on pill selection** (user): selecting View/Join should
   animate the CARD too, not just the pill — e.g. the card leans/lifts subtly or
   its accent edge flares wider. Card-level acknowledgement, SNAP-layer timing.

## Constraint compliance (engine)
No perspective() in @keyframes (detonation-flash lesson) · planes move imperatively
ONLY (class-toggle transform = snap) · transforms only on elements that already own
them; FadeOnly for flex-grow hosts · transition-delay needs `.settled` zeroing ·
scene wrappers flip display flex↔none · steady CSS transform chain must match the
imperative chain shape for seamless null-on-settle.

## V2 AMENDMENTS (2026-06-09, post-first-pass fixes + full-width redesign)
1. **NEVER null an OUTGOING plane's inline transform at the swap frame.**
   Style.Transform applies same-frame; the wrapper's display:none lands on the
   NEXT rendered frame → 1-2 frames of the menu at its steady pose ("departure
   flash", user-visible). ResetPlanesForSwap now only halts tweens + pre-poses
   the INCOMING backdrop. Outgoing planes keep their flown-out pose while
   hidden; the next arrival re-seeds them (StartTween seeds when inactive;
   PlayEntranceSequence nulls on fresh entrance).
2. **Cursor retarget is a POLL, not a timed set.** `_pendingSceneFocus` arms at
   the swap frame; UpdatePendingSceneFocus (OnUpdate) lands selectedIndex=20
   only after the close button's Box.Rect reads real (W≥1, Top≥1) for 2
   consecutive frames. Scene-content @refs (`_refSceneClose` etc. +
   detailScrollBody) are NULLED whenever the `_mountedScene` branch swaps so a
   deleted panel's frozen rect can never strand the ring. A user hover that
   lands on a scene focusable first cancels the pending retarget.
3. **Full-width footprint:** frame = 1700px (1920 canvas, 64px vert padding);
   backdrop slab = 2070px wide, **7° rest** (10° foreshortened the far edge too
   hard to cover the frame's corners at this width). Tween numbers: arrive
   12→7, depart 7→14. Roadmap = lead + 3 fixed-height 2-card grid rows in a
   non-scrolling `.lhd-body` (stagger selector `.lhd-body > *`); Patch Notes =
   `.lhd-pn` row (360px `.lhd-rail` version/TOC rail + `.lhd-scroll` reading
   column). Menu planes keep their 10° dialect — the angle difference is
   Suto-style wayfinding.
4. **Key caps everywhere** (`.lhd-key`, ported from BattleView `.kb-key`):
   Q on close, number caps (Slot1/Slot2 actions) on the footer CTAs, A/D ·
   ENTER · Q hint cluster in the footer, W/S hint in the patch-notes rail.
   Docked Options mirrors with `.opt-key` (W/S · SPACE · Q) + Q cap on close.
5. **Docked Options = two scroll columns** (`.modal-content.docked-cols` +
   `.opt-col`): sections live in RenderFragments shared with the in-game
   single-column modal; all 9 toggle rows render in the LEFT column so the
   panel's W/S cursor walks one coherent column.
6. Title headers are GAP-FLEX word-span rows (`.lhd-title-white` +
   `.lhd-title-gold`) — bare text + nested span drops the separating space in
   s&box.
7. Focusable map (unchanged indices): 20 close · 21 primary footer action ·
   22 secondary (roadmap only). Options scene = no shell focusables (docked
   panel owns keyboard; ring hides).
