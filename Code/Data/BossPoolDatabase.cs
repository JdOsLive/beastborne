using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Database of boss pools for launch expeditions.
///
/// Weaverton has NO boss (`HasBoss = false` on the expedition), so no pool
/// is needed. Forest + Weavermere each get a 3-boss pool sourced from their
/// own wild species — `ExpeditionManager.SelectBossForExpedition` picks one per
/// run via weighted selection, and the chosen species is filtered out of the
/// normal wave pool so it only appears in its boss fight.
///
/// Post-launch pools (echo_canyon, elemental_nexus, etc.) were cut 2026-04-21
/// alongside their expedition entries. Re-add here when post-launch zones ship.
/// </summary>
public static class BossPoolDatabase
{
	private static Dictionary<string, BossPool> _pools;

	public static BossPool GetPool( string expeditionId )
	{
		Initialize();
		return _pools.TryGetValue( expeditionId, out var pool ) ? pool : null;
	}

	public static IReadOnlyDictionary<string, BossPool> AllPools
	{
		get
		{
			Initialize();
			return _pools;
		}
	}

	private static void Initialize()
	{
		if ( _pools != null ) return;

		_pools = new Dictionary<string, BossPool>
		{
			// ─── Weaverwood (Lv 10) — Nature/Shadow tier boss ──────────
			// Wave 7 flips to a boss wave. One of these species becomes
			// the boss; the others remain in the normal wild rotation.
			// Old AI-gen pool (thornveil/bloomguard/curublast) retired
			// alongside their wave-pool counterparts. Gnollium is
			// boss-only here — players normally meet it through Gnoll
			// evolution; rolling it as boss is the rare encounter.
			["saltmoor_forest"] = new BossPool
			{
				ExpeditionId = "saltmoor_forest",
				Bosses = new List<BossData>
				{
					CreateBoss( "jackacabra", BossTier.Normal, 1, 4,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "The Jackacabra bares its fangs — the herd's nightmare wakes!", ATKMultiplier = 1.3f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "twincoil", BossTier.Normal, 1, 4,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Twincoil tightens its grip!", DEFMultiplier = 1.3f, Ability = BossAbilityType.Shield } ),
					CreateBoss( "gnollium", BossTier.Elite, 2, 6,
						new BossPhase { HPThreshold = 0.6f, TransitionMessage = "Gnollium unfurls — the grove's keeper is roused!", ATKMultiplier = 1.3f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Gnollium calls the wood — every leaf is its weapon!", DEFMultiplier = 1.2f, Ability = BossAbilityType.Regenerate } )
				}
			},

			// ─── Loomweaver's Burrow (Mini, Lv 25) — Elite named boss ────
			// Loomweaver (stage 2) appears here as the named wave-4 boss —
			// NOT in the contract pool. Players meet it dramatically then
			// earn their own by evolving Threadlet at Lv 40.
			["mini_loomweaver_burrow"] = new BossPool
			{
				ExpeditionId = "mini_loomweaver_burrow",
				Bosses = new List<BossData>
				{
					CreateBoss( "loomweaver", BossTier.Elite, 2, 10,
						new BossPhase { HPThreshold = 0.6f, TransitionMessage = "The pot shatters — what steps out was never really inside it.", ATKMultiplier = 1.35f, Ability = BossAbilityType.Enrage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Loomweaver draws silk through the fragments — the shards become armour.", DEFMultiplier = 1.3f, Ability = BossAbilityType.Shield } )
				}
			},

			// ─── Weavermere (Lv 20) — Water/Spirit tier boss ──────────
			// Final launch zone; wave 10 boss. The old AI-gen water pool
			// (mirrorpond/coralheim/rivercrest as bosses) was retired
			// pre-launch. Liliprince — the artists' pond muse, in
			// frog-prince form — is the only boss species. Every run
			// rolls the same fight until more handmade water beasts
			// ship. Liliprince is BOSS-EXCLUSIVE; players who haven't
			// rolled the boss meet it via evolution (Padlip → Liliprince
			// at 32).
			["old_saltmoor"] = new BossPool
			{
				ExpeditionId = "old_saltmoor",
				Bosses = new List<BossData>
				{
					CreateBoss( "liliprince", BossTier.Elite, 2, 8,
						new BossPhase { HPThreshold = 0.6f, TransitionMessage = "Liliprince's crown takes the dawn-light — the muse turns toward you!", ATKMultiplier = 1.4f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "The pond ripples — every lily answers Liliprince's call!", DEFMultiplier = 1.3f, Ability = BossAbilityType.Regenerate } )
				}
			}
		};
	}

	/// <summary>
	/// Create a boss with phases.
	/// </summary>
	private static BossData CreateBoss( string speciesId, BossTier tier, int baseTokens, int firstClear, params BossPhase[] phases )
	{
		return new BossData
		{
			SpeciesId = speciesId,
			Tier = tier,
			BaseTokenReward = baseTokens,
			FirstClearBonus = firstClear,
			Phases = phases?.ToList() ?? new List<BossPhase>()
		};
	}
}
