using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Database of boss pools for launch expeditions.
///
/// Saltmoor Cove has NO boss (`HasBoss = false` on the expedition), so no pool
/// is needed. Forest + Old Saltmoor each get a 3-boss pool sourced from their
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
			// ─── Saltmoor Forest (Lv 15) — Nature tier boss ──────────────
			// Wave 7 flips to a boss wave. One of these species becomes the
			// boss; the others remain in the normal wild rotation.
			["saltmoor_forest"] = new BossPool
			{
				ExpeditionId = "saltmoor_forest",
				Bosses = new List<BossData>
				{
					CreateBoss( "thornveil", BossTier.Normal, 1, 4,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Thornveil bristles — vines thicken!", DEFMultiplier = 1.3f, Ability = BossAbilityType.Shield } ),
					CreateBoss( "bloomguard", BossTier.Normal, 1, 4,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Bloomguard flowers with fury!", Ability = BossAbilityType.Regenerate } ),
					CreateBoss( "curublast", BossTier.Normal, 1, 4,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Curublast releases a cloud of spores!", ATKMultiplier = 1.2f, Ability = BossAbilityType.AreaDamage } )
				}
			},

			// ─── Old Saltmoor (Lv 30) — Water tier boss ──────────────────
			// Final launch zone; wave 10 boss. Pool drawn from Old Saltmoor
			// wild species so the boss feels native to the drowned town.
			["old_saltmoor"] = new BossPool
			{
				ExpeditionId = "old_saltmoor",
				Bosses = new List<BossData>
				{
					CreateBoss( "mirrorpond", BossTier.Normal, 1, 6,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Mirrorpond reflects your attacks!", DEFMultiplier = 1.4f, Ability = BossAbilityType.Shield } ),
					CreateBoss( "coralheim", BossTier.Normal, 1, 6,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Coralheim regenerates from the tide!", Ability = BossAbilityType.Regenerate } ),
					CreateBoss( "rivercrest", BossTier.Normal, 1, 6,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Rivercrest surges!", ATKMultiplier = 1.3f, Ability = BossAbilityType.Enrage } )
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
