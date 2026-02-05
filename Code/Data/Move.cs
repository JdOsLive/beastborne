using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Category of move - determines which stats are used for damage calculation
/// </summary>
public enum MoveCategory
{
	Physical,  // Uses ATK vs DEF
	Special,   // Uses SpA vs SpD
	Status     // No direct damage, applies effects
}

/// <summary>
/// Types of effects that moves can apply
/// </summary>
public enum MoveEffectType
{
	// Stat modifications (apply to target or self)
	RaiseATK,
	RaiseDEF,
	RaiseSpA,
	RaiseSpD,
	RaiseSPD,
	RaiseAccuracy,
	RaiseEvasion,
	LowerATK,
	LowerDEF,
	LowerSpA,
	LowerSpD,
	LowerSPD,
	LowerAccuracy,
	LowerEvasion,

	// Status conditions
	Burn,       // DoT damage + ATK reduction
	Freeze,     // Chance to skip turn until thawed
	Paralyze,   // SPD reduction + chance to skip turn
	Poison,     // DoT damage
	Sleep,      // Skip turns until wake
	Confuse,    // Chance to hurt self

	// Special effects
	Heal,       // Restore HP (percentage of max)
	FullHeal,   // Fully restore HP
	Recoil,     // Self-damage (percentage of damage dealt)
	Drain,      // Heal for percentage of damage dealt
	Shield,     // Reduce damage taken for X turns
	Flinch,     // Target skips next turn if hit first
	CritBoost,  // Increased critical hit chance for this move

	// Turn mechanics
	Recharge,   // User must skip next turn
	Charge,     // Move takes a turn to charge before attacking

	// Field effects
	Weather,    // Change weather (Value: 1=Rain, 2=Hail, 3=Sandstorm, 4=Sun)
	LifeSiphon, // Drain HP each turn (like vines/parasites)
	Torment,    // Damage sleeping targets each turn

	// Protection and healing
	Guard,      // Block all damage this turn
	DelayedHeal,// Heal next turn
	Decoy,      // Create a decoy using HP
	Cleanse     // Remove status conditions
}

/// <summary>
/// A single effect that a move can apply
/// </summary>
public class MoveEffect
{
	/// <summary>
	/// The type of effect
	/// </summary>
	public MoveEffectType Type { get; set; }

	/// <summary>
	/// Value of the effect (percentage for stat changes, stages for buffs/debuffs)
	/// For stat stages: typically 1 or 2 (can go up to 6)
	/// For percentage effects: 10 = 10%, 50 = 50%
	/// </summary>
	public float Value { get; set; }

	/// <summary>
	/// Chance of effect applying (0.0 to 1.0, default 1.0 = 100%)
	/// </summary>
	public float Chance { get; set; } = 1.0f;

	/// <summary>
	/// Duration in turns (0 = instant/permanent stat change)
	/// </summary>
	public int Duration { get; set; } = 0;

	/// <summary>
	/// If true, effect targets the user instead of the opponent
	/// </summary>
	public bool TargetsSelf { get; set; } = false;
}

/// <summary>
/// Static definition of a move - shared across all monsters that know this move
/// </summary>
public class MoveDefinition
{
	/// <summary>
	/// Unique identifier (e.g., "ember", "flame_burst", "tackle")
	/// </summary>
	public string Id { get; set; }

	/// <summary>
	/// Display name (e.g., "Ember", "Flame Burst", "Tackle")
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Description of what the move does
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// Element type of the move (for STAB and type effectiveness)
	/// </summary>
	public ElementType Element { get; set; }

	/// <summary>
	/// Category determines which stats are used (Physical, Special, Status)
	/// </summary>
	public MoveCategory Category { get; set; }

	/// <summary>
	/// Base power of the move (0 for status moves)
	/// Typical ranges: 40 (weak), 65 (medium), 90+ (strong)
	/// </summary>
	public int BasePower { get; set; }

	/// <summary>
	/// Accuracy percentage (0-100, default 100)
	/// </summary>
	public int Accuracy { get; set; } = 100;

	/// <summary>
	/// Maximum PP (power points) - uses per expedition before needing rest
	/// Typical ranges: 5 (powerful), 15 (medium), 35 (weak/utility)
	/// </summary>
	public int MaxPP { get; set; }

	/// <summary>
	/// Priority modifier (-1 to +2, default 0)
	/// Higher priority moves go first regardless of speed
	/// </summary>
	public int Priority { get; set; } = 0;

	/// <summary>
	/// List of additional effects this move can apply
	/// </summary>
	public List<MoveEffect> Effects { get; set; } = new();

	/// <summary>
	/// Animation hint for UI (e.g., "fire_burst", "water_splash", "claw_slash")
	/// </summary>
	public string AnimationHint { get; set; }

	/// <summary>
	/// Whether this move makes contact (relevant for some traits/abilities)
	/// </summary>
	public bool MakesContact { get; set; } = true;
}

/// <summary>
/// A move that a monster knows, with current PP tracking
/// </summary>
public class MonsterMove
{
	/// <summary>
	/// Reference to the move definition
	/// </summary>
	public string MoveId { get; set; }

	/// <summary>
	/// Current PP remaining (restored after expedition)
	/// </summary>
	public int CurrentPP { get; set; }

	/// <summary>
	/// Whether this move has PP remaining
	/// </summary>
	public bool HasPP => CurrentPP > 0;

	/// <summary>
	/// Use one PP
	/// </summary>
	public void UsePP()
	{
		if ( CurrentPP > 0 )
			CurrentPP--;
	}

	/// <summary>
	/// Restore PP to max
	/// </summary>
	public void RestorePP( int maxPP )
	{
		CurrentPP = maxPP;
	}
}

/// <summary>
/// Defines which move a species can learn and at what level
/// </summary>
public class LearnableMove
{
	/// <summary>
	/// The move that can be learned
	/// </summary>
	public string MoveId { get; set; }

	/// <summary>
	/// Level at which this move is learned
	/// </summary>
	public int LearnLevel { get; set; }

	/// <summary>
	/// If set, this move replaces the specified move when learned (evolution upgrade)
	/// e.g., "ember" evolves into "flame_burst"
	/// </summary>
	public string EvolvesFrom { get; set; }
}
