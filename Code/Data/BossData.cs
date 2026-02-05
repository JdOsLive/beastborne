using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Boss tier - determines stat multipliers and rewards
/// </summary>
public enum BossTier
{
	Normal,     // 1.8x HP, 1.2x ATK
	Elite,      // 2.5x HP, 1.5x ATK, 1.3x DEF
	Legendary,  // 3.0x HP, 1.8x ATK, 1.5x DEF
	Mythic      // 4.0x HP, 2.0x ATK, 1.8x DEF
}

/// <summary>
/// Boss ability types that can trigger during phase transitions
/// </summary>
public enum BossAbilityType
{
	None,
	Enrage,         // ATK boost
	Shield,         // DEF boost
	Regenerate,     // Heal over time
	AreaDamage,     // Damage all enemies
	SummonMinion,   // Spawn an ally
	SpeedBoost,     // SPD boost
	ElementalShift  // Change element temporarily
}

/// <summary>
/// A phase in a multi-phase boss fight
/// </summary>
public class BossPhase
{
	/// <summary>
	/// HP threshold to trigger this phase (0.75 = 75% HP, 0.5 = 50% HP, etc.)
	/// </summary>
	public float HPThreshold { get; set; }

	/// <summary>
	/// Message displayed when entering this phase
	/// </summary>
	public string TransitionMessage { get; set; }

	/// <summary>
	/// Stat multiplier for this phase (stacks with boss tier multiplier)
	/// </summary>
	public float ATKMultiplier { get; set; } = 1.0f;
	public float DEFMultiplier { get; set; } = 1.0f;
	public float SPDMultiplier { get; set; } = 1.0f;

	/// <summary>
	/// Ability that triggers when entering this phase
	/// </summary>
	public BossAbilityType Ability { get; set; } = BossAbilityType.None;

	/// <summary>
	/// For SummonMinion - which species to summon
	/// </summary>
	public string SummonSpeciesId { get; set; }
}

/// <summary>
/// Defines a boss encounter
/// </summary>
public class BossData
{
	/// <summary>
	/// The monster species ID for this boss
	/// </summary>
	public string SpeciesId { get; set; }

	/// <summary>
	/// Boss tier for stat multipliers
	/// </summary>
	public BossTier Tier { get; set; } = BossTier.Normal;

	/// <summary>
	/// Phases for this boss (triggers at HP thresholds)
	/// </summary>
	public List<BossPhase> Phases { get; set; } = new();

	/// <summary>
	/// Base Boss Tokens awarded for defeating this boss
	/// </summary>
	public int BaseTokenReward { get; set; } = 2;

	/// <summary>
	/// First-time clear bonus tokens
	/// </summary>
	public int FirstClearBonus { get; set; } = 10;

	/// <summary>
	/// Get stat multipliers for this boss tier
	/// </summary>
	public (float HP, float ATK, float DEF) GetTierMultipliers()
	{
		return Tier switch
		{
			BossTier.Normal => (1.8f, 1.2f, 1.0f),
			BossTier.Elite => (2.5f, 1.5f, 1.3f),
			BossTier.Legendary => (3.0f, 1.8f, 1.5f),
			BossTier.Mythic => (4.0f, 2.0f, 1.8f),
			_ => (1.0f, 1.0f, 1.0f)
		};
	}
}

/// <summary>
/// Pool of possible bosses for an expedition
/// </summary>
public class BossPool
{
	/// <summary>
	/// Expedition ID this pool belongs to
	/// </summary>
	public string ExpeditionId { get; set; }

	/// <summary>
	/// List of possible bosses (one is randomly selected)
	/// </summary>
	public List<BossData> Bosses { get; set; } = new();

	/// <summary>
	/// Level 50+ only: chance for a legendary/celestial/mythic boss to spawn instead
	/// </summary>
	public float RareBossChance { get; set; } = 0f;

	/// <summary>
	/// Possible rare bosses that can spawn (overrides normal bosses)
	/// </summary>
	public List<BossData> RareBosses { get; set; } = new();
}

/// <summary>
/// Tracks active boss state during a fight
/// </summary>
public class ActiveBossState
{
	public BossData BossData { get; set; }
	public int CurrentPhaseIndex { get; set; } = 0;
	public bool IsRareBoss { get; set; } = false;

	/// <summary>
	/// True for one turn after a phase transition occurred (for Phase Breaker skill)
	/// </summary>
	public bool JustTransitioned { get; set; } = false;

	/// <summary>
	/// Check if we should transition to the next phase based on current HP
	/// </summary>
	public bool ShouldTransitionPhase( float hpPercent )
	{
		if ( BossData?.Phases == null || BossData.Phases.Count == 0 )
			return false;

		// Check if there's a phase we haven't reached yet
		for ( int i = CurrentPhaseIndex; i < BossData.Phases.Count; i++ )
		{
			if ( hpPercent <= BossData.Phases[i].HPThreshold )
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Get the next phase to transition to
	/// </summary>
	public BossPhase GetNextPhase( float hpPercent )
	{
		if ( BossData?.Phases == null || BossData.Phases.Count == 0 )
			return null;

		for ( int i = CurrentPhaseIndex; i < BossData.Phases.Count; i++ )
		{
			if ( hpPercent <= BossData.Phases[i].HPThreshold )
			{
				CurrentPhaseIndex = i + 1;
				return BossData.Phases[i];
			}
		}

		return null;
	}
}
