using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Types of effects that traits can provide
/// </summary>
public enum TraitEffectType
{
	// Flat damage modifiers
	DamageBonus,           // +X% to all damage dealt
	DamageReduction,       // -X% to all damage taken

	// Element-specific modifiers
	ElementDamageBonus,    // +X% damage for specific element moves
	ElementResistance,     // -X% damage taken from specific element

	// Stat modifiers (applied at battle start or conditionally)
	ATKBonus,
	DEFBonus,
	SpABonus,
	SpDBonusBattle,        // Named differently to avoid conflict with SPD stat
	SPDBonus,

	// Combat mechanics
	EvasionBonus,          // +X% chance to dodge attacks
	AccuracyBonus,         // +X% accuracy on moves
	CritBonus,             // +X% critical hit chance
	CritDamageBonus,       // +X% critical hit damage multiplier

	// Status-related
	StatusResistance,      // -X% chance to be affected by status conditions
	StatusDurationBonus,   // +X turns on inflicted status conditions

	// Conditional bonuses (activated when condition is met)
	LowHPATKBonus,         // +X% ATK when HP below 33%
	LowHPSpABonus,         // +X% SpA when HP below 33%
	LowHPDEFBonus,         // +X% DEF when HP below 33%
	LowHPSpDBonus,         // +X% SpD when HP below 33%
	LowHPSPDBonus,         // +X% SPD when HP below 33%

	HighHPBonus,           // +X% damage when HP above 80%
	FirstTurnBonus,        // +X% damage on first turn of battle
	LastStandBonus,        // +X% damage when last monster standing
	RevengeBonus,          // +X% damage after ally faints

	// Special mechanics
	PriorityBonus,         // +X to move priority
	HealingBonus,          // +X% effectiveness on healing moves
	RecoilReduction,       // -X% recoil damage taken
	ContactDamage,         // Deal X% damage to attackers that make contact
	DrainBonus,            // +X% HP recovered from drain moves

	// Element immunity/absorption
	ElementImmunity,       // Immune to specific element (takes 0 damage)
	ElementAbsorption      // Absorb specific element (heal instead of damage)
}

/// <summary>
/// A single effect that a trait provides
/// </summary>
public class TraitEffect
{
	/// <summary>
	/// The type of effect
	/// </summary>
	public TraitEffectType Type { get; set; }

	/// <summary>
	/// Value of the effect (usually a percentage)
	/// e.g., 15 = 15% bonus, 50 = 50% bonus
	/// </summary>
	public float Value { get; set; }

	/// <summary>
	/// If set, this effect only applies to a specific element
	/// Used with ElementDamageBonus, ElementResistance, etc.
	/// </summary>
	public ElementType? AffectedElement { get; set; }

	/// <summary>
	/// Optional condition string for conditional effects
	/// Examples: "below_33_hp", "above_80_hp", "first_turn", "last_standing"
	/// </summary>
	public string Condition { get; set; }
}

/// <summary>
/// Static definition of a trait - defines what passive effects it provides
/// </summary>
public class TraitDefinition
{
	/// <summary>
	/// Unique identifier (matches the string stored in Monster.Traits)
	/// e.g., "ember_heart", "shadow_step", "blaze"
	/// </summary>
	public string Id { get; set; }

	/// <summary>
	/// Display name (e.g., "Ember Heart", "Shadow Step", "Blaze")
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Description of what the trait does
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// List of effects this trait provides
	/// A trait can have multiple effects
	/// </summary>
	public List<TraitEffect> Effects { get; set; } = new();

	/// <summary>
	/// Whether this trait is hidden from the player until discovered
	/// </summary>
	public bool IsHidden { get; set; } = false;

	/// <summary>
	/// Rarity of this trait (affects breeding inheritance chances)
	/// </summary>
	public TraitRarity Rarity { get; set; } = TraitRarity.Common;
}

/// <summary>
/// Rarity tiers for traits (affects inheritance and appearance rates)
/// </summary>
public enum TraitRarity
{
	Common,      // 60% inheritance chance
	Uncommon,    // 40% inheritance chance
	Rare,        // 25% inheritance chance
	Epic,        // 10% inheritance chance
	Legendary    // 5% inheritance chance (hidden ability equivalent)
}
