# Balance Decisions Log

Append-only record of every balance change the `/balance` agent proposes and executes. Entries go at the TOP (newest first).

Template:

```
## YYYY-MM-DD — Short title
**Scope:** what was changed (e.g. "3 starter species stat rebalance")
**Before:**
- field: value
**After:**
- field: value
**Reasoning:**
- bullet
- bullet
**Impact:**
- downstream thing affected
**Approved by:** user message reference or summary
```

---

## Entries

## 2026-04-24 — Guild leveling overhaul: cap 20 → 50, new XP curve, 10-perk tree

**Scope:** Extend guild progression from L20 → L50, replace XP-per-level formula, swap the 8-perk all-buffs tree for a 10-perk mix (economy + roster + raid + cosmetic + prestige). Auto-mode execution.

### XP curve

`GetXPForGuildLevel(L)` returns the **cumulative** lifetime XP threshold required to BE at level L (the panel uses it that way: `xpInLevel = guild.GuildXP - GetXPForGuildLevel(level)`).

**Before:** `GetXPForGuildLevel(L) = 500 + L² × 50` → threshold to reach L20 = **20,500 XP**.

**After:** `GetXPForGuildLevel(L) = 200 + 1.2 × L³` — cubic gives a sharper late-game ramp than the old quadratic, so 25 → 50 actually feels aspirational instead of just numerically larger.

Sample thresholds + per-level deltas:
| L | Cumulative threshold | Δ from prev L |
|---|---|---|
| 5  | 350     | — |
| 10 | 1,400   | 311 (L9→10) |
| 20 | 9,800   | 1,369 (L19→20) |
| 30 | 32,600  | 3,138 (L29→30) |
| 40 | 77,000  | 5,617 (L39→40) |
| 50 | 150,200 | 8,821 (L49→50) |

Threshold to reach L50 = **150,200 XP** ≈ **7.3× the old to-L20 budget** (20,500). Threshold to reach L20 in the new curve = 9,800 — about half the old to-L20, friendlier early game. The L49→L50 step (8.8k XP) is 6.4× the L19→L20 step (1.4k XP), so the ramp climbs visibly into the late game.

### 10 perks (was 8, all stat-percent stacks → mixed tree)

| L | Name | Effect | Hook | Tier |
|---|---|---|---|---|
| 5  | Coffer Cut         | +5% expedition gold              | `GetGoldMultiplier(true)` | tactical |
| 10 | Pack Mentor        | +10% Tamer XP                    | `GetTamerXPMultiplier()` | tactical |
| 15 | Wider Banner       | Roster cap 30 → 35               | `GetMaxMembers()` | identity |
| 20 | Beast Trainer      | +10% Beast XP                    | `GetBeastXPMultiplier()` | tactical |
| 25 | Raid Vigor         | Raid attempts 3 → 4              | `GetRaidAttemptsPerDay()` | identity |
| 30 | Sharper Eye        | +10% catch rate                  | `GetCatchRateBonus()` | tactical |
| 35 | Halls of Renown    | Roster cap 35 → 40, banner FX    | `GetMaxMembers()` (cap) + UI stub (FX) | identity |
| 40 | Crested Emblem     | Animated emblem + custom titles  | UI stub (no hook yet) | cosmetic |
| 45 | Boss Hunter's Cut  | +15% raid raw damage             | `GetRaidDamageMultiplier()` (applied in `CompleteRaidAttempt`) | tactical |
| 50 | Eternal Beastlord  | Hall-of-fame slot + golden title + emblem ring | UI stub (no hook yet) | trophy |

### Side effects on existing perk math

- Lv4 +5% Tamer XP perk REMOVED. New Tamer XP perk lives at Lv10 (still +10%, same total cap).
- Lv2 +5% expedition gold + Lv8 +10% all gold + Lv15 +15% all gold COLLAPSED into single Lv5 perk (+5% expedition gold). Net gold ceiling reduced from +25% all + 5% expedition to +5% expedition only — old curve was double-stacking economy too aggressively. Want guilds to feel earned, not subsidized.
- Lv6 +5% catch rate moved to Lv30 and bumped to +10% (parity with new "this is a high-tier perk" framing).
- Lv12 +10% beast XP moved to Lv20.
- Lv20 "Exclusive Title" trophy moved to Lv50 and re-named "Eternal Beastlord."

### Files touched

- `Code/Core/GuildManager.cs` — `MAX_GUILD_LEVEL = 50`, `BASE_MAX_MEMBERS = 30`, `BASE_RAID_ATTEMPTS_PER_DAY = 3`, `MAX_MEMBERS = 40` and `MAX_RAID_ATTEMPTS_PER_DAY = 4` kept as legacy ceiling aliases. New `GetMaxMembers()`, `GetRaidAttemptsPerDay()`, `GetRaidDamageMultiplier()` methods. `GetGoldMultiplier`, `GetTamerXPMultiplier`, `GetCatchRateBonus`, `GetBeastXPMultiplier` rewritten for new perk gates. `InvitePlayer` + `ApproveJoinRequest` cap checks now use `GetMaxMembers()`. `CanAttemptRaid` uses `GetRaidAttemptsPerDay()`. `CompleteRaidAttempt` applies `GetRaidDamageMultiplier()` to `rawDamage` before submission. `GetXPForGuildLevel` formula updated.
- `Code/UI/Panels/GuildPanel.razor` — `GetPerkList()` and `GetPerkIcon()` rewritten for the 10-perk tree. Landing-hero stat shows 10 perks (not 8) and uses `BASE_MAX_MEMBERS` for the public-facing "30 members" claim. Raid attempt pip row + label + "attempts left" string all use `gm.GetRaidAttemptsPerDay()`.

### Open questions / unimplemented hooks

- **Lv35 banner FX:** numeric +5 cap works; "animated banner FX" is a UI-only flourish requiring an emblem renderer extension. Currently a UI stub — perk shows in the list but FX doesn't render. TODO when emblem system gets a polish pass.
- **Lv40 Crested Emblem:** UI stub only. Animated emblem frames + per-rank custom title strings need (a) an emblem-frame asset set, (b) a `Guild.RankTitleOverrides` dict on `GuildDefinition` + an editor UI in the leader settings page. Not blocking — the perk advertises, the engine ignores.
- **Lv50 Eternal Beastlord:** UI stub only. Trophy slot needs (a) an API endpoint exposing top-50 guilds, (b) a hall-of-fame panel surface, (c) an `IsLevel50` flag on the master's title chip. Not blocking — server data already records guild levels, frontend hookup is post-launch polish.

### Existing-guild migration safety

Forward-compatible. `GuildDefinition.Level` and `GuildDefinition.GuildXP` are stored as `int` and `long` on the API side; raising the cap from 20 to 50 only widens the valid range. Any guild currently sitting at L20 with leftover XP that would have been clamped under the old cap will simply continue accruing on the new curve from wherever the API stored their XP value. No save migration, no API schema change required. The new XP-per-level formula returns smaller values than the old one at every level (e.g. L19 = 9,275 vs old 18,550), so guilds sitting at L20 will likely level UP a few times immediately after the patch lands as their stockpile of XP exceeds the new thresholds — a pleasant surprise rather than a regression.

**Red flags triggered:** none. Guild XP is server-side state, not damage/Power math; no formula divisors. The +15% raid damage perk is the only direct combat-math touch and it scales raw damage post-roll, not stat weighting.

**Approved by:** auto-mode directive (2026-04-24).

---

## 2026-04-21 — Signature material drops (Monster Hunter-lite) + BeastiaryPanel launch filter

**Scope:** new themed material drop system (1 guaranteed per KO) + Beastiary UI limited to launch roster.

### 1. New `SignatureDropName` / `SignatureDropDescription` fields on `MonsterSpecies`

Added to [MonsterSpecies.cs](../../Code/Data/MonsterSpecies.cs). Item IDs follow `mat_{speciesId}` pattern. Null/empty name falls back to auto-generated "{Species} Spirit" at registration.

### 2. ItemCategory.Material added

New category in [Item.cs](../../Code/Data/Item.cs). Categorizes the 31 launch materials (+ future trade-material content).

### 3. Auto-registration

`ItemManager.RegisterBeastMaterials(speciesDb, launchRoster)` iterates launch roster and creates a `Material` item for each. Called from `MonsterManager.LoadSpeciesDatabase` after species DB is populated.

Rarity mapped from species rarity. Sell prices: Common 25g, Uncommon 60g, Rare 140g. Not buyable — drop-only.

### 4. Drop hook

`OnEnemyDefeated` in ExpeditionManager: **guaranteed +1 material per KO**. Adds to `tamer.Inventory[mat_{speciesId}]`. No RNG — predictable feedback every kill.

### 5. Themed material names (10 hand-authored so far)

| Species | Drop name |
|---|---|
| embrik | Ember Ash |
| pagefin | Knight's Fin |
| cherune | Lost Feather |
| pollenpuff | Pollen Burst |
| twigsnap | Snapped Twig |
| branchling | Heartwood Branch |
| dewdrop | Dewdroplet |
| dustling | Dust Tuft |
| mosscreep | Elder Moss |
| whiskerwind | Whisker |
| glimshroom | Glimcap |

Remaining 20 species in LaunchRoster (zone-2 nature pool, zone-3 water pool, remaining evolutions) use auto-generated "{Species} Spirit" fallback until a themed-naming pass lands. Mechanically functional, just uniform flavor.

### 6. Beastiary UI filter

[BeastiaryPanel.razor](../../Code/UI/Panels/BeastiaryPanel.razor) now uses `GetLaunchSpecies()` helper that filters `MonsterManager.GetAllSpecies()` through `LaunchRoster`. Completion counter shows `N / 30` not `N / 143`.

### 7. Impact

- Every kill now produces a tangible reward (material drop) regardless of RNG item tables.
- Inventory bloats by ~31 new items — manageable. New Materials category keeps them separate.
- No save migration needed (Tamer.Inventory already a string-keyed dict).
- Trade nodes (next session) plug in cleanly — they'll require `mat_{speciesId}` IDs which now exist.
- Side quest objectives can reference material IDs.

**Red flags triggered:** none. Material drops don't touch balance formulas directly.

**Approved by:** user said "lets do it!" after MH-lite themed-name commit (2026-04-21).

---

## 2026-04-21 — Pokemon-style XP yield on defeat + completion bonus

**Scope:** new XP formula for monster leveling, applied to core launch roster (4 handmade + 6 zone-1 wilds + branchling + pollenpuff).

### 1. `BaseExpYield` field on `MonsterSpecies`

Added `public int BaseExpYield { get; set; } = 60;` to [MonsterSpecies.cs](../../Code/Data/MonsterSpecies.cs). Default 60 = sane zone-1 Common baseline.

### 2. Formulas wired in `ExpeditionManager.cs`

**Per-KO XP** (awarded to active player beast when enemy KOed):
```
xp = BaseExpYield × defeatedLevel / 7 × levelRatio × (1 + skillXpBonus)
levelRatio = clamp(defeatedLevel / participantLevel, 0.5, 2.0)
```

**Completion XP** (distributed to every team member on expedition victory):
```
xp = Expedition.XPReward × (1 + skillXpBonus) × hardModeMultiplier × levelRatio
levelRatio = clamp(enemyZoneLevel / monsterLevel, 0.5, 2.0)
```

### 3. Backfilled `BaseExpYield` values

| Species | Rarity | Value | Notes |
|---|---|---|---|
| embrik | Starter | 100 | Flat starter value per reference-values.md |
| pagefin | Starter | 100 | |
| cherune | Starter | 100 | |
| pollenpuff | Common (mascot) | 70 | Standalone Common upper floor |
| twigsnap | Common (evo-base) | 55 | |
| dustling | Common (evo-base) | 55 | |
| whiskerwind | Common (evo-base) | 55 | |
| dewdrop | Common (standalone) | 75 | Higher floor for final-form Common |
| mosscreep | Common (standalone) | 75 | |
| glimshroom | Common (standalone) | 75 | |
| branchling | Uncommon (evolution) | 140 | +85 over base form |

Everyone else in the 143-species pool uses the `BaseExpYield = 60` default. Balance agent tunes per-rarity as beasts come into the launch roster.

### 4. Level-up notification

When a beast levels up (from per-KO OR completion bonus), fires `NotificationManager.AddNotification` with `NotificationType.Success` + `"Level Up!"` + monster name + new level. Per-KO level-ups also play `SoundManager.PlaySuccess()`. No dedicated `OnMonsterLevelUp` event yet — could add if UI wants to hook into it for screen-level juice later.

### 5. Reasoning

- **Level-scaling with 0.5-2.0 caps** (not Pokemon's Gen 5 `(2L+10)/(L+Lp+10)^2.5` curve) — simpler to reason about, readable in playtest, still solves the grinding + over-level problems.
- **70/30 split per-KO vs completion bonus** — per-KO is the main source so in-battle moments feel satisfying. Completion bonus lets bench beasts grow slowly without removing reward from active attacker.
- **Skill XP bonus applies to both** — Tamer's `ExpeditionXPBonus` skill now buffs monster XP too, which is thematic (the Tamer is getting better at teaching).
- **Hard mode multiplier applies to completion bonus** — already baked into `finalXP` at the callsite.

### 6. Impact

- Monsters now level from combat (previously only from contract success + items). Core RPG loop fixed.
- Tamer XP math unchanged — still gets flat expedition completion bonus + per-KO small bonus.
- No save-blob migration — `CurrentXP`/`Level` already persisted on Monster.
- Rare-tier / Epic+ species left at `BaseExpYield = 60` default (they're cut pre-launch); no tuning needed until post-launch roster expansion.

**Red flags triggered:** none. All changes respect 1:1 stat weighting (principle §2), zone progression (§4), and propose-before-edit workflow.

**Approved by:** user said "lets do it!" after reviewing the C/both design + level-scaling formula (2026-04-21).

---

## 2026-04-21 — PowerRating formula fix + starter rebalance + zone-1 pool cleanup

**Scope:** Formula replacement, three starter stat blocks, one expedition pool, one knowledge-file refinement.

### 1. PowerRating formula

**Before** (`Monster.cs:161`):
```csharp
public int PowerRating => (MaxHP / 10) + ATK + DEF + SpA + SpD + (SPD / 2) + (Level * 5);
```

**After** (split into two concepts):
```csharp
// MonsterSpecies.cs — species intrinsic
public int BaseStatTotal => BaseHP + BaseATK + BaseDEF + BaseSpA + BaseSpD + BaseSPD;
// Monster.cs — individual current state
public int PowerRating => MaxHP + ATK + DEF + SpA + SpD + SPD;
```

**Reasoning:** three bugs in the old formula (HP/10, SPD/2, Level×5) caused tanks + speedsters to show weaker than actual, and leveled Commons to out-Power fresh Legendaries. See `power-formula.md` for the full write-up.

**Impact:** all displayed Power values change in ArenaPanel, BreedingPanel, ExpeditionPanel, MonsterCard, ChatManager. No migration — all read dynamically. Old chat-message snapshots remain at their frozen values, which is fine.

### 2. Starter rebalance (embrik / pagefin / cherune → BST 283 parity)

| Starter | Before BST | After BST | Before growth | After growth |
|---|---|---|---|---|
| embrik | 260 | 283 | 22 | 32 |
| pagefin | 300 | 283 | 26 | 32 |
| cherune | 275 | 283 | 23 | 32 |

**Reasoning:**
- Starter BST target 275-290 (reference-values.md) — embrik was below, pagefin was above.
- Internal parity now — no dominant starter pick.
- Archetypes preserved: embrik stays physical attacker (ATK 65 highest), pagefin stays bulky special-defender (HP 52 / SpD 55 still top), cherune stays speedster (SPD 69 by far highest).
- Growth rates bumped to hit Starter band (30-34).

**Impact:** starter early-game feel improves (+5-10 effective power at level 10). No fusion math impact (starters rarely fused pre-launch). No evolution math impact (evolutions unchanged).

### 3. Zone-1 wild pool cleanup

**Before:** `{ twigsnap, dewdrop, dustling, mosscreep, whiskerwind, glimshroom, branchling }` — 7 species, BST spread 150 points.

**After:** `{ twigsnap, dewdrop, dustling, mosscreep, whiskerwind, glimshroom }` — 6 species, BST spread 55 points.

**Reasoning:**
- branchling is an **Uncommon evolution** of twigsnap (BST 385). Evolved forms shouldn't spawn as wild encounters in the base species' starter zone — player should acquire by evolving their own twigsnap, not catching one at level 1.
- Pool spread dropped from 150 → 55 points, well within the 80-point red-flag limit.
- dewdrop (290), mosscreep (290), glimshroom (280) intentionally kept — they're **standalone Commons** (no evolution chain), which deserve a higher BST floor than evo-base Commons. Added a formal distinction to `reference-values.md`.

**Impact:** zone-1 difficulty becomes consistent. Players at Lv 1 no longer face a 385-BST tank. No data-shape changes (still `List<string>`).

### 4. Knowledge update — standalone vs evo-base Common distinction

Updated `reference-values.md` to split Common into two bands:
- **Evo-base Common** (220-260): evolves up, grows out of tier
- **Standalone Common** (260-310): "final-form Common", higher floor

Added two rules:
- **Evolution gap rule:** +80 to +140 BST from Common → Uncommon evolution
- **Wild pool rule:** evolved forms don't spawn in their base species' starter zone

**Approved by:** user said "lets green light it" after revised Task 3 proposal (2026-04-21).

---
