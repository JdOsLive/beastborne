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
			BasePower = 38,
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
			BasePower = 62,
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
			BasePower = 76,
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
			Description = "A menacing growl. Lowers target ATK by 1 stage.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 40,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerATK, Value = 1 } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "harden",
			Name = "Harden",
			Description = "Tenses muscles. Raises user DEF by 1 stage.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 40,
			Effects = new() { new MoveEffect { Type = MoveEffectType.RaiseDEF, Value = 1, TargetsSelf = true } }
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
			BasePower = 58,
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
			BasePower = 58,
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
			BasePower = 88,
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
			BasePower = 98,
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
			BasePower = 58,
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
			BasePower = 78,
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
			BasePower = 98,
			Accuracy = 80,
			MaxPP = 5
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
			BasePower = 18,
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
			BasePower = 42,
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
			BasePower = 52,
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
			BasePower = 88,
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
			BasePower = 88,
			Accuracy = 80,
			MaxPP = 5,
			Effects = new() { new MoveEffect { Type = MoveEffectType.CritBoost, Value = 1 } }
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
			BasePower = 52,
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
			BasePower = 52,
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
			BasePower = 98,
			Accuracy = 70,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Confuse, Chance = 0.3f } }
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
			BasePower = 58,
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
			BasePower = 78,
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
			BasePower = 98,
			Accuracy = 70,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Paralyze, Chance = 0.3f } }
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
			BasePower = 58,
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
			BasePower = 78,
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
			BasePower = 98,
			Accuracy = 70,
			MaxPP = 5,
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
			BasePower = 38,
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
			BasePower = 46,
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
			BasePower = 78,
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
			BasePower = 108,
			Accuracy = 100,
			MaxPP = 10,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Charge } }
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
			BasePower = 42,
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
			BasePower = 72,
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
			BasePower = 72,
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
			BasePower = 88,
			Accuracy = 75,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerDEF, Chance = 0.3f, Value = 1 } }
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
			BasePower = 62,
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
			BasePower = 72,
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
			BasePower = 72,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Flinch, Chance = 0.2f } }
		} );

		AddMove( new MoveDefinition
		{
			Id = "terror_visions",
			Name = "Terror Visions",
			Description = "Inflicts tormented visions on sleeping targets.",
			Element = ElementType.Shadow,
			Category = MoveCategory.Status,
			BasePower = 0,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.Torment } }
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
			BasePower = 72,
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
			BasePower = 82,
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
			BasePower = 66,
			Accuracy = 100,
			MaxPP = 15,
			Effects = new() { new MoveEffect { Type = MoveEffectType.LowerSpA, Value = 1 } }
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
	}

	private static void AddMove( MoveDefinition move )
	{
		_moves[move.Id] = move;
	}
}
