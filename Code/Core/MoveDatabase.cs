using System.Collections.Generic;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// Static database of all move definitions - unique to Beastborne
/// </summary>
public static class MoveDatabase
{
	private static Dictionary<string, MoveDefinition> _moves;

	public static IReadOnlyDictionary<string, MoveDefinition> AllMoves
	{
		get
		{
			if ( _moves == null )
				InitializeMoves();
			return _moves;
		}
	}

	public static MoveDefinition GetMove( string id )
	{
		if ( _moves == null )
			InitializeMoves();
		return _moves.TryGetValue( id, out var move ) ? move : null;
	}

	public static IEnumerable<MoveDefinition> GetMovesByElement( ElementType element )
	{
		foreach ( var move in AllMoves.Values )
		{
			if ( move.Element == element )
				yield return move;
		}
	}

	private static void InitializeMoves()
	{
		_moves = new Dictionary<string, MoveDefinition>();

		// ============================================
		// NEUTRAL MOVES (Universal)
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "strike",
			Name = "Strike",
			Description = "A straightforward physical blow.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Physical,
			BasePower = 35,
			Accuracy = 100,
			MaxPP = 35
		} );

		AddMove( new MoveDefinition
		{
			Id = "rend",
			Name = "Rend",
			Description = "Tears at the target with claws or fangs.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Physical,
			BasePower = 40,
			Accuracy = 100,
			MaxPP = 30
		} );

		AddMove( new MoveDefinition
		{
			Id = "swift_lunge",
			Name = "Swift Lunge",
			Description = "A quick strike that acts before most moves. +1 priority.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Physical,
			BasePower = 35,
			Accuracy = 100,
			MaxPP = 30,
			Priority = 1
		} );

		AddMove( new MoveDefinition
		{
			Id = "vicious_cut",
			Name = "Vicious Cut",
			Description = "A brutal slash. +1 crit stage.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Physical,
			BasePower = 60,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.CritBoost, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "crushing_blow",
			Name = "Crushing Blow",
			Description = "A heavy impact. 30% chance to paralyze.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Physical,
			BasePower = 75,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "annihilate",
			Name = "Annihilate",
			Description = "Devastating force. User cannot act next turn.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Special,
			BasePower = 135,
			Accuracy = 90,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recharge } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "intimidate",
			Name = "Intimidate",
			Description = "A menacing growl. Lowers target ATK and SpA by 1 stage each.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 30,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.LowerATK, Value = 1 },
				new MoveEffect { Type = MoveEffectType.LowerSpA, Value = 1 }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "harden",
			Name = "Harden",
			Description = "Tenses muscles. Raises user DEF and SpD by 1 stage each.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 30,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 1, TargetsSelf = true }
			}
		} );

		// ============================================
		// FIRE MOVES
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "kindle",
			Name = "Kindle",
			Description = "Sparks a small flame. 10% burn chance.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 35,
			Accuracy = 100,
			MaxPP = 25,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "searing_rush",
			Name = "Searing Rush",
			Description = "Charges with flames wreathing the body.",
			Element = ElementType.Fire,
			Category = MoveCategory.Physical,
			BasePower = 60,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "pyre_fangs",
			Name = "Pyre Fangs",
			Description = "Bites with superheated jaws. 10% burn, 10% flinch.",
			Element = ElementType.Fire,
			Category = MoveCategory.Physical,
			BasePower = 60,
			Accuracy = 95,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.1f },
				new MoveEffect { Type = MoveEffectType.Flinch, Chance = 0.1f }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "conflagration",
			Name = "Conflagration",
			Description = "Engulfs target in roaring flames. 100% burn. 50% accuracy.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 90,
			Accuracy = 50,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 1.0f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "blazing_wrath",
			Name = "Blazing Wrath",
			Description = "Unleashes furious flames. 10% burn chance.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 100,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "cinders_curse",
			Name = "Cinder's Curse",
			Description = "Ghostly flames that guarantee a burn. 85% accuracy.",
			Element = ElementType.Fire,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 85,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 1.0f } }
		} );

		// ============================================
		// WATER MOVES
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "splash_jet",
			Name = "Splash Jet",
			Description = "A focused stream of water.",
			Element = ElementType.Water,
			Category = MoveCategory.Special,
			BasePower = 35,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "froth_barrage",
			Name = "Froth Barrage",
			Description = "Rapid bubbles. 10% chance to lower target SPD by 1.",
			Element = ElementType.Water,
			Category = MoveCategory.Special,
			BasePower = 60,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSPD, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "tidal_slam",
			Name = "Tidal Slam",
			Description = "Crashes down with a wave-like force.",
			Element = ElementType.Water,
			Category = MoveCategory.Physical,
			BasePower = 80,
			Accuracy = 90,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "deluge",
			Name = "Deluge",
			Description = "A massive torrent that overwhelms the target.",
			Element = ElementType.Water,
			Category = MoveCategory.Special,
			BasePower = 100,
			Accuracy = 80,
			MaxPP = 5
		} );

		AddMove( new MoveDefinition
		{
			Id = "aqua_strike",
			Name = "Aqua Strike",
			Description = "Slams the target with a water-coated limb.",
			Element = ElementType.Water,
			Category = MoveCategory.Physical,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "monsoon_call",
			Name = "Monsoon Call",
			Description = "Summons rain. Boosts Water moves for 5 turns.",
			Element = ElementType.Water,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Weather, Value = 1, Duration = 5 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "terra_pulse",
			Name = "Terra Pulse",
			Description = "Channels earth energy into a focused blast.",
			Element = ElementType.Earth,
			Category = MoveCategory.Special,
			BasePower = 65,
			Accuracy = 100,
			MaxPP = 15
		} );

		// ============================================
		// EARTH MOVES
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "mud_hurl",
			Name = "Mud Hurl",
			Description = "Throws mud. Lowers target accuracy by 1 stage.",
			Element = ElementType.Earth,
			Category = MoveCategory.Special,
			BasePower = 20,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerAccuracy, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "boulder_toss",
			Name = "Boulder Toss",
			Description = "Hurls a heavy rock.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 40,
			Accuracy = 90,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "earthrend",
			Name = "Earthrend",
			Description = "Tears up the ground. Lowers target SPD by 1.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 50,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSPD, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "seismic_crash",
			Name = "Seismic Crash",
			Description = "A devastating quake.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 90,
			Accuracy = 100,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "jagged_spike",
			Name = "Jagged Spike",
			Description = "Sharp stone eruption. +1 crit stage.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 90,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.CritBoost, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "stone_wall",
			Name = "Stone Wall",
			Description = "Raises a wall of stone. Raises user DEF by 1 and lowers target SPD by 1.",
			Element = ElementType.Earth,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.LowerSPD, Value = 1 }
			}
		} );

		// ============================================
		// WIND MOVES
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "breeze_cut",
			Name = "Breeze Cut",
			Description = "A sharp gust of wind.",
			Element = ElementType.Wind,
			Category = MoveCategory.Special,
			BasePower = 35,
			Accuracy = 100,
			MaxPP = 35
		} );

		AddMove( new MoveDefinition
		{
			Id = "razor_gale",
			Name = "Razor Gale",
			Description = "Cutting winds. +1 crit stage.",
			Element = ElementType.Wind,
			Category = MoveCategory.Special,
			BasePower = 50,
			Accuracy = 95,
			MaxPP = 25,
			Effects = new() { new MoveEffect { Type = MoveEffectType.CritBoost, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "dive_strike",
			Name = "Dive Strike",
			Description = "An aerial assault that never misses.",
			Element = ElementType.Wind,
			Category = MoveCategory.Physical,
			BasePower = 50,
			Accuracy = 101, // 101 = always hits
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "tempest",
			Name = "Tempest",
			Description = "A violent storm. 30% confusion chance.",
			Element = ElementType.Wind,
			Category = MoveCategory.Special,
			BasePower = 100,
			Accuracy = 70,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Confuse, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "gale_slam",
			Name = "Gale Slam",
			Description = "Slams down with the force of a hurricane.",
			Element = ElementType.Wind,
			Category = MoveCategory.Physical,
			BasePower = 80,
			Accuracy = 90,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "updraft",
			Name = "Updraft",
			Description = "Rising winds. Raises user SPD by 2 stages for 4 turns.",
			Element = ElementType.Wind,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 2, Duration = 4, TargetsSelf = true } }
		} );

		// ============================================
		// ELECTRIC MOVES
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "static_jolt",
			Name = "Static Jolt",
			Description = "A quick shock. 10% paralysis chance.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 35,
			Accuracy = 100,
			MaxPP = 30,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "volt_charge",
			Name = "Volt Charge",
			Description = "An electrified tackle. 30% paralysis chance.",
			Element = ElementType.Electric,
			Category = MoveCategory.Physical,
			BasePower = 60,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "arc_bolt",
			Name = "Arc Bolt",
			Description = "A powerful lightning arc. 10% paralysis chance.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 80,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "storm_strike",
			Name = "Storm Strike",
			Description = "A devastating bolt from above. 30% paralysis. 70% accuracy.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 100,
			Accuracy = 70,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "thunder_fang",
			Name = "Thunder Fang",
			Description = "Bites with electrified jaws. 10% paralysis chance.",
			Element = ElementType.Electric,
			Category = MoveCategory.Physical,
			BasePower = 85,
			Accuracy = 95,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "nerve_lock",
			Name = "Nerve Lock",
			Description = "Disabling current. Guarantees paralysis. 90% accuracy.",
			Element = ElementType.Electric,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 90,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 1.0f } }
		} );

		// ============================================
		// ICE MOVES
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "frost_breath",
			Name = "Frost Breath",
			Description = "A chilling exhale. 10% freeze chance.",
			Element = ElementType.Ice,
			Category = MoveCategory.Special,
			BasePower = 35,
			Accuracy = 100,
			MaxPP = 25,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "glacial_bite",
			Name = "Glacial Bite",
			Description = "Freezing jaws. 10% freeze, 10% flinch.",
			Element = ElementType.Ice,
			Category = MoveCategory.Physical,
			BasePower = 60,
			Accuracy = 95,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.1f },
				new MoveEffect { Type = MoveEffectType.Flinch, Chance = 0.1f }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "permafrost_ray",
			Name = "Permafrost Ray",
			Description = "An intensely cold beam. 10% freeze chance.",
			Element = ElementType.Ice,
			Category = MoveCategory.Special,
			BasePower = 80,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "avalanche_wrath",
			Name = "Avalanche Wrath",
			Description = "A howling frozen storm. 10% freeze. 70% accuracy.",
			Element = ElementType.Ice,
			Category = MoveCategory.Special,
			BasePower = 100,
			Accuracy = 70,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "frost_crush",
			Name = "Frost Crush",
			Description = "A freezing body slam. 10% freeze chance.",
			Element = ElementType.Ice,
			Category = MoveCategory.Physical,
			BasePower = 85,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "winter_veil",
			Name = "Winter Veil",
			Description = "Summons hail for 5 turns.",
			Element = ElementType.Ice,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Weather, Value = 2, Duration = 5 } }
		} );

		// ============================================
		// NATURE MOVES
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "thorn_lash",
			Name = "Thorn Lash",
			Description = "Strikes with thorny tendrils.",
			Element = ElementType.Nature,
			Category = MoveCategory.Physical,
			BasePower = 40,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "blade_leaf",
			Name = "Blade Leaf",
			Description = "Sharp foliage. +1 crit stage.",
			Element = ElementType.Nature,
			Category = MoveCategory.Physical,
			BasePower = 45,
			Accuracy = 95,
			MaxPP = 25,
			Effects = new() { new MoveEffect { Type = MoveEffectType.CritBoost, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "vitality_burst",
			Name = "Vitality Burst",
			Description = "Concentrated life force. 10% chance to lower target SpD by 1.",
			Element = ElementType.Nature,
			Category = MoveCategory.Special,
			BasePower = 80,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpD, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "solstice_beam",
			Name = "Solstice Beam",
			Description = "A powerful nature beam. Requires charging first turn.",
			Element = ElementType.Nature,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Charge } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "vine_crush",
			Name = "Vine Crush",
			Description = "Constricts the target with powerful vines.",
			Element = ElementType.Nature,
			Category = MoveCategory.Physical,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "root_bind",
			Name = "Root Bind",
			Description = "Entangling roots that drain HP each turn.",
			Element = ElementType.Nature,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LifeSiphon } }
		} );

		// ============================================
		// METAL MOVES
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "steel_rake",
			Name = "Steel Rake",
			Description = "Metallic claws. 10% chance to raise user ATK by 1.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 40,
			Accuracy = 95,
			MaxPP = 35,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseATK, Chance = 0.1f, Value = 1, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "iron_rush",
			Name = "Iron Rush",
			Description = "A steel-hard headbutt. 30% flinch chance.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Flinch, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "gleaming_ray",
			Name = "Gleaming Ray",
			Description = "A metallic beam. 10% chance to lower target SpD by 1.",
			Element = ElementType.Metal,
			Category = MoveCategory.Special,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpD, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "alloy_smash",
			Name = "Alloy Smash",
			Description = "A heavy metal tail. 30% chance to lower target DEF by 1. 75% accuracy.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 90,
			Accuracy = 75,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 0.3f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "meltdown_beam",
			Name = "Meltdown Beam",
			Description = "A superheated metal ray. 20% burn chance.",
			Element = ElementType.Metal,
			Category = MoveCategory.Special,
			BasePower = 95,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "magnet_rise",
			Name = "Magnet Rise",
			Description = "Generates a magnetic field. Raises user SpA by 1 and SPD by 1.",
			Element = ElementType.Metal,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseSpA, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "temper",
			Name = "Temper",
			Description = "Hardens the body. Raises user DEF by 2 stages.",
			Element = ElementType.Metal,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 2, TargetsSelf = true } }
		} );

		// ============================================
		// SHADOW MOVES
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "shade_step",
			Name = "Shade Step",
			Description = "Strikes from the shadows. +1 priority.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Physical,
			BasePower = 35,
			Accuracy = 100,
			MaxPP = 30,
			Priority = 1
		} );

		AddMove( new MoveDefinition
		{
			Id = "umbral_claw",
			Name = "Umbral Claw",
			Description = "Shadow-infused claws. +1 crit stage.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Physical,
			BasePower = 60,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.CritBoost, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "void_sphere",
			Name = "Void Sphere",
			Description = "A ball of darkness. 20% chance to lower target SpD by 1.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpD, Chance = 0.2f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "nightmare_wave",
			Name = "Nightmare Wave",
			Description = "Dark thoughts made manifest. 20% flinch chance.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Flinch, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "abyssal_torrent",
			Name = "Abyssal Torrent",
			Description = "A surge of pure darkness. 20% chance to lower target SpD by 1.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 95,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpD, Chance = 0.2f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "terror_visions",
			Name = "Terror Visions",
			Description = "Dark visions haunt the target. 50% sleep chance, then torments sleepers.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.Sleep, Chance = 0.5f },
				new MoveEffect { Type = MoveEffectType.Torment }
			}
		} );

		// ============================================
		// SPIRIT MOVES
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "soul_siphon",
			Name = "Soul Siphon",
			Description = "Drains life force. Heals 25% of damage dealt.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 35,
			Accuracy = 100,
			MaxPP = 25,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Drain, Value = 0.25f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "aether_pulse",
			Name = "Aether Pulse",
			Description = "A sphere of pure spirit energy. Never misses.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 70,
			Accuracy = 101, // Always hits
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "lunar_radiance",
			Name = "Lunar Radiance",
			Description = "Moonlit power. 30% chance to lower target SpA by 1.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 80,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpA, Chance = 0.3f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "spirit_rend",
			Name = "Spirit Rend",
			Description = "Tears at the soul. Lowers target SpA by 1.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Physical,
			BasePower = 65,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpA, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "spectral_rush",
			Name = "Spectral Rush",
			Description = "A ghostly charge that phases through defenses. 10% chance to lower target DEF by 1.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Physical,
			BasePower = 85,
			Accuracy = 95,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "divine_grace",
			Name = "Divine Grace",
			Description = "Channels healing. Restores HP next turn.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.DelayedHeal } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "cleansing_light",
			Name = "Cleansing Light",
			Description = "Purifies status ailments from self.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Cleanse, TargetsSelf = true } }
		} );

		// ============================================
		// ADDITIONAL UTILITY MOVES
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "brace",
			Name = "Brace",
			Description = "Braces for impact. Blocks damage this turn. +4 priority. May fail if used repeatedly.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 10,
			Priority = 4,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Guard } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "deep_slumber",
			Name = "Deep Slumber",
			Description = "Sleeps for 2 turns. Fully restores HP.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.FullHeal, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.Sleep, Duration = 2, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "phantom_double",
			Name = "Phantom Double",
			Description = "Creates a decoy using 25% of max HP.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Decoy, Value = 0.25f } }
		} );

		// ============================================
		// FIRE MOVES (Extended)
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "flame_vortex",
			Name = "Flame Vortex",
			Description = "Traps the target in a vortex of flames that burns each turn.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 35,
			Accuracy = 85,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LifeSiphon } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "rift_flame",
			Name = "Rift Flame",
			Description = "A tear of fire from another dimension.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 55,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "scorching_gust",
			Name = "Scorching Gust",
			Description = "A scorching blast of hot air. 10% burn chance.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 95,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "solar_flare",
			Name = "Solar Flare",
			Description = "Channels the sun's fury into a searing blast. 20% burn chance.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 100,
			Accuracy = 90,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "inferno_blitz",
			Name = "Inferno Blitz",
			Description = "A reckless charge wreathed in white-hot flame. Takes recoil damage.",
			Element = ElementType.Fire,
			Category = MoveCategory.Physical,
			BasePower = 110,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recoil, Value = 0.33f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "primordial_flame",
			Name = "Primordial Flame",
			Description = "Flames from the dawn of creation. 30% burn chance.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 120,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "solar_reign",
			Name = "Solar Reign",
			Description = "Intensifies sunlight for 5 turns. Boosts Fire moves.",
			Element = ElementType.Fire,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Weather, Value = 4, Duration = 5 } }
		} );

		// ============================================
		// WATER MOVES (Extended)
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "tidal_pulse",
			Name = "Tidal Pulse",
			Description = "An ultrasonic wave of water. 20% confusion chance.",
			Element = ElementType.Water,
			Category = MoveCategory.Special,
			BasePower = 60,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Confuse, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "mist_veil",
			Name = "Mist Veil",
			Description = "Surrounds self in a healing veil of water. Restores HP over time.",
			Element = ElementType.Water,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Heal, Value = 25, TargetsSelf = true } }
		} );

		// ============================================
		// EARTH MOVES (Extended)
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "primeval_force",
			Name = "Primeval Force",
			Description = "Attacks with prehistoric energy. 10% chance to raise all stats.",
			Element = ElementType.Earth,
			Category = MoveCategory.Special,
			BasePower = 60,
			Accuracy = 100,
			MaxPP = 5,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseATK, Chance = 0.1f, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseDEF, Chance = 0.1f, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpA, Chance = 0.1f, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Chance = 0.1f, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSPD, Chance = 0.1f, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "world_crush",
			Name = "World Crush",
			Description = "Brings down the weight of the world. User must recharge next turn.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 120,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recharge } }
		} );

		// ============================================
		// WIND MOVES (Extended)
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "phase_shift",
			Name = "Phase Shift",
			Description = "Shifts between dimensions. Raises evasion and SPD by 1.",
			Element = ElementType.Wind,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseEvasion, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 1, TargetsSelf = true }
			}
		} );

		// ============================================
		// ELECTRIC MOVES (Extended)
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "quantum_flux",
			Name = "Quantum Flux",
			Description = "Unstable electric energy. 20% confusion chance.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 90,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Confuse, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "storm_surge",
			Name = "Storm Surge",
			Description = "Calls down a massive lightning storm. 30% paralysis chance.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "dimensional_rift",
			Name = "Dimensional Rift",
			Description = "Tears open a rift of pure electricity. 10% paralysis chance.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.1f } }
		} );

		// ============================================
		// ICE MOVES (Extended)
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "absolute_zero",
			Name = "Absolute Zero",
			Description = "The coldest force in existence. 20% freeze chance.",
			Element = ElementType.Ice,
			Category = MoveCategory.Special,
			BasePower = 120,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.2f } }
		} );

		// ============================================
		// NATURE MOVES (Extended)
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "pollen_burst",
			Name = "Pollen Burst",
			Description = "Releases a cloud of toxic pollen.",
			Element = ElementType.Nature,
			Category = MoveCategory.Special,
			BasePower = 55,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "bloom_burst",
			Name = "Bloom Burst",
			Description = "An eruption of blooming energy. 10% chance to raise SpA.",
			Element = ElementType.Nature,
			Category = MoveCategory.Special,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSpA, Chance = 0.1f, Value = 1, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "thorn_barrage",
			Name = "Thorn Barrage",
			Description = "Launches a volley of sharp thorns. +1 crit stage.",
			Element = ElementType.Nature,
			Category = MoveCategory.Physical,
			BasePower = 85,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.CritBoost, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "verdant_edge",
			Name = "Verdant Edge",
			Description = "Slashes with a razor-sharp leaf. +1 crit stage.",
			Element = ElementType.Nature,
			Category = MoveCategory.Physical,
			BasePower = 90,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.CritBoost, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "blossom_frenzy",
			Name = "Blossom Frenzy",
			Description = "A wild flurry of petals. Confuses the user afterward.",
			Element = ElementType.Nature,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Confuse, Chance = 1.0f, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "timber_slam",
			Name = "Timber Slam",
			Description = "A devastating trunk strike. Takes heavy recoil damage.",
			Element = ElementType.Nature,
			Category = MoveCategory.Physical,
			BasePower = 110,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recoil, Value = 0.33f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "nature_shield",
			Name = "Nature Shield",
			Description = "A barrier of living vines. Raises user DEF and SpD by 1.",
			Element = ElementType.Nature,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "parasite_spore",
			Name = "Parasite Spore",
			Description = "Plants a spore that drains the target's HP each turn.",
			Element = ElementType.Nature,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LifeSiphon } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "floral_guard",
			Name = "Floral Guard",
			Description = "Wraps in dense petals. Raises user DEF by 3 stages.",
			Element = ElementType.Nature,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 3, TargetsSelf = true } }
		} );

		// ============================================
		// METAL MOVES (Extended)
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "rapid_shear",
			Name = "Rapid Shear",
			Description = "Quick metallic slashes.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 40,
			Accuracy = 95,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "chrome_wing",
			Name = "Chrome Wing",
			Description = "Strikes with hardened wings. 10% chance to raise user DEF.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 70,
			Accuracy = 90,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseDEF, Chance = 0.1f, Value = 1, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "cog_crush",
			Name = "Cog Crush",
			Description = "Grinds the target between spinning gears.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 80,
			Accuracy = 85,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "corrosive_breath",
			Name = "Corrosive Breath",
			Description = "Exhales caustic metallic fumes. 30% chance to lower target DEF.",
			Element = ElementType.Metal,
			Category = MoveCategory.Special,
			BasePower = 80,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 0.3f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "spin_crash",
			Name = "Spin Crash",
			Description = "Crashes into the target with a spinning body.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 80,
			Accuracy = 100,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "iron_rebound",
			Name = "Iron Rebound",
			Description = "Returns damage with metallic force. Lowers target ATK by 1.",
			Element = ElementType.Metal,
			Category = MoveCategory.Special,
			BasePower = 75,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerATK, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "titan_drop",
			Name = "Titan Drop",
			Description = "Drops with tremendous weight on the target. 20% flinch chance.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 95,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Flinch, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "starfall_strike",
			Name = "Starfall Strike",
			Description = "Strikes with the force of a meteor. 20% chance to raise user ATK.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 100,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseATK, Chance = 0.2f, Value = 1, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "resonance_bell",
			Name = "Resonance Bell",
			Description = "A soothing bell tone. Cures status conditions.",
			Element = ElementType.Metal,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Cleanse, TargetsSelf = true } }
		} );

		// ============================================
		// SHADOW MOVES (Extended)
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "dusk_bolt",
			Name = "Dusk Bolt",
			Description = "Strikes with sinister shadows.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 50,
			Accuracy = 100,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "sly_strike",
			Name = "Sly Strike",
			Description = "Approaches the target disarmingly then strikes. Never misses.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Physical,
			BasePower = 60,
			Accuracy = 101,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "shadow_lunge",
			Name = "Shadow Lunge",
			Description = "Strikes first from the shadows. +1 priority.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Physical,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 10,
			Priority = 1
		} );

		AddMove( new MoveDefinition
		{
			Id = "void_wings",
			Name = "Void Wings",
			Description = "Dark wings drain the target's life force. Heals 50% of damage dealt.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 80,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Drain, Value = 0.5f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "void_pulse",
			Name = "Void Pulse",
			Description = "A pulse of emptiness. 10% chance to lower target SpD.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 85,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpD, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "void_tear",
			Name = "Void Tear",
			Description = "Rips at the target with claws of nothingness. 20% chance to lower DEF.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Physical,
			BasePower = 85,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 0.2f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "eclipse_ray",
			Name = "Eclipse Ray",
			Description = "A beam of eclipsed light. 20% chance to lower target SpD.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 95,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpD, Chance = 0.2f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "void_bloom",
			Name = "Void Bloom",
			Description = "Dark flowers erupt with shadow energy. 20% chance to lower SpD.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 100,
			Accuracy = 90,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpD, Chance = 0.2f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "absolute_void",
			Name = "Absolute Void",
			Description = "Erases the target from existence. User must recharge.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 130,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recharge } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "cosmic_erasure",
			Name = "Cosmic Erasure",
			Description = "Wipes the target from the cosmic record. User must recharge.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 130,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recharge } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "dark_scheme",
			Name = "Dark Scheme",
			Description = "Schemes dark thoughts. Raises user SpA by 2 stages.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSpA, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "dread_gaze",
			Name = "Dread Gaze",
			Description = "A terrifying visage. Lowers target SPD by 2 stages.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSPD, Value = 2 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "fated_curse",
			Name = "Fated Curse",
			Description = "Binds fates together. Curses the target, lowering ATK and SpA.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 5,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.LowerATK, Value = 1 },
				new MoveEffect { Type = MoveEffectType.LowerSpA, Value = 1 }
			}
		} );

		// ============================================
		// SPIRIT MOVES (Extended)
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "spirit_breeze",
			Name = "Spirit Breeze",
			Description = "A gentle breeze of spirit energy.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 40,
			Accuracy = 100,
			MaxPP = 30
		} );

		AddMove( new MoveDefinition
		{
			Id = "dream_feast",
			Name = "Dream Feast",
			Description = "Feeds on the target's dreams. Heals 50% of damage dealt.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 90,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Drain, Value = 0.5f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "fate_glimpse",
			Name = "Fate Glimpse",
			Description = "Foresees the target's fate with devastating clarity.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 100,
			Accuracy = 100,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "divine_light",
			Name = "Divine Light",
			Description = "A blinding radiance from the heavens. 20% chance to lower SpA.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 100,
			Accuracy = 95,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpA, Chance = 0.2f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "prismatic_ray",
			Name = "Prismatic Ray",
			Description = "A beam of every color. 10% burn, 10% freeze chance.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 90,
			MaxPP = 5,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.1f },
				new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.1f }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "genesis_thought",
			Name = "Genesis Thought",
			Description = "The first thought of creation given form.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 90,
			MaxPP = 5
		} );

		AddMove( new MoveDefinition
		{
			Id = "genesis_wave",
			Name = "Genesis Wave",
			Description = "A wave of primordial creation energy. 10% chance to raise all stats.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 115,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseATK, Chance = 0.1f, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseDEF, Chance = 0.1f, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpA, Chance = 0.1f, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Chance = 0.1f, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSPD, Chance = 0.1f, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "creation_burst",
			Name = "Creation Burst",
			Description = "The spark of creation itself. Overwhelming spirit power.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 120,
			Accuracy = 90,
			MaxPP = 5
		} );

		AddMove( new MoveDefinition
		{
			Id = "universe_burst",
			Name = "Universe Burst",
			Description = "Channels the energy of an entire universe.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 120,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recharge } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "inner_focus",
			Name = "Inner Focus",
			Description = "Deepens focus. Raises user SpA and SpD by 1 stage each.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseSpA, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "astral_ward",
			Name = "Astral Ward",
			Description = "Channels cosmic energy. Raises user DEF and SpD by 1 stage each.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "bolster",
			Name = "Bolster",
			Description = "A surge of support. Raises user ATK and SpA by 1 stage each.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseATK, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpA, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "luminous_wall",
			Name = "Luminous Wall",
			Description = "A wall of light that reduces damage. Raises user SpD by 2 stages.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 2, Duration = 4, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "warding_hymn",
			Name = "Warding Hymn",
			Description = "A protective chant that shields against damage.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Shield, Duration = 3 } }
		} );

		// ============================================
		// NEUTRAL MOVES (Extended)
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "primordial_surge",
			Name = "Primordial Surge",
			Description = "Raw energy from before time itself. User must recharge.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Special,
			BasePower = 130,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recharge } }
		} );

		// ============================================
		// MYTHIC SIGNATURE MOVES
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "seismic_surge",
			Name = "Seismic Surge",
			Description = "The ocean floor splits apart. Massive physical water attack that lowers the target's DEF.",
			Element = ElementType.Water,
			Category = MoveCategory.Physical,
			BasePower = 120,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 1.0f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "molten_vein",
			Name = "Molten Vein",
			Description = "Channels the living ichor that sustains its bronze form. Drains HP from the target.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 115,
			Accuracy = 90,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Drain, Value = 30 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "tempest_unleash",
			Name = "Tempest Unleash",
			Description = "Opens the wind sack fully, releasing every storm at once. Raises user SPD.",
			Element = ElementType.Wind,
			Category = MoveCategory.Special,
			BasePower = 120,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 1, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "cold_shoulder",
			Name = "Cold Shoulder",
			Description = "Turns away in a dramatic huff, blasting the target with icy disdain. Lowers target SpA.",
			Element = ElementType.Ice,
			Category = MoveCategory.Special,
			BasePower = 115,
			Accuracy = 90,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpA, Chance = 1.0f, Value = 1 } }
		} );

		// ============================================
		// === NEW NEUTRAL MOVES ===
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "blunt_slam",
			Name = "Blunt Slam",
			Description = "A heavy, unfocused blow.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Physical,
			BasePower = 50,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "feral_charge",
			Name = "Feral Charge",
			Description = "A wild, reckless rush forward.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Physical,
			BasePower = 65,
			Accuracy = 95,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "savage_rend",
			Name = "Savage Rend",
			Description = "Brutally tears at the target.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Physical,
			BasePower = 80,
			Accuracy = 90,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "primal_crash",
			Name = "Primal Crash",
			Description = "A devastating full-body impact.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Physical,
			BasePower = 100,
			Accuracy = 85,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "cataclysm",
			Name = "Cataclysm",
			Description = "World-shaking force. Takes recoil damage.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Physical,
			BasePower = 120,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recoil, Value = 0.33f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "energy_pulse",
			Name = "Energy Pulse",
			Description = "A pulse of raw energy.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Special,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "resonant_howl",
			Name = "Resonant Howl",
			Description = "A piercing cry that rattles the target.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Special,
			BasePower = 65,
			Accuracy = 95,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "wild_surge",
			Name = "Wild Surge",
			Description = "Untamed energy lashes out.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Special,
			BasePower = 85,
			Accuracy = 90,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "oblivion_ray",
			Name = "Oblivion Ray",
			Description = "A beam of pure destruction.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 80,
			MaxPP = 5
		} );

		AddMove( new MoveDefinition
		{
			Id = "rally_cry",
			Name = "Rally Cry",
			Description = "A rallying shout. Raises user ATK by 1.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseATK, Value = 1, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "sharp_focus",
			Name = "Sharp Focus",
			Description = "Sharpens concentration. Raises user SpA by 2.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSpA, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "taunt_roar",
			Name = "Taunt Roar",
			Description = "A mocking roar. Lowers target DEF and SpD by 1.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.LowerDEF, Value = 1 },
				new MoveEffect { Type = MoveEffectType.LowerSpD, Value = 1 }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "agility_boost",
			Name = "Agility Boost",
			Description = "Limbering up. Raises user SPD by 2.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "war_cry",
			Name = "War Cry",
			Description = "A fearsome battle cry. Raises user ATK by 2.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseATK, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "primal_roar",
			Name = "Primal Roar",
			Description = "An ancient roar. Lowers target ATK by 2.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerATK, Value = 2 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "feint_jab",
			Name = "Feint Jab",
			Description = "A quick deceptive strike. +1 priority.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Physical,
			BasePower = 40,
			Accuracy = 100,
			MaxPP = 20,
			Priority = 1
		} );

		AddMove( new MoveDefinition
		{
			Id = "endure_stance",
			Name = "Endure Stance",
			Description = "Braces to survive any hit with 1 HP. +4 priority.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 10,
			Priority = 4,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Guard } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "concentration",
			Name = "Concentration",
			Description = "Deep focus. Raises user accuracy by 2.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseAccuracy, Value = 2, TargetsSelf = true } }
		} );

		// ============================================
		// === NEW FIRE MOVES ===
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "ember_claw",
			Name = "Ember Claw",
			Description = "Scratches with heated talons. 10% burn.",
			Element = ElementType.Fire,
			Category = MoveCategory.Physical,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 25,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "flame_tackle",
			Name = "Flame Tackle",
			Description = "Charges wreathed in fire.",
			Element = ElementType.Fire,
			Category = MoveCategory.Physical,
			BasePower = 65,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "magma_fang",
			Name = "Magma Fang",
			Description = "Bites with molten jaws. 20% burn.",
			Element = ElementType.Fire,
			Category = MoveCategory.Physical,
			BasePower = 85,
			Accuracy = 90,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "eruption_slam",
			Name = "Eruption Slam",
			Description = "An explosive body slam.",
			Element = ElementType.Fire,
			Category = MoveCategory.Physical,
			BasePower = 100,
			Accuracy = 85,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "pyroclasm",
			Name = "Pyroclasm",
			Description = "A volcanic eruption of force. 30% burn.",
			Element = ElementType.Fire,
			Category = MoveCategory.Physical,
			BasePower = 120,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "scorch_wave",
			Name = "Scorch Wave",
			Description = "A spreading wave of heat.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 55,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "molten_ray",
			Name = "Molten Ray",
			Description = "A beam of liquid fire. 10% burn.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 75,
			Accuracy = 95,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "wildfire_burst",
			Name = "Wildfire Burst",
			Description = "Flames spread uncontrollably.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 90,
			Accuracy = 85,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "volcanic_fury",
			Name = "Volcanic Fury",
			Description = "Erupts with volcanic rage. Takes recoil.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recoil, Value = 0.33f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "singe_breath",
			Name = "Singe Breath",
			Description = "A quick puff of flame. 20% burn.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 40,
			Accuracy = 100,
			MaxPP = 30,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "lava_plume",
			Name = "Lava Plume",
			Description = "A burst of lava in all directions. 30% burn.",
			Element = ElementType.Fire,
			Category = MoveCategory.Special,
			BasePower = 80,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "flame_shroud",
			Name = "Flame Shroud",
			Description = "Wraps in flames. Raises user SpA and SPD by 1.",
			Element = ElementType.Fire,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseSpA, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "smolder",
			Name = "Smolder",
			Description = "Sparks that guarantee a burn. 75% accuracy.",
			Element = ElementType.Fire,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 75,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 1.0f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "heat_haze",
			Name = "Heat Haze",
			Description = "Shimmering air distorts vision. Raises user evasion by 1.",
			Element = ElementType.Fire,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseEvasion, Value = 1, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "ash_cloud",
			Name = "Ash Cloud",
			Description = "Fills the air with ash. Lowers target accuracy by 1.",
			Element = ElementType.Fire,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerAccuracy, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "forge_temper",
			Name = "Forge Temper",
			Description = "Tempered by fire. Raises user ATK by 2.",
			Element = ElementType.Fire,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseATK, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "ember_wall",
			Name = "Ember Wall",
			Description = "A wall of embers. Raises DEF and SpD by 1.",
			Element = ElementType.Fire,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 1, TargetsSelf = true }
			}
		} );

		// ============================================
		// === NEW WATER MOVES ===
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "torrent_claw",
			Name = "Torrent Claw",
			Description = "Slashes with water-coated claws.",
			Element = ElementType.Water,
			Category = MoveCategory.Physical,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "crashing_wave",
			Name = "Crashing Wave",
			Description = "Body slams with the force of a wave.",
			Element = ElementType.Water,
			Category = MoveCategory.Physical,
			BasePower = 65,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "hydro_fang",
			Name = "Hydro Fang",
			Description = "Bites with pressurized water. 10% lowers DEF.",
			Element = ElementType.Water,
			Category = MoveCategory.Physical,
			BasePower = 75,
			Accuracy = 95,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "maelstrom_slam",
			Name = "Maelstrom Slam",
			Description = "Slams with swirling water force.",
			Element = ElementType.Water,
			Category = MoveCategory.Physical,
			BasePower = 90,
			Accuracy = 85,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "tsunami_crush",
			Name = "Tsunami Crush",
			Description = "The full force of a tidal wave. 20% lowers SPD.",
			Element = ElementType.Water,
			Category = MoveCategory.Physical,
			BasePower = 110,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSPD, Chance = 0.2f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "undertow_strike",
			Name = "Undertow Strike",
			Description = "A quick pull from below. +1 priority.",
			Element = ElementType.Water,
			Category = MoveCategory.Physical,
			BasePower = 55,
			Accuracy = 100,
			MaxPP = 20,
			Priority = 1
		} );

		AddMove( new MoveDefinition
		{
			Id = "water_bolt",
			Name = "Water Bolt",
			Description = "A focused bolt of water.",
			Element = ElementType.Water,
			Category = MoveCategory.Special,
			BasePower = 40,
			Accuracy = 100,
			MaxPP = 30
		} );

		AddMove( new MoveDefinition
		{
			Id = "ripple_burst",
			Name = "Ripple Burst",
			Description = "Expanding ripples of force.",
			Element = ElementType.Water,
			Category = MoveCategory.Special,
			BasePower = 60,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "whirlpool_surge",
			Name = "Whirlpool Surge",
			Description = "A churning vortex. 10% confuse.",
			Element = ElementType.Water,
			Category = MoveCategory.Special,
			BasePower = 75,
			Accuracy = 95,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Confuse, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "ocean_torrent",
			Name = "Ocean Torrent",
			Description = "The ocean's full fury unleashed.",
			Element = ElementType.Water,
			Category = MoveCategory.Special,
			BasePower = 90,
			Accuracy = 90,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "abyssal_wave",
			Name = "Abyssal Wave",
			Description = "A wave from the deepest trench.",
			Element = ElementType.Water,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 85,
			MaxPP = 5
		} );

		AddMove( new MoveDefinition
		{
			Id = "brine_spray",
			Name = "Brine Spray",
			Description = "Sprays salt water. 10% lowers SpD.",
			Element = ElementType.Water,
			Category = MoveCategory.Special,
			BasePower = 50,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpD, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "depth_charge",
			Name = "Depth Charge",
			Description = "Pressurized water detonation. 20% lowers DEF.",
			Element = ElementType.Water,
			Category = MoveCategory.Special,
			BasePower = 100,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 0.2f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "aqua_shield",
			Name = "Aqua Shield",
			Description = "A barrier of water. Raises DEF and SpD by 1.",
			Element = ElementType.Water,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "tidal_ward",
			Name = "Tidal Ward",
			Description = "A ward of ocean energy. Shields for 3 turns.",
			Element = ElementType.Water,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Shield, Duration = 3 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "drizzle_mist",
			Name = "Drizzle Mist",
			Description = "A gentle mist. Heals 25% HP.",
			Element = ElementType.Water,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Heal, Value = 25, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "current_boost",
			Name = "Current Boost",
			Description = "Rides the current. Raises SPD by 2.",
			Element = ElementType.Water,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "waterlog",
			Name = "Waterlog",
			Description = "Drenches the target. Lowers SPD by 2.",
			Element = ElementType.Water,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSPD, Value = 2 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "deep_pressure",
			Name = "Deep Pressure",
			Description = "Crushing ocean pressure. Lowers target ATK and SpA by 1.",
			Element = ElementType.Water,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 90,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.LowerATK, Value = 1 },
				new MoveEffect { Type = MoveEffectType.LowerSpA, Value = 1 }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "foam_coat",
			Name = "Foam Coat",
			Description = "Covers in protective foam. Raises SpD by 2.",
			Element = ElementType.Water,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "sea_chant",
			Name = "Sea Chant",
			Description = "An ocean melody. Raises SpA by 1.",
			Element = ElementType.Water,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSpA, Value = 1, TargetsSelf = true } }
		} );

		// ============================================
		// === NEW EARTH MOVES ===
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "gravel_toss",
			Name = "Gravel Toss",
			Description = "Flings a handful of rocks.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 35,
			Accuracy = 100,
			MaxPP = 30
		} );

		AddMove( new MoveDefinition
		{
			Id = "sandstone_strike",
			Name = "Sandstone Strike",
			Description = "Strikes with compressed sand.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 55,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "fault_breaker",
			Name = "Fault Breaker",
			Description = "Cracks the ground beneath the target.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 75,
			Accuracy = 90,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "tectonic_slam",
			Name = "Tectonic Slam",
			Description = "A plate-shifting slam. 20% flinch.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 95,
			Accuracy = 85,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Flinch, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "continental_crush",
			Name = "Continental Crush",
			Description = "The weight of a continent. User must recharge.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 120,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recharge } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "sand_blast",
			Name = "Sand Blast",
			Description = "A blast of abrasive sand. 10% lowers accuracy.",
			Element = ElementType.Earth,
			Category = MoveCategory.Special,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 25,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerAccuracy, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "tremor_pulse",
			Name = "Tremor Pulse",
			Description = "Sends tremors through the ground.",
			Element = ElementType.Earth,
			Category = MoveCategory.Special,
			BasePower = 60,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "magma_vent",
			Name = "Magma Vent",
			Description = "Superheated earth. 10% burn.",
			Element = ElementType.Earth,
			Category = MoveCategory.Special,
			BasePower = 80,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Burn, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "geode_burst",
			Name = "Geode Burst",
			Description = "Crystals explode outward.",
			Element = ElementType.Earth,
			Category = MoveCategory.Special,
			BasePower = 90,
			Accuracy = 85,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "fissure_wave",
			Name = "Fissure Wave",
			Description = "A shockwave from a deep fissure.",
			Element = ElementType.Earth,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 80,
			MaxPP = 5
		} );

		AddMove( new MoveDefinition
		{
			Id = "mineral_lance",
			Name = "Mineral Lance",
			Description = "A spear of crystallized minerals.",
			Element = ElementType.Earth,
			Category = MoveCategory.Special,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "stalagmite_jab",
			Name = "Stalagmite Jab",
			Description = "A sharp stone spike. +1 crit.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 60,
			Accuracy = 95,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.CritBoost, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "landslide",
			Name = "Landslide",
			Description = "Buries the target. 10% lowers SPD.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 85,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSPD, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "quake_stomp",
			Name = "Quake Stomp",
			Description = "A ground-shaking stomp. 10% flinch.",
			Element = ElementType.Earth,
			Category = MoveCategory.Physical,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Flinch, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "bedrock_stance",
			Name = "Bedrock Stance",
			Description = "Plants feet on solid bedrock. Raises DEF by 2.",
			Element = ElementType.Earth,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "dust_storm",
			Name = "Dust Storm",
			Description = "Kicks up blinding dust. Lowers target accuracy by 1.",
			Element = ElementType.Earth,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerAccuracy, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "fossil_power",
			Name = "Fossil Power",
			Description = "Ancient strength flows. Raises ATK and DEF by 1.",
			Element = ElementType.Earth,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseATK, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "quicksand",
			Name = "Quicksand",
			Description = "Traps the target. Lowers SPD by 2.",
			Element = ElementType.Earth,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSPD, Value = 2 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "earthen_armor",
			Name = "Earthen Armor",
			Description = "Coats in mud and stone. Raises DEF and SpD by 1.",
			Element = ElementType.Earth,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "petrify",
			Name = "Petrify",
			Description = "Turns the target partly to stone. 30% paralysis.",
			Element = ElementType.Earth,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 90,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "ground_pulse",
			Name = "Ground Pulse",
			Description = "A shockwave along the ground. Lowers target evasion by 1.",
			Element = ElementType.Earth,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerEvasion, Value = 1 } }
		} );

		// ============================================
		// === NEW WIND MOVES ===
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "gust_claw",
			Name = "Gust Claw",
			Description = "Slashes with wind-wrapped talons.",
			Element = ElementType.Wind,
			Category = MoveCategory.Physical,
			BasePower = 40,
			Accuracy = 100,
			MaxPP = 30
		} );

		AddMove( new MoveDefinition
		{
			Id = "cyclone_tackle",
			Name = "Cyclone Tackle",
			Description = "Charges with spinning winds.",
			Element = ElementType.Wind,
			Category = MoveCategory.Physical,
			BasePower = 60,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "storm_talon",
			Name = "Storm Talon",
			Description = "Electrified wind claws. +1 crit.",
			Element = ElementType.Wind,
			Category = MoveCategory.Physical,
			BasePower = 75,
			Accuracy = 95,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.CritBoost, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "hurricane_slam",
			Name = "Hurricane Slam",
			Description = "Slams with hurricane force.",
			Element = ElementType.Wind,
			Category = MoveCategory.Physical,
			BasePower = 90,
			Accuracy = 85,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "tornado_dive",
			Name = "Tornado Dive",
			Description = "Dives from a tornado. 20% confuse.",
			Element = ElementType.Wind,
			Category = MoveCategory.Physical,
			BasePower = 110,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Confuse, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "gale_fang",
			Name = "Gale Fang",
			Description = "Bites with pressurized wind. 10% flinch.",
			Element = ElementType.Wind,
			Category = MoveCategory.Physical,
			BasePower = 55,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Flinch, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "draft_rush",
			Name = "Draft Rush",
			Description = "A sudden gust attack. +1 priority.",
			Element = ElementType.Wind,
			Category = MoveCategory.Physical,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 20,
			Priority = 1
		} );

		AddMove( new MoveDefinition
		{
			Id = "wind_blade",
			Name = "Wind Blade",
			Description = "A cutting edge of compressed air.",
			Element = ElementType.Wind,
			Category = MoveCategory.Special,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "zephyr_bolt",
			Name = "Zephyr Bolt",
			Description = "A bolt of gentle wind energy.",
			Element = ElementType.Wind,
			Category = MoveCategory.Special,
			BasePower = 60,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "sky_shear",
			Name = "Sky Shear",
			Description = "Tears the sky with cutting wind.",
			Element = ElementType.Wind,
			Category = MoveCategory.Special,
			BasePower = 75,
			Accuracy = 95,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "vortex_blast",
			Name = "Vortex Blast",
			Description = "A concentrated vortex. 20% confuse.",
			Element = ElementType.Wind,
			Category = MoveCategory.Special,
			BasePower = 90,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Confuse, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "typhoon_wrath",
			Name = "Typhoon Wrath",
			Description = "The fury of a typhoon. 30% confuse.",
			Element = ElementType.Wind,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Confuse, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "windshear",
			Name = "Windshear",
			Description = "Shearing winds. 10% lowers DEF.",
			Element = ElementType.Wind,
			Category = MoveCategory.Special,
			BasePower = 50,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "squall_burst",
			Name = "Squall Burst",
			Description = "A sudden squall. 10% flinch.",
			Element = ElementType.Wind,
			Category = MoveCategory.Special,
			BasePower = 80,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Flinch, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "jet_stream",
			Name = "Jet Stream",
			Description = "Rides the jet stream. Raises user SPD by 1.",
			Element = ElementType.Wind,
			Category = MoveCategory.Special,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 1, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "tailwind_boost",
			Name = "Tailwind Boost",
			Description = "A powerful tailwind. Raises SPD by 2.",
			Element = ElementType.Wind,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "wind_wall",
			Name = "Wind Wall",
			Description = "A wall of wind. Raises SpD and evasion by 1.",
			Element = ElementType.Wind,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseEvasion, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "howling_gust",
			Name = "Howling Gust",
			Description = "Howling winds. Lowers target SpA by 1.",
			Element = ElementType.Wind,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpA, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "slipstream",
			Name = "Slipstream",
			Description = "Slips into the wind. Raises SPD and evasion by 1.",
			Element = ElementType.Wind,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseEvasion, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "sky_current",
			Name = "Sky Current",
			Description = "Channels sky energy. Raises SpA by 2.",
			Element = ElementType.Wind,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSpA, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "gust_shield",
			Name = "Gust Shield",
			Description = "A shield of swirling wind. Shields for 3 turns.",
			Element = ElementType.Wind,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Shield, Duration = 3 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "calm_air",
			Name = "Calm Air",
			Description = "Stills the air around the target. Lowers SPD and ATK by 1.",
			Element = ElementType.Wind,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.LowerSPD, Value = 1 },
				new MoveEffect { Type = MoveEffectType.LowerATK, Value = 1 }
			}
		} );

		// ============================================
		// === NEW ELECTRIC MOVES ===
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "spark_claw",
			Name = "Spark Claw",
			Description = "Electrified claws scratch.",
			Element = ElementType.Electric,
			Category = MoveCategory.Physical,
			BasePower = 40,
			Accuracy = 100,
			MaxPP = 30
		} );

		AddMove( new MoveDefinition
		{
			Id = "shock_tackle",
			Name = "Shock Tackle",
			Description = "A charged tackle. 10% paralysis.",
			Element = ElementType.Electric,
			Category = MoveCategory.Physical,
			BasePower = 60,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "lightning_fang",
			Name = "Lightning Fang",
			Description = "Bites with lightning. 20% paralysis.",
			Element = ElementType.Electric,
			Category = MoveCategory.Physical,
			BasePower = 75,
			Accuracy = 95,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "plasma_charge",
			Name = "Plasma Charge",
			Description = "Charges with superheated plasma.",
			Element = ElementType.Electric,
			Category = MoveCategory.Physical,
			BasePower = 90,
			Accuracy = 90,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "overload_crash",
			Name = "Overload Crash",
			Description = "Overloads with energy. Takes recoil.",
			Element = ElementType.Electric,
			Category = MoveCategory.Physical,
			BasePower = 110,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recoil, Value = 0.33f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "static_strike",
			Name = "Static Strike",
			Description = "A static-charged hit. 20% paralysis.",
			Element = ElementType.Electric,
			Category = MoveCategory.Physical,
			BasePower = 50,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "bolt_rush",
			Name = "Bolt Rush",
			Description = "A lightning-fast charge. +1 priority.",
			Element = ElementType.Electric,
			Category = MoveCategory.Physical,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 25,
			Priority = 1
		} );

		AddMove( new MoveDefinition
		{
			Id = "spark_wave",
			Name = "Spark Wave",
			Description = "A wave of sparks.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "voltage_ray",
			Name = "Voltage Ray",
			Description = "A beam of voltage.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 65,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "circuit_burst",
			Name = "Circuit Burst",
			Description = "A burst of electrical circuits.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 80,
			Accuracy = 95,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "ion_cannon",
			Name = "Ion Cannon",
			Description = "A concentrated ion beam. 10% paralysis.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 100,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "megavolt",
			Name = "Megavolt",
			Description = "Massive voltage discharge. 30% paralysis.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 120,
			Accuracy = 75,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "discharge_pulse",
			Name = "Discharge Pulse",
			Description = "A pulse of discharge. 20% paralysis.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 55,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "chain_lightning",
			Name = "Chain Lightning",
			Description = "Lightning that jumps between targets. 10% paralysis.",
			Element = ElementType.Electric,
			Category = MoveCategory.Special,
			BasePower = 75,
			Accuracy = 90,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "charge_up",
			Name = "Charge Up",
			Description = "Stores electrical energy. Raises SpA by 2.",
			Element = ElementType.Electric,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSpA, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "surge_field",
			Name = "Surge Field",
			Description = "An electric field. Raises SpA and SPD by 1.",
			Element = ElementType.Electric,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseSpA, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "stun_wave",
			Name = "Stun Wave",
			Description = "A wave that stuns. Guarantees paralysis.",
			Element = ElementType.Electric,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 75,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 1.0f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "static_field",
			Name = "Static Field",
			Description = "A field of static. Lowers target SPD by 2.",
			Element = ElementType.Electric,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSPD, Value = 2 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "overcharge",
			Name = "Overcharge",
			Description = "Overcharges the body. Raises ATK and SpA by 1.",
			Element = ElementType.Electric,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseATK, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpA, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "power_sap",
			Name = "Power Sap",
			Description = "Saps the target's power. Lowers SpA by 2.",
			Element = ElementType.Electric,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpA, Value = 2 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "galvanic_shield",
			Name = "Galvanic Shield",
			Description = "An electric barrier. Raises DEF and SpD by 1.",
			Element = ElementType.Electric,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 1, TargetsSelf = true }
			}
		} );

		// ============================================
		// === NEW ICE MOVES ===
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "ice_fang",
			Name = "Ice Fang",
			Description = "Bites with frozen jaws. 10% freeze.",
			Element = ElementType.Ice,
			Category = MoveCategory.Physical,
			BasePower = 40,
			Accuracy = 100,
			MaxPP = 30,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "frozen_claw",
			Name = "Frozen Claw",
			Description = "Slashes with frost-coated claws.",
			Element = ElementType.Ice,
			Category = MoveCategory.Physical,
			BasePower = 55,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "hail_strike",
			Name = "Hail Strike",
			Description = "Strikes with a hail of ice. 10% flinch.",
			Element = ElementType.Ice,
			Category = MoveCategory.Physical,
			BasePower = 70,
			Accuracy = 95,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Flinch, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "glacier_slam",
			Name = "Glacier Slam",
			Description = "Slams with glacial force.",
			Element = ElementType.Ice,
			Category = MoveCategory.Physical,
			BasePower = 90,
			Accuracy = 85,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "polar_charge",
			Name = "Polar Charge",
			Description = "A charge from the frozen poles. 10% freeze.",
			Element = ElementType.Ice,
			Category = MoveCategory.Physical,
			BasePower = 80,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "shatter_punch",
			Name = "Shatter Punch",
			Description = "Shatters frozen armor. 20% lowers DEF.",
			Element = ElementType.Ice,
			Category = MoveCategory.Physical,
			BasePower = 100,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 0.2f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "icicle_lance",
			Name = "Icicle Lance",
			Description = "Thrusts with an icicle spear. +1 crit.",
			Element = ElementType.Ice,
			Category = MoveCategory.Physical,
			BasePower = 65,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.CritBoost, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "winter_grasp",
			Name = "Winter Grasp",
			Description = "Grabs with freezing hands. 10% lowers SPD.",
			Element = ElementType.Ice,
			Category = MoveCategory.Physical,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 25,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSPD, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "chill_wind",
			Name = "Chill Wind",
			Description = "A freezing gust. 10% lowers SPD.",
			Element = ElementType.Ice,
			Category = MoveCategory.Special,
			BasePower = 40,
			Accuracy = 100,
			MaxPP = 30,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSPD, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "sleet_barrage",
			Name = "Sleet Barrage",
			Description = "A barrage of sleet and ice.",
			Element = ElementType.Ice,
			Category = MoveCategory.Special,
			BasePower = 60,
			Accuracy = 95,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "cryo_beam",
			Name = "Cryo Beam",
			Description = "A beam of extreme cold. 10% freeze.",
			Element = ElementType.Ice,
			Category = MoveCategory.Special,
			BasePower = 75,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "blizzard_wrath",
			Name = "Blizzard Wrath",
			Description = "The wrath of a blizzard. 20% freeze.",
			Element = ElementType.Ice,
			Category = MoveCategory.Special,
			BasePower = 95,
			Accuracy = 85,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "frostbite_nova",
			Name = "Frostbite Nova",
			Description = "An explosion of frost. 30% freeze.",
			Element = ElementType.Ice,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.3f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "subzero_pulse",
			Name = "Subzero Pulse",
			Description = "A pulse of subzero energy. 10% freeze.",
			Element = ElementType.Ice,
			Category = MoveCategory.Special,
			BasePower = 50,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Freeze, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "ice_armor",
			Name = "Ice Armor",
			Description = "Coats in thick ice. Raises DEF by 2.",
			Element = ElementType.Ice,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "frost_ward",
			Name = "Frost Ward",
			Description = "A ward of frost. Raises SpD by 2.",
			Element = ElementType.Ice,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "frozen_ground",
			Name = "Frozen Ground",
			Description = "Freezes the ground. Lowers target SPD by 2.",
			Element = ElementType.Ice,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSPD, Value = 2 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "crystallize",
			Name = "Crystallize",
			Description = "Crystallizes a barrier. Raises DEF and SpD by 1.",
			Element = ElementType.Ice,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "numb_chill",
			Name = "Numb Chill",
			Description = "Numbing cold. Lowers target ATK and SpA by 1.",
			Element = ElementType.Ice,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.LowerATK, Value = 1 },
				new MoveEffect { Type = MoveEffectType.LowerSpA, Value = 1 }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "glacial_mirror",
			Name = "Glacial Mirror",
			Description = "An ice mirror. Shields for 3 turns.",
			Element = ElementType.Ice,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Shield, Duration = 3 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "deep_freeze",
			Name = "Deep Freeze",
			Description = "Intense cold. Guarantees freeze. 50% accuracy.",
			Element = ElementType.Ice,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 50,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Freeze, Chance = 1.0f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "rime_coat",
			Name = "Rime Coat",
			Description = "Coats in healing rime. Heals 25% HP.",
			Element = ElementType.Ice,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Heal, Value = 25, TargetsSelf = true } }
		} );

		// ============================================
		// === NEW NATURE MOVES ===
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "bramble_strike",
			Name = "Bramble Strike",
			Description = "Strikes with thorny brambles.",
			Element = ElementType.Nature,
			Category = MoveCategory.Physical,
			BasePower = 55,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "canopy_crash",
			Name = "Canopy Crash",
			Description = "Drops from the canopy.",
			Element = ElementType.Nature,
			Category = MoveCategory.Physical,
			BasePower = 80,
			Accuracy = 90,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "overgrowth_slam",
			Name = "Overgrowth Slam",
			Description = "Slams with wild overgrowth.",
			Element = ElementType.Nature,
			Category = MoveCategory.Physical,
			BasePower = 100,
			Accuracy = 85,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "wild_thorn",
			Name = "Wild Thorn",
			Description = "A wild thorn strike. 10% poison.",
			Element = ElementType.Nature,
			Category = MoveCategory.Physical,
			BasePower = 65,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Poison, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "spore_cloud",
			Name = "Spore Cloud",
			Description = "Releases toxic spores. 10% poison.",
			Element = ElementType.Nature,
			Category = MoveCategory.Special,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 25,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Poison, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "photon_bloom",
			Name = "Photon Bloom",
			Description = "A burst of photosynthetic energy.",
			Element = ElementType.Nature,
			Category = MoveCategory.Special,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "jungle_wrath",
			Name = "Jungle Wrath",
			Description = "The jungle strikes back.",
			Element = ElementType.Nature,
			Category = MoveCategory.Special,
			BasePower = 95,
			Accuracy = 85,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "evergreen_pulse",
			Name = "Evergreen Pulse",
			Description = "Life energy that heals. Drains 25% of damage.",
			Element = ElementType.Nature,
			Category = MoveCategory.Special,
			BasePower = 80,
			Accuracy = 95,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Drain, Value = 0.25f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "seed_volley",
			Name = "Seed Volley",
			Description = "A volley of hard seeds. 10% lowers SpD.",
			Element = ElementType.Nature,
			Category = MoveCategory.Special,
			BasePower = 55,
			Accuracy = 95,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpD, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "growth_surge",
			Name = "Growth Surge",
			Description = "A surge of growth. Raises ATK and SpA by 1.",
			Element = ElementType.Nature,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseATK, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpA, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "regenerate",
			Name = "Regenerate",
			Description = "Natural healing. Heals 25% HP.",
			Element = ElementType.Nature,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Heal, Value = 25, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "toxic_pollen",
			Name = "Toxic Pollen",
			Description = "Toxic pollen cloud. Guarantees poison.",
			Element = ElementType.Nature,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 85,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Poison, Chance = 1.0f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "tangling_roots",
			Name = "Tangling Roots",
			Description = "Roots that tangle. Lowers target SPD by 2.",
			Element = ElementType.Nature,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSPD, Value = 2 } }
		} );

		// ============================================
		// === NEW METAL MOVES ===
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "rivet_strike",
			Name = "Rivet Strike",
			Description = "Pummels with riveted fists.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 50,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "anvil_drop",
			Name = "Anvil Drop",
			Description = "Drops like an anvil. 20% flinch.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 75,
			Accuracy = 90,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Flinch, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "steel_rend",
			Name = "Steel Rend",
			Description = "Rends with hardened steel.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 85,
			Accuracy = 95,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "foundry_crash",
			Name = "Foundry Crash",
			Description = "Crashes with foundry force. Takes recoil.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 110,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Recoil, Value = 0.33f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "slag_shot",
			Name = "Slag Shot",
			Description = "Fires a glob of molten slag.",
			Element = ElementType.Metal,
			Category = MoveCategory.Special,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "oxidize_ray",
			Name = "Oxidize Ray",
			Description = "A corroding beam. 10% lowers DEF.",
			Element = ElementType.Metal,
			Category = MoveCategory.Special,
			BasePower = 65,
			Accuracy = 100,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "forge_blast",
			Name = "Forge Blast",
			Description = "A blast of forge heat.",
			Element = ElementType.Metal,
			Category = MoveCategory.Special,
			BasePower = 90,
			Accuracy = 85,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "mercury_surge",
			Name = "Mercury Surge",
			Description = "A surge of liquid metal. 10% poison.",
			Element = ElementType.Metal,
			Category = MoveCategory.Special,
			BasePower = 110,
			Accuracy = 85,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Poison, Chance = 0.1f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "burnish",
			Name = "Burnish",
			Description = "Polishes the body. Raises SPD by 2.",
			Element = ElementType.Metal,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 2, TargetsSelf = true } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "rust_curse",
			Name = "Rust Curse",
			Description = "Corrodes the target. Lowers ATK and DEF by 1.",
			Element = ElementType.Metal,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.LowerATK, Value = 1 },
				new MoveEffect { Type = MoveEffectType.LowerDEF, Value = 1 }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "scrap_barrage",
			Name = "Scrap Barrage",
			Description = "Launches scrap metal. 10% lowers DEF.",
			Element = ElementType.Metal,
			Category = MoveCategory.Physical,
			BasePower = 60,
			Accuracy = 95,
			MaxPP = 20,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 0.1f, Value = 1 } }
		} );

		// ============================================
		// === NEW SHADOW MOVES ===
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "gloom_strike",
			Name = "Gloom Strike",
			Description = "Strikes from the gloom.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Physical,
			BasePower = 45,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "nightfall_rush",
			Name = "Nightfall Rush",
			Description = "Rushes in darkness.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Physical,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "phantom_rend",
			Name = "Phantom Rend",
			Description = "Ghostly claws rend. 10% lowers DEF.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Physical,
			BasePower = 90,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "oblivion_claw",
			Name = "Oblivion Claw",
			Description = "Claws of pure oblivion.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Physical,
			BasePower = 105,
			Accuracy = 85,
			MaxPP = 5
		} );

		AddMove( new MoveDefinition
		{
			Id = "murk_bolt",
			Name = "Murk Bolt",
			Description = "A bolt of murky energy.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 40,
			Accuracy = 100,
			MaxPP = 30
		} );

		AddMove( new MoveDefinition
		{
			Id = "shadow_wave",
			Name = "Shadow Wave",
			Description = "A wave of shadow energy.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 65,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "hex_blast",
			Name = "Hex Blast",
			Description = "A blast of hexing energy. 20% confuse.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Special,
			BasePower = 85,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Confuse, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "shadow_cloak",
			Name = "Shadow Cloak",
			Description = "Cloaks in shadow. Raises evasion and SPD by 1.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.RaiseEvasion, Value = 1, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSPD, Value = 1, TargetsSelf = true }
			}
		} );

		AddMove( new MoveDefinition
		{
			Id = "curse_mark",
			Name = "Curse Mark",
			Description = "Marks with a curse. Lowers target SpA by 2.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpA, Value = 2 } }
		} );

		// ============================================
		// === NEW SPIRIT MOVES ===
		// ============================================

		AddMove( new MoveDefinition
		{
			Id = "sacred_strike",
			Name = "Sacred Strike",
			Description = "A blessed physical strike.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Physical,
			BasePower = 50,
			Accuracy = 100,
			MaxPP = 25
		} );

		AddMove( new MoveDefinition
		{
			Id = "ethereal_rush",
			Name = "Ethereal Rush",
			Description = "Rushes through the ethereal plane.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Physical,
			BasePower = 70,
			Accuracy = 100,
			MaxPP = 15
		} );

		AddMove( new MoveDefinition
		{
			Id = "judgment_blow",
			Name = "Judgment Blow",
			Description = "A blow of divine judgment. 10% lowers SpD.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Physical,
			BasePower = 95,
			Accuracy = 90,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpD, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "radiant_burst",
			Name = "Radiant Burst",
			Description = "A burst of radiant light.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 55,
			Accuracy = 100,
			MaxPP = 20
		} );

		AddMove( new MoveDefinition
		{
			Id = "aurora_wave",
			Name = "Aurora Wave",
			Description = "A wave of aurora light. 10% lowers SpA.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 75,
			Accuracy = 95,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpA, Chance = 0.1f, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "celestial_flare",
			Name = "Celestial Flare",
			Description = "A flare from the heavens.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Special,
			BasePower = 95,
			Accuracy = 90,
			MaxPP = 10
		} );

		AddMove( new MoveDefinition
		{
			Id = "spirit_link",
			Name = "Spirit Link",
			Description = "Links spirits. Heals 25% HP and raises SpD by 1.",
			Element = ElementType.Spirit,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new()
			{
				new MoveEffect { Type = MoveEffectType.Heal, Value = 25, TargetsSelf = true },
				new MoveEffect { Type = MoveEffectType.RaiseSpD, Value = 1, TargetsSelf = true }
			}
		} );
	}

	private static void AddMove( MoveDefinition move )
	{
		_moves[move.Id] = move;
	}
}
