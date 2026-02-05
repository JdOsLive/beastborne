using System.Collections.Generic;
using System.Linq;

namespace Beastborne.Data;

/// <summary>
/// The complete skill tree containing all nodes organized into branches
/// God of War style linear progression with multiple ranks per skill
/// </summary>
public class SkillTree
{
	public List<SkillNode> Nodes { get; set; } = new();

	// Alias for Nodes property (used by UI)
	public List<SkillNode> AllNodes => Nodes;

	public SkillNode GetNode( string id )
	{
		return Nodes.FirstOrDefault( n => n.Id == id );
	}

	public List<SkillNode> GetNodesByBranch( SkillBranch branch )
	{
		return Nodes.Where( n => n.Branch == branch ).OrderBy( n => n.Order ).ToList();
	}

	// Legacy method for backward compatibility
	public List<SkillNode> GetNodesByCategory( SkillCategory category )
	{
		return Nodes.Where( n => n.Category == category ).ToList();
	}

	/// <summary>
	/// Calculate total skill points spent in a branch
	/// </summary>
	public int GetPointsSpentInBranch( SkillBranch branch, Dictionary<string, int> skillRanks )
	{
		int total = 0;
		foreach ( var node in Nodes.Where( n => n.Branch == branch ) )
		{
			int rank = skillRanks.GetValueOrDefault( node.Id, 0 );
			total += rank * node.CostPerRank;
		}
		return total;
	}

	public List<SkillNode> GetUnlockableNodes( Dictionary<string, int> skillRanks )
	{
		return Nodes.Where( n => CanUnlockInternal( n, skillRanks ) ).ToList();
	}

	private bool CanUnlockInternal( SkillNode node, Dictionary<string, int> skillRanks )
	{
		int currentRank = skillRanks.GetValueOrDefault( node.Id, 0 );
		if ( currentRank >= node.MaxRank ) return false; // Already maxed

		// Check branch point requirement (tier-based)
		int branchPoints = GetPointsSpentInBranch( node.Branch, skillRanks );
		if ( branchPoints < node.RequiredBranchPoints ) return false;

		// Check specific skill prerequisite (for special chains like Crit Eye -> Devastating Blows)
		if ( !string.IsNullOrEmpty( node.RequiredSkillId ) )
		{
			int reqRank = skillRanks.GetValueOrDefault( node.RequiredSkillId, 0 );
			if ( reqRank < node.RequiredSkillRank ) return false;
		}

		return true;
	}

	public bool CanUnlock( string nodeId, Dictionary<string, int> skillRanks, int availablePoints )
	{
		var node = GetNode( nodeId );
		if ( node == null ) return false;
		if ( availablePoints < node.CostPerRank ) return false; // Not enough points

		return CanUnlockInternal( node, skillRanks );
	}

	/// <summary>
	/// Generate the skill tree with 5 branches using tier-based progression
	/// Each branch has 3 tiers: Foundation (always available), Advancement, Capstone
	/// </summary>
	public static SkillTree CreateDefault()
	{
		var tree = new SkillTree();

		// ==========================================
		// POWER BRANCH (Combat Stats) - 80 SP total
		// Tier 1: ATK, DEF, HP (0 pts)
		// Tier 2: SpA, SpD, SPD (15 pts)
		// Tier 3: Crit (40 pts)
		// ==========================================

		// TIER 1 - Foundation (no requirements)
		tree.Nodes.Add( new SkillNode
		{
			Id = "power_might",
			Name = "Might",
			Description = "Increase ATK for all monsters",
			Branch = SkillBranch.Power,
			Tier = 1,
			GridRow = 0,
			Order = 0,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.AllMonsterATKPercent, Value = 2 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "power_fortitude",
			Name = "Fortitude",
			Description = "Increase DEF for all monsters",
			Branch = SkillBranch.Power,
			Tier = 1,
			GridRow = 1,
			Order = 1,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.AllMonsterDEFPercent, Value = 2 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "power_vitality",
			Name = "Vitality",
			Description = "Increase HP for all monsters",
			Branch = SkillBranch.Power,
			Tier = 1,
			GridRow = 2,
			Order = 2,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.AllMonsterHPPercent, Value = 2 } }
		} );

		// TIER 2 - Advancement (15 pts in Power branch)
		tree.Nodes.Add( new SkillNode
		{
			Id = "power_arcane",
			Name = "Arcane Power",
			Description = "Increase SpA for all monsters",
			Branch = SkillBranch.Power,
			Tier = 2,
			GridRow = 0,
			Order = 3,
			RequiredBranchPoints = 15,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.AllMonsterSpAPercent, Value = 2 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "power_arcshield",
			Name = "Arcane Shield",
			Description = "Increase SpD for all monsters",
			Branch = SkillBranch.Power,
			Tier = 2,
			GridRow = 1,
			Order = 4,
			RequiredBranchPoints = 15,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.AllMonsterSpDPercent, Value = 2 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "power_swiftness",
			Name = "Swiftness",
			Description = "Increase SPD for all monsters",
			Branch = SkillBranch.Power,
			Tier = 2,
			GridRow = 2,
			Order = 5,
			RequiredBranchPoints = 15,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.AllMonsterSPDPercent, Value = 2 } }
		} );

		// TIER 3 - Capstone (40 pts in Power branch)
		tree.Nodes.Add( new SkillNode
		{
			Id = "power_criteye",
			Name = "Critical Eye",
			Description = "Increase critical hit chance",
			Branch = SkillBranch.Power,
			Tier = 3,
			GridRow = 0,
			Order = 6,
			RequiredBranchPoints = 40,
			MaxRank = 5,
			CostPerRank = 2,
			Effects = new() { new SkillEffect { Type = SkillEffectType.CritChanceBonus, Value = 3 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "power_devastating",
			Name = "Devastating Blows",
			Description = "Increase critical hit damage",
			Branch = SkillBranch.Power,
			Tier = 3,
			GridRow = 1,
			Order = 7,
			RequiredBranchPoints = 40,
			RequiredSkillId = "power_criteye",  // Special chain: requires Crit Eye
			RequiredSkillRank = 1,
			MaxRank = 5,
			CostPerRank = 2,
			Effects = new() { new SkillEffect { Type = SkillEffectType.CritDamageBonus, Value = 10 } }
		} );

		// ==========================================
		// FUSION BRANCH (Breeding) - 96 SP total
		// Tier 1: Gene Surge, Inheritance, Fusion Mastery (0 pts)
		// Tier 2: Mutation, Trait Affinity, Twin Spirit (15 pts)
		// Tier 3: Nature Bond, Gene Lock (35 pts)
		// ==========================================

		// TIER 1 - Foundation
		tree.Nodes.Add( new SkillNode
		{
			Id = "fusion_genesurge",
			Name = "Gene Surge",
			Description = "Bonus gene points on fusion",
			Branch = SkillBranch.Fusion,
			Tier = 1,
			GridRow = 0,
			Order = 0,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.GeneBonusFlat, Value = 1 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "fusion_inheritance",
			Name = "Inheritance",
			Description = "Better parent gene selection",
			Branch = SkillBranch.Fusion,
			Tier = 1,
			GridRow = 1,
			Order = 1,
			MaxRank = 5,
			CostPerRank = 2,
			Effects = new() { new SkillEffect { Type = SkillEffectType.GeneticInheritanceBonus, Value = 5 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "fusion_mastery",
			Name = "Fusion Mastery",
			Description = "Reduced fusion costs",
			Branch = SkillBranch.Fusion,
			Tier = 1,
			GridRow = 2,
			Order = 2,
			MaxRank = 5,
			CostPerRank = 3,
			Effects = new() { new SkillEffect { Type = SkillEffectType.BreedingCostReduction, Value = 10 } }
		} );

		// TIER 2 - Advancement (15 pts)
		tree.Nodes.Add( new SkillNode
		{
			Id = "fusion_mutation",
			Name = "Mutation Chance",
			Description = "Increased positive mutation rate",
			Branch = SkillBranch.Fusion,
			Tier = 2,
			GridRow = 0,
			Order = 3,
			RequiredBranchPoints = 15,
			MaxRank = 5,
			CostPerRank = 2,
			Effects = new() { new SkillEffect { Type = SkillEffectType.MutationChance, Value = 2 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "fusion_trait",
			Name = "Trait Affinity",
			Description = "Increased rare trait inheritance",
			Branch = SkillBranch.Fusion,
			Tier = 2,
			GridRow = 1,
			Order = 4,
			RequiredBranchPoints = 15,
			MaxRank = 5,
			CostPerRank = 3,
			Effects = new() { new SkillEffect { Type = SkillEffectType.RareTraitChance, Value = 4 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "fusion_twin",
			Name = "Twin Spirit",
			Description = "Chance to get twins when fusing",
			Branch = SkillBranch.Fusion,
			Tier = 2,
			GridRow = 2,
			Order = 5,
			RequiredBranchPoints = 15,
			MaxRank = 5,
			CostPerRank = 3,
			Effects = new() { new SkillEffect { Type = SkillEffectType.TwinChance, Value = 3 } }
		} );

		// TIER 3 - Capstone (35 pts)
		tree.Nodes.Add( new SkillNode
		{
			Id = "fusion_nature",
			Name = "Nature Bond",
			Description = "Chance to inherit parent's nature",
			Branch = SkillBranch.Fusion,
			Tier = 3,
			GridRow = 0,
			Order = 6,
			RequiredBranchPoints = 35,
			MaxRank = 3,
			CostPerRank = 3,
			Effects = new() { new SkillEffect { Type = SkillEffectType.NatureInheritance, Value = 25 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "fusion_genelock",
			Name = "Gene Lock",
			Description = "Lock genes for guaranteed inheritance",
			Branch = SkillBranch.Fusion,
			Tier = 3,
			GridRow = 2,
			Order = 7,
			RequiredBranchPoints = 35,
			MaxRank = 3,
			CostPerRank = 4,
			Effects = new() { new SkillEffect { Type = SkillEffectType.GeneLock, Value = 1 } }
		} );

		// ==========================================
		// EXPEDITION BRANCH - 80 SP total
		// Tier 1: Pathfinder, Treasure Hunter, Scout (0 pts)
		// Tier 2: Team Spirit, Lucky Find, Endurance (15 pts)
		// Tier 3: Cartographer (35 pts)
		// ==========================================

		// TIER 1 - Foundation
		tree.Nodes.Add( new SkillNode
		{
			Id = "exp_pathfinder",
			Name = "Prospector",
			Description = "Find more gold on expeditions",
			Branch = SkillBranch.Expedition,
			Tier = 1,
			GridRow = 0,
			Order = 0,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.ExpeditionGoldBonus, Value = 5 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "exp_treasure",
			Name = "Treasure Hunter",
			Description = "Find more items on expeditions",
			Branch = SkillBranch.Expedition,
			Tier = 1,
			GridRow = 1,
			Order = 1,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.ItemFindBonus, Value = 3 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "exp_scout",
			Name = "Scout",
			Description = "Increased encounter rate",
			Branch = SkillBranch.Expedition,
			Tier = 1,
			GridRow = 2,
			Order = 2,
			MaxRank = 5,
			CostPerRank = 2,
			Effects = new() { new SkillEffect { Type = SkillEffectType.EncounterRateBonus, Value = 10 } }
		} );

		// TIER 2 - Advancement (15 pts)
		tree.Nodes.Add( new SkillNode
		{
			Id = "exp_teamspirit",
			Name = "Team Spirit",
			Description = "Bonus XP for expedition team",
			Branch = SkillBranch.Expedition,
			Tier = 2,
			GridRow = 0,
			Order = 3,
			RequiredBranchPoints = 15,
			MaxRank = 5,
			CostPerRank = 2,
			Effects = new() { new SkillEffect { Type = SkillEffectType.ExpeditionXPBonus, Value = 5 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "exp_luckyfind",
			Name = "Lucky Find",
			Description = "Chance to find rare items",
			Branch = SkillBranch.Expedition,
			Tier = 2,
			GridRow = 1,
			Order = 4,
			RequiredBranchPoints = 15,
			MaxRank = 5,
			CostPerRank = 3,
			Effects = new() { new SkillEffect { Type = SkillEffectType.RareItemChance, Value = 5 } }
		} );

		// TIER 3 - Capstone (35 pts)
		tree.Nodes.Add( new SkillNode
		{
			Id = "exp_cartographer",
			Name = "Cartographer",
			Description = "Unlock special expedition modes",
			Branch = SkillBranch.Expedition,
			Tier = 3,
			GridRow = 1,
			Order = 6,
			RequiredBranchPoints = 35,
			MaxRank = 5,
			CostPerRank = 2,
			Effects = new() { new SkillEffect { Type = SkillEffectType.CartographerUnlock, Value = 1 } },
			UnlocksAtRank = new() { "nightmare_mode", "element_hunt", "boss_rush", "rare_den", "relic_expedition" }
		} );

		// ==========================================
		// MASTERY BRANCH (Boss Combat) - 82 SP total
		// Tier 1: Boss Slayer, Resilience (0 pts)
		// Tier 2: Giant Killer, Token Collector, Phase Breaker (10 pts)
		// Tier 3: Mythbreaker, Boss Hunter (30 pts)
		// ==========================================

		// TIER 1 - Foundation
		tree.Nodes.Add( new SkillNode
		{
			Id = "mastery_slayer",
			Name = "Boss Slayer",
			Description = "Deal more damage to bosses",
			Branch = SkillBranch.Mastery,
			Tier = 1,
			GridRow = 0,
			Order = 0,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.BossDamageBonus, Value = 3 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "mastery_resilience",
			Name = "Resilience",
			Description = "Take less damage from bosses",
			Branch = SkillBranch.Mastery,
			Tier = 1,
			GridRow = 2,
			Order = 1,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.BossDamageReduction, Value = 2 } }
		} );

		// TIER 2 - Advancement (10 pts)
		tree.Nodes.Add( new SkillNode
		{
			Id = "mastery_giantkiller",
			Name = "Giant Killer",
			Description = "Bonus damage vs higher tier bosses",
			Branch = SkillBranch.Mastery,
			Tier = 2,
			GridRow = 0,
			Order = 2,
			RequiredBranchPoints = 10,
			MaxRank = 5,
			CostPerRank = 2,
			Effects = new() { new SkillEffect { Type = SkillEffectType.HigherTierDamageBonus, Value = 5 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "mastery_tokens",
			Name = "Token Collector",
			Description = "Earn more boss tokens",
			Branch = SkillBranch.Mastery,
			Tier = 2,
			GridRow = 1,
			Order = 3,
			RequiredBranchPoints = 10,
			MaxRank = 5,
			CostPerRank = 2,
			Effects = new() { new SkillEffect { Type = SkillEffectType.BossTokenBonus, Value = 10 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "mastery_phasebreaker",
			Name = "Phase Breaker",
			Description = "Bonus damage during phase transitions",
			Branch = SkillBranch.Mastery,
			Tier = 2,
			GridRow = 2,
			Order = 4,
			RequiredBranchPoints = 10,
			MaxRank = 5,
			CostPerRank = 3,
			Effects = new() { new SkillEffect { Type = SkillEffectType.PhaseDamageBonus, Value = 10 } }
		} );

		// TIER 3 - Capstone (30 pts)
		tree.Nodes.Add( new SkillNode
		{
			Id = "mastery_mythbreaker",
			Name = "Mythbreaker",
			Description = "Massive bonus damage vs Mythic bosses",
			Branch = SkillBranch.Mastery,
			Tier = 3,
			GridRow = 0,
			Order = 5,
			RequiredBranchPoints = 30,
			RequiredSkillId = "mastery_slayer",  // Requires Boss Slayer rank 5
			RequiredSkillRank = 5,
			MaxRank = 3,
			CostPerRank = 4,
			Effects = new() { new SkillEffect { Type = SkillEffectType.MythicDamageBonus, Value = 15 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "mastery_hunter",
			Name = "Boss Hunter",
			Description = "Increased boss spawn rate",
			Branch = SkillBranch.Mastery,
			Tier = 3,
			GridRow = 2,
			Order = 6,
			RequiredBranchPoints = 30,
			MaxRank = 5,
			CostPerRank = 3,
			Effects = new() { new SkillEffect { Type = SkillEffectType.BossSpawnBonus, Value = 5 } }
		} );

		// ==========================================
		// FORTUNE BRANCH (Economy) - 82 SP total
		// Tier 1: Bargain Hunter, Gold Rush, Lucky Star (0 pts)
		// Tier 2: Investor, Jackpot, Merchant Prince (15 pts)
		// Tier 3: Golden Touch (35 pts)
		// ==========================================

		// TIER 1 - Foundation
		tree.Nodes.Add( new SkillNode
		{
			Id = "fortune_bargain",
			Name = "Bargain Hunter",
			Description = "Reduced shop prices",
			Branch = SkillBranch.Fortune,
			Tier = 1,
			GridRow = 0,
			Order = 0,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.ShopDiscount, Value = 2 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "fortune_goldrush",
			Name = "Gold Rush",
			Description = "Increased gold drops",
			Branch = SkillBranch.Fortune,
			Tier = 1,
			GridRow = 1,
			Order = 1,
			MaxRank = 10,
			CostPerRank = 1,
			Effects = new() { new SkillEffect { Type = SkillEffectType.GoldDropBonus, Value = 5 } }
		} );

		// TIER 2 - Advancement (15 pts)
		tree.Nodes.Add( new SkillNode
		{
			Id = "fortune_savvy",
			Name = "Savvy Shopper",
			Description = "Extra shop discount based on gold spent",
			Branch = SkillBranch.Fortune,
			Tier = 2,
			GridRow = 0,
			Order = 3,
			RequiredBranchPoints = 15,
			MaxRank = 5,
			CostPerRank = 2,
			Effects = new() { new SkillEffect { Type = SkillEffectType.DiscountStackingBonus, Value = 1 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "fortune_jackpot",
			Name = "Jackpot",
			Description = "Chance to double drops",
			Branch = SkillBranch.Fortune,
			Tier = 2,
			GridRow = 1,
			Order = 4,
			RequiredBranchPoints = 15,
			MaxRank = 5,
			CostPerRank = 3,
			Effects = new() { new SkillEffect { Type = SkillEffectType.DoubleDropChance, Value = 3 } }
		} );

		tree.Nodes.Add( new SkillNode
		{
			Id = "fortune_amplifier",
			Name = "Amplifier",
			Description = "Shop boosts are more powerful",
			Branch = SkillBranch.Fortune,
			Tier = 2,
			GridRow = 2,
			Order = 5,
			RequiredBranchPoints = 15,
			MaxRank = 5,
			CostPerRank = 3,
			Effects = new() { new SkillEffect { Type = SkillEffectType.BoostPotencyBonus, Value = 5 } }
		} );

		// TIER 3 - Capstone (35 pts)
		tree.Nodes.Add( new SkillNode
		{
			Id = "fortune_golden",
			Name = "Golden Touch",
			Description = "Bonus gold from all sources",
			Branch = SkillBranch.Fortune,
			Tier = 3,
			GridRow = 1,
			Order = 6,
			RequiredBranchPoints = 35,
			MaxRank = 3,
			CostPerRank = 4,
			Effects = new() { new SkillEffect { Type = SkillEffectType.GoldFromAllSources, Value = 10 } }
		} );

		return tree;
	}
}
