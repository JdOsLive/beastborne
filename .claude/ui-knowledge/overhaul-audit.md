# UI Overhaul Audit — summary (Phase 0, code-read pass)

2026-09-25. Six parallel code-read passes against the rubric in `ui-overhaul-brief.md`.
Scores are from reading code only; nothing was run in-editor. **Per-screen evidence
(file:line):** `overhaul-audit-details.md` (grep by screen name, don't read it whole).
Scale 0–3 · **C** cohesion · **M** motion · **I** input parity · **Id** identity.

## Scores

| Family | Screen | C | M | I | Id |
|---|---|---|---|---|---|
| Beasts | Roster grid + FilterBar | 2 | 2 | 2 | 2 |
| | Beast stage / dossier | 3 | 2 | 2 | 3 |
| | Fusion mode + ritual/result | 2 | 3 | 2 | 2 |
| | Journal / sub-view pickers | 2 | 2 | 2 | 2 |
| | MonsterCard (game-wide) | 2 | 1 | 1 | 2 |
| Skills | Skill Tree | 2 | 3 | 2 | 2 |
| Expedition | World Map | 1 | 2 | 1 | 2 |
| | ExpeditionPanel (run host) | **0** | 1 | **0** | 1 |
| Online | Online hub (Tamers) | 2 | 2 | 1 | 1 |
| | Leaderboards | 1 | 2 | 2 | 2 |
| Beastbook | Specimen Hall | 3 | 3 | 2 | 3 |
| Shop | Merchant Stall | 2 | 2 | 2 | 2 |
| Overlays | Quests | 1 | 2 | **0** | 2 |
| | Bag | 2 | 2 | 2 | 2 |
| | Profile | 1 | 1 | **0** | 1 |
| PawPad | Launcher + Dock + swipe | 2 | 3 | 2 | 2 |
| | In-phone apps (Chat/Radio/Effects/Alerts) | 1 | 1 | 1 | 1 |
| HUD | GameHUD shell | 1 | 1 | 2 | 2 |
| System popups | Options | 1 | 1 | 1 | 1 |
| | Guide / Help | 1 | 2 | 2 | 1 |
| | Feedback | 1 | 2 | **0** | 1 |
| | MenuPopup | 2 | 1 | 2 | 2 |
| | ConfirmDialog | 1 | 1 | 2 | **0** |
| Flow popups | TeamPicker | 1 | 2 | 2 | 2 |
| | Contract negotiation | 2 | 2 | 2 | 2 |
| | Expedition result | 2 | 2 | 2 | 1 |
| Meta/reward | Achievements | 1 | 2 | 1 | 1 |
| | Daily | 1 | 2 | 1 | 2 |
| | Gift inbox | 1 | 1 | **0** | 1 |
| | Showcases (Beast + TamerCard) | 1 | 1 | 2 | 1 |
| | Trading | 2 | 2 | **0** | 1 |
| | Tutorial | 1 | 1 | 2 | 1 |
| | Credits | 1 | 1 | 2 | 1 |
| Menu | Main menu | 1 | 3 | 2 | 2 |

**Read:** Beastbook and the Beasts stage are the only screens near the target. Nothing
scores 3 on input anywhere, and **no screen supports a controller**. Expedition, Quests,
Profile, the in-phone apps and most popups sit at 0–1 on input.

---

## Quick-fix list (live bugs — fix before/alongside Phase 1)
Mostly input-routing fixes, safe to do early. *verify* = confirm in-editor first.
1. **Q at a page's top level freezes the page.** GameHUD flips `IsNavigatingTabs` (a mode
   for the retired tab bar) which stops ticking the page; `ExitPanel()` is empty. Hits
   Skill Tree, World Map, Beastbook (W off top row), Shop (Q with nothing selected), Bag/
   Quests/Profile close. (`GameHUD.razor` ~1032-1051, ~689)
2. **Feedback popup is a keyboard trap** — registered as a blocking modal, handles no keys.
3. **Trading has no keyboard support and blocks GameHUD keys** while open → stuck.
4. **Gift inbox isn't registered in `UIModalState`** → on the main menu, Space fires PLAY behind it.
5. **Retired floating chat still opens on T or any Enter** (`ChatPanel.razor` ~590, ungated) *verify*.
6. **Unregistered page modals** (roster release confirms, FilterBar search/dropdowns,
   Beastbook zoom + new-species reveal, leave-expedition confirm) → hotkeys fire underneath;
   FilterBar dropdowns can't be navigated/closed by keyboard.
7. **GameHUD ticks both ExpeditionPanel and WorldMapPanel with no battle guard** → Space
   in battle may drive the unmounted map / open TeamPicker *verify*.
8. **Double-fires on chained opens:** Space on Expedition result's EMBARK AGAIN also toggles
   a beast in TeamPicker; only PhoneLauncher stamps `MarkRouted()` on close.
9. **Shop:** Space buys instantly (no confirm, no deny sound when unaffordable); caps show `<` `>` but keys are Z/X.
10. **Tutorial points at the retired bottom bar**; 6 of 9 spotlight targets match nothing.
11. **Quests rewards are wrong:** mission `GemReward` shows the token icon but grants gems;
    daily/weekly bonus show tokens, grant gems; streak calendar shows gems, hides Ink.
    *Needs a data decision.*
12. **Leaderboard:** big numbers never get K/M formatting; Gold Earned shows ×1000; spinner doesn't spin.
13. **ZONE CLEARED celebration** uses `animation-delay` (cancelled) and fires behind the result popup.
14. **Main menu:** version strings disagree (UPDATE 1.3 / v1.2.1 / v1.2.0); "SCREENSHOT · KEY
    ART HERE" placeholder ships; PLAY has no repeat-press guard; starter A/D order doesn't
    match the on-screen triangle.
15. **Options** "beast level-up popup" toggle actually controls the Tamer level-up alert.
16. **Anti-gacha wording** in the fusion result ("LUCKY / HARSH", "the roll deserves drama", "RISK").
17. Copy debt: gems in Daily/Achievement/Gift/Help; Help mentions dormant Arena and retired
    toasts; 19 "Screenshot pending" placeholders; Daily/Achievement fly-to-HUD effects aim
    at a HUD currency pill that no longer exists.

## Cross-cutting problems (what Phase 1 must solve)
1. **No shared shells.** `BbHeaderWaves` is on only 3 pages; there's no modal shell — a
   "dock-tier popup" look is copy-pasted into 11 scss files with ~6 header styles.
2. **The selection indicator exists 6+ times** (roster, menu, PawPad, MenuPopup, Online,
   Shop, Beastbook…) with stale width math, plus extra per-card hover rings (two cursors
   at once). World Map needs a free-position mode (pins/nodes). Decide ring vs. alternative once.
3. **Input is rebuilt per page:** `UiInput.ConfirmPressed` (E) barely used; hover ≠ focus
   almost everywhere; device flag missing on most pages; **no controller mapping at all**;
   5+ key-cap styles and many hotkeyed controls with no cap; stray Escape binds; large
   mouse-only zones (Online sidebar, Shop qty/sort, Beastbook filters, gene locks, unequip).
4. **GameHUD's input architecture is the root cause of several bugs:** the dead
   `IsNavigatingTabs` mode, ticking unmounted panels, partial double-fire guards, and no
   one-line way to register a page modal. Replace with one input router.
5. **Motion is engine-unsafe where it exists:** 196 `animation-delay` uses (even in the
   roster reference), transforms/glows inside scrolls, per-frame `BuildHash` on Roster /
   Beastbook / GameHUD / menu, World Map runs ~15 infinite loops, three hand-built claim
   celebrations with their own particle keyframes.
6. **Color identity has gaps:** system and meta screens have no accent, so violet (the
   cursor color) becomes their page color; gold overused (Done/SEND/Retry); element
   colors duplicated across 16+ files (Beastbook alone has 4 tables).
7. **Nav leftovers:** Chat/Radio/Effects have two homes (floating widgets + phone apps);
   56px spacer; dead bottom-bar markup; tutorial and HUD-target references to removed UI.
8. **Missing moments:** no level-up / evolution / unlock presentation outside the roster;
   Expedition result has no roll-up; Quests claim has no reward beat.
9. **Dead code everywhere** (≈663 probably-dead CSS classes — `tools/ui_deadcss.py`; plus
   BreedingPanel, CardCollectionPanel, TamerCardComponent, GameHUD `@if (false)` bar,
   ~1,900 lines of unused Help renderers, ExpeditionPanel's dead views/~95% of its CSS,
   World Map dev editor, TeamPicker dev slot editor, `dev_skillspage`, orphaned menu CSS).
   Help's ~1,000 lines of global bare selectors (`h2`, `p`, `li`…) can leak into other panels.
10. **PawPad look:** strongest motion in the game (accent swipe, ring) but the device reads
    as a generic graphite phone with gold trim (real-world material + decorative gold),
    iOS-style pictograms (Beasts and Beastbook share a paw), equal-weight 3×4 grid, plain
    in-phone lists with hard-cut transitions.

## Decisions needed from the user
1. **Quests rewards:** what should the gem rewards become — Tokens, Ink, or removed?
   (Blocks the Quests pilot.) Also: drop the "Coming Soon" Main tab and the dormant Guild tab?
2. **Chat / Radio / Effects:** make the phone the only home and retire the floating widgets (and their R/C hotkeys → phone apps)?
3. **Accent for system & meta screens** (Options, Help, Feedback, Achievements, Gift, Credits…): one shared "system" color, or reuse app colors?
4. **Level-up / evolution / unlock moments:** add a proper presentation (e.g. a shared "moment" overlay), or keep them as Alerts?

## Proposed order
- **Phase 0.5 — quick fixes** 1–9 (input routing; mostly mechanical) + the copy/data items once decisions land.
- **Phase 1 — foundations**, in this order: in-editor engine check (`dev_uilab`) → shapes
  decision session → dead-code purge (shrinks every later step) → input router in GameHUD
  (replaces `IsNavigatingTabs`; ticks only the mounted page; modal-registration helper;
  device flag for mouse/keyboard/controller; key-cap component with device glyphs) →
  selection-indicator decision + component → page shell + modal shell → motion kit
  (keyframe-percentage staggers, one celebration beat) → tokens (element palette, accents)
  → split MonsterRosterPanel.
- **Phase 2 — pilot: Quests** (weakest overlay, user-flagged; after decision 1).
- **Phase 3 — rollout:** Expedition family (World Map, TeamPicker, Contract, Result — full
  redesign) → PawPad + in-phone apps (+ phone visual upgrade) → system popups on the modal
  shell → meta/reward screens → Online/Leaderboards/Shop → Skills → Beastbook → Beasts.
- **Phase 4 — cohesion pass**, **Phase 5 — main menu rebuild** (carry over: ring liquid
  stretch + ink-lean, PLAY detonation, scene-swap machine, mouse/keyboard mode switch,
  adjacency nav, diagonal wipe — extracted as shared pieces first).

## Running log
| Date | Phase | Item | Status | Notes |
|---|---|---|---|---|
| 2026-09-25 | 0 | Code-read audit | done | this file + details |
| 2026-09-25 | 0.5 | M closes phone; R no longer double-fires radio; button-focus guard | pushed, verify in-editor | commits 27dac6d, a2ffeee |
