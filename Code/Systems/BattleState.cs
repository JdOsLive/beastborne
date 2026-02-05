using System;
using System.Collections.Generic;
using Beastborne.Data;

namespace Beastborne.Systems;

/// <summary>
/// Stat indices for stat stage tracking
/// </summary>
public enum StatIndex
{
	ATK = 0,
	DEF = 1,
	SpA = 2,
	SpD = 3,
	SPD = 4,
	Accuracy = 5,
	Evasion = 6
}

/// <summary>
/// Status conditions that can affect monsters in battle
/// </summary>
public enum StatusCondition
{
	None,
	Burn,       // DoT damage + ATK reduction
	Freeze,     // Chance to skip turn until thawed
	Paralyze,   // SPD reduction + chance to skip turn
	Poison,     // DoT damage
	Sleep,      // Skip turns until wake
	Confuse     // Chance to hurt self
}

/// <summary>
/// An active status effect on a monster
/// </summary>
public class ActiveStatus
{
	public StatusCondition Condition { get; set; }
	public int TurnsRemaining { get; set; }  // -1 = until cured
	public int TurnApplied { get; set; }

	/// <summary>
	/// Check if this status should wear off naturally
	/// </summary>
	public bool IsExpired => TurnsRemaining == 0;
}

/// <summary>
/// Temporary battle effects (shields, delayed heals, etc.)
/// </summary>
public class ActiveEffect
{
	public MoveEffectType Type { get; set; }
	public float Value { get; set; }
	public int TurnsRemaining { get; set; }
	public int TurnApplied { get; set; }
}

/// <summary>
/// Type of action a combatant can take
/// </summary>
public enum BattleActionType
{
	Attack,
	Swap,
	UseItem,
	Flee
}

/// <summary>
/// A choice of action for a turn
/// </summary>
public class MoveChoice
{
	public BattleActionType ActionType { get; set; }
	public Guid MonsterId { get; set; }
	public string MoveId { get; set; }          // For Attack actions
	public int SwapToIndex { get; set; }        // For Swap actions
	public int Priority { get; set; }           // Move priority (higher goes first)
	public int Speed { get; set; }              // Monster's speed for tiebreaking
}

/// <summary>
/// Tracks all in-battle state for a combat encounter
/// </summary>
public class BattleState
{
	/// <summary>
	/// Stat stages for each monster (-6 to +6)
	/// Key: Monster GUID, Value: array of 7 stat stages
	/// </summary>
	public Dictionary<Guid, int[]> StatStages { get; set; } = new();

	/// <summary>
	/// Active status conditions for each monster
	/// </summary>
	public Dictionary<Guid, List<ActiveStatus>> Statuses { get; set; } = new();

	/// <summary>
	/// Active temporary effects for each monster (shields, etc.)
	/// </summary>
	public Dictionary<Guid, List<ActiveEffect>> Effects { get; set; } = new();

	/// <summary>
	/// Current turn number
	/// </summary>
	public int TurnNumber { get; set; } = 0;

	/// <summary>
	/// Index of the currently active player monster
	/// </summary>
	public int PlayerActiveIndex { get; set; } = 0;

	/// <summary>
	/// Index of the currently active enemy monster
	/// </summary>
	public int EnemyActiveIndex { get; set; } = 0;

	/// <summary>
	/// Random seed for deterministic online battles (null for local battles)
	/// </summary>
	public int? RandomSeed { get; set; } = null;

	/// <summary>
	/// Whether this is an arena battle (1v1 with swaps) vs expedition (horde mode)
	/// </summary>
	public bool IsArenaMode { get; set; } = false;

	/// <summary>
	/// Track which monsters have acted this turn
	/// </summary>
	public HashSet<Guid> ActedThisTurn { get; set; } = new();

	/// <summary>
	/// Track last move used by each monster (for move restrictions)
	/// </summary>
	public Dictionary<Guid, string> LastMoveUsed { get; set; } = new();

	/// <summary>
	/// Track consecutive Guard uses for failure chance
	/// </summary>
	public Dictionary<Guid, int> ConsecutiveGuards { get; set; } = new();

	/// <summary>
	/// Track KO count per monster (for Bloodlust trait)
	/// </summary>
	public Dictionary<Guid, int> KOCounts { get; set; } = new();

	/// <summary>
	/// Whether it's the first turn of battle (for first turn bonuses)
	/// </summary>
	public bool IsFirstTurn => TurnNumber == 1;

	/// <summary>
	/// Record a KO for a monster (used by Bloodlust trait)
	/// </summary>
	public void RecordKO( Guid attackerId )
	{
		if ( !KOCounts.ContainsKey( attackerId ) )
			KOCounts[attackerId] = 0;
		KOCounts[attackerId]++;
	}

	/// <summary>
	/// Get KO count for a monster
	/// </summary>
	public int GetKOCount( Guid monsterId )
	{
		return KOCounts.TryGetValue( monsterId, out int count ) ? count : 0;
	}

	/// <summary>
	/// Initialize stat stages for a monster
	/// </summary>
	public void InitializeMonster( Guid monsterId )
	{
		if ( !StatStages.ContainsKey( monsterId ) )
		{
			StatStages[monsterId] = new int[7]; // All start at 0
		}
		if ( !Statuses.ContainsKey( monsterId ) )
		{
			Statuses[monsterId] = new List<ActiveStatus>();
		}
		if ( !Effects.ContainsKey( monsterId ) )
		{
			Effects[monsterId] = new List<ActiveEffect>();
		}
	}

	/// <summary>
	/// Get stat stage for a monster (-6 to +6)
	/// </summary>
	public int GetStatStage( Guid monsterId, StatIndex stat )
	{
		if ( StatStages.TryGetValue( monsterId, out var stages ) )
		{
			return stages[(int)stat];
		}
		return 0;
	}

	/// <summary>
	/// Modify stat stage (clamped to -6 to +6)
	/// Returns the actual change applied
	/// </summary>
	public int ModifyStatStage( Guid monsterId, StatIndex stat, int change )
	{
		InitializeMonster( monsterId );
		var stages = StatStages[monsterId];
		int oldValue = stages[(int)stat];
		int newValue = Math.Clamp( oldValue + change, -6, 6 );
		stages[(int)stat] = newValue;
		return newValue - oldValue;
	}

	/// <summary>
	/// Apply stat stage multiplier to a base stat value
	/// Stage 0 = 1.0x, +1 = 1.5x, +2 = 2.0x, etc.
	/// Stage -1 = 0.67x, -2 = 0.5x, etc.
	/// </summary>
	public static float GetStatMultiplier( int stage )
	{
		// Standard Pokemon-style stat stage multipliers
		return stage switch
		{
			-6 => 0.25f,
			-5 => 0.29f,
			-4 => 0.33f,
			-3 => 0.40f,
			-2 => 0.50f,
			-1 => 0.67f,
			0 => 1.00f,
			1 => 1.50f,
			2 => 2.00f,
			3 => 2.50f,
			4 => 3.00f,
			5 => 3.50f,
			6 => 4.00f,
			_ => 1.00f
		};
	}

	/// <summary>
	/// Get accuracy/evasion stage multiplier (different formula)
	/// </summary>
	public static float GetAccuracyMultiplier( int stage )
	{
		return stage switch
		{
			-6 => 0.33f,
			-5 => 0.38f,
			-4 => 0.43f,
			-3 => 0.50f,
			-2 => 0.60f,
			-1 => 0.75f,
			0 => 1.00f,
			1 => 1.33f,
			2 => 1.67f,
			3 => 2.00f,
			4 => 2.33f,
			5 => 2.67f,
			6 => 3.00f,
			_ => 1.00f
		};
	}

	/// <summary>
	/// Add a status condition to a monster
	/// Returns false if the monster already has a primary status
	/// </summary>
	public bool AddStatus( Guid monsterId, StatusCondition condition, int duration = -1 )
	{
		InitializeMonster( monsterId );
		var statuses = Statuses[monsterId];

		// Can only have one primary status (Burn, Freeze, Paralyze, Poison, Sleep)
		bool isPrimary = condition != StatusCondition.Confuse && condition != StatusCondition.None;
		if ( isPrimary && statuses.Exists( s => s.Condition != StatusCondition.Confuse && s.Condition != StatusCondition.None ) )
		{
			return false; // Already has a primary status
		}

		// Don't add duplicate statuses
		if ( statuses.Exists( s => s.Condition == condition ) )
		{
			return false;
		}

		statuses.Add( new ActiveStatus
		{
			Condition = condition,
			TurnsRemaining = duration,
			TurnApplied = TurnNumber
		} );

		return true;
	}

	/// <summary>
	/// Remove a status condition from a monster
	/// </summary>
	public bool RemoveStatus( Guid monsterId, StatusCondition condition )
	{
		if ( Statuses.TryGetValue( monsterId, out var statuses ) )
		{
			return statuses.RemoveAll( s => s.Condition == condition ) > 0;
		}
		return false;
	}

	/// <summary>
	/// Remove all status conditions from a monster
	/// </summary>
	public void ClearStatuses( Guid monsterId )
	{
		if ( Statuses.TryGetValue( monsterId, out var statuses ) )
		{
			statuses.Clear();
		}
	}

	/// <summary>
	/// Check if a monster has a specific status
	/// </summary>
	public bool HasStatus( Guid monsterId, StatusCondition condition )
	{
		if ( Statuses.TryGetValue( monsterId, out var statuses ) )
		{
			return statuses.Exists( s => s.Condition == condition );
		}
		return false;
	}

	/// <summary>
	/// Check if a monster has any status condition
	/// </summary>
	public bool HasAnyStatus( Guid monsterId )
	{
		if ( Statuses.TryGetValue( monsterId, out var statuses ) )
		{
			return statuses.Count > 0;
		}
		return false;
	}

	/// <summary>
	/// Get all active statuses for a monster
	/// </summary>
	public List<ActiveStatus> GetStatuses( Guid monsterId )
	{
		if ( Statuses.TryGetValue( monsterId, out var statuses ) )
		{
			return statuses;
		}
		return new List<ActiveStatus>();
	}

	/// <summary>
	/// Add a temporary effect to a monster
	/// </summary>
	public void AddEffect( Guid monsterId, MoveEffectType type, float value, int duration )
	{
		InitializeMonster( monsterId );
		Effects[monsterId].Add( new ActiveEffect
		{
			Type = type,
			Value = value,
			TurnsRemaining = duration,
			TurnApplied = TurnNumber
		} );
	}

	/// <summary>
	/// Check if a monster has a specific effect active
	/// </summary>
	public bool HasEffect( Guid monsterId, MoveEffectType type )
	{
		if ( Effects.TryGetValue( monsterId, out var effects ) )
		{
			return effects.Exists( e => e.Type == type && e.TurnsRemaining > 0 );
		}
		return false;
	}

	/// <summary>
	/// Get the value of an active effect
	/// </summary>
	public float GetEffectValue( Guid monsterId, MoveEffectType type )
	{
		if ( Effects.TryGetValue( monsterId, out var effects ) )
		{
			var effect = effects.Find( e => e.Type == type && e.TurnsRemaining > 0 );
			return effect?.Value ?? 0;
		}
		return 0;
	}

	/// <summary>
	/// Process end-of-turn effects (decrement durations, tick damage, etc.)
	/// </summary>
	public void ProcessEndOfTurn()
	{
		TurnNumber++;
		ActedThisTurn.Clear();

		// Decrement status durations
		foreach ( var (monsterId, statuses) in Statuses )
		{
			foreach ( var status in statuses )
			{
				if ( status.TurnsRemaining > 0 )
				{
					status.TurnsRemaining--;
				}
			}
			// Remove expired statuses
			statuses.RemoveAll( s => s.TurnsRemaining == 0 );
		}

		// Decrement effect durations
		foreach ( var (monsterId, effects) in Effects )
		{
			foreach ( var effect in effects )
			{
				if ( effect.TurnsRemaining > 0 )
				{
					effect.TurnsRemaining--;
				}
			}
			// Remove expired effects
			effects.RemoveAll( e => e.TurnsRemaining == 0 );
		}
	}

	/// <summary>
	/// Reset consecutive guard counter for a monster
	/// </summary>
	public void ResetGuardCounter( Guid monsterId )
	{
		ConsecutiveGuards[monsterId] = 0;
	}

	/// <summary>
	/// Increment consecutive guard counter and return current count
	/// </summary>
	public int IncrementGuardCounter( Guid monsterId )
	{
		if ( !ConsecutiveGuards.ContainsKey( monsterId ) )
		{
			ConsecutiveGuards[monsterId] = 0;
		}
		ConsecutiveGuards[monsterId]++;
		return ConsecutiveGuards[monsterId];
	}

	/// <summary>
	/// Reset all state for a new battle
	/// </summary>
	public void Reset()
	{
		StatStages.Clear();
		Statuses.Clear();
		Effects.Clear();
		TurnNumber = 0;
		PlayerActiveIndex = 0;
		EnemyActiveIndex = 0;
		ActedThisTurn.Clear();
		LastMoveUsed.Clear();
		ConsecutiveGuards.Clear();
	}
}
