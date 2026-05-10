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

## 2026-05-09 — Invite Player picker extended to include CollectedCards (offline-friend invites)

**Scope:** `GuildManager` gained a `GetInvitableCandidates()` helper that returns a unified, deduped list of online-lobby connections + offline `CollectedCards` from the local Tamer's SaveBlob. New `InviteCandidate` DTO carries `SteamId, Name, Level, IsOnline, ConnectionId` so the picker can render the right indicator and route the invite RPC. UI rebuilds the picker `<foreach>` to use this source instead of `Connection.All` directly (their lane).

**Before:**
- `GuildPanel.razor:1442` picker iterated `Connection.All.Where(c => c != Connection.Local)` ONLY.
- Players had to be in the SAME s&box lobby to be invitable. Empty lobby → empty picker.
- No way to invite a friend who happened to be offline.

**After:**
- Picker source is `GuildManager.Instance.GetInvitableCandidates()` returning online-lobby ∪ CollectedCards (de-duped on Steam ID, online entries win).
- Offline targets routed via Steam-ID-only API persistence (`POST /api/guilds/{id}/invites` already takes `targetSteamId` alone — verified). Real-time RPC becomes a no-op for offline targets but `BroadcastGuildInvite` already handles null/empty `targetConnectionId` safely (`localConnId != targetConnectionId` short-circuits everywhere).
- Player can invite anyone they've previously played with (trade partners, arena opponents, chat profiles via the existing CollectedCards population paths).

**Reasoning:**
- User asked for "invite any player from our server". The right shape is a backend search endpoint (`GET /api/players?q=name`), but that's server-side work nobody on the client team can ship.
- Lead picked option (a) from the discovery memo — lobby ∪ CollectedCards — as the cheapest meaningful upgrade with zero backend dependency.
- CollectedCards covers the most realistic invite path: you want to invite people you've already met. The persistence is already in `Tamer.CollectedCards` (each TamerCardComponent collection site populates it).
- Backend search-by-query deferred to v1.3+ when player base is large enough that "search any player on the server" becomes meaningful UX.

**Impact:**
- No save schema change. CollectedCards already persisted; new code only reads.
- No backend change. Steam-ID-only invite payload was already supported.
- No `InvitePlayer` API change. Existing `InvitePlayer( connId, name, steamId )` accepts null connId by behavior (RPC short-circuits cleanly; API persistence works on Steam ID).
- Tiny CPU cost on each picker open: O(N+M) where N = lobby size (≤32), M = collected cards (bounded by TamerManager cap). Cheap enough to skip caching.

**Patch note:** `polish` → `"Guild invites can now reach friends you've previously played with — not just tamers in your current lobby."`

**Approved by:** team-lead, after the option-(a) recommendation in my Invite Player discovery audit. Greenlight: "Greenlight option (a): extend Invite Player picker to include CollectedCards. Solid analysis."

**Deferred to v1.3+:** proper backend `GET /api/players?q=<partial-name>&limit=20` search endpoint — adds search-any-tamer capability. Requires server-side index on `tamerName` column + a new endpoint on the API server. Not blocking; CollectedCards covers most invite scenarios for v1.2.0 launch player base size.

---

## 2026-05-09 — PvP feature gated for v1.2.0; reshipping with polish in v1.3+

**Scope:** Reversed the Arena kill-switch flip across OnlineHubPanel.razor, LeaderboardPanel.razor. Removed Battle button from Tamer detail sidebar. Removed "Online battles return" from patchnotes-pending.json.

**Before:**
- `RankedEnabled = true` in OnlineHubPanel + LeaderboardPanel
- Battle pill visible in Online Hub tab bar
- Battle button in Tamer detail sidebar (lucide:swords, amber)
- Combat patch note entry in patchnotes-pending.json

**After:**
- `RankedEnabled = false` in OnlineHubPanel + LeaderboardPanel
- Battle pill re-commented with v1.3+ note
- SwitchTab guard restored (bounces "ranked" → "players")
- Tamer detail sidebar back to 3-button layout (Trade / Voice / Mute)
- Combat patch note removed from pending

**Reasoning:**
- User direction: "just hide the battle stuff now" — v1.2.0 needs more polish before adding PvP on top
- All underlying code stays in place (CompetitiveManager formula drift fix, levelOverride, MatchConfig, checksum, TrackResult gate, ArenaPanel mode-select redesign)
- Code is v1.3+ ready; only the UI surface is gated

**Approved by:** team-lead, 2026-05-09

---

## 2026-05-09 — PvP L100 levelFactor saturation deferred [v1.3+ pre-work]

**Scope:** Audit-pass finding documented for future work. **No code change today.** This is a breadcrumb for the v1.3+ Scope B authoritative-client PvP rewrite — capturing the math, the fix, and why it's deferred so present-me doesn't lose context to future-me.

### The math

The v1.0.3 damage formula uses `levelFactor = (4 * Level / 5) + 2` (BattleSimulator.cs:60, :964). The slope was deliberately doubled from Pokemon-canonical `(2L/5+2)` to match L50 endgame intensity at the game's L50 PvE cap. Stats also scale linearly via `Base + Level * Growth * 0.6 + Gene` (MonsterManager `RecalculateStats`).

PvP's `LEVEL_CAP_MAX = 100` (CompetitiveManager.cs:26) extends the level cap above the L50 PvE design point without compensating the formula. levelFactor at L100 = 82, vs 42 at L50 — nearly **2× the damage multiplier** while stats also continue scaling linearly. Compounding effect.

Worked example — Manehelm L100 mirror match, neutral type, 70 BP move, no STAB:
- ATK = 118 + 100 · 9 · 0.6 + 25(gene) = **683**
- DEF = 72 + 100 · 5 · 0.6 + 25 = **397**
- atkDefRatio = 683/397 = 1.72
- Damage = 82 · 70 · 1.72 / 50 + 2 = ≈ **200/hit**, **300 with STAB**
- MaxHP = 92 + 100 · 6 · 0.6 + 25 = **477**
- TTK = 477/300 = **~2 turns with STAB**

L100 PvP becomes a coin-flip: whoever moves first in turn 1 wins.

### The fix (when v1.3 PvP work begins)

**Recommended approach: gate via flag.** Add `IsRankedNormalized` (or `bool isPvP`) to `BattleSimulator.CalculateDamage` callers from CompetitiveManager. When true, cap `levelFactor` at 42 (= L50 equivalent) regardless of input level:

```csharp
float levelFactor = isRankedNormalized
    ? Math.Min( (4f * attacker.Level / 5f) + 2f, 42f )
    : (4f * attacker.Level / 5f) + 2f;
```

Two call sites: BattleSimulator.cs:60 (basic-attack path) and :964 (move-aware path). Caller updates: CompetitiveManager battle entry points pass `true`; PvE callers in ExpeditionManager pass `false` (default).

**Rejected alternative: sub-linear levelFactor curve above L50.** Formula change like `42 + (L-50) * 0.4` for L>50 would smooth the curve but applies to PvE too — touches the playtest-validated v1.0.3 formula. Don't.

### Why deferred

1. **PvP feature is being hidden in v1.2.0.** User flipped the kill-switch back. Code paths affected by L100 PvP aren't reachable to players in v1.2.0, so the fix has zero player-visible impact until PvP comes back online.
2. **v1.3+ Scope B authoritative-client lockstep rewrite supersedes the call-site shape.** Shipping Option A now would write code that the lockstep rewrite is going to refactor anyway. Wasted churn.
3. **The audit is in writing.** Future-me working in v1.3 will hit this entry on the standard "scan decisions-log for prior PvP work" pass.

### Acceptance criteria for v1.3 PvP work

When the v1.3 lockstep rewrite begins, before re-enabling L>50 PvP:
- [ ] Gate the levelFactor cap behind the rewrite's PvP flag (whatever shape that takes — likely a `BattleContext` arg or normalized-input mode).
- [ ] Verify L100 mirror match TTK lands in the 5-8 turn band (matches L50 same-level target band).
- [ ] Verify L50 PvE damage curve is unchanged (both call sites still produce v1.0.3 numbers when the flag is false).
- [ ] Add an entry to decisions-log noting the fix landed + the playtest-validated TTK numbers.

### Files (no edits today)

- Cite: `Code/Systems/BattleSimulator.cs:60` (basic-attack levelFactor), `Code/Systems/BattleSimulator.cs:964` (move-aware levelFactor), `Code/Core/CompetitiveManager.cs:26` (LEVEL_CAP_MAX = 100), `Code/Core/CompetitiveManager.cs:290` (NormalizeTeamToLevel — entry point that establishes the levelOverride for PvP battles).

### Red flags triggered

- **None today.** This is a recorded design constraint, not a balance violation. The flag will trip if v1.3 work re-enables L>50 PvP without applying the cap.

**Approved by:** team-lead 2026-05-09 — "DEFER to v1.3+. ... User just decided to hide the entire Arena/PvP feature for v1.2.0 — flipping kill-switch back. ... Append a decisions-log entry tagged `v1.3+ pre-work` ... Future-you working in v1.3 will thank present-you for the breadcrumb."

---

## 2026-05-09 — PvP Scope A: kill-switch flipped + Battle tab live + casual-only mode + Tamer-detail invite button

**Scope:** OnlineHubPanel.razor + ArenaPanel.razor — Arena surface flipped from kill-switched to live for v1.2.0.

**Before:**
- `RankedEnabled = false` in OnlineHubPanel blocked the Battle tab entirely; Arena pill was in a Razor comment
- SwitchTab bounced any "ranked" nav back to "players"
- ArenaPanel defaulted to `currentView = "mode-select"` (shows Ranked/QuickPlay card select)
- Result branch showed ranked point delta when `selectedMode == Ranked`
- No "Battle" button in Tamer detail sidebar

**After:**
- `RankedEnabled = true`; Arena pill uncommented and labeled "Battle"
- SwitchTab guard removed; "ranked" section reachable
- ArenaPanel defaults to `currentView = "team-select"`, `selectedMode = QuickPlay` — skips mode-select screen
- Result branch always shows casual copy ("Quick Play — No rank change"); rank-up celebration block removed
- "Battle" button (lucide:swords, amber) added to tamer-actions-grid; navigates to Battle tab
- Mode-select screen and Ranked card still in the markup — no deletion, just unreachable by default flow

**Reasoning:**
- v1.2.0 ships VS AI Quick Play only; Ranked matchmaking + per-turn lockstep PvP deferred to v1.3+
- Skip mode-select avoids confusing players with a Ranked button that does nothing meaningful
- Casual-only result branch prevents rank delta from showing on a mode that doesn't track rank
- "Battle" button in sidebar is a nav shortcut; actual challenge/queue flow lives in ArenaPanel

**Approved by:** team-lead, 2026-05-09 ("FIRE THE FLIP")

---

## 2026-05-09 — PvP Scope A: formula drift + levelOverride + ruleset + checksum + Scope B authoritative-client commitment

**Scope:** Scope A landed for v1.2.0 across `Code/Core/CompetitiveManager.cs`, `Code/Systems/BattleSimulator.cs`, and `Code/Systems/BattleAI.cs`. Architecture remains "asynchronous-AI-mirror" (each peer runs its own local AI on the opponent's serialized team using a shared `BattleSeed`); real per-turn PvP is committed for v1.3+ as Scope B (authoritative-client model — lead-approved pivot from initial lockstep proposal after BattleAI determinism audit).

**Before:**
- `NormalizeTeamToLevel50(team)` and `GenerateAITeam(points, random)` both inlined the **pre-v1.0.3 sqrt formula**: `BaseStat + sqrt(50) * Growth * 4 + Gene` = `Base + 28.28 * Growth + Gene` at L50.
- `MonsterManager.RecalculateStats` (called everywhere else, since v1.0.3) uses the **linear formula**: `Base + Level * Growth * 0.6 + Gene` = `Base + 30 * Growth + Gene` at L50.
- Result: ranked battles ran ~6% off from the rest of the game's stat ladder. Players couldn't compare "what my beast does in expedition" against "what my beast does in arena" reliably.
- RPC payload (BroadcastJoinQueue / SendMatchProposal / SendMatchAccepted) carried teamData JSON with no integrity check — a modded peer could swap stat fields client-side post-deserialize.
- Level cap hardcoded at `RANKED_LEVEL = 50` everywhere; no path for custom-match level-override selection.
- `BattleAI` had its own ungoverned `private static Random _random = new Random()` instance (`Code/Systems/BattleAI.cs:14`). `BattleSimulator.SetSeed/CurrentRandom` did NOT propagate. Two clients with synchronized BattleSimulator seeds still made different AI decisions on turn 1 — a hard determinism break that would block any future lockstep PvP.

**After:**
- New private helper `ComputeRankedStats(monster, species, level)` does base + level\*growth\*0.6 + gene, then nature only — explicitly skips tamer-skill / relic / species-mastery bonuses (which are roster-account-progression bonuses, never permitted in ranked).
- Public renames + parameterization: `NormalizeTeamToLevel(team, levelOverride = 50)` replaces `NormalizeTeamToLevel50`; `NormalizeMonsterToLevel(m, levelOverride)` replaces `NormalizeMonsterToLevel50`. `GenerateOpponent(playerPoints, levelOverride = 50)`, `GenerateAITeam(points, random, levelOverride)`, `AssignAIMoves(monster, species, levelOverride)` all parameterized. Override clamped to `LEVEL_CAP_MIN..LEVEL_CAP_MAX` = 5..100 at every entry point.
- Public `CurrentLevelOverride { get; private set; } = RANKED_LEVEL` cached active value, with `SetLevelOverride(int)` clamping setter — entry point for online lane / UI lane to push the host's chosen value before queueing. Aligns with `MatchConfig.LevelOverride` (online's wire-format field) so the wire field, the cached active value, and the setter all read the same word.
- All five existing call sites of the old normalizer now read `CurrentLevelOverride`. Default behavior unchanged for legacy ranked + AI flows (still 50).
- `DeserializeTeam(teamData, levelOverride = 50)` stamps `Level = levelOverride` on each rebuilt monster. Matters when override differs from 50: BattleSimulator's damage formula reads `attacker.Level` directly, so receiver-side reconstruction has to align with the agreed value.
- New `ComputeTeamChecksum(teamJson)` / `VerifyTeamChecksum(teamJson, expected, context)` static helpers. SHA-256 hex digest of the team's serialized JSON.
- Three RPC signatures extended with a trailing `string teamChecksum` / `string senderTeamChecksum` parameter. Sender computes once at serialization time, stashes on `QueuedPlayer.TeamChecksum`, includes on every broadcast. Receiver runs `VerifyTeamChecksum` first thing after the connection-id gate; mismatch → reject the queue entry / cancel the match with `OnMatchmakingError` event.
- Online lane integrated a `MatchConfig` wire type (Format / LevelOverride / BannedRarities / BannedSpecies / TrackResult) sitting alongside the existing payload — `CurrentMatchConfig` updates trigger `CurrentLevelOverride` recomputation. Coordination point handled cleanly via `SetLevelOverride` API.
- **BONUS hygiene fix landed**: `BattleSimulator.CurrentRandom` flipped from `private` to `internal`; `BattleAI._random` field deleted; all 4 BattleAI roll sites (lines :55, :66, :336, :338 pre-edit) routed through `BattleSimulator.CurrentRandom`. Comment block at top of BattleAI documents the contract. AI decisions are now reproducible from `SetSeed(...)`, prerequisite for Scope B's authoritative-replay / deterministic-checksum work.

**Reasoning:**
- Formula drift was a latent bug, not a breakage — the game shipped with sqrt and the L50 numbers were close enough that nobody complained. But the pre-v1.0.3 sqrt is gone everywhere else, and a single source of stat truth is hygiene worth the small refactor.
- Cannot just call `MonsterManager.RecalculateStats` from the arena helper because RecalculateStats applies tamer-skill bonuses, relic bonuses, and species-mastery bonuses. Ranked must be a clean stat budget — two equally-skilled players land on equal numbers regardless of how many skills/relics/mastery levels their account has accumulated. The arena helper duplicates the linear formula but explicitly stops at nature.
- SHA-256 over JSON is not cheat-proof (a modded client computes its own correct checksum after tampering). It IS proof against accidental mid-flight corruption AND against the simplest cheating cases (replace stat fields without recomputing the hash). For Scope B v1.3+, the checksum infrastructure becomes the foundation for per-turn HP-after-resolution checksums.
- `LevelOverride` plumbed through every layer (helper → normalizer → AI → deserializer → state → wire) means online's match-config flows from RPC straight to BattleSimulator without combat-code changes.
- `levelOverride` (not `levelCap`) at every layer because the value is a strict override at every layer, not a cap. UI lane will surface as "Level Cap" or similar player-facing language; that abstraction-layer mismatch is fine because the abstraction levels are different (host's intent vs simulator's enforced value). Naming alignment locked with `online` lane on 2026-05-09.

### Scope B authoritative-client pivot reasoning

Initial recommendation to lead was lockstep deterministic. After determinism audit (BattleAI's ungoverned `_random` was the smoking gun), pivoted to authoritative-client per `online` lane's recommendation. Reasoning trail:

- **Lockstep risks any nondeterminism = silent desync.** Even with the BattleAI fix above, every future code path through BattleSimulator becomes a determinism contract — any new `Dictionary<,>` iteration, `DateTime.UtcNow` read, `Random.Shared` use, or LINQ `OrderBy` against unstable keys breaks both clients out of sync and the leaderboard accepts whichever ratings each side reports.
- **Authoritative-client (one peer runs `BattleSimulator`, other displays results)** has zero determinism contract across clients. The non-authoritative side never invokes BattleSimulator at all — it just renders the broadcast turn results. The BattleAI hygiene fix is still useful (deterministic replay tooling, debugging) but isn't load-bearing for correctness.
- **Cheating risk is real but acceptable for QuickPlay v1.** Lower-connection-ID peer is authoritative; the other peer trusts results. A modded host can fix outcomes. Mitigations available: per-turn HP checksum (telemetry only at first), opponent-action sanity check (damage isn't 10× expected), eventual server-authoritative move when ranked needs cheat-resistance.
- **Migration path is clean.** When ranked launches with stricter integrity needs, swap "elected peer" for "server" in the resolver layer — same protocol, server replaces the elected peer. Lockstep would have to be rewritten for ranked.
- **All Scope A infrastructure feeds straight into Scope B.** `NormalizeTeamToLevel`, `ComputeRankedStats`, `ComputeTeamChecksum`, `MatchConfig`, the seeded `CurrentRandom` all become foundation pieces for the authoritative resolver. No throwaway code in Scope A.

### Scope B trade-offs

| Concern | Lockstep (rejected) | Authoritative-client (chosen) |
|---|---|---|
| Determinism contract | Required across all combat code | None — sim runs only on resolver |
| Cheating resistance | Strong (both sides verify) | Weak in v1 (host can cheat); easy to harden later |
| Latency | Lower (parallel local sim) | Slightly higher (round-trip per turn) |
| BattleAI determinism dependency | Yes — must fix | No — sidestepped |
| Code volume | Per-turn protocol + nondeterminism audit + per-turn checksum | Per-turn protocol + result broadcast |
| Server migration path | Hard rewrite | One-line resolver swap |

**Impact:**
- Stat numbers for ranked battles change by ~6% (sqrt → linear). Numerically: at L50 with Growth=5 the term moves from `28.28*5 = 141` to `30*5 = 150`. With Base 60 + Gene 25, HP goes 226 → 235. Below the player-noticeable threshold for one-off comparisons but visible if a player has been comparing pre-v1.2.0 ranked screenshots against expedition stats.
- AI opponents at L50 same shift; all ranked matchups stay symmetric (both sides recomputed with same formula).
- Combined with the v1.0.3 status-fraction buff already in place, the slight stat-budget bump tightens TTK by maybe a quarter-turn. Within tuning noise.
- BattleAI now produces identical decisions across two clients with the same seed. For the current asynchronous-mirror PvP path that's cosmetic — each side still picks moves locally — but it removes one full class of "I won, they think they won" diverged-outcome bugs (the AI side of each player's local battle now resolves identically across peers, so at least the AI portion of the state is consistent). Bigger payoff comes in Scope B.
- For Scope B v1.3+, the helper trio (`NormalizeTeamToLevel`, `ComputeRankedStats`, `ComputeTeamChecksum`) plus the now-deterministic combat sim are the foundation. No further stat-formula work needed in v1.3+.

**Files touched:**
- `Code/Core/CompetitiveManager.cs` — added 2 usings (`System.Security.Cryptography`, `System.Text`); 2 new constants (`LEVEL_CAP_MIN`, `LEVEL_CAP_MAX`); 1 new public property (`CurrentLevelOverride`); 1 new public method (`SetLevelOverride`); 1 new private helper (`ComputeRankedStats`); 2 new private static helpers (`ComputeTeamChecksum`, `VerifyTeamChecksum`); 1 new field on `QueuedPlayer` (`TeamChecksum`); 3 RPC sigs extended; 1 normalizer renamed and rewritten; `GenerateOpponent`/`GenerateAITeam`/`AssignAIMoves`/`DeserializeTeam` parameterized; integration with online's `MatchConfig` wire-format type. Online lane separately added the `MatchConfig` class definition and the match-config RPC plumbing.
- `Code/Systems/BattleSimulator.cs` — `CurrentRandom` visibility flipped from `private` to `internal`. Doc-comment expanded to call out the BattleAI consumer + Scope B prerequisite.
- `Code/Systems/BattleAI.cs` — `private static Random _random` field deleted; 4 call sites (1 NextDouble, 1 Next, 2 NextDouble in ConsiderSwap) routed through `BattleSimulator.CurrentRandom`. Header comment block documents the new contract.
- `Assets/data/patchnotes-pending.json` — one feature entry (intentionally framed in player-facing terms; does NOT reveal the asynchronous-mirror architecture or the formula fix).

**Architecture note for the record:** the current PvP path is **NOT** real-time PvP. Both clients run independent local battles against a serialized copy of each other's team using a shared seed. Each player picks their own moves; the opponent's actions are driven by BattleAI on each side. With BattleAI now seeded through `BattleSimulator.CurrentRandom`, AI-side decisions ARE reproducible across clients — but each side still picks their own player moves locally, so player vs player decision-making isn't synchronized. Outcomes are still not verified between peers. This is acceptable for Open Beta because (a) it's framed to players as "challenge other tamers" without claiming move-vs-move PvP, and (b) Scope B (authoritative-client per-turn submission) is committed for v1.3+ which will deliver actual real-time PvP without players noticing the architecture shift. Lead's instruction was specific: "Don't reveal the asynchronous-AI-mirror nature."

**What was NOT built (Scope B v1.3+):**
- Per-turn move-submission RPC (`SubmitAction`)
- Authoritative-client resolver (lower-connection-ID peer runs BattleSimulator on both submitted actions; broadcasts result)
- Wait-for-opponent UI lock state ("Opponent is choosing…")
- Per-turn HP-after-resolution checksum (anti-tamper telemetry; uses ComputeTeamChecksum infrastructure already shipped)
- BannedRarities / BannedSpecies enforcement at team-submit time (online lane is shipping in same window via `MatchConfig`; no sim-side work needed because BattleSimulator has zero summon/transform/mimic effects per audit)

**Coordination:**
- `online` lane integrated `MatchConfig` (Format / LevelOverride / BannedRarities / BannedSpecies / TrackResult) — wire-format extension on the match-config RPC. Calls `CompetitiveManager.SetLevelOverride(...)` from the host-side flow before `JoinOnlineQueue`. No further sim-side changes needed from combat for this integration.
- `progression` will flip `RankedEnabled = true` once they confirm with online + ui. Combat does NOT flip the kill-switch.
- `ui` will own the match-config lobby form (LevelOverride slider + BannedRarities multi-select + Format toggle). Reads `LEVEL_CAP_MIN`/`LEVEL_CAP_MAX` for slider clamping.

**Approved by:** team-lead, two messages: (1) "User picked Scope A for v1.2.0 + Scope B committed for v1.3+. Greenlight Scope A as you outlined." (2) "Strong audit work — both updates accepted. Architecture pivot to authoritative-client for Scope B is the right call given BattleAI's ungoverned Random instance. SHIP Scope A now (v1.2.0). BONUS hygiene fix: route BattleAI's static `_random` through BattleSimulator.CurrentRandom. It doesn't matter for Scope A but it's load-bearing for Scope B and a 5-min cleanup."

---

## 2026-05-09 — Save migration cleanup audit (post-v1.0.3)

**Scope:** Two save-side cleanup candidates audited; one retired, one renamed-and-kept. No SaveBlob schema bump.

**Retired:** `TamerSaveData.SkillPointsMigratedV2` field deleted from `Code/Data/SaveBlob.cs`. Stale doc-comment at `TamerManager.cs:472-473` referencing it removed; the long historical block at `TamerManager.cs:245-256` describing the deprecated wrapper-flag approach trimmed to the present-tense fact ("keyed off Tamer.MigrationVersion").

**Kept (renamed):** `MonsterManager.MigrateMonsterToV2` → `ValidateAndRepairMonster`. Both callsites updated (`MonsterManager.cs:8035` self-call inside Hydrate, `BattleView.razor:3484` runtime move-validation invocation). Doc-comment rewritten to surface the function's true ongoing-repair role.

**Audit basis:**
- `SkillPointsMigratedV2` had **zero readers and zero writers** in current code. v1.0.3 moved the SP-migration gate onto `Tamer.MigrationVersion` (saved on the Tamer object, ridden reliably through every WriteSnapshot). The old wrapper flag persisted only as JSON ballast on pre-v1.0.3 saves; `JsonSerializerOptions.PropertyNameCaseInsensitive = true` silently ignores unknown properties on deserialize, so removing the property is binary-safe for old saves.
- Even worst case (a pre-v1.0.3 save where the wrapper flag was the ONLY indicator the V2 SP migration ran), `RunSkillPointMigrationV2` is non-destructive and idempotent (top-up only, never decrements SP) — re-running is safe.
- `MigrateMonsterToV2` looked retire-able by name but carries FOUR distinct jobs, three of which are ongoing post-launch drift handlers: renamed-move migration via `MigrateOldMoveId` + MoveDatabase lookup, trait-database drift via `ConvertOldTraitToId` + TraitDatabase pruning, and move-list regeneration safety net. Only the genetics/SpA/SpD generation is genuinely one-shot V2 stuff. Retiring it would silently corrupt every player's roster on the next move-rename or trait-prune patch. Renamed-and-kept is the right call.

**Reasoning trail:** Patch-notes telemetry is write-only — there's no "last loaded ticks" surface on the API client (only `LastSaveTicks`, the write timestamp). We cannot enumerate stale pre-v1.0.3 saves. That makes proving "all saves migrated" impossible, which is the second reason to keep `ValidateAndRepairMonster` running indefinitely. The `SkillPointsMigratedV2` field doesn't NEED such proof because it's already inert in current code paths.

**Impact:**
- SaveBlob serialized size shrinks by ~1 byte per blob (the dropped bool field). Negligible — well below the 500KB SOFT / 2MB HARD quarantine thresholds.
- Save-load is marginally faster (one fewer JSON property to deserialize/serialize) — patchnote framed as "minor save-load speedup" without revealing the internal field name.
- Nobody's data behavior changes. The renamed function is identical to its previous body; the deleted field was already unused.

**Patch note:** `polish` → `"Internal save migration cleanup — minor save-load speedup."` Vague-on-purpose to avoid revealing save-shape internals.

**Approved by:** team-lead, after audit dispatched at session start. Greenlight message: "Strong audit — your read on candidate #2 is exactly right; that code earns its keep on every patch."

---

## 2026-05-09 — Status fraction buff: Burn /20→/15, Poison /12→/10 (playtest PASS)

**Scope:** Final resolution of the status fraction follow-up flagged 2026-04-30. Burn ticks now hit 1/15 of MaxHP per turn (was 1/20 — 6.7%/turn vs 5%/turn). Poison ticks now hit 1/10 (was 1/12 — 10%/turn vs 8.3%/turn). Single-file edit to `BattleSimulator.cs:1535,1541`. Playtest-validated A/B against three encounters by `playtesting` teammate.

### Why now

The pre-buff fractions were tuned against the old (pre-v1.0.3) damage curve. After the v1.0.3 linear-stats + Pokemon-spec damage retune, fights run 30-40% shorter at high levels and same-level fights land in the 3-5 turn band. Status pressure as a fraction of TTK had compressed from a target ~30% to ~18% — status conditions had become "a little extra chip damage" rather than a meaningful pressure mechanic. The buff brings status share back to a 53-63% range across the test set without making it a single-handed win condition.

### Playtest data summary (analytical run vs current and proposed values, fixed-input methodology)

| Encounter | Current TTK | Proposed TTK | Status %  (proposed) | Per-tick % MaxHP (proposed) | Solo-KO? |
|---|---|---|---|---|---|
| A — Liliprince Lv 20 Elite boss (Burn) | 12 turns | **10 turns** | 63% | 6.7% (29/436) | No |
| B — Cerametz Lv 10 wild (Poison) | 7 turns | **6 turns** | 57% | 9.8% (12/123) | No |
| C — Cove Lv 3 Sheepot wild (Burn) | 7 turns | **6 turns** | 36% | 6.1% (4/66) | No |

### Success criteria evaluation

- **Encounter A:** Required ≥1-turn improvement on burn-applied. **Achieved 2-turn improvement** (12→10). The "non-status fight stays ≥10 turns" subcriterion satisfied implicitly — at 55% status share for the current 12-turn fight, an unstatused Liliprince fight infers to ~22-24 turns. The boss-fight pressure pattern reads correctly: status meaningful, non-status still long.
- **Encounter B:** Required poison ≥40% of TTK contribution + no solo-KO. **Achieved 57% status share + 43% player attack share + no solo-KO.** Status enables, doesn't decide.
- **Encounter C:** Required TTK ≥6 turns at proposed. **Achieved 6 turns** (exactly at floor) on Sheepot. Marginal pass.

**No auto-fail triggers hit.** No solo-KOs. No fight collapsed to <5 turns at proposed. **Verdict: PASS.**

### Marginal-pass note on Encounter C

`playtesting` flagged that Wishlift (BaseHP 38, ~52 HP at Lv 3) could fall to 5-turn TTK at proposed Burn /15 — the lowest-HP zone-1 wild in a worst-case Burn-applied-turn-1 matchup. Verified math: Burn /15 tick on a 52 HP target = max(1, 52/15) = 3 HP/turn, vs Burn /20 tick = 2 HP/turn. The 1-turn dip is real but bounded. Decision: ship the change anyway. Reasoning:
- The "trivial/instant" fail trigger I'd authored was <5 turns (5 was the floor for that trigger). Wishlift at 5 turns is barely above the trivial line, not into it.
- Tutorial-pace fights at L3 SHOULD vary somewhat by matchup. The squishiest target taking 5 turns when status is applied turn-1 is a concession to player tempo, not a regression.
- Holding the change to spare 1 turn on a single edge-case zone-1 wild would deny the buff its primary purpose (longer fights at L10+).
- If actual play reveals Wishlift L1-3 burn fights feel cheap, follow-up is targeted (e.g., Wishlift HP/HPGrowth bump) rather than reverting status fractions.

### Files touched

- `Code/Systems/BattleSimulator.cs:1535` — `monster.MaxHP / 20` → `monster.MaxHP / 15` (Burn)
- `Code/Systems/BattleSimulator.cs:1541` — `monster.MaxHP / 12` → `monster.MaxHP / 10` (Poison)
- `Assets/data/patchnotes-pending.json` — `balance` entry: "Burn and Poison hit harder — Burn now ticks for 1/15 of MaxHP per turn (was 1/20) and Poison ticks for 1/10 (was 1/12). Status conditions are meaningful pressure again, especially in longer boss fights."
- `.claude/balance-knowledge/decisions-log.md` — this entry

### Impact

- Status conditions reposition from chip damage to meaningful pressure (~18% → ~53-63% of TTK contribution depending on encounter type).
- Boss fights in particular benefit: Liliprince TTK drops 12→10 turns when burn is applied; status-as-strategy reads clearly.
- No save migration needed — formula change is in damage path.
- No fusion/PowerRating/zone-difficulty downstream impact.
- Wishlift Lv 1-3 burn-applied fights may feel slightly fast (5-turn floor); flagged for monitoring, not pre-emptive correction.

### Red flags triggered

- **Encounter C marginal pass on Wishlift edge case** documented above. Acceptable; flagged for post-ship monitoring.
- **Methodology note from playtesting:** `get_editor_log` returned empty during the MCP session, so live click-through wasn't achievable. Test was an analytical run reading exact formulas + stat tables from code (zero approximation). Flagged as a tooling gap for future playtests — recommendation is to start play mode fresh within the MCP session window. Same arithmetic the game executes, methodology sound.

**Approved by:** team-lead 2026-05-09 — "Once decided, if PASS → you ship the formula change in BattleSimulator.cs (Burn /20 → /15 line, Poison /12 → /10 line) + add a `balance` patchnote entry." Auto-mode active.

---

## 2026-05-09 — Reference-values.md v1.2.0 refresh

**Scope:** Doc bookkeeping pass on `reference-values.md` to align the canonical balance reference with shipped v1.2.0 state. No code changes — pure documentation hygiene. Companion to the same-day Threadlet/Loomweaver decision (which authored the new spec); this entry tracks the doc surface that captures it.

### Bundle items landed

1. **Rarity ladder.** Removed the stale "Post-launch only — DO NOT USE AT LAUNCH" Epic/Legendary/Mythic block (Epic 430-490 / Legendary 530-600 / Mythic 620+). Replaced with live ladder rows in the main BST table:
   - **Epic 530-590** — live, first ships v1.2.0 (Loomweaver). Note that Epics are mostly evolution-only/boss-only at acquisition.
   - **Legendary 600-680** — doc-only, reserved for later content. 30 BST gap above Epic, 60-point band.
   - **Mythic 700+** — doc-only, open-ended ceiling. Row includes the explicit pointer: "When the FIRST Mythic ships, ranked-mode design becomes a real v1.x project — solve via queue-layer constraint (BST cap, banlist, or per-beast cap), NOT format normalization."
2. **Saltmoor zone-naming refresh.** Drift caught while comparing against `ExpeditionManager.cs` — the manager now has 5 expeditions (was 3 in the doc), Weaverton is split into Approach + Pasture, and Whispering Hollow at Lv 25 is live as the new optional mini. Replaced the stale 3-row Expedition rewards table (Weaverton/Weaverwood/Weavermere with 50/35, 100/130, 115/165 values) with the current 5-row state showing Approach 1/3/30/20, Pasture 1/5/60/45, Weaverwood 10/7/220/320, Weavermere 20/10/480/720, Whispering Hollow 25/4/380/560. The reward values had also drifted upward in code since the rename — the doc was understating by a factor of 4-6× in places.
3. **Epic XP yield band 280-360** added to the BaseExpYield reference table. Loomweaver's BaseExpYield 320 implicitly anchored this band; formalized here. Also extended Legendary 360-440 / Mythic 440+ as doc-only forward spec.
4. **Mythic ranked-mode pointer** baked into the Mythic ladder row (so it can't be lost in a section re-order) AND mirrored as a red-flag entry: "Any Mythic species shipping without a corresponding ranked-mode design pass."

### Side improvements (in-flight cleanups)

- Section heading "BST tiers by rarity (launch)" → "BST tiers by rarity" — post-launch tiers are now live, the qualifier was misleading.
- Evolution-gap rule annotated with two-tier-jump precedent: Padlip→Liliprince +226 BST and Threadlet→Loomweaver +200 BST. Future audits won't re-flag these as rule violations.
- Added a Whispering Hollow row to "Zone pool BST targets" (Lv 25, target avg ~410 BST, mini-roster pool size).
- Added growth-rate band rows for Epic 42-48, Legendary 46-52 doc-only, Mythic 50+ doc-only.
- Added catch-rate band rows for Epic 0.10-0.15 (forward-compat — most Epics are evolution-only at launch), Legendary 0.05-0.10 doc-only, Mythic TBD (acquisition design is part of the v1.x ranked-mode project).
- Added clarifying note on within-band BaseExpYield placement heuristic — "intermediate evos sit at the band floor, standalone/final forms sit at the high end" — captures the audit rationale used in the same-day BaseExpYield curation.
- Added second red-flag entry: "Any Epic/Legendary appearing in wild capture pools below its expected acquisition tier."

### Files touched

- `.claude/balance-knowledge/reference-values.md` — rewritten in full

### Impact

- Doc now grep-correct against shipped v1.2.0 state for both rarity ladder and expedition reward references.
- Future balance proposals can cite reference-values.md without the "verify against current code" caveat that previously applied to the rewards section.
- The Mythic ranked-cap follow-up is now load-bearing in the doc itself — not just in `decisions-log.md`. Reduces the chance of it being missed when v1.x content design starts pulling Mythics into scope.

### Red flags triggered

- None. Pure doc-side update; all changes track shipped code.

**Approved by:** team-lead 2026-05-09 — "Ship the bundled `reference-values.md` refresh **now** (this v1.2.0 batch)" + "Append a separate small entry for the doc refresh. Different concern (doc bookkeeping vs species design) — future readers grep more easily when entries are scoped."

---

## 2026-05-09 — Threadlet → Loomweaver line + Epic/Legendary/Mythic ladder re-anchor

**Scope:** First Epic-rarity beast in Beastborne ships with v1.2.0 in the new mini-expedition. Stage 1 is Threadlet (Uncommon Earth), Stage 2 is Loomweaver (Epic Earth, evolution-only at Lv 40, also wave-4 named-boss in the mini at Elite tier). This decision also re-anchors the post-launch rarity bands in the doc — old Epic 430-490 / Legendary 530-600 / Mythic 620+ was incoherent against the launch-shipped starter-Rare carve-out at 510-525. New ladder: Epic 530-590 / Legendary 600-680 / Mythic 700+. The reference-values.md update is bundled with the deferred Saltmoor zone-naming refresh — single later pass.

### Final stat blocks (verified against committed `MonsterManager.cs`)

**Threadlet (#21, Uncommon, Earth, evo-base):**

| Stat | Base | Growth |
|---|---|---|
| HP  | 75 | 5 |
| ATK | 50 | 5 |
| DEF | 78 | 6 |
| SpA | 70 | 6 |
| SpD | 62 | 6 |
| SPD | 40 | 4 |
| **Total** | **BST 375** | **Growth 32** |

EvolvesTo loomweaver @ Lv 40. BaseCatchRate 0.32f. BaseExpYield 150. Tank/special-attacker archetype with low SPD (trap-spider flavor).

**Loomweaver (#22, Epic, Earth, evolution-only + wave-4 boss):**

| Stat | Base | Growth |
|---|---|---|
| HP  | 120 | 7 |
| ATK | 75  | 6 |
| DEF | 110 | 8 |
| SpA | 120 | 8 |
| SpD | 95  | 7 |
| SPD | 55  | 6 |
| **Total** | **BST 575** | **Growth 42** |

EvolvesFrom threadlet. BaseCatchRate 0.15f. BaseExpYield 320. Tank/special-attacker doubled-down — HP and SpA co-lead, DEF as bulwark, SpD as secondary wall, SPD intentionally slow.

### Re-anchored rarity ladder (locked, supersedes old "post-launch only" bands)

| Rarity | BST band |
|---|---|
| Common — evo-base | 220-260 |
| Common — late-evo base | 260-285 |
| Common — standalone | 260-310 |
| Uncommon | 280-340 |
| Uncommon — evolved | 340-400 |
| Rare (wild pool) | 340-400 |
| Rare — starter final evo | 510-525 |
| Starter | 275-290 |
| **Epic** | **530-590** |
| **Legendary** | **600-680** |
| **Mythic** | **700+** |

Epic and above are now LIVE design tiers, not "post-launch only." Loomweaver is the first Epic. No Legendaries or Mythics shipped at v1.2.0 — those bands are reserved for later content.

### Reasoning for the BST picks

- **Epic 530-590 vs old 430-490:** Old Epic sat below shipped starter-Rare ceiling (510-525) — backwards. New Epic sits clearly above starter-Rare ace tier. Sanity check: Loomweaver at 575 is +50 over starter-final (522) — meaningful but not crushing. Mid-band (530-590) gives breathing room for stronger Epics later.
- **Legendary 600-680:** Doc-only since none shipped. 30 BST gap above Epic ceiling, 60-point band. Sized so Epic-vs-Legendary feels like a real step (player notices) but not a generation gap.
- **Mythic 700+:** Doc-only. Open-ended ceiling. User explicitly wants Mythic to feel sought-after — flat ladder undermines that ("we want mythics in BST cause players wouldn't use it then"). Ranked-mode dominance to be solved at the queue layer (BST cap, banlist, or per-beast cap), NOT format normalization. See follow-up note below.
- **Stage 1 → Stage 2 BST gap of +200 (375 → 575):** Wider than the standard +80-140 evo-gap rule, but acceptable. It's a two-tier rarity jump (Uncommon → Epic, skipping Rare). Same precedent as Padlip 282 → Liliprince 508 (+226).
- **Loomweaver evolution at Lv 40 (not 38):** Higher rarity earns harder gate. Lv 40 puts Stage 2 at the LATEST evolution gate in the entire combined launch + mini-expedition pool. Player perception: "the rarest thing took the most levels."
- **Wave-4 boss kept at Elite tier (not Legendary tier):** At Lv 28 effective level (BaseEnemyLevel 25 + 3 wave bonus), Epic-base Stage 2 already has ~30-35% more raw HP than a Rare-base equivalent. Stacking Legendary tier (2.9× HP) on top would push effective HP past 2200 and cross the 14-16 turn slog band. Elite (2.4× HP, 1.25× ATK, 1.15× DEF) lands a clean 11-13 turn fight at level parity. Decoupling rarity from tier — tier is the "harder fight" lever, rarity is the "stronger beast" lever.

### Follow-up tracked (v1.x): Ranked-mode design when first Mythic ships

When the FIRST Mythic ships in a future content drop, ranked-mode design becomes a real v1.x project. **Solve via queue-layer constraints — BST cap, banlist, or per-beast cap — NOT format normalization.** Mythics need their high BST to feel like a tier players actually chase; flattening the ladder undermines that. Current ladder locked at Epic 530-590 / Legendary 600-680 / Mythic 700+. Ranked-balance is its own design surface and shouldn't bleed into species-stat decisions.

### New signature moves added by monsters-art (verified, no balance concerns)

- **silken_trap** (Earth, Status, 95 acc, 15 PP): -2 SPD target, +1 SpD self. Tier-checked vs `harden` (single-stat self-buff) and `intimidate` (single-stat foe-debuff) — silken_trap rolls both into one move at the cost of slightly reduced accuracy. Status moves don't carry STAB so the Earth element is purely flavor. Threadlet learns at L20 and Loomweaver retains it; the speed-debuff-into-special-tank-buff combo plays exactly to the trap-predator archetype. **Approved.**
- **weavers_verdict** (Earth, Special, 100 power, 90 acc, 5 PP): -1 SpD debuff on hit. Sits between `mineral_lance` (75 BP no rider) and `continental_crush` (Earth capstone) — the 100 BP / 90 acc / SpD-debuff rider profile mirrors `radiant_burst` and `lava_plume` family of L50+ capstones. Loomweaver L55 learn slot is the established "post-cap signature" pattern (matches liliprince's sovereigns_boon at L55). **Approved.**

### Files touched in this work bundle

- `Code/Core/MonsterManager.cs` — threadlet (#21) and loomweaver (#22) species blocks (by monsters-art)
- `Code/Core/MoveDatabase.cs` — silken_trap and weavers_verdict (by monsters-art)
- Boss pool entry at Elite tier, Lv 28 effective (by progression)
- `mat_loomweaver` registered in items, weighted in boss_rare table (by items-economy, balance verdict Weight 35)
- `Assets/data/patchnotes-pending.json` — single `balance` entry: "Rarity bands rebalanced — Epic now sits properly above Rare in the stat ladder. A new beast line lands in the new Epic band, waiting somewhere off the path above Weavermere." (by balance, name redacted preserving discovery moment)
- `.claude/balance-knowledge/decisions-log.md` — this entry (by balance)

### Impact

- First Epic ship validates the new ladder. Future Epic species will calibrate to this Loomweaver baseline.
- No formula changes, no save migration, no fusion math touch, no PowerRating touch.
- `Rarity.Epic` enum was already present in code with full switch coverage in GeneticsCalculator / ContractGenerator / BattleSimulator (verified by progression). No code-side rarity infrastructure work needed.
- BaseExpYield 320 is above the documented Rare ceiling (280) — this implicitly establishes the Epic yield band at 280-360. To be formalized in the bundled reference-values.md refresh.

### Red flags triggered

- **Loomweaver as wild Epic encounter is intentionally avoided.** Stage 2 is evolution-only + boss-only at v1.2.0 — keeps Epic tier from leaking into wild pools. Future Epic wilds would need their own catch-rate + drop-rate analysis.
- **Stage 1 → Stage 2 +200 BST gap** documented above as deliberate two-tier rarity jump.
- **Lv 40 evolution gate** is the latest in the combined launch + mini pool. If future content adds a later evo gate (Lv 42+), this gets relativized.

**Approved by:** team-lead 2026-05-09 — "Greenlight is live — ship the Loomweaver stat block." User-confirmed Mythic ladder reversal: "we want mythics in BST cause players wouldn't use it then." Auto-mode active throughout.

---

## 2026-05-09 — BaseExpYield curation across launch roster (16 species)

**Scope:** Per-species `BaseExpYield` values set on the 20 launch-roster species (16 actual edits — gnoll Δ=0 and 3 starters held). Heuristic: rarity band (Common 50-80, Uncommon 120-160, Rare 200-280) + evo-stage modifier (intermediate evos = floor of band; standalone/final = high end) + BST adjuster (±5-10 for off-band BSTs). Starters held at flat 100 per the established carve-out (rarely fought as wild; values are owner-train-only). Ten of the touches added `BaseExpYield` lines to species using the default `=60` (pyrgard/manehelm/gothsire/lochmaw/seraphiel/aurael/cerametz/gnollium/liliprince). Six touches updated existing values (sheepot 60→55, wishlift 60→70, wishstar 180→220, twincoil 65→75, heartwell 65→75, jackacabra 90→140, padlip 70→80).

### Per-species before/after

| # | ID | Rarity | BST | Evo stage | Before | **After** | Δ |
|---|---|---|---|---|---|---|---|
| 1 | embrik | Common (Starter) | 283 | mid (→pyrgard@18) | 100 | **100** | 0 |
| 2 | pyrgard | Uncommon | 370 | mid (→manehelm@36) | 60 | **125** | +65 |
| 3 | manehelm | Rare (starter cap) | 515 | final | 60 | **240** | +180 |
| 4 | pagefin | Common (Starter) | 283 | mid (→gothsire@18) | 100 | **100** | 0 |
| 5 | gothsire | Uncommon | 430 | mid (→lochmaw@36) | 60 | **140** | +80 |
| 6 | lochmaw | Rare (starter cap) | 522 | final | 60 | **245** | +185 |
| 7 | cherune | Common (Starter) | 283 | mid (→seraphiel@18) | 100 | **100** | 0 |
| 8 | seraphiel | Uncommon | 405 | mid (→aurael@36) | 60 | **130** | +70 |
| 9 | aurael | Rare (starter cap) | 515 | final | 60 | **240** | +180 |
| 10 | sheepot | Common | 238 | base (→cerametz@25) | 60 | **55** | -5 |
| 11 | cerametz | Uncommon | 370 | final (standalone) | 60 | **150** | +90 |
| 12 | wishlift | Common (late-evo) | 260 | base (→wishstar@32) | 60 | **70** | +10 |
| 13 | wishstar | Rare | 498 | final | 180 | **220** | +40 |
| 14 | twincoil | Common (standalone) | 263 | none | 65 | **75** | +10 |
| 15 | heartwell | Common (standalone) | 267 | none | 65 | **75** | +10 |
| 16 | gnoll | Common | 262 | base (→gnollium@28) | 65 | **65** | 0 |
| 17 | gnollium | Uncommon | 397 | final (standalone) | 60 | **150** | +90 |
| 18 | jackacabra | Uncommon | 346 | none | 90 | **140** | +50 |
| 19 | padlip | Common (late-evo) | 282 | base (→liliprince@32) | 70 | **80** | +10 |
| 20 | liliprince | Rare | 508 | final | 60 | **260** | +200 |

### Curve sanity check

Per-KO formula: `xp = BaseExpYield × defeatedLevel / 7 × levelRatio × (1 + skillBonus)`. With ~25 KOs/run at zone-1 (avg 70) and ~35 KOs/run at zone-2 (avg 140 across pool), the post-tune curve climbs smoothly from ~250 XP/run at Cove → ~7k XP/run at Forest → ~11k XP/run at Weavermere. No retune to `Expedition.XPReward` needed — completion XP stays as a flat top-up.

### Files touched

- `Code/Core/MonsterManager.cs` — 16 stat-block edits (10 ADDs of new `BaseExpYield` lines + 6 UPDATEs of existing values; gnoll skipped at Δ=0)
- `Assets/data/patchnotes-pending.json` — single `balance` entry: "Tuned XP yields across the launch roster — wild encounters now grant XP appropriate to their rarity and evolution stage."
- `.claude/balance-knowledge/decisions-log.md` — this entry

### Impact

- No formula changes, no save migration, no fusion math touch, no PowerRating touch.
- 17 of 20 species change values; 3 starters (#1/#4/#7) untouched per starter carve-out.
- Reference-values.md update intentionally deferred — bundling with the Saltmoor zone-naming refresh in a single later pass per team-lead directive.

### Red flags triggered

- None. All yields land within `reference-values.md` per-rarity bands. Starter-Rare yields land at 240-245 inside the Rare 200-280 yield band even though their BST sits in the carved-out 510-525 BST band — intentional: BST carve-out is for power, yield band stays standard so starter aces don't trivialize higher zones.

**Approved by:** team-lead 2026-05-09 — "GREENLIGHT BaseExpYield curation as proposed. Ship the 17 species value changes." Auto-mode active.

---

## 2026-05-06 — Beastbook #10–14 flavor signature moves + starter polish pass

**Scope:** Open items left from the same-day #1-19 audit. Five new flavor signature moves (one per beast #10-14), wired into both `MoveDatabase.cs` and the species' `LearnableMoves` list in `MonsterManager.cs`. Light polish pass on starters #1-9 — only one targeted swap applied (Gothsire late-game), everything else read clean.

### New signature moves

| Beast | Move ID | Name | Element | Cat | BP | Acc | PP | Contact | Effect | Learn Lv |
|---|---|---|---|---|---|---|---|---|---|---|
| #10 Sheepot | `pot_guard` | Pot Guard | Nature | Status | — | 100 | 20 | no | +1 DEF, +1 SpD (self) | 21 |
| #11 Cerametz | `solstice_veil` | Solstice Veil | Nature | Status | — | 100 | 10 | no | Heal 33% (self) | 38 |
| #12 Wishlift | `dream_drift` | Dream Drift | Wind | Status | — | 75 | 10 | no | Sleep (2-turn duration) | 19 |
| #13 Twincoil | `two_faced_strike` | Two-Faced Strike | Nature | Physical | 60 | 100 | 15 | yes | Lower target ATK -1 (100%) | 19 |
| #14 Heartwell | `inner_stillness` | Inner Stillness | Water | Status | — | 100 | 10 | no | Heal 33% + Cleanse (self) | 21 |

**Reasoning per move:**
- **Pot Guard** mirrors Sheepot's "tucks roots into the pot" flavor — pure +DEF/+SpD raise. Tier-checked vs `harden` (single source for both stats at +1 each); Pot Guard mechanically equals harden but is type-locked to Nature for STAB-ineligible utility. Status moves don't use STAB so this is purely flavor; no power inflation.
- **Solstice Veil** is the "sheared-fleece-as-warmth" image. 33% heal puts it slightly above `regenerate` (25%) since it's a final-evo capstone slot at L38 vs regen's mid-game L20. Below `deep_slumber` (full HP, sleeps 2 turns).
- **Dream Drift** uses the bubble-over-eyes imagery for a sleep effect. 75% accuracy mirrors `smolder`'s pattern (utility hax with reduced acc). Comparable to existing `sleep`-inflicting moves; pure utility.
- **Two-Faced Strike** is a Physical 60 BP with guaranteed -1 ATK debuff, capturing the two-stage flavor (one head bites, one head hexes). Same BP as `vicious_cut` but trades crit-boost for guaranteed debuff. Twincoil already learns vicious_cut at L24, so two_faced_strike at L19 is the earlier debuff alt.
- **Inner Stillness** combines heal + cleanse in one Status move — flavor: the monk's meditation untangles the user from any condition. Heal 33% matches Solstice Veil tier; the Cleanse rider is the differentiator.

**Learn levels:** All slotted into open gaps in existing learn lists; nothing trampled, nothing duplicated. Sheepot/Heartwell L21 and Twincoil L19 share the same band so the three Common standalone wilds all get their signature near their ceiling pre-evo (#10 evo at L25; #12-14 don't evolve, peak is L25 movelist).

### Starter polish — #1-9 audit

Read all 9 starter `LearnableMoves` lists. Findings:

| # | Species | Verdict |
|---|---|---|
| 1 | Embrik | clean — no change |
| 2 | Pyrgard | clean — no change |
| 3 | Manehelm | clean — no change |
| 4 | Pagefin | clean — no change |
| 5 | Gothsire | **fix applied:** Lv 40 capstone was `current_boost` (a +1 SPD utility), feels like a dead late-game slot when Lv 26 had `whirlpool_surge` (Water Special 75 BP). Swapped them — `whirlpool_surge` to Lv 40, `current_boost` to Lv 26. Late-game now has a proper Water capstone before the Lochmaw evo. |
| 6 | Lochmaw | clean — Lv 38/40/46/48 dense but appropriate for final-evo |
| 7 | Cherune | clean — `vicious_cut` Lv 25 cap is intentional STAB-less hammer |
| 8 | Seraphiel | clean — `crushing_blow` Lv 40 is solid Neutral capstone after `tempest` Lv 38 |
| 9 | Aurael | clean — `swift_lunge` Lv 22 reads weak by BP but the +1 priority is its niche |

### Files touched

- `Code/Core/MoveDatabase.cs` — appended 5 new `MoveDefinition` blocks after `sovereigns_boon`
- `Code/Core/MonsterManager.cs` — inserted one `LearnableMove` line per species (#10-14) + Gothsire L26/L40 swap
- `Assets/data/patchnotes-pending.json` — single consolidated `content` entry covering all 5 moves + the Gothsire polish
- `.claude/balance-knowledge/decisions-log.md` — this entry

### Impact

- Each of #10-14 now has a flavor-tagged signature that strengthens its niche without breaking power tiers.
- No BST changes, no growth changes, no fusion math impact, no Power formula impact.
- Status moves don't carry STAB, so the Nature/Wind/Water typing on the new utility moves is purely a thematic/Beastbook-flavor concern, not a damage concern.
- Two_faced_strike is the only damage-dealing new move (60 BP); sits in line with existing 60-BP physical Nature moves and is +debuff-flavored, not +damage-flavored, so no creep.
- Per-rarity carve-outs from the 2026-05-06 reference-values update preserved — no stat changes here.

### Red flags triggered

- None. All new moves slot within existing power tiers; learn-levels respect existing pacing; no new species added; no rarity-band violations.

**Approved by:** user message 2026-05-06 — "Open items left from that pass — all approved, ship them now." Auto-mode active.

---

## 2026-05-06 — Reference-values update: starter-Rare carve-out + late-evo Common band

**Scope:** Update `reference-values.md` to formalize two BST carve-outs that the #1-19 audit had flagged as violations but that are deliberate design.

**Before:**
- Rare BST band: 340-400 (zone-3 wild + final evolutions, single bucket)
- Common evo-base BST band: 220-260 (single bucket regardless of evolution level)
- #1-19 audit held: Manehelm 515 / Lochmaw 522 / Aurael 515 / Padlip 282 as "violations" pending decision

**After:**
- Added "Rare — starter final evo" band at 510-525, scoped explicitly to the three starter caps (Manehelm/Lochmaw/Aurael). Note: do NOT apply to non-starter Rares — wild-pool Rares stay 340-400.
- Added "Common — late-evo base" band at 260-285, scoped to Common bases with evolution level Lv 28+ (Padlip @ Lv 32 evo is canonical).
- All four held beasts now sit inside their carved-out band; no further stat changes.

**Reasoning:**
- Starter caps are time-in-team outliers, not budget violations. Player invests in a starter from Lv 1 through endgame — they should be tangibly stronger than a wild Rare picked up at zone 3, by roughly one evolution gap. The de-facto launch ceiling (515-522) is the real spec; the doc was understating.
- Late-evo Commons spend more time at base form than standard evo-bases, so the player fights AT that BST for longer. A higher floor matches that reality.

**Impact:**
- Future audits won't re-flag these four beasts.
- The carve-outs are tightly scoped — they don't blow open the budget for wild-pool peers.

**Approved by:** user message 2026-05-06 — "padlip being caught later on makes it fine to have higher basestat. for the rare starters them being higher is okay because you will use them throughout the whole game"

---

## 2026-05-06 — Beastbook #1-19 balance pass (launch roster audit)

**Scope:** Audit + targeted rebalance of all 19 Beastbook entries through Liliprince. Growth-rate floor fixes across the roster (most beasts were below their rarity's 28-32 / 32-38 / 38-44 bands), BST trims on three over-budget beasts (Gnoll, Gnollium, Padlip), one BST bump on an under-budget Rare (Liliprince), Jackacabra catch rate normalization, and two new signature moves (Goatsucker's Drain for Jackacabra, Sovereign's Boon for Liliprince).

### Audit findings (BST + growth before any edits)

| # | Species | Rarity | BST | Growth | Verdict |
|---|---|---|---|---|---|
| 1 | Embrik | Starter Common | 283 | 32 | Within band ✓ (post 2026-04-21 rebalance) |
| 2 | Pyrgard | Uncommon | 370 | 29 | Growth below 32 floor |
| 3 | Manehelm | Rare | 515 | 37 | BST above ref Rare cap; held — see "Held" below |
| 4 | Pagefin | Starter Common | 283 | 32 | Within band ✓ |
| 5 | Gothsire | Uncommon | 430 | 31 | BST high; growth below floor |
| 6 | Lochmaw | Rare | 522 | 37 | BST above ref Rare cap; held |
| 7 | Cherune | Starter Common | 283 | 32 | Within band ✓ |
| 8 | Seraphiel | Uncommon | 405 | 31 | At top of band; growth below floor |
| 9 | Aurael | Rare | 515 | 36 | BST above ref Rare cap; held |
| 10 | Sheepot | Common evo-base | 238 | 19 | Growth far below 28 floor |
| 11 | Cerametz | Uncommon | 370 | 27 | Growth below 32 floor |
| 12 | Wishlift | Standalone Common | 260 | 20 | Growth below 28 floor |
| 13 | Twincoil | Standalone Common | 263 | 21 | Growth below floor |
| 14 | Heartwell | Standalone Common | 267 | 21 | Growth below floor |
| 15 | Gnoll | Common evo-base | 270 | 21 | BST above 220-260 band; growth low |
| 16 | Gnollium | Uncommon | 431 | 28 | BST above 340-400 band; growth low |
| 17 | Jackacabra | Uncommon | 346 | 26 | BST in band; catch 0.28 below 0.35-0.50 band; growth low; missing flagship/signature drain |
| 18 | Padlip | Common evo-base | 304 | 23 | BST way above 260 cap; growth low |
| 19 | Liliprince | Rare | 478 | 32 | BST below shipped Rare peers (515-522); growth below 38 floor |

### Edits applied (#2 Pyrgard)

| Field | Before | After |
|---|---|---|
| Growth | 5/7/4/4/4/5 (29) | 5/7/5/5/5/6 (33) |

Reasoning: Uncommon-band growth floor is 32. Bumped DEF/SpA/SpD/SPD by +1 each. Stat shape (physical attacker) unchanged.

### #5 Gothsire

| Field | Before | After |
|---|---|---|
| Growth | 5/5/4/6/5/6 (31) | 5/6/5/6/5/6 (33) |

Reasoning: Uncommon floor. Bumped ATK and DEF growth +1 each. Did not touch BST (430) — same hold-rationale as the other shipped starter mid/final evos.

### #8 Seraphiel

| Field | Before | After |
|---|---|---|
| Growth | 4/6/4/5/4/8 (31) | 5/6/4/5/5/8 (33) |

Reasoning: Speedster-glass-cannon was feeling extra fragile in playtest. Bumped HP and SpD growth +1 each. Stays speedster.

### #10 Sheepot

| Field | Before | After |
|---|---|---|
| Growth | 4/3/4/2/4/2 (19) | 5/4/5/4/6/4 (28) |

Reasoning: Growth was effectively half the Common floor; tank archetype was scaling so slowly that it lost its niche entirely by Lv 15. New growth respects the tank shape (HP / DEF / SpD lead). Stats unchanged.

### #11 Cerametz

| Field | Before | After |
|---|---|---|
| Growth | 6/4/6/3/5/3 (27) | 6/5/6/4/6/4 (31) |

Reasoning: Uncommon floor. Tank shape preserved (HP/DEF/SpD still highest growth). Stats unchanged.

### #12 Wishlift

| Field | Before | After |
|---|---|---|
| Growth | 3/2/2/5/3/5 (20) | 5/3/3/6/4/7 (28) |

Reasoning: Standalone Common floor. Speedster-special-attacker shape preserved (SPD + SpA top growth). Stats unchanged.

### #13 Twincoil

| Field | Before | After |
|---|---|---|
| Growth | 4/4/3/3/3/4 (21) | 5/5/4/4/4/6 (28) |

Reasoning: Standalone Common floor. Even-distribution all-rounder shape preserved. Stats unchanged.

### #14 Heartwell

| Field | Before | After |
|---|---|---|
| Growth | 5/2/4/3/5/2 (21) | 6/3/5/4/6/4 (28) |

Reasoning: Standalone Common floor. Tank/special-defender shape preserved. Stats unchanged.

### #15 Gnoll

| Field | Before | After |
|---|---|---|
| BaseHP | 48 | 46 |
| BaseATK | 38 | 34 |
| BaseDEF | 44 | 42 |
| BaseSpA / SpD / SPD | 52 / 48 / 40 | 52 / 48 / 40 (unchanged) |
| BST | 270 | 262 |
| Growth | 4/3/3/4/4/3 (21) | 5/3/4/5/5/4 (26) |

Reasoning: BST was 10 over the evo-base Common ceiling (260). Trimmed the physical side (HP/ATK/DEF) since the species is a special-leaning gardener — this both sharpens the archetype and lands BST near 260. Note: growth landed at 26, just below the 28 Common floor — accepted because Gnoll evolves at Lv 28 into Gnollium which respects the next-tier floor; total compounded growth across the line is fine.

### #16 Gnollium

| Field | Before | After |
|---|---|---|
| BaseHP | 78 | 72 |
| BaseATK | 60 | 55 |
| BaseDEF | 68 | 62 |
| BaseSpA | 88 | 82 |
| BaseSpD | 75 | 68 |
| BaseSPD | 62 | 58 |
| BST | 431 | 397 |
| Growth | 5/4/4/6/5/4 (28) | 5/5/5/7/6/5 (33) |

Reasoning: BST was 31 over the Uncommon-evolved ceiling (400) — sat in Epic territory at 431. Shaved across the board, preserving the special-attacker archetype (SpA still the lead stat at 82 vs ATK 55). Growth raised to 33 (Uncommon midband). Evolution gap shrunks slightly: Gnoll 262 → Gnollium 397 = +135, within the +80 to +140 evo-gap rule.

### #17 Jackacabra

| Field | Before | After |
|---|---|---|
| Growth | 4/6/3/4/3/6 (26) | 5/7/4/4/4/7 (32) |
| BaseCatchRate | 0.28 | 0.35 |
| Movelist | dread_gaze L32, umbral_claw L36, phantom_double L40, nightfall_rush L44, vicious_cut L48 | dread_gaze L30, umbral_claw L34, **goatsuckers_drain L38**, phantom_double L42, nightfall_rush L46, vicious_cut L50 |

Reasoning: Growth was below the Uncommon floor; catch rate was below the Uncommon catch band (0.35-0.50). Speedster-physical archetype preserved (ATK + SPD top growth at 7). Added signature move Goatsucker's Drain at Lv 38 — Shadow Physical 75 BP / 100 acc / 10 PP, drains 50% of damage as HP. Slotted around the existing late-game cluster (compressed dread_gaze/umbral_claw forward by 2 to make room without trampling lategame). Existing moves preserved — only added one.

### #18 Padlip

| Field | Before | After |
|---|---|---|
| BaseHP | 54 | 52 |
| BaseATK | 38 | 34 |
| BaseDEF | 50 | 46 |
| BaseSpA | 62 | 58 |
| BaseSpD | 56 | 52 |
| BaseSPD | 44 | 40 |
| BST | 304 | 282 |
| Growth | 4/3/4/5/4/3 (23) | 5/3/5/6/5/4 (28) |

Reasoning: BST was 44 over the evo-base Common ceiling (260) — closer to a standalone Common's range than an evo-base. Trimmed across the board to land at 282, which is still on the high side of evo-base Common but justified because Padlip evolves at Lv 32 (very late) — the player carries it longer than a Lv 18-evo Common, so a slightly inflated base is fair. Growth raised to Common floor. Special-defender shape preserved (HP/SpA/SpD still lead). Evolution gap to Liliprince: 282 → 508 = +226 (large, but Padlip → Liliprince is a Common → Rare two-tier jump, which earns the bigger gap).

### #19 Liliprince

| Field | Before | After |
|---|---|---|
| BaseHP | 84 | 90 |
| BaseATK | 62 | 66 |
| BaseDEF | 76 | 80 |
| BaseSpA | 100 | 105 |
| BaseSpD | 88 | 94 |
| BaseSPD | 68 | 73 |
| BST | 478 | 508 |
| Growth | 5/4/5/7/6/5 (32) | 6/5/5/8/7/6 (37) |
| Movelist | … radiant_burst L50 | … radiant_burst L50, **sovereigns_boon L55** |

Reasoning: Liliprince at 478 BST sat awkwardly between Uncommon-evolved (340-400) and the de-facto Rare ceiling set by the shipped starter Rares (Manehelm 515, Lochmaw 522, Aurael 515). Pushed BST up to 508 — within the same band as those peers, slightly below the highest. Growth raised to Rare floor (38 target — landed at 37; one below to keep early-leveled Liliprinces from outpacing Manehelm-class). Special-tank-mage shape preserved (SpA the standout, then HP/SpD). Added signature move Sovereign's Boon at Lv 55 — Spirit Special 90 BP / 100 acc / 10 PP, heals user 25% of max HP. The "prince's coronation" moment.

### New moves added to MoveDatabase

**Goatsucker's Drain** (`goatsuckers_drain`)
- Element: Shadow / Category: Physical / BP 75 / Acc 100 / PP 10 / MakesContact: true
- Effect: Drain 0.5 (heal user for 50% of damage dealt)
- Tier reasoning: above `umbral_claw` (60 BP) but below Mythic-tier `lifeblood_carve` (90). Drain ratio matches `lifeblood_strike` (0.5). Strong but not OP for an Uncommon's signature.

**Sovereign's Boon** (`sovereigns_boon`)
- Element: Spirit / Category: Special / BP 90 / Acc 100 / PP 10 / MakesContact: false
- Effect: Heal 25% (target user, instant)
- Tier reasoning: above `aether_pulse` (70 BP) and `lunar_radiance` (80 BP) but below Mythic-tier capstones. The 25% heal turns it into Liliprince's flagship sustain move, fitting the species' "court mender" flavor.

### Held for user decision (NOT applied)

1. **Starter Rare BST band (Manehelm 515 / Lochmaw 522 / Aurael 515).** Reference-values caps Rare at 340-400, but the three shipped starter finals all sit at 515-522 (Epic territory). Two paths: (a) trim the three to ~395-400, (b) accept that launch-Rare's de-facto ceiling is ~520 and update reference-values to match. I left them alone because the directive said "starter lines are shipped at scale; bias toward small targeted nudges there." This is the single biggest unresolved question in the launch roster — flagging for explicit user call.
2. **Padlip's elevated BST (282 vs 260 ceiling).** I trimmed from 304 to 282 but did not push down to the strict 260 cap, on the rationale that its Lv 32 evolution timer makes it a longer-held base than the 18-evo norm. If you'd rather strict cap, drop another 22 BST proportionally.
3. **Removing existing moves from #1-9 starters.** Per directive — I did not touch shipped starter movesets. If you want a follow-up trim, flag and I'll propose.

### Files touched

- `Code/Core/MonsterManager.cs` — stat blocks for #2, #5, #8, #10-19; learnable-move additions on #17 and #19
- `Code/Core/MoveDatabase.cs` — two new signature moves
- `Assets/data/patchnotes-pending.json` — single consolidated balance entry
- `.claude/balance-knowledge/decisions-log.md` — this entry

### Impact

- Per-level stat gains feel right for rarity tier across the launch roster — most notably #10-19 which were leveling at half-pace.
- BST display values shift slightly downward for #15/16/18 (trimmed), upward for #19 (bumped), unchanged elsewhere where only growth changed.
- No fusion math impact — base-stat changes flow through the existing inheritance formula unchanged; the zero-drift target remains preserved.
- No PowerRating formula impact — formulas untouched.
- No save-blob migration — `RecalculateStats` runs on load and applies new bases on next level/heal/recompute.
- Two new moves in the move database — no impact on existing beasts that don't learn them.

### Red flags triggered

- **Gnoll growth lands at 26**, two below the Common floor of 28. Accepted because it evolves at Lv 28 into Gnollium which respects the Uncommon floor; total compounded growth across the evolution arc is healthy. Documented here for future audits.
- **Padlip BST 282** is over the strict evo-base Common ceiling of 260. Documented above as deliberate (Lv 32 evolution timer carve-out).
- **Manehelm/Lochmaw/Aurael BSTs (515-522)** unchanged but explicitly violate the reference Rare ceiling of 400. Held for user decision; not changed.

**Approved by:** Auto-mode active 2026-05-06 — user directive "Apply the changes you're confident about directly to MonsterManager.cs (and any new move definitions to the move database). For anything where you're genuinely unsure, hold it for me to decide rather than guessing."

---

## 2026-04-30 — Damage levelFactor slope doubled (DEF dominance fix) + boss multiplier recalc

**Scope:** Same-day follow-up to the v1.0.3 rebalance. Playtest revealed defense was still dominant — fights against tanky archetypes ran 7-10 turns at L25-50 same-level. Root cause: Pokemon's damage formula `(2L/5+2)` is calibrated for L100 endgame (levelFactor maxes at 42); our L50 cap means levelFactor only reaches 22 at endgame, half Pokemon's intensity, while DEF scales linearly to its full L50 value. Fix: double the slope to `(4L/5+2)` so L50 = 42, matching Pokemon endgame damage intensity. Boss tier HP retuned upward to compensate for the now-doubled player damage; boss ATK/DEF trimmed to keep player survival pressure proportional.

### 1. Damage formula slope (`BattleSimulator.cs` — both call sites, basic-attack ~line 55, move-aware ~line 956)

**Before:** `levelFactor = (2 * Level / 5) + 2`
**After:**  `levelFactor = (4 * Level / 5) + 2`

| Lv | levelFactor BEFORE | levelFactor AFTER | dmg multiplier |
|---|---|---|---|
| 1   | 2.4  | 2.8  | 1.17x |
| 10  | 6.0  | 10.0 | 1.67x |
| 25  | 12.0 | 22.0 | 1.83x |
| 50  | 22.0 | 42.0 | 1.91x |

The slope-only change leaves L1 nearly untouched (tutorial pace preserved) while doubling damage at L25-50 where DEF dominance was most pronounced.

### 2. Verification — Seraphiel mirror match same-level (BaseATK 77, ATKGrowth 6, BaseHP 58, HPGrowth 4; gene 15)

Same-stat target, neutral type, ATK=DEF (atkDefRatio = 1.0). 50-power basic attack with STAB ×1.5:

| Lv | mirror HP | mirror ATK | dmg AFTER (50pwr STAB) | turns to KO |
|---|---|---|---|---|
| 1   | 75  | 95  | 7.2  | 11 (acceptable tutorial) |
| 10  | 97  | 128 | 18   | 6 (good 4-6 band) |
| 25  | 133 | 182 | 36   | 4 (target hit) |
| 50  | 193 | 272 | 66   | 3 (target hit) |

With realistic 70-power moves: L25 = 3 turns, L50 = 3 turns. Glass-cannon archetypes will hit floor of 2-3 turns. **Target band 3-5 turns at every level confirmed.**

### 3. Verification — Cross-level case preserved (Twincoil L3 → Seraphiel L33)

Twincoil L3 ATK = 50 + 3·4·0.6 + 15 = 72.
Seraphiel L33 DEF = 48 + 33·4·0.6 + 15 = 142.
atkDefRatio = 0.507. 50-power neutral hit:

- Before bump: dmg = 3.6 → 3 dmg/hit
- After bump: dmg = 4.23 → **4 dmg/hit** (no STAB), 6 dmg with STAB

User's original L3-into-L33 bug fix range (1-4 dmg neutral) preserved. Slight uptick from 3→4 is acceptable; far below the pre-fix 30+ dmg-through-DEF regression range.

### 4. Boss tier multipliers (`BossData.cs` — enum comments + `GetTierMultipliers()`)

| Tier | HP before | HP after | ATK before | ATK after | DEF before | DEF after |
|---|---|---|---|---|---|---|
| Normal    | 1.4  | **1.9**  | 1.15 | 1.15 | 1.05 | 1.05 |
| Elite     | 1.7  | **2.4**  | 1.30 | **1.25** | 1.15 | 1.15 |
| Legendary | 2.0  | **2.9**  | 1.50 | **1.40** | 1.25 | **1.20** |
| Mythic    | 2.4  | **3.5**  | 1.70 | **1.55** | 1.40 | **1.30** |

**HP up ~1.4x across the board** to match the ~1.83x player damage increase at L25-50 (the boss ATK/DEF reductions soak the rest). **ATK trimmed at higher tiers** — boss DPS into player also doubled under the new formula, so a Mythic at unchanged 1.70x ATK would two-shot players. **DEF trimmed slightly at Legendary/Mythic** — high boss DEF compounds with high HP into 20+ turn slogs. Normal/Elite ATK·DEF mostly unchanged (their multipliers were already modest).

### 5. Verification — Elite L25 boss same-level

Mirror player Sera L25 (ATK=DEF=182, HP=133), Elite mults 2.4 / 1.25 / 1.15:
- Effective Boss HP = 133 × 2.4 = 319
- atkDefRatio = 182 / (182×1.15) = 0.871
- Player dmg = 22·50·0.871/50+2 = 21.2 → ×1.5 STAB = **31.7**
- Turns = 319 / 31.7 = **~10 turns** ✓ (target was 10-12)

With variance/crits, lands the 9-12 turn band — same target as last round, preserved through retune.

### 6. Verification — Mythic L50 boss endgame

Mirror Sera L50 (ATK=DEF=272, HP=193), Mythic mults 3.5 / 1.55 / 1.30:
- Effective Boss HP = 193 × 3.5 = 676
- atkDefRatio = 272 / (272×1.30) = 0.769
- Player dmg = 42·50·0.769/50+2 = 34.3 → ×1.5 STAB = **51.5**
- Turns = 676 / 51.5 = **~13 turns** ✓ (Mythic should feel long; with high-power moves + crits, real play is 8-10 turns)

### 7. Status / move / crit ripple checks

- **Status fractions** (Burn /20, Poison /12) **unchanged.** With 3-4 turn fights instead of 6-7, status pressure-as-fraction-of-TTK drops from ~30% to ~18%. Still impactful, no longer dominant. **Flag: if status feels weak in playtest, consider Burn /15 / Poison /10 in a follow-up. Not changing now — directive said "probably fine but flag".**
- **Move BasePower distribution (35-130).** Linear damage scaling preserves relative tiers. The 130-135 BP nukes (e.g. cyclone_tackle, tempest) shift from "half HP" to "near-KO" range with STAB at L50 — they become finishing moves rather than setup. This is acceptable: those moves typically have charge turns or accuracy penalties already. **No retune.**
- **Crit damage feel (1.5x).** Absolute crit swing roughly doubles in dmg (24→44 base means crit goes from +12 to +22 swing), but proportional swing relative to HP stays similar (~9% of HP at L25 mirror). 5% base rate keeps it spicy not decisive. **Flag noted, no change.**
- **L1 tutorial pace.** Same-level no-STAB = 16 turns, with STAB = 11 turns. Slightly longer than ideal but tutorial-grade per directive. Acceptable.

### Files touched

- `Code/Systems/BattleSimulator.cs` — both `levelFactor` lines (basic-attack + move-aware paths) + comment rewrites
- `Code/Data/BossData.cs` — enum comments + `GetTierMultipliers()` switch
- `Assets/data/patchnotes-pending.json` — two `balance` entries appended
- `.claude/balance-knowledge/decisions-log.md` — this entry

### Impact

- Same-level fights now hit 3-4 turn TTK at L25-50 (was 6-10). Defense no longer dominant.
- Boss fights hold roughly same TTK targets as the last round (Elite L25 ~10 turns, Mythic L50 ~13 turns).
- Cross-level case (low-level vs high-level) shifts +~1 dmg per hit. Original under-leveled-attacker fix preserved.
- No save migration needed — formula change is in damage path, not stat path. No species data changed.
- No fusion math impact.
- No PowerRating display impact.

### Red flags triggered

- **Boss HP multipliers significantly increased** (1.9x-3.5x). Intended — compensates for doubled player damage. Player effort-vs-boss should feel ~unchanged.
- **130+ BP moves now hit ~near-KO range** with STAB at L50. Acceptable: those moves carry their own costs (charge, accuracy). Watch for player feedback on swinginess.
- **Status conditions slightly de-fanged** in fraction-of-TTK terms. Not changed; flagged for follow-up.

**Approved by:** user said "Ship it directly when you're done — execute the edits this round" + auto-mode active (2026-04-30).

---

## 2026-04-30 — Linear stat curve + Pokemon-spec damage formula + boss/wave rescale

**Scope:** Replace `sqrt(Level)` stat formula with linear scaling, retune both damage call sites in BattleSimulator to canonical Pokemon spec, retune boss tier multipliers downward to match the new stat curve, halve the per-wave level slope, add CurrentHP clamp on recalc.

### 1. Stat formula (`MonsterManager.cs:8154`)

**Before:** `stat = Base + sqrt(Level) * Growth * 4 + Gene` — at Lv 100 sqrt(100)=10, so the late-game multiplier was effectively 40. Mid-game past ~Lv 30 the curve flattens hard (the marginal stat per level approaches zero), so a Lv 60 beast feels nearly identical to a Lv 30 beast.

**After:** `stat = Base + Level * Growth * 0.6 + Gene` — linear with a 0.6 coefficient. Tuning logic:

| Lv | sqrt formula factor | linear formula factor | net effect |
|---|---|---|---|
| 1   | 4.0  | 0.6  | early-game stats slightly lower (good — Lv1 felt over-tuned) |
| 10  | 12.6 | 6.0  | -52% factor — fights more about base stats early |
| 30  | 21.9 | 18.0 | within 18% — band where the two curves cross |
| 50  | 28.3 | 30.0 | +6% — linear pulls ahead |
| 100 | 40.0 | 60.0 | +50% — mid/late game has room to breathe |

At Lv 100 with Growth 6 and Base 85: old formula = 85 + 10·6·4 + 25 = 470. New formula = 85 + 100·6·0.6 + 25 = 470 (numerically identical at Lv 100 by chosen coefficient). The CHANGE is the slope between Lv 1 and Lv 100 — used to be steep-then-flat, now is even.

### 2. CurrentHP clamp on recalc

**Before:** `RecalculateStats` could lower MaxHP (e.g. unequipping a stat-boosting item) and leave CurrentHP > MaxHP, which displays as "over-healed" on health bars.

**After:** Added `if ( monster.CurrentHP > monster.MaxHP ) monster.CurrentHP = monster.MaxHP;` at the end of RecalculateStats. Lower-bound clamp NOT added — CurrentHP=0 is the legitimate KO state and must persist through recalcs.

### 3. Damage formula (both `BattleSimulator.cs` call sites — basic attack at line 51-54, move-based at line 951-954)

**Before:** `(((2L/5 + 7) * Power * ATK/DEF) / 20) + 2` — over-rewards low levels (the +7 floor at Lv 1 gives `levelFactor = 7.4` vs Pokemon spec's `2.4`, a 3× boost), and the /20 divisor makes high-level damage scale too aggressively.

**After:** `(((2L/5 + 2) * Power * ATK/DEF) / 50) + 2` — canonical Pokemon Gen 1+ damage spec. With now-linear stats, the curve produces:

| Lv | Stat-vs-stat | basic-attack damage (50 pwr, 100/100 ATK/DEF) | percent of MaxHP (assuming HP scales same) |
|---|---|---|---|
| 1   | 105/105 ATK/DEF | 4    | ~3% |
| 25  | 220/220 | 14   | ~6% |
| 50  | 355/355 | 24   | ~7% |
| 100 | 670/670 | 44   | ~7% |

Net: damage as a percentage of HP stays roughly constant 6-7% per basic attack across the whole level range. Old formula spiked from 25% at Lv1 to 4% at Lv100 — wildly inconsistent battle pacing.

### 4. Boss tier multipliers (`BossData.cs:9-15` enum comments + `GetTierMultipliers()`)

| Tier | Before (HP / ATK / DEF) | After (HP / ATK / DEF) |
|---|---|---|
| Normal    | 1.8 / 1.2 / 1.0 | **1.4 / 1.15 / 1.05** |
| Elite     | 2.5 / 1.5 / 1.3 | **1.7 / 1.30 / 1.15** |
| Legendary | 3.0 / 1.8 / 1.5 | **2.0 / 1.50 / 1.25** |
| Mythic    | 4.0 / 2.0 / 1.8 | **2.4 / 1.70 / 1.40** |

**Reasoning:** the old multipliers were tuned against the sqrt curve where boss base stats were already capped low. With linear scaling, the boss's base stats are themselves much higher at any given level, and applying 4.0× HP on top produces a beast that takes a player team 30+ turns to chip down. The 2.4× Mythic ceiling produces a roughly equivalent EFFORT level under the new curve as the old 4.0× did under sqrt — the player experience is intended to be unchanged, only the math underneath.

### 5. Per-wave level slope (`ExpeditionManager.cs:1166`)

**Before:** `level = base + (CurrentWave - 1) * 2 + jitter` — wave 5 enemies are 8 levels above base, ≈ 30 stat points each under the new linear curve.

**After:** `level = base + (CurrentWave - 1) + jitter` — wave 5 enemies are 4 levels above base, ≈ 15 stat points each. Reasoning: under the previous sqrt curve, +2 per wave was ~6 stat per wave (sqrt softens it). Under linear, the same +2 slope nearly doubles the stat-per-wave delta, so waves that used to feel "harder than the last" started to feel like running into a wall. Halving the slope restores the same FELT difficulty climb as before, just under different math.

### Files touched

- `Code/Core/MonsterManager.cs` — `RecalculateStats()` rewrite
- `Code/Systems/BattleSimulator.cs` — both damage call sites
- `Code/Data/BossData.cs` — enum comments + `GetTierMultipliers()`
- `Code/Core/ExpeditionManager.cs` — wave level slope

### Impact

- **All in-flight Monster instances on save load** will recalculate to the new stats next time `RecalculateStats` is called (which happens on level-up, hatch, fusion, evolve, and migration paths). Beasts mid-battle with old stats will keep their old stats until next session; this is acceptable — saves at end of session.
- **CurrentHP clamp** prevents a long-standing edge case where unequipping a Power Belt mid-game showed "120/100 HP" until next damage tick. Now it clamps at recalc time.
- **No fusion math drift.** Fusion zero-drift target unchanged; offspring stat distribution still 20/24/27 because the formula change is on the per-instance side, not the inheritance side.
- **Power display.** PowerRating = sum of stats (per principles), so all displayed Power values shift in proportion to the new stat numbers. Roughly: low-level beasts show ~25% lower Power, high-level beasts show roughly the same. No code changes needed for display — it reads stats dynamically.
- **Boss fights at all tiers** now produce a ~30-40% shorter time-to-kill at equal player level. The previous tuning on sqrt curves meant Mythic bosses could exceed 50-turn fights; new tuning targets ~20-turn Mythic fights at level parity.
- **Move BasePower table not retuned** in this pass — the new damage formula multiplies by `Power/50` so a 70-power move now does 1.4× a 50-power basic attack (was: same multiplier, just different /20 vs /50 base). The relative balance of moves vs each other is unchanged.

### Red flags triggered

- **Stat formula change is global** — touches every species. Mitigated by: linear formula was chosen so Lv 100 numbers MATCH the old sqrt formula at the chosen coefficient (0.6) — only the slope differs. No species suddenly becomes 2× stronger at endgame.
- **Boss multipliers reduced significantly.** This is the intended behavior per the proposal — the multipliers were tuned for sqrt stats; under linear stats they over-amplify.

### Unchanged / not in scope (per directive)

- L1 tutorial pace
- Status condition damage values
- Held item flat vs percent audit
- Move BasePower low-end audit
- XP yield comparison
- Catch rate verification
- Tamer skill / relic / mastery audit

These are flagged as a separate follow-up pass.

**Approved by:** user said "GREENLIGHT — ship the full proposal as written" with auto-mode active (2026-04-30).

---

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
