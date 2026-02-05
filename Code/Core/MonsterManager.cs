using Sandbox;
using Beastborne.Data;
using Beastborne.Systems;
using System.Text.Json;

namespace Beastborne.Core;

/// <summary>
/// Manages the player's monster collection
/// </summary>
public sealed class MonsterManager : Component
{
	public static MonsterManager Instance { get; private set; }

	private const string MONSTER_COOKIE_KEY = "monsters-data";
	private const string MAX_MONSTERS_COOKIE_KEY = "max-monsters";
	private const int BASE_MAX_MONSTERS = 50;
	private const int ABSOLUTE_MAX_MONSTERS = 500; // Hard cap on storage

	/// <summary>
	/// Get the full key with slot prefix
	/// </summary>
	private static string GetKey( string key ) => $"{SaveSlotManager.GetSlotPrefix()}{key}";

	public List<Monster> OwnedMonsters { get; private set; } = new();

	// Dynamic max monsters (can be increased via shop)
	public int MaxMonsters { get; private set; } = BASE_MAX_MONSTERS;

	/// <summary>
	/// Check if storage can be expanded further
	/// </summary>
	public bool CanExpandStorage => MaxMonsters < ABSOLUTE_MAX_MONSTERS;

	/// <summary>
	/// Get the absolute maximum storage limit
	/// </summary>
	public int AbsoluteMaxMonsters => ABSOLUTE_MAX_MONSTERS;

	/// <summary>
	/// Increase the maximum monster storage capacity
	/// </summary>
	/// <returns>True if storage was increased, false if at max cap</returns>
	public bool IncreaseMaxMonsters( int amount )
	{
		if ( MaxMonsters >= ABSOLUTE_MAX_MONSTERS )
		{
			Log.Info( $"Cannot increase storage: already at max capacity ({ABSOLUTE_MAX_MONSTERS})" );
			return false;
		}

		// Clamp to not exceed absolute max
		int newMax = Math.Min( MaxMonsters + amount, ABSOLUTE_MAX_MONSTERS );
		int actualIncrease = newMax - MaxMonsters;

		MaxMonsters = newMax;
		SaveMaxMonsters();
		Log.Info( $"Increased max monsters by {actualIncrease} to {MaxMonsters}" );
		return true;
	}

	private void SaveMaxMonsters()
	{
		Game.Cookies.Set( GetKey( MAX_MONSTERS_COOKIE_KEY ), MaxMonsters );
	}

	private void LoadMaxMonsters()
	{
		MaxMonsters = Game.Cookies.Get<int>( GetKey( MAX_MONSTERS_COOKIE_KEY ), BASE_MAX_MONSTERS );
		// Ensure MaxMonsters is never below the base (fixes corrupted/reset slots)
		if ( MaxMonsters < BASE_MAX_MONSTERS )
		{
			MaxMonsters = BASE_MAX_MONSTERS;
		}
	}

	// Species database
	private Dictionary<string, MonsterSpecies> _speciesDatabase = new();
	public IReadOnlyDictionary<string, MonsterSpecies> SpeciesDatabase => _speciesDatabase;

	// Events
	public Action<Monster> OnMonsterAdded;
	public Action<Monster> OnMonsterRemoved;
	public Action<Monster> OnMonsterUpdated;
	public Action<Monster, MonsterSpecies, MonsterSpecies> OnMonsterEvolved; // monster, fromSpecies, toSpecies

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Log.Info( "MonsterManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnStart()
	{
		LoadSpeciesDatabase();
		LoadMonsters();
		LoadMaxMonsters();
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "MonsterManager";
		go.Components.Create<MonsterManager>();
	}

	private void LoadSpeciesDatabase()
	{
		// Create default species for MVP
		CreateDefaultSpecies();
		Log.Info( $"Loaded {_speciesDatabase.Count} monster species" );
	}

	private void CreateDefaultSpecies()
	{
		// ═══════════════════════════════════════════════════════════════════════════
		// STARTERS - Beastiary #1-9
		// Three evolution lines given to new players
		// ═══════════════════════════════════════════════════════════════════════════

		// ═══════════════════════════════════════════════════════════════
		// FIRE STARTER LINE (#1-3): Embrik → Charrow → Ashenmare
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "embrik",
			Name = "Embrik",
			Description = "A chubby little ember foal with stubby legs and a flickering mane. It trips over its own hooves but leaves tiny scorch marks wherever it tumbles.",
			IconPath = "ui/monsters/embrik/idle/embrik_idle_01.png",
			BaseHP = 44, BaseATK = 57, BaseDEF = 38, BaseSpA = 36, BaseSpD = 33, BaseSPD = 52,
			HPGrowth = 4, ATKGrowth = 5, DEFGrowth = 3, SpAGrowth = 3, SpDGrowth = 3, SPDGrowth = 4,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Common,
			EvolvesTo = "charrow",
			EvolutionLevel = 16,
			BaseCatchRate = 0.6f,
			PossibleTraits = new() { "kindle_heart", "infernal_rage", "flame_eater" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "kindle", LearnLevel = 1 },
				new LearnableMove { MoveId = "rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 5 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 10 },
				new LearnableMove { MoveId = "pyre_fangs", LearnLevel = 15 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/embrik/idle/embrik_idle_01.png",
				"ui/monsters/embrik/idle/embrik_idle_02.png",
				"ui/monsters/embrik/idle/embrik_idle_03.png",
				"ui/monsters/embrik/idle/embrik_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BeastiaryNumber = 1,
			IconOffsetX = 10f,
			IconOffsetY = 24f
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "charrow",
			Name = "Charrow",
			Description = "A hollow creature with seven burning eyes. It was once a funeral pyre that refused to die, taking the shape of what it consumed.",
			IconPath = "ui/monsters/charrow/idle/charrow_idle_01.png",
			BaseHP = 67, BaseATK = 82, BaseDEF = 53, BaseSpA = 48, BaseSpD = 51, BaseSPD = 69,
			HPGrowth = 5, ATKGrowth = 7, DEFGrowth = 4, SpAGrowth = 4, SpDGrowth = 4, SPDGrowth = 5,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "embrik",
			EvolvesTo = "ashenmare",
			EvolutionLevel = 32,
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "kindle_heart", "infernal_rage", "flame_eater" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "searing_rush", LearnLevel = 1, EvolvesFrom = "kindle" },
				new LearnableMove { MoveId = "rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "pyre_fangs", LearnLevel = 1 },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 20 },
				new LearnableMove { MoveId = "cinders_curse", LearnLevel = 28 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/charrow/idle/charrow_idle_01.png",
				"ui/monsters/charrow/idle/charrow_idle_02.png",
				"ui/monsters/charrow/idle/charrow_idle_03.png",
				"ui/monsters/charrow/idle/charrow_idle_04.png"
			},
			BeastiaryNumber = 2,
			IconOffsetX = 12f,
			IconOffsetY = 32f
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "ashenmare",
			Name = "Ashenmare",
			Description = "A beast of obsidian and eternal flame, born when a volcano's heart broke. Its hooves leave glass flowers that bloom into fire.",
			IconPath = "ui/monsters/ashenmare/idle/ashenmare_idle_01.png",
			BaseHP = 87, BaseATK = 112, BaseDEF = 68, BaseSpA = 63, BaseSpD = 62, BaseSPD = 88,
			HPGrowth = 6, ATKGrowth = 9, DEFGrowth = 5, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 6,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Rare,
			EvolvesFrom = "charrow",
			BaseCatchRate = 0.2f,
			PossibleTraits = new() { "kindle_heart", "infernal_rage", "flame_eater" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "blazing_wrath", LearnLevel = 1, EvolvesFrom = "searing_rush" },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "cinders_curse", LearnLevel = 1 },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 36 },
				new LearnableMove { MoveId = "conflagration", LearnLevel = 45 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/ashenmare/idle/ashenmare_idle_01.png",
				"ui/monsters/ashenmare/idle/ashenmare_idle_02.png",
				"ui/monsters/ashenmare/idle/ashenmare_idle_03.png",
				"ui/monsters/ashenmare/idle/ashenmare_idle_04.png"
			},
			BeastiaryNumber = 3,
			IconOffsetX = 8f,
			IconOffsetY = 26f
		} );

		// ═══════════════════════════════════════════════════════════════
		// WATER STARTER LINE (#4-6): Droskul → Luracoil → Tidehollow
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "droskul",
			Name = "Droskul",
			Description = "A plump little eel with a stubby body and oversized eyes. It can barely swim straight, but the soft glow at its tail already hints at its predatory future.",
			IconPath = "ui/monsters/droskul/idle/droskul_idle_01.png",
			BaseHP = 52, BaseATK = 38, BaseDEF = 57, BaseSpA = 48, BaseSpD = 62, BaseSPD = 43,
			HPGrowth = 5, ATKGrowth = 3, DEFGrowth = 5, SpAGrowth = 4, SpDGrowth = 5, SPDGrowth = 4,
			Element = ElementType.Water,
			BaseRarity = Rarity.Common,
			EvolvesTo = "luracoil",
			EvolutionLevel = 16,
			BaseCatchRate = 0.6f,
			PossibleTraits = new() { "torrent_soul", "tidal_wrath", "aqua_siphon" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "splash_jet", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 5 },
				new LearnableMove { MoveId = "froth_barrage", LearnLevel = 12 },
				new LearnableMove { MoveId = "harden", LearnLevel = 15 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/droskul/idle/droskul_idle_01.png",
				"ui/monsters/droskul/idle/droskul_idle_02.png",
				"ui/monsters/droskul/idle/droskul_idle_03.png",
				"ui/monsters/droskul/idle/droskul_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BeastiaryNumber = 4
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "luracoil",
			Name = "Luracoil",
			Description = "An electric eel that traded its spark for the ocean's depths. Its bioluminescent lure attracts prey with false promises of warmth in cold waters.",
			IconPath = "ui/monsters/luracoil/idle/luracoil_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/luracoil/idle/luracoil_idle_01.png",
				"ui/monsters/luracoil/idle/luracoil_idle_02.png",
				"ui/monsters/luracoil/idle/luracoil_idle_03.png",
				"ui/monsters/luracoil/idle/luracoil_idle_04.png"
			},
			BaseHP = 75, BaseATK = 70, BaseDEF = 55, BaseSpA = 85, BaseSpD = 65, BaseSPD = 80,
			HPGrowth = 5, ATKGrowth = 5, DEFGrowth = 4, SpAGrowth = 6, SpDGrowth = 5, SPDGrowth = 6,
			Element = ElementType.Water,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "droskul",
			EvolvesTo = "tidehollow",
			EvolutionLevel = 32,
			BaseCatchRate = 0.35f,
			PossibleTraits = new() { "torrent_soul", "tidal_wrath", "aqua_siphon" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "froth_barrage", LearnLevel = 1, EvolvesFrom = "splash_jet" },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "tidal_slam", LearnLevel = 20 },
				new LearnableMove { MoveId = "temper", LearnLevel = 28 }
			},
			AnimationFrameRate = 8f,
			BeastiaryNumber = 5,
			IconOffsetX = 10f,
			IconOffsetY = 24f
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "tidehollow",
			Name = "Tidehollow",
			Description = "The abyss given form. It is said to be where the ocean goes to forget. Ships that enter its presence simply cease to have ever existed.",
			IconPath = "ui/monsters/tidehollow/idle/tidehollow_idle_01.png",
			BaseHP = 102, BaseATK = 83, BaseDEF = 107, BaseSpA = 93, BaseSpD = 112, BaseSPD = 73,
			HPGrowth = 7, ATKGrowth = 6, DEFGrowth = 8, SpAGrowth = 6, SpDGrowth = 8, SPDGrowth = 5,
			Element = ElementType.Water,
			BaseRarity = Rarity.Rare,
			EvolvesFrom = "luracoil",
			BaseCatchRate = 0.15f,
			PossibleTraits = new() { "torrent_soul", "tidal_wrath", "aqua_siphon" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "deluge", LearnLevel = 1, EvolvesFrom = "froth_barrage" },
				new LearnableMove { MoveId = "tidal_slam", LearnLevel = 1 },
				new LearnableMove { MoveId = "temper", LearnLevel = 1 },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 36 },
				new LearnableMove { MoveId = "monsoon_call", LearnLevel = 42 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/tidehollow/idle/tidehollow_idle_01.png",
				"ui/monsters/tidehollow/idle/tidehollow_idle_02.png",
				"ui/monsters/tidehollow/idle/tidehollow_idle_03.png",
				"ui/monsters/tidehollow/idle/tidehollow_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BeastiaryNumber = 6,
			IconOffsetX = 10f,
			IconOffsetY = 32f
		} );

		// ═══════════════════════════════════════════════════════════════
		// WIND STARTER LINE (#7-9): Wispryn → Hollowgale → Vexstorm
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "wispryn",
			Name = "Wispryn",
			Description = "A playful spirit born from echoes that never found their way back. It collects sounds and releases them in unexpected places.",
			IconPath = "ui/monsters/wispryn/idle/wispryn_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/wispryn/idle/Wispryn_idle_01.png",
				"ui/monsters/wispryn/idle/Wispryn_idle_02.png",
				"ui/monsters/wispryn/idle/Wispryn_idle_03.png",
				"ui/monsters/wispryn/idle/Wispryn_idle_04.png"
			},
			AnimationFrameRate = 6f,
			BaseHP = 38, BaseATK = 52, BaseDEF = 33, BaseSpA = 47, BaseSpD = 38, BaseSPD = 67,
			HPGrowth = 3, ATKGrowth = 4, DEFGrowth = 3, SpAGrowth = 4, SpDGrowth = 3, SPDGrowth = 6,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Common,
			EvolvesTo = "hollowgale",
			EvolutionLevel = 16,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "gale_spirit", "phantom_step", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "breeze_cut", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 5 },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 12 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 15 }
			},
			BeastiaryNumber = 7
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "hollowgale",
			Name = "Hollowgale",
			Description = "A faceless wanderer that wears the skin of passing storms. Where it walks, the air forgets how to be still.",
			IconPath = "ui/monsters/hollowgate/idle/hollowgate_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/hollowgate/idle/hollowgate_idle_01.png",
				"ui/monsters/hollowgate/idle/hollowgate_idle_02.png",
				"ui/monsters/hollowgate/idle/hollowgate_idle_03.png",
				"ui/monsters/hollowgate/idle/hollowgate_idle_04.png"
			},
			AnimationFrameRate = 6f,
			BaseHP = 58, BaseATK = 77, BaseDEF = 48, BaseSpA = 72, BaseSpD = 53, BaseSPD = 97,
			HPGrowth = 4, ATKGrowth = 6, DEFGrowth = 4, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 8,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "wispryn",
			EvolvesTo = "vexstorm",
			EvolutionLevel = 32,
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "gale_spirit", "phantom_step", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "razor_gale", LearnLevel = 1, EvolvesFrom = "breeze_cut" },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 1 },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 22 }
			},
			BeastiaryNumber = 8,
			IconOffsetX = 10f,
			IconOffsetY = 24f
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "vexstorm",
			Name = "Vexstorm",
			Description = "Not a bird but the angry pause between storm_strikeclaps given consciousness. It exists in the moment before lightning strikes, forever.",
			IconPath = "ui/monsters/vexstorm/idle/vexstorm_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/vexstorm/idle/vexstorm_idle_01.png",
				"ui/monsters/vexstorm/idle/vexstorm_idle_02.png",
				"ui/monsters/vexstorm/idle/vexstorm_idle_03.png",
				"ui/monsters/vexstorm/idle/vexstorm_idle_04.png"
			},
			AnimationFrameRate = 6f,
			BaseHP = 73, BaseATK = 97, BaseDEF = 63, BaseSpA = 92, BaseSpD = 68, BaseSPD = 122,
			HPGrowth = 5, ATKGrowth = 7, DEFGrowth = 5, SpAGrowth = 6, SpDGrowth = 5, SPDGrowth = 10,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Rare,
			EvolvesFrom = "hollowgale",
			BaseCatchRate = 0.18f,
			PossibleTraits = new() { "gale_spirit", "phantom_step", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "tempest", LearnLevel = 1, EvolvesFrom = "razor_gale" },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 1 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 1 },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 38 },
				new LearnableMove { MoveId = "annihilate", LearnLevel = 50 }
			},
			BeastiaryNumber = 9,
			IconOffsetX = 14f,
			IconOffsetY = 32f
		} );

		// ═══════════════════════════════════════════════════════════════════════════
		// WILD MONSTERS - Beastiary #10+
		// Organized by expedition area / element
		// ═══════════════════════════════════════════════════════════════════════════

		// ═══════════════════════════════════════════════════════════════
		// WHISPERING WOODS - MIXED ELEMENTS (#10-18)
		// The starting area with diverse early creatures
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "twigsnap",
			Name = "Twigsnap",
			Description = "A small creature made of fallen branches that mimic the sound of snapping twigs to lure curious travelers. It means no harm, just wants company.",
			IconPath = "ui/monsters/twigsnap/idle/twigsnap_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/twigsnap/idle/twigsnap_idle_01.png",
				"ui/monsters/twigsnap/idle/twigsnap_idle_02.png",
				"ui/monsters/twigsnap/idle/twigsnap_idle_03.png",
				"ui/monsters/twigsnap/idle/twigsnap_idle_04.png"
			},
			BaseHP = 42, BaseATK = 33, BaseDEF = 47, BaseSpA = 28, BaseSpD = 52, BaseSPD = 38,
			HPGrowth = 3, ATKGrowth = 3, DEFGrowth = 4, SpAGrowth = 2, SpDGrowth = 4, SPDGrowth = 3,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Common,
			EvolvesTo = "branchling",
			EvolutionLevel = 14,
			BaseCatchRate = 0.7f,
			PossibleTraits = new() { "verdant_power", "wild_harden", "barbed_hide" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "thorn_lash", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 5 },
				new LearnableMove { MoveId = "blade_leaf", LearnLevel = 10 }
			},
			BeastiaryNumber = 10
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "branchling",
			Name = "Branchling",
			Description = "A walking thicket that plants itself in divine_grace. It protects the youngest trees in the fodeep_slumber from being trampled.",
			IconPath = "ui/monsters/branchling/idle/branchling_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/branchling/idle/branchling_idle_01.png",
				"ui/monsters/branchling/idle/branchling_idle_02.png",
				"ui/monsters/branchling/idle/branchling_idle_03.png",
				"ui/monsters/branchling/idle/branchling_idle_04.png"
			},
			BaseHP = 67, BaseATK = 48, BaseDEF = 72, BaseSpA = 38, BaseSpD = 77, BaseSPD = 43,
			HPGrowth = 5, ATKGrowth = 4, DEFGrowth = 6, SpAGrowth = 3, SpDGrowth = 6, SPDGrowth = 3,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "twigsnap",
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "verdant_power", "wild_harden", "barbed_hide" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "blade_leaf", LearnLevel = 1, EvolvesFrom = "thorn_lash" },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 16 },
				new LearnableMove { MoveId = "vitality_burst", LearnLevel = 22 },
				new LearnableMove { MoveId = "root_bind", LearnLevel = 28 }
			},
			BeastiaryNumber = 11
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "dewdrop",
			Name = "Dewdrop",
			Description = "A tiny water spirit that forms on leaves at dawn. It believes morning is the best part of the day and sleeps through everything else.",
			IconPath = "ui/monsters/dewdrop/idle/dewdrop_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/dewdrop/idle/dewdrop_idle_01.png",
				"ui/monsters/dewdrop/idle/dewdrop_idle_02.png",
				"ui/monsters/dewdrop/idle/dewdrop_idle_03.png",
				"ui/monsters/dewdrop/idle/dewdrop_idle_04.png"
			},
			BaseHP = 33, BaseATK = 32, BaseDEF = 37, BaseSpA = 47, BaseSpD = 38, BaseSPD = 48,
			HPGrowth = 3, ATKGrowth = 2, DEFGrowth = 3, SpAGrowth = 4, SpDGrowth = 3, SPDGrowth = 5,
			Element = ElementType.Water,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.7f,
			PossibleTraits = new() { "torrent_soul", "cleansing_retreat", "vital_recovery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "splash_jet", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "deep_slumber", LearnLevel = 8 },
				new LearnableMove { MoveId = "froth_barrage", LearnLevel = 15 }
			},
			BeastiaryNumber = 12
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "dustling",
			Name = "Dustling",
			Description = "A fuzzy moth larva that gathers dust and starlight in its soft coat. It dreams of flight while crawling through moonlit shadows.",
			IconPath = "ui/monsters/dustling/idle/dustling_idle_01.png",
			BaseHP = 28, BaseATK = 42, BaseDEF = 23, BaseSpA = 52, BaseSpD = 28, BaseSPD = 62,
			HPGrowth = 2, ATKGrowth = 4, DEFGrowth = 2, SpAGrowth = 5, SpDGrowth = 2, SPDGrowth = 6,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Common,
			EvolvesTo = "flickermoth",
			EvolutionLevel = 18,
			BaseCatchRate = 0.6f,
			PossibleTraits = new() { "kindle_heart", "phantom_step", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "kindle", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "breeze_cut", LearnLevel = 8 },
				new LearnableMove { MoveId = "searing_rush", LearnLevel = 15 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/dustling/idle/dustling_idle_01.png",
				"ui/monsters/dustling/idle/dustling_idle_02.png",
				"ui/monsters/dustling/idle/dustling_idle_03.png",
				"ui/monsters/dustling/idle/dustling_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BeastiaryNumber = 13
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "flickermoth",
			Name = "Flickermoth",
			Description = "A luminous moth with a lantern heart that glows with stolen starlight. It guides lost travelers, though not always in the direction they intended.",
			IconPath = "ui/monsters/flickermoth/idle/flickermoth_idle_01.png",
			BaseHP = 53, BaseATK = 72, BaseDEF = 38, BaseSpA = 87, BaseSpD = 48, BaseSPD = 87,
			HPGrowth = 4, ATKGrowth = 6, DEFGrowth = 3, SpAGrowth = 7, SpDGrowth = 4, SPDGrowth = 7,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "dustling",
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "kindle_heart", "phantom_step", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "searing_rush", LearnLevel = 1, EvolvesFrom = "kindle" },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "razor_gale", LearnLevel = 1, EvolvesFrom = "breeze_cut" },
				new LearnableMove { MoveId = "cinders_curse", LearnLevel = 24 },
				new LearnableMove { MoveId = "blazing_wrath", LearnLevel = 32 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/flickermoth/idle/flickermoth_idle_01.png",
				"ui/monsters/flickermoth/idle/flickermoth_idle_02.png",
				"ui/monsters/flickermoth/idle/flickermoth_idle_03.png",
				"ui/monsters/flickermoth/idle/flickermoth_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BeastiaryNumber = 14
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "mosscreep",
			Name = "Mosscreep",
			Description = "A slow creature covered in ancient moss. It remkindles every footstep that has passed over it and dreams of all the places it cannot go.",
			IconPath = "ui/monsters/mosscreep/idle/mosscreep_idle_01.png",
			BaseHP = 57, BaseATK = 28, BaseDEF = 62, BaseSpA = 23, BaseSpD = 67, BaseSPD = 18,
			HPGrowth = 5, ATKGrowth = 2, DEFGrowth = 6, SpAGrowth = 2, SpDGrowth = 6, SPDGrowth = 1,
			Element = ElementType.Earth,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.65f,
			PossibleTraits = new() { "terra_force", "hardened_resolve", "enduring_will" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "mud_hurl", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 6 },
				new LearnableMove { MoveId = "boulder_toss", LearnLevel = 12 },
				new LearnableMove { MoveId = "temper", LearnLevel = 20 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/mosscreep/idle/mosscreep_idle_01.png",
				"ui/monsters/mosscreep/idle/mosscreep_idle_02.png",
				"ui/monsters/mosscreep/idle/mosscreep_idle_03.png",
				"ui/monsters/mosscreep/idle/mosscreep_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BeastiaryNumber = 15
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "whiskerwind",
			Name = "Whiskerwind",
			Description = "A small fox-like spirit that rides on gentle breezes. It collects whispers and trades them for secrets.",
			IconPath = "ui/monsters/whiskerwind/idle/whiskerwind_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/whiskerwind/idle/whiskerwind_idle_01.png",
				"ui/monsters/whiskerwind/idle/whiskerwind_idle_02.png",
				"ui/monsters/whiskerwind/idle/whiskerwind_idle_03.png",
				"ui/monsters/whiskerwind/idle/whiskerwind_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 33, BaseATK = 47, BaseDEF = 28, BaseSpA = 42, BaseSpD = 33, BaseSPD = 57,
			HPGrowth = 3, ATKGrowth = 4, DEFGrowth = 2, SpAGrowth = 3, SpDGrowth = 3, SPDGrowth = 5,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Common,
			EvolvesTo = "galefox",
			EvolutionLevel = 20,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "gale_spirit", "phantom_step", "trickster" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "breeze_cut", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 5 },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 12 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 18 }
			},
			BeastiaryNumber = 16
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "galefox",
			Name = "Galefox",
			Description = "A graceful fox that runs so fast it leaves temporary gaps in the air. Other creatures use these wind-tunnels to travel unseen.",
			IconPath = "ui/monsters/galefox/idle/galefox_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/galefox/idle/galefox_idle_01.png",
				"ui/monsters/galefox/idle/galefox_idle_02.png",
				"ui/monsters/galefox/idle/galefox_idle_03.png",
				"ui/monsters/galefox/idle/galefox_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 53, BaseATK = 72, BaseDEF = 43, BaseSpA = 62, BaseSpD = 48, BaseSPD = 92,
			HPGrowth = 4, ATKGrowth = 6, DEFGrowth = 3, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 8,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "whiskerwind",
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "gale_spirit", "phantom_step", "trickster" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "razor_gale", LearnLevel = 1, EvolvesFrom = "breeze_cut" },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 1 },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 25 },
				new LearnableMove { MoveId = "tempest", LearnLevel = 38 }
			},
			BeastiaryNumber = 17
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "glimshroom",
			Name = "Glimshroom",
			Description = "A phosphorescent mushroom creature that pulses with soft light. It feeds on ambient magic and glows brighter near powerful creatures.",
			IconPath = "ui/monsters/glimshroom/idle/glimshroom_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/glimshroom/idle/glimshroom_idle_01.png",
				"ui/monsters/glimshroom/idle/glimshroom_idle_02.png",
				"ui/monsters/glimshroom/idle/glimshroom_idle_03.png",
				"ui/monsters/glimshroom/idle/glimshroom_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 47, BaseATK = 38, BaseDEF = 52, BaseSpA = 53, BaseSpD = 57, BaseSPD = 33,
			HPGrowth = 4, ATKGrowth = 3, DEFGrowth = 5, SpAGrowth = 5, SpDGrowth = 5, SPDGrowth = 3,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.6f,
			PossibleTraits = new() { "verdant_power", "ethereal_blessing", "vital_recovery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "thorn_lash", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "vitality_burst", LearnLevel = 15 },
				new LearnableMove { MoveId = "root_bind", LearnLevel = 22 }
			},
			BeastiaryNumber = 18
		} );

		// ═══════════════════════════════════════════════════════════════
		// EMBER CAVERN - FIRE (#19-24)
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "cinderscale",
			Name = "Cinderscale",
			Description = "A chubby salamander with spotted scales that glow like cooling lava. Small crystalline formations sprout along its back, releasing thin wisps of smoke as it waddles about on stubby legs.",
			IconPath = "ui/monsters/cinderscale/idle/cinderscale_idle_01.png",
			BaseHP = 43, BaseATK = 58, BaseDEF = 37, BaseSpA = 34, BaseSpD = 36, BaseSPD = 52,
			HPGrowth = 4, ATKGrowth = 5, DEFGrowth = 3, SpAGrowth = 3, SpDGrowth = 3, SPDGrowth = 4,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Common,
			EvolvesTo = "blazefang",
			EvolutionLevel = 20,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "kindle_heart", "infernal_rage", "reckless_charge" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "kindle", LearnLevel = 1 },
				new LearnableMove { MoveId = "rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 5 },
				new LearnableMove { MoveId = "pyre_fangs", LearnLevel = 12 },
				new LearnableMove { MoveId = "searing_rush", LearnLevel = 18 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/cinderscale/idle/cinderscale_idle_01.png",
				"ui/monsters/cinderscale/idle/cinderscale_idle_02.png",
				"ui/monsters/cinderscale/idle/cinderscale_idle_03.png",
				"ui/monsters/cinderscale/idle/cinderscale_idle_04.png"
			},
			BeastiaryNumber = 19,
			IconOffsetX = 8f
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "blazefang",
			Name = "Blazefang",
			Description = "A fire serpent with spotted scales and crystalline lava formations along its spine. Smoke rises from its back as its tail flame burns with predatory intensity.",
			IconPath = "ui/monsters/blazefang/idle/blazefang_idle_01.png",
			BaseHP = 68, BaseATK = 87, BaseDEF = 58, BaseSpA = 52, BaseSpD = 53, BaseSPD = 77,
			HPGrowth = 5, ATKGrowth = 7, DEFGrowth = 4, SpAGrowth = 4, SpDGrowth = 4, SPDGrowth = 6,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "cinderscale",
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "kindle_heart", "infernal_rage", "reckless_charge" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "searing_rush", LearnLevel = 1, EvolvesFrom = "kindle" },
				new LearnableMove { MoveId = "pyre_fangs", LearnLevel = 1 },
				new LearnableMove { MoveId = "rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 24 },
				new LearnableMove { MoveId = "cinders_curse", LearnLevel = 30 },
				new LearnableMove { MoveId = "blazing_wrath", LearnLevel = 38 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/blazefang/idle/blazefang_idle_01.png",
				"ui/monsters/blazefang/idle/blazefang_idle_02.png",
				"ui/monsters/blazefang/idle/blazefang_idle_03.png",
				"ui/monsters/blazefang/idle/blazefang_idle_04.png"
			},
			BeastiaryNumber = 20,
			IconOffsetX = 10f,
			IconOffsetY = 24f
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "magmite",
			Name = "Magmite",
			Description = "A tiny spirit that lives in the gaps between cooling lava. It believes it can keep volcanoes from dying if it works hard enough.",
			IconPath = "ui/monsters/magmite/idle/magmite_idle_01.png",
			BaseHP = 57, BaseATK = 68, BaseDEF = 52, BaseSpA = 58, BaseSpD = 47, BaseSPD = 38,
			HPGrowth = 4, ATKGrowth = 6, DEFGrowth = 4, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 3,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "kindle_heart", "hardened_resolve", "barbed_hide" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "kindle", LearnLevel = 1 },
				new LearnableMove { MoveId = "boulder_toss", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 8 },
				new LearnableMove { MoveId = "searing_rush", LearnLevel = 15 },
				new LearnableMove { MoveId = "seismic_crash", LearnLevel = 22 },
				new LearnableMove { MoveId = "blazing_wrath", LearnLevel = 30 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/magmite/idle/magmite_idle_01.png",
				"ui/monsters/magmite/idle/magmite_idle_02.png",
				"ui/monsters/magmite/idle/magmite_idle_03.png",
				"ui/monsters/magmite/idle/magmite_idle_04.png"
			},
			BeastiaryNumber = 21
		} );

		// Smolderpup 3-stage line
		AddSpecies( new MonsterSpecies
		{
			Id = "smolderpup",
			Name = "Smolderpup",
			Description = "A small canine with fur that smolders but never burns. It was born from the ashes of a faithful hound that protected its home from wildfire.",
			IconPath = "ui/monsters/smolderpup/idle/smolderpup_idle_01.png",
			BaseHP = 42, BaseATK = 48, BaseDEF = 37, BaseSpA = 28, BaseSpD = 32, BaseSPD = 53,
			HPGrowth = 3, ATKGrowth = 5, DEFGrowth = 3, SpAGrowth = 2, SpDGrowth = 2, SPDGrowth = 5,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Common,
			EvolvesTo = "emberhound",
			EvolutionLevel = 18,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "kindle_heart", "infernal_rage", "bloodlust" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "kindle", LearnLevel = 1 },
				new LearnableMove { MoveId = "rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 5 },
				new LearnableMove { MoveId = "pyre_fangs", LearnLevel = 10 },
				new LearnableMove { MoveId = "searing_rush", LearnLevel = 16 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/smolderpup/idle/smolderpup_idle_01.png",
				"ui/monsters/smolderpup/idle/smolderpup_idle_02.png",
				"ui/monsters/smolderpup/idle/smolderpup_idle_03.png",
				"ui/monsters/smolderpup/idle/smolderpup_idle_04.png"
			},
			BeastiaryNumber = 24
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "emberhound",
			Name = "Emberhound",
			Description = "A fierce wolf whose howl carries kindles on the wind. It marks its territory with rings of controlled fire that never spread.",
			IconPath = "ui/monsters/emberhound/idle/emberhound_idle_01.png",
			BaseHP = 63, BaseATK = 82, BaseDEF = 53, BaseSpA = 52, BaseSpD = 48, BaseSPD = 77,
			HPGrowth = 5, ATKGrowth = 7, DEFGrowth = 4, SpAGrowth = 4, SpDGrowth = 4, SPDGrowth = 6,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "smolderpup",
			EvolvesTo = "infernowarg",
			EvolutionLevel = 36,
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "kindle_heart", "infernal_rage", "bloodlust" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "searing_rush", LearnLevel = 1, EvolvesFrom = "kindle" },
				new LearnableMove { MoveId = "pyre_fangs", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 22 },
				new LearnableMove { MoveId = "cinders_curse", LearnLevel = 28 },
				new LearnableMove { MoveId = "blazing_wrath", LearnLevel = 34 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/emberhound/idle/emberhound_idle_01.png",
				"ui/monsters/emberhound/idle/emberhound_idle_02.png",
				"ui/monsters/emberhound/idle/emberhound_idle_03.png",
				"ui/monsters/emberhound/idle/emberhound_idle_04.png"
			},
			BeastiaryNumber = 25,
			IconOffsetX = 10f,
			IconOffsetY = 24f
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "infernowarg",
			Name = "Infernowarg",
			Description = "A massive wolf wreathed in eternal flame. Ancient stories say it was once a star that fell to earth and chose to run with the wolves rather than return to the sky.",
			IconPath = "ui/monsters/infernowarg/idle/infernowarg_idle_01.png",
			BaseHP = 88, BaseATK = 107, BaseDEF = 68, BaseSpA = 67, BaseSpD = 58, BaseSPD = 97,
			HPGrowth = 6, ATKGrowth = 8, DEFGrowth = 5, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 7,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Rare,
			EvolvesFrom = "emberhound",
			BaseCatchRate = 0.15f,
			PossibleTraits = new() { "kindle_heart", "infernal_rage", "bloodlust" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "blazing_wrath", LearnLevel = 1, EvolvesFrom = "searing_rush" },
				new LearnableMove { MoveId = "pyre_fangs", LearnLevel = 1 },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 1 },
				new LearnableMove { MoveId = "cinders_curse", LearnLevel = 1 },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 40 },
				new LearnableMove { MoveId = "conflagration", LearnLevel = 50 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/infernowarg/idle/infernowarg_idle_01.png",
				"ui/monsters/infernowarg/idle/infernowarg_idle_02.png",
				"ui/monsters/infernowarg/idle/infernowarg_idle_03.png",
				"ui/monsters/infernowarg/idle/infernowarg_idle_04.png"
			},
			BeastiaryNumber = 26,
			IconOffsetX = 10f,
			IconOffsetY = 24f
		} );

		// ═══════════════════════════════════════════════════════════════
		// LAKE OF TEARS - WATER (#25-33)
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "puddlejaw",
			Name = "Puddlejaw",
			Description = "A small beast that forms when rain collects in forgotten places. It swallows reflections and spits them out in the wrong order.",
			IconPath = "ui/monsters/puddlejaw/idle/puddlejaw_idle_01.png",
			BaseHP = 52, BaseATK = 43, BaseDEF = 52, BaseSpA = 53, BaseSpD = 57, BaseSPD = 43,
			HPGrowth = 4, ATKGrowth = 4, DEFGrowth = 5, SpAGrowth = 5, SpDGrowth = 5, SPDGrowth = 4,
			Element = ElementType.Water,
			BaseRarity = Rarity.Common,
			EvolvesTo = "mirrorpond",
			EvolutionLevel = 22,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "torrent_soul", "tidal_wrath", "menacing_aura" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "splash_jet", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 5 },
				new LearnableMove { MoveId = "froth_barrage", LearnLevel = 12 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 18 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/puddlejaw/idle/puddlejaw_idle_01.png",
				"ui/monsters/puddlejaw/idle/puddlejaw_idle_02.png",
				"ui/monsters/puddlejaw/idle/puddlejaw_idle_03.png",
				"ui/monsters/puddlejaw/idle/puddlejaw_idle_04.png"
			},
			BeastiaryNumber = 27
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "mirrorpond",
			Name = "Mirrorpond",
			Description = "A creature that is simultaneously the tidal_slamace and depth of still water. Those who gaze into it see not themselves, but who they were afraid to become.",
			IconPath = "ui/monsters/mirrorpond/idle/mirrorpond_idle_01.png",
			BaseHP = 82, BaseATK = 63, BaseDEF = 87, BaseSpA = 78, BaseSpD = 92, BaseSPD = 53,
			HPGrowth = 6, ATKGrowth = 5, DEFGrowth = 7, SpAGrowth = 6, SpDGrowth = 7, SPDGrowth = 4,
			Element = ElementType.Water,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "puddlejaw",
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "torrent_soul", "tidal_wrath", "menacing_aura" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "froth_barrage", LearnLevel = 1, EvolvesFrom = "splash_jet" },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "tidal_slam", LearnLevel = 26 },
				new LearnableMove { MoveId = "deluge", LearnLevel = 34 },
				new LearnableMove { MoveId = "temper", LearnLevel = 40 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/mirrorpond/idle/mirrorpond_idle_01.png",
				"ui/monsters/mirrorpond/idle/mirrorpond_idle_02.png",
				"ui/monsters/mirrorpond/idle/mirrorpond_idle_03.png",
				"ui/monsters/mirrorpond/idle/mirrorpond_idle_04.png"
			},
			BeastiaryNumber = 28
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "weepfin",
			Name = "Weepfin",
			Description = "A fish made entirely of crystallized tears. It swims through air as easily as water, always searching for someone crying alone.",
			IconPath = "ui/monsters/weepfin/idle/weepfin_idle_01.png",
			BaseHP = 47, BaseATK = 48, BaseDEF = 47, BaseSpA = 58, BaseSpD = 52, BaseSPD = 63,
			HPGrowth = 4, ATKGrowth = 4, DEFGrowth = 4, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 6,
			Element = ElementType.Water,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.5f,
			PossibleTraits = new() { "torrent_soul", "phantom_step", "vital_recovery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "splash_jet", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "froth_barrage", LearnLevel = 10 },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 15 },
				new LearnableMove { MoveId = "tidal_slam", LearnLevel = 22 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/weepfin/idle/weepfin_idle_01.png",
				"ui/monsters/weepfin/idle/weepfin_idle_02.png",
				"ui/monsters/weepfin/idle/weepfin_idle_03.png",
				"ui/monsters/weepfin/idle/weepfin_idle_04.png"
			},
			BeastiaryNumber = 29
		} );

		// Streamling 3-stage line
		AddSpecies( new MonsterSpecies
		{
			Id = "streamling",
			Name = "Streamling",
			Description = "A playful spirit born from babbling brooks. It communicates in the sounds of running water and is always surprised when others can't understand.",
			IconPath = "ui/monsters/streamling/idle/streamling_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/streamling/idle/streamling_idle_01.png",
				"ui/monsters/streamling/idle/streamling_idle_02.png",
				"ui/monsters/streamling/idle/streamling_idle_03.png",
				"ui/monsters/streamling/idle/streamling_idle_04.png"
			},
			BaseHP = 47, BaseATK = 38, BaseDEF = 47, BaseSpA = 48, BaseSpD = 52, BaseSPD = 53,
			HPGrowth = 4, ATKGrowth = 3, DEFGrowth = 4, SpAGrowth = 4, SpDGrowth = 4, SPDGrowth = 5,
			Element = ElementType.Water,
			BaseRarity = Rarity.Common,
			EvolvesTo = "rivercrest",
			EvolutionLevel = 18,
			BaseCatchRate = 0.6f,
			PossibleTraits = new() { "torrent_soul", "tidal_wrath", "aqua_siphon" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "splash_jet", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 5 },
				new LearnableMove { MoveId = "froth_barrage", LearnLevel = 12 },
				new LearnableMove { MoveId = "tidal_slam", LearnLevel = 16 }
			},
			BeastiaryNumber = 30
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "rivercrest",
			Name = "Rivercrest",
			Description = "A dignified water spirit that guards the junction where rivers meet. It mediates disputes between streams that cannot agree on where to flow.",
			IconPath = "ui/monsters/rivercrest/idle/rivercrest_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/rivercrest/idle/rivercrest_idle_01.png",
				"ui/monsters/rivercrest/idle/rivercrest_idle_02.png",
				"ui/monsters/rivercrest/idle/rivercrest_idle_03.png",
				"ui/monsters/rivercrest/idle/rivercrest_idle_04.png"
			},
			BaseHP = 72, BaseATK = 58, BaseDEF = 77, BaseSpA = 68, BaseSpD = 82, BaseSPD = 63,
			HPGrowth = 5, ATKGrowth = 5, DEFGrowth = 6, SpAGrowth = 5, SpDGrowth = 6, SPDGrowth = 5,
			Element = ElementType.Water,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "streamling",
			EvolvesTo = "oceanmaw",
			EvolutionLevel = 36,
			BaseCatchRate = 0.35f,
			PossibleTraits = new() { "torrent_soul", "tidal_wrath", "aqua_siphon" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "froth_barrage", LearnLevel = 1, EvolvesFrom = "splash_jet" },
				new LearnableMove { MoveId = "tidal_slam", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "temper", LearnLevel = 24 },
				new LearnableMove { MoveId = "deluge", LearnLevel = 32 }
			},
			BeastiaryNumber = 31
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "oceanmaw",
			Name = "Oceanmaw",
			Description = "The place where all rivers end given consciousness. It remkindles every drop of water that has ever reached the sea and mourns those that evaporated before arriving.",
			IconPath = "ui/monsters/oceanmaw/idle/oceanmaw_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/oceanmaw/idle/oceanmaw_idle_01.png",
				"ui/monsters/oceanmaw/idle/oceanmaw_idle_02.png",
				"ui/monsters/oceanmaw/idle/oceanmaw_idle_03.png",
				"ui/monsters/oceanmaw/idle/oceanmaw_idle_04.png"
			},
			BaseHP = 98, BaseATK = 82, BaseDEF = 93, BaseSpA = 92, BaseSpD = 98, BaseSPD = 72,
			HPGrowth = 7, ATKGrowth = 6, DEFGrowth = 7, SpAGrowth = 6, SpDGrowth = 7, SPDGrowth = 5,
			Element = ElementType.Water,
			BaseRarity = Rarity.Rare,
			EvolvesFrom = "rivercrest",
			BaseCatchRate = 0.15f,
			PossibleTraits = new() { "torrent_soul", "tidal_wrath", "aqua_siphon" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "deluge", LearnLevel = 1, EvolvesFrom = "froth_barrage" },
				new LearnableMove { MoveId = "tidal_slam", LearnLevel = 1 },
				new LearnableMove { MoveId = "temper", LearnLevel = 1 },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 40 },
				new LearnableMove { MoveId = "monsoon_call", LearnLevel = 46 },
				new LearnableMove { MoveId = "annihilate", LearnLevel = 52 }
			},
			BeastiaryNumber = 32
		} );

		// Bubblite 2-stage line
		AddSpecies( new MonsterSpecies
		{
			Id = "bubblite",
			Name = "Bubblite",
			Description = "A tiny jellyfish whose dome holds a pocket of air from its first breath. It drifts through the shallows, afraid to venture into deeper waters alone.",
			IconPath = "ui/monsters/bubblite/idle/bubblite_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/bubblite/idle/bubblite_idle_01.png",
				"ui/monsters/bubblite/idle/bubblite_idle_02.png",
				"ui/monsters/bubblite/idle/bubblite_idle_03.png",
				"ui/monsters/bubblite/idle/bubblite_idle_04.png"
			},
			BaseHP = 37, BaseATK = 33, BaseDEF = 57, BaseSpA = 43, BaseSpD = 62, BaseSPD = 38,
			HPGrowth = 3, ATKGrowth = 3, DEFGrowth = 5, SpAGrowth = 4, SpDGrowth = 5, SPDGrowth = 4,
			Element = ElementType.Water,
			BaseRarity = Rarity.Common,
			EvolvesTo = "foamwraith",
			EvolutionLevel = 24,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "torrent_soul", "phantom_step", "barbed_hide" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "splash_jet", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 6 },
				new LearnableMove { MoveId = "froth_barrage", LearnLevel = 12 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 18 }
			},
			BeastiaryNumber = 33
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "foamwraith",
			Name = "Foamwraith",
			Description = "A ghostly jellyfish that drifts where sailors were lost at sea. It carries their final divine_gracees in its trailing tendrils and guides ships away from the same fate.",
			IconPath = "ui/monsters/foamwraith/idle/foamwraith_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/foamwraith/idle/foamwraith_idle_01.png",
				"ui/monsters/foamwraith/idle/foamwraith_idle_02.png",
				"ui/monsters/foamwraith/idle/foamwraith_idle_03.png",
				"ui/monsters/foamwraith/idle/foamwraith_idle_04.png"
			},
			BaseHP = 62, BaseATK = 68, BaseDEF = 67, BaseSpA = 83, BaseSpD = 82, BaseSPD = 73,
			HPGrowth = 5, ATKGrowth = 6, DEFGrowth = 5, SpAGrowth = 7, SpDGrowth = 6, SPDGrowth = 6,
			Element = ElementType.Water,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "bubblite",
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "torrent_soul", "phantom_step", "barbed_hide" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "froth_barrage", LearnLevel = 1, EvolvesFrom = "splash_jet" },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 28 },
				new LearnableMove { MoveId = "deluge", LearnLevel = 36 },
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 42 }
			},
			BeastiaryNumber = 34
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "coralheim",
			Name = "Coralheim",
			Description = "A living reef that detached itself from the ocean floor. It carries an entire ecosystem on its back and is always looking for a new place to anchor.",
			IconPath = "ui/monsters/coralheim/idle/coralheim_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/coralheim/idle/coralheim_idle_01.png",
				"ui/monsters/coralheim/idle/coralheim_idle_02.png",
				"ui/monsters/coralheim/idle/coralheim_idle_03.png",
				"ui/monsters/coralheim/idle/coralheim_idle_04.png"
			},
			BaseHP = 80, BaseATK = 55, BaseDEF = 90, BaseSpA = 48, BaseSpD = 95, BaseSPD = 25,
			HPGrowth = 7, ATKGrowth = 4, DEFGrowth = 8, SpAGrowth = 4, SpDGrowth = 8, SPDGrowth = 2,
			Element = ElementType.Water,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.35f,
			PossibleTraits = new() { "torrent_soul", "hardened_resolve", "barbed_hide" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "splash_jet", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 6 },
				new LearnableMove { MoveId = "froth_barrage", LearnLevel = 14 },
				new LearnableMove { MoveId = "temper", LearnLevel = 22 },
				new LearnableMove { MoveId = "deluge", LearnLevel = 32 }
			},
			BeastiaryNumber = 35
		} );

		// ═══════════════════════════════════════════════════════════════
		// ECHO CANYON - WIND (#34-42)
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "driftmote",
			Name = "Driftmote",
			Description = "A creature made of dust and forgotten paths. It follows travelers not to harm them, but because it has forgotten how to find its own way.",
			IconPath = "ui/monsters/driftmote/idle/driftmote_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/driftmote/idle/driftmote_idle_01.png",
				"ui/monsters/driftmote/idle/driftmote_idle_02.png",
				"ui/monsters/driftmote/idle/driftmote_idle_03.png",
				"ui/monsters/driftmote/idle/driftmote_idle_04.png"
			},
			BaseHP = 40, BaseATK = 45, BaseDEF = 35, BaseSpA = 52, BaseSpD = 38, BaseSPD = 60,
			HPGrowth = 3, ATKGrowth = 4, DEFGrowth = 3, SpAGrowth = 5, SpDGrowth = 3, SPDGrowth = 6,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Common,
			EvolvesTo = "galeclaw",
			EvolutionLevel = 18,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "gale_spirit", "phantom_step", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "breeze_cut", LearnLevel = 1 },
				new LearnableMove { MoveId = "rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 6 },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 12 },
				new LearnableMove { MoveId = "razor_gale", LearnLevel = 16 }
			},
			BeastiaryNumber = 36
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "galeclaw",
			Name = "Galeclaw",
			Description = "A beast woven from the last breaths of dying winds. Its claws are made of solidified breeze_cuts, and its cry is the sound of doors slamming in empty houses.",
			IconPath = "ui/monsters/galeclaw/idle/galeclaw_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/galeclaw/idle/galeclaw_idle_01.png",
				"ui/monsters/galeclaw/idle/galeclaw_idle_02.png",
				"ui/monsters/galeclaw/idle/galeclaw_idle_03.png",
				"ui/monsters/galeclaw/idle/galeclaw_idle_04.png"
			},
			BaseHP = 60, BaseATK = 70, BaseDEF = 50, BaseSpA = 78, BaseSpD = 52, BaseSPD = 90,
			HPGrowth = 4, ATKGrowth = 6, DEFGrowth = 4, SpAGrowth = 6, SpDGrowth = 4, SPDGrowth = 8,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "driftmote",
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "gale_spirit", "phantom_step", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "razor_gale", LearnLevel = 1, EvolvesFrom = "breeze_cut" },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 22 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 28 },
				new LearnableMove { MoveId = "tempest", LearnLevel = 38 }
			},
			BeastiaryNumber = 37
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "whistleshade",
			Name = "Whistleshade",
			Description = "A creature that exists only in the sound of wind passing through hollow places. It can only be seen from the corner of your eye.",
			IconPath = "ui/monsters/whistleshade/idle/whistleshade_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/whistleshade/idle/whistleshade_idle_01.png",
				"ui/monsters/whistleshade/idle/whistleshade_idle_02.png",
				"ui/monsters/whistleshade/idle/whistleshade_idle_03.png",
				"ui/monsters/whistleshade/idle/whistleshade_idle_04.png"
			},
			BaseHP = 35, BaseATK = 55, BaseDEF = 30, BaseSpA = 68, BaseSpD = 42, BaseSPD = 80,
			HPGrowth = 3, ATKGrowth = 5, DEFGrowth = 2, SpAGrowth = 6, SpDGrowth = 3, SPDGrowth = 7,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.35f,
			PossibleTraits = new() { "gale_spirit", "phantom_step", "trickster" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "breeze_cut", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 8 },
				new LearnableMove { MoveId = "razor_gale", LearnLevel = 15 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 22 },
				new LearnableMove { MoveId = "tempest", LearnLevel = 32 }
			},
			BeastiaryNumber = 38
		} );

		// Zephyrmite 3-stage line
		AddSpecies( new MonsterSpecies
		{
			Id = "zephyrmite",
			Name = "Zephyrmite",
			Description = "A tiny creature that rides on the backs of breezes. It weighs less than a whisper and dreams of becoming a tempest someday.",
			IconPath = "ui/monsters/zephyrmite/idle/zephyrmite_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/zephyrmite/idle/zephyrmite_idle_01.png",
				"ui/monsters/zephyrmite/idle/zephyrmite_idle_02.png",
				"ui/monsters/zephyrmite/idle/zephyrmite_idle_03.png",
				"ui/monsters/zephyrmite/idle/zephyrmite_idle_04.png"
			},
			BaseHP = 30, BaseATK = 40, BaseDEF = 25, BaseSpA = 48, BaseSpD = 28, BaseSPD = 70,
			HPGrowth = 2, ATKGrowth = 4, DEFGrowth = 2, SpAGrowth = 4, SpDGrowth = 2, SPDGrowth = 7,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Common,
			EvolvesTo = "cyclonyx",
			EvolutionLevel = 20,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "gale_spirit", "skyborne", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "breeze_cut", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 8 },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 14 },
				new LearnableMove { MoveId = "razor_gale", LearnLevel = 18 }
			},
			BeastiaryNumber = 40
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "cyclonyx",
			Name = "Cyclonyx",
			Description = "A spinning creature that has become one with the wind itself. It communicates by changing air pressure and is constantly dizzy but doesn't mind.",
			IconPath = "ui/monsters/cyclonyx/idle/cyclonyx_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/cyclonyx/idle/cyclonyx_idle_01.png",
				"ui/monsters/cyclonyx/idle/cyclonyx_idle_02.png",
				"ui/monsters/cyclonyx/idle/cyclonyx_idle_03.png",
				"ui/monsters/cyclonyx/idle/cyclonyx_idle_04.png"
			},
			BaseHP = 50, BaseATK = 65, BaseDEF = 40, BaseSpA = 72, BaseSpD = 45, BaseSPD = 100,
			HPGrowth = 4, ATKGrowth = 5, DEFGrowth = 3, SpAGrowth = 6, SpDGrowth = 3, SPDGrowth = 9,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "zephyrmite",
			EvolvesTo = "tempestking",
			EvolutionLevel = 38,
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "gale_spirit", "skyborne", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "razor_gale", LearnLevel = 1, EvolvesFrom = "breeze_cut" },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 24 },
				new LearnableMove { MoveId = "tempest", LearnLevel = 34 }
			},
			BeastiaryNumber = 41
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "tempestking",
			Name = "Tempestking",
			Description = "The storm that other storms tell stories about. It doesn't cause destruction - destruction simply happens where it chooses to exist. It considers this a minor inconvenience.",
			IconPath = "ui/monsters/tempestking/idle/tempestking_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/tempestking/idle/tempestking_idle_01.png",
				"ui/monsters/tempestking/idle/tempestking_idle_02.png",
				"ui/monsters/tempestking/idle/tempestking_idle_03.png",
				"ui/monsters/tempestking/idle/tempestking_idle_04.png"
			},
			BaseHP = 75, BaseATK = 90, BaseDEF = 60, BaseSpA = 98, BaseSpD = 65, BaseSPD = 125,
			HPGrowth = 5, ATKGrowth = 7, DEFGrowth = 4, SpAGrowth = 7, SpDGrowth = 4, SPDGrowth = 10,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Rare,
			EvolvesFrom = "cyclonyx",
			BaseCatchRate = 0.12f,
			PossibleTraits = new() { "gale_spirit", "skyborne", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "tempest", LearnLevel = 1, EvolvesFrom = "razor_gale" },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 1 },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 42 },
				new LearnableMove { MoveId = "storm_strike", LearnLevel = 50 },
				new LearnableMove { MoveId = "annihilate", LearnLevel = 58 }
			},
			BeastiaryNumber = 42
		} );

		// Featherwisp 2-stage line
		AddSpecies( new MonsterSpecies
		{
			Id = "featherwisp",
			Name = "Featherwisp",
			Description = "A delicate bird made entirely of detached feathers. Each feather came from a different bird, and the Featherwisp remkindles all their flights.",
			IconPath = "ui/monsters/featherwisp/idle/featherwisp_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/featherwisp/idle/featherwisp_idle_01.png",
				"ui/monsters/featherwisp/idle/featherwisp_idle_02.png",
				"ui/monsters/featherwisp/idle/featherwisp_idle_03.png",
				"ui/monsters/featherwisp/idle/featherwisp_idle_04.png"
			},
			BaseHP = 35, BaseATK = 45, BaseDEF = 30, BaseSpA = 58, BaseSpD = 38, BaseSPD = 65,
			HPGrowth = 3, ATKGrowth = 4, DEFGrowth = 2, SpAGrowth = 5, SpDGrowth = 3, SPDGrowth = 6,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Common,
			EvolvesTo = "plumestorm",
			EvolutionLevel = 22,
			BaseCatchRate = 0.5f,
			PossibleTraits = new() { "gale_spirit", "phantom_step", "skyborne" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "breeze_cut", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 8 },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 14 },
				new LearnableMove { MoveId = "razor_gale", LearnLevel = 20 }
			},
			BeastiaryNumber = 43
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "plumestorm",
			Name = "Plumestorm",
			Description = "A majestic bird that trails a storm of feathers wherever it flies. Legends say finding one of its feathers grants a single perfect day of weather.",
			IconPath = "ui/monsters/plumestorm/idle/plumestorm_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/plumestorm/idle/plumestorm_idle_01.png",
				"ui/monsters/plumestorm/idle/plumestorm_idle_02.png",
				"ui/monsters/plumestorm/idle/plumestorm_idle_03.png",
				"ui/monsters/plumestorm/idle/plumestorm_idle_04.png"
			},
			BaseHP = 60, BaseATK = 75, BaseDEF = 50, BaseSpA = 88, BaseSpD = 55, BaseSPD = 95,
			HPGrowth = 4, ATKGrowth = 6, DEFGrowth = 4, SpAGrowth = 7, SpDGrowth = 4, SPDGrowth = 8,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "featherwisp",
			BaseCatchRate = 0.25f,
			PossibleTraits = new() { "gale_spirit", "phantom_step", "skyborne" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "razor_gale", LearnLevel = 1, EvolvesFrom = "breeze_cut" },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 1 },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 26 },
				new LearnableMove { MoveId = "tempest", LearnLevel = 36 },
				new LearnableMove { MoveId = "annihilate", LearnLevel = 46 }
			},
			BeastiaryNumber = 44
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "vortexel",
			Name = "Vortexel",
			Description = "A creature that is actually a small, stable tornado that gained sentience. It's perpetually confused about why other creatures don't spin.",
			IconPath = "ui/monsters/vortexel/idle/vortexel_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/vortexel/idle/vortexel_idle_01.png",
				"ui/monsters/vortexel/idle/vortexel_idle_02.png",
				"ui/monsters/vortexel/idle/vortexel_idle_03.png",
				"ui/monsters/vortexel/idle/vortexel_idle_04.png"
			},
			BaseHP = 45, BaseATK = 60, BaseDEF = 35, BaseSpA = 72, BaseSpD = 42, BaseSPD = 85,
			HPGrowth = 4, ATKGrowth = 5, DEFGrowth = 3, SpAGrowth = 6, SpDGrowth = 3, SPDGrowth = 7,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "gale_spirit", "momentum", "reckless_charge" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "breeze_cut", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 10 },
				new LearnableMove { MoveId = "razor_gale", LearnLevel = 18 },
				new LearnableMove { MoveId = "tempest", LearnLevel = 28 },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 35 }
			},
			BeastiaryNumber = 45
		} );

		// ═══════════════════════════════════════════════════════════════
		// STORM SPIRE - ELECTRIC (#43-51)
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "sparklet",
			Name = "Sparklet",
			Description = "A living spark born when lightning struck the same place twice. It desperately seeks others of its kind but its touch destroys what it loves.",
			IconPath = "ui/monsters/sparklet/idle/sparklet_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/sparklet/idle/sparklet_idle_01.png",
				"ui/monsters/sparklet/idle/sparklet_idle_02.png",
				"ui/monsters/sparklet/idle/sparklet_idle_03.png",
				"ui/monsters/sparklet/idle/sparklet_idle_04.png"
			},
			BaseHP = 35, BaseATK = 55, BaseDEF = 30, BaseSpA = 68, BaseSpD = 32, BaseSPD = 70,
			HPGrowth = 3, ATKGrowth = 5, DEFGrowth = 2, SpAGrowth = 6, SpDGrowth = 2, SPDGrowth = 7,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Common,
			EvolvesTo = "voltweave",
			EvolutionLevel = 16,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "static_charge", "lightning_rod", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "static_jolt", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "nerve_lock", LearnLevel = 6 },
				new LearnableMove { MoveId = "volt_charge", LearnLevel = 12 }
			},
			BeastiaryNumber = 46
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "voltweave",
			Name = "Voltweave",
			Description = "A web of consciousness spun from storm-stolen thoughts. Each arc of electricity is a memory it has borrowed from those struck by lightning.",
			IconPath = "ui/monsters/voltweave/idle/voltweave_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/voltweave/idle/voltweave_idle_01.png",
				"ui/monsters/voltweave/idle/voltweave_idle_02.png",
				"ui/monsters/voltweave/idle/voltweave_idle_03.png",
				"ui/monsters/voltweave/idle/voltweave_idle_04.png"
			},
			BaseHP = 55, BaseATK = 85, BaseDEF = 50, BaseSpA = 98, BaseSpD = 52, BaseSPD = 100,
			HPGrowth = 4, ATKGrowth = 7, DEFGrowth = 4, SpAGrowth = 8, SpDGrowth = 4, SPDGrowth = 8,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "sparklet",
			EvolvesTo = "temporal",
			EvolutionLevel = 34,
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "static_charge", "lightning_rod", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "volt_charge", LearnLevel = 1, EvolvesFrom = "static_jolt" },
				new LearnableMove { MoveId = "nerve_lock", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 22 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 28 }
			},
			BeastiaryNumber = 47
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "temporal",
			Name = "Temporal",
			Description = "The pulse between heartbeats given form. It exists in the space between seconds, where time stutters and reality holds its breath.",
			IconPath = "ui/monsters/temporal/idle/temporal_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/temporal/idle/temporal_idle_01.png",
				"ui/monsters/temporal/idle/temporal_idle_02.png",
				"ui/monsters/temporal/idle/temporal_idle_03.png",
				"ui/monsters/temporal/idle/temporal_idle_04.png"
			},
			BaseHP = 70, BaseATK = 110, BaseDEF = 65, BaseSpA = 125, BaseSpD = 68, BaseSPD = 130,
			HPGrowth = 5, ATKGrowth = 9, DEFGrowth = 5, SpAGrowth = 10, SpDGrowth = 5, SPDGrowth = 10,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Rare,
			EvolvesFrom = "voltweave",
			BaseCatchRate = 0.15f,
			PossibleTraits = new() { "static_charge", "lightning_rod", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 1, EvolvesFrom = "volt_charge" },
				new LearnableMove { MoveId = "nerve_lock", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 1 },
				new LearnableMove { MoveId = "storm_strike", LearnLevel = 40 },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 48 },
				new LearnableMove { MoveId = "annihilate", LearnLevel = 56 }
			},
			BeastiaryNumber = 48
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "staticling",
			Name = "Staticling",
			Description = "A creature born from the frustration of electronics that don't work. It feeds on the anger of people hitting machines to make them function.",
			IconPath = "ui/monsters/staticling/idle/staticling_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/staticling/idle/staticling_idle_01.png",
				"ui/monsters/staticling/idle/staticling_idle_02.png",
				"ui/monsters/staticling/idle/staticling_idle_03.png",
				"ui/monsters/staticling/idle/staticling_idle_04.png"
			},
			BaseHP = 40, BaseATK = 60, BaseDEF = 35, BaseSpA = 72, BaseSpD = 38, BaseSPD = 75,
			HPGrowth = 3, ATKGrowth = 5, DEFGrowth = 3, SpAGrowth = 6, SpDGrowth = 3, SPDGrowth = 7,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.5f,
			PossibleTraits = new() { "static_charge", "trickster", "barbed_hide" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "static_jolt", LearnLevel = 1 },
				new LearnableMove { MoveId = "rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 8 },
				new LearnableMove { MoveId = "volt_charge", LearnLevel = 14 },
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 24 }
			},
			BeastiaryNumber = 49
		} );

		// Joltpaw 2-stage line
		AddSpecies( new MonsterSpecies
		{
			Id = "joltpaw",
			Name = "Joltpaw",
			Description = "A small rodent whose fur stands on end permanently. It accidentally shocks everyone it tries to befriend and has learned to express affection from a distance.",
			IconPath = "ui/monsters/joltpaw/idle/joltpaw_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/joltpaw/idle/joltpaw_idle_01.png",
				"ui/monsters/joltpaw/idle/joltpaw_idle_02.png",
				"ui/monsters/joltpaw/idle/joltpaw_idle_03.png",
				"ui/monsters/joltpaw/idle/joltpaw_idle_04.png"
			},
			BaseHP = 35, BaseATK = 50, BaseDEF = 30, BaseSpA = 62, BaseSpD = 35, BaseSPD = 75,
			HPGrowth = 3, ATKGrowth = 4, DEFGrowth = 2, SpAGrowth = 5, SpDGrowth = 3, SPDGrowth = 7,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Common,
			EvolvesTo = "thundermane",
			EvolutionLevel = 24,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "static_charge", "phantom_step", "barbed_hide" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "static_jolt", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 5 },
				new LearnableMove { MoveId = "volt_charge", LearnLevel = 12 },
				new LearnableMove { MoveId = "nerve_lock", LearnLevel = 18 }
			},
			BeastiaryNumber = 50
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "thundermane",
			Name = "Thundermane",
			Description = "A majestic lion whose mane crackles with contained storms. It has learned to control its power and can finally touch those it loves without hurting them.",
			IconPath = "ui/monsters/thundermane/idle/thundermane_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/thundermane/idle/thundermane_idle_01.png",
				"ui/monsters/thundermane/idle/thundermane_idle_02.png",
				"ui/monsters/thundermane/idle/thundermane_idle_03.png",
				"ui/monsters/thundermane/idle/thundermane_idle_04.png"
			},
			BaseHP = 70, BaseATK = 90, BaseDEF = 55, BaseSpA = 105, BaseSpD = 58, BaseSPD = 100,
			HPGrowth = 5, ATKGrowth = 7, DEFGrowth = 4, SpAGrowth = 8, SpDGrowth = 4, SPDGrowth = 8,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "joltpaw",
			BaseCatchRate = 0.25f,
			PossibleTraits = new() { "static_charge", "phantom_step", "barbed_hide" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "volt_charge", LearnLevel = 1, EvolvesFrom = "static_jolt" },
				new LearnableMove { MoveId = "nerve_lock", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 28 },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 34 },
				new LearnableMove { MoveId = "storm_strike", LearnLevel = 44 }
			},
			BeastiaryNumber = 51
		} );

		// Single-stage Electric creatures
		AddSpecies( new MonsterSpecies
		{
			Id = "zapfin",
			Name = "Zapfin",
			Description = "An electric eel that left the water to explore the land. It's constantly amazed that air doesn't conduct electricity as well as water.",
			IconPath = "ui/monsters/zapfin/idle/zapfin_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/zapfin/idle/zapfin_idle_01.png",
				"ui/monsters/zapfin/idle/zapfin_idle_02.png",
				"ui/monsters/zapfin/idle/zapfin_idle_03.png",
				"ui/monsters/zapfin/idle/zapfin_idle_04.png"
			},
			BaseHP = 50, BaseATK = 65, BaseDEF = 40, BaseSpA = 78, BaseSpD = 45, BaseSPD = 60,
			HPGrowth = 4, ATKGrowth = 6, DEFGrowth = 3, SpAGrowth = 6, SpDGrowth = 4, SPDGrowth = 5,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "static_charge", "aqua_siphon", "lightning_rod" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "static_jolt", LearnLevel = 1 },
				new LearnableMove { MoveId = "splash_jet", LearnLevel = 1 },
				new LearnableMove { MoveId = "volt_charge", LearnLevel = 10 },
				new LearnableMove { MoveId = "froth_barrage", LearnLevel = 16 },
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 24 },
				new LearnableMove { MoveId = "tidal_slam", LearnLevel = 30 }
			},
			BeastiaryNumber = 52
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "boltgeist",
			Name = "Boltgeist",
			Description = "The ghost of lightning that struck but didn't hit anything. It wanders eternally, looking for something worthy of being struck.",
			IconPath = "ui/monsters/boltgeist/idle/boltgeist_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/boltgeist/idle/boltgeist_idle_01.png",
				"ui/monsters/boltgeist/idle/boltgeist_idle_02.png",
				"ui/monsters/boltgeist/idle/boltgeist_idle_03.png",
				"ui/monsters/boltgeist/idle/boltgeist_idle_04.png"
			},
			BaseHP = 45, BaseATK = 80, BaseDEF = 35, BaseSpA = 95, BaseSpD = 42, BaseSPD = 95,
			HPGrowth = 3, ATKGrowth = 7, DEFGrowth = 3, SpAGrowth = 8, SpDGrowth = 3, SPDGrowth = 8,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.35f,
			PossibleTraits = new() { "static_charge", "phantom_step", "fortunate_strike" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "static_jolt", LearnLevel = 1 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 1 },
				new LearnableMove { MoveId = "nerve_lock", LearnLevel = 8 },
				new LearnableMove { MoveId = "volt_charge", LearnLevel = 14 },
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 22 },
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 30 }
			},
			BeastiaryNumber = 53
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "circuitsprite",
			Name = "Circuitsprite",
			Description = "A creature that lives in the flow of electricity itself. It perceives the world as pathways and resistances, and finds organic life bafflingly inefficient.",
			IconPath = "ui/monsters/circuitsprite/idle/circuitsprite_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/circuitsprite/idle/circuitsprite_idle_01.png",
				"ui/monsters/circuitsprite/idle/circuitsprite_idle_02.png",
				"ui/monsters/circuitsprite/idle/circuitsprite_idle_03.png",
				"ui/monsters/circuitsprite/idle/circuitsprite_idle_04.png"
			},
			BaseHP = 40, BaseATK = 55, BaseDEF = 45, BaseSpA = 68, BaseSpD = 52, BaseSPD = 80,
			HPGrowth = 3, ATKGrowth = 5, DEFGrowth = 4, SpAGrowth = 6, SpDGrowth = 4, SPDGrowth = 7,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.45f,
			PossibleTraits = new() { "static_charge", "momentum", "iron_will" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "static_jolt", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "nerve_lock", LearnLevel = 6 },
				new LearnableMove { MoveId = "volt_charge", LearnLevel = 12 },
				new LearnableMove { MoveId = "temper", LearnLevel = 18 },
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 26 }
			},
			BeastiaryNumber = 54
		} );

		// ═══════════════════════════════════════════════════════════════
		// ANCIENT RUINS - EARTH (#52-60)
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "rootling",
			Name = "Rootling",
			Description = "A small creature formed from the first footprint ever pressed into soil. It remkindles every step taken upon the earth.",
			IconPath = "ui/monsters/rootling/idle/rootling_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/rootling/idle/rootling_idle_01.png",
				"ui/monsters/rootling/idle/rootling_idle_02.png",
				"ui/monsters/rootling/idle/rootling_idle_03.png",
				"ui/monsters/rootling/idle/rootling_idle_04.png"
			},
			BaseHP = 55, BaseATK = 45, BaseDEF = 60, BaseSpA = 38, BaseSpD = 65, BaseSPD = 30,
			HPGrowth = 5, ATKGrowth = 4, DEFGrowth = 6, SpAGrowth = 3, SpDGrowth = 6, SPDGrowth = 2,
			Element = ElementType.Earth,
			BaseRarity = Rarity.Common,
			EvolvesTo = "cragmaw",
			EvolutionLevel = 18,
			BaseCatchRate = 0.65f,
			PossibleTraits = new() { "terra_force", "hardened_resolve", "enduring_will" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "mud_hurl", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 6 },
				new LearnableMove { MoveId = "boulder_toss", LearnLevel = 10 },
				new LearnableMove { MoveId = "earthrend", LearnLevel = 16 }
			},
			BeastiaryNumber = 55,
			IconOffsetX = 4f,
			IconOffsetY = 30f
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "cragmaw",
			Name = "Cragmaw",
			Description = "A walking cavern with teeth of stalactite. It doesn't hunt - it waits, and the earth reshapes to bring prey inside.",
			IconPath = "ui/monsters/cragmaw/idle/cragmaw_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/cragmaw/idle/cragmaw_idle_01.png",
				"ui/monsters/cragmaw/idle/cragmaw_idle_02.png",
				"ui/monsters/cragmaw/idle/cragmaw_idle_03.png",
				"ui/monsters/cragmaw/idle/cragmaw_idle_04.png"
			},
			BaseHP = 90, BaseATK = 70, BaseDEF = 90, BaseSpA = 55, BaseSpD = 95, BaseSPD = 40,
			HPGrowth = 7, ATKGrowth = 5, DEFGrowth = 8, SpAGrowth = 4, SpDGrowth = 8, SPDGrowth = 3,
			Element = ElementType.Earth,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "rootling",
			EvolvesTo = "monoleth",
			EvolutionLevel = 34,
			BaseCatchRate = 0.35f,
			PossibleTraits = new() { "terra_force", "hardened_resolve", "enduring_will" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "earthrend", LearnLevel = 1, EvolvesFrom = "mud_hurl" },
				new LearnableMove { MoveId = "boulder_toss", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "seismic_crash", LearnLevel = 22 },
				new LearnableMove { MoveId = "temper", LearnLevel = 28 },
				new LearnableMove { MoveId = "jagged_spike", LearnLevel = 32 }
			},
			BeastiaryNumber = 56
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "monoleth",
			Name = "Monoleth",
			Description = "A mountain that remkindleed it could move. Civilizations have risen and fallen on its back without ever knowing they walked upon something alive.",
			IconPath = "ui/monsters/monoleth/idle/monoleth_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/monoleth/idle/monoleth_idle_01.png",
				"ui/monsters/monoleth/idle/monoleth_idle_02.png",
				"ui/monsters/monoleth/idle/monoleth_idle_03.png",
				"ui/monsters/monoleth/idle/monoleth_idle_04.png"
			},
			BaseHP = 130, BaseATK = 95, BaseDEF = 120, BaseSpA = 72, BaseSpD = 125, BaseSPD = 45,
			HPGrowth = 9, ATKGrowth = 7, DEFGrowth = 10, SpAGrowth = 5, SpDGrowth = 10, SPDGrowth = 3,
			Element = ElementType.Earth,
			BaseRarity = Rarity.Rare,
			EvolvesFrom = "cragmaw",
			BaseCatchRate = 0.15f,
			PossibleTraits = new() { "terra_force", "hardened_resolve", "enduring_will" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "jagged_spike", LearnLevel = 1, EvolvesFrom = "earthrend" },
				new LearnableMove { MoveId = "seismic_crash", LearnLevel = 1 },
				new LearnableMove { MoveId = "temper", LearnLevel = 1 },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 40 },
				new LearnableMove { MoveId = "jagged_spike", LearnLevel = 48 },
				new LearnableMove { MoveId = "annihilate", LearnLevel = 56 }
			},
			BeastiaryNumber = 57
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "rubblekin",
			Name = "Rubblekin",
			Description = "A spirit that possesses fallen debris. It collects broken things and tries to rebuild them, though it never quite understands what they were supposed to be.",
			IconPath = "ui/monsters/rubblekin/idle/rubblekin_idle_01.png",
			AnimationFrames = new() { "ui/monsters/rubblekin/idle/rubblekin_idle_01.png", "ui/monsters/rubblekin/idle/rubblekin_idle_02.png", "ui/monsters/rubblekin/idle/rubblekin_idle_03.png", "ui/monsters/rubblekin/idle/rubblekin_idle_04.png" },
			BaseHP = 60, BaseATK = 50, BaseDEF = 70, BaseSpA = 42, BaseSpD = 75, BaseSPD = 25,
			HPGrowth = 5, ATKGrowth = 4, DEFGrowth = 6, SpAGrowth = 3, SpDGrowth = 6, SPDGrowth = 2,
			Element = ElementType.Earth,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "terra_force", "barbed_hide", "hardened_resolve" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "boulder_toss", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 6 },
				new LearnableMove { MoveId = "earthrend", LearnLevel = 12 },
				new LearnableMove { MoveId = "seismic_crash", LearnLevel = 20 },
				new LearnableMove { MoveId = "temper", LearnLevel = 28 }
			},
			BeastiaryNumber = 58
		} );

		// Pebblit 2-stage line
		AddSpecies( new MonsterSpecies
		{
			Id = "pebblit",
			Name = "Pebblit",
			Description = "A living pebble that rolls around collecting other small stones. It dreams of becoming a boulder someday but enjoys being small enough to fit in cozy places.",
			IconPath = "ui/monsters/pebblit/idle/pebblit_idle_01.png",
			AnimationFrames = new() { "ui/monsters/pebblit/idle/pebblit_idle_01.png", "ui/monsters/pebblit/idle/pebblit_idle_02.png", "ui/monsters/pebblit/idle/pebblit_idle_03.png", "ui/monsters/pebblit/idle/pebblit_idle_04.png" },
			BaseHP = 45, BaseATK = 35, BaseDEF = 55, BaseSpA = 28, BaseSpD = 58, BaseSPD = 35,
			HPGrowth = 4, ATKGrowth = 3, DEFGrowth = 5, SpAGrowth = 2, SpDGrowth = 5, SPDGrowth = 3,
			Element = ElementType.Earth,
			BaseRarity = Rarity.Common,
			EvolvesTo = "boulderon",
			EvolutionLevel = 26,
			BaseCatchRate = 0.6f,
			PossibleTraits = new() { "terra_force", "enduring_will", "reckless_charge" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "boulder_toss", LearnLevel = 8 },
				new LearnableMove { MoveId = "earthrend", LearnLevel = 14 },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 22 }
			},
			BeastiaryNumber = 59
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "boulderon",
			Name = "Boulderon",
			Description = "A massive rolling stone that gained wisdom from its journey. It has traveled so far that it no longer remkindles where it started, and that makes it sad.",
			IconPath = "ui/monsters/boulderon/idle/boulderon_idle_01.png",
			AnimationFrames = new() { "ui/monsters/boulderon/idle/boulderon_idle_01.png", "ui/monsters/boulderon/idle/boulderon_idle_02.png", "ui/monsters/boulderon/idle/boulderon_idle_03.png", "ui/monsters/boulderon/idle/boulderon_idle_04.png" },
			BaseHP = 95, BaseATK = 75, BaseDEF = 100, BaseSpA = 58, BaseSpD = 105, BaseSPD = 40,
			HPGrowth = 7, ATKGrowth = 6, DEFGrowth = 8, SpAGrowth = 4, SpDGrowth = 8, SPDGrowth = 3,
			Element = ElementType.Earth,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "pebblit",
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "terra_force", "enduring_will", "reckless_charge" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "earthrend", LearnLevel = 1, EvolvesFrom = "boulder_toss" },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "seismic_crash", LearnLevel = 30 },
				new LearnableMove { MoveId = "jagged_spike", LearnLevel = 38 },
				new LearnableMove { MoveId = "jagged_spike", LearnLevel = 46 }
			},
			BeastiaryNumber = 60
		} );

		// Single-stage Earth creatures
		AddSpecies( new MonsterSpecies
		{
			Id = "quartzite",
			Name = "Quartzite",
			Description = "A creature made of crystal-veined stone. It hums at frequencies that make other crystals vibrate, creating music only the earth can hear.",
			IconPath = "ui/monsters/quartzite/idle/quartzite_idle_01.png",
			AnimationFrames = new() { "ui/monsters/quartzite/idle/quartzite_idle_01.png", "ui/monsters/quartzite/idle/quartzite_idle_02.png", "ui/monsters/quartzite/idle/quartzite_idle_03.png", "ui/monsters/quartzite/idle/quartzite_idle_04.png" },
			BaseHP = 55, BaseATK = 60, BaseDEF = 65, BaseSpA = 72, BaseSpD = 70, BaseSPD = 35,
			HPGrowth = 4, ATKGrowth = 5, DEFGrowth = 6, SpAGrowth = 6, SpDGrowth = 6, SPDGrowth = 3,
			Element = ElementType.Earth,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "terra_force", "ethereal_blessing", "hardened_resolve" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "boulder_toss", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 8 },
				new LearnableMove { MoveId = "earthrend", LearnLevel = 14 },
				new LearnableMove { MoveId = "seismic_crash", LearnLevel = 22 },
				new LearnableMove { MoveId = "gleaming_ray", LearnLevel = 30 }
			},
			BeastiaryNumber = 62
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "dustback",
			Name = "Dustback",
			Description = "A turtle-like creature with an entire desert ecosystem on its shell. Tiny sandstorms form in miniature dunes when it walks.",
			IconPath = "ui/monsters/dustback/idle/dustback_idle_01.png",
			AnimationFrames = new() { "ui/monsters/dustback/idle/dustback_idle_01.png", "ui/monsters/dustback/idle/dustback_idle_02.png", "ui/monsters/dustback/idle/dustback_idle_03.png", "ui/monsters/dustback/idle/dustback_idle_04.png" },
			BaseHP = 70, BaseATK = 45, BaseDEF = 85, BaseSpA = 38, BaseSpD = 90, BaseSPD = 20,
			HPGrowth = 6, ATKGrowth = 3, DEFGrowth = 7, SpAGrowth = 3, SpDGrowth = 7, SPDGrowth = 1,
			Element = ElementType.Earth,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.5f,
			PossibleTraits = new() { "terra_force", "enduring_will", "hardened_resolve" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "mud_hurl", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 6 },
				new LearnableMove { MoveId = "earthrend", LearnLevel = 14 },
				new LearnableMove { MoveId = "temper", LearnLevel = 22 },
				new LearnableMove { MoveId = "jagged_spike", LearnLevel = 32 }
			},
			BeastiaryNumber = 63
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "terraclops",
			Name = "Terraclops",
			Description = "A one-eyed creature that sees through stone as easily as air. It guards ancient treasures that it has completely forgotten the purpose of.",
			IconPath = "ui/monsters/terraclops/idle/terraclops_idle_01.png",
			AnimationFrames = new() { "ui/monsters/terraclops/idle/terraclops_idle_01.png", "ui/monsters/terraclops/idle/terraclops_idle_02.png", "ui/monsters/terraclops/idle/terraclops_idle_03.png", "ui/monsters/terraclops/idle/terraclops_idle_04.png" },
			BaseHP = 80, BaseATK = 70, BaseDEF = 75, BaseSpA = 58, BaseSpD = 78, BaseSPD = 30,
			HPGrowth = 6, ATKGrowth = 5, DEFGrowth = 6, SpAGrowth = 4, SpDGrowth = 6, SPDGrowth = 2,
			Element = ElementType.Earth,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.35f,
			PossibleTraits = new() { "terra_force", "hunters_focus", "menacing_aura" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "boulder_toss", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 10 },
				new LearnableMove { MoveId = "earthrend", LearnLevel = 16 },
				new LearnableMove { MoveId = "seismic_crash", LearnLevel = 24 },
				new LearnableMove { MoveId = "jagged_spike", LearnLevel = 34 }
			},
			BeastiaryNumber = 64
		} );

		// ═══════════════════════════════════════════════════════════════
		// FROZEN VALE - ICE (#61-70)
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "frostling",
			Name = "Frostling",
			Description = "A shard of the first winter that ever was. It doesn't understand warmth and believes all things secretly divine_grace to be still and frozen.",
			IconPath = "ui/monsters/frostling/idle/frostling_idle_01.png",
			AnimationFrames = new() { "ui/monsters/frostling/idle/frostling_idle_01.png", "ui/monsters/frostling/idle/frostling_idle_02.png", "ui/monsters/frostling/idle/frostling_idle_03.png", "ui/monsters/frostling/idle/frostling_idle_04.png" },
			BaseHP = 45, BaseATK = 40, BaseDEF = 55, BaseSpA = 52, BaseSpD = 58, BaseSPD = 50,
			HPGrowth = 4, ATKGrowth = 3, DEFGrowth = 5, SpAGrowth = 4, SpDGrowth = 5, SPDGrowth = 4,
			Element = ElementType.Ice,
			BaseRarity = Rarity.Common,
			EvolvesTo = "glacimaw",
			EvolutionLevel = 20,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "frost_core", "thermal_hide", "hardened_resolve" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "frost_breath", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 5 },
				new LearnableMove { MoveId = "winter_veil", LearnLevel = 12 },
				new LearnableMove { MoveId = "permafrost_ray", LearnLevel = 18 }
			},
			BeastiaryNumber = 65
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "glacimaw",
			Name = "Glacimaw",
			Description = "Formed from the crystallized last words of those who froze alone. It speaks only in the voices of the dying, promising warmth it cannot give.",
			IconPath = "ui/monsters/glacimaw/idle/glacimaw_idle_01.png",
			AnimationFrames = new() { "ui/monsters/glacimaw/idle/glacimaw_idle_01.png", "ui/monsters/glacimaw/idle/glacimaw_idle_02.png", "ui/monsters/glacimaw/idle/glacimaw_idle_03.png", "ui/monsters/glacimaw/idle/glacimaw_idle_04.png" },
			BaseHP = 70, BaseATK = 65, BaseDEF = 85, BaseSpA = 78, BaseSpD = 88, BaseSPD = 60,
			HPGrowth = 5, ATKGrowth = 5, DEFGrowth = 7, SpAGrowth = 6, SpDGrowth = 7, SPDGrowth = 5,
			Element = ElementType.Ice,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "frostling",
			EvolvesTo = "permafrost",
			EvolutionLevel = 36,
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "frost_core", "thermal_hide", "hardened_resolve" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "permafrost_ray", LearnLevel = 1, EvolvesFrom = "frost_breath" },
				new LearnableMove { MoveId = "winter_veil", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "temper", LearnLevel = 24 },
				new LearnableMove { MoveId = "avalanche_wrath", LearnLevel = 32 }
			},
			BeastiaryNumber = 66
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "permafrost",
			Name = "Permafrost",
			Description = "The memory of a world that died of cold, walking. It is what remains when even stars forget how to burn. Patient. Inevitable. Waiting.",
			IconPath = "ui/monsters/permafrost/idle/permafrost_idle_01.png",
			AnimationFrames = new() { "ui/monsters/permafrost/idle/permafrost_idle_01.png", "ui/monsters/permafrost/idle/permafrost_idle_02.png", "ui/monsters/permafrost/idle/permafrost_idle_03.png", "ui/monsters/permafrost/idle/permafrost_idle_04.png" },
			BaseHP = 95, BaseATK = 85, BaseDEF = 115, BaseSpA = 102, BaseSpD = 118, BaseSPD = 65,
			HPGrowth = 7, ATKGrowth = 6, DEFGrowth = 9, SpAGrowth = 8, SpDGrowth = 9, SPDGrowth = 5,
			Element = ElementType.Ice,
			BaseRarity = Rarity.Rare,
			EvolvesFrom = "glacimaw",
			BaseCatchRate = 0.12f,
			PossibleTraits = new() { "frost_core", "thermal_hide", "hardened_resolve" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "avalanche_wrath", LearnLevel = 1, EvolvesFrom = "permafrost_ray" },
				new LearnableMove { MoveId = "temper", LearnLevel = 1 },
				new LearnableMove { MoveId = "winter_veil", LearnLevel = 1 },
				new LearnableMove { MoveId = "avalanche_wrath", LearnLevel = 42 },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 48 },
				new LearnableMove { MoveId = "annihilate", LearnLevel = 56 }
			},
			BeastiaryNumber = 67
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "shivershard",
			Name = "Shivershard",
			Description = "A creature made of the cold that seeps into bones. It doesn't freeze you - it simply reminds your body that warmth was never promised.",
			IconPath = "ui/monsters/shivershard/idle/shivershard_idle_01.png",
			AnimationFrames = new() { "ui/monsters/shivershard/idle/shivershard_idle_01.png", "ui/monsters/shivershard/idle/shivershard_idle_02.png", "ui/monsters/shivershard/idle/shivershard_idle_03.png", "ui/monsters/shivershard/idle/shivershard_idle_04.png" },
			BaseHP = 50, BaseATK = 55, BaseDEF = 60, BaseSpA = 68, BaseSpD = 62, BaseSPD = 45,
			HPGrowth = 4, ATKGrowth = 5, DEFGrowth = 5, SpAGrowth = 6, SpDGrowth = 5, SPDGrowth = 4,
			Element = ElementType.Ice,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "frost_core", "menacing_aura", "barbed_hide" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "frost_breath", LearnLevel = 1 },
				new LearnableMove { MoveId = "rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "winter_veil", LearnLevel = 8 },
				new LearnableMove { MoveId = "permafrost_ray", LearnLevel = 16 },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 24 },
				new LearnableMove { MoveId = "avalanche_wrath", LearnLevel = 34 }
			},
			BeastiaryNumber = 68
		} );

		// Snowmite 2-stage line
		AddSpecies( new MonsterSpecies
		{
			Id = "snowmite",
			Name = "Snowmite",
			Description = "A tiny creature that lives inside individual snowflakes. Each one is unique, and they take great pride in their patterns.",
			IconPath = "ui/monsters/snowmite/idle/snowmite_idle_01.png",
			AnimationFrames = new() { "ui/monsters/snowmite/idle/snowmite_idle_01.png", "ui/monsters/snowmite/idle/snowmite_idle_02.png", "ui/monsters/snowmite/idle/snowmite_idle_03.png", "ui/monsters/snowmite/idle/snowmite_idle_04.png" },
			BaseHP = 35, BaseATK = 40, BaseDEF = 45, BaseSpA = 52, BaseSpD = 48, BaseSPD = 55,
			HPGrowth = 3, ATKGrowth = 3, DEFGrowth = 4, SpAGrowth = 4, SpDGrowth = 4, SPDGrowth = 5,
			Element = ElementType.Ice,
			BaseRarity = Rarity.Common,
			EvolvesTo = "blizzardian",
			EvolutionLevel = 26,
			BaseCatchRate = 0.6f,
			PossibleTraits = new() { "frost_core", "phantom_step", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "frost_breath", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "winter_veil", LearnLevel = 8 },
				new LearnableMove { MoveId = "permafrost_ray", LearnLevel = 16 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 22 }
			},
			BeastiaryNumber = 69
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "blizzardian",
			Name = "Blizzardian",
			Description = "When enough Snowmites gather together, they form a Blizzardian - a walking snowstorm that remembers being thousands of unique individuals.",
			IconPath = "ui/monsters/blizzardian/idle/blizzardian_idle_01.png",
			AnimationFrames = new() { "ui/monsters/blizzardian/idle/blizzardian_idle_01.png", "ui/monsters/blizzardian/idle/blizzardian_idle_02.png", "ui/monsters/blizzardian/idle/blizzardian_idle_03.png", "ui/monsters/blizzardian/idle/blizzardian_idle_04.png" },
			BaseHP = 70, BaseATK = 75, BaseDEF = 70, BaseSpA = 88, BaseSpD = 72, BaseSPD = 80,
			HPGrowth = 5, ATKGrowth = 6, DEFGrowth = 5, SpAGrowth = 7, SpDGrowth = 5, SPDGrowth = 6,
			Element = ElementType.Ice,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "snowmite",
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "frost_core", "phantom_step", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "permafrost_ray", LearnLevel = 1, EvolvesFrom = "frost_breath" },
				new LearnableMove { MoveId = "winter_veil", LearnLevel = 1 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 1 },
				new LearnableMove { MoveId = "avalanche_wrath", LearnLevel = 30 },
				new LearnableMove { MoveId = "avalanche_wrath", LearnLevel = 40 },
				new LearnableMove { MoveId = "tempest", LearnLevel = 48 }
			},
			BeastiaryNumber = 70
		} );

		// Single-stage Ice creatures
		AddSpecies( new MonsterSpecies
		{
			Id = "iciclaw",
			Name = "Iciclaw",
			Description = "A predator made of living ice. It hunts by waiting motionless until prey mistakes it for a harmless ice formation.",
			IconPath = "ui/monsters/iciclaw/idle/iciclaw_idle_01.png",
			AnimationFrames = new() { "ui/monsters/iciclaw/idle/iciclaw_idle_01.png", "ui/monsters/iciclaw/idle/iciclaw_idle_02.png", "ui/monsters/iciclaw/idle/iciclaw_idle_03.png", "ui/monsters/iciclaw/idle/iciclaw_idle_04.png" },
			BaseHP = 55, BaseATK = 70, BaseDEF = 50, BaseSpA = 45, BaseSpD = 52, BaseSPD = 65,
			HPGrowth = 4, ATKGrowth = 6, DEFGrowth = 4, SpAGrowth = 4, SpDGrowth = 4, SPDGrowth = 5,
			Element = ElementType.Ice,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.45f,
			PossibleTraits = new() { "frost_core", "fortunate_strike", "precision_hunter" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "frost_breath", LearnLevel = 1 },
				new LearnableMove { MoveId = "rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 6 },
				new LearnableMove { MoveId = "vicious_cut", LearnLevel = 14 },
				new LearnableMove { MoveId = "permafrost_ray", LearnLevel = 22 },
				new LearnableMove { MoveId = "avalanche_wrath", LearnLevel = 32 }
			},
			BeastiaryNumber = 71
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "frostwisp",
			Name = "Frostwisp",
			Description = "The ghost of breath that froze mid-exhale. It drifts through cold places, leaving tiny ice crystals that spell out words no one can read.",
			IconPath = "ui/monsters/frostwisp/idle/frostwisp_idle_01.png",
			AnimationFrames = new() { "ui/monsters/frostwisp/idle/frostwisp_idle_01.png", "ui/monsters/frostwisp/idle/frostwisp_idle_02.png", "ui/monsters/frostwisp/idle/frostwisp_idle_03.png", "ui/monsters/frostwisp/idle/frostwisp_idle_04.png" },
			BaseHP = 40, BaseATK = 50, BaseDEF = 40, BaseSpA = 62, BaseSpD = 45, BaseSPD = 70,
			HPGrowth = 3, ATKGrowth = 4, DEFGrowth = 3, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 6,
			Element = ElementType.Ice,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.5f,
			PossibleTraits = new() { "frost_core", "phantom_step", "trickster" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "frost_breath", LearnLevel = 1 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 1 },
				new LearnableMove { MoveId = "winter_veil", LearnLevel = 8 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 14 },
				new LearnableMove { MoveId = "permafrost_ray", LearnLevel = 22 },
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 30 }
			},
			BeastiaryNumber = 72
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "sleethorn",
			Name = "Sleethorn",
			Description = "A creature covered in razor-sharp ice horns. It's actually quite gentle but has learned to keep its distance from others for their safety.",
			IconPath = "ui/monsters/sleethorn/idle/sleethorn_idle_01.png",
			AnimationFrames = new() { "ui/monsters/sleethorn/idle/sleethorn_idle_01.png", "ui/monsters/sleethorn/idle/sleethorn_idle_02.png", "ui/monsters/sleethorn/idle/sleethorn_idle_03.png", "ui/monsters/sleethorn/idle/sleethorn_idle_04.png" },
			BaseHP = 60, BaseATK = 65, BaseDEF = 60, BaseSpA = 52, BaseSpD = 62, BaseSPD = 50,
			HPGrowth = 5, ATKGrowth = 5, DEFGrowth = 5, SpAGrowth = 4, SpDGrowth = 5, SPDGrowth = 4,
			Element = ElementType.Ice,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "frost_core", "barbed_hide", "cleansing_retreat" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "frost_breath", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "winter_veil", LearnLevel = 8 },
				new LearnableMove { MoveId = "permafrost_ray", LearnLevel = 16 },
				new LearnableMove { MoveId = "crushing_blow", LearnLevel = 24 },
				new LearnableMove { MoveId = "avalanche_wrath", LearnLevel = 34 }
			},
			BeastiaryNumber = 73
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "glacierback",
			Name = "Glacierback",
			Description = "A massive creature that carries an actual glacier on its shell. Entire ecosystems of ice-dwelling creatures live on its back without knowing they're mobile.",
			IconPath = "ui/monsters/glacierback/idle/glacierback_idle_01.png",
			AnimationFrames = new() { "ui/monsters/glacierback/idle/glacierback_idle_01.png", "ui/monsters/glacierback/idle/glacierback_idle_02.png", "ui/monsters/glacierback/idle/glacierback_idle_03.png", "ui/monsters/glacierback/idle/glacierback_idle_04.png" },
			BaseHP = 100, BaseATK = 60, BaseDEF = 95, BaseSpA = 52, BaseSpD = 98, BaseSPD = 15,
			HPGrowth = 8, ATKGrowth = 4, DEFGrowth = 8, SpAGrowth = 4, SpDGrowth = 8, SPDGrowth = 1,
			Element = ElementType.Ice,
			BaseRarity = Rarity.Rare,
			BaseCatchRate = 0.2f,
			PossibleTraits = new() { "frost_core", "enduring_will", "hardened_resolve" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "frost_breath", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 8 },
				new LearnableMove { MoveId = "permafrost_ray", LearnLevel = 18 },
				new LearnableMove { MoveId = "temper", LearnLevel = 28 },
				new LearnableMove { MoveId = "avalanche_wrath", LearnLevel = 40 }
			},
			BeastiaryNumber = 74
		} );

		// ═══════════════════════════════════════════════════════════════
		// OVERGROWN HEART - NATURE (#71-80)
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "sproutkin",
			Name = "Sproutkin",
			Description = "A wandering seed that forgot where it was supposed to grow. It carries the potential of fodeep_slumbers that will never exist, dreaming of roots it will never have.",
			IconPath = "ui/monsters/sproutkin/idle/sproutkin_idle_01.png",
			AnimationFrames = new() { "ui/monsters/sproutkin/idle/sproutkin_idle_01.png", "ui/monsters/sproutkin/idle/sproutkin_idle_02.png", "ui/monsters/sproutkin/idle/sproutkin_idle_03.png", "ui/monsters/sproutkin/idle/sproutkin_idle_04.png" },
			BaseHP = 50, BaseATK = 35, BaseDEF = 50, BaseSpA = 55, BaseSpD = 50, BaseSPD = 45,
			HPGrowth = 5, ATKGrowth = 3, DEFGrowth = 5, SpAGrowth = 5, SpDGrowth = 5, SPDGrowth = 4,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Common,
			EvolvesTo = "thornveil",
			EvolutionLevel = 18,
			BaseCatchRate = 0.6f,
			PossibleTraits = new() { "verdant_power", "wild_harden", "vital_recovery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "thorn_lash", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 1 },
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 6 },
				new LearnableMove { MoveId = "nature_shield", LearnLevel = 10 },
				new LearnableMove { MoveId = "blade_leaf", LearnLevel = 15 }
			},
			BeastiaryNumber = 75
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "thornveil",
			Name = "Thornveil",
			Description = "A mass of vines that learned to think by strangling a forgotten god. It grows not toward sunlight but toward secrets, and its thorns drink memories.",
			IconPath = "ui/monsters/thornveil/idle/thornveil_idle_01.png",
			AnimationFrames = new() { "ui/monsters/thornveil/idle/thornveil_idle_01.png", "ui/monsters/thornveil/idle/thornveil_idle_02.png", "ui/monsters/thornveil/idle/thornveil_idle_03.png", "ui/monsters/thornveil/idle/thornveil_idle_04.png" },
			BaseHP = 75, BaseATK = 70, BaseDEF = 70, BaseSpA = 80, BaseSpD = 70, BaseSPD = 55,
			HPGrowth = 6, ATKGrowth = 6, DEFGrowth = 6, SpAGrowth = 6, SpDGrowth = 6, SPDGrowth = 4,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "sproutkin",
			EvolvesTo = "eldergrove",
			EvolutionLevel = 36,
			BaseCatchRate = 0.35f,
			PossibleTraits = new() { "verdant_power", "wild_harden", "vital_recovery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "vitality_burst", LearnLevel = 1, EvolvesFrom = "thorn_lash" },
				new LearnableMove { MoveId = "vitality_burst", LearnLevel = 1, EvolvesFrom = "soul_siphon" },
				new LearnableMove { MoveId = "blade_leaf", LearnLevel = 1 },
				new LearnableMove { MoveId = "nature_shield", LearnLevel = 1 },
				new LearnableMove { MoveId = "thorn_barrage", LearnLevel = 24 },
				new LearnableMove { MoveId = "leech_seed", LearnLevel = 30 }
			},
			BeastiaryNumber = 76
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "eldergrove",
			Name = "Eldergrove",
			Description = "An entire fodeep_slumber compressed into a single being. Every tree that ever was or will be exists somewhere in its rings. It speaks in the language of harden and decay.",
			IconPath = "ui/monsters/eldergrove/idle/eldergrove_idle_01.png",
			AnimationFrames = new() { "ui/monsters/eldergrove/idle/eldergrove_idle_01.png", "ui/monsters/eldergrove/idle/eldergrove_idle_02.png", "ui/monsters/eldergrove/idle/eldergrove_idle_03.png", "ui/monsters/eldergrove/idle/eldergrove_idle_04.png" },
			BaseHP = 110, BaseATK = 90, BaseDEF = 95, BaseSpA = 100, BaseSpD = 95, BaseSPD = 50,
			HPGrowth = 8, ATKGrowth = 7, DEFGrowth = 7, SpAGrowth = 8, SpDGrowth = 7, SPDGrowth = 4,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Rare,
			EvolvesFrom = "thornveil",
			BaseCatchRate = 0.15f,
			PossibleTraits = new() { "verdant_power", "wild_harden", "vital_recovery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "solstice_beam", LearnLevel = 1, EvolvesFrom = "vitality_burst" },
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 1, EvolvesFrom = "vitality_burst" },
				new LearnableMove { MoveId = "thorn_barrage", LearnLevel = 1 },
				new LearnableMove { MoveId = "leech_seed", LearnLevel = 1 },
				new LearnableMove { MoveId = "wood_hammer", LearnLevel = 42 },
				new LearnableMove { MoveId = "divine_grace", LearnLevel = 50 }
			},
			BeastiaryNumber = 77
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "mosswhisper",
			Name = "Mosswhisper",
			Description = "A creature that grows in places where secrets were buried. It soul_siphons confessions whispered to empty rooms and hums them back at inappropriate times.",
			IconPath = "ui/monsters/mosswhisper/idle/mosswhisper_idle_01.png",
			BaseHP = 55, BaseATK = 40, BaseDEF = 55, BaseSpA = 65, BaseSpD = 60, BaseSPD = 50,
			HPGrowth = 5, ATKGrowth = 4, DEFGrowth = 5, SpAGrowth = 5, SpDGrowth = 5, SPDGrowth = 4,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.5f,
			PossibleTraits = new() { "verdant_power", "phantom_step", "menacing_aura" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 8 },
				new LearnableMove { MoveId = "blade_leaf", LearnLevel = 14 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 20 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/mosswhisper/idle/mosswhisper_idle_01.png",
				"ui/monsters/mosswhisper/idle/mosswhisper_idle_02.png",
				"ui/monsters/mosswhisper/idle/mosswhisper_idle_03.png",
				"ui/monsters/mosswhisper/idle/mosswhisper_idle_04.png"
			},
			BeastiaryNumber = 78
		} );

		// Pollenpuff 2-stage line
		AddSpecies( new MonsterSpecies
		{
			Id = "pollenpuff",
			Name = "Pollenpuff",
			Description = "A fluffy creature made entirely of pollen. It spreads flowers wherever it bounces, though it's terribly allergic to itself and constantly sneezes.",
			IconPath = "ui/monsters/pollenpuff/idle/pollenpuff_idle_01.png",
			BaseHP = 40, BaseATK = 35, BaseDEF = 40, BaseSpA = 50, BaseSpD = 45, BaseSPD = 55,
			HPGrowth = 3, ATKGrowth = 3, DEFGrowth = 3, SpAGrowth = 4, SpDGrowth = 4, SPDGrowth = 5,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Common,
			EvolvesTo = "bloomguard",
			EvolutionLevel = 22,
			BaseCatchRate = 0.6f,
			PossibleTraits = new() { "verdant_power", "phantom_step", "adrenaline_rush" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 1 },
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 7 },
				new LearnableMove { MoveId = "cotton_guard", LearnLevel = 12 },
				new LearnableMove { MoveId = "pollen_burst", LearnLevel = 18 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/pollenpuff/idle/pollenpuff_idle_01.png",
				"ui/monsters/pollenpuff/idle/pollenpuff_idle_02.png",
				"ui/monsters/pollenpuff/idle/pollenpuff_idle_03.png",
				"ui/monsters/pollenpuff/idle/pollenpuff_idle_04.png"
			},
			BeastiaryNumber = 79
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "bloomguard",
			Name = "Bloomguard",
			Description = "A noble flower knight that protects gardens from those who would pick without permission. It has overcome its allergy and now commands pollen armies.",
			IconPath = "ui/monsters/bloomguard/idle/bloomguard_idle_01.png",
			BaseHP = 70, BaseATK = 65, BaseDEF = 75, BaseSpA = 75, BaseSpD = 70, BaseSPD = 60,
			HPGrowth = 5, ATKGrowth = 5, DEFGrowth = 6, SpAGrowth = 6, SpDGrowth = 5, SPDGrowth = 5,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "pollenpuff",
			BaseCatchRate = 0.35f,
			PossibleTraits = new() { "verdant_power", "phantom_step", "adrenaline_rush" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "petal_dance", LearnLevel = 1, EvolvesFrom = "pollen_burst" },
				new LearnableMove { MoveId = "vitality_burst", LearnLevel = 1, EvolvesFrom = "soul_siphon" },
				new LearnableMove { MoveId = "cotton_guard", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 1 },
				new LearnableMove { MoveId = "leaf_blade", LearnLevel = 28 },
				new LearnableMove { MoveId = "divine_grace", LearnLevel = 35 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/bloomguard/idle/bloomguard_idle_01.png",
				"ui/monsters/bloomguard/idle/bloomguard_idle_02.png",
				"ui/monsters/bloomguard/idle/bloomguard_idle_03.png",
				"ui/monsters/bloomguard/idle/bloomguard_idle_04.png"
			},
			BeastiaryNumber = 80
		} );

		// Single-stage Nature creatures
		AddSpecies( new MonsterSpecies
		{
			Id = "vinewhip",
			Name = "Vinewhip",
			Description = "A tempered vine that moves like a snake. It root_binds around trees for comfort and gets terribly lonely when there's nothing nearby to hug.",
			IconPath = "ui/monsters/vinewhip/idle/vinewhip_idle_01.png",
			BaseHP = 50, BaseATK = 55, BaseDEF = 50, BaseSpA = 45, BaseSpD = 45, BaseSPD = 60,
			HPGrowth = 4, ATKGrowth = 5, DEFGrowth = 4, SpAGrowth = 4, SpDGrowth = 4, SPDGrowth = 5,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.5f,
			PossibleTraits = new() { "verdant_power", "barbed_hide", "reckless_charge" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "thorn_lash", LearnLevel = 1 },
				new LearnableMove { MoveId = "root_bind", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 8 },
				new LearnableMove { MoveId = "thorn_lash", LearnLevel = 16 },
				new LearnableMove { MoveId = "root_bind", LearnLevel = 22 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/vinewhip/idle/vinewhip_idle_01.png",
				"ui/monsters/vinewhip/idle/vinewhip_idle_02.png",
				"ui/monsters/vinewhip/idle/vinewhip_idle_03.png",
				"ui/monsters/vinewhip/idle/vinewhip_idle_04.png"
			},
			BeastiaryNumber = 81
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "funharden",
			Name = "Funharden",
			Description = "A mushroom creature that communicates through underground networks. It knows what every plant within a mile is feeling but has no way to express itself.",
			IconPath = "ui/monsters/funharden/idle/funharden_idle_01.png",
			BaseHP = 65, BaseATK = 45, BaseDEF = 60, BaseSpA = 70, BaseSpD = 75, BaseSPD = 35,
			HPGrowth = 5, ATKGrowth = 4, DEFGrowth = 5, SpAGrowth = 6, SpDGrowth = 6, SPDGrowth = 3,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.45f,
			PossibleTraits = new() { "verdant_power", "hardened_resolve", "menacing_aura" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 1 },
				new LearnableMove { MoveId = "vitality_burst", LearnLevel = 10 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 18 },
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 26 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/funharden/idle/funharden_idle_01.png",
				"ui/monsters/funharden/idle/funharden_idle_02.png",
				"ui/monsters/funharden/idle/funharden_idle_03.png",
				"ui/monsters/funharden/idle/funharden_idle_04.png"
			},
			BeastiaryNumber = 82
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "willowwisp",
			Name = "Willowwisp",
			Description = "A spirit that lives in weeping willows, collecting the tears the trees shed. It believes the trees are sad and tries to cheer them up, not understanding they're just shaped that way.",
			IconPath = "ui/monsters/willowwisp/idle/willowwisp_idle_01.png",
			BaseHP = 45, BaseATK = 40, BaseDEF = 45, BaseSpA = 65, BaseSpD = 50, BaseSPD = 65,
			HPGrowth = 4, ATKGrowth = 3, DEFGrowth = 4, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 6,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.5f,
			PossibleTraits = new() { "verdant_power", "phantom_step", "trickster" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "thorn_lash", LearnLevel = 1 },
				new LearnableMove { MoveId = "cinders_curse", LearnLevel = 1 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 10 },
				new LearnableMove { MoveId = "vitality_burst", LearnLevel = 18 },
				new LearnableMove { MoveId = "divine_grace", LearnLevel = 24 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/willowwisp/idle/willowwisp_idle_01.png",
				"ui/monsters/willowwisp/idle/willowwisp_idle_02.png",
				"ui/monsters/willowwisp/idle/willowwisp_idle_03.png",
				"ui/monsters/willowwisp/idle/willowwisp_idle_04.png"
			},
			BeastiaryNumber = 83
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "verdantis",
			Name = "Verdantis",
			Description = "A creature that is spring itself given form. Where it walks, flowers bloom out of season and dormant seeds spontaneously sprout.",
			IconPath = "ui/monsters/verdantis/idle/verdantis_idle_01.png",
			BaseHP = 80, BaseATK = 70, BaseDEF = 70, BaseSpA = 85, BaseSpD = 75, BaseSPD = 70,
			HPGrowth = 6, ATKGrowth = 5, DEFGrowth = 5, SpAGrowth = 6, SpDGrowth = 5, SPDGrowth = 5,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Rare,
			BaseCatchRate = 0.2f,
			PossibleTraits = new() { "verdant_power", "wild_harden", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "vitality_burst", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "petal_dance", LearnLevel = 15 },
				new LearnableMove { MoveId = "solstice_beam", LearnLevel = 25 },
				new LearnableMove { MoveId = "divine_grace", LearnLevel = 35 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/verdantis/idle/verdantis_idle_01.png",
				"ui/monsters/verdantis/idle/verdantis_idle_02.png",
				"ui/monsters/verdantis/idle/verdantis_idle_03.png",
				"ui/monsters/verdantis/idle/verdantis_idle_04.png"
			},
			BeastiaryNumber = 85
		} );

		// ═══════════════════════════════════════════════════════════════
		// RUSTED FOUNDRY - METAL (#81-91)
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "coglet",
			Name = "Coglet",
			Description = "A small spirit born from a gear that fell from something vast and unknowable. It spins endlessly, believing that if it stops, some great machine will fail.",
			IconPath = "ui/monsters/coglet/idle/coglet_idle_01.png",
			BaseHP = 50, BaseATK = 45, BaseDEF = 60, BaseSpA = 35, BaseSpD = 55, BaseSPD = 40,
			HPGrowth = 4, ATKGrowth = 4, DEFGrowth = 6, SpAGrowth = 3, SpDGrowth = 5, SPDGrowth = 3,
			Element = ElementType.Metal,
			BaseRarity = Rarity.Common,
			EvolvesTo = "ironclad",
			EvolutionLevel = 20,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "iron_will", "hardened_resolve", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "steel_rake", LearnLevel = 8 },
				new LearnableMove { MoveId = "temper", LearnLevel = 14 },
				new LearnableMove { MoveId = "gear_grind", LearnLevel = 18 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/coglet/idle/coglet_idle_01.png",
				"ui/monsters/coglet/idle/coglet_idle_02.png",
				"ui/monsters/coglet/idle/coglet_idle_03.png",
				"ui/monsters/coglet/idle/coglet_idle_04.png"
			},
			BeastiaryNumber = 86
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "ironclad",
			Name = "Ironclad",
			Description = "An amalgamation of swords that refused to draw blood. They bound themselves together in shame, becoming a creature that protects rather than destroys.",
			IconPath = "ui/monsters/ironclad/idle/ironclad_idle_01.png",
			BaseHP = 80, BaseATK = 75, BaseDEF = 95, BaseSpA = 50, BaseSpD = 85, BaseSPD = 45,
			HPGrowth = 6, ATKGrowth = 6, DEFGrowth = 8, SpAGrowth = 4, SpDGrowth = 7, SPDGrowth = 3,
			Element = ElementType.Metal,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "coglet",
			EvolvesTo = "forgeborn",
			EvolutionLevel = 38,
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "iron_will", "hardened_resolve", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "iron_rush", LearnLevel = 1, EvolvesFrom = "steel_rake" },
				new LearnableMove { MoveId = "gear_grind", LearnLevel = 1 },
				new LearnableMove { MoveId = "temper", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "heavy_slam", LearnLevel = 26 },
				new LearnableMove { MoveId = "metal_burst", LearnLevel = 34 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/ironclad/idle/ironclad_idle_01.png",
				"ui/monsters/ironclad/idle/ironclad_idle_02.png",
				"ui/monsters/ironclad/idle/ironclad_idle_03.png",
				"ui/monsters/ironclad/idle/ironclad_idle_04.png"
			},
			BeastiaryNumber = 87
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "forgeborn",
			Name = "Forgeborn",
			Description = "The first metal that was ever smelted, remkindleing when it was ore, remkindleing when it was mountain, choosing to become something new. It is the moment of transformation incarnate.",
			IconPath = "ui/monsters/forgeborn/idle/forgeborn_idle_01.png",
			BaseHP = 105, BaseATK = 100, BaseDEF = 115, BaseSpA = 65, BaseSpD = 100, BaseSPD = 55,
			HPGrowth = 7, ATKGrowth = 8, DEFGrowth = 9, SpAGrowth = 5, SpDGrowth = 8, SPDGrowth = 4,
			Element = ElementType.Metal,
			BaseRarity = Rarity.Rare,
			EvolvesFrom = "ironclad",
			BaseCatchRate = 0.12f,
			PossibleTraits = new() { "iron_will", "hardened_resolve", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "meteor_mash", LearnLevel = 1, EvolvesFrom = "iron_rush" },
				new LearnableMove { MoveId = "heavy_slam", LearnLevel = 1 },
				new LearnableMove { MoveId = "metal_burst", LearnLevel = 1 },
				new LearnableMove { MoveId = "temper", LearnLevel = 1 },
				new LearnableMove { MoveId = "gleaming_ray", LearnLevel = 45 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 52 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/forgeborn/idle/forgeborn_idle_01.png",
				"ui/monsters/forgeborn/idle/forgeborn_idle_02.png",
				"ui/monsters/forgeborn/idle/forgeborn_idle_03.png",
				"ui/monsters/forgeborn/idle/forgeborn_idle_04.png"
			},
			BeastiaryNumber = 88
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "corrode",
			Name = "Corrode",
			Description = "A spirit of entropy that clings to metal. It believes it is helping things return to their natural state, unaware that it is feared rather than thanked.",
			IconPath = "ui/monsters/corrode/idle/corrode_idle_01.png",
			BaseHP = 55, BaseATK = 60, BaseDEF = 45, BaseSpA = 72, BaseSpD = 48, BaseSPD = 50,
			HPGrowth = 4, ATKGrowth = 5, DEFGrowth = 4, SpAGrowth = 6, SpDGrowth = 4, SPDGrowth = 4,
			Element = ElementType.Metal,
			BaseRarity = Rarity.Uncommon,
			EvolvesTo = "oxidrake",
			EvolutionLevel = 30,
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "iron_will", "barbed_hide", "menacing_aura" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 1 },
				new LearnableMove { MoveId = "steel_rake", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 12 },
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 18 },
				new LearnableMove { MoveId = "iron_rush", LearnLevel = 26 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/corrode/idle/corrode_idle_01.png",
				"ui/monsters/corrode/idle/corrode_idle_02.png",
				"ui/monsters/corrode/idle/corrode_idle_03.png",
				"ui/monsters/corrode/idle/corrode_idle_04.png"
			},
			BeastiaryNumber = 89
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "oxidrake",
			Name = "Oxidrake",
			Description = "A dragon made of every rusted sword, every fallen crown, every abandoned machine. It is the graveyard of human ambition given wings.",
			IconPath = "ui/monsters/oxidrake/idle/oxidrake_idle_01.png",
			BaseHP = 87, BaseATK = 98, BaseDEF = 72, BaseSpA = 93, BaseSpD = 68, BaseSPD = 77,
			HPGrowth = 6, ATKGrowth = 8, DEFGrowth = 5, SpAGrowth = 7, SpDGrowth = 5, SPDGrowth = 6,
			Element = ElementType.Metal,
			BaseRarity = Rarity.Rare,
			EvolvesFrom = "corrode",
			BaseCatchRate = 0.18f,
			PossibleTraits = new() { "iron_will", "barbed_hide", "menacing_aura" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "gleaming_ray", LearnLevel = 1, EvolvesFrom = "void_sphere" },
				new LearnableMove { MoveId = "iron_rush", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 1 },
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 1 },
				new LearnableMove { MoveId = "umbral_claw", LearnLevel = 36 },
				new LearnableMove { MoveId = "corrosive_breath", LearnLevel = 44 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/oxidrake/idle/oxidrake_idle_01.png",
				"ui/monsters/oxidrake/idle/oxidrake_idle_02.png",
				"ui/monsters/oxidrake/idle/oxidrake_idle_03.png",
				"ui/monsters/oxidrake/idle/oxidrake_idle_04.png"
			},
			BeastiaryNumber = 90
		} );

		// Scrapper 2-stage line
		AddSpecies( new MonsterSpecies
		{
			Id = "scrapper",
			Name = "Scrapper",
			Description = "A small creature assembled from discarded scrap metal. It adds pieces to itself constantly, dreaming of becoming a mighty machine.",
			IconPath = "ui/monsters/scrapper/idle/scrapper_idle_01.png",
			BaseHP = 48, BaseATK = 52, BaseDEF = 58, BaseSpA = 32, BaseSpD = 52, BaseSPD = 33,
			HPGrowth = 4, ATKGrowth = 4, DEFGrowth = 5, SpAGrowth = 3, SpDGrowth = 4, SPDGrowth = 3,
			Element = ElementType.Metal,
			BaseRarity = Rarity.Common,
			EvolvesTo = "junktitan",
			EvolutionLevel = 28,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "iron_will", "reckless_charge", "titanic_might" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "steel_rake", LearnLevel = 7 },
				new LearnableMove { MoveId = "magnet_rise", LearnLevel = 14 },
				new LearnableMove { MoveId = "iron_rush", LearnLevel = 22 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/scrapper/idle/scrapper_idle_01.png",
				"ui/monsters/scrapper/idle/scrapper_idle_02.png",
				"ui/monsters/scrapper/idle/scrapper_idle_03.png",
				"ui/monsters/scrapper/idle/scrapper_idle_04.png"
			},
			BeastiaryNumber = 91
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "junktitan",
			Name = "Junktitan",
			Description = "A massive construct assembled from an entire junkyard's worth of metal. It finally achieved its dream of becoming mighty, but now worries about being dismantled for parts.",
			IconPath = "ui/monsters/junktitan/idle/junktitan_idle_01.png",
			BaseHP = 98, BaseATK = 88, BaseDEF = 92, BaseSpA = 45, BaseSpD = 78, BaseSPD = 38,
			HPGrowth = 7, ATKGrowth = 6, DEFGrowth = 7, SpAGrowth = 3, SpDGrowth = 6, SPDGrowth = 3,
			Element = ElementType.Metal,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "scrapper",
			BaseCatchRate = 0.25f,
			PossibleTraits = new() { "iron_will", "reckless_charge", "titanic_might" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "heavy_slam", LearnLevel = 1, EvolvesFrom = "iron_rush" },
				new LearnableMove { MoveId = "steel_rake", LearnLevel = 1 },
				new LearnableMove { MoveId = "magnet_rise", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "gyro_ball", LearnLevel = 34 },
				new LearnableMove { MoveId = "jagged_spike", LearnLevel = 42 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/junktitan/idle/junktitan_idle_01.png",
				"ui/monsters/junktitan/idle/junktitan_idle_02.png",
				"ui/monsters/junktitan/idle/junktitan_idle_03.png",
				"ui/monsters/junktitan/idle/junktitan_idle_04.png"
			},
			BeastiaryNumber = 92
		} );

		// Single-stage Metal creatures
		AddSpecies( new MonsterSpecies
		{
			Id = "bladefly",
			Name = "Bladefly",
			Description = "An insect with wings of sharpened steel. It doesn't mean to cut things - it just can't help that it's made of razors.",
			IconPath = "ui/monsters/bladefly/idle/bladefly_idle_01.png",
			BaseHP = 37, BaseATK = 68, BaseDEF = 42, BaseSpA = 38, BaseSpD = 36, BaseSPD = 83,
			HPGrowth = 3, ATKGrowth = 6, DEFGrowth = 3, SpAGrowth = 3, SpDGrowth = 3, SPDGrowth = 7,
			Element = ElementType.Metal,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.5f,
			PossibleTraits = new() { "iron_will", "barbed_hide", "fortunate_strike" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "fury_cutter", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "steel_rake", LearnLevel = 9 },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 16 },
				new LearnableMove { MoveId = "steel_wing", LearnLevel = 23 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/bladefly/idle/bladefly_idle_01.png",
				"ui/monsters/bladefly/idle/bladefly_idle_02.png",
				"ui/monsters/bladefly/idle/bladefly_idle_03.png",
				"ui/monsters/bladefly/idle/bladefly_idle_04.png"
			},
			BeastiaryNumber = 93
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "bellguard",
			Name = "Bellguard",
			Description = "A creature that formed inside an old church bell. It rings itself to warn of danger and is confused when people run away from the warning instead of toward it.",
			IconPath = "ui/monsters/bellguard/idle/bellguard_idle_01.png",
			BaseHP = 73, BaseATK = 52, BaseDEF = 84, BaseSpA = 67, BaseSpD = 88, BaseSPD = 28,
			HPGrowth = 6, ATKGrowth = 4, DEFGrowth = 7, SpAGrowth = 5, SpDGrowth = 7, SPDGrowth = 2,
			Element = ElementType.Metal,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "iron_will", "hardened_resolve", "enduring_will" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 1 },
				new LearnableMove { MoveId = "temper", LearnLevel = 10 },
				new LearnableMove { MoveId = "gleaming_ray", LearnLevel = 18 },
				new LearnableMove { MoveId = "heal_bell", LearnLevel = 26 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/bellguard/idle/bellguard_idle_01.png",
				"ui/monsters/bellguard/idle/bellguard_idle_02.png",
				"ui/monsters/bellguard/idle/bellguard_idle_03.png",
				"ui/monsters/bellguard/idle/bellguard_idle_04.png"
			},
			BeastiaryNumber = 94
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "chainlink",
			Name = "Chainlink",
			Description = "A creature made of interlocking metal links. It can stretch itself incredibly thin or ball up into an impenetrable sphere. It's not sure which form is the 'real' one.",
			IconPath = "ui/monsters/chainlink/idle/chainlink_idle_01.png",
			BaseHP = 62, BaseATK = 53, BaseDEF = 68, BaseSpA = 47, BaseSpD = 62, BaseSPD = 56,
			HPGrowth = 5, ATKGrowth = 4, DEFGrowth = 5, SpAGrowth = 4, SpDGrowth = 5, SPDGrowth = 5,
			Element = ElementType.Metal,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.45f,
			PossibleTraits = new() { "iron_will", "phantom_step", "hardened_resolve" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "root_bind", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "root_bind", LearnLevel = 8 },
				new LearnableMove { MoveId = "temper", LearnLevel = 15 },
				new LearnableMove { MoveId = "gyro_ball", LearnLevel = 22 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/chainlink/idle/chainlink_idle_01.png",
				"ui/monsters/chainlink/idle/chainlink_idle_02.png",
				"ui/monsters/chainlink/idle/chainlink_idle_03.png",
				"ui/monsters/chainlink/idle/chainlink_idle_04.png"
			},
			BeastiaryNumber = 95
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "chromedragon",
			Name = "Chromedragon",
			Description = "A dragon whose scales are polished to a perfect mirror shine. It's incredibly vain and spends hours admiring its own reflection.",
			IconPath = "ui/monsters/chromedragon/idle/chromedragon_idle_01.png",
			BaseHP = 86, BaseATK = 92, BaseDEF = 88, BaseSpA = 78, BaseSpD = 82, BaseSPD = 74,
			HPGrowth = 6, ATKGrowth = 7, DEFGrowth = 6, SpAGrowth = 6, SpDGrowth = 6, SPDGrowth = 5,
			Element = ElementType.Metal,
			BaseRarity = Rarity.Rare,
			BaseCatchRate = 0.15f,
			PossibleTraits = new() { "iron_will", "precision_hunter", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "steel_rake", LearnLevel = 1 },
				new LearnableMove { MoveId = "blazing_wrath", LearnLevel = 1 },
				new LearnableMove { MoveId = "gleaming_ray", LearnLevel = 18 },
				new LearnableMove { MoveId = "umbral_claw", LearnLevel = 28 },
				new LearnableMove { MoveId = "meteor_mash", LearnLevel = 38 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/chromedragon/idle/chromedragon_idle_01.png",
				"ui/monsters/chromedragon/idle/chromedragon_idle_02.png",
				"ui/monsters/chromedragon/idle/chromedragon_idle_03.png",
				"ui/monsters/chromedragon/idle/chromedragon_idle_04.png"
			},
			BeastiaryNumber = 96
		} );

		// ═══════════════════════════════════════════════════════════════
		// SPIRIT SANCTUM - SPIRIT (#92-101)
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "dawnmote",
			Name = "Dawnmote",
			Description = "A fragment of the first sunrise that refused to fade. It brings warmth to cold hearts but cannot understand why shadows exist.",
			IconPath = "ui/monsters/dawnmote/idle/dawnmote_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/dawnmote/idle/dawnmote_idle_01.png",
				"ui/monsters/dawnmote/idle/dawnmote_idle_02.png",
				"ui/monsters/dawnmote/idle/dawnmote_idle_03.png",
				"ui/monsters/dawnmote/idle/dawnmote_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 42, BaseATK = 38, BaseDEF = 42, BaseSpA = 58, BaseSpD = 48, BaseSPD = 57,
			HPGrowth = 3, ATKGrowth = 3, DEFGrowth = 3, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 5,
			Element = ElementType.Spirit,
			BaseRarity = Rarity.Common,
			EvolvesTo = "haloveil",
			EvolutionLevel = 22,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "ethereal_blessing", "phantom_step", "vital_recovery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "fairy_wind", LearnLevel = 1 },
				new LearnableMove { MoveId = "aether_pulse", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 8 },
				new LearnableMove { MoveId = "aether_pulse", LearnLevel = 14 },
				new LearnableMove { MoveId = "light_screen", LearnLevel = 18 }
			},
			BeastiaryNumber = 97
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "haloveil",
			Name = "Haloveil",
			Description = "When a Dawnmote gathers enough light, it condenses into a veiled spirit crowned by a golden halo. It drifts silently through the world, leaving trails of warmth in its wake.",
			IconPath = "ui/monsters/haloveil/idle/haloveil_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/haloveil/idle/haloveil_idle_01.png",
				"ui/monsters/haloveil/idle/haloveil_idle_02.png",
				"ui/monsters/haloveil/idle/haloveil_idle_03.png",
				"ui/monsters/haloveil/idle/haloveil_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 72, BaseATK = 55, BaseDEF = 68, BaseSpA = 88, BaseSpD = 78, BaseSPD = 82,
			HPGrowth = 5, ATKGrowth = 4, DEFGrowth = 5, SpAGrowth = 7, SpDGrowth = 6, SPDGrowth = 6,
			Element = ElementType.Spirit,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "dawnmote",
			EvolvesTo = "solmara",
			EvolutionLevel = 40,
			BaseCatchRate = 0.25f,
			PossibleTraits = new() { "ethereal_blessing", "phantom_step", "vital_recovery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "lunar_radiance", LearnLevel = 1, EvolvesFrom = "aether_pulse" },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "light_screen", LearnLevel = 1 },
				new LearnableMove { MoveId = "aether_pulse", LearnLevel = 1 },
				new LearnableMove { MoveId = "calm_mind", LearnLevel = 28 },
				new LearnableMove { MoveId = "divine_grace", LearnLevel = 34 }
			},
			BeastiaryNumber = 98
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "solmara",
			Name = "Solmara",
			Description = "A radiant bird born from gathered dawn-light, crowned by rings of every color sunrise has ever worn. Its prismatic feathers shimmer with hues that have no names.",
			IconPath = "ui/monsters/solmara/idle/solmara_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/solmara/idle/solmara_idle_01.png",
				"ui/monsters/solmara/idle/solmara_idle_02.png",
				"ui/monsters/solmara/idle/solmara_idle_03.png",
				"ui/monsters/solmara/idle/solmara_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 97, BaseATK = 72, BaseDEF = 92, BaseSpA = 118, BaseSpD = 98, BaseSPD = 103,
			HPGrowth = 7, ATKGrowth = 5, DEFGrowth = 7, SpAGrowth = 9, SpDGrowth = 7, SPDGrowth = 7,
			Element = ElementType.Spirit,
			BaseRarity = Rarity.Epic,
			EvolvesFrom = "haloveil",
			BaseCatchRate = 0.1f,
			PossibleTraits = new() { "ethereal_blessing", "phantom_step", "vital_recovery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "prismatic_ray", LearnLevel = 1, EvolvesFrom = "lunar_radiance" },
				new LearnableMove { MoveId = "calm_mind", LearnLevel = 1 },
				new LearnableMove { MoveId = "divine_grace", LearnLevel = 1 },
				new LearnableMove { MoveId = "light_screen", LearnLevel = 1 },
				new LearnableMove { MoveId = "avalanche_wrath", LearnLevel = 48 },
				new LearnableMove { MoveId = "divine_light", LearnLevel = 56 }
			},
			BeastiaryNumber = 99
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "echomind",
			Name = "Echomind",
			Description = "A being formed from thoughts that outlived their thinkers. It knows things that no living person remkindles and asks questions that have no answers.",
			IconPath = "ui/monsters/echomind/idle/echomind_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/echomind/idle/echomind_idle_01.png",
				"ui/monsters/echomind/idle/echomind_idle_02.png",
				"ui/monsters/echomind/idle/echomind_idle_03.png",
				"ui/monsters/echomind/idle/echomind_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 57, BaseATK = 42, BaseDEF = 52, BaseSpA = 78, BaseSpD = 68, BaseSPD = 73,
			HPGrowth = 4, ATKGrowth = 3, DEFGrowth = 4, SpAGrowth = 6, SpDGrowth = 5, SPDGrowth = 6,
			Element = ElementType.Spirit,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.35f,
			PossibleTraits = new() { "ethereal_blessing", "trickster", "menacing_aura" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 1 },
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 1 },
				new LearnableMove { MoveId = "spirit_rend", LearnLevel = 16 },
				new LearnableMove { MoveId = "calm_mind", LearnLevel = 22 },
				new LearnableMove { MoveId = "dream_eater", LearnLevel = 28 }
			},
			BeastiaryNumber = 100
		} );

		// Wishling 2-stage line
		AddSpecies( new MonsterSpecies
		{
			Id = "wishling",
			Name = "Wishling",
			Description = "A small spirit born from wishes thrown into fountains. It wants desperately to grant them but doesn't understand that most wishes are impossible.",
			IconPath = "ui/monsters/wishling/idle/wishling_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/wishling/idle/wishling_idle_01.png",
				"ui/monsters/wishling/idle/wishling_idle_02.png",
				"ui/monsters/wishling/idle/wishling_idle_03.png",
				"ui/monsters/wishling/idle/wishling_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 43, BaseATK = 35, BaseDEF = 42, BaseSpA = 58, BaseSpD = 52, BaseSPD = 62,
			HPGrowth = 3, ATKGrowth = 3, DEFGrowth = 3, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 5,
			Element = ElementType.Spirit,
			BaseRarity = Rarity.Common,
			EvolvesTo = "hopebringer",
			EvolutionLevel = 26,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "ethereal_blessing", "vital_recovery", "trickster" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "fairy_wind", LearnLevel = 1 },
				new LearnableMove { MoveId = "helping_hand", LearnLevel = 1 },
				new LearnableMove { MoveId = "divine_grace", LearnLevel = 10 },
				new LearnableMove { MoveId = "aether_pulse", LearnLevel = 16 },
				new LearnableMove { MoveId = "lucky_chant", LearnLevel = 22 }
			},
			BeastiaryNumber = 101
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "hopebringer",
			Name = "Hopebringer",
			Description = "A majestic spirit that has learned to grant wishes in ways that actually help. It understands now that the best wishes are the ones people must work for.",
			IconPath = "ui/monsters/hopebringer/idle/hopebringer_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/hopebringer/idle/hopebringer_idle_01.png",
				"ui/monsters/hopebringer/idle/hopebringer_idle_02.png",
				"ui/monsters/hopebringer/idle/hopebringer_idle_03.png",
				"ui/monsters/hopebringer/idle/hopebringer_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 78, BaseATK = 52, BaseDEF = 72, BaseSpA = 85, BaseSpD = 82, BaseSPD = 88,
			HPGrowth = 5, ATKGrowth = 4, DEFGrowth = 5, SpAGrowth = 6, SpDGrowth = 6, SPDGrowth = 7,
			Element = ElementType.Spirit,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "wishling",
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "ethereal_blessing", "vital_recovery", "trickster" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "lunar_radiance", LearnLevel = 1, EvolvesFrom = "aether_pulse" },
				new LearnableMove { MoveId = "divine_grace", LearnLevel = 1 },
				new LearnableMove { MoveId = "lucky_chant", LearnLevel = 1 },
				new LearnableMove { MoveId = "helping_hand", LearnLevel = 1 },
				new LearnableMove { MoveId = "divine_grace", LearnLevel = 32 },
				new LearnableMove { MoveId = "spirit_rend", LearnLevel = 38 }
			},
			BeastiaryNumber = 102
		} );

		// Single-stage Spirit creatures
		AddSpecies( new MonsterSpecies
		{
			Id = "memoryveil",
			Name = "Memoryveil",
			Description = "A spirit made of preserved memories. It can show you moments from your past with perfect clarity, though it cannot understand why some memories make people cry.",
			IconPath = "ui/monsters/memoryveil/idle/memoryveil_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/memoryveil/idle/memoryveil_idle_01.png",
				"ui/monsters/memoryveil/idle/memoryveil_idle_02.png",
				"ui/monsters/memoryveil/idle/memoryveil_idle_03.png",
				"ui/monsters/memoryveil/idle/memoryveil_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 52, BaseATK = 38, BaseDEF = 57, BaseSpA = 72, BaseSpD = 68, BaseSPD = 63,
			HPGrowth = 4, ATKGrowth = 3, DEFGrowth = 5, SpAGrowth = 6, SpDGrowth = 5, SPDGrowth = 5,
			Element = ElementType.Spirit,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "ethereal_blessing", "phantom_step", "hardened_resolve" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 1 },
				new LearnableMove { MoveId = "lunar_radiance", LearnLevel = 12 },
				new LearnableMove { MoveId = "dream_eater", LearnLevel = 20 },
				new LearnableMove { MoveId = "spirit_rend", LearnLevel = 28 }
			},
			BeastiaryNumber = 103
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "dreamspark",
			Name = "Dreamspark",
			Description = "A creature that lives in the moment between waking and sleeping. It collects the dreams you forget and sometimes returns them to the wrong people.",
			IconPath = "ui/monsters/dreamspark/idle/dreamspark_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/dreamspark/idle/dreamspark_idle_01.png",
				"ui/monsters/dreamspark/idle/dreamspark_idle_02.png",
				"ui/monsters/dreamspark/idle/dreamspark_idle_03.png",
				"ui/monsters/dreamspark/idle/dreamspark_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 47, BaseATK = 38, BaseDEF = 47, BaseSpA = 72, BaseSpD = 53, BaseSPD = 78,
			HPGrowth = 4, ATKGrowth = 3, DEFGrowth = 4, SpAGrowth = 6, SpDGrowth = 4, SPDGrowth = 6,
			Element = ElementType.Spirit,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.5f,
			PossibleTraits = new() { "ethereal_blessing", "trickster", "phantom_step" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 1 },
				new LearnableMove { MoveId = "dream_eater", LearnLevel = 14 },
				new LearnableMove { MoveId = "aether_pulse", LearnLevel = 20 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 26 }
			},
			BeastiaryNumber = 104
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "soulflare",
			Name = "Soulflare",
			Description = "A brilliant spirit that appears during moments of great inspiration. Artists and inventors often see it in the corner of their vision right before a breakthrough.",
			IconPath = "ui/monsters/soulflare/idle/soulflare_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/soulflare/idle/soulflare_idle_01.png",
				"ui/monsters/soulflare/idle/soulflare_idle_02.png",
				"ui/monsters/soulflare/idle/soulflare_idle_03.png",
				"ui/monsters/soulflare/idle/soulflare_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 58, BaseATK = 48, BaseDEF = 52, BaseSpA = 88, BaseSpD = 62, BaseSPD = 82,
			HPGrowth = 4, ATKGrowth = 4, DEFGrowth = 4, SpAGrowth = 7, SpDGrowth = 5, SPDGrowth = 6,
			Element = ElementType.Spirit,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.35f,
			PossibleTraits = new() { "ethereal_blessing", "fortunate_strike", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "aether_pulse", LearnLevel = 1 },
				new LearnableMove { MoveId = "aether_pulse", LearnLevel = 14 },
				new LearnableMove { MoveId = "calm_mind", LearnLevel = 22 },
				new LearnableMove { MoveId = "spirit_rend", LearnLevel = 30 }
			},
			BeastiaryNumber = 105
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "eternawing",
			Name = "Eternawing",
			Description = "A spirit that has witnessed every meaningful moment in history. It carries the weight of time gracefully and believes every small kindness matters.",
			IconPath = "ui/monsters/eternawing/idle/eternawing_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/eternawing/idle/eternawing_idle_01.png",
				"ui/monsters/eternawing/idle/eternawing_idle_02.png",
				"ui/monsters/eternawing/idle/eternawing_idle_03.png",
				"ui/monsters/eternawing/idle/eternawing_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 83, BaseATK = 62, BaseDEF = 82, BaseSpA = 95, BaseSpD = 88, BaseSPD = 92,
			HPGrowth = 6, ATKGrowth = 4, DEFGrowth = 6, SpAGrowth = 7, SpDGrowth = 6, SPDGrowth = 7,
			Element = ElementType.Spirit,
			BaseRarity = Rarity.Rare,
			BaseCatchRate = 0.15f,
			PossibleTraits = new() { "ethereal_blessing", "elemental_mastery", "enduring_will" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "ancient_power", LearnLevel = 1 },
				new LearnableMove { MoveId = "aether_pulse", LearnLevel = 1 },
				new LearnableMove { MoveId = "lunar_radiance", LearnLevel = 22 },
				new LearnableMove { MoveId = "spirit_rend", LearnLevel = 32 },
				new LearnableMove { MoveId = "future_sight", LearnLevel = 42 }
			},
			BeastiaryNumber = 106
		} );

		// ═══════════════════════════════════════════════════════════════
		// SHADOW DEPTHS - SHADOW (#102-112)
		// ═══════════════════════════════════════════════════════════════

		AddSpecies( new MonsterSpecies
		{
			Id = "murkmaw",
			Name = "Murkmaw",
			Description = "Born from thoughts pushed to the back of minds. It feeds not on fear but on the things people refuse to remkindle.",
			IconPath = "ui/monsters/murkmaw/idle/murkmaw_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/murkmaw/idle/murkmaw_idle_01.png",
				"ui/monsters/murkmaw/idle/murkmaw_idle_02.png",
				"ui/monsters/murkmaw/idle/murkmaw_idle_03.png",
				"ui/monsters/murkmaw/idle/murkmaw_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 47, BaseATK = 62, BaseDEF = 47, BaseSpA = 55, BaseSpD = 42, BaseSPD = 57,
			HPGrowth = 4, ATKGrowth = 5, DEFGrowth = 4, SpAGrowth = 4, SpDGrowth = 3, SPDGrowth = 5,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Common,
			EvolvesTo = "voidweep",
			EvolutionLevel = 22,
			BaseCatchRate = 0.5f,
			PossibleTraits = new() { "dark_presence", "phantom_step", "bloodlust" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 1 },
				new LearnableMove { MoveId = "shade_step", LearnLevel = 8 },
				new LearnableMove { MoveId = "night_shade", LearnLevel = 14 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 20 }
			},
			BeastiaryNumber = 107
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "voidweep",
			Name = "Voidweep",
			Description = "A collector of final moments. It arrives not to cause endings but to witness them, carrying the weight of every 'last time' ever experienced.",
			IconPath = "ui/monsters/voidweep/idle/voidweep_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/voidweep/idle/voidweep_idle_01.png",
				"ui/monsters/voidweep/idle/voidweep_idle_02.png",
				"ui/monsters/voidweep/idle/voidweep_idle_03.png",
				"ui/monsters/voidweep/idle/voidweep_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 72, BaseATK = 93, BaseDEF = 62, BaseSpA = 78, BaseSpD = 58, BaseSPD = 77,
			HPGrowth = 5, ATKGrowth = 7, DEFGrowth = 4, SpAGrowth = 6, SpDGrowth = 4, SPDGrowth = 6,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "murkmaw",
			EvolvesTo = "nullgrave",
			EvolutionLevel = 40,
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "dark_presence", "phantom_step", "bloodlust" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 1, EvolvesFrom = "nightmare_wave" },
				new LearnableMove { MoveId = "shade_step", LearnLevel = 1 },
				new LearnableMove { MoveId = "night_shade", LearnLevel = 1 },
				new LearnableMove { MoveId = "rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "nasty_plot", LearnLevel = 28 },
				new LearnableMove { MoveId = "umbral_claw", LearnLevel = 36 }
			},
			BeastiaryNumber = 108
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "nullgrave",
			Name = "Nullgrave",
			Description = "The shape of what happens when even darkness forgets itself. It is neither evil nor good - it is simply the absence of ever having been.",
			IconPath = "ui/monsters/nullgrave/idle/nullgrave_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/nullgrave/idle/nullgrave_idle_01.png",
				"ui/monsters/nullgrave/idle/nullgrave_idle_02.png",
				"ui/monsters/nullgrave/idle/nullgrave_idle_03.png",
				"ui/monsters/nullgrave/idle/nullgrave_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 92, BaseATK = 118, BaseDEF = 82, BaseSpA = 102, BaseSpD = 75, BaseSPD = 97,
			HPGrowth = 6, ATKGrowth = 9, DEFGrowth = 6, SpAGrowth = 8, SpDGrowth = 5, SPDGrowth = 7,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Epic,
			EvolvesFrom = "voidweep",
			BaseCatchRate = 0.08f,
			PossibleTraits = new() { "dark_presence", "phantom_step", "bloodlust" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "void_pulse", LearnLevel = 1, EvolvesFrom = "void_sphere" },
				new LearnableMove { MoveId = "umbral_claw", LearnLevel = 1 },
				new LearnableMove { MoveId = "nasty_plot", LearnLevel = 1 },
				new LearnableMove { MoveId = "night_shade", LearnLevel = 1 },
				new LearnableMove { MoveId = "destiny_bond", LearnLevel = 48 },
				new LearnableMove { MoveId = "oblivion_wing", LearnLevel = 56 }
			},
			BeastiaryNumber = 109
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "gloomling",
			Name = "Gloomling",
			Description = "A creature that lives in the shadow you cast when no light is present. It's always been there. You just never had reason to look.",
			IconPath = "ui/monsters/gloomling/idle/gloomling_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/gloomling/idle/gloomling_idle_01.png",
				"ui/monsters/gloomling/idle/gloomling_idle_02.png",
				"ui/monsters/gloomling/idle/gloomling_idle_03.png",
				"ui/monsters/gloomling/idle/gloomling_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 52, BaseATK = 57, BaseDEF = 52, BaseSpA = 58, BaseSpD = 48, BaseSPD = 62,
			HPGrowth = 4, ATKGrowth = 5, DEFGrowth = 4, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 5,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.45f,
			PossibleTraits = new() { "dark_presence", "phantom_step", "menacing_aura" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "shade_step", LearnLevel = 1 },
				new LearnableMove { MoveId = "shade_step", LearnLevel = 1 },
				new LearnableMove { MoveId = "night_shade", LearnLevel = 10 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 18 },
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 26 }
			},
			BeastiaryNumber = 110
		} );

		// Nightcrawl 2-stage line
		AddSpecies( new MonsterSpecies
		{
			Id = "nightcrawl",
			Name = "Nightcrawl",
			Description = "A creature that moves only when you're not looking. It's not malicious - it's just incredibly shy and mortified when caught in motion.",
			IconPath = "ui/monsters/nightcrawl/idle/nightcrawl_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/nightcrawl/idle/nightcrawl_idle_01.png",
				"ui/monsters/nightcrawl/idle/nightcrawl_idle_02.png",
				"ui/monsters/nightcrawl/idle/nightcrawl_idle_03.png",
				"ui/monsters/nightcrawl/idle/nightcrawl_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 47, BaseATK = 58, BaseDEF = 42, BaseSpA = 43, BaseSpD = 38, BaseSPD = 68,
			HPGrowth = 4, ATKGrowth = 5, DEFGrowth = 3, SpAGrowth = 4, SpDGrowth = 3, SPDGrowth = 6,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Common,
			EvolvesTo = "duskstalker",
			EvolutionLevel = 24,
			BaseCatchRate = 0.5f,
			PossibleTraits = new() { "dark_presence", "phantom_step", "fortunate_strike" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "intimidate", LearnLevel = 1 },
				new LearnableMove { MoveId = "shade_step", LearnLevel = 8 },
				new LearnableMove { MoveId = "feint_attack", LearnLevel = 14 },
				new LearnableMove { MoveId = "umbral_claw", LearnLevel = 20 }
			},
			BeastiaryNumber = 111
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "duskstalker",
			Name = "Duskstalker",
			Description = "A graceful predator that has embraced its nature. It no longer feels shame about moving unseen - it has learned that some things must happen in darkness.",
			IconPath = "ui/monsters/duskstalker/idle/duskstalker_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/duskstalker/idle/duskstalker_idle_01.png",
				"ui/monsters/duskstalker/idle/duskstalker_idle_02.png",
				"ui/monsters/duskstalker/idle/duskstalker_idle_03.png",
				"ui/monsters/duskstalker/idle/duskstalker_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 72, BaseATK = 88, BaseDEF = 62, BaseSpA = 58, BaseSpD = 55, BaseSPD = 93,
			HPGrowth = 5, ATKGrowth = 7, DEFGrowth = 4, SpAGrowth = 4, SpDGrowth = 4, SPDGrowth = 7,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Uncommon,
			EvolvesFrom = "nightcrawl",
			BaseCatchRate = 0.3f,
			PossibleTraits = new() { "dark_presence", "phantom_step", "fortunate_strike" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "umbral_claw", LearnLevel = 1, EvolvesFrom = "umbral_claw" },
				new LearnableMove { MoveId = "shade_step", LearnLevel = 1 },
				new LearnableMove { MoveId = "feint_attack", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "sucker_punch", LearnLevel = 30 },
				new LearnableMove { MoveId = "shade_step", LearnLevel = 38 }
			},
			BeastiaryNumber = 112
		} );

		// Single-stage Shadow creatures
		AddSpecies( new MonsterSpecies
		{
			Id = "fearling",
			Name = "Fearling",
			Description = "A creature made of childhood fears that were never outgrown. It doesn't want to scare anyone - it just doesn't know how to be anything else.",
			IconPath = "ui/monsters/fearling/idle/fearling_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/fearling/idle/fearling_idle_01.png",
				"ui/monsters/fearling/idle/fearling_idle_02.png",
				"ui/monsters/fearling/idle/fearling_idle_03.png",
				"ui/monsters/fearling/idle/fearling_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 42, BaseATK = 48, BaseDEF = 47, BaseSpA = 58, BaseSpD = 52, BaseSPD = 57,
			HPGrowth = 3, ATKGrowth = 4, DEFGrowth = 4, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 5,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Common,
			BaseCatchRate = 0.55f,
			PossibleTraits = new() { "dark_presence", "menacing_aura", "adrenaline_rush" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "shade_step", LearnLevel = 1 },
				new LearnableMove { MoveId = "scary_face", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 10 },
				new LearnableMove { MoveId = "night_shade", LearnLevel = 16 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 22 }
			},
			BeastiaryNumber = 113
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "umbralynx",
			Name = "Umbralynx",
			Description = "A cat made entirely of shadows. It can slip through any space where light doesn't reach and sometimes gets stuck in places that have no shadows.",
			IconPath = "ui/monsters/umbralynx/idle/umbralynx_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/umbralynx/idle/umbralynx_idle_01.png",
				"ui/monsters/umbralynx/idle/umbralynx_idle_02.png",
				"ui/monsters/umbralynx/idle/umbralynx_idle_03.png",
				"ui/monsters/umbralynx/idle/umbralynx_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 52, BaseATK = 68, BaseDEF = 47, BaseSpA = 53, BaseSpD = 45, BaseSPD = 83,
			HPGrowth = 4, ATKGrowth = 5, DEFGrowth = 4, SpAGrowth = 4, SpDGrowth = 4, SPDGrowth = 7,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "dark_presence", "phantom_step", "precision_hunter" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "shade_step", LearnLevel = 1 },
				new LearnableMove { MoveId = "umbral_claw", LearnLevel = 14 },
				new LearnableMove { MoveId = "umbral_claw", LearnLevel = 22 },
				new LearnableMove { MoveId = "sucker_punch", LearnLevel = 30 }
			},
			BeastiaryNumber = 114
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "secretkeeper",
			Name = "Secretkeeper",
			Description = "A creature that feeds on secrets. It knows everything that's ever been whispered in darkness and guards this knowledge jealously, though it has no use for it.",
			IconPath = "ui/monsters/secretkeeper/idle/secretkeeper_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/secretkeeper/idle/secretkeeper_idle_01.png",
				"ui/monsters/secretkeeper/idle/secretkeeper_idle_02.png",
				"ui/monsters/secretkeeper/idle/secretkeeper_idle_03.png",
				"ui/monsters/secretkeeper/idle/secretkeeper_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 63, BaseATK = 55, BaseDEF = 62, BaseSpA = 68, BaseSpD = 67, BaseSPD = 58,
			HPGrowth = 5, ATKGrowth = 4, DEFGrowth = 5, SpAGrowth = 5, SpDGrowth = 5, SPDGrowth = 5,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.35f,
			PossibleTraits = new() { "dark_presence", "hardened_resolve", "trickster" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 1 },
				new LearnableMove { MoveId = "night_shade", LearnLevel = 12 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 20 },
				new LearnableMove { MoveId = "nasty_plot", LearnLevel = 28 }
			},
			BeastiaryNumber = 115
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "eclipsara",
			Name = "Eclipsara",
			Description = "A creature that only fully exists during eclipses. Between eclipses, it's only partially real, which it finds deeply inconvenient.",
			IconPath = "ui/monsters/eclipsara/idle/eclipsara_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/eclipsara/idle/eclipsara_idle_01.png",
				"ui/monsters/eclipsara/idle/eclipsara_idle_02.png",
				"ui/monsters/eclipsara/idle/eclipsara_idle_03.png",
				"ui/monsters/eclipsara/idle/eclipsara_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 77, BaseATK = 72, BaseDEF = 73, BaseSpA = 87, BaseSpD = 78, BaseSPD = 78,
			HPGrowth = 5, ATKGrowth = 5, DEFGrowth = 5, SpAGrowth = 7, SpDGrowth = 6, SPDGrowth = 6,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Rare,
			BaseCatchRate = 0.2f,
			PossibleTraits = new() { "dark_presence", "phantom_step", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 1 },
				new LearnableMove { MoveId = "divine_grace", LearnLevel = 1 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 18 },
				new LearnableMove { MoveId = "calm_mind", LearnLevel = 28 },
				new LearnableMove { MoveId = "eclipse_ray", LearnLevel = 38 }
			},
			BeastiaryNumber = 116
		} );

		AddSpecies( new MonsterSpecies
		{
			Id = "nightmarex",
			Name = "Nightmarex",
			Description = "The king of bad dreams. It doesn't create nightmares - it rules them, trying to keep them from becoming too frightening because even shadows need balance.",
			IconPath = "ui/monsters/nightmarex/idle/nightmarex_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/nightmarex/idle/nightmarex_idle_01.png",
				"ui/monsters/nightmarex/idle/nightmarex_idle_02.png",
				"ui/monsters/nightmarex/idle/nightmarex_idle_03.png",
				"ui/monsters/nightmarex/idle/nightmarex_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 88, BaseATK = 78, BaseDEF = 77, BaseSpA = 103, BaseSpD = 82, BaseSPD = 87,
			HPGrowth = 6, ATKGrowth = 6, DEFGrowth = 5, SpAGrowth = 8, SpDGrowth = 6, SPDGrowth = 6,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Rare,
			BaseCatchRate = 0.12f,
			PossibleTraits = new() { "dark_presence", "bloodlust", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 1 },
				new LearnableMove { MoveId = "dream_eater", LearnLevel = 18 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 28 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 40 }
			},
			BeastiaryNumber = 117
		} );

		// ═══════════════════════════════════════════════════════════════
		// LATE GAME / EPIC / LEGENDARY (#113-120)
		// ═══════════════════════════════════════════════════════════════

		// Epic Nature - rare spawn in Garden of Origins
		AddSpecies( new MonsterSpecies
		{
			Id = "primbloom",
			Name = "Primbloom",
			Description = "The first flower that ever opened, still blooming, still closing, caught in the eternal moment of becoming. All plants remkindle being part of it.",
			IconPath = "ui/monsters/primbloom/idle/primbloom_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/primbloom/idle/primbloom_idle_01.png",
				"ui/monsters/primbloom/idle/primbloom_idle_02.png",
				"ui/monsters/primbloom/idle/primbloom_idle_03.png",
				"ui/monsters/primbloom/idle/primbloom_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 82, BaseATK = 68, BaseDEF = 83, BaseSpA = 98, BaseSpD = 92, BaseSPD = 72,
			HPGrowth = 6, ATKGrowth = 5, DEFGrowth = 6, SpAGrowth = 7, SpDGrowth = 7, SPDGrowth = 5,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Epic,
			BaseCatchRate = 0.1f,
			PossibleTraits = new() { "verdant_power", "wild_harden", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "solstice_beam", LearnLevel = 1 },
				new LearnableMove { MoveId = "divine_grace", LearnLevel = 1 },
				new LearnableMove { MoveId = "petal_dance", LearnLevel = 20 },
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 32 },
				new LearnableMove { MoveId = "bloom_burst", LearnLevel = 45 }
			},
			BeastiaryNumber = 118
		} );

		// Epic Fire - rare spawn in deep volcanic areas
		AddSpecies( new MonsterSpecies
		{
			Id = "sunforged",
			Name = "Sunforged",
			Description = "A creature made from a fragment of the sun that fell to earth. It radiates warmth that can melt stone and light that casts no shadows.",
			IconPath = "ui/monsters/sunforged/idle/sunforged_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/sunforged/idle/sunforged_idle_01.png",
				"ui/monsters/sunforged/idle/sunforged_idle_02.png",
				"ui/monsters/sunforged/idle/sunforged_idle_03.png",
				"ui/monsters/sunforged/idle/sunforged_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 87, BaseATK = 72, BaseDEF = 77, BaseSpA = 112, BaseSpD = 83, BaseSPD = 88,
			HPGrowth = 6, ATKGrowth = 5, DEFGrowth = 5, SpAGrowth = 9, SpDGrowth = 6, SPDGrowth = 6,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Epic,
			BaseCatchRate = 0.08f,
			PossibleTraits = new() { "kindle_heart", "infernal_rage", "flame_eater" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "blazing_wrath", LearnLevel = 1 },
				new LearnableMove { MoveId = "sunny_day", LearnLevel = 1 },
				new LearnableMove { MoveId = "solstice_beam", LearnLevel = 24 },
				new LearnableMove { MoveId = "heat_wave", LearnLevel = 36 },
				new LearnableMove { MoveId = "solar_flare", LearnLevel = 48 }
			},
			BeastiaryNumber = 119
		} );

		// Epic Ice - rare spawn in deepest frozen areas
		AddSpecies( new MonsterSpecies
		{
			Id = "absolutezero",
			Name = "Absolutezero",
			Description = "The theoretical coldest possible creature. It exists at temperatures where even atoms stop moving. Time itself seems to slow in its presence.",
			IconPath = "ui/monsters/absolutezero/idle/absolutezero_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/absolutezero/idle/absolutezero_idle_01.png",
				"ui/monsters/absolutezero/idle/absolutezero_idle_02.png",
				"ui/monsters/absolutezero/idle/absolutezero_idle_03.png",
				"ui/monsters/absolutezero/idle/absolutezero_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 93, BaseATK = 68, BaseDEF = 103, BaseSpA = 98, BaseSpD = 107, BaseSPD = 58,
			HPGrowth = 7, ATKGrowth = 5, DEFGrowth = 8, SpAGrowth = 7, SpDGrowth = 8, SPDGrowth = 4,
			Element = ElementType.Ice,
			BaseRarity = Rarity.Epic,
			BaseCatchRate = 0.08f,
			PossibleTraits = new() { "frost_core", "thermal_hide", "enduring_will" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "avalanche_wrath", LearnLevel = 1 },
				new LearnableMove { MoveId = "winter_veil", LearnLevel = 1 },
				new LearnableMove { MoveId = "permafrost_ray", LearnLevel = 22 },
				new LearnableMove { MoveId = "avalanche_wrath", LearnLevel = 38 },
				new LearnableMove { MoveId = "absolute_zero", LearnLevel = 50 }
			},
			BeastiaryNumber = 120
		} );

		// Epic Electric - rare spawn during perfect storms
		AddSpecies( new MonsterSpecies
		{
			Id = "stormtyrant",
			Name = "Stormtyrant",
			Description = "The spirit of the most powerful storm that ever raged. It doesn't create storms - storms remkindle what it felt like to be part of this one and try to recreate it.",
			IconPath = "ui/monsters/stormtyrant/idle/stormtyrant_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/stormtyrant/idle/stormtyrant_idle_01.png",
				"ui/monsters/stormtyrant/idle/stormtyrant_idle_02.png",
				"ui/monsters/stormtyrant/idle/stormtyrant_idle_03.png",
				"ui/monsters/stormtyrant/idle/stormtyrant_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 82, BaseATK = 78, BaseDEF = 72, BaseSpA = 115, BaseSpD = 75, BaseSPD = 113,
			HPGrowth = 5, ATKGrowth = 6, DEFGrowth = 5, SpAGrowth = 9, SpDGrowth = 5, SPDGrowth = 9,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Epic,
			BaseCatchRate = 0.08f,
			PossibleTraits = new() { "static_charge", "momentum", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "storm_strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "monsoon_call", LearnLevel = 1 },
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 22 },
				new LearnableMove { MoveId = "tempest", LearnLevel = 36 },
				new LearnableMove { MoveId = "storm_surge", LearnLevel = 48 }
			},
			BeastiaryNumber = 121
		} );

		// Legendary - The Mythweaver
		AddSpecies( new MonsterSpecies
		{
			Id = "mythweaver",
			Name = "Mythweaver",
			Description = "The creature that exists because stories needed to be told. It is not the first beast, but the reason beasts exist in stories at all. All creatures are echoes of its dreaming.",
			IconPath = "ui/monsters/mythweaver/idle/mythweaver_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/mythweaver/idle/mythweaver_idle_01.png",
				"ui/monsters/mythweaver/idle/mythweaver_idle_02.png",
				"ui/monsters/mythweaver/idle/mythweaver_idle_03.png",
				"ui/monsters/mythweaver/idle/mythweaver_idle_04.png"
			},
			BaseHP = 122, BaseATK = 108, BaseDEF = 118, BaseSpA = 128, BaseSpD = 122, BaseSPD = 118,
			HPGrowth = 8, ATKGrowth = 7, DEFGrowth = 8, SpAGrowth = 9, SpDGrowth = 8, SPDGrowth = 8,
			Element = ElementType.Neutral,
			BaseRarity = Rarity.Legendary,
			IsCatchable = true,
			BaseCatchRate = 0.01f,
			PossibleTraits = new() { "elemental_mastery", "enduring_will", "titanic_might" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "ancient_power", LearnLevel = 1 },
				new LearnableMove { MoveId = "cosmic_power", LearnLevel = 1 },
				new LearnableMove { MoveId = "annihilate", LearnLevel = 30 },
				new LearnableMove { MoveId = "annihilate", LearnLevel = 50 },
				new LearnableMove { MoveId = "genesis_wave", LearnLevel = 70 }
			},
			BeastiaryNumber = 122
		} );

		// Legendary - The World Serpent
		AddSpecies( new MonsterSpecies
		{
			Id = "worldserpent",
			Name = "Worldserpent",
			Description = "A creature so vast that its body encircles the world. What we see is only the smallest part of it - the deep_slumber exists in dimensions we cannot perceive.",
			IconPath = "ui/monsters/worldserpent/idle/worldserpent_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/worldserpent/idle/worldserpent_idle_01.png",
				"ui/monsters/worldserpent/idle/worldserpent_idle_02.png",
				"ui/monsters/worldserpent/idle/worldserpent_idle_03.png",
				"ui/monsters/worldserpent/idle/worldserpent_idle_04.png"
			},
			BaseHP = 133, BaseATK = 118, BaseDEF = 132, BaseSpA = 88, BaseSpD = 115, BaseSPD = 98,
			HPGrowth = 9, ATKGrowth = 8, DEFGrowth = 9, SpAGrowth = 6, SpDGrowth = 8, SPDGrowth = 7,
			Element = ElementType.Earth,
			BaseRarity = Rarity.Legendary,
			IsCatchable = true,
			BaseCatchRate = 0.01f,
			PossibleTraits = new() { "terra_force", "enduring_will", "titanic_might" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "jagged_spike", LearnLevel = 1 },
				new LearnableMove { MoveId = "temper", LearnLevel = 1 },
				new LearnableMove { MoveId = "jagged_spike", LearnLevel = 28 },
				new LearnableMove { MoveId = "tidal_slam", LearnLevel = 42 },
				new LearnableMove { MoveId = "world_crush", LearnLevel = 60 }
			},
			BeastiaryNumber = 123
		} );

		// Legendary - The Void Dragon
		AddSpecies( new MonsterSpecies
		{
			Id = "voiddragon",
			Name = "Voiddragon",
			Description = "A dragon that exists in the space between stars. It eats light and exhales darkness. Where it flies, the universe forgets that stars were ever there.",
			IconPath = "ui/monsters/voiddragon/idle/voiddragon_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/voiddragon/idle/voiddragon_idle_01.png",
				"ui/monsters/voiddragon/idle/voiddragon_idle_02.png",
				"ui/monsters/voiddragon/idle/voiddragon_idle_03.png",
				"ui/monsters/voiddragon/idle/voiddragon_idle_04.png"
			},
			BaseHP = 117, BaseATK = 128, BaseDEF = 108, BaseSpA = 133, BaseSpD = 105, BaseSPD = 117,
			HPGrowth = 8, ATKGrowth = 9, DEFGrowth = 7, SpAGrowth = 9, SpDGrowth = 7, SPDGrowth = 8,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Legendary,
			IsCatchable = true,
			BaseCatchRate = 0.01f,
			PossibleTraits = new() { "dark_presence", "bloodlust", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "void_pulse", LearnLevel = 1 },
				new LearnableMove { MoveId = "umbral_claw", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 32 },
				new LearnableMove { MoveId = "oblivion_wing", LearnLevel = 48 },
				new LearnableMove { MoveId = "cosmic_erasure", LearnLevel = 65 }
			},
			BeastiaryNumber = 124
		} );

		// Mythic - radeep_slumber creature
		AddSpecies( new MonsterSpecies
		{
			Id = "genesis",
			Name = "Genesis",
			Description = "A primordial egg of obsidian, cracked with veins of golden light. Within, something vast stirs - wings of pure creation folding and unfolding. It is the moment before birth, frozen in time. All that ever was or will be sleeps inside.",
			IconPath = "ui/monsters/genesis/idle/genesis_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/genesis/idle/genesis_idle_01.png",
				"ui/monsters/genesis/idle/genesis_idle_02.png",
				"ui/monsters/genesis/idle/genesis_idle_03.png",
				"ui/monsters/genesis/idle/genesis_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BaseHP = 152, BaseATK = 138, BaseDEF = 142, BaseSpA = 145, BaseSpD = 138, BaseSPD = 138,
			HPGrowth = 10, ATKGrowth = 10, DEFGrowth = 10, SpAGrowth = 10, SpDGrowth = 10, SPDGrowth = 10,
			Element = ElementType.Neutral,
			BaseRarity = Rarity.Mythic,
			IsCatchable = true,
			BaseCatchRate = 0.001f,
			PossibleTraits = new() { "elemental_mastery", "enduring_will", "titanic_might" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "cosmic_power", LearnLevel = 1 },
				new LearnableMove { MoveId = "ancient_power", LearnLevel = 1 },
				new LearnableMove { MoveId = "annihilate", LearnLevel = 40 },
				new LearnableMove { MoveId = "genesis_wave", LearnLevel = 60 },
				new LearnableMove { MoveId = "creation_burst", LearnLevel = 80 }
			},
			BeastiaryNumber = 139
		} );

		// ═══════════════════════════════════════════════════════════════
		// GARDEN OF ORIGINS EXCLUSIVES (#121-128)
		// Primordial creatures found only in the Garden of Origins
		// ═══════════════════════════════════════════════════════════════

		// Legendary - The first consciousness
		AddSpecies( new MonsterSpecies
		{
			Id = "genisoul",
			Name = "Genisoul",
			Description = "The very first consciousness to emerge from the void. It remembers a time before time, when existence was merely a suggestion waiting to be spoken.",
			IconPath = "ui/monsters/genisoul/idle/genisoul_idle_01.png",
			BaseHP = 128, BaseATK = 85, BaseDEF = 112, BaseSpA = 125, BaseSpD = 118, BaseSPD = 118,
			HPGrowth = 8, ATKGrowth = 6, DEFGrowth = 7, SpAGrowth = 8, SpDGrowth = 8, SPDGrowth = 8,
			Element = ElementType.Spirit,
			BaseRarity = Rarity.Mythic,
			IsCatchable = true,
			BaseCatchRate = 0.01f,
			PossibleTraits = new() { "ethereal_blessing", "enduring_will", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "spirit_rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "cosmic_power", LearnLevel = 1 },
				new LearnableMove { MoveId = "lunar_radiance", LearnLevel = 28 },
				new LearnableMove { MoveId = "future_sight", LearnLevel = 42 },
				new LearnableMove { MoveId = "genesis_thought", LearnLevel = 60 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/genisoul/idle/genisoul_idle_01.png",
				"ui/monsters/genisoul/idle/genisoul_idle_02.png",
				"ui/monsters/genisoul/idle/genisoul_idle_03.png",
				"ui/monsters/genisoul/idle/genisoul_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BeastiaryNumber = 125
		} );

		// Epic Fire - Inspired by Hinokagutsuchi, the fire god whose birth destroyed its creator
		AddSpecies( new MonsterSpecies
		{
			Id = "primeflare",
			Name = "Primeflare",
			Description = "A flame born from the death of something greater. The ground cracks and crumbles beneath it, unable to contain a fire that was never meant to exist.",
			IconPath = "ui/monsters/primeflare/idle/primeflare_idle_01.png",
			BaseHP = 87, BaseATK = 78, BaseDEF = 72, BaseSpA = 118, BaseSpD = 77, BaseSPD = 103,
			HPGrowth = 6, ATKGrowth = 5, DEFGrowth = 5, SpAGrowth = 9, SpDGrowth = 5, SPDGrowth = 7,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Epic,
			BaseCatchRate = 0.05f,
			PossibleTraits = new() { "kindle_heart", "infernal_rage", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "blazing_wrath", LearnLevel = 1 },
				new LearnableMove { MoveId = "cinders_curse", LearnLevel = 1 },
				new LearnableMove { MoveId = "solstice_beam", LearnLevel = 26 },
				new LearnableMove { MoveId = "heat_wave", LearnLevel = 38 },
				new LearnableMove { MoveId = "primordial_flame", LearnLevel = 50 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/primeflare/idle/primeflare_idle_01.png",
				"ui/monsters/primeflare/idle/primeflare_idle_02.png",
				"ui/monsters/primeflare/idle/primeflare_idle_03.png",
				"ui/monsters/primeflare/idle/primeflare_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BeastiaryNumber = 126
		} );

		// Epic Shadow - Inspired by Yomotsu Hirasaka (boundary between life and death) and Hel (half-living, half-dead)
		AddSpecies( new MonsterSpecies
		{
			Id = "voidbloom",
			Name = "Voidbloom",
			Description = "A flower that took root on the threshold between life and death. One half blooms in dark violet; the other crumbles into void. It has never fully belonged to either world.",
			IconPath = "ui/monsters/voidlboom/idle/voidbloom_idle_01.png",
			BaseHP = 92, BaseATK = 68, BaseDEF = 87, BaseSpA = 105, BaseSpD = 93, BaseSPD = 97,
			HPGrowth = 6, ATKGrowth = 5, DEFGrowth = 6, SpAGrowth = 8, SpDGrowth = 7, SPDGrowth = 7,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Epic,
			BaseCatchRate = 0.05f,
			PossibleTraits = new() { "dark_presence", "phantom_step", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 1 },
				new LearnableMove { MoveId = "petal_dance", LearnLevel = 1 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 24 },
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 36 },
				new LearnableMove { MoveId = "void_bloom", LearnLevel = 48 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/voidlboom/idle/voidbloom_idle_01.png",
				"ui/monsters/voidlboom/idle/voidbloom_idle_02.png",
				"ui/monsters/voidlboom/idle/voidbloom_idle_03.png",
				"ui/monsters/voidlboom/idle/voidbloom_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BeastiaryNumber = 127
		} );

		// Rare Nature - The first seed
		AddSpecies( new MonsterSpecies
		{
			Id = "edenseed",
			Name = "Edenseed",
			Description = "The seed from which the Garden of Origins itself grew. It carries within it the blueprint for all life that ever was or will be.",
			IconPath = "ui/monsters/edenseed/idle/edenseed_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/edenseed/idle/edenseed_idle_01.png",
				"ui/monsters/edenseed/idle/edenseed_idle_02.png",
				"ui/monsters/edenseed/idle/edenseed_idle_03.png",
				"ui/monsters/edenseed/idle/edenseed_idle_04.png"
			},
			BaseHP = 82, BaseATK = 52, BaseDEF = 92, BaseSpA = 78, BaseSpD = 95, BaseSPD = 58,
			HPGrowth = 6, ATKGrowth = 4, DEFGrowth = 7, SpAGrowth = 5, SpDGrowth = 7, SPDGrowth = 4,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Rare,
			BaseCatchRate = 0.1f,
			PossibleTraits = new() { "verdant_power", "vital_recovery", "enduring_will" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "vitality_burst", LearnLevel = 16 },
				new LearnableMove { MoveId = "divine_grace", LearnLevel = 26 },
				new LearnableMove { MoveId = "solstice_beam", LearnLevel = 38 }
			},
			BeastiaryNumber = 128
		} );

		// Rare Water - The first droplet
		AddSpecies( new MonsterSpecies
		{
			Id = "aquagenesis",
			Name = "Aquagenesis",
			Description = "The primordial droplet from which all waters flowed. Oceans, rivers, rain, and tears - all remember being part of this single drop.",
			IconPath = "ui/monsters/aquagenesis/idle/aquagenesis_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/aquagenesis/idle/aquagenesis_idle_01.png",
				"ui/monsters/aquagenesis/idle/aquagenesis_idle_02.png",
				"ui/monsters/aquagenesis/idle/aquagenesis_idle_03.png",
				"ui/monsters/aquagenesis/idle/aquagenesis_idle_04.png"
			},
			BaseHP = 87, BaseATK = 58, BaseDEF = 82, BaseSpA = 85, BaseSpD = 88, BaseSPD = 77,
			HPGrowth = 6, ATKGrowth = 4, DEFGrowth = 6, SpAGrowth = 6, SpDGrowth = 6, SPDGrowth = 5,
			Element = ElementType.Water,
			BaseRarity = Rarity.Rare,
			BaseCatchRate = 0.1f,
			PossibleTraits = new() { "torrent_soul", "tidal_wrath", "aqua_siphon" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "splash_jet", LearnLevel = 1 },
				new LearnableMove { MoveId = "aqua_ring", LearnLevel = 1 },
				new LearnableMove { MoveId = "water_pulse", LearnLevel = 16 },
				new LearnableMove { MoveId = "tidal_slam", LearnLevel = 28 },
				new LearnableMove { MoveId = "deluge", LearnLevel = 40 }
			},
			BeastiaryNumber = 129
		} );

		// Uncommon Electric - Inspired by Raijū (Japanese thunder beast) + Weasel/Stoat — Stage 1
		AddSpecies( new MonsterSpecies
		{
			Id = "prismite",
			Name = "Prismite",
			Description = "A small weasel-like creature inspired by the Raijū of Japanese legend. Its sleek dark teal fur is marked with faint glowing yellow stripes, and a spark crackles at the tip of its long tail when startled.",
			IconPath = "ui/monsters/prismite/idle/prismite_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/prismite/idle/prismite_idle_01.png",
				"ui/monsters/prismite/idle/prismite_idle_02.png",
				"ui/monsters/prismite/idle/prismite_idle_03.png",
				"ui/monsters/prismite/idle/prismite_idle_04.png"
			},
			BaseHP = 52, BaseATK = 45, BaseDEF = 42, BaseSpA = 72, BaseSpD = 48, BaseSPD = 82,
			HPGrowth = 4, ATKGrowth = 3, DEFGrowth = 3, SpAGrowth = 6, SpDGrowth = 4, SPDGrowth = 6,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.25f,
			EvolvesTo = "arcferron",
			PossibleTraits = new() { "static_charge", "momentum", "adrenaline_rush" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "static_jolt", LearnLevel = 1 },
				new LearnableMove { MoveId = "aether_pulse", LearnLevel = 1 },
				new LearnableMove { MoveId = "volt_charge", LearnLevel = 10 },
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 22 },
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 32 }
			},
			BeastiaryNumber = 130
		} );

		// Uncommon Earth - A primordial bear cub
		AddSpecies( new MonsterSpecies
		{
			Id = "terracub",
			Name = "Terracub",
			Description = "A chubby bear cub made of living stone and packed earth. It loves to dig and tumble, leaving small craters wherever it plays.",
			IconPath = "ui/monsters/terracub/idle/terracub_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/terracub/idle/terracub_idle_01.png",
				"ui/monsters/terracub/idle/terracub_idle_02.png",
				"ui/monsters/terracub/idle/terracub_idle_03.png",
				"ui/monsters/terracub/idle/terracub_idle_04.png"
			},
			BaseHP = 67, BaseATK = 58, BaseDEF = 78, BaseSpA = 42, BaseSpD = 68, BaseSPD = 38,
			HPGrowth = 5, ATKGrowth = 4, DEFGrowth = 6, SpAGrowth = 3, SpDGrowth = 5, SPDGrowth = 3,
			Element = ElementType.Earth,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.25f,
			PossibleTraits = new() { "terra_force", "hardened_resolve", "enduring_will" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "boulder_toss", LearnLevel = 1 },
				new LearnableMove { MoveId = "harden", LearnLevel = 1 },
				new LearnableMove { MoveId = "seismic_crash", LearnLevel = 14 },
				new LearnableMove { MoveId = "jagged_spike", LearnLevel = 26 },
				new LearnableMove { MoveId = "jagged_spike", LearnLevel = 36 }
			},
			BeastiaryNumber = 61
		} );

		// Uncommon Wind - The first breath
		AddSpecies( new MonsterSpecies
		{
			Id = "dandepuff",
			Name = "Dandepuff",
			Description = "A tiny creature shaped like a dandelion puff, born from the first breath ever exhaled. Its fluffy seeds drift away and become new breezes.",
			IconPath = "ui/monsters/dandepuff/idle/dandepuff_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/dandepuff/idle/dandepuff_idle_01.png",
				"ui/monsters/dandepuff/idle/dandepuff_idle_02.png",
				"ui/monsters/dandepuff/idle/dandepuff_idle_03.png",
				"ui/monsters/dandepuff/idle/dandepuff_idle_04.png"
			},
			BaseHP = 57, BaseATK = 42, BaseDEF = 47, BaseSpA = 68, BaseSpD = 55, BaseSPD = 87,
			HPGrowth = 4, ATKGrowth = 3, DEFGrowth = 4, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 7,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.25f,
			PossibleTraits = new() { "gale_spirit", "phantom_step", "skyborne" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "breeze_cut", LearnLevel = 1 },
				new LearnableMove { MoveId = "updraft", LearnLevel = 1 },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 14 },
				new LearnableMove { MoveId = "tempest", LearnLevel = 26 },
				new LearnableMove { MoveId = "tempest", LearnLevel = 38 }
			},
			BeastiaryNumber = 39
		} );

		// ═══════════════════════════════════════════════════════════════
		// PRIMORDIAL RIFT EXCLUSIVES (#130-134)
		// Reality-warping creatures found only in the Primordial Rift
		// Raijura line (#128-130) evolves through this region
		// ═══════════════════════════════════════════════════════════════

		// Epic Electric - Inspired by Raijū + Weasel/Stoat — Stage 3 (Final)
		AddSpecies( new MonsterSpecies
		{
			Id = "raijura",
			Name = "Raijura",
			Description = "The full Raijū realized — a powerful serpentine thunder weasel wreathed in crackling lightning. Its dark teal body is covered in blazing yellow storm markings, and its long whip-like tail trails bolts of electricity. It moves like living lightning, striking before its thunder arrives.",
			IconPath = "ui/monsters/raijura/idle/raijura_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/raijura/idle/raijura_idle_01.png",
				"ui/monsters/raijura/idle/raijura_idle_02.png",
				"ui/monsters/raijura/idle/raijura_idle_03.png",
				"ui/monsters/raijura/idle/raijura_idle_04.png"
			},
			BaseHP = 82, BaseATK = 73, BaseDEF = 67, BaseSpA = 108, BaseSpD = 72, BaseSPD = 113,
			HPGrowth = 5, ATKGrowth = 5, DEFGrowth = 4, SpAGrowth = 8, SpDGrowth = 5, SPDGrowth = 8,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Epic,
			BaseCatchRate = 0.06f,
			EvolvesFrom = "arcferron",
			PossibleTraits = new() { "static_charge", "momentum", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 1 },
				new LearnableMove { MoveId = "brace", LearnLevel = 1 },
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 24 },
				new LearnableMove { MoveId = "storm_strike", LearnLevel = 38 },
				new LearnableMove { MoveId = "dimensional_rift", LearnLevel = 50 }
			},
			BeastiaryNumber = 132
		} );

		// Rare Electric - Inspired by Raijū + Weasel/Stoat — Stage 2
		AddSpecies( new MonsterSpecies
		{
			Id = "arcferron",
			Name = "Arcferron",
			Description = "As Prismite grows, its sleek body elongates and its teal fur darkens. Bright yellow lightning patterns streak across its back and arcs of electricity snap between its pointed ears. Faster than the eye can follow, it leaves afterimages of crackling light.",
			IconPath = "ui/monsters/arcferron/idle/arcferron_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/arcferron/idle/arcferron_idle_01.png",
				"ui/monsters/arcferron/idle/arcferron_idle_02.png",
				"ui/monsters/arcferron/idle/arcferron_idle_03.png",
				"ui/monsters/arcferron/idle/arcferron_idle_04.png"
			},
			BaseHP = 72, BaseATK = 58, BaseDEF = 62, BaseSpA = 88, BaseSpD = 68, BaseSPD = 93,
			HPGrowth = 5, ATKGrowth = 4, DEFGrowth = 4, SpAGrowth = 7, SpDGrowth = 5, SPDGrowth = 7,
			Element = ElementType.Electric,
			BaseRarity = Rarity.Rare,
			BaseCatchRate = 0.12f,
			EvolvesFrom = "prismite",
			EvolvesTo = "raijura",
			PossibleTraits = new() { "static_charge", "phantom_step", "trickster" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "volt_charge", LearnLevel = 1 },
				new LearnableMove { MoveId = "phantom_double", LearnLevel = 1 },
				new LearnableMove { MoveId = "arc_bolt", LearnLevel = 18 },
				new LearnableMove { MoveId = "spirit_rend", LearnLevel = 28 },
				new LearnableMove { MoveId = "quantum_flux", LearnLevel = 40 }
			},
			BeastiaryNumber = 131
		} );

		// Rare Shadow - Inspired by Ammit (Egyptian devourer of unworthy souls) + Crocodile
		AddSpecies( new MonsterSpecies
		{
			Id = "devorah",
			Name = "Devorah",
			Description = "A squat, heavily-armored reptile inspired by Ammit, the Egyptian devourer of unworthy souls. Its broad crocodilian jaw unhinges impossibly wide, revealing a faint golden glow deep within its maw — the last light of every heart it has judged. Dark bronze scales shift to shadow at the edges, and its eyes burn with ancient authority.",
			IconPath = "ui/monsters/devorah/idle/devorah_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/devorah/idle/devorah_idle_01.png",
				"ui/monsters/devorah/idle/devorah_idle_02.png",
				"ui/monsters/devorah/idle/devorah_idle_03.png",
				"ui/monsters/devorah/idle/devorah_idle_04.png"
			},
			BaseHP = 77, BaseATK = 68, BaseDEF = 72, BaseSpA = 92, BaseSpD = 82, BaseSPD = 77,
			HPGrowth = 5, ATKGrowth = 5, DEFGrowth = 5, SpAGrowth = 7, SpDGrowth = 6, SPDGrowth = 5,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Rare,
			BaseCatchRate = 0.12f,
			PossibleTraits = new() { "dark_presence", "hardened_resolve", "bloodlust" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 1 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 1 },
				new LearnableMove { MoveId = "nightmare_wave", LearnLevel = 18 },
				new LearnableMove { MoveId = "nasty_plot", LearnLevel = 28 },
				new LearnableMove { MoveId = "void_tear", LearnLevel = 40 }
			},
			BeastiaryNumber = 133
		} );

		// Uncommon Wind - Inspired by Púca (Celtic shapeshifter) + Hare
		AddSpecies( new MonsterSpecies
		{
			Id = "pucling",
			Name = "Pucling",
			Description = "A swift hare-like creature inspired by the Púca of Celtic legend. Its pale fur flickers between solid and translucent as it darts through the wind, leaving shimmering afterimages in its wake. Its long ears twist toward sounds that haven't happened yet.",
			IconPath = "ui/monsters/pucling/idle/pucling_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/pucling/idle/pucling_idle_01.png",
				"ui/monsters/pucling/idle/pucling_idle_02.png",
				"ui/monsters/pucling/idle/pucling_idle_03.png",
				"ui/monsters/pucling/idle/pucling_idle_04.png"
			},
			BaseHP = 52, BaseATK = 48, BaseDEF = 42, BaseSpA = 63, BaseSpD = 52, BaseSPD = 97,
			HPGrowth = 4, ATKGrowth = 4, DEFGrowth = 3, SpAGrowth = 5, SpDGrowth = 4, SPDGrowth = 7,
			Element = ElementType.Wind,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.2f,
			PossibleTraits = new() { "gale_spirit", "phantom_step", "skyborne" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "breeze_cut", LearnLevel = 1 },
				new LearnableMove { MoveId = "phantom_double", LearnLevel = 1 },
				new LearnableMove { MoveId = "dive_strike", LearnLevel = 12 },
				new LearnableMove { MoveId = "shade_step", LearnLevel = 20 },
				new LearnableMove { MoveId = "phase_shift", LearnLevel = 28 }
			},
			BeastiaryNumber = 134
		} );

		// Uncommon Fire - Inspired by Cherufe (Mapuche volcanic spirit) + Crab
		AddSpecies( new MonsterSpecies
		{
			Id = "scaldnip",
			Name = "Scaldnip",
			Description = "A small crab-like creature inspired by the Cherufe of Mapuche legend. Its rocky shell glows with veins of molten magma, and its claws snap with bursts of sparks. It skitters along volcanic vents where dimensions grind together, feeding on the heat of colliding realities.",
			IconPath = "ui/monsters/scaldnip/idle/scaldnip_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/scaldnip/idle/scaldnip_idle_01.png",
				"ui/monsters/scaldnip/idle/scaldnip_idle_02.png",
				"ui/monsters/scaldnip/idle/scaldnip_idle_03.png",
				"ui/monsters/scaldnip/idle/scaldnip_idle_04.png"
			},
			BaseHP = 57, BaseATK = 52, BaseDEF = 47, BaseSpA = 75, BaseSpD = 53, BaseSPD = 82,
			HPGrowth = 4, ATKGrowth = 4, DEFGrowth = 3, SpAGrowth = 6, SpDGrowth = 4, SPDGrowth = 6,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.2f,
			PossibleTraits = new() { "kindle_heart", "momentum", "reckless_charge" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "kindle", LearnLevel = 1 },
				new LearnableMove { MoveId = "aether_pulse", LearnLevel = 1 },
				new LearnableMove { MoveId = "searing_rush", LearnLevel = 12 },
				new LearnableMove { MoveId = "fire_spin", LearnLevel = 20 },
				new LearnableMove { MoveId = "rift_flame", LearnLevel = 30 }
			},
			BeastiaryNumber = 135
		} );

		// ═══════════════════════════════════════════════════════════════
		// ORIGIN VOID EXCLUSIVES (#134-136)
		// Creatures from before existence - found only in The Origin Void
		// ═══════════════════════════════════════════════════════════════

		// Legendary Neutral - The first matter
		AddSpecies( new MonsterSpecies
		{
			Id = "primordius",
			Name = "Primordius",
			Description = "The first matter that ever existed, before it chose what to become. It holds within it the potential to be anything - stone, star, or soul.",
			IconPath = "ui/monsters/primordius/idle/primordius_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/primordius/idle/primordius_idle_01.png",
				"ui/monsters/primordius/idle/primordius_idle_02.png",
				"ui/monsters/primordius/idle/primordius_idle_03.png",
				"ui/monsters/primordius/idle/primordius_idle_04.png"
			},
			BaseHP = 132, BaseATK = 108, BaseDEF = 128, BaseSpA = 103, BaseSpD = 118, BaseSPD = 98,
			HPGrowth = 8, ATKGrowth = 7, DEFGrowth = 9, SpAGrowth = 7, SpDGrowth = 8, SPDGrowth = 6,
			Element = ElementType.Neutral,
			BaseRarity = Rarity.Legendary,
			IsCatchable = true,
			BaseCatchRate = 0.02f,
			PossibleTraits = new() { "enduring_will", "titanic_might", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "ancient_power", LearnLevel = 1 },
				new LearnableMove { MoveId = "cosmic_power", LearnLevel = 1 },
				new LearnableMove { MoveId = "annihilate", LearnLevel = 32 },
				new LearnableMove { MoveId = "annihilate", LearnLevel = 48 },
				new LearnableMove { MoveId = "primordial_surge", LearnLevel = 65 }
			},
			BeastiaryNumber = 136
		} );

		// Epic Shadow - Inspired by Erebus, the primordial darkness before light
		AddSpecies( new MonsterSpecies
		{
			Id = "nihilex",
			Name = "Nihilex",
			Description = "A formless shadow coiled around a dying star. Inspired by Erebus, the primordial darkness that existed before light, its body is a swirling vortex of deep violet and black with a single pale eye at its center that sees into the space between moments.",
			IconPath = "ui/monsters/nihilex/idle/nihilex_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/nihilex/idle/nihilex_idle_01.png",
				"ui/monsters/nihilex/idle/nihilex_idle_02.png",
				"ui/monsters/nihilex/idle/nihilex_idle_03.png",
				"ui/monsters/nihilex/idle/nihilex_idle_04.png"
			},
			BaseHP = 92, BaseATK = 73, BaseDEF = 82, BaseSpA = 107, BaseSpD = 88, BaseSPD = 97,
			HPGrowth = 6, ATKGrowth = 5, DEFGrowth = 6, SpAGrowth = 8, SpDGrowth = 6, SPDGrowth = 7,
			Element = ElementType.Shadow,
			BaseRarity = Rarity.Epic,
			BaseCatchRate = 0.05f,
			PossibleTraits = new() { "dark_presence", "phantom_step", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "void_sphere", LearnLevel = 1 },
				new LearnableMove { MoveId = "nasty_plot", LearnLevel = 1 },
				new LearnableMove { MoveId = "terror_visions", LearnLevel = 26 },
				new LearnableMove { MoveId = "void_pulse", LearnLevel = 38 },
				new LearnableMove { MoveId = "absolute_void", LearnLevel = 50 }
			},
			BeastiaryNumber = 137
		} );

		// Mythic Spirit - Inspired by Aboriginal Dreamtime ancestors who sang reality into existence (CATCHABLE ultra-rare)
		AddSpecies( new MonsterSpecies
		{
			Id = "songborne",
			Name = "Songborne",
			Description = "The first voice that ever spoke, still singing. Reality ripples outward from its song — every sound ever heard is an echo of its endless melody.",
			IconPath = "ui/monsters/songborne/idle/songborne_idle_01.png",
			BaseHP = 102, BaseATK = 78, BaseDEF = 98, BaseSpA = 103, BaseSpD = 102, BaseSPD = 92,
			HPGrowth = 7, ATKGrowth = 5, DEFGrowth = 7, SpAGrowth = 7, SpDGrowth = 7, SPDGrowth = 6,
			Element = ElementType.Spirit,
			BaseRarity = Rarity.Mythic,
			BaseCatchRate = 0.01f,
			PossibleTraits = new() { "ethereal_blessing", "enduring_will", "elemental_mastery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "cosmic_power", LearnLevel = 1 },
				new LearnableMove { MoveId = "spirit_rend", LearnLevel = 1 },
				new LearnableMove { MoveId = "lunar_radiance", LearnLevel = 24 },
				new LearnableMove { MoveId = "future_sight", LearnLevel = 36 },
				new LearnableMove { MoveId = "universe_burst", LearnLevel = 50 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/songborne/idle/songborne_idle_01.png",
				"ui/monsters/songborne/idle/songborne_idle_02.png",
				"ui/monsters/songborne/idle/songborne_idle_03.png",
				"ui/monsters/songborne/idle/songborne_idle_04.png"
			},
			AnimationFrameRate = 8f,
			BeastiaryNumber = 138
		} );

		// Fungrowth - Nature mushroom creature (Overgrown Heart)
		AddSpecies( new MonsterSpecies
		{
			Id = "fungrowth",
			Name = "Fungrowth",
			Description = "A towering mushroom that walks on root-like legs. Spores drift from its cap like snow, and wherever they land, new fungi spring up overnight.",
			IconPath = "ui/monsters/fungrowth/idle/fungrowth_idle_01.png",
			AnimationFrames = new()
			{
				"ui/monsters/fungrowth/idle/fungrowth_idle_01.png",
				"ui/monsters/fungrowth/idle/fungrowth_idle_02.png",
				"ui/monsters/fungrowth/idle/fungrowth_idle_03.png",
				"ui/monsters/fungrowth/idle/fungrowth_idle_04.png"
			},
			BaseHP = 70, BaseATK = 50, BaseDEF = 65, BaseSpA = 75, BaseSpD = 70, BaseSPD = 40,
			HPGrowth = 5, ATKGrowth = 4, DEFGrowth = 5, SpAGrowth = 6, SpDGrowth = 5, SPDGrowth = 3,
			Element = ElementType.Nature,
			BaseRarity = Rarity.Uncommon,
			BaseCatchRate = 0.4f,
			PossibleTraits = new() { "verdant_power", "hardened_resolve", "vital_recovery" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "soul_siphon", LearnLevel = 1 },
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "nature_shield", LearnLevel = 8 },
				new LearnableMove { MoveId = "vitality_burst", LearnLevel = 16 },
				new LearnableMove { MoveId = "pollen_burst", LearnLevel = 24 },
				new LearnableMove { MoveId = "bloom_burst", LearnLevel = 34 }
			},
			BeastiaryNumber = 84
		} );

		// ═══════════════════════════════════════════════════════════════
		// NINJA MONKEY LINE (#137-138)
		// Fire ninja monkeys inspired by Sarugami and Hanuman
		// ═══════════════════════════════════════════════════════════════

		// Common Fire - Mischievous ninja monkey
		AddSpecies( new MonsterSpecies
		{
			Id = "hinobi",
			Name = "Hinobi",
			Description = "A small monkey wrapped in smoldering bandages, darting through shadows with embers trailing behind. It watches from rooftops and vanishes before anyone can look twice.",
			IconPath = "ui/monsters/hinobi/idle/hinobi_idle_01.png",
			BaseHP = 38, BaseATK = 55, BaseDEF = 28, BaseSpA = 32, BaseSpD = 30, BaseSPD = 62,
			HPGrowth = 3, ATKGrowth = 5, DEFGrowth = 2, SpAGrowth = 3, SpDGrowth = 2, SPDGrowth = 6,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Common,
			IsCatchable = true,
			BaseCatchRate = 0.55f,
			EvolvesTo = "enkong",
			EvolutionLevel = 22,
			PossibleTraits = new() { "kindle_heart", "phantom_step", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "kindle", LearnLevel = 6 },
				new LearnableMove { MoveId = "searing_rush", LearnLevel = 14 },
				new LearnableMove { MoveId = "fire_spin", LearnLevel = 20 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/hinobi/idle/hinobi_idle_01.png",
				"ui/monsters/hinobi/idle/hinobi_idle_02.png",
				"ui/monsters/hinobi/idle/hinobi_idle_03.png",
				"ui/monsters/hinobi/idle/hinobi_idle_04.png"
			},
			BeastiaryNumber = 22
		} );

		// Uncommon Fire - Blazing ninja warrior
		AddSpecies( new MonsterSpecies
		{
			Id = "enkong",
			Name = "Enkong",
			Description = "A fearsome monkey warrior with golden eyes that see through smoke and shadow. Its tail burns like a whip of living flame, said to have once set an entire fortress ablaze in a single sweep.",
			IconPath = "ui/monsters/enkong/idle/enkong_idle_01.png",
			BaseHP = 62, BaseATK = 88, BaseDEF = 43, BaseSpA = 52, BaseSpD = 45, BaseSPD = 92,
			HPGrowth = 4, ATKGrowth = 7, DEFGrowth = 3, SpAGrowth = 4, SpDGrowth = 3, SPDGrowth = 7,
			Element = ElementType.Fire,
			BaseRarity = Rarity.Uncommon,
			IsCatchable = true,
			BaseCatchRate = 0.25f,
			EvolvesFrom = "hinobi",
			PossibleTraits = new() { "kindle_heart", "phantom_step", "momentum" },
			LearnableMoves = new()
			{
				new LearnableMove { MoveId = "strike", LearnLevel = 1 },
				new LearnableMove { MoveId = "swift_lunge", LearnLevel = 1 },
				new LearnableMove { MoveId = "searing_rush", LearnLevel = 1, EvolvesFrom = "kindle" },
				new LearnableMove { MoveId = "fire_spin", LearnLevel = 20 },
				new LearnableMove { MoveId = "blazing_wrath", LearnLevel = 30 },
				new LearnableMove { MoveId = "inferno_blitz", LearnLevel = 40 }
			},
			AnimationFrames = new()
			{
				"ui/monsters/enkong/idle/enkong_idle_01.png",
				"ui/monsters/enkong/idle/enkong_idle_02.png",
				"ui/monsters/enkong/idle/enkong_idle_03.png",
				"ui/monsters/enkong/idle/enkong_idle_04.png"
			},
			BeastiaryNumber = 23
		} );

	}

	private void AddSpecies( MonsterSpecies species )
	{
		_speciesDatabase[species.Id] = species;
	}

	private void LoadMonsters()
	{
		var json = Game.Cookies.Get<string>( GetKey( MONSTER_COOKIE_KEY ), "[]" );
		try
		{
			OwnedMonsters = JsonSerializer.Deserialize<List<Monster>>( json ) ?? new();
		}
		catch
		{
			OwnedMonsters = new();
		}

		// Migrate existing monsters to v2 format (SpA/SpD stats and moves)
		bool needsSave = false;
		foreach ( var monster in OwnedMonsters )
		{
			if ( MigrateMonsterToV2( monster ) )
				needsSave = true;
		}

		if ( needsSave )
		{
			Log.Info( "Migrated monsters to v2 format with SpA/SpD and moves" );
			SaveMonsters();
		}

		Log.Info( $"Loaded {OwnedMonsters.Count} monsters" );
	}

	/// <summary>
	/// Migrate a monster to v2 format (SpA/SpD stats and moves)
	/// Returns true if migration was performed. Can be called to fix invalid moves at runtime.
	/// </summary>
	public bool MigrateMonsterToV2( Monster monster )
	{
		var species = GetSpecies( monster.SpeciesId );
		if ( species == null ) return false;

		bool migrated = false;

		// Ensure genetics exists (for very old saves)
		if ( monster.Genetics == null )
		{
			monster.Genetics = Genetics.GenerateRandom();
			migrated = true;
		}

		// Add SpA/SpD genes if missing (both zero suggests old save)
		if ( monster.Genetics.SpAGene == 0 && monster.Genetics.SpDGene == 0 )
		{
			var random = new Random();
			var avgGene = (monster.Genetics.ATKGene + monster.Genetics.DEFGene) / 2;

			// Determine if species is physical or special oriented
			bool isPhysical = species.BaseATK > species.BaseSpA;

			if ( isPhysical )
			{
				// Physical attackers get lower SpA genes
				monster.Genetics.SpAGene = Math.Clamp( avgGene - 5 + random.Next( -5, 6 ), 0, Genetics.MaxGeneValue );
				monster.Genetics.SpDGene = Math.Clamp( avgGene + random.Next( -5, 6 ), 0, Genetics.MaxGeneValue );
			}
			else
			{
				// Special attackers get higher SpA genes
				monster.Genetics.SpAGene = Math.Clamp( avgGene + 5 + random.Next( -5, 6 ), 0, Genetics.MaxGeneValue );
				monster.Genetics.SpDGene = Math.Clamp( avgGene + random.Next( -5, 6 ), 0, Genetics.MaxGeneValue );
			}

			migrated = true;
		}

		// Recalculate stats including SpA/SpD
		if ( monster.SpA == 0 && monster.SpD == 0 )
		{
			RecalculateStats( monster );
			migrated = true;
		}

		// Assign starting moves if monster has none
		if ( monster.Moves == null || monster.Moves.Count == 0 )
		{
			monster.Moves = GetStartingMoves( monster, species );
			migrated = true;
		}

		// Validate moves exist in MoveDatabase - fix old/invalid move IDs
		if ( monster.Moves != null && monster.Moves.Count > 0 )
		{
			bool hasInvalidMoves = false;
			foreach ( var move in monster.Moves )
			{
				if ( move != null && MoveDatabase.GetMove( move.MoveId ) == null )
				{
					Log.Warning( $"Monster {monster.Nickname ?? monster.SpeciesId} has invalid move: {move.MoveId}" );
					hasInvalidMoves = true;
					break;
				}
			}

			// If any moves are invalid, regenerate the entire moveset
			if ( hasInvalidMoves )
			{
				Log.Info( $"Regenerating moves for {monster.Nickname ?? monster.SpeciesId} due to invalid move IDs" );
				monster.Moves = GetStartingMoves( monster, species );
				migrated = true;
			}
		}

		// Migrate old trait display names to new trait IDs and validate they exist
		if ( monster.Traits != null && monster.Traits.Count > 0 )
		{
			var newTraits = new List<string>();
			foreach ( var trait in monster.Traits )
			{
				var convertedId = ConvertOldTraitToId( trait );

				// Validate the trait exists in TraitDatabase
				if ( TraitDatabase.GetTrait( convertedId ) != null )
				{
					if ( convertedId != trait )
						migrated = true;
					newTraits.Add( convertedId );
				}
				else
				{
					// Trait doesn't exist - skip it (will be replaced below)
					migrated = true;
				}
			}

			// If we lost traits due to invalid ones, assign new valid traits from species
			if ( newTraits.Count == 0 && species.PossibleTraits?.Count > 0 )
			{
				var random = new Random();
				int traitCount = Math.Min( random.Next( 1, 4 ), species.PossibleTraits.Count );
				newTraits = species.PossibleTraits
					.OrderBy( _ => random.Next() )
					.Take( traitCount )
					.ToList();
				migrated = true;
			}

			monster.Traits = newTraits;
		}

		return migrated;
	}

	/// <summary>
	/// Convert old trait display names to new trait IDs
	/// </summary>
	private string ConvertOldTraitToId( string trait )
	{
		// If trait already exists in TraitDatabase, return as-is
		if ( TraitDatabase.GetTrait( trait ) != null )
			return trait;

		// Map of old display names to new IDs
		var oldToNewMapping = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase )
		{
			// Fire traits
			{ "Ember Heart", "kindle_heart" },
			{ "Infernal Rage", "infernal_rage" },
			{ "Flame Eater", "flame_eater" },
			{ "Memory Keeper", "memory_keeper" },
			{ "Flickering", "flickering" },
			{ "Seven-Sighted", "seven_sighted" },
			{ "Pyre Born", "pyre_born" },
			{ "Hollow Flame", "hollow_flame" },
			{ "Volcanic Heart", "volcanic_heart" },
			{ "Glass Walker", "glass_walker" },
			{ "Eternal Burn", "eternal_burn" },
			{ "Ember Guardian", "kindle_guardian" },
			{ "Warmth Keeper", "warmth_keeper" },
			{ "Campfire Born", "campfire_born" },
			{ "Spark Shedder", "volt_charge_shedder" },
			{ "Fodeep_slumber Fire", "fodeep_slumber_fire" },
			{ "Scale Forge", "scale_forge" },
			{ "Lava Dweller", "lava_dweller" },
			{ "Tireless Worker", "tireless_worker" },
			{ "Heat Core", "heat_core" },
			{ "Loyal Flame", "loyal_flame" },
			{ "Never Burns", "never_burns" },
			{ "Ash Born", "ash_born" },
			{ "Ember Howl", "kindle_howl" },
			{ "Controlled Burn", "controlled_burn" },
			{ "Pack Leader", "pack_leader" },
			{ "Fallen Star", "fallen_star" },
			{ "Eternal Flame", "eternal_flame" },
			{ "Sky Forsaker", "sky_forsaker" },

			// Water traits
			{ "Torrent Soul", "torrent_soul" },
			{ "Deep Pressure", "deep_pressure" },
			{ "Aqua Siphon", "aqua_siphon" },
			{ "Tidal Wrath", "tidal_wrath" },
			{ "Coral Shield", "coral_shield" },
			{ "Abyssal Hide", "abyssal_hide" },
			{ "Current Rider", "current_rider" },
			{ "Bioluminescent", "bioluminescent" },
			{ "Pressure Adapt", "pressure_adapt" },
			{ "Ink Cloud", "ink_cloud" },
			{ "Storm Caller", "storm_caller" },
			{ "Fog Walker", "fog_walker" },
			{ "Rain Dance", "monsoon_call" },

			// Wind traits
			{ "Zephyr Grace", "zephyr_grace" },
			{ "Storm Surge", "storm_surge" },
			{ "Gale Force", "gale_force" },
			{ "Sky Dancer", "sky_dancer" },
			{ "Feather Fall", "feather_fall" },
			{ "Wind Walker", "wind_walker" },
			{ "Cyclone Heart", "cyclone_heart" },
			{ "Updraft", "updraft" },
			{ "Sonic Boom", "sonic_boom" },
			{ "Cloud Form", "cloud_form" },

			// Earth traits
			{ "Stone Heart", "stone_heart" },
			{ "Tremor Sense", "tremor_sense" },
			{ "Crystal Armor", "crystal_armor" },
			{ "Mud Slick", "mud_slick" },
			{ "Earthen Might", "earthen_might" },
			{ "Burrower", "burrower" },
			{ "Gem Encrusted", "gem_encrusted" },
			{ "Quake Step", "quake_step" },
			{ "Mountain Born", "mountain_born" },

			// Electric traits
			{ "Static Charge", "static_charge" },
			{ "Lightning Rod", "lightning_rod" },
			{ "Volt Absorb", "volt_soul_siphon" },
			{ "Thunder Clap", "storm_strike_clap" },
			{ "Spark Skin", "volt_charge_skin" },
			{ "Storm Battery", "storm_battery" },
			{ "Chain Lightning", "chain_lightning" },

			// Ice traits
			{ "Frost Heart", "frost_heart" },
			{ "Permafrost", "permafrost" },
			{ "Ice Body", "ice_body" },
			{ "Snow Cloak", "snow_cloak" },
			{ "Blizzard Born", "avalanche_wrath_born" },
			{ "Shatter", "shatter" },
			{ "Cold Blooded", "cold_blooded" },

			// Nature traits
			{ "Verdant Soul", "verdant_soul" },
			{ "Photodivine_grace", "photodivine_grace" },
			{ "Overharden", "overharden" },
			{ "Toxic Spore", "terror_visions_terror_visions" },
			{ "Root Network", "root_network" },
			{ "Healing Sap", "healing_sap" },
			{ "Thorn Armor", "thorn_armor" },
			{ "Fodeep_slumber Friend", "fodeep_slumber_friend" },

			// Metal traits
			{ "Iron Will", "iron_will" },
			{ "Steel Body", "steel_body" },
			{ "Rust Proof", "rust_proof" },
			{ "Magnetic", "magnetic" },
			{ "Heavy Metal", "heavy_metal" },
			{ "Chrome Plated", "chrome_plated" },
			{ "Forge Born", "forge_born" },

			// Shadow traits
			{ "Shadow Cloak", "shadow_cloak" },
			{ "Dark Pulse", "nightmare_wave" },
			{ "Nightmare", "nightmare_wave" },
			{ "Void Touch", "void_touch" },
			{ "Eclipse", "eclipse" },
			{ "Shadow Step", "shadow_step" },
			{ "Dark Adaptation", "dark_adaptation" },

			// Spirit traits
			{ "Spirit Link", "spirit_link" },
			{ "Ethereal", "ethereal" },
			{ "Soul Sight", "soul_sight" },
			{ "Blessing", "blessing" },
			{ "Divine Shield", "divine_shield" },
			{ "Purify", "purify" },
			{ "Celestial", "celestial" },
		};

		// Try to find a mapping
		if ( oldToNewMapping.TryGetValue( trait, out var newId ) )
			return newId;

		// If no mapping found, convert display name to snake_case ID
		return trait.ToLower().Replace( " ", "_" ).Replace( "-", "_" );
	}

	/// <summary>
	/// Get starting moves for a monster based on its level and species learnset
	/// </summary>
	private List<MonsterMove> GetStartingMoves( Monster monster, MonsterSpecies species )
	{
		var moves = new List<MonsterMove>();
		if ( species.LearnableMoves == null || species.LearnableMoves.Count == 0 )
			return moves;

		// Get all moves learnable at or below current level
		var availableMoves = species.LearnableMoves
			.Where( lm => lm.LearnLevel <= monster.Level )
			.OrderByDescending( lm => lm.LearnLevel )
			.ToList();

		// Handle evolution upgrades - if a move has EvolvesFrom, remove the old move
		var upgradedMoves = new HashSet<string>();
		foreach ( var lm in availableMoves )
		{
			if ( !string.IsNullOrEmpty( lm.EvolvesFrom ) )
				upgradedMoves.Add( lm.EvolvesFrom );
		}

		// Add up to 4 moves, preferring higher level moves, excluding upgraded ones
		foreach ( var lm in availableMoves )
		{
			if ( moves.Count >= Monster.MaxMoves )
				break;

			// Skip if this move was upgraded to a better version
			if ( upgradedMoves.Contains( lm.MoveId ) )
				continue;

			var moveDef = MoveDatabase.GetMove( lm.MoveId );
			if ( moveDef != null )
			{
				moves.Add( new MonsterMove
				{
					MoveId = lm.MoveId,
					CurrentPP = moveDef.MaxPP
				} );
			}
		}

		return moves;
	}

	/// <summary>
	/// Refresh a monster's moves based on its current level (useful after catching or level changes)
	/// </summary>
	public void RefreshMovesForLevel( Monster monster )
	{
		var species = GetSpecies( monster.SpeciesId );
		if ( species == null ) return;

		monster.Moves = GetStartingMoves( monster, species );
		Log.Info( $"Refreshed moves for {monster.Nickname} at level {monster.Level}: {string.Join( ", ", monster.Moves.Select( m => m.MoveId ) )}" );
	}

	public void SaveMonsters()
	{
		try
		{
			var json = JsonSerializer.Serialize( OwnedMonsters );
			Game.Cookies.Set( GetKey( MONSTER_COOKIE_KEY ), json );
		}
		catch ( Exception e )
		{
			Log.Warning( $"Failed to save monsters: {e.Message}" );
		}
	}

	/// <summary>
	/// Reload data from the current save slot
	/// </summary>
	public void ReloadFromSlot()
	{
		Log.Info( $"MonsterManager reloading from slot {SaveSlotManager.Instance?.ActiveSlot}, prefix={SaveSlotManager.GetSlotPrefix()}" );
		LoadMonsters();
		LoadMaxMonsters();
		Log.Info( $"MonsterManager reloaded: {OwnedMonsters.Count} monsters, MaxMonsters={MaxMonsters}" );
	}

	/// <summary>
	/// Clears all owned monsters and resets save data
	/// </summary>
	public void ClearAllMonsters()
	{
		OwnedMonsters.Clear();
		SaveMonsters();
		Log.Info( "All monsters cleared - returning to starter selection" );
	}

	[ConCmd( "reset_character" )]
	public static void ResetCharacterCommand()
	{
		Instance?.ClearAllMonsters();
		TamerManager.Instance?.ResetTamer();
		Log.Info( "Character reset complete. Restart the game to select a new starter." );
	}

	public MonsterSpecies GetSpecies( string speciesId )
	{
		return _speciesDatabase.TryGetValue( speciesId, out var species ) ? species : null;
	}

	public List<MonsterSpecies> GetAllSpecies()
	{
		return _speciesDatabase.Values.ToList();
	}

	public Monster GetMonster( Guid id )
	{
		return OwnedMonsters.FirstOrDefault( m => m.Id == id );
	}

	public Monster AddMonster( Monster monster )
	{
		Log.Info( $"AddMonster: OwnedMonsters.Count={OwnedMonsters.Count}, MaxMonsters={MaxMonsters}" );
		if ( OwnedMonsters.Count >= MaxMonsters )
		{
			Log.Warning( $"Monster storage full! Count={OwnedMonsters.Count}, Max={MaxMonsters}" );
			NotificationManager.Instance?.AddNotification(
				NotificationType.Warning,
				"Storage Full!",
				$"Your beast storage is full ({MaxMonsters}/{MaxMonsters}). Release some beasts or buy more storage in the shop.",
				8f
			);
			return null;
		}

		OwnedMonsters.Add( monster );

		// Add journal entry for joining the team
		var species = GetSpecies( monster.SpeciesId );
		if ( monster.IsBred )
		{
			monster.AddJournalEntry(
				$"Was born into the team! A new beginning awaits.",
				Data.JournalEntryType.Bred
			);
		}
		else
		{
			monster.AddJournalEntry(
				$"Joined the team through a contract. Ready to prove itself!",
				Data.JournalEntryType.Caught
			);
		}

		SaveMonsters();

		// Update beastiary
		BeastiaryManager.Instance?.DiscoverSpecies( monster.SpeciesId );

		OnMonsterAdded?.Invoke( monster );
		return monster;
	}

	public bool RemoveMonster( Guid monsterId )
	{
		var monster = OwnedMonsters.FirstOrDefault( m => m.Id == monsterId );
		if ( monster == null ) return false;

		OwnedMonsters.Remove( monster );
		SaveMonsters();

		OnMonsterRemoved?.Invoke( monster );
		return true;
	}

	public Monster CreateMonster( string speciesId, bool isBred = false, Genetics genetics = null )
	{
		var species = GetSpecies( speciesId );
		if ( species == null )
		{
			Log.Warning( $"Unknown species: {speciesId}" );
			return null;
		}

		var monster = new Monster
		{
			SpeciesId = speciesId,
			Nickname = species.Name,
			Genetics = genetics ?? Genetics.GenerateRandom(),
			Level = 1
		};

		// Calculate initial stats
		RecalculateStats( monster );
		monster.CurrentHP = monster.MaxHP;

		// Generate contract if not bred
		if ( !isBred )
		{
			monster.Contract = ContractGenerator.GenerateContract( species );
		}

		// Assign random traits
		var random = new Random();
		int traitCount = Math.Min( random.Next( 1, 4 ), species.PossibleTraits.Count );
		var selectedTraits = species.PossibleTraits
			.OrderBy( _ => random.Next() )
			.Take( traitCount )
			.ToList();
		monster.Traits = selectedTraits;

		// Assign starting moves based on level
		monster.Moves = GetStartingMoves( monster, species );

		return AddMonster( monster );
	}

	public Monster BreedMonsters( Monster parent1, Monster parent2, HashSet<string> lockedGenes = null )
	{
		// Must be same species (or evolutions of same line)
		var species1 = GetSpecies( parent1.SpeciesId );
		var species2 = GetSpecies( parent2.SpeciesId );

		if ( species1 == null || species2 == null ) return null;

		// Determine offspring species (use base form)
		string offspringSpeciesId = GetBaseFormId( parent1.SpeciesId );

		var offspringGenetics = GeneticsCalculator.CalculateOffspringGenetics( parent1.Genetics, parent2.Genetics, lockedGenes );

		// Offspring starts at average level of parents (minimum 1)
		int averageLevel = Math.Max( 1, (parent1.Level + parent2.Level) / 2 );

		var offspring = new Monster
		{
			SpeciesId = offspringSpeciesId,
			Nickname = GetSpecies( offspringSpeciesId )?.Name ?? "Baby",
			Genetics = offspringGenetics,
			Level = averageLevel,
			Parent1Id = parent1.Id,
			Parent2Id = parent2.Id,
			Generation = Math.Max( parent1.Generation, parent2.Generation ) + 1,
			Contract = null  // Bred monsters are loyal
		};

		RecalculateStats( offspring );
		offspring.CurrentHP = offspring.MaxHP;

		// Inherit traits from parents with rarity-based chances
		var random = new Random();
		var parentTraits = parent1.Traits.Concat( parent2.Traits ).Distinct().ToList();
		var inheritedTraits = new List<string>();

		// Get rare trait chance bonus from skills
		float rareTraitBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.RareTraitChance ) ?? 0;

		foreach ( var traitId in parentTraits )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait == null ) continue;

			// Base inheritance chance by rarity
			float inheritChance = trait.Rarity switch
			{
				TraitRarity.Common => 0.60f,
				TraitRarity.Uncommon => 0.40f,
				TraitRarity.Rare => 0.25f,
				TraitRarity.Epic => 0.10f,
				TraitRarity.Legendary => 0.05f,
				_ => 0.60f
			};

			// Apply rare trait bonus (affects rare+ traits more)
			if ( trait.Rarity >= TraitRarity.Rare )
			{
				inheritChance += rareTraitBonus / 100f;
			}

			if ( random.NextDouble() < inheritChance )
			{
				inheritedTraits.Add( traitId );
			}
		}

		// Ensure at least 1 trait if parents had any, cap at 3
		if ( inheritedTraits.Count == 0 && parentTraits.Count > 0 )
		{
			inheritedTraits.Add( parentTraits[random.Next( parentTraits.Count )] );
		}
		else if ( inheritedTraits.Count > 3 )
		{
			inheritedTraits = inheritedTraits.OrderBy( _ => random.Next() ).Take( 3 ).ToList();
		}

		offspring.Traits = inheritedTraits;

		// Assign starting moves based on offspring's level
		var offspringSpecies = GetSpecies( offspringSpeciesId );
		if ( offspringSpecies != null )
		{
			offspring.Moves = GetStartingMoves( offspring, offspringSpecies );
		}

		// Update tamer stats
		if ( TamerManager.Instance?.CurrentTamer != null )
		{
			TamerManager.Instance.CurrentTamer.TotalMonstersBred++;
		}

		// Remove parent monsters (fusion consumes them)
		RemoveMonster( parent1.Id );
		RemoveMonster( parent2.Id );

		return AddMonster( offspring );
	}

	private string GetBaseFormId( string speciesId )
	{
		var species = GetSpecies( speciesId );
		if ( species == null ) return speciesId;

		// Follow evolution chain backwards
		while ( !string.IsNullOrEmpty( species.EvolvesFrom ) )
		{
			species = GetSpecies( species.EvolvesFrom );
			if ( species == null ) break;
		}

		return species?.Id ?? speciesId;
	}

	public void RecalculateStats( Monster monster )
	{
		var species = GetSpecies( monster.SpeciesId );
		if ( species == null ) return;

		// Ensure genetics exists (for migrating old saves)
		if ( monster.Genetics == null )
		{
			monster.Genetics = Genetics.GenerateRandom();
		}

		// Stat formula with diminishing returns at higher levels:
		// Base + sqrt(Level) * Growth * 4 + Gene
		// This keeps early-game progression while capping high-level stats
		// Example at Lv100: 85 + 10*6*4 + 50 = 375 HP (vs old formula: 785)
		float levelFactor = (float)Math.Sqrt( monster.Level );

		monster.MaxHP = (int)(species.BaseHP + (levelFactor * species.HPGrowth * 4) + monster.Genetics.HPGene);
		monster.ATK = (int)(species.BaseATK + (levelFactor * species.ATKGrowth * 4) + monster.Genetics.ATKGene);
		monster.DEF = (int)(species.BaseDEF + (levelFactor * species.DEFGrowth * 4) + monster.Genetics.DEFGene);
		monster.SpA = (int)(species.BaseSpA + (levelFactor * species.SpAGrowth * 4) + monster.Genetics.SpAGene);
		monster.SpD = (int)(species.BaseSpD + (levelFactor * species.SpDGrowth * 4) + monster.Genetics.SpDGene);
		monster.SPD = (int)(species.BaseSPD + (levelFactor * species.SPDGrowth * 4) + monster.Genetics.SPDGene);

		// Apply nature modifiers
		ApplyNatureModifiers( monster );

		// Apply tamer skill bonuses
		ApplyTamerBonuses( monster, species );

		// Apply relic bonuses
		ApplyRelicBonuses( monster );

		// Apply veteran bonuses (based on battles fought)
		ApplyVeteranBonuses( monster );
	}

	/// <summary>
	/// Apply veteran bonuses based on battles fought
	/// </summary>
	private void ApplyVeteranBonuses( Monster monster )
	{
		float bonusPercent = monster.GetVeteranBonusPercent();
		if ( bonusPercent <= 0 )
			return;

		// Apply bonus to all stats except MaxHP (which gets a smaller bonus to avoid snowballing)
		monster.ATK = (int)(monster.ATK * (1 + bonusPercent));
		monster.DEF = (int)(monster.DEF * (1 + bonusPercent));
		monster.SpA = (int)(monster.SpA * (1 + bonusPercent));
		monster.SpD = (int)(monster.SpD * (1 + bonusPercent));
		monster.SPD = (int)(monster.SPD * (1 + bonusPercent));

		// HP gets half the bonus to keep battles balanced
		monster.MaxHP = (int)(monster.MaxHP * (1 + bonusPercent * 0.5f));
	}

	private void ApplyNatureModifiers( Monster monster )
	{
		float modifier = 0.1f;

		switch ( monster.Genetics.Nature )
		{
			case NatureType.Ferocious:  // +ATK, -DEF
				monster.ATK = (int)(monster.ATK * (1 + modifier));
				monster.DEF = (int)(monster.DEF * (1 - modifier));
				break;
			case NatureType.Stalwart:   // +DEF, -ATK
				monster.DEF = (int)(monster.DEF * (1 + modifier));
				monster.ATK = (int)(monster.ATK * (1 - modifier));
				break;
			case NatureType.Restless:   // +SPD, -HP
				monster.SPD = (int)(monster.SPD * (1 + modifier));
				monster.MaxHP = (int)(monster.MaxHP * (1 - modifier));
				break;
			case NatureType.Enduring:   // +HP, -SPD
				monster.MaxHP = (int)(monster.MaxHP * (1 + modifier));
				monster.SPD = (int)(monster.SPD * (1 - modifier));
				break;
			case NatureType.Reckless:   // +ATK, -SPD
				monster.ATK = (int)(monster.ATK * (1 + modifier));
				monster.SPD = (int)(monster.SPD * (1 - modifier));
				break;
			case NatureType.Stoic:      // +DEF, -SPD
				monster.DEF = (int)(monster.DEF * (1 + modifier));
				monster.SPD = (int)(monster.SPD * (1 - modifier));
				break;
			case NatureType.Skittish:   // +SPD, -DEF
				monster.SPD = (int)(monster.SPD * (1 + modifier));
				monster.DEF = (int)(monster.DEF * (1 - modifier));
				break;
			case NatureType.Vigorous:   // +HP, -ATK
				monster.MaxHP = (int)(monster.MaxHP * (1 + modifier));
				monster.ATK = (int)(monster.ATK * (1 - modifier));
				break;
			case NatureType.Ruthless:   // +ATK, -HP
				monster.ATK = (int)(monster.ATK * (1 + modifier));
				monster.MaxHP = (int)(monster.MaxHP * (1 - modifier));
				break;
			case NatureType.Nimble:     // +SPD, -ATK
				monster.SPD = (int)(monster.SPD * (1 + modifier));
				monster.ATK = (int)(monster.ATK * (1 - modifier));
				break;
			case NatureType.Mystical:   // +SpA, -ATK
				monster.SpA = (int)(monster.SpA * (1 + modifier));
				monster.ATK = (int)(monster.ATK * (1 - modifier));
				break;
			case NatureType.Resolute:   // +SpD, -SpA
				monster.SpD = (int)(monster.SpD * (1 + modifier));
				monster.SpA = (int)(monster.SpA * (1 - modifier));
				break;
			case NatureType.Arcane:     // +SpA, -DEF
				monster.SpA = (int)(monster.SpA * (1 + modifier));
				monster.DEF = (int)(monster.DEF * (1 - modifier));
				break;
			case NatureType.Warded:     // +SpD, -SPD
				monster.SpD = (int)(monster.SpD * (1 + modifier));
				monster.SPD = (int)(monster.SPD * (1 - modifier));
				break;
			case NatureType.Cunning:    // +SpA, -HP
				monster.SpA = (int)(monster.SpA * (1 + modifier));
				monster.MaxHP = (int)(monster.MaxHP * (1 - modifier));
				break;
			case NatureType.Serene:     // +SpD, -ATK
				monster.SpD = (int)(monster.SpD * (1 + modifier));
				monster.ATK = (int)(monster.ATK * (1 - modifier));
				break;
			// NatureType.Balanced has no effect
		}
	}

	private void ApplyTamerBonuses( Monster monster, MonsterSpecies species )
	{
		var tamer = TamerManager.Instance;
		if ( tamer == null ) return;

		// Apply percentage bonuses from skills
		float atkBonus = tamer.GetSkillBonus( SkillEffectType.AllMonsterATKPercent );
		float defBonus = tamer.GetSkillBonus( SkillEffectType.AllMonsterDEFPercent );
		float spdBonus = tamer.GetSkillBonus( SkillEffectType.AllMonsterSPDPercent );
		float hpBonus = tamer.GetSkillBonus( SkillEffectType.AllMonsterHPPercent );
		float spaBonus = tamer.GetSkillBonus( SkillEffectType.AllMonsterSpAPercent );
		float spdDefBonus = tamer.GetSkillBonus( SkillEffectType.AllMonsterSpDPercent );

		monster.ATK = (int)(monster.ATK * (1 + atkBonus / 100f));
		monster.DEF = (int)(monster.DEF * (1 + defBonus / 100f));
		monster.SPD = (int)(monster.SPD * (1 + spdBonus / 100f));
		monster.MaxHP = (int)(monster.MaxHP * (1 + hpBonus / 100f));
		monster.SpA = (int)(monster.SpA * (1 + spaBonus / 100f));
		monster.SpD = (int)(monster.SpD * (1 + spdDefBonus / 100f));
	}

	private void ApplyRelicBonuses( Monster monster )
	{
		var items = ItemManager.Instance;
		if ( items == null ) return;

		float atkBonus = items.GetRelicBonus( ItemEffectType.PassiveATKBoost );
		float defBonus = items.GetRelicBonus( ItemEffectType.PassiveDEFBoost );
		float spdBonus = items.GetRelicBonus( ItemEffectType.PassiveSPDBoost );
		float hpBonus = items.GetRelicBonus( ItemEffectType.PassiveHPBoost );

		if ( atkBonus != 0 ) monster.ATK = (int)(monster.ATK * (1 + atkBonus / 100f));
		if ( defBonus != 0 ) monster.DEF = (int)(monster.DEF * (1 + defBonus / 100f));
		if ( spdBonus != 0 ) monster.SPD = (int)(monster.SPD * (1 + spdBonus / 100f));
		if ( hpBonus != 0 ) monster.MaxHP = (int)(monster.MaxHP * (1 + hpBonus / 100f));
	}

	public void ReleaseMonster( Guid monsterId )
	{
		var monster = OwnedMonsters.FirstOrDefault( m => m.Id == monsterId );
		if ( monster != null )
		{
			OwnedMonsters.Remove( monster );
			SaveMonsters();
			OnMonsterRemoved?.Invoke( monster );
		}
	}

	/// <summary>
	/// Release multiple monsters at once
	/// </summary>
	/// <param name="monsterIds">List of monster IDs to release</param>
	/// <returns>Number of monsters released</returns>
	public int ReleaseMonsters( List<Guid> monsterIds )
	{
		if ( monsterIds == null || monsterIds.Count == 0 ) return 0;

		// Don't allow releasing all monsters - must keep at least 1
		int maxToRelease = OwnedMonsters.Count - 1;
		int toRelease = Math.Min( monsterIds.Count, maxToRelease );

		if ( toRelease <= 0 ) return 0;

		int released = 0;
		foreach ( var monsterId in monsterIds.Take( toRelease ) )
		{
			var monster = OwnedMonsters.FirstOrDefault( m => m.Id == monsterId );
			if ( monster != null )
			{
				OwnedMonsters.Remove( monster );
				OnMonsterRemoved?.Invoke( monster );
				released++;
			}
		}

		if ( released > 0 )
		{
			SaveMonsters();
		}

		return released;
	}

	public bool EvolveMonster( Guid monsterId )
	{
		var monster = GetMonster( monsterId );
		if ( monster == null ) return false;

		var species = GetSpecies( monster.SpeciesId );
		if ( species == null || string.IsNullOrEmpty( species.EvolvesTo ) ) return false;
		if ( monster.Level < species.EvolutionLevel ) return false;

		var evolvedSpecies = GetSpecies( species.EvolvesTo );
		if ( evolvedSpecies == null ) return false;

		// Store the old species for the event
		var fromSpecies = species;

		// Evolve
		monster.SpeciesId = species.EvolvesTo;
		monster.Nickname = evolvedSpecies.Name;

		// Add journal entry for evolution
		monster.AddJournalEntry(
			$"Evolved from {fromSpecies.Name} into {evolvedSpecies.Name}! A major milestone!",
			Data.JournalEntryType.Evolution
		);

		// Upgrade moves to evolved versions
		UpgradeMovesOnEvolution( monster, evolvedSpecies );

		RecalculateStats( monster );
		monster.FullHeal();

		// Discover the new species in beastiary
		BeastiaryManager.Instance?.DiscoverSpecies( evolvedSpecies.Id );

		SaveMonsters();
		OnMonsterUpdated?.Invoke( monster );
		OnMonsterEvolved?.Invoke( monster, fromSpecies, evolvedSpecies );

		Log.Info( $"Monster evolved from {fromSpecies.Name} to {evolvedSpecies.Name}!" );

		// Update tamer stats
		if ( TamerManager.Instance?.CurrentTamer != null )
		{
			TamerManager.Instance.CurrentTamer.TotalMonstersEvolved++;
		}

		return true;
	}

	/// <summary>
	/// Check for new moves to learn on level up
	/// Returns a list of move names that were learned
	/// </summary>
	public List<string> CheckAndLearnNewMoves( Monster monster )
	{
		var learnedMoves = new List<string>();
		var species = GetSpecies( monster.SpeciesId );
		if ( species == null || species.LearnableMoves == null || monster.Moves == null )
			return learnedMoves;

		// Find moves that can be learned at current level
		var currentMoveIds = new HashSet<string>( monster.Moves.Select( m => m.MoveId ) );

		foreach ( var lm in species.LearnableMoves )
		{
			// Skip if we already know this move
			if ( currentMoveIds.Contains( lm.MoveId ) )
				continue;

			// Skip if this is an evolution upgrade (handled in UpgradeMovesOnEvolution)
			if ( !string.IsNullOrEmpty( lm.EvolvesFrom ) )
				continue;

			// Check if we meet the level requirement
			if ( monster.Level >= lm.LearnLevel )
			{
				var moveDef = MoveDatabase.GetMove( lm.MoveId );
				if ( moveDef == null )
					continue;

				// If we have room, add the move
				if ( monster.Moves.Count < Monster.MaxMoves )
				{
					monster.Moves.Add( new MonsterMove
					{
						MoveId = lm.MoveId,
						CurrentPP = moveDef.MaxPP
					} );
					learnedMoves.Add( moveDef.Name );
					currentMoveIds.Add( lm.MoveId );
				}
				// If full, the player will need to choose which move to replace (UI handles this)
				// For now, we just note that a new move could be learned
			}
		}

		if ( learnedMoves.Count > 0 )
		{
			SaveMonsters();
		}

		return learnedMoves;
	}

	/// <summary>
	/// Upgrade moves when a monster evolves (e.g., Ember -> Flame Burst)
	/// </summary>
	private void UpgradeMovesOnEvolution( Monster monster, MonsterSpecies evolvedSpecies )
	{
		if ( monster.Moves == null || evolvedSpecies.LearnableMoves == null )
			return;

		// Build a map of which moves upgrade to which
		var upgrades = new Dictionary<string, string>();
		foreach ( var lm in evolvedSpecies.LearnableMoves )
		{
			if ( !string.IsNullOrEmpty( lm.EvolvesFrom ) && lm.LearnLevel <= monster.Level )
			{
				upgrades[lm.EvolvesFrom] = lm.MoveId;
			}
		}

		// Replace old moves with upgraded versions
		foreach ( var move in monster.Moves )
		{
			if ( upgrades.TryGetValue( move.MoveId, out var upgradedMoveId ) )
			{
				var newMoveDef = MoveDatabase.GetMove( upgradedMoveId );
				if ( newMoveDef != null )
				{
					Log.Info( $"Move {move.MoveId} evolved to {upgradedMoveId}!" );
					move.MoveId = upgradedMoveId;
					move.CurrentPP = newMoveDef.MaxPP;
				}
			}
		}
	}
}



