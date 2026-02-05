using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Skill branches for the God of War style skill tree
/// </summary>
public enum SkillBranch
{
	Power,      // Combat stats (ATK, DEF, HP, etc.)
	Fusion,     // Breeding/fusion bonuses
	Expedition, // Exploration and expedition bonuses
	Mastery,    // Boss combat bonuses
	Fortune     // Economy and luck bonuses
}

// Keep old enum for backward compatibility during migration
public enum SkillCategory
{
	Combat,
	Breeding,
	Affinity,
	Expedition,
	Contract,
	Exploration
}

public enum SkillEffectType
{
	// Combat/Power effects
	AllMonsterATKPercent,
	AllMonsterDEFPercent,
	AllMonsterSPDPercent,
	AllMonsterHPPercent,
	AllMonsterSpAPercent,
	AllMonsterSpDPercent,
	ElementDamageBonus,
	CritChanceBonus,
	CritDamageBonus,
	CritChance, // Alias for CritChanceBonus

	// Fusion/Breeding effects
	GeneticInheritanceBonus,
	GeneInheritanceBonus, // Alias for GeneticInheritanceBonus
	GeneBonusFlat,        // Flat gene bonus on fusion
	TwinChance,
	RareTraitChance,
	BreedingCostReduction,
	MutationChance,
	NatureInheritance,    // Chance to inherit parent nature
	GeneLock,             // Number of genes that can be locked

	// Expedition effects
	ExpeditionXPBonus,
	ExpeditionGoldBonus,
	ExpeditionSpeedBonus,
	ItemFindBonus,
	RareItemChance,
	TeamSizeBonus,
	EncounterRateBonus,
	XPGainBonus, // Alias for ExpeditionXPBonus
	GoldFindBonus, // Alias for ExpeditionGoldBonus
	CatchRateBonus,
	RareEncounterChance,

	// Mastery/Boss effects
	BossDamageBonus,
	BossDamageReduction,
	BossTokenBonus,
	HigherTierDamageBonus,
	PhaseDamageBonus,
	MythicDamageBonus,
	BossSpawnBonus,

	// Fortune/Economy effects
	ShopDiscount,
	GoldDropBonus,
	DiscountStackingBonus,  // Extra discount based on gold spent
	DoubleDropChance,
	BoostPotencyBonus,      // Boosts are stronger
	GoldFromAllSources,

	// Contract effects (legacy)
	ContractSatisfactionDecayReduction,
	ContractDemandReduction,
	StartingSatisfactionBonus,
	ContractRewardBonus,
	InstantLoyaltyChance,
	FewerDemandsChance,

	// Special unlocks
	CartographerUnlock     // Unlocks special expedition modes
}

/// <summary>
/// Effect applied by a skill node
/// </summary>
public class SkillEffect
{
	public SkillEffectType Type { get; set; }
	public float Value { get; set; }
	public ElementType? AffectedElement { get; set; }

	public string GetDescription()
	{
		string valueStr = Value >= 0 ? $"+{Value}" : $"{Value}";

		return Type switch
		{
			SkillEffectType.AllMonsterATKPercent => $"{valueStr}% ATK to all monsters",
			SkillEffectType.AllMonsterDEFPercent => $"{valueStr}% DEF to all monsters",
			SkillEffectType.AllMonsterSPDPercent => $"{valueStr}% SPD to all monsters",
			SkillEffectType.AllMonsterHPPercent => $"{valueStr}% HP to all monsters",
			SkillEffectType.AllMonsterSpAPercent => $"{valueStr}% SpA to all monsters",
			SkillEffectType.AllMonsterSpDPercent => $"{valueStr}% SpD to all monsters",
			SkillEffectType.ElementDamageBonus => $"{valueStr}% {AffectedElement} damage",
			SkillEffectType.CritChanceBonus => $"{valueStr}% critical hit chance",
			SkillEffectType.CritDamageBonus => $"{valueStr}% critical damage",
			SkillEffectType.GeneticInheritanceBonus => $"{valueStr}% better gene inheritance",
			SkillEffectType.GeneBonusFlat => $"{valueStr} gene point bonus",
			SkillEffectType.TwinChance => $"{valueStr}% chance for twins",
			SkillEffectType.RareTraitChance => $"{valueStr}% rare trait chance",
			SkillEffectType.MutationChance => $"{valueStr}% beneficial mutation chance",
			SkillEffectType.NatureInheritance => $"{valueStr}% nature inheritance",
			SkillEffectType.GeneLock => $"Lock {valueStr} genes",
			SkillEffectType.ExpeditionXPBonus => $"{valueStr}% expedition XP",
			SkillEffectType.ExpeditionGoldBonus => $"{valueStr}% expedition gold",
			SkillEffectType.ExpeditionSpeedBonus => $"{valueStr}% expedition speed",
			SkillEffectType.ItemFindBonus => $"{valueStr}% item find rate",
			SkillEffectType.RareItemChance => $"{valueStr}% rare item chance",
			SkillEffectType.TeamSizeBonus => $"{valueStr} team size",
			SkillEffectType.EncounterRateBonus => $"{valueStr}% encounter rate",
			SkillEffectType.CatchRateBonus => $"{valueStr}% catch rate",
			SkillEffectType.RareEncounterChance => $"{valueStr}% rare encounter chance",
			SkillEffectType.BossDamageBonus => $"{valueStr}% damage vs bosses",
			SkillEffectType.BossDamageReduction => $"{valueStr}% less boss damage taken",
			SkillEffectType.BossTokenBonus => $"{valueStr}% boss tokens",
			SkillEffectType.HigherTierDamageBonus => $"{valueStr}% damage vs higher tier",
			SkillEffectType.PhaseDamageBonus => $"{valueStr}% phase transition damage",
			SkillEffectType.MythicDamageBonus => $"{valueStr}% damage vs Mythic bosses",
			SkillEffectType.BossSpawnBonus => $"{valueStr}% boss spawn rate",
			SkillEffectType.ShopDiscount => $"{valueStr}% shop discount",
			SkillEffectType.GoldDropBonus => $"{valueStr}% gold drops",
			SkillEffectType.DiscountStackingBonus => $"{valueStr}% extra discount per 100k spent",
			SkillEffectType.DoubleDropChance => $"{valueStr}% double drop chance",
			SkillEffectType.BoostPotencyBonus => $"{valueStr}% boost potency",
			SkillEffectType.GoldFromAllSources => $"{valueStr}% gold from all sources",
			SkillEffectType.CartographerUnlock => "Unlock expedition mode",
			SkillEffectType.BreedingCostReduction => $"{valueStr}% fusion cost reduction",
			SkillEffectType.ContractSatisfactionDecayReduction => $"{valueStr}% slower satisfaction decay",
			SkillEffectType.ContractDemandReduction => $"{valueStr}% reduced demand requirements",
			SkillEffectType.StartingSatisfactionBonus => $"{valueStr} starting satisfaction",
			SkillEffectType.ContractRewardBonus => $"{valueStr}% bonus satisfaction from demands",
			SkillEffectType.InstantLoyaltyChance => $"{valueStr}% chance for instant loyalty",
			SkillEffectType.FewerDemandsChance => $"{valueStr}% chance for fewer demands",
			_ => $"{valueStr} unknown effect"
		};
	}
}

/// <summary>
/// A single node in the skill tree (supports multiple ranks)
/// </summary>
public class SkillNode
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string IconPath { get; set; }

	// New branch-based system
	public SkillBranch Branch { get; set; }
	public int Order { get; set; } = 0;  // Position in branch (0 = first skill)

	// Rank system
	public int MaxRank { get; set; } = 1;      // Maximum ranks for this skill
	public int CostPerRank { get; set; } = 1;  // SP cost per rank

	// Effect per rank (multiplied by current rank)
	public float EffectPerRank { get; set; } = 0;

	// Effects this skill provides (base effects, multiplied by rank)
	public List<SkillEffect> Effects { get; set; } = new();

	// Tier-based progression (1 = foundation, 2 = advancement, 3 = capstone)
	public int Tier { get; set; } = 1;
	public int RequiredBranchPoints { get; set; } = 0;  // Points needed in branch to unlock
	public int GridRow { get; set; } = 0;  // 0=top, 1=middle, 2=bottom (for UI layout)

	// Prerequisites - specific skill requirement (for special chains like Crit Eye -> Devastating Blows)
	public string RequiredSkillId { get; set; } = null;
	public int RequiredSkillRank { get; set; } = 1;  // Minimum rank needed in required skill

	// Special unlock data (for Cartographer skill)
	public List<string> UnlocksAtRank { get; set; } = new();

	// Legacy properties for backward compatibility
	public SkillCategory Category { get; set; }
	public int SkillPointCost { get => CostPerRank; set => CostPerRank = value; }
	public List<string> RequiredSkillIds
	{
		get => string.IsNullOrEmpty( RequiredSkillId ) ? new() : new() { RequiredSkillId };
		set => RequiredSkillId = value?.Count > 0 ? value[0] : null;
	}

	// Position in the skill tree grid (for UI)
	public int GridX { get; set; }
	public int GridY { get; set; }

	// Aliases for position (used by UI components)
	public int X => GridX * 100 + 50;
	public int Y => GridY * 100 + 50;

	// Get total SP cost to max this skill
	public int TotalCost => MaxRank * CostPerRank;

	// Get combined description of all effects at max rank
	public string GetEffectsDescription()
	{
		if ( Effects == null || Effects.Count == 0 )
			return Description;

		var descriptions = new List<string>();
		foreach ( var effect in Effects )
		{
			// Show effect at max rank
			var scaledValue = effect.Value * MaxRank;
			var desc = effect.Type switch
			{
				SkillEffectType.GeneLock => $"Lock {effect.Value} genes (at max rank)",
				_ => effect.GetDescription().Replace( $"+{effect.Value}", $"+{scaledValue}" )
			};
			descriptions.Add( desc );
		}
		return string.Join( "\n", descriptions );
	}

	// Get effect value at a specific rank
	public float GetEffectValueAtRank( SkillEffectType type, int rank )
	{
		foreach ( var effect in Effects )
		{
			if ( effect.Type == type )
			{
				return effect.Value * rank;
			}
		}
		return 0;
	}
}
