# Currency audit (2026-09-25): proposal only, no code edited

## Summary (recommendations)
1. **Token inflow after the switch is acceptable.** A typical active player goes from about 25 to about 83 Tokens/week (about 12/day; ceiling about 117). The 100-1200 boss shop moves from out of reach to multi-week buys. Keep the amounts. Fix three things: remove arena quests while ranked is off, fix the Token Collector bonus rounding down to 0, and delete the 150-Token Ink x5 (gold sells the same ink far cheaper).
2. **Gems.** The only faucets left are achievements (305 fixed + 52 from arena ranks) and gifts. There is no working sink: no shop item is priced in Gems. Convert both to Tokens 1:1 (the 2c142fc precedent). Delete `CurrencyType.Gems`, `AddGems`/`SpendGems` and `TotalGems`.
3. **Saved Gems.** Migration v3 converts them to Tokens 1:1, with a safety cap of 4000. The quest UI already showed Token icons for these rewards, and 1:1 back-pays exactly the post-switch rate, so a veteran is no richer than a newcomer after the same time. Keep the `Tamer.Gems` property until at least 1.4.
4. **Remove regional tokens (Tide/Loom/Dawn/Threaded).** Nothing spends them, no UI shows them, they are unreleased (only in `patchnotes-pending` 1.2.1), and Tide can never be earned because of a zone-ID bug. Replace with +5 Tokens on the first Hard clear of each expedition.
5. **RewardBundle.** 13 grant sites use 3 grant styles. Gold bonuses are dead or inconsistent: Golden Touch has no skill node, and the guild multiplier in `AddGold` is always 1.0. Put one bonus policy table in the single payout function.

---

## 1. Token economy

**Faucets**
| Source | Amount | Where |
|---|---|---|
| Boss clear, repeatable | 1 (Jackacabra/Twincoil), 2 (Gnollium/Loomweaver/Liliprince) | `BossPoolDatabase.cs:55-59,74,94`; `ExpeditionManager.cs:1632-1681` |
| Boss first clear | +4/+6/+10/+8, about 22 lifetime | same |
| Rare boss ×3 | never fires: no launch pool has `RareBosses` | `ExpeditionManager.cs:1082-1093` |
| Token Collector 10%/rank | `(int)(2×0.20)=0`, adds nothing on repeat clears | `ExpeditionManager.cs:1660-1665` |
| Achievement `boss_tokens_100` | 25 once | `AchievementManager.cs:272-274` |
| Gifts | server-defined | `GiftManager.cs:157` |
| **New:** missions (`GemReward`) | daily 0-3, weekly 0-5, monthly 20-40 | `MissionManager.cs:44-110, 621` |
| **New:** daily / weekly bonus | 5 / 10 | `MissionManager.cs:683, 709` |
| **New:** streak day 5/6/7 | 2/3/5 = 10 per cycle | `DailyRewardManager.cs:136-142, 312` |
| **New:** milestones | 5/10/25/50/100/250 at 7/14/30/60/100/365 days | `DailyRewardManager.cs:402-410, 453` |

**Sinks:** Tribute costs 1/2/3 Tokens for Common/Uncommon/Rare (`ContractGenerator.cs:382-394, 545-551`). Boss shop (`ShopPanelContent.razor:1988-2013`): XP Orb S 100 · Ink×5 150 · Elite Ink 200 · XP Orb L 300 · Rune 400 · Trait Reroll 500 · Gene Booster 1000 · Master Ink 1200.

**Rates.** Typical active player: logs in daily, clears about 2 boss expeditions a day (matches the "Defeat 1 boss" daily and "Defeat 10 bosses" weekly), finishes about 80% of dailies and 2 of 3 weeklies.
- Expected daily-mission Tokens: 0.2 + 0.4 + 1.0 + 0 + 0.6 = 2.2/day. Arena is excluded because ranked is off (`OnlineHubPanel.razor:872`).
- Arena quests can't be finished, so they block bonuses: `arena_challenger` rolls on 20% of days and blocks the daily bonus; `arena_warrior` is in 25% of weekly sets and blocks the weekly bonus.

| Tokens/week | Before | After (typical) | After (ceiling) |
|---|---|---|---|
| Boss repeats | 25 | 25 | 25 |
| Daily missions + bonus (about 50% hit) | 0 | 12 + 17.5 | 18 + 35 |
| Streak | 0 | 10 | 10 |
| Weekly missions + bonus | 0 | 8 + 5 | 12 + 10 |
| Monthly (achievable about 21/month) | 0 | 5 | 7 |
| **Total** | **~25** | **~83** | **~117** |

Month 1 also adds 40 from milestones.

**What that buys (weeks per item, before → after):** Rune 16 → 4.8 · Trait Reroll 20 → 6 · Gene Booster 40 → 12 · Master Ink 48 → 14.5. Master Ink already comes free with every day-7 streak reward (`DailyRewardManager.cs:356`), so at 1200 it is only a sink. Tribute becomes a cheap sink you can use every day.

**Verdict: acceptable.** The shop was priced for an income that never existed. One side effect: bosses now give about 30% of Tokens and quests/logins about 70%, which weakens the "boss currency" identity.

| # | Adjustment | Δ Tokens/week |
|---|---|---|
| A1 | Skip `arena_challenger`/`arena_warrior`/`arena_champion` in generation while ranked is gated (`MissionManager.cs:78, 91, 105`) | about +8 |
| A2 | Token Collector: `Math.Ceiling` instead of `(int)` | about +7 for owners of the node |
| A3 | Delete boss-shop Ink×5. Gold `ink_small` sells 5 ink for 1000 g (`ShopManager.cs:116-124`); one daily mission pays 5000 g+ | 0 |
| A4 (optional, boss identity) | Boss repeat base: Normal 1→2, Elite 2→3 | about +18 → about 100/week |

## 2. Gems

**Faucets**
- 28 fixed achievements, 305 Gems in total, in amounts of 5/10/15/25. Definitions at `AchievementManager.cs:89-379`; grant `tamer.Gems +=` at `:614`. 35 of these Gems are on arena achievements (`:288, 305, 311`).
- Arena ranks Diamond to Mythic pay `rankValue×2` = 52 (`:441`). Not earnable while ranked is off.
- Gifts: `GiftManager.cs:155`, DTO `GiftApiClient.cs:134`.
- `AddGems` (`TamerManager.cs:599`) is only called by a shop refund that can never run (`ShopManager.cs:760`).
- `BattleResult.TotalGems` (`BattleSimulator.cs:2826`) is never used.

**Sinks:** none that work. `SpendGems` (`TamerManager.cs:605`) is only reached through `ShopManager.cs:743`, and all 30 shop items are Gold-priced (`:121-502`). The balance is shown nowhere: `GetGems()` in `GameHUD.razor:1202` and `ShopPanelContent.razor:1023` is never called.

**Proposal**
- Achievements: change every Gem reward to BossTokens 1:1. The lifetime PvE total is about 270 Tokens, about 3 weeks of income.
- Gifts: the client maps `gift.Gems` to Tokens 1:1 as a backstop. The backend (not in this repo) must stop authoring gems; escalate to its owner.
- Delete `CurrencyType.Gems`, `AchievementRewardType.Gems`, `AddGems`/`SpendGems`/`OnGemsChanged`, `TotalGems` and both `GetGems()`.
- UI/narrative lane: `HelpPanel.razor:497, 3212-3242, 3370, 3742, 5541, 5559` and `AchievementPanel.razor:448-478` still describe Gems.

**Existing balances.** Before 2c142fc, quests, streak and milestones paid Gems, so saves hold all of that. Estimate: about 58/week plus milestones (up to 190) plus achievements. A 12-week player has about 790. The legitimate maximum at 6 months is about 3600.

| Option | Fairness | Economy impact |
|---|---|---|
| **1:1 → Tokens (recommended)** | Pays what the quest UI displayed (Token icons) | Exact back-pay of the new rate, so no extra inflation. Worst case about 1 Gene Booster |
| 1:2 → Tokens | Takes away half of what the UI promised | saves a bounded one-time amount |
| → Gold / drop | Worth almost nothing / quietly deletes earned rewards | none |

**Safe migration.** `Gems` lives at `Tamer.cs:27` and is serialized as part of the whole `Tamer` object (`SaveBlob.cs:85-88`).
1. **Don't delete the property yet.** System.Text.Json silently drops unknown fields, so deleting it destroys balances before the migration can read them. Mark it migration-only and remove it no earlier than 1.4.
2. Add a `MigrationVersion < 3` block after v2 (`TamerManager.cs:260-262`):
   - `g = Math.Clamp(Gems, 0, 4000)`, logging anything over the cap
   - `BossTokens += g` directly (not through `AddBossTokens`; not counted in `BossTokensSpent`)
   - `Gems = 0`, and the regional-token step from §3
   - `MigrationVersion = 3`, then `SaveToCloud()`
   - one notification, "Your N Gems are now N Tokens"
3. The bump and the zeroing land in the same save, so it is idempotent for any cloud or cache snapshot. Add a patch note for players.

## 3. Regional tokens

- **Earned:** every Hard clear pays 3-5, repeatable (`ExpeditionManager.cs:46-47, 128-146`), sorted into buckets by expedition ID (`:112-119`).
- **Bug:** the Tide keys `weaverton_pasture`/`weaverton_approach` don't match the real IDs `saltmoor_approach`/`saltmoor_cove` (`:475, 501`), so Tide Tokens are never awarded.
- **Spent:** nothing ("redemption ships in a follow-up", `Tamer.cs:81`).
- **UI:** none. They appear only in `HelpPanel.razor:2870` and `patchnotes-pending.json:227`.
- **Can players tell them from Tokens?** No. They can't see them, and the shared word "Token" would clash as soon as they become visible.

**Recommendation: remove them.** They contradict the three-currency decision, and they are unreleased. Replacement: **+5 Tokens on the first Hard clear of each expedition** (25 lifetime), in line with the boss first-clear bonus of 4-10. Repeat Hard clears already pay ×2 gold, ×2 XP and ×1.5 drops. Paying +1 Token per repeat Hard clear would add about 70/week for a 10-run/day player and nearly double income, so it is rejected.
- Migration v3 zeroes all four if no public build has shipped Hard Mode; otherwise convert at 4:1 with floor.
- Pending patch-note line 227 needs rewording.
- If the user keeps them anyway: rename to *Seals* ("Weaverton / Weaverwood / Weavermere / Hollow Seal"), fix the Tide keys, show them in the Hard Mode UI, and ship the redemption shop with them.

## 4. Reward-definition inventory (RewardBundle input)

Legend:
- **AG** = routed through `AddGold`. It applies Golden Touch (`TamerManager.cs:570`) and the guild multiplier (`:577`), and both are currently no-ops: no skill node grants `GoldFromAllSources`, and `GuildManager.GetGoldMultiplier()` returns 1.0 without `isExpedition` (`GuildManager.cs:1562-1568`).
- **AX** = routed through `AddXP` (shop boost, relic, 2× event, guild, live event).
- **D** = direct write. It skips bonuses, `TotalGoldEarned` and change events.

| # | Source | Defined → granted | Kinds (path) |
|---|---|---|---|
| 1 | Missions | `MissionManager.cs:44-110` → `:605-650` | Gold AG, Tokens, Ink, XP AX, `ItemReward` D (unused) |
| 2 | Daily/weekly bonus | hard-coded `:678-720` | Gold AG, Tokens, XP AX (daily only) |
| 3 | Login streak | `DailyRewardManager.cs:132-160` → `:297-356` | Gold × week multiplier 1/1.5/2 then AG; Tokens; Ink; day 4/6 items D; day-7 Master Ink D |
| 4 | Milestones | `:394-412` → `:444-485` | Gold AG, Tokens, item D, Title D **unvalidated** (none of the five titles is in `HudTheme.cs:56-128`) |
| 5 | Achievements | `AchievementManager.cs:80-445` → `:606-650` | Gold D, Gems D, Tokens D, Ink D, Title (validated), Item, Monster |
| 6 | Gifts | `GiftApiClient.cs:130-150` → `GiftManager.cs:152-175` | Gold/Gems/Ink/Tokens D, items via `AddItem` |
| 7 | Expedition completion | `ExpeditionManager.cs:475-600` → `:1544-1583` **plus duplicate `:853-889`** | Gold × Prospector × guild 5% at **Lv≥2** (hard-coded; the perk is Lv5) × Hard × live event, then AG; XP × skill × Hard, then AX |
| 8 | Boss / Hard | `BossPoolDatabase.cs` → `ExpeditionManager.cs:1632-1681, 128-146` | Tokens (first clear, ×3 rare, Token Collector, Rare Radar); Hard tokens D |
| 9 | Battles/waves | `BattleSimulator.cs:505, 625, 1978` → `BattleManager.cs:629`, `ExpeditionManager.cs:214, 376` | Gold × GoldDropBonus then AG; XP AX; items `AddItem` (Hard ×1.5); materials D |
| 10 | Side quests | → `SideQuestManager.cs:288-294` | **Gold D**, XP AX, SkillPoints D, items |
| 11 | Trade nodes | → `TradeNodeManager.cs:330-336` | **Gold D**, XP AX, SkillPoints D, items |
| 12 | Guild raid/goals | `GuildManager.cs:1354-1359` → `:1976-1981, 1463` | Raid gold AG; goals pay guild XP only |
| 13 | Tutorial | → `TutorialManager.cs:485` | Ink +50 D |

**Inconsistencies to fix**
- Gold:
  - Live-event gold applies to #7 only.
  - Guild gold has two conflicting versions (#7's Lv2 5% vs the Lv5 perk); the comment at `TamerManager.cs:576` is stale.
  - #5, #6, #10 and #11 skip `TotalGoldEarned`, which undercounts `gold_100k`/`gold_1m` and Treasury Push.
  - The shop refund (`ShopManager.cs:758`) goes through AG.
- Tokens: the Tribute spend (`ContractGenerator.cs:550`) skips `BossTokensSpent`. The direct `+=` in #5 and #6 skips `OnBossTokensChanged`.
- Items and titles: some grants write `Inventory[]++` directly and others use `ItemManager.AddItem`; titles have one validated and one unvalidated path.
- **Proposed bundle:** `{Gold, XP, Ink, Tokens, Items[(id,qty)], Titles[], SkillPoints, SpeciesId?}` plus `RewardSource`, and one `Grant(bundle, source)`. Gold bonuses apply to earned sources (#1-5, #7, #9-12) and not to gifts or refunds. Token Collector applies to boss tokens with ceiling rounding. Every write goes through events and lifetime counters.

Nothing edited. Each item needs approval; A3, the copy changes and gift gems are outside the balance lane (UI / backend).
