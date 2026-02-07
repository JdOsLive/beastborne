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
	/// Calculate a single gene inheritance with mutation chance
	/// Uses "best of selection" - biased toward picking the higher parent's gene
	/// When locked, always picks the higher parent's gene (guaranteed)
	/// </summary>
	private static int InheritGene( int gene1, int gene2, bool isLocked = false )
	{
		int inheritedValue;

		if ( isLocked )
		{
			// Gene Lock: guaranteed higher parent's gene value, no variance or mutation
			inheritedValue = Math.Max( gene1, gene2 );
			return Math.Clamp( inheritedValue, 0, Genetics.MaxGeneValue );
		}
		else
		{
			// Get skill bonus for gene inheritance (slight bias toward higher gene)
			float geneBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.GeneticInheritanceBonus ) ?? 0;

			// Base: average of both parents (not biased toward higher)
			float average = (gene1 + gene2) / 2f;

			// Skill bonus adds a small weight toward the higher parent (up to 20% of the difference)
			int higher = Math.Max( gene1, gene2 );
			int lower = Math.Min( gene1, gene2 );
			float skillBias = (geneBonus / 500f) * (higher - lower); // Very small bias
			skillBias = Math.Clamp( skillBias, 0f, 3f ); // Cap at +3

			inheritedValue = (int)Math.Round( average + skillBias );
		}

		// Small random bonus (+1 to +3)
		int bonus = random.Next( 1, 4 );

		// Diminishing returns: the closer to max, the smaller the bonus
		if ( inheritedValue >= 27 )
			bonus = Math.Min( bonus, 1 ); // Genes 27+ only get +1 max
		else if ( inheritedValue >= 25 )
			bonus = Math.Min( bonus, 2 ); // Genes 25-26 get +2 max
		// Below 25: full +1 to +3 bonus

		inheritedValue += bonus;

		// Mutation chance (10% base, reduced from 15%)
		float mutationChance = 0.10f;
		float mutationBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.MutationChance ) ?? 0;
		mutationChance += mutationBonus / 100f;

		if ( random.NextDouble() < mutationChance )
		{
			// Mutations can be positive or negative (60% positive, 40% negative)
			int mutation = random.NextDouble() < 0.6 ? random.Next( 1, 4 ) : random.Next( -3, 0 );

			// Diminishing returns on positive mutations too
			if ( mutation > 0 && inheritedValue >= 25 )
				mutation = Math.Min( mutation, 1 );

			inheritedValue += mutation;
		}

		// Apply flat gene bonus from skills (Gene Surge) - reduced impact
		float geneBonusFlat = TamerManager.Instance?.GetSkillBonus( SkillEffectType.GeneBonusFlat ) ?? 0;
		if ( geneBonusFlat > 0 && inheritedValue < Genetics.MaxGeneValue )
		{
			int flatBonus = (int)geneBonusFlat;
			// Diminishing returns on flat bonus too
			if ( inheritedValue >= 25 )
				flatBonus = Math.Min( flatBonus, 1 );
			inheritedValue += flatBonus;
		}

		// Clamp to valid range
		return Math.Clamp( inheritedValue, 0, Genetics.MaxGeneValue );
	}

	/// <summary>
	/// Preview possible offspring genetics (for breeding UI)
	/// Uses best-of selection system - expected value biased toward higher parent
	/// </summary>
	public static BreedingPreview PreviewOffspring( Monster parent1, Monster parent2, HashSet<string> lockedGenes = null )
	{
		var preview = new BreedingPreview();

		int maxGene = Genetics.MaxGeneValue;

		// Get skill bonus for expected value calculation
		float geneBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.GeneticInheritanceBonus ) ?? 0;
		float higherWeight = 0.7f + (geneBonus / 300f); // Same formula as InheritGene (70% base)

		// Gene lock info
		float geneLockBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.GeneLock ) ?? 0;
		preview.GeneLockSlots = (int)geneLockBonus;

		// Calculate min/max/expected for each stat
		// Locked genes: min = higher parent (guaranteed), expected = higher parent + variance
		// Unlocked: min = lower parent - 2, expected = weighted average

		bool hpLocked = lockedGenes?.Contains( "HP" ) == true;
		preview.MinHP = hpLocked
			? Math.Max( parent1.Genetics.HPGene, parent2.Genetics.HPGene )
			: Math.Max( 0, Math.Min( parent1.Genetics.HPGene, parent2.Genetics.HPGene ) - 2 );
		preview.MaxHP = Math.Min( maxGene, Math.Max( parent1.Genetics.HPGene, parent2.Genetics.HPGene ) + 7 );
		preview.AvgHP = hpLocked
			? CalculateLockedExpectedGene( parent1.Genetics.HPGene, parent2.Genetics.HPGene )
			: CalculateExpectedGene( parent1.Genetics.HPGene, parent2.Genetics.HPGene, higherWeight );

		bool atkLocked = lockedGenes?.Contains( "ATK" ) == true;
		preview.MinATK = atkLocked
			? Math.Max( parent1.Genetics.ATKGene, parent2.Genetics.ATKGene )
			: Math.Max( 0, Math.Min( parent1.Genetics.ATKGene, parent2.Genetics.ATKGene ) - 2 );
		preview.MaxATK = Math.Min( maxGene, Math.Max( parent1.Genetics.ATKGene, parent2.Genetics.ATKGene ) + 7 );
		preview.AvgATK = atkLocked
			? CalculateLockedExpectedGene( parent1.Genetics.ATKGene, parent2.Genetics.ATKGene )
			: CalculateExpectedGene( parent1.Genetics.ATKGene, parent2.Genetics.ATKGene, higherWeight );

		bool defLocked = lockedGenes?.Contains( "DEF" ) == true;
		preview.MinDEF = defLocked
			? Math.Max( parent1.Genetics.DEFGene, parent2.Genetics.DEFGene )
			: Math.Max( 0, Math.Min( parent1.Genetics.DEFGene, parent2.Genetics.DEFGene ) - 2 );
		preview.MaxDEF = Math.Min( maxGene, Math.Max( parent1.Genetics.DEFGene, parent2.Genetics.DEFGene ) + 7 );
		preview.AvgDEF = defLocked
			? CalculateLockedExpectedGene( parent1.Genetics.DEFGene, parent2.Genetics.DEFGene )
			: CalculateExpectedGene( parent1.Genetics.DEFGene, parent2.Genetics.DEFGene, higherWeight );

		bool spaLocked = lockedGenes?.Contains( "SpA" ) == true;
		preview.MinSpA = spaLocked
			? Math.Max( parent1.Genetics.SpAGene, parent2.Genetics.SpAGene )
			: Math.Max( 0, Math.Min( parent1.Genetics.SpAGene, parent2.Genetics.SpAGene ) - 2 );
		preview.MaxSpA = Math.Min( maxGene, Math.Max( parent1.Genetics.SpAGene, parent2.Genetics.SpAGene ) + 7 );
		preview.AvgSpA = spaLocked
			? CalculateLockedExpectedGene( parent1.Genetics.SpAGene, parent2.Genetics.SpAGene )
			: CalculateExpectedGene( parent1.Genetics.SpAGene, parent2.Genetics.SpAGene, higherWeight );

		bool spdLocked = lockedGenes?.Contains( "SpD" ) == true;
		preview.MinSpD = spdLocked
			? Math.Max( parent1.Genetics.SpDGene, parent2.Genetics.SpDGene )
			: Math.Max( 0, Math.Min( parent1.Genetics.SpDGene, parent2.Genetics.SpDGene ) - 2 );
		preview.MaxSpD = Math.Min( maxGene, Math.Max( parent1.Genetics.SpDGene, parent2.Genetics.SpDGene ) + 7 );
		preview.AvgSpD = spdLocked
			? CalculateLockedExpectedGene( parent1.Genetics.SpDGene, parent2.Genetics.SpDGene )
			: CalculateExpectedGene( parent1.Genetics.SpDGene, parent2.Genetics.SpDGene, higherWeight );

		bool spdStatLocked = lockedGenes?.Contains( "SPD" ) == true;
		preview.MinSPD = spdStatLocked
			? Math.Max( parent1.Genetics.SPDGene, parent2.Genetics.SPDGene )
			: Math.Max( 0, Math.Min( parent1.Genetics.SPDGene, parent2.Genetics.SPDGene ) - 2 );
		preview.MaxSPD = Math.Min( maxGene, Math.Max( parent1.Genetics.SPDGene, parent2.Genetics.SPDGene ) + 7 );
		preview.AvgSPD = spdStatLocked
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

		// Mutation chance (base 15%)
		float mutationChance = 0.15f;
		float mutationBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.MutationChance ) ?? 0;
		preview.MutationChance = mutationChance + (mutationBonus / 100f);

		// Nature inheritance chance (base 0%)
		float natureBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.NatureInheritance ) ?? 0;
		preview.NatureInheritChance = natureBonus / 100f;

		return preview;
	}

	/// <summary>
	/// Calculate expected gene value based on best-of selection with skill bonus weight
	/// </summary>
	private static int CalculateExpectedGene( int gene1, int gene2, float higherWeight )
	{
		// New system: average of parents + small bonus
		float average = (gene1 + gene2) / 2f;

		// Expected bonus is ~+2 (average of 1-3 range)
		float expectedBonus = 2f;

		// Diminishing returns reduce expected bonus at high values
		if ( average >= 27 )
			expectedBonus = 1f;
		else if ( average >= 25 )
			expectedBonus = 1.5f;

		return (int)Math.Round( average + expectedBonus );
	}

	/// <summary>
	/// Calculate expected gene value when locked (always picks higher parent)
	/// </summary>
	private static int CalculateLockedExpectedGene( int gene1, int gene2 )
	{
		// Locked: guaranteed higher parent value (variance/mutation still apply at breed time)
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
