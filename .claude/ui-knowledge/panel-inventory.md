# Beastborne UI Panel Inventory

Map of every UI panel in the game. Built by the sbox-ui agent on first scan and updated incrementally as panels change. This is the agent's "I know the whole game" reference.

**Last full scan:** 2026-04-09 (46 .razor files mapped)

**Header chrome canonical (2026-04-25, pass 3):** Top-level panels with tabbed navigation use the OnlineHubPanel "P5 stamp pill" pattern — see `OnlineHubPanel.razor.scss` `.hub-header-row` / `.hub-pills` / `.hub-pill` (lines 56-185). **Full-treatment ported (header bar + tabbed pill cluster):** InventoryPanel (7 tabs), AchievementPanel (10 tabs, per-category accent), DailyPanel (2 tabs), FeedbackPanel (4 tabs, per-category accent). **Chrome-only treatment (header bar + 24px uppercase title + 24px icon, no tabs):** BreedingPanel, CreditsPanel, SkillTreePanel, HelpPanel, QuestPanel (Quest keeps its 11px subtitle stack inside the new chrome), GuildPanel (.guild-header-bar inside browse + create views). **Title-touchup only (already had matching FilterBar wrapper from earlier roster work, just bumped 22→24px title + line-height + 24px icon):** MonsterRosterPanel, BeastiaryPanel. **Skip list with reasons:** WorldMap (intentionally full-bleed, no chrome by design), ArenaPanel (deliberately headerless — Online tab bar handles nav), ExpeditionPanel (`.panel-header` is dead code behind `if (false &&`), TradingPanel (`.trade-header` is a themed trade-window bar, not navigation chrome), ProfilePanel (modal whose hero-banner IS the title zone). **Reminder:** `headerSlideIn` keyframe is panel-scoped — every panel that uses `animation: headerSlideIn ...` must define the keyframe locally (Roster/Quest/Help got it added in pass 3 since they referenced it without defining).

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

### MainMenu _[BIG pass + Beastborne button language 2026-04-17]_
- **Files:** `Code/UI/MainMenu.razor` (~1300), `.scss` (~2500)
- **Purpose:** Title screen. Composition: video/nebula/silhouette bg → logo + splash → four pillar cards (Tame/Fuse/Expedition/Ascend) at corners → button column + rails → news ticker at top → bottom HUD (discord / event / what's-new / version). Save-slot select + starter-select + roadmap are in-place takeovers / modals.
- **Visual style notes:** Three-layer atmospheric wash (vignette + purple content-wash + warm CTA-wash) instead of a single backdrop. Logo entrance uses overshoot cubic-bezier (slam). Hover on menu buttons kerns out letter-spacing (P5 "letters snap apart" trope). Ambient drifting beast silhouettes pulled from `MonsterManager.GetAllSpecies()` on mount. News ticker rotates 6 curated messages every 6.5s (fade-in transition — does NOT rely on `transition-delay` which s&box drops). Slot select is a full-screen takeover, NOT a modal — matches starter-select pattern; Steam avatar appears ONCE in a "Signed in as" header (not per-slot). Roadmap v2 is 3-column (Recently Shipped / In Development / Launch Targets) with launch progress bar and post-launch teaser.
- **Button language (2026-04-17 BIG pass):** Three tiers share one "chunky Beastborne bevel" DNA. **Hero tier (PLAY)** — 440×92, 3px gold border with brighter top highlight, deep warm interior gradient, 34px letter-spaced 10 text, 38px icon, breath-pulse animation. **Secondary tier (TUTORIAL / ROADMAP)** — 440×62, 2px warm bronze/copper border (`rgba(168, 114, 46, 0.55)` sides, `rgba(218, 162, 82, 0.80)` top highlight) over navy-purple vertical gradient body, 20px bold letter-spaced 4 cream text, 28px icons, bronze right-edge accent bar on hover. **Utility tier (LANG / QUIT)** — 40px height, same bronze border DNA at lower alpha + thinner, 13px letter-spaced 2.4 cream text. All three tiers read as the same component family — only size and saturation change. The bronze rim ties back to the logo's warm bevel and the PLAY button's gold, giving the whole menu one voice. QUIT chip overrides the bronze to red on hover.
- **Sizing ratios:** Logo 540→720 (+33%), menu-hero 760→960 wide, PLAY 360×72→440×92 (+22%/+28%), secondary buttons 360×46→440×62 (+22%/+35%), utility chips 30→40 (+33%), news ticker 460×30→560×38 (+22%/+27%), splash text 14→18px (+29%), bottom HUD pills 32→38 height (+19%). Hero moved up from 14% to 8% of viewport; button column moved from 44% to 46% — gives more breathing room between hero and buttons.
- **Juice tier (current / target):** Tier 1 ambient is REAL — drifting silhouettes, pillar cards drifting, nebula pulse, logo float, play-button swirl, ticker rotation. Tier 2 on button hover (kern + accent slide + bronze→gold rim brightening). Target: keep — the menu is a first-five-seconds impression surface, and the three-tier button hierarchy keeps PLAY as the single hero.
- **Known issues:** None current. Avatar-path gender swap on save slots removed as part of 2026-04-17 major pass (Steam avatar now used). `.roadmap-panel` base class no longer has SCSS — only `.roadmap-panel-v2` does, so if legacy code elsewhere constructs a `.roadmap-panel` div it'll render unstyled. Dead `.btn-quit` SCSS block still exists (~820) but no markup uses it; kept as no-op until a cleanup pass.
- **Signature moves worth reusing:** (1) Four-corner pillar cards for "marketing verbs" atmospheric frame. (2) Full-screen takeover pattern for multi-step flows (slot → starter → game-start). (3) Launch progress bar with gradient fill + shimmer for roadmap-style progression displays.
- **Neighbors:** GameHUD (post-start), CreditsPanel, HelpPanel, OptionsPanel.

### GameHUD _[Unified Command Bar 2026-04-23, 4th HUD rework this week]_
- **Files:** `Code/UI/GameHUD.razor` (~1100), `.scss` (~1200)
- **Purpose:** Always-on HUD wrapper — ONE floating bottom bar carries the entire HUD chrome (avatar + currency + 7 destinations + primary actions + utility diamonds + help + version). Replaces the Option B vertical rail + floating cluster + corner diamonds.
- **Why this exists:** The game is grid-heavy (Beasts / Beastbook / Shop / Online grids). Vertical chrome on the left fought every grid layout. A unified bottom bar lets panels be full-bleed horizontal, which is what they want. Four HUD reworks in one week before the user committed to this layout.
- **Visual style notes:**
  - **`.command-bar`** — `position: absolute; bottom: 20px; left: 20px; right: 20px; height: 88px; border-radius: 26px;`. Background `rgba(14, 10, 26, 0.90)` (atmospheric bg peeks through). Border `1.5px solid rgba(139, 92, 246, 0.36)`. Outer drop shadow + subtle purple halo: `box-shadow: 0 16px 40px rgba(0,0,0,0.6), 0 2px 16px rgba(139, 92, 246, 0.12)`. Tier 1 ambient `cbBreath` 5s loop — translateY 0→-1 + scale 1→1.002, barely perceptible.
  - **Three zones** separated by 1px × 56px purple rules (`rgba(139, 92, 246, 0.22)`).
  - **LEFT ZONE (~300px)** — Steam avatar 48px circle + tamer name (13px/800) + LV chip (11px / gradient purple). Avatar wrap is `onclick → ProfilePanel`, hover tints the wrap bg and brightens the avatar border with a purple halo. Breath 3.2s on the avatar. Then a currency trio: Gold / Ink / Tokens as 40px-tall pill cells (10px radius) with 20px icons + 14px values. Currency pulse on value-change (up/down), roll-up preserved.
  - **CENTER ZONE (7 tabs, flex: 1)** — each tab 96px wide, 88px tall, icon+label stacked. Icon-wrap 36×36 holds static SVG (34px) + animated WebP (36px) that cross-fade on hover. Label 12px/800/uppercase/ls 1.4. Active state fills `rgba(124, 58, 237, 0.16)`, brightens icon, and draws a 52px purple underline at the bottom with a slow `cbActiveGlow` 2.5s pulse. Hover underline is 36px at 0.4 alpha. `keyboard-selected` gets brighter purple underline. The underline uses CSS transitions (width + margin-left with a cubic spring) so the new-active tab's bar "slides in" when you switch tabs — a subtle but satisfying state-change animation. Expedition tab still shows the green running-dot top-right of its icon when a run is active.
  - **RIGHT ZONE (~520px)** — action group (Quests / Menu / Bag labeled pills, 56px tall, 14px radius) + thin divider + utility group (Chat / Radio / Effects / Notifs / Help as 48×56 hit areas with 34×34 rotated-45° diamond shells containing counter-rotated iconify glyphs — signature Beastborne diamond pattern lifted from roster's `.action-v2` / Beastbook's `.bb-detail-diamond`). Then a tiny version chip (`α 0.8`).
  - **Labels bigger than Option C/B:** 13px on actions (up from 15/11), 12px on tabs (up from 11), 14px on currency values (up from 11). Avatar meta has tamer name visible inline (previously only in tooltip).
  - **Tooltips** — single shared `.cb-tooltip` class that positions above whatever is hovered (`cb-tooltip-above`). Some have `cb-tooltip-wide` (200px) for radio song titles.
  - **Theme variants (fire/water/nature/genesis)** tint the bar border, active tab underline, active tab bg fill, LV chip gradient, utility shell border + hover halo, action hover bg, and zone dividers. Every theme swaps uniformly across the bar for cohesion.
  - **3D battle mode** — `.command-bar` is omitted from the DOM via Razor `@if (!Is3DBattleActive())`. Conditional render, NOT opacity toggle (per the learned quirk that opacity-based rail-hide got stuck invisible).
- **Sizing rationale:** Everything bumped BIGGER than previous reworks. User has complained ×3 that things felt tiny. Bar is 88px tall (not 64/72). Icons 32–36px. Labels 12–13px. Currency 14px bold. Utility diamonds 34×34 shells inside 48×56 hit areas. Padding generous (22px zone padding on both outer edges).
- **Juice tier (current / target):** Tier 1 ambient (bar breath 5s, avatar breath 3.2s, active tab underline glow 2.5s, expedition running-dot pulse 1.8s, `new-badge-pulse` on quest/bag badges 2s). Tier 2 on hover (tab lift + icon swap + underline extend, action bg purple + lift, utility scale 1.08 + colored halo, currency lift). Tier 2 on currency change (Balatro roll-up preserved). Target: keep — no Tier 3+ here; this is HUD chrome, not a reward surface.
- **Signature moves used:** (1) Purple lift on hover (translateY(-1) to -2). (2) Counter-rotated diamond pattern for utility buttons. (3) Active tab underline slide (width transitions from 0 → 36 → 52 via cubic spring). (4) Currency pulse + roll-up Balatro-style.
- **Known issues / notes:**
  - Currency tooltips in the bar show full number + label ("1,234,567 GOLD"). Positioned above the bar.
  - Currencies use `FormatNumber` for the displayed value (abbreviated K/M/B at ≥100K) — tooltip carries the full precise number.
  - Keyboard shortcuts: number keys 1–6 map to tabs 1–6 (Home = index 0, H-key hotkey NOT wired yet — s&box's engine "use" binding uses H). Tab-level nav uses Left/Right primarily now that the bar is horizontal (Up/Down still aliased for muscle-memory).
  - Anchored widgets (Chat overlay / Notification history / ActiveEffects popup / RadioWidget panel / AutoBattlerPopup / ExpeditionPopup / MenuPopup) all updated to `bottom: 124px` (20 bar-inset + 88 bar-height + 16 breathing) so they sit above the bar cleanly.
  - ChatPanel anchors `left: 20px`, RadioWidget `left: 20px` (aligned with bar's left inset instead of the old 80px rail-clear offset).
  - MenuPopup repositioned to `bottom: 122px; right: 350px` so it anchors above the MENU action's position in the right zone.
  - BattleView's `battle3d-action-bar` keeps `bottom: 84px` — the bar is absent in 3D battle, so the old clearance still reads right visually.
- **Load-bearing preserved behavior:**
  - 7 destinations (home + 6 originals), keyboard 1–6 shortcuts, Left/Right/Up/Down tab nav (`IsNavigatingTabs`).
  - All click handlers: OnPlayerInfoClicked, OnHelpClicked, OnChatClicked, OnRadioClicked, OnQuestsClicked, OnMenuClicked, OnInventoryClicked, OnEffectsClicked, OnNotificationsClicked, HandleHomeHubNavigate.
  - All badges: chat unread, notification count, effects count, quest unclaimed `!`, inventory `NEW`.
  - Animated WebP hover swap on all 6 original tabs preserved (core signature). Home uses iconify `lucide:home`.
  - BuildHash updated — single `hoveredBar` field replaces the old three (`hoveredRail` + `hoveredCorner` + `hoveredCluster`).
  - Theme variants (fire/water/nature/genesis) still apply.
  - Currency roll-up + pulse behavior unchanged.
  - Steam avatar fallback preserved.
- **Superseded layouts (same week):** Option A horizontal top + bottom bars (rejected as "chrome-heavy") → Option C compact top + corner rails (rejected as "too tiny") → Option B vertical rail + Home hub (rejected — fought grid layouts across every tab panel) → **Unified Command Bar (this)**.
- **Neighbors:** HomeHubPanel (default landing, left-padding reclaimed to 48px now that rail is gone), all overlays dock here; currency anchor for fly-to particles moved from top-right / rail-bottom-left → bar's left zone (future pass may re-aim flyer translate offsets).

### HomeHubPanel _[new 2026-04-23]_
- **Files:** `Code/UI/Panels/HomeHubPanel.razor` (~280), `.scss` (~420)
- **Purpose:** Default landing destination introduced in the Option B rail rebrand. Mounts inside `GameHUD.content-area` when `currentTab == "home"`. Calm, glanceable "arriving at your tamer's study" surface — state-aware, anti-gacha. Not a utility surface, not a hype screen.
- **Visual style notes:**
  - **Hero welcome strip** — 200px hero card, breathing purple radial glow behind, time-of-day greeting ("WELCOME BACK," / "GOOD MORNING," / etc.), 54px tamer name all-caps, meta line with title + LV + beast count.
  - **Active expedition ribbon (conditional)** — only renders when `ExpeditionManager.CurrentExpedition != null`. Purple-bordered card with compass icon, "EXPEDITION IN PROGRESS" label, zone + wave status line, RESUME CTA pill. Ambient horizontal pulse gradient (2.6s) + hover lift.
  - **Main row (side-by-side)** — 50/50 split. Daily card (clickable, opens QuestPanel): flame icon + 46px streak counter + mission pill ("READY TO CLAIM X" green / "ALL CAUGHT UP" gray). Favorites showcase: up to 3 favorited monster sprites (fallback to top-level owned if none favorited) with 124px idle-animated sprites, drifting vertically on staggered 3.4s delays, nameplate with name + LV chip.
  - **Quick-jump shortcuts** — 3–4 context-aware cards (Beasts / Expeditions / Fuse Beasts [if ≥2 beasts owned] / Shop). Each is a 40px iconify icon + title + sub, chevron slides right on hover. Clicking a shortcut calls `OnNavigateTab(string)` which GameHUD routes to `SwitchTab`.
  - **Layout style:** Scroll container with 32px top / 48px side / 120px bottom padding (clears floating cluster + corner rails). Cards use roster's canonical treatment (rgba(20, 20, 35, 0.95) bg, 14–18px radius, section-title 3px purple bar + uppercase label, purple lift on hover).
- **Juice tier (current / target):** Tier 1 ambient — hero glow breath, sprite idles, favorites drift, expedition ribbon pulse. Tier 2 on hover (card lifts, chevron slides, CTA brightens). Target: keep calm. A future pass could add a Tier 2 "daily moment" — e.g. when a fresh day rolls over, brief sparkle on the daily card — but avoid RNG-coded reveals.
- **Content decisions made in first pass:**
  - Greeting is time-of-day based (UTC-local, branches at 5/12/17/21). No random "tips" or "news" rotator on the hub itself — the news ticker lives on MainMenu. Kept the hub one-screen to avoid scroll-discovery.
  - Favorite monster showcase uses `Monster.IsFavorite == true` sorted by Level desc, capped at 3. Fallback to top-3-by-level when no favorites. Empty state ("No beasts bound yet") for brand new players.
  - Fuse shortcut gated at `MonsterCount >= 2` (below that, fusion is unusable — hiding it keeps the grid actionable).
  - Quick-jump Resume Expedition is subsumed into the expedition ribbon (avoids duplicate CTAs). The row shows Beasts / Expeditions / Fuse / Shop always.
- **Known issues / follow-ups:**
  - Hub doesn't yet have its own sound cue on open/focus. Could add a soft "home ambient" loop via `SoundManager` — deferred.
  - Favorite showcase click currently falls through to the outer card hover state with no dedicated click handler. Candidate for "click a favorite → jump to that monster in roster" deeplink. Requires a MonsterRosterPanel public `SelectMonsterById(Guid)` method (out of UI agent scope to add; flag for product if we want the deeplink).
  - Fuse shortcut routes to the monsters tab but doesn't automatically toggle `IsFusionMode = true` on MonsterRosterPanel. Could add a param to the tab-switch call to light up fusion mode on entry — small follow-up.
  - Hero glow uses `radial-gradient(...)` without shape keyword (per the s&box quirk). Verified it parses, but if it ever logs "Unknown Image Type" on a playtest, strip to a flat linear-gradient fallback.
- **Load-bearing behavior:**
  - `OnNavigateTab` is an `Action<string>` set by the parent GameHUD markup. All navigation flows through it so GameHUD keeps sole control of tab routing + live-expedition guard.
  - BuildHash includes tamer identity, monster count, expedition state, daily streak, unclaimed count, hovered shortcut, sprite frame, and favorite membership — panel re-renders on any of these.
- **Neighbors:** GameHUD (parent / tab router), QuestPanel (opened by daily card), ExpeditionManager (running-state source), MonsterManager (favorites + sprite frames), DailyRewardManager (streak source).

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

### FeedbackPanel _[charter rebuild 2026-04-19]_
- **Files:** `Code/UI/Panels/FeedbackPanel.razor` (~830), `.scss` (~1700)
- **Purpose:** In-game community feedback. Players post bugs/ideas, upvote others' posts, see resolved items, manage their own posts. Wired to `FeedbackApiClient` (DigitalOcean droplet). Dev-only admin actions (resolve/delete/reopen) inside expanded entries when `Connection.Local.SteamId == 76561198088759073`.
- **Visual style notes:** Center modal, 720×84% (max 880px), charter-aligned. Top accent line only (no bottom — symmetric edges read as neon). Header with diamond title icon + "DEV REPLY" gold chip when there are unread resolves on other tabs + hover-spin close. Tab strip is segmented chip group with per-tab category color (red bugs / sky ideas / green resolved / purple my-posts), each tab carries its own border-bottom rail in the category color when active. Toolbar: sort-toggle (flex-thumb, NOT absolute) + tag filter chips (each chip carries a colored dot + colored bg/border when active via inline `Hex2Rgba` helper) + new-post primary button. Entry list: vote column → 3px tag-color rail → body → chevron. Per-tag color extends from rail to tag-pill. Composer is a SIBLING modal (not nested in scroll), slides in from below with overshoot easing. Submit button has the purple halo + iconify spring on hover (matches Options DONE).
- **Juice tier (current / target):** Tier 2 — vote button springs the chevron up on hover, voted state pops the count number (1.08 scale + color flip), tag chip hover glow, new-post button icon spins 90° on hover, footer refresh icon does a 180° spin, empty-state diamond breathes (3.2s loop), unread tab dot pulses (1.6s loop). Composer slide-in is Tier 3 movement. Target: keep — feedback is a utility surface, doesn't need Tier 4 fireworks.
- **Known issues:** Vote count number doesn't have a dedicated "increment pop" (we'd need a render-key to retrigger; current voted-state scale is the consolation). The empty-frame's planned soft-glow layer is a no-op placeholder (`.empty-frame-bg` is zero-size) — could add a real glow if we ever do a one-off radial gradient outside-scroll. Composer textarea heights are hardcoded (96 / 140) — fine for now, no auto-grow API in s&box TextEntry.
- **Critical preserved API:** `FeedbackApiClient.{ListAsync, ToggleVoteAsync, CreateAsync, PatchAsync, DeleteAsync}`, seen-resolved-IDs persistence to `FileSystem.Data/feedback-seen-ids.json`, `myResolvedUnreadCount` badge logic, all TextEntry `@ref` + `Tick()` poll pattern (titleInput, whatInput, stepsInput, expectedInput, actualInput, resolveNoteInput, versionInput).
- **Neighbors:** MenuPopup (launcher → `FeedbackPanel.Show()`), OptionsPanel (sibling center modal — same envelope/edge/header pattern).

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

### ShopPanel _[DELETED 2026-04-17]_
- Legacy standalone modal version; verified orphaned via grep (zero call sites) and deleted during the ShopPanelContent rework. Both `Code/UI/Panels/ShopPanel.razor` and its `.scss` are gone. The live shop is `ShopPanelContent` mounted inside `GameHUD.razor:169`.

### ShopPanelContent _[rebuilt 2026-04-17, polish passes 2026-04-17 x4 + transitions pass 2026-04-17]_
- **Files:** `Code/UI/Panels/ShopPanelContent.razor` (~2050), `.scss` (~2400)
- **Purpose:** Embedded two-pane shop mounted in GameHUD — grid left, detail right, store-toggle pills in the header. Gold store (contract ink, boosts, storage, revive) and Boss store (HUD themes, titles, consumables + 16 nature runes) reachable via header pills.
- **Visual style notes:** Two-row header (title/store-toggle/currency on row 1, active-boost rail + category dropdown + sort button on row 2). Body is a scroll container with `flex-wrap: wrap` (roster pattern) where `.shop-section-title` children force line-breaks via `flex-basis: 100%` so items wrap into grouped sections. `items-grid` wrapper divs were removed to avoid the nested-flex-inside-scroll collapse. Featured card at top of gold-all view: gold ribbon + sheen + larger icon + divider + `.featured-top-row` (name-stack on the left, right-aligned `.featured-value-callout` pill showing SAVE X% / BEST VALUE on real bundle math — OR the red `.sale` variant showing "15% OFF TODAY" when the Daily Spotlight sale is active). Ambient sparkles (3 staggered dots, 3.6s loop) + warm-gold radial breathing glow (spotlightBreathe 4.5s) live over the warm gold area. **Pass 4 removed the rarity-accent left strip** — cards read with just their category badges now; cleaner grid. Boss-store left panel gets a subtle purple border tint to differentiate from the gold store. Boost icons migrated from emoji → iconify (lucide:zap, lucide:coins, lucide:radar, lucide:clover). Active boosts on grid cards get a triple-signal treatment: warm-tint card bg, size-30 lucide:check overlay on the icon container (+30% warm tint), and a live `.item-time-remaining` chip that replaces the price line — plus a Tier 1 ambient 3.2s border-breath that pauses on hover. Scroll-fade overlay at the bottom of both the gold + boss item areas hints "more below" without affecting scroll interactivity.
- **Juice tier (current / target):** Tier 2 routine claim (achieved 2026-04-17, polished 2026-04-17). Purchase path: click buy-button → button overshoots (0.12s spring up to 1.05 scale) → 10-particle radial burst + gold/token reward flyer animates toward top-right viewport (deferred ShopManager call by 0.35s so the GameHUD currency roll-up fires as the flyer "lands") → purchase flash on the button (0.8s green). Hover path: buy-btn lifts 1px + iconify glyph nudges right 3px via cubic-bezier spring for tactile "I can press this." Tier 1 ambient: featured card sheen hover sweep, featured-sparkles (3 dots, staggered), boost-active border breath (3.2s loop), empty-state nudge arrow bounce (1.8s loop). Can't-afford cards show inline "need +X" deficit pill in amber-red. Target: Tier 3 on first-time legendary/theme purchase (not yet implemented — candidate for a cosmetic-theme reveal setpiece).
- **Known issues / notes:**
  - Reward flyer uses a fixed translate `translate(200px, -380px)` aimed at the top-right viewport corner. Not pixel-perfect across aspect ratios but the GameHUD's own `.currency-pill .pulse-up` class fires on real currency change, which the brain stitches into cause-effect.
  - The purchase burst layer is a sibling of `.shop-layout` inside `<root>` so it isn't clipped by pane `overflow: hidden`.
  - Active-boost rail is always visible in the subheader; expired boosts disappear naturally (driven by `ShopManager.GetAllActiveBoosts()`). Expiring-soon (<60s) pills get an amber border pulse (Tier 1 ambient). Empty state gets a purple-tinted "Grab one below to stack XP & gold" nudge with a bouncing down-arrow (gold side) / up-left arrow (boss side, pointing back to gold tab).
  - Nature runes (16 items) are sub-grouped into Offense / Defense / Speed / Hybrid sections by description keywords — scannable without sorting.
  - Theme preview card/large show a miniature mock HUD (bar + dot + pill + lines) painted with the theme's `PrimaryColor` / `SecondaryColor` / `AccentColor` — not just 3 color stripes. The theme detail pane also shows a 3-swatch palette row with hex codes.
  - Consumable cards show an inline "owned X" counter chip in the top-right; detail pane shows "OWNED X / Y" as a pill.
  - Sort button cycles default → price ↑ → price ↓ → level-req → default. When a sort is applied, items flatten into a single "Results" section instead of category-split groups.
  - Level-lock pill only paints when actionable: always for locked items, or for met items if the player is within 5 levels of the gate. Veterans (lv 158 browsing req-10 items) see clean cards; new players still see "I just unlocked this" recall chips. `ShouldShowLevelChip()` helper centralises the rule.
  - Featured value-callout is gated by `if (savePct < 5) return null;` in `GetFeaturedSavings()` so trivial savings don't surface. Tiered copy: `SAVE X%` for 5-29%, `BEST VALUE · X% OFF` for 30%+. Anti-gacha safe (math is transparent, not RNG).
  - Featured-item selection uses a lifecycle-aware ladder: storage expansion for players who can still expand, else highest-priced personal boost, else highest-priced non-server item. Never server-wide as hero (communal, not personal-progression). `GetFeaturedItem()` enforces the ladder + syncs the pick back to `ShopManager.SetSpotlight(id)` so the manager applies the sale discount to whatever the UI actually shows.
  - **Daily Spotlight is a real sale (pass 4).** `ShopManager.SpotlightItemId` + `SpotlightDiscountPercent` (default 15%) track the spotlight on the manager. `ShopManager.GetDiscountedPrice()` additively stacks the spotlight discount with existing skill discounts (Bargain Hunter, Savvy Shopper) up to a 70% cap. `PurchaseItem()` already routed through `GetDiscountedPrice` → `finalPrice` is charged and mission-tracked, so the sale flows through every hook for free. UI shows struck-through original price + arrow + discounted price, plus a "-15% TODAY" pill in the ribbon and detail pane. Fake strike-through via absolutely-positioned 2px line inside `.price-struck-wrap` (s&box doesn't reliably support `text-decoration: line-through`). Real sale suppresses the bundle-math chip so the two value signals never stack on one card.
  - `CanAfford` was refactored to use `GetEffectivePrice(item)` which routes through `ShopManager.GetDiscountedPrice` — so the Purchase button + cant-afford state + deficit math all reflect the real price the player will be charged (including spotlight + skill discounts).
  - Tier 1 ambient glow on the spotlight card: `.featured-ambient-glow` is an absolutely-positioned 240×132 `radial-gradient(circle, gold...)` breathing 0.5 → 1.0 alpha over 4.5s. Single layer only — the sparkles + hover sheen + icon lift already carry the card's motion budget. `@keyframes` works here because the card is remounted via `.items-scroll.swap-@swapVersion` on category/sort change; `radial-gradient(circle, ...)` works in s&box, `radial-gradient(ellipse, ...)` does NOT (per DailyPanel precedent).
  - EQUIPPED theme/title is visually distinct from OWNED via (a) gold border + warm card bg, (b) gold corner chip (not green), (c) 2px gold `.equipped-chrome` hairline at the card top. OWNED cards are slate/neutral so the 7 owned + 1 equipped grid scans as "one of these stands out."
  - Theme and title cards carry a very faint `background-color` tint of their signature color (accent for themes, title color for titles, alpha 0.06) so the grid reads as a scannable palette. Tint is skipped for equipped/selected cards so SCSS chrome wins — inline styles beat stylesheet rules in s&box's cascade.
  - Section spacing uses 16px (style-guide `lg`) between sections with `:first-child { margin-top: 0 }` so the first section rides tight under the featured card.
  - Trailing "More coming" placeholder card (`.shop-item-coming-soon`) sits at the end of each non-empty grid to tease future content and absorb the final-row whitespace. Low-contrast purple hint, pointer-events:none, no animation — reads as structure not interaction.
  - Boss-store empty-state (no item selected) shows a small stock-rundown summary under the hint: "8 Themes | 7 Titles | 19 Items" pulled from the live catalogue. Gives the empty sidebar ambient weight without competing with the hero line.
  - **Transitions pass 2026-04-17.** (1) Panel entrance: `.shop-panel-content` fades in with subtle translateY on mount (fires every tab-open since ShopPanelContent remounts); header slides down (shopHeaderEntrance), layout fades+lifts with 80ms delay (shopLayoutEntrance). (2) Store switch (gold↔boss): tracks direction via `viewSwitchDir`, applies `.dir-forward` (slides in from right) or `.dir-back` (from left) via @keyframes on the swapVersion-keyed `.shop-layout`. (3) Staggered section reveal: `.shop-section-title.stagger-0/1/2/3` with 60ms animation-delay each — sections slide in from the left in sequence as the grid opens. (4) One-shot card-select pulse: clicking a card mounts `<div class="select-pulse pulse-@selectPulseVersion" />` as an overlay on the clicked card, keyframe scales from 0.94→1.08 + opacity pulses — fresh remount per click via the version key. Featured card gets gold variant. (5) Post-purchase green-tick overlay: `<div class="purchase-confirm" />` mounts on the purchased card for 900ms, scales in with spring + holds + fades. (6) Dropdown menu: replaced plain fadeIn with scaleY(0.85)→1 unfold + 80ms-delayed item fade. Items also get `translateX(2px)` on hover and `:active` compression. (7) Press feedback across `.shop-item`, `.hub-pill`, `.dropdown-btn`, `.sort-btn` (scale 0.95-0.98), sort-btn icon rotates -15° on hover to tease cycling. (8) Card hover enrichment: icon scale 1.06 on hover (child scale — safe, not the whole card). (9) Featured card: icon-bg border breathes 4.2s, on-hover pauses breath and holds bright; divider brightens; icon scales. Spotlight slide widened to 45% + added held dark pause at end of cycle. New right-half gold dust (3 motes, 5.4s staggered). (10) Boost pill entrance: `boostPillEnter` spring on mount so newly-activated boosts arrive with a scale-up, not a pop.
- **Architecture invariants kept intact:**
  - Single embedded panel inside GameHUD — no sub-file split.
  - BuildHash includes every state field that affects rendering (shopView / category / sort / selectedId / currencies / active-theme+title / dropdown open state / swap-version / detail-version / buy-overshoot / burst-active / per-boost type + time-remaining seconds).
  - Hover-sound guards unchanged (per-family `hoveredX` string guard).
  - Currency flash detection via `CheckCurrencyFlash()` called from `BuildHash`.
- **Pending product calls (out of scope for UI agent):**
  - Description strings like "A massive stockpile of contract ink" are flavor-only. A first-time player doesn't know what contract ink DOES. Proposed standard: mechanical line first (`"Used to capture wild beasts."`), flavor on a separate line. Would require ShopManager + localization changes. **Flag for product review.**
  - Keyboard shortcuts (1/2 to swap stores, Tab to cycle filter, Enter to buy). Low priority; s&box input quirks make this non-trivial. **Skipped.**
  - Ambient shop "treasure feel" (gold particles drifting in bg, currency-icon sheen sweeps). User flagged optional — skipped for now; animated-bg.webp is visually busy enough already.
- **Neighbors:** GameHUD (currency-pill roll-up target), InventoryPanel (purchased consumables land here).

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

### GuildPanel _[plaza redesigned 2026-04-24, P5 stamp pass]_
- **Files:** `Code/UI/Panels/GuildPanel.razor` (~2010), `.scss` (~5350)
- **Purpose:** Guild landing → list → detail flow. In-guild "Public Square" plaza is the main scene (banner / monuments / members / perks / danger).
- **Visual style notes:**
  - Plaza root has a P5 stage wash (purple radial + red diagonal slash + cyan kicker + dark floor) plus a subtle plaza-wide halftone overlay (low-alpha radial dot lattice tiled at 18px) so dark areas read as paper, not flat color.
  - **Banner** is now a single P5 headline slab: angular 96×96 emblem SLAB on the LEFT (4px black border, 5×5 offset shadow, gold corner-cut accent matching `.tile-corner-cut`), LEFT-aligned guild name (30px italic, 2.5px letterspace, hard text-shadow), three rotated stamp pills under it ([TAG] purple, LV gold, RANK in tier color — all decorative pointer-events: none so rotation is safe). Chunky 18px XP slab below with hard border + offset shadow + overlaid % + N/M XP text inside the bar.
  - **Monuments** (MOTD + Raid) get index stamps at top-LEFT ("01" gold, "BOSS" red, rotated -4°), corner-cut accents at top-RIGHT (gold/red), and a halftone hatch in the bottom-right. Headers padded so they clear the index stamp. Raid hover bumps the offset shadow to 6×6.
  - **Members box** has a `ROSTER` yellow stamp (-3° rotation) pinned top-left + dark count chip top-right + halftone bg lattice that fills sparse rosters with texture instead of dead space. Member cards are now P5 mini-stamps (2px black border, 2px offset shadow, 13px italic name, role-color underline stripe, 9px italic role label).
  - **Perks** has a matching `PERKS` yellow stamp + halftone bg. Each perk row is a hard-bordered slab with offset shadow. Unlocked rows have a 4px yellow LEFT-edge accent strip (absolutely-positioned child div — per-side border with style word is rejected) and a yellow icon disc + yellow LV stamp pill (rotated -2°, decorative). Locked rows are dimmed and carry a -8° "LOCKED" red corner stamp.
  - **Danger zone** (bottom) — small rotated "DANGER ZONE" label above two slab buttons (LEAVE in vermillion `#dc2626`, DISBAND in deeper crimson `#7f1d1d`), each a P5 stamp with 2px black border + 3px offset shadow + uppercase italic letterspaced label + icon. Claim/transfer variants in gold/dark.
- **Constraints honored:** No `transform: rotate` on any clickable element (only on decorative `pointer-events: none` overlays — index stamps, corner stamps, ROSTER/PERKS labels, LV/rank stamps). Per-side borders with style word avoided in favor of absolute child divs (perk-row-edge). All `background-color: rgba(...)` explicit (never `background:` shorthand). All radial-gradients use bare-stops form. Halftone effects achieved via `background-size` tiling on a single bare-stop radial.
- **Juice tier (current / target):** Tier 2. Plaza entrance has stagger keyframes (banner emblem pop, monument slide-in, portrait drop). Target: Tier 3 on raid complete + perk unlock celebration.
- **Neighbors:** OnlineHubPanel, ChatPanel.

### OnlineHubPanel
- **Files:** `Code/UI/Panels/OnlineHubPanel.razor` (1040), `.scss`
- **Purpose:** Online hub wrapper with section pills (players/leaderboards/guild), `bg-{currentSection}` dynamic background. Tamers tab hosts split layout (tile grid + detail sidebar).
- **Visual style notes:** Hub-header-row with pills, dynamic section bg via class. Host for sub-panels. **Tamer tile (2026-04-25):** Persona 5 "stamped tag" treatment — purple bg-block gradient on left, gold rail bar across bottom, angled gold corner accent in top-right, twin-slash purple flourish in bottom-right corner. LV chip is flat black-on-gold stamp. DEV badge is solid gold flag. Italic uppercase typography with 1px hard-offset shadows. YOU tile shows "01" italic gold index stamp at corner; selected state replaces it with gold "YOU"/"SEL" rectangle stamp. ALL layers respect scroll-grid clip rule (no transform/no halo on hover/selected, only border + bg-color shifts). Stamp layers use `background-image: linear-gradient(...)` rather than `<img>` overlays so they stack above parent bg-color correctly.
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

### SkillTreePanel _[batch polish 2026-04-17]_
- **Files:** `Code/UI/Panels/SkillTreePanel.razor` (~825), `.razor.scss` (~915)
- **Purpose:** Hex-cluster flower skill tree — 5 branches side-by-side (Power / Fortune / Fusion / Expedition / Mastery), each a vertical column. Launch tree = 15 talents total (5 × keystone + 2 petals). Click-to-invest single-rank-at-a-time with SP currency. No branch-point tier gates at launch — gating is per-node prereq (petals require keystone rank ≥ 1).
- **Visual style notes:** 5 `.branch-cluster` columns (260px wide) inside `.clusters-row`. Each cluster = `.branch-header` (branch-tinted top-bar gradient + name + SP/max pill, gold crown + cream `partial` amber tint mid-progress) → `.cluster-canvas` (position: relative, 180px tall, three nodes triangle-positioned by `GridX/GridY`). Header has ONE combined `.sp-pill-combined` showing `★ <available> / <total> SP` with a 2px gold fill across the bottom representing spent/total (single chip, replaces the prior SPENT + AVAILABLE pair). Hex bodies are 52x52 rounded-squares (keystone bumps to 56x56 + 2.5px border). States: `.locked`/`.gated` (dim 0.4 opacity + small lucide:lock corner overlay) / `.available` (strong branch-colored border + tinted bg, NO colored halo — `overflow: hidden` skill-body would leak) / `.partial` (1+ rank) / `.maxed` (solid branch fill + gold border + checkmark badge). Connector lines (`.hex-connector.left/.right`): 2px rotated divs anchored at keystone center (129, 61), rotated ±46.5° to point at petal centers (49/209, 137); faint gray when prereq unmet, branch-color at full alpha when active — visualizes the "energy now flows here" path. Rank badge bottom-right corner with branch-colored border, dark fill (no gold flip — keeps the 18px badge visually consistent). One-shot unlock feedback: `.hex-unlock-pulse.pulse-@version` overlay scaling 1.0→1.4 + fading, rendered only on last-invested hex during pulse window (`lastInvestedId` + `sinceBurst < 0.6f`). 8-particle radial burst at cursor, branch-colored. Tooltip follows cursor with clamped position, branch-tinted top border + branch tag pill, shows current vs next effect in green→gold delta, cost pill, prereq/SP warnings, "click to invest" hint. Inline two-stage reset button on header (no modal — render-key cycle on each state flip so armed pulse keyframe replays cleanly).
- **Content (launch set, 2026-04):** 15 talents total, 55 SP to max-all (matches lv 50 cap exactly — player can max the tree at level cap). Power (3, 12 SP): Might (keystone) / Vitality / Critical Eye. Fortune (3, 12 SP): Gold Rush (keystone) / Bargain Hunter / Jackpot. Fusion (3, 11 SP): Gene Surge (keystone) / Inheritance / Mutation. Expedition (3, 10 SP): Prospector (keystone) / Scout / Lucky Find. Mastery (3, 10 SP): Boss Slayer (keystone) / Resilience / Token Collector. All petals require keystone rank ≥ 1 (`RequiredSkillId` + `RequiredSkillRank: 1`). Post-launch updates will introduce capstones + outer-ring slots — `Slot.Capstone/T2Left/T2Right` enum members + coord constants are intentionally absent (re-add when nodes ship; comment in SkillTree.cs marks this).
- **Juice tier (current / target):** Tier 2 on individual invest (particle burst + hex pulse + branch-color sound). Locked-petal hover plays `PlayDeny` instead of `PlayHover` — audio reinforces the visual lock state. No Tier 3/4 setpieces at launch (capstones removed — `node.Tier == 3` is unreachable). Target: as-is for launch; capstone setpiece mounted alongside post-launch outer-ring talents.
- **Known issues / notes:**
  - Launch tree has no tier-point gates (`gate: 0` on every Add call). Per-node prereqs handle progression. The branch-points counter in the header is purely informational (shows progress toward branch mastery).
  - Migration v2 (`TamerManager.cs:177-225`) preserves surviving talent ranks across the schema change — only IDs no longer in `SkillTree.CreateDefault()` are pruned, surviving ranks are clamped to new MaxRank, SP pool is recomputed as `totalEarned - spentOnPreserved`. Atomic; old playtest investment for surviving talents is kept.
  - Orphan component: `Code/UI/Components/SkillNodeComponent.razor(.scss)` is no longer referenced — the panel renders hex nodes inline. Candidate for deletion if confirmed unused.
  - `overflow: hidden` on `.skill-body` intentionally clips cluster column overflow; per that rule, NO colored box-shadow halos on any hex state. Carry state signal via border + bg tint only.
  - Reset is free (no gem cost) currently — header button uses inline two-stage confirm (no modal). Spec mentioned a gem cost option; left as free per existing manager code.
- **Neighbors:** TamerManager (CurrentTamer.SkillRanks / SkillPoints / CanUnlockSkill / UnlockSkill / ResetSkillTree), GameHUD (currency pill + tab mount).

### ExpeditionPanel
- **Files:** `Code/UI/Panels/ExpeditionPanel.razor` (1867), `.scss`
- **Purpose:** Expedition list → team → battle flow. Dynamic bg class, 3D-battle mode class.
- **Visual style notes:** Multi-view (list/team/battle), difficulty dropdown with hard-active state, controls.
- **Juice tier (current / target):** Tier 1 (user flagged as "doesn't feel satisfying to open"). Target: Tier 2 on open, Tier 3 on expedition clear.
- **Known issues:** **User flagged on 2026-04-09 as flat on open.** High-priority for next dedicated pass.
- **Neighbors:** ExpeditionPopup, AutoBattlerPopup, BattleView.

### BeastiaryPanel (user-facing name: "BEASTBOOK") _[robustness pass 2026-04-17]_
- **Files:** `Code/UI/Panels/BeastiaryPanel.razor` (~1075), `.scss` (~2060)
- **Purpose:** Species catalog / "book of bound beasts." Full-screen two-pane grimoire — 8-col grid of beast cards on the left, detail "page" on the right.
- **Visual style notes:** Roster-style full-screen layout with animated-bg.webp, floating filter header, 480px detail sidebar. Grid cards (172x216) with 180px sprites, number pill header, thin rarity-colored bottom strip (not badge), element icon tiles in name strip. Detail pane: 180x180 portrait container with 160px sprite, neutral dark frame (no rarity/element bleed), identity block (vertical stack: number pill / name / element+personality pills wrap / rarity+BST summary row), diamond action row (Cry/Moves/Evolution/Lore/Mastery). Architecture: fixed top (portrait + identity + flavor) + swappable bottom section via `detailView` state string ("stats"|"moves"|"evolution"|"lore"|"mastery"). Stats default view has 6-bar stat grid + TOTAL row + TRAITS row. Moves view uses strict 4-column row grid (diamond 40px | name+level flex | category pill 50px | power/accuracy 60px right-aligned). Evolution view uses 100x100 headshot-cropped portrait frames per stage (sprite sized 140% with `object-position: 50% 25%` for face focus) with name + "Lv N" evolution requirement below; chevron-right arrows between stages. Lore view = neutral dark rows (not amber) with tier-colored LV chip + tier-colored box-shadow left accent on unlocked rows, dim italic locked text. Mastery view has level badge (1-6 tier colors), tier-colored title pill (`#b8b8b8` Lv0 → `#ec4899` Lv6; selectors flattened to avoid s&box nested-combinator resolution issue), purple progress bar toward next tier, 6-tier checklist with tier-colored row text. Filter state filters by primary OR secondary element (dual-type aware).
- **Juice tier (current / target):** Tier 1-2 ambient (sprite idle on grid cards + portrait + current-stage evo). Target: Tier 3 on new-species discovery reveal — not implemented yet, candidate for a future setpiece.
- **Known issues:**
  - Trait tooltip uses a positioned `:hover` div (s&box won't render native `title` attribute); if `:hover` on a flex child misbehaves, fallback is to track a `hoveredTraitId` in code-behind.
  - Beast-cry click on portrait currently calls `PlayClick()` as placeholder — no per-species audio pipeline yet.
  - **[fixed 2026-04-17]** `.bb-evo-stage.current .bb-evo-portrait` (scss:1779-1789) had element-colored 22px blur halos at 0.42-0.45 alpha inside the `.bb-detail-scroll` scroll container — classic Lesson 3 leak pattern (s&box does not clip outer box-shadow against scroll viewport). Removed colored halos; kept the 0.9-alpha colored border + dark offset drop shadow + existing `transform: translateY(-2px) scale(1.05)` hero lift, which carry the "you are here" feedback cleanly.
- **Neighbors:** FilterBar (shared, untouched), MonsterRosterPanel, BeastiaryManager (data API, untouched).
- **Data additions:** `MonsterSpecies.IsAIGenerated` (bool) and `MonsterSpecies.ArtistCredit` (string, nullable) — additive, UI-only, do not affect battle or breeding. `SecondaryElement` already existed.

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

### MonsterRosterPanel _[robustness pass 2026-04-17]_
- **Files:** `Code/UI/Panels/MonsterRosterPanel.razor` (3361 — largest in project), `.scss`
- **Purpose:** Full monster roster grid with filter bar, sort, rarity filters; fusion sub-view; duplicated move-picker from MonsterDetailPanel.
- **Visual style notes:** Purple theme, concept-D compact cards (recent refactor per git log), helix-particle pattern lives here at scss:1811.
- **Juice tier (current / target):** Tier 1-2. Target: Tier 2 on card hover/select, Tier 3 on evolution.
- **Known issues / notes:**
  - Duplicated move-picker with MonsterDetailPanel (CLAUDE.md quirk).
  - 8 instances of `flex-wrap: wrap` flagged in learnings — need to verify none are broken.
  - Sheer size (3361 lines) makes targeted edits risky.
  - **[fixed 2026-04-17]** `.action-v2.item` (held-item button, line 662) had a Razor @if/else that swapped `<img>` vs `<iconify>` as the diamond child — same ghost-child bug class as the shop Purchase button. Applied stable-DOM fix: both slots always mount, visibility toggled via `has-item` / `no-item` class on the button. See learnings 2026-04-17.
  - **[flagged 2026-04-17]** `.portrait-container.rarity-epic/legendary` (scss:3905-3906) has 16px blur colored halos at 0.2 alpha and lives inside `.detail-scroll`. Alpha is subtle; no observed bug; left alone. If leak is seen on scrolling detail pane, reduce blur or remove colored halo.
- **Neighbors:** MonsterCard, MonsterDetailPanel, FilterBar, BreedingPanel.

### MonsterDetailPanel _[robustness pass 2026-04-17]_
- **Files:** `Code/UI/Panels/MonsterDetailPanel.razor` (1177), `.scss`
- **Purpose:** Single-monster deep view — portrait, stats, moves, journal, actions.
- **Visual style notes:** Back button with animated WebP swap, pill-btn header actions (show-off, favorite, journal).
- **Juice tier (current / target):** Tier 2. Target: Tier 3 on move-equip / level-up / evolve trigger.
- **Known issues / notes:**
  - Duplicated move-picker (see CLAUDE.md quirk).
  - **Panel is NOT mounted anywhere** — grep for `<MonsterDetailPanel` in `Code/UI/` returns zero. Roster panel (`MonsterRosterPanel`) is the live detail pane. Keep this file in sync for now as a legacy reference; if you confirm it's fully dead, propose for deletion.
  - **[fixed 2026-04-17]** `.action-btn.item` (line 314) had a Razor @if/else swapping `<img class="action-item-icon">` vs `<span class="action-icon">⚔</span>` inside the button. Applied stable-DOM fix: both slots always mount, visibility toggled via `has-item` / `no-item` class. Mirrors the roster fix.
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
- **Files:** `Code/UI/Components/ActiveEffectsPanel.razor` (503), `.scss` in GameHUD.razor.scss
- **Purpose:** In-battle active effect/buff display, expandable. Master Ink row removed (one-use item, not a timed buff). Elite Ink kept (timed buff). BuildHash now rounds to whole-second granularity (not per-frame). Expiring-soon border pulse (<60s) via `@keyframes expiring-border-pulse`. Full empty state with icon+label+hint.
- **Juice tier (current / target):** Tier 1-2.
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

### MenuPopup _[redesign 2026-04-18]_
- **Files:** `Code/UI/Components/MenuPopup.razor` (~330), `.scss` (~575)
- **Purpose:** In-game ESC menu — opened from GameHUD top-right "Menu" button. Hosts game shortcuts (Help / Tutorial / Achievements / Options), community actions (Feedback / Discord), utility chips (EN/FR language, mute, credits), and the danger "Return to Main Menu" exit.
- **Visual style notes:** 420px-wide glass-morphism popup, transition-driven reveal (`<root class="@(IsVisible ? "visible" : "")">` toggles `MenuPopup.visible .menu-popup { transform: scale(1) }`), bg-pattern layer (`.menu-bg` mirroring the `.bg-scroll` pattern from Daily/Chat/Inventory at 0.04 alpha). Header has `lucide:swords` title icon + gold version chip (clickable → CreditsPanel) + iconify close button. Sectioned via `.menu-section-title` (3px purple `box-shadow: -3px 0 0` left bar + uppercase 10px label, ported from style-guide). 2x2 grid items use 44×44 colored option-icon tiles with per-icon tints (sky/green/gold/purple/discord/danger). Utility strip beneath the grids hosts language flags, mute toggle, and credits chip — chip family mirrors MainMenu utility-chip language (28px tall, faint border, cream text, purple-on-active). Danger exit button is full-width below utility row with subtle red bleed + chevron-nudge on hover. Footer has Esc hint + close button. Esc-to-close wired via `Tick() + Input.Pressed("Escape")`.
- **Juice tier (current / target):** Tier 1 ambient (purple lift on hover, version chip + utility chip color hovers, danger chevron nudge). Target: keep — utility menu shouldn't compete with rewards/setpiece moments. The redesign earns its calm calmness via section structure, not animation flourish.
- **Known issues:**
  - `GameVersion` is a hardcoded `"0.8.0"` const inside MenuPopup — same string also lives in `MainMenu.razor:130` and `FeedbackPanel.razor:739`. Three places to update on each version bump. **[Flag for product]** Consider hoisting to `Code/Core/AppVersion.cs` or similar.
  - Mute toggle's `_volumeBeforeMute` is a `static float` — restored value persists across MenuPopup remounts but resets to 1.0 on game restart. Acceptable for a session-scoped quick toggle; permanent volume is in OptionsPanel.
  - Language toggle calls `LocalizationManager.SetLanguage(code)` which fires `OnLanguageChanged` — popup `StateHasChanged()` is called locally but other in-flight panels rely on subscribing to `OnLanguageChanged` themselves (existing pattern, not broken by this redesign).
- **Neighbors:** GameHUD (mount point — `GameHUD.OnMenuClicked` calls `MenuPopup.Show()`), HelpPanel, OptionsPanel, AchievementPanel, FeedbackPanel, CreditsPanel, TutorialManager, GameManager (`ReturnToMainMenu`).

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
- **Files:** `Code/UI/Components/NotificationPanel.razor` (261), `.scss` in GameHUD.razor.scss
- **Purpose:** Notification history dropdown with unread count badge. Per-type sounds wired. Achievement type has gold-distinct toast styling.
- **Visual style notes:** bg-scroll pattern (same as DailyPanel), expanded state class. Achievement toast: gold border + dark warm bg + box-shadow, distinct from plain Success green.
- **Juice tier (current / target):** Tier 2. NotificationManager now subscribes to AchievementManager.OnAchievementUnlocked — no double-fire.
- **Neighbors:** GameHUD.

### ChatPanel
- **Files:** `Code/UI/Components/ChatPanel.razor` (457), `.scss`
- **Purpose:** In-game chat with expand/collapse, unread class. Global, Guild, and Trade channels. Achievement messages get gold-left-border treatment.
- **Juice tier (current / target):** Tier 1-2. `hasAchievementUnread` flag drives chat-button gold flash for 2s when achievement arrives with panel closed. Trade channel functional (global broadcast via `BroadcastTradeChat`). Help channel removed.
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

### SkillNodeComponent _[orphan 2026-04-17]_
- **Files:** `Code/UI/Components/SkillNodeComponent.razor` (78), `.scss`
- **Purpose:** (historical) Single skill tree node — used by the previous god-of-war linear skill tree.
- **Status:** **Orphaned** after the 2026-04-17 hex-cluster rework. Grep for `<SkillNodeComponent` returns zero references. The new SkillTreePanel renders hex nodes inline as `.hex-slot` divs directly. Candidate for deletion after a final verification pass.
- **Neighbors:** (none live).

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
- **Files:** `Code/UI/Components/OptionsPanel.razor` (~720), `.scss` (~900). Charter-compliant Center Modal redesigned 2026-04-18.
- **Purpose:** Game settings — gameplay flow, on-screen display toggles, confirmation prompts, accessibility, language, danger zone (full game data wipe).
- **Layout:** Center Modal per menu-style-charter §1. 640px wide, 82% tall (max 820px), centered with overlay click-to-close + X button + Done button. Scale + fade entrance from `transform-origin: 50% 50%` via transition (NOT @keyframes — replays on re-open).
- **Visual identity:** Per-section diamond icon frames with tone classes (gold=Gameplay, sky=Display, rose=Confirmations, green=Accessibility, purple=Language, danger=DangerZone). Diamond-thumb pill toggles + iconify ± steppers. Top/bottom 2px gradient accent lines mirror MenuPopup. All-iconify (no emoji).
- **Sections (in order):** Gameplay (auto-contract, skip animations, level-up notif, discovery alerts, contract warning stepper) / Display (damage numbers, type effectiveness, genetics on cards, compact view, power ratings) / Confirmations (release, fusion, large purchase threshold stepper) / Accessibility (larger text, high contrast) / Language (EN/FR cards) / Danger Zone (Reset Game Data with two-stage confirm).
- **Notable:** Reset-to-Defaults moved from footer to a small `.reset-chip` next to the close button (less ceremonial than a footer button). Notifications section folded into Gameplay (level-up + discovery alerts naturally belong with gameplay flow). Render rows via `RenderFragment ... => __builder => { ... }` helpers with closure-captured params to keep markup DRY.
- **Juice tier (current / target):** Tier 1 (utility). Calm by design — toggles and steppers should feel responsive but not celebratory. The visual richness comes from per-section color identity, not animation. Target: keep calm.
- **Neighbors:** MainMenu, MenuPopup (entry point — `MenuPopup.OnOptionsClicked` → `OptionsPanel.Show`), SettingsManager (state source), GameManager (`ReturnToMainMenu` after Reset Game Data), SaveService (`ForceResetAsync` for data wipe).

---

## High-level takeaways from the scan

1. **Show/Close pattern is consistent** — almost every modal uses `public static bool IsVisible` + `Show()` + `Close()` + `Toggle()`, with `SoundManager.PlayPopup/PlayPopdown`. Good consistency, but only DailyPanel uses `popVersion++` to retrigger open animations — worth promoting this pattern to a shared convention.

2. **Biggest panels:** MonsterRosterPanel (3361), BattleView (2378), HelpPanel (2085), GuildPanel (1928), ExpeditionPanel (1867), ArenaPanel (1387). These are the risk surfaces — any change needs scoping.

3. **Consistent visual grammar:** header with `panel-title-icon` SVG + title, sort-btn pill tabs, close-btn with optional animated WebP swap, bg-scroll SVG pattern for missions/notifications. This vocabulary is solid.

4. **Emoji icon inconsistency:** MenuPopup and ShopPanel header use emoji icons (⚔️, 🛒) where the rest of the project uses SVG. Flag for cleanup.

5. **Currency roll-up is missing project-wide.** GameHUD top bar snaps values instantly. This is the #1 missing ambient juice technique — would benefit DailyPanel, ShopPanel, and every claim in the game.

6. **Ambient idle motion is scarce.** Very few panels have the Balatro breathing. MainMenu's nebula is the closest. Candidates for ambient float: MonsterCard sprites, day-nodes in DailyPanel, hero banners (Profile/Guild).

7. **"Setpiece" moments are all flat.** Evolution, fusion, first-time legendary — these should be Tier 4 but I didn't spot any fullscreen sequence code in the skim. BreedingPanel fuse and DailyPanel Day 7 are the two most deserving candidates.
