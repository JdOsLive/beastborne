# Balance Decisions — Summary (always read)
Condensed final state of every logged balance decision; bands, formulas and targets already in `principles.md` / `power-formula.md` / `reference-values.md` are NOT repeated here.
Full reasoning for any entry: grep `decisions-log.md` for the date or title.

## Settled decisions — do not reopen without the user

### Formulas & combat
- 2026-04-21 — PowerRating = sum of current stats, BaseStatTotal = sum of base stats (see `power-formula.md`); old HP/10, SPD/2, Level×5 removed.
- 2026-04-30 — Stat formula linear: `Base + Level·Growth·0.6 + Gene` (replaced `sqrt(L)·Growth·4`); RecalculateStats clamps CurrentHP ≤ MaxHP (no lower clamp).
- 2026-04-30 — Damage = `((levelFactor·Power·ATK/DEF)/50)+2`, levelFactor `4L/5+2` (L50 = 42). Supersedes same-day `2L/5+2`. Must change at BOTH BattleSimulator call sites (basic-attack + move-aware).
- 2026-04-30 — Boss tier HP/ATK/DEF: Normal 1.9/1.15/1.05 · Elite 2.4/1.25/1.15 · Legendary 2.9/1.40/1.20 · Mythic 3.5/1.55/1.30 (supersedes same-day 1.4…2.4 set). Targets: same-level TTK 3-5 turns; Elite L25 ~10 turns; Mythic L50 ~13.
- 2026-04-30 — Wave level slope `base + (wave-1) + jitter` (was ×2).
- 2026-05-09 — Status ticks: Burn MaxHP/15 (was /20), Poison MaxHP/10 (was /12). Playtest PASS; status share 53-63% of TTK.
- 2026-04-30 — Crit 1.5× and 130+ BP nukes left unchanged (near-KO at L50 accepted).

### Rarity / BST tiers
- 2026-05-06 — Starter-final Rare carve-out 510-525 (Manehelm 515 / Lochmaw 522 / Aurael 515) and Common late-evo base 260-285 (Padlip 282) accepted — do not re-flag.
- 2026-05-09 — Ladder re-anchored Epic 530-590 / Legendary 600-680 / Mythic 700+ (supersedes old Epic 430-490 / Leg 530-600 / Mythic 620+). User: Mythics keep high BST; ranked handled at queue layer.
- 2026-05-09 — Two-tier rarity evo jumps may exceed +140: Padlip→Liliprince +226, Threadlet→Loomweaver +200.
- 2026-05-13 — Dewdrop growth 21 (BST 290, 45/38/45/58/48/56) is a deliberate dual-type tax. Precedent: future dual-type Commons pay via under-band growth, not BST.
- 2026-05-06 — Gnoll growth 26 (below Common 28) accepted — evolves L28 into Gnollium.
- 2026-05-13 — Staiju BST 320 (+10 over standalone ceiling) accepted as Option B; paid for by priority `skyfall_pounce`. Revisit only when Staiju gets a map slot.

### Starters
- 2026-04-21 — Embrik / Pagefin / Cherune all BST 283, growth 32; archetypes: Embrik physical, Pagefin bulky special-defender, Cherune speedster.
- 2026-05-06 — Mid-evo growth → 33: Pyrgard 5/7/5/5/5/6, Gothsire 5/6/5/6/5/6, Seraphiel 5/6/4/5/5/8. Gothsire BST 430 held unchanged (see Open).
- 2026-05-06 — Gothsire learnset: `whirlpool_surge` L40, `current_boost` L26 (swapped).

### Per-species stat blocks (final)
- 2026-05-06 — Growth floors: Sheepot 28 (5/4/5/4/6/4), Cerametz 31 (6/5/6/4/6/4), Wishlift 28 (5/3/3/6/4/7).
- 2026-05-06 — Gnoll BST 262 (46/34/42/52/48/40). Gnollium BST 397 (72/55/62/82/68/58), growth 33 (5/5/5/7/6/5).
- 2026-05-06 — Jackacabra growth 32 (5/7/4/4/4/7), catch 0.28→0.35.
- 2026-05-06 — Padlip BST 282 (52/34/46/58/52/40), growth 28. Liliprince BST 508 (90/66/80/105/94/73), growth 37.
- 2026-05-13 — Twincoil BST 263→290 (50/56/46/42/45/51), growth 30 (5/5/5/4/5/6). Heartwell 267→297 (65/30/52/52/68/30), growth 30 (6/3/5/5/7/4). Supersedes 2026-05-06 growth-28 values.
- 2026-05-13 — Stomplet BST 382→330 (70/58/65/45/52/40), growth 32, evo L32, stays in Weaverwood (Option A). Skunkape 497→470 (100/85/90/68/72/55), growth 40. Gap +140.
- 2026-05-09 — Threadlet (#21 Uncommon Earth) BST 375 (75/50/78/70/62/40), growth 32, catch 0.32, yield 150, evo L40. Loomweaver (#22, first Epic) BST 575 (120/75/110/120/95/55), growth 42, catch 0.15, yield 320; evo-only + wave-4 Elite boss (Lv 28 effective). `mat_loomweaver` weight 35 in boss_rare.

### Evolution lines
- 2026-05-13 — Sheepot evo stays L25 ("first contract" pacing); Stomplet L32; Gnoll L28; Padlip/Wishlift L32; starters L18/L36; Threadlet L40 (latest gate).
- 2026-05-13 — Staiju evo design handed to user + artist; balance takes no action.
- 2026-06-03 — Dewdrop evo (Duodew, #26, Uncommon evo-only @ L30) validated: 60/42/58/92/66/72 (BST 390), growth 5/3/4/9/5/8 (34), catch 0.4, yield 150; `wellspring_surge` Water Special 90 BP / 100 acc / 10 PP, Drain 0.25, L34. User authors the block.

### Zone pools
- 2026-04-21 — Evolved forms never spawn in their base species' zone (branchling pulled from zone-1 pool; spread 150→55). Zone/species names since renamed (verify current pools).
- 2026-05-13 — Heartwell + Twincoil spawn in Weaverton + Weaverwood. Weaverwood pool incl. Gnoll 262 / Jackacabra 346 / Wishlift 260 / Twincoil / Stomplet 330.
- 2026-05-09 — No Epic in wild pools; Loomweaver is evolution/boss-only.

### Moves & signatures
- Signatures (BP/effect/learn Lv): `pot_guard` Sheepot +1 DEF/+1 SpD L21 · `solstice_veil` Cerametz heal 33% L38 · `dream_drift` Wishlift sleep 75 acc L19 · `two_faced_strike` Twincoil 60 BP -1 ATK L19 · `inner_stillness` Heartwell heal 33%+cleanse L21 (all 2026-05-06).
- 2026-05-06 — `goatsuckers_drain` Jackacabra Shadow Phys 75 BP drain 0.5 L38; `sovereigns_boon` Liliprince Spirit Spec 90 BP heal 25% L55.
- 2026-05-13 — `leafcap_drift` Dewdrop Nature Spec 50 BP sleep 30%/2 turns L28; `skyfall_pounce` Staiju Electric Phys 85 BP 90 acc priority +1 para 20% L32; Staiju personality Greedy→Wild.
- 2026-05-13 — `sapling_stomp` Stomplet Nature Phys 60 BP +1 DEF self L21; `shepherds_vigil` Skunkape Nature Spec 90 BP drain 0.25 L55; Skunkape L1 cluster pruned 14→5.
- 2026-05-09 — `silken_trap` (Earth Status 95 acc, -2 SPD foe/+1 SpD self, L20) and `weavers_verdict` (Earth Spec 100 BP 90 acc 5 PP, -1 SpD, L55) approved.
- Signature slot rules: Common signatures <90 BP; heal-signatures on Uncommon+ = 90 BP (Uncommon uses Drain 0.25, flat heal is Epic/Rare-grade); mid-tier signatures L19-21; post-cap signatures L55.
- 2026-05-13→05-18 — Moveset lore passes are settled (four passes; final learnsets are in `MonsterManager.cs`, read code not log). Rules: move must fit anatomy, habitat (no ocean moves on pond/well beasts; ocean line = Pagefin/Gothsire/Lochmaw), and personality verb. Off-element moves OK when the description supports it; 20-40% off-type is a guideline, not a quota.
- 2026-05-18 — Deliberately mono-element: Pagefin, Gnoll/Gnollium, Sheepot/Cerametz, Stomplet/Skunkape, Jackacabra, Padlip (Spirit awakens at Liliprince), Threadlet/Loomweaver (Earth via Anansi clay pot — intentional).
- 2026-05-13 — Dewdrop final kit 18 moves (Water 9 / Nature 3 / Spirit 2 / Wind 1 / Neutral 3); L20 `nature_shield`, L23 `blade_leaf`, `deep_pressure` L26 kept per user.

### XP & rewards
- 2026-04-21 — Per-KO XP is primary (~70/30 vs completion). Completion XP = `XPReward × (1+skill) × hardMult × clamp(zoneLv/monLv, 0.5, 2)`; skill XP bonus applies to both.
- 2026-05-09 — BaseExpYield: starters 100; Pyrgard 125, Gothsire 140, Seraphiel 130; Manehelm 240, Lochmaw 245, Aurael 240; Sheepot 55, Cerametz 150, Wishlift 70, Wishstar 220, Twincoil 75, Heartwell 75, Gnoll 65, Gnollium 150, Jackacabra 140, Padlip 80, Liliprince 260. Starter aces stay in Rare yield band despite 510-525 BST.
- 2026-04-21 — Guaranteed +1 `mat_{speciesId}` per KO; sell Common 25g / Uncommon 60g / Rare 140g; drop-only.

### Expedition difficulty (Hard Mode)
- 2026-05-13 — Hard Mode LIVE per-expedition (unlock by clearing Normal); supersedes 2026-04-27 "dormant" directive and old +10 lvl / 1.5× rewards. Enemy level `floor(L×1.25)`; ATK/SpA ×1.15; HP/DEF/SpD ×1.10; SPD ×1. Rewards XP ×2, Gold ×2, drop rate ×1.5. 3-5 Hard Tokens per clear (Tide/Loom/Dawn/Threaded per zone). Multipliers apply to stat OUTPUT after RecalculateStats. Global `HardModeEnabled` deleted.

### Guild
- 2026-04-24 — Cap 50; cumulative XP `200 + 1.2·L³` (L50 = 150,200); 10-perk tree (L5 +5% exp. gold … L45 +15% raid dmg, L50 trophy); base members 30 (35 @L15, 40 @L35), raids 3/day (4 @L25).

### Architecture / PvP / saves
- 2026-05-09 — PvP UI gated for v1.2.0 (`RankedEnabled = false`), supersedes same-day "flip live" entry; code kept for v1.3+.
- 2026-05-09 — Ranked stats via `ComputeRankedStats` (linear formula + nature only; no skill/relic/mastery). `levelOverride` default 50, clamp 5..100. SHA-256 team checksum on RPCs. BattleAI rolls through `BattleSimulator.CurrentRandom`.
- 2026-05-09 — v1.3+ PvP = Scope B authoritative-client (lockstep rejected).
- 2026-05-09 — `ValidateAndRepairMonster` (ex-`MigrateMonsterToV2`) must keep running forever; `SkillPointsMigratedV2` deleted.
- 2026-05-09 — Guild invite picker = lobby ∪ CollectedCards.

## Open / pending items
- Duodew (2026-06-03) was validate-only, "user writes the entry" — no follow-up log entry exists. A `duodew` block is now in `MonsterManager.cs`; confirm it matches the validated numbers and log it.
- PvP L100 levelFactor: cap at 42 behind a PvP flag (both call sites) when v1.3 PvP starts; acceptance = L100 mirror TTK 5-8 turns, PvE unchanged. Do NOT change the PvE curve.
- Scope B not built: `SubmitAction` RPC, resolver, wait UI, per-turn HP checksum, banlist enforcement.
- Hard Token redemption flow (items-economy lane); orphaned Cartographer skill node, 4 `UnlockCartographerMode` items, HelpPanel Cartographer text. Per-wave Hard reward plumbing via BattleSimulator was delegated, not verified.
- Mythic ranked-mode design when first Mythic ships (queue-layer constraint).
- Staiju map placement → revisit evo; if it becomes an evo-base, re-check `skyfall_pounce` L32.
- Gothsire BST 430 and Seraphiel 405 sit above the Uncommon-evolved 340-400 band; only the starter FINALS got a carve-out (verify in log).
- Skunkape 470 / Wishstar 498 exceed the Rare 340-400 band; the log cites a Rare-wall allowance that `reference-values.md` doesn't contain (verify in log). Cerametz growth 31 is 1 under the Uncommon floor.
- Judgment calls flagged for a future pass: Cherune `gust_claw` L7, Seraphiel `storm_talon` L36 (talon wording).
- Monitor Wishlift L1-3 Burn fights (5-turn TTK floor) — fix via Wishlift HP, not status fractions.
- Guild perk UI stubs: L35 banner FX, L40 Crested Emblem, L50 Eternal Beastlord.
- Backend `GET /api/players?q=` search (v1.3+). Themed material names for remaining species (fallback "{Species} Spirit") (verify in log).
- Playtest tooling gap: `get_editor_log` returned empty in MCP; start play mode fresh in-session.

## Recurring pitfalls
- Re-flagging accepted carve-outs as violations — check the Settled list above (starter finals, Padlip, Dewdrop/Gnoll growth, Staiju 320, two-tier gaps) before proposing.
- Docs drift from code (expedition rewards were 4-6× understated; zone names changed). Verify against `ExpeditionManager.cs` / `MonsterManager.cs` before citing numbers.
- Duplicate formula paths drift (CompetitiveManager kept the old sqrt; two `levelFactor` sites). Grep every copy when touching a formula.
- Difficulty multipliers go on stat OUTPUTS, never formula inputs; PvP fixes go behind a flag, never into the PvE curve.
- Moveset lore clashes recur: ocean moves on pond/well beasts, claw/talon/jab on beasts without that anatomy, intimidate/war_cry/annihilate on Timid/Loyal beasts.
- Evolved or over-band species leaking into early wild pools (branchling, Stomplet 382, Epic in wilds).
- Log only what was actually edited — the round-2 entry claimed a Lochmaw edit that never shipped.
- Save safety: learnsets hot-update via `ValidateAndRepairMonster`; new int fields default 0; adding `EvolvesTo` makes existing high-level beasts evolve on load — flag to QA.

### Currency (UI overhaul)
- 2026-09-25 — **Quests & daily streak pay Tokens (BossTokens), not gems** (user decision; the game has no gem currency). Same amounts the UI already displayed: mission `GemReward` (legacy name), daily bonus 5, weekly bonus 10, streak day + milestone "gems" slot.
- 2026-09-25 — **Gems removed** (user): all 29 achievement gem rewards + arena-rank gem rewards → Tokens 1:1; gift `Gems` field pays Tokens 1:1 (backend should stop authoring gems); save migration v3 converts saved Gems → Tokens 1:1 (cap 4000). `Tamer.Gems`, `CurrencyType.Gems`, `AddGems`/`SpendGems` remain as dead code until the RewardBundle pass (keep `Tamer.Gems` until ≥1.4 for the migration).
- 2026-09-25 — **Regional Hard tokens (Tide/Loom/Dawn/Threaded) removed** (user; unreleased, no sink, Tide un-earnable). Replaced by `HARD_MODE_FIRST_CLEAR_TOKENS` = +5 Tokens on each expedition's first Hard clear. Open follow-ups from `currency-audit-2026-09.md`: A1 skip arena quests while ranked is gated, A2 Token Collector ceiling rounding, A3 delete boss-shop Ink×5, A4 (optional) boss repeat 1→2 / 2→3.
