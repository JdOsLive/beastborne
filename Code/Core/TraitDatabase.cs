using System.Collections.Generic;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// Static database of all trait definitions with specific, clear effects
/// </summary>
public static class TraitDatabase
{
	private static Dictionary<string, TraitDefinition> _traits;

	public static IReadOnlyDictionary<string, TraitDefinition> AllTraits
	{
		get
		{
			if ( _traits == null )
				InitializeTraits();
			return _traits;
		}
	}

	public static TraitDefinition GetTrait( string id )
	{
		if ( _traits == null )
			InitializeTraits();
		return _traits.TryGetValue( id, out var trait ) ? trait : null;
	}

	public static IEnumerable<TraitDefinition> GetTraitsByRarity( TraitRarity rarity )
	{
		foreach ( var trait in AllTraits.Values )
		{
			if ( trait.Rarity == rarity )
				yield return trait;
		}
	}

	private static void InitializeTraits()
	{
		_traits = new Dictionary<string, TraitDefinition>();

		// ============================================
		// ELEMENT POWER TRAITS (Boost same-element moves)
		// ============================================

		AddTrait( new TraitDefinition
		{
			Id = "ember_heart",
			Name = "Ember Heart",
			Description = "Fire-type moves deal +15% damage.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 15, AffectedElement = ElementType.Fire }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "torrent_soul",
			Name = "Torrent Soul",
			Description = "Water-type moves deal +15% damage.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 15, AffectedElement = ElementType.Water }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "terra_force",
			Name = "Terra Force",
			Description = "Earth-type moves deal +15% damage.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 15, AffectedElement = ElementType.Earth }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "gale_spirit",
			Name = "Gale Spirit",
			Description = "Wind-type moves deal +15% damage.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 15, AffectedElement = ElementType.Wind }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "static_charge",
			Name = "Static Charge",
			Description = "Electric-type moves deal +15% damage.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 15, AffectedElement = ElementType.Electric }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "frost_core",
			Name = "Frost Core",
			Description = "Ice-type moves deal +15% damage.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 15, AffectedElement = ElementType.Ice }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "verdant_power",
			Name = "Verdant Power",
			Description = "Nature-type moves deal +15% damage.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 15, AffectedElement = ElementType.Nature }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "iron_will",
			Name = "Iron Will",
			Description = "Metal-type moves deal +15% damage.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 15, AffectedElement = ElementType.Metal }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "dark_presence",
			Name = "Dark Presence",
			Description = "Shadow-type moves deal +15% damage.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 15, AffectedElement = ElementType.Shadow }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "ethereal_blessing",
			Name = "Ethereal Blessing",
			Description = "Spirit-type moves deal +15% damage.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 15, AffectedElement = ElementType.Spirit }
			}
		} );

		// ============================================
		// LOW HP POWER TRAITS (Desperation abilities)
		// ============================================

		AddTrait( new TraitDefinition
		{
			Id = "infernal_rage",
			Name = "Infernal Rage",
			Description = "Fire-type moves deal +50% damage when HP is below 33%.",
			Rarity = TraitRarity.Uncommon,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 50, AffectedElement = ElementType.Fire, Condition = "below_33_hp" }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "kindle_heart",
			Name = "Kindle Heart",
			Description = "Fire-type moves deal +15% damage.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 15, AffectedElement = ElementType.Fire }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "tidal_wrath",
			Name = "Tidal Wrath",
			Description = "Water-type moves deal +50% damage when HP is below 33%.",
			Rarity = TraitRarity.Uncommon,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 50, AffectedElement = ElementType.Water, Condition = "below_33_hp" }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "wild_growth",
			Name = "Wild Growth",
			Description = "Nature-type moves deal +50% damage when HP is below 33%.",
			Rarity = TraitRarity.Uncommon,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementDamageBonus, Value = 50, AffectedElement = ElementType.Nature, Condition = "below_33_hp" }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "wild_harden",
			Name = "Wild Harden",
			Description = "DEF and SpD are increased by +20%.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.DEFBonus, Value = 20 },
				new TraitEffect { Type = TraitEffectType.SpDBonusBattle, Value = 20 }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "last_stand",
			Name = "Last Stand",
			Description = "ATK and SpA are increased by +30% when HP is below 33%.",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.LowHPATKBonus, Value = 30, Condition = "below_33_hp" },
				new TraitEffect { Type = TraitEffectType.LowHPSpABonus, Value = 30, Condition = "below_33_hp" }
			}
		} );

		// ============================================
		// STAT BONUS TRAITS
		// ============================================

		AddTrait( new TraitDefinition
		{
			Id = "titanic_might",
			Name = "Titanic Might",
			Description = "ATK is permanently doubled (+100%).",
			Rarity = TraitRarity.Legendary,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ATKBonus, Value = 100 }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "thermal_hide",
			Name = "Thermal Hide",
			Description = "Takes 50% less damage from Fire and Ice moves.",
			Rarity = TraitRarity.Uncommon,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementResistance, Value = 50, AffectedElement = ElementType.Fire },
				new TraitEffect { Type = TraitEffectType.ElementResistance, Value = 50, AffectedElement = ElementType.Ice }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "enduring_will",
			Name = "Enduring Will",
			Description = "Cannot be knocked out in one hit from full HP. Survives with 1 HP.",
			Rarity = TraitRarity.Rare,
			IsHidden = true,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.DamageReduction, Value = 100, Condition = "ohko_protection" }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "momentum",
			Name = "Momentum",
			Description = "SPD increases by +10% at the end of each turn. Stacks up to 5 times.",
			Rarity = TraitRarity.Epic,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.SPDBonus, Value = 10, Condition = "per_turn" }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "menacing_aura",
			Name = "Menacing Aura",
			Description = "Enemy ATK is reduced by 20% while this beast is active.",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ATKBonus, Value = -20, Condition = "on_enemy" }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "bloodlust",
			Name = "Bloodlust",
			Description = "ATK increases by +30% after knocking out an enemy. Resets on switch.",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ATKBonus, Value = 30, Condition = "on_ko" }
			}
		} );

		// ============================================
		// COMBAT MECHANIC TRAITS
		// ============================================

		AddTrait( new TraitDefinition
		{
			Id = "fortunate_strike",
			Name = "Fortunate Strike",
			Description = "Critical hit chance increased by +15% (base 6.25% → 21.25%).",
			Rarity = TraitRarity.Uncommon,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.CritBonus, Value = 15 }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "precision_hunter",
			Name = "Precision Hunter",
			Description = "Critical hits deal +50% more damage (1.5x → 2.25x).",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.CritDamageBonus, Value = 50 }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "phantom_step",
			Name = "Phantom Step",
			Description = "+10% chance to evade incoming attacks.",
			Rarity = TraitRarity.Uncommon,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.EvasionBonus, Value = 10 }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "hunters_focus",
			Name = "Hunter's Focus",
			Description = "Move accuracy is increased by +10%.",
			Rarity = TraitRarity.Common,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.AccuracyBonus, Value = 10 }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "trickster",
			Name = "Trickster",
			Description = "Status moves gain +1 priority (act before normal priority moves).",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.PriorityBonus, Value = 1, Condition = "status_moves" }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "adrenaline_rush",
			Name = "Adrenaline Rush",
			Description = "SPD is increased by +50% while afflicted with a status condition.",
			Rarity = TraitRarity.Uncommon,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.SPDBonus, Value = 50, Condition = "has_status" }
			}
		} );

		// ============================================
		// DEFENSIVE TRAITS
		// ============================================

		AddTrait( new TraitDefinition
		{
			Id = "barbed_hide",
			Name = "Barbed Hide",
			Description = "Attackers take 12% of their max HP as damage when using contact moves.",
			Rarity = TraitRarity.Uncommon,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ContactDamage, Value = 12 }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "cleansing_retreat",
			Name = "Cleansing Retreat",
			Description = "All status conditions are cured when switching out.",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.StatusResistance, Value = 100, Condition = "on_switch" }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "hardened_resolve",
			Name = "Hardened Resolve",
			Description = "DEF is increased by +50% while afflicted with a status condition.",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.DEFBonus, Value = 50, Condition = "has_status" }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "reckless_charge",
			Name = "Reckless Charge",
			Description = "Recoil damage from moves is reduced to 0.",
			Rarity = TraitRarity.Uncommon,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.RecoilReduction, Value = 100 }
			}
		} );

		// ============================================
		// SPECIAL/UNIQUE TRAITS
		// ============================================

		AddTrait( new TraitDefinition
		{
			Id = "skyborne",
			Name = "Skyborne",
			Description = "Immune to Earth-type moves (takes 0 damage).",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementImmunity, AffectedElement = ElementType.Earth }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "flame_eater",
			Name = "Flame Eater",
			Description = "Fire-type moves heal this beast for 25% of max HP instead of dealing damage.",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementAbsorption, AffectedElement = ElementType.Fire }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "aqua_siphon",
			Name = "Aqua Siphon",
			Description = "Water-type moves heal this beast for 25% of max HP instead of dealing damage.",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementAbsorption, AffectedElement = ElementType.Water }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "lightning_rod",
			Name = "Lightning Rod",
			Description = "Electric-type moves heal this beast for 25% of max HP instead of dealing damage.",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.ElementAbsorption, AffectedElement = ElementType.Electric }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "elemental_mastery",
			Name = "Elemental Mastery",
			Description = "STAB (Same-Type Attack Bonus) is increased from 1.5x to 2x damage.",
			Rarity = TraitRarity.Epic,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.DamageBonus, Value = 33, Condition = "same_type" }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "subtle_arts",
			Name = "Subtle Arts",
			Description = "Moves with base power 60 or less deal +50% damage.",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.DamageBonus, Value = 50, Condition = "base_power_60_or_less" }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "brutal_force",
			Name = "Brutal Force",
			Description = "Moves with secondary effects (burn chance, stat drops, etc.) deal +30% damage.",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.DamageBonus, Value = 30, Condition = "has_secondary_effect" }
			}
		} );

		AddTrait( new TraitDefinition
		{
			Id = "vital_recovery",
			Name = "Vital Recovery",
			Description = "Heals 33% of max HP when switching out.",
			Rarity = TraitRarity.Rare,
			Effects = new()
			{
				new TraitEffect { Type = TraitEffectType.HealingBonus, Value = 33, Condition = "on_switch" }
			}
		} );
	}

	private static void AddTrait( TraitDefinition trait )
	{
		_traits[trait.Id] = trait;
	}
}
