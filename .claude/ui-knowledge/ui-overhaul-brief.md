# UI Overhaul Brief — "One Game" pass (drafted 2026-09-25)

**How to use this:** this is the standing prompt for the whole UI overhaul. Every session
on it starts with: *"Read `.claude/ui-knowledge/ui-overhaul-brief.md`. We're on Phase N
(screen/family X)."* It supersedes `sweep-brief-v2.md` (the July sweep) as the active brief.

---

## The goal

Most screens have been redone at least once, but they were redone at different times, by
different passes and against different mocks, so the game reads as a set of good screens
rather than **one game**. This pass is about the *system*, not new looks:

1. **Cohesion** — every screen is obviously a sibling: same page frame, same cursor, same
   state language, same motion vocabulary, same hotkey caps, same sounds.
2. **Motion** — every screen has entrance choreography, view-swap transitions, reactive
   feedback on every input, and one quiet ambient layer. No dead-static screens.
3. **Input parity** — every screen is fully playable by **mouse, keyboard, AND controller
   (gamepad)**, and switching between them mid-screen feels natural (last input wins, one
   selection indicator, no jumps). The user rates this as critical.
4. **Our own identity** — built from Beastborne's vocabulary, not from generic design-tool
   output. Mocks (Claude Design / DesignSync, ChatGPT, any AI tool) are *input to taste*,
   never the spec. If a screen could belong to any dark-mode app, it isn't done.

**Take our time.** Quality over coverage: one family done right beats six done fast. Never
batch-rewrite screens in one context.

## Fixed points (don't relitigate)
- **Keep `BbHeaderWaves`** (the wave header) as the page-top signature, and extend it to
  every tab page that doesn't have it yet.
- **Beasts are the heroes; UI is the frame.** Anti-gacha: celebrate deterministic, visible
  results; never dramatize an RNG roll.
- **One selection indicator** + app color per page + rationed gold. **The living violet
  ring is NOT a fixed point** (user, 2026-09-25): Phase 1 must prove it works as a
  universal selection indicator — across grids, lists, stage controls, popups, mouse,
  keyboard and controller, and on the new 26.09 engine. If it doesn't hold up everywhere,
  replace it with something that does rather than keeping it for its looks.
- **Engine law:** `CLAUDE.md` quirks table + `laws.md`. Shipped roster code wins over
  docs; when this pass settles something new, update `guiding-star.md` / `laws.md`.
- **Out of scope (user, 2026-09-25):** BattleView / battle HUD stays off-limits for now;
  also dormant features (GuildPanel, ArenaPanel, PvP) and gameplay logic/data.
- **Main menu: REBUILD on the new system** (user decision 2026-09-25) — the current
  ChatGPT-era build gets replaced so menu → game reads as one product. It's the LAST phase,
  after the foundations are proven in-game; don't restyle it piecemeal before then.

## Known unfinished work (user, 2026-09-25)
- **Expedition** (World Map → prep → run → result) is the app that never got a full design
  pass. It needs a real redesign, not a polish pass.
- **Quests** is partly done; the **Quests popup** in particular still needs work.
- **The PawPad itself** (device shell, tiles, dock, icons, in-phone apps, open/close) is in
  scope for a visual upgrade, not just the pages it routes to.

## Code debt & shapes (user, 2026-09-25)
- **Minimize code debt as we go.** Delete dead code before redesigning a screen (use
  `tools/ui_deadcss.py` + the audit's dead-code lists); every rewrite should end smaller than
  it started; shared components replace private copies. Targets are in `GOALS.md`.
- **Skew is under review, not a signature.** The skew angles mostly weren't working. Don't
  add new skew; the Phase 1 shapes decision session decides where (if anywhere) it stays.

## Anti-"AI UI" checklist (reject on sight)
Uniform 1px `rgba(255,255,255,.1)` hairlines on everything · the same 12–16px radius on
every box · soft layered shadows as decoration · purple accents everywhere · decorative
glows/halos · icons in every label · stat-dump cards · symmetric three-card rows ·
generic "dashboard" grids · emoji. Instead: contrast-separated slabs, corner cuts, skew
chips, italic display type, the app color used with intent, varied layout cadence.

---

## Phases (each ends with a user checkpoint)

### Phase 0 — Audit (no code)
Walk every screen family and score it 0–3 on: **cohesion** (frame, header, cursor, tokens),
**motion** (entrance, swap, reactivity, ambient), **input parity** (full keyboard, mouse,
switching, hotkey caps), **identity** (anti-AI checklist). Also list shared-component
gaps and one-off copies. Output: `.claude/ui-knowledge/overhaul-audit.md` (matrix + top
10 cross-cutting problems + proposed phase order). Use `panel-inventory.md` by grep.

Screen families:
1. Tab pages — Beasts (roster/stage/fusion/journal), Skills, Expedition + World Map,
   Online hub, Beastbook, Shop
2. Overlays — Quests, Bag, Profile
3. PawPad — launcher + in-phone apps (Chat, Radio, Effects, Alerts) + System Dock
4. System popups — Options, Guide/Help, Feedback, MenuPopup, ConfirmDialog
5. Flow popups — TeamPicker, Contract negotiation, Expedition result, evolution/fusion
   reveals, level-up/unlock moments
6. Meta/reward — Achievements, Daily, Gift inbox, Showcases, Trading, Tutorial, Credits
7. Main menu + save/starter flow (last — full rebuild)

### Phase 1 — Foundations (shared systems before any screen)
Build or fix the pieces every screen will share, so screens stop re-implementing them:
- **Page frame:** one shared page shell = `BbHeaderWaves` + kicker/title/app color +
  consistent content column + bottom clearance for the phone button.
- **Engine check first:** s&box 26.09.x replaced the layout engine, the renderer, and
  made `<button>` focusable (see `sbox-26-09-changes.md` + CLAUDE.md "Test these
  first"). Verify those in-editor before building foundations on top of them.
- **Selection indicator — decide, then build once:** evaluate the living ring honestly
  (does it read on every surface? cost? jitter? does it work with controller focus?)
  against simpler alternatives (e.g. the `.kb-focused` border-recolor + `kbLand` flash
  already used on stage controls). Show the user both on one screen, pick one, then make
  it a single reusable component. If the ring stays, settle its width math (see
  `laws.md` Unresolved) here.
- **Input model:** one focus-graph pattern per screen (zones → items, WASD moves,
  Space/Enter/E confirms, Q backs out one level, R page power action, Z/X cycle), with a
  **controller mapping for every action** (d-pad/left stick move, A confirm, B back,
  bumpers cycle, a face button for the power action, Start/Select for phone/menu — check
  `ProjectSettings/Input.config` and s&box gamepad glyph support), and on-screen key caps
  that swap to the active device's glyphs. One device flag (last input wins across mouse /
  keyboard / controller; mouse hover == focus; keyboard or pad input hides hover-only
  states). Evaluate the new 26.09 focus APIs (`TabIndex`, `FocusNext()`,
  `ScrollIntoView`) vs our own routing — adopt them where they fit. hotkey caps (`.kb-key`) on every hotkeyed control, and the double-fire guards
  (`PanelHandledBackKey` / `PanelHandledPowerKey` / `MarkRouted`) built into the pattern
  instead of sprinkled. Use the `input` agent; check current s&box focus APIs first.
- **Motion kit:** one place for tokens (fix stale `BbTokens.cs`), plus reusable recipes:
  page entrance (header → content cascade via keyframe-percentage staggers),
  view swap (directional slide keyed by version), selection change, confirm SNAP,
  reward/celebration beat, list-item arrival. Remember: no `animation-delay`.
- **Shapes decision session (first thing in Phase 1):** for card shape, corner radii,
  angles/skew, selection indicator, button shapes and motion timings, show 2–3 options side
  by side (quick HTML mock), the user picks, lock the picks as tokens. Output replaces the
  matching guiding-star sections.
- **Tooling already in place:** `tools/ui_lint.py` (run before committing UI work) and
  `tools/ui_deadcss.py` (dead-CSS finder). Still to build: an in-game `dev_uilab` engine
  test screen (box model, gradients, focus, scroll clipping, blur side by side) to verify
  the 26.09 engine changes once and keep CLAUDE.md honest.
- **State language:** one hover/press/selected/disabled/locked recipe (light → dark hover
  polarity; bring `BbButton`, `FilterBar`, `MonsterCard` in line).
- **Element palette:** consolidate the ~16 per-panel copies onto `BbTokens.Element`.
- **Sound map:** hover, focus move, confirm, back, deny, tab/view swap, reward — same
  cue for the same meaning everywhere.
Deliver a short `components.md` usage sheet for the new shared pieces.

### Phase 2 — Pilot one screen end-to-end
Pick a mid-complexity screen (suggestion: Shop or Quests) and rebuild it on the
foundations with full motion + input parity. The user plays it and approves the *feel*
before anything else is rolled out. Adjust the foundations from what the pilot teaches.

### Phase 3 — Roll out by family
One family per session (or per few sessions), in the order the audit recommends. Beasts
(roster) is the reference, so align it last-but-one rather than rebuilding it first.
For each screen: propose (what changes, why, with a rough sketch) → user OK → implement →
verify in-editor → log.

### Phase 4 — Cohesion pass
Cross-screen: tab↔tab and phone transitions, overlay open/close, sound consistency,
consistent empty/loading/error states, performance check (no per-frame `BuildHash` churn,
no display-flip stalls), a keyboard-only playthrough and a mouse-only playthrough of the
core loop.

### Phase 5 — Main menu rebuild
Rebuild the menu (and save-slot / starter flow) on the shared system so the menu → game
handoff feels like one product. Carry over what works — the living ring's liquid stretch,
the PLAY detonation beat, the scene-swap transitions (`scene-swap-spec.md`) — as
deliberate choices, not inherited code. Propose the new composition (sketch/mock) and get
the user's OK before building.

---

## Definition of done (per screen)
- [ ] Page shell: wave header, kicker/title in the app color, shared spacing.
- [ ] One cursor; every interactive element reachable by keyboard in a sensible order;
      Q always backs out exactly one level; hotkeyed controls wear their key cap.
- [ ] Mouse: hover == focus, no hover-only information, click targets ≥ 44px on main flows.
- [ ] Fully playable on controller; key caps show the active device's glyphs.
- [ ] Switching mouse ↔ keyboard ↔ controller mid-screen never jumps the selection or
      leaves ghost hovers.
- [ ] Entrance choreography, view-swap transition, SNAP feedback on every input,
      one ambient layer (FLOW) — and nothing moving that doesn't belong to a layer.
- [ ] Tokens only (surfaces, app color, element palette, type ladder, motion tokens).
- [ ] Empty, loading, locked and error states designed, not defaulted.
- [ ] Passes the anti-AI checklist and the one-line test ("the player is here to ___").
- [ ] Engine-safe (CLAUDE.md + laws.md); no new double-fire or focus bugs.
- [ ] Verified in the s&box editor (screenshots / `dev_*` freeze commands for animations).
- [ ] Patch-note line added; any new durable lesson added to `laws.md`.

## Working rules
- **One screen/family per session.** Read `CLAUDE.md` (auto-loaded), `laws.md`,
  `guiding-star.md`, then only the files for the screen at hand. Grep, don't bulk-read.
- **Propose before implementing** anything that changes layout or feel; small fixes can
  go straight in.
- **Verification needs the editor.** Cloud sessions can write code but can't compile or
  see it — say what needs checking, and do the risky/visual work in a local session with
  the `sbox` MCP connected.
- Restart the game after big UI changes (hotload leaves dead onclicks / fps craters).
- Use the agents: `sbox-ui` for visual/juice proposals, `input` for focus models,
  `balance` never (not a balance pass).
- Commit per screen with a clear message; keep a short running log at the end of
  `overhaul-audit.md` (phase, screen, status, open questions).
