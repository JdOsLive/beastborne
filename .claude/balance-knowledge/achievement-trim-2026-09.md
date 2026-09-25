# Achievement Restart — proposal (2026-09-25)

Proposal only; no code edited. Brief: **15 max, fresh restart**. Already decided by the user: keep earned Tokens, remove old-achievement titles, unlock the new set retroactively.

## 1. Inventory of the current set (73 = 65 hand-written + 8 generated arena ranks)

Sources: `Code/Core/AchievementManager.cs:66-399`, rank generator `:425-446`.

| Group (ids) | Requirement → targets | Rewards (Tokens = T) | Status |
|---|---|---|---|
| catch_1/10/50/100/500 | TotalMonstersCaught 1/10/50/100/500 | 500g, 2kg, 10kg+10 Ink, 5T+title *Master Tamer*, 25T | OK (500 is a grind) |
| beast_complete | BeastiaryCompleted (launch roster) | 25T + *Beastborne Master* | OK |
| win_1/10/100/1000 | TotalBattlesWon | 500g, 2kg, 10kg, 10T | OK (grind at the top) |
| damage_10k/100k/1m | TotalDamageDealt | 2kg, 10kg, 5T | OK, filler |
| knockouts_10/100/500 | TotalKnockouts | 2kg, 10kg, 10T | OK, filler |
| expedition_1/5/12/16 | HighestExpeditionCleared 1/2/**3/3** | 1kg, 5kg+10 Ink, 5T+20 Ink, 10T+*Conqueror* | **12 and 16 are duplicates** (same target, unlock together) |
| hard_mode_1/10/16 | HighestHardModeCleared 1/2/3 | 5kg, 10T, 15T | OK |
| expeditions_50/250 | ExpeditionsCompleted | 10kg, 10T | grind |
| boss_first / boss_all | BossesCleared 1 / 3 | 5T+*Boss Slayer* / 25T+*Supreme Tamer* | OK |
| breed_1/10/50/100 | TotalMonstersBred | 1kg, 5kg, 5T, 10T+*Master Fuser* | OK (100 is a grind) |
| high_genes | BredHighGenes: sum of all 6 genes ≥25 | 5kg | **Trivial.** Nearly every fusion passes (`MonsterManager.cs:9001-9005`) |
| perfect_gene | Any gene = 30 | 5T | RNG. Wild genes roll 0-30 uniformly |
| gold_1k/10k/100k/1m | TotalGoldEarned | 500g, 2kg, 5T, 10T | passive filler |
| items_10, three_relics, server_boost | shop / relics / boost | 2kg, 3kg, 2kg | trivial |
| boss_tokens_100 | BossTokensSpent 100 (Tribute spend not counted) | 25T | OK |
| arena_win_1/25/100, rank_bronze…mythic (8), win_streak_3/10, arena_sets_100, reverse_sweep | Arena* | 2kg…; ranks pay rank×2kg, +rank×2 T from Diamond up (52T); arena_win_100 gives 15T + *Arena Legend* | **DORMANT**: `MissionManager.RankedEnabled = false` (15 achievements, 87T) |
| trade_1/25/50 | TotalTradesCompleted | 2kg, 5T, 10T | Live (Online Hub → Trade), but needs a second player in the lobby |
| chat_10, beast_showcase | chat | 1kg, 1kg | trivial |
| cards_10 | TamerCardsCollected | 5kg | Cards come only from trades now (the arena path is off) |
| level_10/50 | TamerLevel | 2kg, 10kg | OK; **50 is the level cap** (`Tamer.MaxLevel = 50`) |
| level_100/200/250 | TamerLevel | 5T, 15T, 25T + *Transcendent* | **UNREACHABLE** (cap is 50) |
| skills_10 / skills_25 | distinct talents | 5kg / 5T | 25 is **UNREACHABLE** (the tree has 15 talents) |
| skill_points_100 | SP invested | 10kg | **UNREACHABLE** (55 SP max) |
| evolve_5 / evolve_50 | MonstersEvolved | 5kg / 10T | 50 is a grind |
| veteran_max | Grandmaster mastery on any species | 10kg | OK; not in the retroactive switch |

**Totals:** 73 achievements, of which 53 are reachable today. 382 T defined, 245 T reachable (arena 87 and unreachable 50 excluded). 240,500 gold and 40 Ink. 8 titles, 2 of them unobtainable (*Arena Legend*, *Transcendent*).

**Feature check:** Ranked is off. Guild UI is on, but `RaidsEnabled = false`. Guild weekly goals are live, but no player achievement uses guilds (`GuildManager.AllAchievements` is a separate guild-XP list). Trading is live.

## 2. The new set: 15 achievements, Tokens only, 195 T lifetime, 5 titles

**BUILT 2026-09-25 with FIXED targets** (user: never "all of a growing set"): Beastbook 10 / 20 (26 launch species, 20 solo-reachable), patterns 1 / 2 (3 exist, 2 without a Pagefin), bosses 1 / 3, Hard 1 / 3. Final ids: `beastbook_10`, `beastbook_20`, `pattern_2`, `boss_3` (rows 3, 4, 8, 12 below are superseded).

R = reused id. N = new id. Rewards are modest: 5 for a first-session beat, 10-15 mid-game, 20-25 for a capstone. Current income is about 83 T/week, so the whole set is worth about 2.5 weeks of play.

| # | id | Name | Requirement → target | Reward | Hook |
|---|---|---|---|---|---|
| 1 | catch_1 R | First Contract (renamed) | TotalMonstersCaught ≥1 | 5T | existing, `ExpeditionManager.cs:1888` |
| 2 | tribute_1 N | Offering | Offer your first Tribute (Tokens → contract), win or lose | 5T | **NEW** `TributesOffered`: ++ in `ContractGenerator.AttemptNegotiation` where `BossTokenCost` is spent (`ContractGenerator.cs:545-551`). Also count this spend in `BossTokensSpent` (audit bug) |
| 3 | beastbook_half N | Field Notes | Discover 15 launch species | 10T | **NEW** `BeastbookDiscovered`: `CheckProgress(..., GetDiscoveryCount())` next to the completion check (`BeastiaryManager.cs:196`) |
| 4 | beast_complete R | Beastborne Master | Whole launch Beastbook | 25T + *Beastborne Master* | existing |
| 5 | veteran_max R | Grandmaster Scholar | Grandmaster (mastery 6) on any species | 15T | existing, `BeastiaryManager.cs:482` |
| 6 | breed_1 R | First Weave (renamed) | TotalMonstersBred ≥1 | 5T | existing, `MonsterManager.cs:8998` |
| 7 | pattern_1 N | Pattern Found | Weave your first cross-species pattern | 10T | **NEW** `PatternsDiscovered`: `CheckProgress(..., DiscoveredPatterns.Count)` in `MonsterManager.DiscoverPattern` (about `:9055`) |
| 8 | pattern_all N | The Full Book | Every pattern in the Pattern Book (target = `FusionPatterns.All.Count`, currently 3; static, so safe at init) | 20T + *Master Fuser* (re-described) | same new hook |
| 9 | evolve_1 N | First Evolution | MonstersEvolved ≥1 | 5T | existing, `MonsterManager.cs:9372` |
| 10 | expedition_1 R | First Steps | Clear the Pasture (HighestExpeditionCleared ≥1) | 5T | existing, `ExpeditionManager.cs:778` |
| 11 | boss_first R | Boss Slayer | BossesCleared ≥1 | 10T + *Boss Slayer* | existing, `TamerManager.cs:680` |
| 12 | boss_all R | Supreme Tamer | All 3 bosses (includes the mini-boss) | 25T + *Supreme Tamer* | existing |
| 13 | hard_mode_1 R | Hard Mode Initiate | Clear 1 expedition on Hard | 10T | existing, `ExpeditionManager.cs:759` |
| 14 | hard_mode_16 R | Hard Mode Master | Clear all 3 on Hard | 25T + **new title** *Unyielding* | existing |
| 15 | level_50 R | The Summit (renamed) | Tamer level 50 | 20T | existing, `TamerManager.cs:752` |

**Why:** each pillar gets a first beat and one capstone, all deterministic and visible (no RNG genes, no count grinds). #2, 3, 7, 8 fill gaps (Tokens, Beastbook midpoint, cross-species fusion). *Conqueror* merges into Supreme Tamer (both main bosses = chain cleared). Level 50 gets no title: the level-title system already gives "Legendary Tamer". Don't reuse "Master Tamer" (clashes with the level-40 level title).

**Cut (58):**
- Dormant: all 15 arena achievements.
- Unreachable: level_100/200/250, skills_25, skill_points_100.
- Duplicate: expedition_12, and expedition_16 (merged into boss_all).
- Trivial or RNG: high_genes, perfect_gene, items_10, three_relics, server_boost, chat_10, beast_showcase.
- Grind or filler ladders: catch_10/50/100/500, win_*, damage_*, knockouts_*, expedition_5, hard_mode_10, expeditions_50/250, breed_10/50/100, gold_*, boss_tokens_100, trade_*, cards_10, level_10, skills_10, evolve_5/50.

**UI follow-ups:**
- In `AchievementPanel.razor:177-189` the Battle, Economy, Arena, Social and Secret tabs would be empty. Secret is already empty. Collapse to All / Contracts & Beastbook / Fusion / Expeditions / Mastery.
- Update `HudTheme.cs` (CosmeticDatabase): re-describe *Master Fuser*, add *Unyielding*, and remove *Master Tamer*, *Conqueror*, *Arena Legend* and *Transcendent*.

## 3. Restart migration: MigrationVersion 4

**⚠ Blocker found (commit 8f40af3, today).** The gems→Tokens step is gated `MigrationVersion < 3` (`TamerManager.cs:274`). But `AchievementManager.RetroactiveCheck` has set `MigrationVersion = 3` since July (`ACHIEVEMENT_CLAIM_MIGRATION_VERSION`, `:529-555`). Almost every existing save is already at 3, so **their gems never convert.** Fix: gate that step on `Gems > 0` instead of the version. It is idempotent, because it zeroes Gems in the same save. The lesson for v4: do not share the counter across managers that run at different times.

**Step A, in `TamerManager` hydrate (pure data, no manager dependencies), `if (MigrationVersion < 4)`:**
1. Pay out old rewards that were unlocked but unclaimed, as Gold, Ink and Tokens only (no titles). This needs a small frozen `LegacyAchievementRewards` table (id → gold/ink/tokens), used only here. Otherwise players silently lose rewards they earned. *Recommended; see decision D1.*
2. `Achievements.Clear()`. Do not touch `BossTokens`, so there is no clawback.
3. Strip the 8 old-achievement titles from `UnlockedTitles`: Boss Slayer, Supreme Tamer, Master Tamer, Beastborne Master, Conqueror, Arena Legend, Master Fuser, Transcendent. Set `ActiveTitleId = null` if it pointed at one. **None of these titles has a non-achievement source.** The only other `UnlockedTitles` writers are Alpha/Johnson (`TamerManager.cs:399-417`), guild raid titles (`GuildManager.cs:1987-1993`) and login milestones (`DailyRewardManager.cs:457`: Dedicated, Devoted and others). Keep all of those. The level-title "Master Tamer" lives in the separate `ActiveLevelTitle` field and is unaffected.
4. Set `AchievementRetroPending = true` (new persisted bool) and `MigrationVersion = 4`, then save.

**Step B, in `AchievementManager.RetroactiveCheck` at `GameManager.StartGame` (`:158`), after all managers have loaded, gated on `AchievementRetroPending`:** replace the old claim-migration body with the steps below.
- For every new definition, read its stat. If it meets the target, **unlock it as claimable** (do not auto-grant). Call `Sandbox.Services.Achievements.Unlock(id)`, push the `achievements-count` stat, and show one notification: "N achievements ready to claim". The coming celebration screen and Claim All then give veterans their moment instead of a silent grant. *See decision D2.*
- Clear the flag and save. Titles re-grant on claim, so a player who still qualifies (for example, already cleared every boss) gets *Supreme Tamer* back.

**Retroactive feed:**

| Achievement | Existing stat | Retroactive? |
|---|---|---|
| catch_1 | `TotalMonstersCaught`; fall back to `GetDiscoveryCount() ≥1` (v1 zeroed beta counters) | yes |
| tribute_1 | none exists | **no.** Unlocks on the next Tribute |
| beastbook_half / beast_complete | `BeastiaryManager.GetDiscoveryCount()` | yes |
| veteran_max | `tamer.SpeciesMastery` any level 6 (add to `GetCurrentValueForRequirement`; missing today) | yes |
| breed_1 | `TotalMonstersBred` | yes (reset to 0 in v1 for beta saves) |
| pattern_1 / pattern_all | `MonsterManager.DiscoveredPatterns.Count` (save blob, so run in Step B) | yes |
| evolve_1 | `TotalMonstersEvolved` | yes (reset in v1) |
| expedition_1 | `HighestExpeditionCleared` | yes |
| boss_first / boss_all | `ClearedBosses.Count` | yes |
| hard_mode_1 / hard_mode_16 | `HighestHardModeCleared` | yes |
| level_50 | `Level` | yes |

14 of 15 can be retroactive.

**Code that iterates achievements.** Nothing crashes on unknown ids: `CheckProgress`, the panel and Claim All all walk definitions. Several places count `tamer.Achievements.Values`, so orphan entries would inflate counts, but the wipe in Step A removes all orphans. For future cuts, filter these by defined ids:
- `AchievementManager.GetUnclaimedCount` (an orphan that was unlocked but unclaimed leaves a badge that never clears)
- `GetUnlockedCount`, and the `achievements-count` stat in `UnlockAchievement`
- `TamerCardComponent.razor:203`, `OnlineHubPanel.razor:1193`, `ProfilePanel.razor:384` (shows unlocked/total and can exceed 100%)
- `ChatManager.cs:340, 560`

Old `CheckProgress` calls become harmless no-ops; enum values are not persisted, so removing them is safe too.

## 4. Impact

| | Before | After |
|---|---|---|
| Achievements | 73 (53 reachable) | **15** |
| Lifetime Tokens | 382 defined / 245 reachable | **195** |
| Gold / Ink | 240,500 g / 40 Ink | 0 / 0 |
| Titles | 8 (2 unobtainable) | 5 (4 reused, 1 new) |

A fully qualified veteran claims up to 190 T once (tribute_1 isn't retroactive), about 2 weeks of income, on top of kept Tokens. Acceptable.

## Decisions for the user
- **D1** Pay out old unclaimed rewards before the wipe (Gold, Ink and Tokens, no titles)? *Recommend yes.*
- **D2** Should retroactive unlocks be claimable (celebration moment) or auto-granted silently? *Recommend claimable.*
- **D3** Rewards are Tokens only, 195 total. Is that OK, or should some Gold or Ink be added to the first-beat achievements?
- **D4** Accept the new title *Unyielding* for Hard Mode Master, and the renames: First Contract, First Weave, The Summit.
- **D5** Tribute (tribute_1) vs the boss shop (BossTokensSpent ≥100, which could be retroactive) for the Tokens achievement. *Recommend Tribute.*
- **Must-fix regardless:** the gems migration v3 gate collision in 8f40af3.
