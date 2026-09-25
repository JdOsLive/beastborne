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

## Decisions (user, 2026-09-25)
1. **Quests rewards → Tokens.** Every gem reward (mission `GemReward`, daily/weekly bonus,
   streak calendar) grants and shows Tokens; the calendar also shows Ink where Ink is granted.
   *Still open:* drop the "Coming Soon" Main tab and the dormant Guild tab?
2. **Chat / Radio / Effects → the phone is the only home.** Retire the floating ChatPanel
   popup, RadioWidget and ActiveEffectsPanel widgets; T / R / C open the phone apps (as N
   already does). The in-phone apps must then reach full parity (keyboard, states, motion).
3. *Still open:* accent for system & meta screens — one shared "system" color, or reuse app colors?
4. **Level-up / evolution / unlock → add a celebration screen.** One shared "moment"
   overlay (Tamer level-up, beast level-up recap, evolution, zone unlock, new species, big
   achievements), built on the motion kit's celebration beat, anti-gacha (celebrate the
   visible result, never a roll). Queue moments so they never stack; Alerts still logs them.

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
| 2026-09-25 | 0.5 | Quick fixes 1–10, 12–16 (input core, Feedback/Trading keyboard, Gift inbox, Shop, Leaderboard, Tutorial, menu, Options label, ZONE CLEARED, fusion wording) | pushed, verify in-editor | 1e62f94 + batch 2 |
| 2026-09-25 | 0.5 | Quests→Tokens; gems→Tokens 1:1 (+save migration v3); regional tokens removed; economy A1–A4 | pushed, verify in-editor | 2c142fc, 8f40af3, 2222d53 |
| 2026-09-25 | 0.5 | Open: #11 remainder (Quests Main/Guild tabs), #17 copy debt (Daily/Achievement fly-to-HUD targets, Help "Screenshot pending"), zone name "Weaverton Approach" vs map label "Serpinglin Approach", Patch Notes screen still v1.2.0 content | todo | — |

