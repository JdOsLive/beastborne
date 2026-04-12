using System;
using System.Collections.Generic;
using Beastborne.Data;
using Beastborne.Core;

namespace Beastborne.Systems;

/// <summary>
/// Calculates genetic inheritance for breeding
/// </summary>
public static class GeneticsCalculator
{
	private static Random random = new Random();

	/// <summary>
	/// Calculate the offspring genetics from two parents
	/// </summary>
	public static Genetics CalculateOffspringGenetics( Genetics parent1, Genetics parent2, HashSet<string> lockedGenes = null )
	{
		var offspring = new Genetics();

		// Each gene has a chance to come from either parent, with possible mutation
		offspring.HPGene = InheritGene( parent1.HPGene, parent2.HPGene, lockedGenes?.Contains( "HP" ) == true );
		offspring.ATKGene = InheritGene( parent1.ATKGene, parent2.ATKGene, lockedGenes?.Contains( "ATK" ) == true );
		offspring.DEFGene = InheritGene( parent1.DEFGene, parent2.DEFGene, lockedGenes?.Contains( "DEF" ) == true );
		offspring.SpAGene = InheritGene( parent1.SpAGene, parent2.SpAGene, lockedGenes?.Contains( "SpA" ) == true );
		offspring.SpDGene = InheritGene( parent1.SpDGene, parent2.SpDGene, lockedGenes?.Contains( "SpD" ) == true );
		offspring.SPDGene = InheritGene( parent1.SPDGene, parent2.SPDGene, lockedGenes?.Contains( "SPD" ) == true );

		// Nature inheritance - chance to inherit from parent instead of random
		float natureInheritChance = TamerManager.Instance?.GetSkillBonus( SkillEffectType.NatureInheritance ) ?? 0;
		if ( natureInheritChance > 0 && random.NextDouble() < (natureInheritChance / 100f) )
		{
			// Inherit nature from a random parent
			offspring.Nature = random.NextDouble() < 0.5 ? parent1.Nature : parent2.Nature;
		}
		else
		{
			// Nature is randomly selected
			offspring.Nature = (NatureType)random.Next( 0, Enum.GetValues( typeof( NatureType ) ).Length );
		}

		return offspring;
	}

	/// <summary>
	/// Calculate a single gene inheritance — REBALANCED 2026-04-11 to be
	/// zero-drift at base. The old math always added a positive bonus on top
	/// of the average, which guaranteed the offspring exceeded both parents
	/// every generation. Players were getting 30/30 perfect beasts within
	/// 4-5 fusions because the system mathematically required upward drift.
	///
	/// New rules:
	/// - Pick higher OR lower parent (60/40 base, up to 85% higher with skills)
	/// - Variance is zero-centered (-2 to +2), not always positive
	/// - Diminishing returns kick in earlier (20+ instead of 27+)
	/// - Mutation chance is 5% base (down from 15%), 50/50 split
	/// - Gene Lock is now "preserve" (higher ±1) not a free climb
	/// - Climbing past parent values requires skill tree investment via
	///   GeneticInheritanceBonus, MutationChance, and GeneBonusFlat
	/// </summary>
	private static int InheritGene( int gene1, int gene2, bool isLocked = false )
	{
		int higher = Math.Max( gene1, gene2 );
		int lower = Math.Min( gene1, gene2 );

		if ( isLocked )
		{
			// Locked: guaranteed higher parent's gene, with small variance
			// (-1 to +1). No more "free climb" — locking preserves your best.
			int locked = higher + random.Next( -1, 2 );
			return Math.Clamp( locked, 0, Genetics.MaxGeneValue );
		}

		// Higher-parent bias: 60% base, up to +25% from GeneticInheritanceBonus
		// skill (cap at 85%). Skill investment is what earns the upward drift.
		float inheritBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.GeneticInheritanceBonus ) ?? 0;
		float higherChance = 0.6f + Math.Min( 0.25f, inheritBonus / 100f );

		int baseValue = random.NextDouble() < higherChance ? higher : lower;

		// Variance: -2 to +2, zero-centered. No guaranteed climb.
		int variance = random.Next( -2, 3 );

		// Diminishing returns at high gene values — pulls high parents down
		// over generations to create a soft ceiling around 26-28.
		if ( baseValue >= 26 )
			variance = Math.Min( variance, 0 );          // 26+: can only stay or fall
		else if ( baseValue >= 23 )
			variance = Math.Min( variance, 1 );          // 23-25: +1 max
		else if ( baseValue >= 20 )
			variance = Math.Min( variance, 2 );          // 20-22: +2 max
		// Below 20: full -2 to +2

		int result = baseValue + variance;

		// GeneBonusFlat skill — this is the player's "fusion payoff" path.
		// Without skill investment, base math is zero-drift; with skills,
		// players can push past the soft cap.
		float geneBonusFlat = TamerManager.Instance?.GetSkillBonus( SkillEffectType.GeneBonusFlat ) ?? 0;
		if ( geneBonusFlat > 0 && result < Genetics.MaxGeneValue )
		{
			int flatBonus = (int)geneBonusFlat;
			if ( result >= 26 ) flatBonus = Math.Min( flatBonus, 1 );
			result += flatBonus;
		}

		// Mutation chance (5% base, +bonus from skills). 50/50 positive vs
		// negative — no more biased-upward mutations.
		float mutationChance = 0.05f;
		float mutationBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.MutationChance ) ?? 0;
		mutationChance += mutationBonus / 100f;

		if ( random.NextDouble() < mutationChance )
		{
			int mutation = random.NextDouble() < 0.5 ? random.Next( 1, 4 ) : random.Next( -3, 0 );
			if ( mutation > 0 && result >= 26 ) mutation = Math.Min( mutation, 1 );
			result += mutation;
		}

		return Math.Clamp( result, 0, Genetics.MaxGeneValue );
	}

	/// <summary>
	/// Preview possible offspring genetics (for breeding UI)
	/// Uses best-of selection system - expected value biased toward higher parent
	/// </summary>
	public static BreedingPreview PreviewOffspring( Monster parent1, Monster parent2, HashSet<string> lockedGenes = null )
	{
		var preview = new BreedingPreview();

		int maxGene = Genetics.MaxGeneValue;

		// Get skill bonus for expected value calculation. Matches the
		// new InheritGene formula: 60% base higher-bias + up to 25% from skills.
		float inheritBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.GeneticInheritanceBonus ) ?? 0;
		float higherWeight = 0.6f + Math.Min( 0.25f, inheritBonus / 100f );

		// Gene lock info
		float geneLockBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.GeneLock ) ?? 0;
		preview.GeneLockSlots = (int)geneLockBonus;

		// Min/max ranges match the new InheritGene math:
		// - Unlocked: lower parent - 2 (worst variance) to higher + 2 (best variance)
		// - Locked:   higher parent ± 1 (preserve, not climb)
		// Mutation is rare (5%) so we exclude it from the displayed range.

		preview.MinHP = ComputeMin( parent1.Genetics.HPGene, parent2.Genetics.HPGene, lockedGenes?.Contains( "HP" ) == true );
		preview.MaxHP = ComputeMax( parent1.Genetics.HPGene, parent2.Genetics.HPGene, lockedGenes?.Contains( "HP" ) == true );
		preview.AvgHP = lockedGenes?.Contains( "HP" ) == true
			? CalculateLockedExpectedGene( parent1.Genetics.HPGene, parent2.Genetics.HPGene )
			: CalculateExpectedGene( parent1.Genetics.HPGene, parent2.Genetics.HPGene, higherWeight );

		preview.MinATK = ComputeMin( parent1.Genetics.ATKGene, parent2.Genetics.ATKGene, lockedGenes?.Contains( "ATK" ) == true );
		preview.MaxATK = ComputeMax( parent1.Genetics.ATKGene, parent2.Genetics.ATKGene, lockedGenes?.Contains( "ATK" ) == true );
		preview.AvgATK = lockedGenes?.Contains( "ATK" ) == true
			? CalculateLockedExpectedGene( parent1.Genetics.ATKGene, parent2.Genetics.ATKGene )
			: CalculateExpectedGene( parent1.Genetics.ATKGene, parent2.Genetics.ATKGene, higherWeight );

		preview.MinDEF = ComputeMin( parent1.Genetics.DEFGene, parent2.Genetics.DEFGene, lockedGenes?.Contains( "DEF" ) == true );
		preview.MaxDEF = ComputeMax( parent1.Genetics.DEFGene, parent2.Genetics.DEFGene, lockedGenes?.Contains( "DEF" ) == true );
		preview.AvgDEF = lockedGenes?.Contains( "DEF" ) == true
			? CalculateLockedExpectedGene( parent1.Genetics.DEFGene, parent2.Genetics.DEFGene )
			: CalculateExpectedGene( parent1.Genetics.DEFGene, parent2.Genetics.DEFGene, higherWeight );

		preview.MinSpA = ComputeMin( parent1.Genetics.SpAGene, parent2.Genetics.SpAGene, lockedGenes?.Contains( "SpA" ) == true );
		preview.MaxSpA = ComputeMax( parent1.Genetics.SpAGene, parent2.Genetics.SpAGene, lockedGenes?.Contains( "SpA" ) == true );
		preview.AvgSpA = lockedGenes?.Contains( "SpA" ) == true
			? CalculateLockedExpectedGene( parent1.Genetics.SpAGene, parent2.Genetics.SpAGene )
			: CalculateExpectedGene( parent1.Genetics.SpAGene, parent2.Genetics.SpAGene, higherWeight );

		preview.MinSpD = ComputeMin( parent1.Genetics.SpDGene, parent2.Genetics.SpDGene, lockedGenes?.Contains( "SpD" ) == true );
		preview.MaxSpD = ComputeMax( parent1.Genetics.SpDGene, parent2.Genetics.SpDGene, lockedGenes?.Contains( "SpD" ) == true );
		preview.AvgSpD = lockedGenes?.Contains( "SpD" ) == true
			? CalculateLockedExpectedGene( parent1.Genetics.SpDGene, parent2.Genetics.SpDGene )
			: CalculateExpectedGene( parent1.Genetics.SpDGene, parent2.Genetics.SpDGene, higherWeight );

		preview.MinSPD = ComputeMin( parent1.Genetics.SPDGene, parent2.Genetics.SPDGene, lockedGenes?.Contains( "SPD" ) == true );
		preview.MaxSPD = ComputeMax( parent1.Genetics.SPDGene, parent2.Genetics.SPDGene, lockedGenes?.Contains( "SPD" ) == true );
		preview.AvgSPD = lockedGenes?.Contains( "SPD" ) == true
			? CalculateLockedExpectedGene( parent1.Genetics.SPDGene, parent2.Genetics.SPDGene )
			: CalculateExpectedGene( parent1.Genetics.SPDGene, parent2.Genetics.SPDGene, higherWeight );

		// Quality rating based on expected genes (max 180 = 6 stats * 30)
		int avgTotal = preview.AvgHP + preview.AvgATK + preview.AvgDEF + preview.AvgSpA + preview.AvgSpD + preview.AvgSPD;
		preview.PredictedQuality = avgTotal switch
		{
			>= 162 => "Perfect",     // 90% of 180
			>= 135 => "Excellent",   // 75%
			>= 108 => "Great",       // 60%
			>= 72 => "Good",         // 40%
			_ => "Average"
		};

		// Twin chance (base 5%)
		float twinChance = 0.05f;
		float twinBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.TwinChance ) ?? 0;
		preview.TwinChance = twinChance + (twinBonus / 100f);

		// Mutation chance — matches InheritGene (5% base + skill bonus)
		float mutationChance = 0.05f;
		float mutationBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.MutationChance ) ?? 0;
		preview.MutationChance = mutationChance + (mutationBonus / 100f);

		// Nature inheritance chance (base 0%)
		float natureBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.NatureInheritance ) ?? 0;
		preview.NatureInheritChance = natureBonus / 100f;

		return preview;
	}

	/// <summary>
	/// Per-gene min for the new InheritGene math. Unlocked: lower parent
	/// minus 2 (worst variance roll). Locked: higher parent minus 1.
	/// Excludes mutation since it's a rare event (5%).
	/// </summary>
	private static int ComputeMin( int g1, int g2, bool isLocked )
	{
		if ( isLocked ) return Math.Max( 0, Math.Max( g1, g2 ) - 1 );
		return Math.Max( 0, Math.Min( g1, g2 ) - 2 );
	}

	/// <summary>
	/// Per-gene max for the new InheritGene math. Unlocked: higher parent
	/// plus 2 (best variance, capped by diminishing returns at 26+).
	/// Locked: higher parent plus 1. Excludes mutation.
	/// </summary>
	private static int ComputeMax( int g1, int g2, bool isLocked )
	{
		int higher = Math.Max( g1, g2 );
		if ( isLocked ) return Math.Min( Genetics.MaxGeneValue, higher + 1 );
		// Diminishing returns: 26+ caps at 0, 23-25 caps at +1, 20-22 caps at +2
		int maxVariance = higher >= 26 ? 0 : (higher >= 23 ? 1 : 2);
		return Math.Min( Genetics.MaxGeneValue, higher + maxVariance );
	}

	/// <summary>
	/// Calculate expected gene value based on rebalanced math (2026-04-11).
	/// Mirrors InheritGene's logic: weighted pick of higher/lower parent,
	/// zero-centered variance, diminishing returns above 20, plus the
	/// optional skill flat bonus that lets invested players climb past
	/// the soft cap.
	/// </summary>
	private static int CalculateExpectedGene( int gene1, int gene2, float higherWeight )
	{
		int higher = Math.Max( gene1, gene2 );
		int lower = Math.Min( gene1, gene2 );

		// Expected base: weighted average of higher/lower based on bias
		float expectedBase = (higher * higherWeight) + (lower * (1f - higherWeight));

		// Variance is zero-centered (-2 to +2), so the EXPECTED variance is
		// 0 at low values. At high values diminishing returns clip the
		// upper variance, so the expected nudges DOWN slightly.
		float expectedVariance = 0f;
		if ( expectedBase >= 26 ) expectedVariance = -1.0f;       // Capped at 0, expected -1
		else if ( expectedBase >= 23 ) expectedVariance = -0.5f;  // Capped at +1
		else if ( expectedBase >= 20 ) expectedVariance = -0.0f;  // Capped at +2 (no shift)

		float expected = expectedBase + expectedVariance;

		// Skill flat bonus contributes to expected value
		float geneBonusFlat = TamerManager.Instance?.GetSkillBonus( SkillEffectType.GeneBonusFlat ) ?? 0;
		if ( geneBonusFlat > 0 )
		{
			float flat = geneBonusFlat;
			if ( expected >= 26 ) flat = Math.Min( flat, 1f );
			expected += flat;
		}

		return Math.Clamp( (int)Math.Round( expected ), 0, Genetics.MaxGeneValue );
	}

	/// <summary>
	/// Calculate expected gene value when locked. Locked = guaranteed
	/// higher parent value (with -1/+1 variance at breed time, expected 0).
	/// </summary>
	private static int CalculateLockedExpectedGene( int gene1, int gene2 )
	{
		return Math.Max( gene1, gene2 );
	}

	/// <summary>
	/// Check if two monsters can breed
	/// </summary>
	public static BreedCompatibility CheckCompatibility( Monster parent1, Monster parent2 )
	{
		var result = new BreedCompatibility { CanBreed = true };

		// Same monster
		if ( parent1.Id == parent2.Id )
		{
			result.CanBreed = false;
			result.Reason = "Cannot breed a monster with itself";
			return result;
		}

		// Must be same species
		if ( parent1.SpeciesId != parent2.SpeciesId )
		{
			result.CanBreed = false;
			result.Reason = "Monsters must be the same species to breed";
			return result;
		}

		// Check if either parent is at risk (contract satisfaction too low)
		if ( parent1.Contract?.IsAtRisk == true )
		{
			result.CanBreed = false;
			result.Reason = $"{parent1.Nickname}'s contract satisfaction is too low";
			return result;
		}

		if ( parent2.Contract?.IsAtRisk == true )
		{
			result.CanBreed = false;
			result.Reason = $"{parent2.Nickname}'s contract satisfaction is too low";
			return result;
		}

		return result;
	}

	/// <summary>
	/// Calculate the cost to breed two monsters
	/// </summary>
	public static int CalculateBreedingCost( Monster parent1, Monster parent2 )
	{
		// Base cost based on average level
		int avgLevel = (parent1.Level + parent2.Level) / 2;
		int baseCost = 100 + (avgLevel * 20);

		// Rarity multiplier
		var species = MonsterManager.Instance?.GetSpecies( parent1.SpeciesId );
		float rarityMultiplier = species?.BaseRarity switch
		{
			Rarity.Uncommon => 1.5f,
			Rarity.Rare => 2.0f,
			Rarity.Epic => 3.0f,
			Rarity.Legendary => 5.0f,
			Rarity.Mythic => 10.0f,
			_ => 1.0f
		};

		// Apply skill tree discount
		float discount = TamerManager.Instance?.GetSkillBonus( SkillEffectType.BreedingCostReduction ) ?? 0;
		float multiplier = 1.0f - (discount / 100f);

		return (int)(baseCost * rarityMultiplier * multiplier);
	}
}

/// <summary>
/// Preview data for breeding UI
/// </summary>
public class BreedingPreview
{
	public int MinHP { get; set; }
	public int MaxHP { get; set; }
	public int AvgHP { get; set; }

	public int MinATK { get; set; }
	public int MaxATK { get; set; }
	public int AvgATK { get; set; }

	public int MinDEF { get; set; }
	public int MaxDEF { get; set; }
	public int AvgDEF { get; set; }

	public int MinSpA { get; set; }
	public int MaxSpA { get; set; }
	public int AvgSpA { get; set; }

	public int MinSpD { get; set; }
	public int MaxSpD { get; set; }
	public int AvgSpD { get; set; }

	public int MinSPD { get; set; }
	public int MaxSPD { get; set; }
	public int AvgSPD { get; set; }

	public string PredictedQuality { get; set; }
	public float TwinChance { get; set; }
	public float MutationChance { get; set; }
	public float NatureInheritChance { get; set; }
	public int GeneLockSlots { get; set; }
}

/// <summary>
/// Result of breed compatibility check
/// </summary>
public class BreedCompatibility
{
	public bool CanBreed { get; set; }
	public string Reason { get; set; }
}
