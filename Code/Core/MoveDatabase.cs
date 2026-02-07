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
	}

	private static void AddMove( MoveDefinition move )
	{
		_moves[move.Id] = move;
	}
}
