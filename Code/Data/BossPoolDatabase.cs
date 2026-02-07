using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Database of boss pools for all expeditions
/// </summary>
public static class BossPoolDatabase
{
	private static Dictionary<string, BossPool> _pools;

	/// <summary>
	/// Get boss pool by expedition ID
	/// </summary>
	public static BossPool GetPool( string expeditionId )
	{
		Initialize();
		return _pools.TryGetValue( expeditionId, out var pool ) ? pool : null;
	}

	/// <summary>
	/// Get all defined pools
	/// </summary>
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
			// Level 1 - Whispering Woods (Normal tier)
			["forest_entrance"] = new BossPool
			{
				ExpeditionId = "forest_entrance",
				Bosses = new List<BossData>
				{
					CreateBoss( "branchling", BossTier.Normal, 1, 3,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Branchling's bark hardens!", DEFMultiplier = 1.3f, Ability = BossAbilityType.Shield } ),
					CreateBoss( "galefox", BossTier.Normal, 1, 3,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Galefox howls with fury!", ATKMultiplier = 1.2f, Ability = BossAbilityType.SpeedBoost } ),
					CreateBoss( "flickermoth", BossTier.Normal, 1, 3,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Flickermoth ignites!", ATKMultiplier = 1.2f, Ability = BossAbilityType.Enrage } )
				}
			},

			// Level 5 - Ember Cavern (Normal tier)
			["ember_cavern"] = new BossPool
			{
				ExpeditionId = "ember_cavern",
				Bosses = new List<BossData>
				{
					CreateBoss( "blazefang", BossTier.Normal, 1, 4,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Blazefang's flames intensify!", ATKMultiplier = 1.3f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "emberhound", BossTier.Normal, 1, 4,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Emberhound releases a burst of fire!", ATKMultiplier = 1.2f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "charrow", BossTier.Normal, 1, 4,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Charrow enters a frenzy!", SPDMultiplier = 1.3f, Ability = BossAbilityType.SpeedBoost } )
				}
			},

			// Level 10 - Lake of Tears (Normal tier)
			["tear_lake"] = new BossPool
			{
				ExpeditionId = "tear_lake",
				Bosses = new List<BossData>
				{
					CreateBoss( "luracoil", BossTier.Normal, 1, 5,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Luracoil's lure pulses brightly!", DEFMultiplier = 1.3f, Ability = BossAbilityType.Shield } ),
					CreateBoss( "mirrorpond", BossTier.Normal, 1, 5,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Mirrorpond reflects your attacks!", DEFMultiplier = 1.4f, Ability = BossAbilityType.Shield } ),
					CreateBoss( "coralheim", BossTier.Normal, 1, 5,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Coralheim regenerates!", Ability = BossAbilityType.Regenerate } )
				}
			},

			// Level 15 - Echo Canyon (Normal tier)
			["echo_canyon"] = new BossPool
			{
				ExpeditionId = "echo_canyon",
				Bosses = new List<BossData>
				{
					CreateBoss( "hollowgale", BossTier.Normal, 1, 6,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Hollowgale summons a cyclone!", SPDMultiplier = 1.4f, Ability = BossAbilityType.SpeedBoost } ),
					CreateBoss( "galeclaw", BossTier.Normal, 1, 6,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Galeclaw screeches!", ATKMultiplier = 1.3f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "vortexel", BossTier.Normal, 1, 6,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Vortexel spins faster!", SPDMultiplier = 1.5f, ATKMultiplier = 1.2f, Ability = BossAbilityType.Enrage } )
				}
			},

			// Level 20 - Storm Spire (Normal tier)
			["storm_spire"] = new BossPool
			{
				ExpeditionId = "storm_spire",
				Bosses = new List<BossData>
				{
					CreateBoss( "staticling", BossTier.Normal, 1, 7,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Staticling charges up!", ATKMultiplier = 1.3f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "thundermane", BossTier.Normal, 1, 7,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Thundermane roars with electricity!", ATKMultiplier = 1.4f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "boltgeist", BossTier.Normal, 1, 7,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Boltgeist phases in and out!", SPDMultiplier = 1.5f, Ability = BossAbilityType.SpeedBoost } )
				}
			},

			// Level 25 - Ancient Ruins (Normal tier)
			["ancient_ruins"] = new BossPool
			{
				ExpeditionId = "ancient_ruins",
				Bosses = new List<BossData>
				{
					CreateBoss( "cragmaw", BossTier.Normal, 2, 8,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Cragmaw's stone form hardens!", DEFMultiplier = 1.5f, Ability = BossAbilityType.Shield } ),
					CreateBoss( "boulderon", BossTier.Normal, 2, 8,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Boulderon smashes the ground!", ATKMultiplier = 1.4f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "monoleth", BossTier.Normal, 2, 8,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Monoleth awakens fully!", ATKMultiplier = 1.3f, DEFMultiplier = 1.3f, Ability = BossAbilityType.Enrage } )
				}
			},

			// Level 30 - Frozen Vale (Normal tier)
			["frozen_vale"] = new BossPool
			{
				ExpeditionId = "frozen_vale",
				Bosses = new List<BossData>
				{
					CreateBoss( "glacimaw", BossTier.Normal, 2, 10,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Glacimaw's icy breath intensifies!", ATKMultiplier = 1.4f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "blizzardian", BossTier.Normal, 2, 10,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Blizzardian summons a blizzard!", SPDMultiplier = 0.8f, ATKMultiplier = 1.3f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "permafrost", BossTier.Normal, 2, 10,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Permafrost's ice shield forms!", DEFMultiplier = 1.5f, Ability = BossAbilityType.Shield } )
				}
			},

			// Level 35 - Overgrown Heart (Normal tier)
			["overgrown_heart"] = new BossPool
			{
				ExpeditionId = "overgrown_heart",
				Bosses = new List<BossData>
				{
					CreateBoss( "thornveil", BossTier.Normal, 3, 12,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Thornveil's vines spread!", DEFMultiplier = 1.4f, Ability = BossAbilityType.Shield } ),
					CreateBoss( "bloomguard", BossTier.Normal, 3, 12,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Bloomguard blooms with power!", Ability = BossAbilityType.Regenerate } ),
					CreateBoss( "eldergrove", BossTier.Normal, 3, 12,
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Eldergrove's ancient roots stir!", ATKMultiplier = 1.3f, DEFMultiplier = 1.3f, Ability = BossAbilityType.Enrage } )
				}
			},

			// Level 40 - Rusted Foundry (Elite tier)
			["rusted_foundry"] = new BossPool
			{
				ExpeditionId = "rusted_foundry",
				Bosses = new List<BossData>
				{
					CreateBoss( "ironclad", BossTier.Elite, 4, 15,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Ironclad activates defense mode!", DEFMultiplier = 1.3f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Ironclad overcharges!", ATKMultiplier = 1.4f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "forgeborn", BossTier.Elite, 4, 15,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Forgeborn heats up!", ATKMultiplier = 1.3f, Ability = BossAbilityType.Enrage },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Forgeborn enters overdrive!", ATKMultiplier = 1.5f, SPDMultiplier = 1.2f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "junktitan", BossTier.Elite, 4, 15,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Junktitan absorbs scrap!", DEFMultiplier = 1.4f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Junktitan unleashes stored energy!", ATKMultiplier = 1.5f, Ability = BossAbilityType.AreaDamage } )
				}
			},

			// Level 45 - Spirit Sanctum (Elite tier)
			["dawn_sanctuary"] = new BossPool
			{
				ExpeditionId = "dawn_sanctuary",
				Bosses = new List<BossData>
				{
					CreateBoss( "haloveil", BossTier.Elite, 4, 15,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Haloveil's halo brightens!", DEFMultiplier = 1.3f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Haloveil channels divine light!", ATKMultiplier = 1.4f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "solmara", BossTier.Elite, 4, 15,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Solmara's radiance intensifies!", ATKMultiplier = 1.3f, Ability = BossAbilityType.Enrage },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Solmara calls upon the dawn!", Ability = BossAbilityType.Regenerate } ),
					CreateBoss( "hopebringer", BossTier.Elite, 4, 15,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Hopebringer inspires hope!", Ability = BossAbilityType.Regenerate },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Hopebringer's light overwhelms!", ATKMultiplier = 1.5f, Ability = BossAbilityType.AreaDamage } )
				}
			},

			// Level 50 - Shadow Depths (Elite tier)
			["shadow_depths"] = new BossPool
			{
				ExpeditionId = "shadow_depths",
				RareBossChance = 0.01f, // 1% - Legendary/Mythic rare bosses are extremely rare
				Bosses = new List<BossData>
				{
					CreateBoss( "voidweep", BossTier.Elite, 5, 20,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Voidweep's tears darken!", ATKMultiplier = 1.3f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Voidweep embraces the void!", ATKMultiplier = 1.5f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "nullgrave", BossTier.Elite, 5, 20,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Nullgrave rises from darkness!", DEFMultiplier = 1.4f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Nullgrave summons the fallen!", Ability = BossAbilityType.SummonMinion, SummonSpeciesId = "fearling" } ),
					CreateBoss( "duskstalker", BossTier.Elite, 5, 20,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Duskstalker fades into shadow!", SPDMultiplier = 1.5f, Ability = BossAbilityType.SpeedBoost },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Duskstalker strikes from the dark!", ATKMultiplier = 1.6f, Ability = BossAbilityType.Enrage } )
				},
				RareBosses = CreateRareBosses()
			},

			// Level 55 - Elemental Nexus (Mythic tier - Primordius as main boss)
			["elemental_nexus"] = new BossPool
			{
				ExpeditionId = "elemental_nexus",
				RareBossChance = 0.015f, // 1.5% - Legendary/Mythic rare bosses are extremely rare
				Bosses = new List<BossData>
				{
					CreateBoss( "primordius", BossTier.Mythic, 15, 60,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Primordius stirs from eternal sleep!", ATKMultiplier = 1.4f, DEFMultiplier = 1.3f, Ability = BossAbilityType.Enrage },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Primordius awakens fully!", ATKMultiplier = 1.7f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Primordius transcends!", ATKMultiplier = 2.0f, DEFMultiplier = 1.5f, SPDMultiplier = 1.3f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "ashenmare", BossTier.Elite, 6, 25,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Ashenmare blazes with fury!", ATKMultiplier = 1.4f, Ability = BossAbilityType.Enrage },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Ashenmare ignites everything!", ATKMultiplier = 1.6f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "tidehollow", BossTier.Elite, 6, 25,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Tidehollow summons the depths!", DEFMultiplier = 1.4f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Tidehollow crashes like a wave!", ATKMultiplier = 1.5f, Ability = BossAbilityType.AreaDamage } )
				},
				RareBosses = CreateRareBosses()
			},

			// Level 65 - Primordial Rift (Mythic tier - Voiddragon as main boss)
			["primordial_rift"] = new BossPool
			{
				ExpeditionId = "primordial_rift",
				RareBossChance = 0.02f, // 2% - Legendary/Mythic rare bosses are extremely rare
				Bosses = new List<BossData>
				{
					CreateBoss( "voiddragon", BossTier.Mythic, 15, 60,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Voiddragon breathes oblivion!", ATKMultiplier = 1.5f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Voiddragon tears through dimensions!", ATKMultiplier = 1.8f, SPDMultiplier = 1.3f, Ability = BossAbilityType.Enrage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Voiddragon becomes entropy!", ATKMultiplier = 2.2f, DEFMultiplier = 1.4f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "raijura", BossTier.Elite, 8, 35,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Raijura splits the sky!", Ability = BossAbilityType.ElementalShift },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Raijura unleashes prismatic thunder!", ATKMultiplier = 1.6f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Raijura becomes the storm!", ATKMultiplier = 1.8f, DEFMultiplier = 1.4f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "temporal", BossTier.Elite, 8, 35,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Temporal bends time!", SPDMultiplier = 1.6f, Ability = BossAbilityType.SpeedBoost },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Temporal rewinds!", Ability = BossAbilityType.Regenerate },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Temporal accelerates!", ATKMultiplier = 1.7f, SPDMultiplier = 1.8f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "genisoul", BossTier.Mythic, 15, 60,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Genisoul recalls the first thought!", DEFMultiplier = 1.4f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Genisoul channels primordial spirit!", ATKMultiplier = 1.7f, Ability = BossAbilityType.Regenerate },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Genisoul awakens its true consciousness!", ATKMultiplier = 2.0f, DEFMultiplier = 1.5f, Ability = BossAbilityType.Enrage } )
				},
				RareBosses = CreateRareBosses()
			},

			// Level 75 - Garden of Origins (Mythic tier - Songborne as main boss)
			["garden_of_origins"] = new BossPool
			{
				ExpeditionId = "garden_of_origins",
				RareBossChance = 0.03f, // 3% - Legendary/Mythic rare bosses are extremely rare
				Bosses = new List<BossData>
				{
					CreateBoss( "songborne", BossTier.Mythic, 15, 60,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Songborne's melody intensifies!", DEFMultiplier = 1.4f, Ability = BossAbilityType.Regenerate },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Songborne's voice echoes across reality!", ATKMultiplier = 1.6f, Ability = BossAbilityType.SummonMinion, SummonSpeciesId = "edenseed" },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Songborne sings the song of creation!", ATKMultiplier = 2.0f, DEFMultiplier = 1.5f, SPDMultiplier = 1.3f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "verdantis", BossTier.Legendary, 10, 50,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Verdantis channels nature's power!", DEFMultiplier = 1.4f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Verdantis blooms with fury!", ATKMultiplier = 1.5f, Ability = BossAbilityType.Regenerate },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Verdantis becomes one with nature!", ATKMultiplier = 1.8f, DEFMultiplier = 1.5f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "primbloom", BossTier.Elite, 8, 35,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Primbloom's petals unfurl!", DEFMultiplier = 1.3f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Primbloom releases ancient spores!", ATKMultiplier = 1.5f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Primbloom enters eternal bloom!", ATKMultiplier = 1.7f, Ability = BossAbilityType.Regenerate } )
				},
				RareBosses = CreateRareBosses()
			},

			// Level 85 - Mythweaver's Realm (Mythic tier)
			["mythweavers_realm"] = new BossPool
			{
				ExpeditionId = "mythweavers_realm",
				RareBossChance = 0.04f, // 4% - Legendary/Mythic rare bosses are extremely rare
				Bosses = new List<BossData>
				{
					CreateBoss( "mythweaver", BossTier.Mythic, 18, 70,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Mythweaver weaves a new tale!", Ability = BossAbilityType.ElementalShift },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Mythweaver rewrites reality!", ATKMultiplier = 1.6f, DEFMultiplier = 1.4f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Mythweaver becomes legend!", ATKMultiplier = 2.0f, DEFMultiplier = 1.6f, SPDMultiplier = 1.3f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "sunforged", BossTier.Mythic, 18, 70,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Sunforged burns brighter!", ATKMultiplier = 1.5f, Ability = BossAbilityType.Enrage },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Sunforged becomes the sun!", ATKMultiplier = 1.8f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Sunforged goes supernova!", ATKMultiplier = 2.2f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "stormtyrant", BossTier.Mythic, 18, 70,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Stormtyrant commands the skies!", SPDMultiplier = 1.5f, Ability = BossAbilityType.SpeedBoost },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Stormtyrant calls down thunder!", ATKMultiplier = 1.7f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Stormtyrant becomes the storm!", ATKMultiplier = 2.0f, SPDMultiplier = 1.8f, Ability = BossAbilityType.Enrage } )
				},
				RareBosses = CreateRareBosses()
			},

			// Level 100 - Origin Void (Mixed tiers - ultimate boss gauntlet)
			// Large pool with weighted selection - Elite bosses are more common, Mythic are rare
			["origin_void"] = new BossPool
			{
				ExpeditionId = "origin_void",
				RareBossChance = 0.05f, // 5% - Legendary/Mythic rare bosses are extremely rare
				Bosses = new List<BossData>
				{
					// === ELITE TIER (Weight 4) - More common "lieutenant" bosses ===
					CreateBoss( "raijura", BossTier.Elite, 8, 35,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Raijura splits the sky!", Ability = BossAbilityType.ElementalShift },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Raijura unleashes prismatic thunder!", ATKMultiplier = 1.6f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "temporal", BossTier.Elite, 8, 35,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Temporal bends time!", SPDMultiplier = 1.6f, Ability = BossAbilityType.SpeedBoost },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Temporal rewinds!", Ability = BossAbilityType.Regenerate } ),
					CreateBoss( "forgeborn", BossTier.Elite, 8, 35,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Forgeborn heats up!", ATKMultiplier = 1.4f, Ability = BossAbilityType.Enrage },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Forgeborn enters overdrive!", ATKMultiplier = 1.6f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "nullgrave", BossTier.Elite, 8, 35,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Nullgrave rises from darkness!", DEFMultiplier = 1.5f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Nullgrave summons the fallen!", Ability = BossAbilityType.SummonMinion, SummonSpeciesId = "fearling" } ),
					CreateBoss( "infernowarg", BossTier.Elite, 8, 35,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Infernowarg howls with fury!", ATKMultiplier = 1.4f, Ability = BossAbilityType.Enrage },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Infernowarg's flames consume all!", ATKMultiplier = 1.7f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "oceanmaw", BossTier.Elite, 8, 35,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Oceanmaw summons the depths!", DEFMultiplier = 1.5f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Oceanmaw crashes like a tidal wave!", ATKMultiplier = 1.6f, Ability = BossAbilityType.AreaDamage } ),
					CreateBoss( "glacierback", BossTier.Elite, 8, 35,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Glacierback's ice armor thickens!", DEFMultiplier = 1.6f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Glacierback unleashes the cold!", ATKMultiplier = 1.5f, Ability = BossAbilityType.AreaDamage } ),

					// === LEGENDARY TIER (Weight 2) - Powerful but not ultimate ===
					CreateBoss( "verdantis", BossTier.Legendary, 12, 50,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Verdantis channels nature's power!", DEFMultiplier = 1.5f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Verdantis blooms with fury!", ATKMultiplier = 1.6f, Ability = BossAbilityType.Regenerate },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Verdantis becomes one with nature!", ATKMultiplier = 1.9f, DEFMultiplier = 1.5f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "eclipsara", BossTier.Legendary, 12, 50,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Eclipsara shifts between light and dark!", Ability = BossAbilityType.ElementalShift },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Eclipsara enters eclipse form!", ATKMultiplier = 1.7f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Eclipsara transcends duality!", ATKMultiplier = 2.0f, DEFMultiplier = 1.5f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "primeflare", BossTier.Legendary, 12, 50,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Primeflare ignites with ancient fire!", ATKMultiplier = 1.5f, Ability = BossAbilityType.Enrage },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Primeflare burns eternal!", ATKMultiplier = 1.8f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Primeflare becomes pure flame!", ATKMultiplier = 2.1f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "aquagenesis", BossTier.Legendary, 12, 50,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Aquagenesis summons primordial waters!", DEFMultiplier = 1.5f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Aquagenesis crashes with the tide!", ATKMultiplier = 1.6f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Aquagenesis becomes the ocean!", ATKMultiplier = 1.9f, DEFMultiplier = 1.6f, Ability = BossAbilityType.Regenerate } ),

					// === MYTHIC TIER (Weight 1) - Rare ultimate bosses ===
					CreateBoss( "genesis", BossTier.Mythic, 25, 100,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Genesis remembers creation!", DEFMultiplier = 1.5f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Genesis channels primordial power!", ATKMultiplier = 1.8f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Genesis becomes existence itself!", ATKMultiplier = 2.2f, DEFMultiplier = 1.8f, SPDMultiplier = 1.5f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "nihilex", BossTier.Mythic, 25, 100,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Nihilex embraces nothing!", Ability = BossAbilityType.Shield, DEFMultiplier = 1.6f },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Nihilex becomes the void!", ATKMultiplier = 1.9f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Nihilex erases everything!", ATKMultiplier = 2.5f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "primordius", BossTier.Mythic, 25, 100,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Primordius stirs from eternal sleep!", ATKMultiplier = 1.5f, DEFMultiplier = 1.4f, Ability = BossAbilityType.Enrage },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Primordius awakens fully!", ATKMultiplier = 1.8f, Ability = BossAbilityType.SummonMinion, SummonSpeciesId = "songborne" },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Primordius transcends!", ATKMultiplier = 2.3f, DEFMultiplier = 1.7f, SPDMultiplier = 1.4f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "worldserpent", BossTier.Mythic, 25, 100,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Worldserpent coils around reality!", DEFMultiplier = 1.6f, Ability = BossAbilityType.Shield },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Worldserpent constricts the battlefield!", ATKMultiplier = 1.7f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Worldserpent devours all!", ATKMultiplier = 2.3f, SPDMultiplier = 1.6f, Ability = BossAbilityType.Enrage } ),
					CreateBoss( "voiddragon", BossTier.Mythic, 25, 100,
						new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Voiddragon breathes oblivion!", ATKMultiplier = 1.6f, Ability = BossAbilityType.AreaDamage },
						new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Voiddragon tears through dimensions!", ATKMultiplier = 1.9f, SPDMultiplier = 1.4f, Ability = BossAbilityType.Enrage },
						new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Voiddragon becomes entropy!", ATKMultiplier = 2.4f, DEFMultiplier = 1.5f, Ability = BossAbilityType.AreaDamage } )
				},
				RareBosses = CreateRareBosses()
			}
		};
	}

	/// <summary>
	/// Create a boss with phases
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

	/// <summary>
	/// Create the pool of rare bosses that can spawn at level 50+
	/// These are Legendary/Celestial/Mythic monsters that ONLY appear as bosses
	/// </summary>
	private static List<BossData> CreateRareBosses()
	{
		return new List<BossData>
		{
			// Celestial rare boss
			CreateBoss( "eclipsara", BossTier.Legendary, 20, 75,
				new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Eclipsara shifts between light and dark!", Ability = BossAbilityType.ElementalShift },
				new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Eclipsara enters eclipse form!", ATKMultiplier = 1.7f, Ability = BossAbilityType.AreaDamage },
				new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Eclipsara transcends duality!", ATKMultiplier = 2.0f, DEFMultiplier = 1.5f, Ability = BossAbilityType.Enrage } ),

			// Mythic rare boss
			CreateBoss( "worldserpent", BossTier.Mythic, 30, 100,
				new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Worldserpent coils around existence!", DEFMultiplier = 1.6f, Ability = BossAbilityType.Shield },
				new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Worldserpent devours reality!", ATKMultiplier = 1.8f, Ability = BossAbilityType.AreaDamage },
				new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Worldserpent swallows the world!", ATKMultiplier = 2.3f, DEFMultiplier = 1.7f, Ability = BossAbilityType.Enrage } ),

			// Mythic rare boss
			CreateBoss( "voiddragon", BossTier.Mythic, 30, 100,
				new BossPhase { HPThreshold = 0.75f, TransitionMessage = "Voiddragon emerges from nothingness!", SPDMultiplier = 1.5f, Ability = BossAbilityType.SpeedBoost },
				new BossPhase { HPThreshold = 0.5f, TransitionMessage = "Voiddragon breathes the void!", ATKMultiplier = 1.9f, Ability = BossAbilityType.AreaDamage },
				new BossPhase { HPThreshold = 0.25f, TransitionMessage = "Voiddragon becomes entropy!", ATKMultiplier = 2.4f, SPDMultiplier = 1.7f, Ability = BossAbilityType.Enrage } )
		};
	}
}
