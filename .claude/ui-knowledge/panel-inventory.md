# Beastborne UI Panel Inventory

Map of every UI panel in the game. Built by the sbox-ui agent on first scan and updated incrementally as panels change. This is the agent's "I know the whole game" reference.

**Last full scan:** 2026-04-09 (46 .razor files mapped)

## Format for each entry

```
### PanelName
- **Files:** Code/UI/Panels/PanelName.razor, .scss
- **Purpose:** One-sentence description
- **Visual style notes:** Dominant colors, layout pattern, signature elements
- **Juice tier:** Current / target (1-4)
- **Known issues:** Quirks, inconsistencies, flat moments
- **Neighbors:** Related panels
```

Entries are skimmed (not deep-read) on first scan — refine each one the first time you touch the panel for real work.

---

## Core panels
_(Primary game screens — main menu, HUD, battle)_

### MainMenu
- **Files:** `Code/UI/MainMenu.razor` (1017), `.scss`
- **Purpose:** Title screen with logo, play/tutorial/roadmap buttons, staged entrance animation.
- **Visual style notes:** Nebula + vignette background layers, staged "entranceStage" reveal (0-5), rotating splash text, menu-btn selection states. Purple accent.
- **Juice tier (current / target):** Tier 2 on entrance (good), Tier 1 on idle. Target: keep.
- **Known issues:** None noted in skim — entrance staging already lives here, could be a template for staggered reveals elsewhere.
- **Neighbors:** GameHUD (post-start), CreditsPanel, HelpPanel, OptionsPanel.

### GameHUD
- **Files:** `Code/UI/GameHUD.razor` (~1330), `.scss` (~2355)
- **Purpose:** Always-on HUD wrapper — top bar (identity, tabs, currency), bottom bar (buttons), panel router.
- **Visual style notes:** Unified top bar pattern, animated WebP tab icons that swap on hover, theme class variants, 3D-battle mode class. Level badge, XP bar, avatar. Currency counters now roll-up (~400ms ease-out lerp) on real-value change, with a gold scale/glow pulse on increase and a subtle red-tint pulse on decrease.
- **Juice tier (current / target):** Tier 1 ambient + Tier 2 on currency change (Balatro-style roll-up + icon pulse). Target: Tier 3 when DailyPanel particle fly-to-HUD lands — the counters are now a valid "target" for particle arrivals.
- **Known issues:** None current. The currency roll-up is restrained on purpose so the upcoming DailyPanel particle moments have headroom.
- **Neighbors:** All overlays dock here; owns the currency display that every reward claim feeds into. `.currency-pill` position in top-right is the target anchor for future fly-to particles.

### BattleView
- **Files:** `Code/UI/Components/BattleView.razor` (2378), `.scss`
- **Purpose:** 2D battle overlay (wild + 3D-mode-aware). Health bars, move picker, turn log, effects.
- **Visual style notes:** Boss-battle variant class, 3D mode class, background style injection. Largest UI file — high surface area.
- **Juice tier (current / target):** Tier 2 on most actions, Tier 3 on boss defeat. Unknown target — needs dedicated pass.
- **Known issues:** Huge file; likely has duplicated move-picker with MonsterRosterPanel per CLAUDE.md quirk note.
- **Neighbors:** MoveSelectionPanel, ActiveEffectsPanel, BattleTransition.

---

## Popups / modals
_(Overlays that dock on top of gameplay — click-outside-to-close, scaled-in modal boxes)_

### DailyPanel
- **Files:** `Code/UI/Panels/DailyPanel.razor` (~600), `.scss` (~1200)
- **Purpose:** Daily login streak (7-day track) + daily/weekly/monthly missions, side-by-side reward and milestone display.
- **Visual style notes:** 1200px wide modal, pop-@popVersion class retriggers modal-scale entrance, escalating day-node heights (90→165px), Day 7 gold tint + rotating legendary silhouette (3s cycle), bg-scroll SVG pattern. Uses the established header with sort-btn pill tabs.
- **Juice tier (current / target):** **Tier 2 solid** on routine claim (Phase 1 landed 2026-04-08): coordinated claim sequence with button overshoot + 12-particle radial burst + gold/ink/token reward flyers toward GameHUD currency pill + deferred manager call timed to flyer landing for cause-effect; staggered content reveal on open (streak → day-nodes cascade → reward row, total ~700ms); mission card state hierarchy (near-complete amber tint + purple glow pulse, completed green glow + pulsing claim button, claimed compressed + dimmed). Target: **Tier 4 setpiece on Day 7** (Phase 2).
- **Known issues:**
  - ~~Claim action has zero visual payoff~~ FIXED (Phase 1).
  - ~~Mission cards visually identical regardless of state~~ FIXED (Phase 1).
  - ~~Reveal on open is a single modal scale~~ FIXED (Phase 1).
  - Day 7 claim still uses the routine Tier 2 claim path — this is intentional until Phase 2 lands the Tier 4 setpiece (milestone flag is already wired via `claimBurstIsMilestone` → purple particle mix, but no fullscreen sequence yet).
  - Streak counter number itself is still static — no roll-up animation when the streak advances. Low priority; the day-track + node stagger carry the entrance.
  - Potential s&box clip risk at `streak-day-big` (42px font / 42 line-height) — still working, untouched by Phase 1.
  - `missions-content` has `padding-bottom: 200px` (line ~671) which is a scroll-height workaround — still load-bearing, do not remove.
  - Reward flyer uses a fixed translate toward top-right (`translate(480px, -420px)`) rather than measuring the GameHUD currency pill. Approximate — will feel off on non-standard aspect ratios but lands "toward the HUD" consistently.
- **Neighbors:** GameHUD (currency target for fly-to animation; its existing pulse-up class fires when the manager call lands), AchievementPanel (sibling reward panel).

### ShopPanel
- **Files:** `Code/UI/Panels/ShopPanel.razor` (386), `.scss`
- **Purpose:** In-game shop modal with active boosts banner, purchase flow.
- **Visual style notes:** Modal pattern with overlay, "🛒" emoji header (should be SVG icon), boost-badge row.
- **Juice tier (current / target):** Tier 1. Target: Tier 2 on purchase (currency deduct + item fly-in).
- **Known issues:** Uses emoji for shop icon (inconsistent with rest-of-project SVG icons).
- **Neighbors:** ShopPanelContent (embedded flow), InventoryPanel.

### ShopPanelContent
- **Files:** `Code/UI/Panels/ShopPanelContent.razor` (1042), `.scss`
- **Purpose:** Embedded shop content — item grid, categories, purchase buttons.
- **Visual style notes:** Large file; likely the shop's meat.
- **Juice tier (current / target):** Unknown — needs pass.
- **Known issues:** Not skimmed in depth.
- **Neighbors:** ShopPanel (host).

### InventoryPanel
- **Files:** `Code/UI/Panels/InventoryPanel.razor` (861), `.scss`
- **Purpose:** Full inventory with sidebar categories + item grid.
- **Visual style notes:** Sidebar + main split layout, count badges, search placeholder, standard modal wrapper.
- **Juice tier (current / target):** Tier 1. Target: Tier 2 on item acquire, Tier 3 on rare item.
- **Known issues:** Search input is placeholder only (line 24 comment) — may be inert.
- **Neighbors:** ShopPanel, MonsterDetailPanel (equip flow).

### AchievementPanel
- **Files:** `Code/UI/Panels/AchievementPanel.razor` (359), `.scss`
- **Purpose:** Achievement list with unlocked counter.
- **Visual style notes:** Modal + overlay, current/total counter in header-right, standard pattern.
- **Juice tier (current / target):** Tier 1. Target: Tier 3 on unlock celebration.
- **Known issues:** Unknown without deeper read. Likely flat-list pattern similar to DailyPanel missions.
- **Neighbors:** DailyPanel, ProfilePanel.

### ProfilePanel
- **Files:** `Code/UI/Panels/ProfilePanel.razor` (669), `.scss`
- **Purpose:** Player profile — hero banner, rank, stats, title. Rank-theme classes.
- **Visual style notes:** Hero banner with gradient avatar frame (rank color), pattern overlay, close button with animated WebP swap.
- **Juice tier (current / target):** Tier 2 on hero. Target: Tier 3 on rank-up.
- **Known issues:** Hero banner is a clear "one hero per screen" win — worth referencing for other panels.
- **Neighbors:** CardCollectionPanel, AchievementPanel.

### CardCollectionPanel
- **Files:** `Code/UI/Panels/CardCollectionPanel.razor` (207), `.scss`
- **Purpose:** Tamer card collection view with "your card" section + favorite monster selector.
- **Visual style notes:** Modal, standard collection header with count.
- **Juice tier (current / target):** Tier 1. Target: Tier 2 on new card.
- **Known issues:** None noted in skim.
- **Neighbors:** TamerCardComponent, TamerCardShowcasePopup.

### HelpPanel
- **Files:** `Code/UI/Panels/HelpPanel.razor` (2085), `.scss`
- **Purpose:** Game guide — sidebar of categories + content panel on right.
- **Visual style notes:** Sidebar + content layout, category icons, huge content file (biggest non-MonsterRoster panel).
- **Juice tier (current / target):** Tier 1. Target: keep quiet — this is utility UI.
- **Known issues:** 2085 lines for content — size is the issue, not feel.
- **Neighbors:** MenuPopup, TutorialPanel.

### CreditsPanel
- **Files:** `Code/UI/Panels/CreditsPanel.razor` (119), `.scss`
- **Purpose:** Rolling credits modal.
- **Juice tier (current / target):** Tier 1 scroll. Target: keep simple.
- **Neighbors:** MainMenu.

### TradingPanel
- **Files:** `Code/UI/Panels/TradingPanel.razor` (744), `.scss`
- **Purpose:** P2P monster trading with request popups.
- **Visual style notes:** Nested trade-request-popup + main trade UI.
- **Juice tier (current / target):** Tier 2 on accept/decline. Target: Tier 3 on trade complete.
- **Neighbors:** OnlineHubPanel, MonsterRosterPanel.

### GuildPanel
- **Files:** `Code/UI/Panels/GuildPanel.razor` (1928), `.scss`
- **Purpose:** Guild landing → list → detail flow (landing hero with animated guild.webp crest, pending invites).
- **Visual style notes:** Landing hero pattern (similar to ProfilePanel), large file with multiple views.
- **Juice tier (current / target):** Tier 2. Target: Tier 3 on join/leave.
- **Neighbors:** OnlineHubPanel, ChatPanel.

### OnlineHubPanel
- **Files:** `Code/UI/Panels/OnlineHubPanel.razor` (1040), `.scss`
- **Purpose:** Online hub wrapper with section pills (players/arena/guild/trading), `bg-{currentSection}` dynamic background.
- **Visual style notes:** Hub-header-row with pills, dynamic section bg via class. Host for sub-panels.
- **Juice tier (current / target):** Tier 1. Target: Tier 2 on section swap.
- **Neighbors:** ArenaPanel, GuildPanel, TradingPanel, LeaderboardPanel.

### ArenaPanel
- **Files:** `Code/UI/Panels/ArenaPanel.razor` (1387), `.scss`
- **Purpose:** PvP arena — mode select → queue → battle, ranked rank display.
- **Visual style notes:** Mode cards (ranked/casual), season badge, rank display with dynamic colors.
- **Juice tier (current / target):** Tier 2. Target: Tier 3 on rank-up, Tier 4 on win streak milestones.
- **Neighbors:** OnlineHubPanel, LeaderboardPanel.

### LeaderboardPanel
- **Files:** `Code/UI/Panels/LeaderboardPanel.razor` (384), `.scss`
- **Purpose:** Live leaderboards with loading spinner + scroll.
- **Visual style notes:** Simple list with loading state.
- **Juice tier (current / target):** Tier 1. Target: keep simple; highlight user's row.
- **Neighbors:** ArenaPanel.

### BreedingPanel
- **Files:** `Code/UI/Panels/BreedingPanel.razor` (615), `.scss`
- **Purpose:** Fusion/breeding UI — 2 parent slots + result preview.
- **Visual style notes:** Parent slots (filled/empty), uses MonsterCard component.
- **Juice tier (current / target):** Tier 2. Target: Tier 4 on fuse setpiece (should be an evolution-grade moment).
- **Known issues:** Almost certainly flat on the actual fuse action — needs review. Candidate for Tier 4 work after DailyPanel.
- **Neighbors:** MonsterRosterPanel (fusion view class on roster).

### SkillTreePanel
- **Files:** `Code/UI/Panels/SkillTreePanel.razor` (741), `.scss`
- **Purpose:** Skill tree with branch tabs (Power/Defense/Utility), node unlock.
- **Visual style notes:** Branch-tab color classes (power-active), SkillNodeComponent for nodes.
- **Juice tier (current / target):** Tier 2. Target: Tier 3 on node unlock — should feel earned.
- **Neighbors:** SkillNodeComponent.

### ExpeditionPanel
- **Files:** `Code/UI/Panels/ExpeditionPanel.razor` (1867), `.scss`
- **Purpose:** Expedition list → team → battle flow. Dynamic bg class, 3D-battle mode class.
- **Visual style notes:** Multi-view (list/team/battle), difficulty dropdown with hard-active state, controls.
- **Juice tier (current / target):** Tier 1 (user flagged as "doesn't feel satisfying to open"). Target: Tier 2 on open, Tier 3 on expedition clear.
- **Known issues:** **User flagged on 2026-04-09 as flat on open.** High-priority for next dedicated pass.
- **Neighbors:** ExpeditionPopup, AutoBattlerPopup, BattleView.

### BeastiaryPanel
- **Files:** `Code/UI/Panels/BeastiaryPanel.razor` (633), `.scss`
- **Purpose:** Beastiary (species catalog) with filter bar + element filters.
- **Visual style notes:** Filter bar component, "seen" content pattern.
- **Juice tier (current / target):** Tier 1. Target: Tier 2 on new species reveal.
- **Neighbors:** FilterBar, MonsterRosterPanel.

### ContractNegotiationPanel
- **Files:** `Code/UI/Panels/ContractNegotiationPanel.razor` (533), `.scss`
- **Purpose:** Radial diamond UI overlay for capturing beasts — 4 approach options positioned around the target beast's 3D screen position.
- **Visual style notes:** Radial positioning, diamond container, result class states, 4 position classes (top/right/bottom/left).
- **Juice tier (current / target):** Tier 2. Target: Tier 3 on successful capture.
- **Known issues:** Unique radial layout — worth preserving, may have specific s&box positioning quirks.
- **Neighbors:** BattleView, MonsterRosterPanel (post-capture).

---

## Detail panels
_(Deep-dive panels for a specific thing)_

### MonsterRosterPanel
- **Files:** `Code/UI/Panels/MonsterRosterPanel.razor` (3361 — largest in project), `.scss`
- **Purpose:** Full monster roster grid with filter bar, sort, rarity filters; fusion sub-view; duplicated move-picker from MonsterDetailPanel.
- **Visual style notes:** Purple theme, concept-D compact cards (recent refactor per git log), helix-particle pattern lives here at scss:1811.
- **Juice tier (current / target):** Tier 1-2. Target: Tier 2 on card hover/select, Tier 3 on evolution.
- **Known issues:**
  - Duplicated move-picker with MonsterDetailPanel (CLAUDE.md quirk).
  - 8 instances of `flex-wrap: wrap` flagged in learnings — need to verify none are broken.
  - Sheer size (3361 lines) makes targeted edits risky.
- **Neighbors:** MonsterCard, MonsterDetailPanel, FilterBar, BreedingPanel.

### MonsterDetailPanel
- **Files:** `Code/UI/Panels/MonsterDetailPanel.razor` (1177), `.scss`
- **Purpose:** Single-monster deep view — portrait, stats, moves, journal, actions.
- **Visual style notes:** Back button with animated WebP swap, pill-btn header actions (show-off, favorite, journal).
- **Juice tier (current / target):** Tier 2. Target: Tier 3 on move-equip / level-up / evolve trigger.
- **Known issues:** Duplicated move-picker (see quirk).
- **Neighbors:** MonsterRosterPanel, MonsterCard.

---

## Components
_(Reusable pieces)_

### MonsterCard
- **Files:** `Code/UI/Components/MonsterCard.razor` (420), `.scss`
- **Purpose:** Reusable monster card — compact and full variants, used in roster/breeding/team.
- **Visual style notes:** Concept-D compact style, sprite animator integration, rarity colors.
- **Juice tier (current / target):** Tier 1 (ambient float?). Target: Balatro-style ambient bob would help.
- **Neighbors:** Used everywhere.

### BattleView
_(see Core panels)_

### MoveSelectionPanel
- **Files:** `Code/UI/Components/MoveSelectionPanel.razor` (137), `.scss`
- **Purpose:** Move picker — shown during battle or monster detail.
- **Juice tier (current / target):** Tier 2. Target: Tier 2 on select.
- **Neighbors:** BattleView, MonsterDetailPanel, MonsterRosterPanel (duplicated).

### FilterBar
- **Files:** `Code/UI/Components/FilterBar.razor` (488), `.scss`
- **Purpose:** Shared element/sort/rarity/name filter UI used in roster + beastiary.
- **Juice tier (current / target):** Tier 1. Target: Tier 1 (utility).
- **Neighbors:** MonsterRosterPanel, BeastiaryPanel.

### ActiveEffectsPanel
- **Files:** `Code/UI/Components/ActiveEffectsPanel.razor` (503), `.scss`
- **Purpose:** In-battle active effect/buff display, expandable.
- **Juice tier (current / target):** Tier 1. Target: Tier 2 on new effect apply.
- **Neighbors:** BattleView.

### BattleTransition
- **Files:** `Code/UI/Components/BattleTransition.razor` (67), `.scss`
- **Purpose:** Between-battle wipe/transition with phase class.
- **Juice tier (current / target):** Tier 2. Target: keep.
- **Neighbors:** BattleView, ExpeditionPanel.

### AutoBattlerPopup
- **Files:** `Code/UI/Components/AutoBattlerPopup.razor` (419), `.scss`
- **Purpose:** Collapsible expedition progress popup — small button expands to detail.
- **Visual style notes:** Expanded/collapsed state classes.
- **Juice tier (current / target):** Tier 1-2. Target: Tier 2 on wave complete.
- **Neighbors:** ExpeditionPanel, ExpeditionPopup.

### ExpeditionPopup
- **Files:** `Code/UI/Components/ExpeditionPopup.razor` (92), `.scss`
- **Purpose:** Small expedition status popup showing current wave + progress.
- **Juice tier (current / target):** Tier 1. Target: keep.
- **Neighbors:** ExpeditionPanel, AutoBattlerPopup.

### MenuPopup
- **Files:** `Code/UI/Components/MenuPopup.razor` (192), `.scss`
- **Purpose:** In-game ESC menu with 2x2 options grid (help/achievements/credits/quit etc).
- **Visual style notes:** 2x2 grid with emoji icons (inconsistent — others are SVG).
- **Known issues:** Emoji icons should migrate to SVG for consistency.
- **Neighbors:** GameHUD, HelpPanel, AchievementPanel.

### EndConfirmPopup
- **Files:** `Code/UI/Components/EndConfirmPopup.razor` (52), `.scss`
- **Purpose:** Confirmation dialog (quit/end run etc).
- **Juice tier (current / target):** Tier 1. Target: keep.
- **Neighbors:** MenuPopup.

### TutorialPanel
- **Files:** `Code/UI/Components/TutorialPanel.razor` (139), `.scss`
- **Purpose:** Tutorial overlay with step-by-step prompts.
- **Juice tier (current / target):** Tier 1. Target: keep quiet.
- **Neighbors:** MainMenu, HelpPanel.

### NotificationPanel
- **Files:** `Code/UI/Components/NotificationPanel.razor` (261), `.scss`
- **Purpose:** Notification history dropdown with unread count badge.
- **Visual style notes:** bg-scroll pattern (same as DailyPanel), expanded state class.
- **Juice tier (current / target):** Tier 1-2. Target: Tier 2 on new incoming notification.
- **Neighbors:** GameHUD.

### ChatPanel
- **Files:** `Code/UI/Components/ChatPanel.razor` (457), `.scss`
- **Purpose:** In-game chat with expand/collapse, unread class.
- **Juice tier (current / target):** Tier 1. Target: keep.
- **Neighbors:** ChatInputBox, VoiceChatPanel.

### ChatInputBox
- **Files:** `Code/UI/Components/ChatInputBox.razor` (62), `.scss`
- **Purpose:** Chat input bar (small component).
- **Neighbors:** ChatPanel.

### VoiceChatPanel
- **Files:** `Code/UI/Components/VoiceChatPanel.razor` (387), `.scss`
- **Purpose:** Voice chat status / mute / speaker indicators.
- **Neighbors:** ChatPanel.

### RadioWidget
- **Files:** `Code/UI/Components/RadioWidget.razor` (352), `.scss`
- **Purpose:** In-game music player widget with toggle.
- **Neighbors:** GameHUD.

### TamerCardComponent
- **Files:** `Code/UI/Components/TamerCardComponent.razor` (247), `.scss`
- **Purpose:** Renders a single tamer card (the player's or others).
- **Neighbors:** CardCollectionPanel, TamerCardShowcasePopup.

### TamerCardShowcasePopup
- **Files:** `Code/UI/Components/TamerCardShowcasePopup.razor` (201), `.scss`
- **Purpose:** Popup when someone shows off their tamer card in chat.
- **Juice tier (current / target):** Tier 2 reveal. Target: keep.
- **Neighbors:** TamerCardComponent, ChatPanel.

### BeastShowcasePopup
- **Files:** `Code/UI/Components/BeastShowcasePopup.razor` (199), `.scss`
- **Purpose:** Popup when someone shows off their beast in chat — portrait + stats.
- **Visual style notes:** Element class on popup, animated sprite.
- **Juice tier (current / target):** Tier 2-3 reveal. Target: keep, possible Tier 3 polish.
- **Neighbors:** ChatPanel, MonsterDetailPanel.

### SkillNodeComponent
- **Files:** `Code/UI/Components/SkillNodeComponent.razor` (78), `.scss`
- **Purpose:** Single skill tree node (unlocked/available/locked states).
- **Neighbors:** SkillTreePanel.

### SwapPanel
- **Files:** `Code/UI/Components/SwapPanel.razor` (174), `.scss`
- **Purpose:** Mid-battle monster swap picker.
- **Neighbors:** BattleView.

### StatBar
- **Files:** `Code/UI/Components/StatBar.razor` (23), `.scss`
- **Purpose:** Tiny reusable stat bar (HP, XP etc). Likely used inside cards.
- **Neighbors:** MonsterCard, BattleView.

---

## Options / settings

### OptionsPanel
- **Files:** `Code/UI/Components/OptionsPanel.razor` (438), `.scss`
- **Purpose:** Game settings — audio, graphics, controls etc.
- **Juice tier (current / target):** Tier 1 (utility). Target: keep calm.
- **Neighbors:** MainMenu, MenuPopup.

---

## High-level takeaways from the scan

1. **Show/Close pattern is consistent** — almost every modal uses `public static bool IsVisible` + `Show()` + `Close()` + `Toggle()`, with `SoundManager.PlayPopup/PlayPopdown`. Good consistency, but only DailyPanel uses `popVersion++` to retrigger open animations — worth promoting this pattern to a shared convention.

2. **Biggest panels:** MonsterRosterPanel (3361), BattleView (2378), HelpPanel (2085), GuildPanel (1928), ExpeditionPanel (1867), ArenaPanel (1387). These are the risk surfaces — any change needs scoping.

3. **Consistent visual grammar:** header with `panel-title-icon` SVG + title, sort-btn pill tabs, close-btn with optional animated WebP swap, bg-scroll SVG pattern for missions/notifications. This vocabulary is solid.

4. **Emoji icon inconsistency:** MenuPopup and ShopPanel header use emoji icons (⚔️, 🛒) where the rest of the project uses SVG. Flag for cleanup.

5. **Currency roll-up is missing project-wide.** GameHUD top bar snaps values instantly. This is the #1 missing ambient juice technique — would benefit DailyPanel, ShopPanel, and every claim in the game.

6. **Ambient idle motion is scarce.** Very few panels have the Balatro breathing. MainMenu's nebula is the closest. Candidates for ambient float: MonsterCard sprites, day-nodes in DailyPanel, hero banners (Profile/Guild).

7. **"Setpiece" moments are all flat.** Evolution, fusion, first-time legendary — these should be Tier 4 but I didn't spot any fullscreen sequence code in the skim. BreedingPanel fuse and DailyPanel Day 7 are the two most deserving candidates.
