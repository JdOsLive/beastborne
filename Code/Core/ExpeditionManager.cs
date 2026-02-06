using Sandbox;
using Beastborne.Data;
using Beastborne.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beastborne.Core;

/// <summary>
/// Manages PvE expedition content
/// </summary>
public sealed class ExpeditionManager : Component
{
	public static ExpeditionManager Instance { get; private set; }

	// Expedition data
	private List<Expedition> _expeditions = new();
	public IReadOnlyList<Expedition> Expeditions => _expeditions;

	// Current expedition state
	public Expedition CurrentExpedition { get; private set; }
	public int CurrentWave { get; private set; }
	public List<Monster> SelectedTeam { get; private set; } = new();

	// Auto-battle settings (default to OFF for better new player experience)
	public bool AutoBattle { get; set; } = false;
	public bool AutoRetry { get; set; } = false;
	public bool AutoNegotiate { get; set; } = false;
	public int AutoNegotiateStrategy { get; set; } = 0; // Index into negotiation options (0-3)
	public string AutoContractTargetSpecies { get; set; } = null;
	public bool IsRunningInBackground { get; private set; } = false;

	// Hard Mode (unlocked via Cartographer skill)
	public bool HardModeEnabled { get; set; } = false;
	public const int HARD_MODE_LEVEL_BONUS = 10;
	public const float HARD_MODE_REWARD_MULTIPLIER = 1.5f;

	/// <summary>
	/// Check if Hard Mode is unlocked via Cartographer skill (rank 1+)
	/// </summary>
	public bool IsHardModeUnlocked()
	{
		return TamerManager.Instance?.GetSkillRank( "exp_cartographer" ) >= 1;
	}

	/// <summary>
	/// Get the effective enemy level for the current expedition
	/// </summary>
	public int GetEffectiveEnemyLevel( int baseLevel )
	{
		if ( HardModeEnabled && IsHardModeUnlocked() )
			return baseLevel + HARD_MODE_LEVEL_BONUS;
		return baseLevel;
	}

	/// <summary>
	/// Get reward multiplier (includes hard mode bonus)
	/// </summary>
	public float GetRewardMultiplier()
	{
		float multiplier = 1f;
		if ( HardModeEnabled && IsHardModeUnlocked() )
			multiplier *= HARD_MODE_REWARD_MULTIPLIER;
		return multiplier;
	}

	// Species filter for auto-contract
	public bool UseSpeciesFilter { get; set; } = false;
	public HashSet<string> EnabledSpeciesFilter { get; set; } = new();

	/// <summary>
	/// Check if a species is enabled for auto-contract
	/// Returns true if filter is disabled OR if species is in the enabled list
	/// </summary>
	public bool IsSpeciesEnabledForAutoContract( string speciesId )
	{
		if ( !UseSpeciesFilter || EnabledSpeciesFilter.Count == 0 )
			return true; // Filter disabled or empty = all enabled
		return EnabledSpeciesFilter.Contains( speciesId );
	}

	// Accumulated rewards during expedition
	public int AccumulatedGold { get; private set; } = 0;
	public int AccumulatedXP { get; private set; } = 0;
	public int AccumulatedTokens { get; private set; } = 0;
	public List<(string ItemId, int Quantity)> AccumulatedItems { get; private set; } = new();

	/// <summary>
	/// Add rewards to accumulated totals (called from GameHUD during background battles)
	/// </summary>
	public void AddAccumulatedRewards( int gold, int xp )
	{
		AccumulatedGold += gold;
		AccumulatedXP += xp;
		Log.Info( $"[ExpeditionManager] AddAccumulatedRewards: +{gold} gold, +{xp} XP. Total: {AccumulatedGold} gold, {AccumulatedXP} XP" );
	}

	/// <summary>
	/// Add item drops to accumulated items
	/// </summary>
	public void AddAccumulatedItems( List<(string ItemId, int Quantity)> items )
	{
		if ( items == null || items.Count == 0 ) return;

		foreach ( var (itemId, qty) in items )
		{
			// Check if we already have this item accumulated, merge quantities
			var existing = AccumulatedItems.FindIndex( i => i.ItemId == itemId );
			if ( existing >= 0 )
			{
				var current = AccumulatedItems[existing];
				AccumulatedItems[existing] = (itemId, current.Quantity + qty);
			}
			else
			{
				AccumulatedItems.Add( (itemId, qty) );
			}
		}

		Log.Info( $"[ExpeditionManager] Added {items.Count} item drops. Total accumulated: {AccumulatedItems.Count} unique items" );
	}

	/// <summary>
	/// Award all accumulated items to player's inventory
	/// </summary>
	private void AwardAccumulatedItems()
	{
		if ( AccumulatedItems == null || AccumulatedItems.Count == 0 ) return;

		foreach ( var (itemId, qty) in AccumulatedItems )
		{
			ItemManager.Instance?.AddItem( itemId, qty );
		}

		Log.Info( $"[ExpeditionManager] Awarded {AccumulatedItems.Count} unique item types to inventory" );
		AccumulatedItems = new();
	}

	// Negotiation state - tracks catchable enemies from the last battle
	public List<Monster> CatchableEnemies { get; private set; } = new();
	public bool HasCatchableEnemy => CatchableEnemies.Count > 0;

	// Boss state
	public ActiveBossState CurrentBossState { get; private set; }
	public BossData SelectedBoss { get; private set; }
	public bool IsBossWave => CurrentExpedition != null && (CurrentExpedition.IsBossGauntlet || CurrentWave == CurrentExpedition.Waves);
	public bool IsRareBossEncounter { get; private set; } = false;
	private List<BossData> GauntletBossOrder { get; set; } // Shuffled boss order for gauntlets

	// Shared random instance to avoid seeding issues on quick retries
	private static readonly Random _sharedRandom = new Random();

	// Events
	public Action<Expedition> OnExpeditionStarted;
	public Action<bool> OnExpeditionComplete;
	public Action<Monster> OnMonsterCaught;
	public Action<int, int> OnWaveCompleted; // wave number, total waves

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Enabled = true; // Ensure component is enabled for OnUpdate to be called
			Log.Info( "ExpeditionManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnStart()
	{
		GenerateExpeditions();

		// Subscribe to battle end events for background processing
		SubscribeToBattleManager();
	}

	/// <summary>
	/// Subscribe to BattleManager events. Called from OnStart and OnUpdate to handle initialization order.
	/// </summary>
	private bool _subscribedToBattleManager = false;
	private void SubscribeToBattleManager()
	{
		if (_subscribedToBattleManager)
		{
			return;
		}

		if (BattleManager.Instance == null)
		{
			Log.Info("SubscribeToBattleManager: BattleManager.Instance is null, will retry later");
			return;
		}

		BattleManager.Instance.OnBattleEnd += OnBattleEndBackground;
		BattleManager.Instance.OnMonsterDefeated += OnEnemyDefeated;
		_subscribedToBattleManager = true;

		int handlerCount = BattleManager.Instance.OnBattleEnd?.GetInvocationList()?.Length ?? 0;
		Log.Info($"ExpeditionManager subscribed to BattleManager.OnBattleEnd! Handler count: {handlerCount}");
	}

	protected override void OnDestroy()
	{
		if (BattleManager.Instance != null)
		{
			BattleManager.Instance.OnBattleEnd -= OnBattleEndBackground;
			BattleManager.Instance.OnMonsterDefeated -= OnEnemyDefeated;
		}
	}

	/// <summary>
	/// Called when any monster is defeated in battle.
	/// Awards Tamer XP immediately when an enemy is defeated.
	/// </summary>
	private void OnEnemyDefeated( Monster defeatedMonster )
	{
		// Only award XP during an active expedition
		if ( CurrentExpedition == null )
			return;

		// Check if the defeated monster is an enemy (not a player monster)
		if ( BattleManager.Instance?.PlayerTeam?.Any( m => m.Id == defeatedMonster.Id ) == true )
			return; // This is a player monster, not an enemy

		// Calculate Tamer XP based on defeated monster's level
		int baseXP = 2 + (defeatedMonster.Level / 2);

		// Apply expedition XP bonus from tamer skills
		float xpBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ExpeditionXPBonus ) ?? 0;
		int totalXP = (int)(baseXP * (1 + xpBonus / 100f));

		// Award XP to tamer
		TamerManager.Instance?.AddXP( totalXP );

		Log.Info( $"Tamer gained {totalXP} XP from defeating {defeatedMonster.Nickname ?? "enemy"} (Level {defeatedMonster.Level})" );
	}

	protected override void OnUpdate()
	{
		// Ensure subscription to BattleManager (handles initialization order)
		if (!_subscribedToBattleManager)
		{
			SubscribeToBattleManager();
		}

		// Note: Background battle ticking is handled by GameHUD.OnUpdate via TickBackgroundExpedition()
		// This ensures reliable ticking since Component.OnUpdate may not run in all scenarios.
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null )
		{
			Log.Info( $"ExpeditionManager.EnsureInstance: Instance already exists, Enabled={Instance.Enabled}" );
			return;
		}

		var go = scene.CreateObject();
		go.Name = "ExpeditionManager";
		go.Flags = GameObjectFlags.DontDestroyOnLoad;
		var em = go.Components.Create<ExpeditionManager>();
		em.Enabled = true;
		Log.Info( $"ExpeditionManager.EnsureInstance: Created new instance, Enabled={em.Enabled}" );
	}

	private void GenerateExpeditions()
	{
		// Create expedition stages
		_expeditions.Clear();

		// Level 1 - Starter area with Whispering Woods creatures
		_expeditions.Add( new Expedition
		{
			Id = "forest_entrance",
			Name = "Whispering Woods",
			Description = "A quiet forest where young spirits gather",
			RequiredLevel = 1,
			Waves = 3,
			BaseEnemyLevel = 1,
			PossibleSpecies = new() { "twigsnap", "dewdrop", "dustling", "mosscreep", "whiskerwind", "glimshroom", "branchling" },
			Element = ElementType.Neutral,
			GoldReward = 75,
			XPReward = 50
		} );

		// Level 5 - Fire
		_expeditions.Add( new Expedition
		{
			Id = "ember_cavern",
			Name = "Ember Cavern",
			Description = "Volcanic caves where fire spirits dwell",
			RequiredLevel = 5,
			Waves = 4,
			BaseEnemyLevel = 5,
			PossibleSpecies = new() { "embrik", "cinderscale", "blazefang", "magmite", "smolderpup", "emberhound", "hinobi", "enkong" },
			Element = ElementType.Fire,
			GoldReward = 150,
			XPReward = 200
		} );

		// Level 10 - Water
		_expeditions.Add( new Expedition
		{
			Id = "tear_lake",
			Name = "Lake of Tears",
			Description = "A melancholy lake shrouded in mist",
			RequiredLevel = 10,
			Waves = 4,
			BaseEnemyLevel = 10,
			PossibleSpecies = new() { "droskul", "puddlejaw", "mirrorpond", "weepfin", "streamling", "rivercrest", "bubblite", "coralheim" },
			Element = ElementType.Water,
			GoldReward = 175,
			XPReward = 250
		} );

		// Level 15 - Wind
		_expeditions.Add( new Expedition
		{
			Id = "echo_canyon",
			Name = "Echo Canyon",
			Description = "Where winds carry voices of the forgotten",
			RequiredLevel = 15,
			Waves = 5,
			BaseEnemyLevel = 15,
			PossibleSpecies = new() { "wispryn", "driftmote", "galeclaw", "whistleshade", "zephyrmite", "cyclonyx", "featherwisp", "vortexel", "dandepuff" },
			Element = ElementType.Wind,
			GoldReward = 225,
			XPReward = 350
		} );

		// Level 20 - Electric
		_expeditions.Add( new Expedition
		{
			Id = "storm_spire",
			Name = "Storm Spire",
			Description = "A tower that attracts endless lightning",
			RequiredLevel = 20,
			Waves = 5,
			BaseEnemyLevel = 20,
			PossibleSpecies = new() { "sparklet", "voltweave", "staticling", "joltpaw", "thundermane", "zapfin", "boltgeist", "circuitsprite" },
			Element = ElementType.Electric,
			GoldReward = 250,
			XPReward = 400
		} );

		// Level 25 - Earth
		_expeditions.Add( new Expedition
		{
			Id = "ancient_ruins",
			Name = "Ancient Ruins",
			Description = "Crumbling stones that remember when they were mountains",
			RequiredLevel = 25,
			Waves = 5,
			BaseEnemyLevel = 25,
			PossibleSpecies = new() { "rootling", "cragmaw", "rubblekin", "pebblit", "boulderon", "quartzite", "dustback", "terraclops", "terracub" },
			Element = ElementType.Earth,
			GoldReward = 275,
			XPReward = 500
		} );

		// Level 30 - Ice
		_expeditions.Add( new Expedition
		{
			Id = "frozen_vale",
			Name = "Frozen Vale",
			Description = "A valley where warmth goes to die",
			RequiredLevel = 30,
			Waves = 6,
			BaseEnemyLevel = 30,
			PossibleSpecies = new() { "frostling", "glacimaw", "shivershard", "snowmite", "blizzardian", "iciclaw", "frostwisp", "sleethorn" },
			Element = ElementType.Ice,
			GoldReward = 325,
			XPReward = 600
		} );

		// Level 35 - Nature
		_expeditions.Add( new Expedition
		{
			Id = "overgrown_heart",
			Name = "Overgrown Heart",
			Description = "The center of a forest that refuses to stop growing",
			RequiredLevel = 35,
			Waves = 6,
			BaseEnemyLevel = 35,
			PossibleSpecies = new() { "sproutkin", "thornveil", "mosswhisper", "pollenpuff", "bloomguard", "vinewhip", "fungrowth", "willowwisp" },
			Element = ElementType.Nature,
			GoldReward = 375,
			XPReward = 700
		} );

		// Level 40 - Metal
		_expeditions.Add( new Expedition
		{
			Id = "rusted_foundry",
			Name = "Rusted Foundry",
			Description = "An ancient forge where metal learned to think",
			RequiredLevel = 40,
			Waves = 6,
			BaseEnemyLevel = 40,
			PossibleSpecies = new() { "coglet", "ironclad", "corrode", "scrapper", "junktitan", "bladefly", "bellguard", "chainlink" },
			Element = ElementType.Metal,
			GoldReward = 425,
			XPReward = 850
		} );

		// Level 45 - Spirit
		_expeditions.Add( new Expedition
		{
			Id = "dawn_sanctuary",
			Name = "Spirit Sanctum",
			Description = "An ethereal place where spirits commune with the living",
			RequiredLevel = 45,
			Waves = 6,
			BaseEnemyLevel = 45,
			PossibleSpecies = new() { "dawnmote", "haloveil", "echomind", "wishling", "hopebringer", "memoryveil", "solmara", "soulflare", "dreamspark" },
			Element = ElementType.Spirit,
			GoldReward = 500,
			XPReward = 1000
		} );

		// Level 50 - Shadow
		_expeditions.Add( new Expedition
		{
			Id = "shadow_depths",
			Name = "Shadow Depths",
			Description = "The place where even darkness is afraid",
			RequiredLevel = 50,
			Waves = 7,
			BaseEnemyLevel = 50,
			PossibleSpecies = new() { "murkmaw", "voidweep", "gloomling", "nightcrawl", "duskstalker", "fearling", "umbralynx", "secretkeeper" },
			Element = ElementType.Shadow,
			GoldReward = 550,
			XPReward = 1250
		} );

		// Level 55 - Elemental Champions (strongest evolved form of each element)
		_expeditions.Add( new Expedition
		{
			Id = "elemental_nexus",
			Name = "Elemental Nexus",
			Description = "Where the elemental champions gather",
			RequiredLevel = 55,
			Waves = 7,
			BaseEnemyLevel = 55,
			PossibleSpecies = new() { "ashenmare", "infernowarg", "tidehollow", "oceanmaw", "vexstorm", "voltweave", "thundermane", "monoleth", "permafrost", "glacierback", "verdantis", "eldergrove", "forgeborn", "chromedragon", "solmara", "nullgrave" },
			Element = ElementType.Neutral,
			GoldReward = 600,
			XPReward = 3500,
			HasBoss = true,
			BossSpeciesId = "primordius"
		} );

		// Level 65 - Primordial Rift (reality-warping creatures only)
		_expeditions.Add( new Expedition
		{
			Id = "primordial_rift",
			Name = "Primordial Rift",
			Description = "A tear in reality where dimensional beings emerge",
			RequiredLevel = 65,
			Waves = 8,
			BaseEnemyLevel = 65,
			PossibleSpecies = new() { "raijura", "arcferron", "devorah", "pucling", "scaldnip", "temporal", "eternawing", "nightmarex" },
			Element = ElementType.Neutral,
			GoldReward = 750,
			XPReward = 5000,
			HasBoss = true,
			BossSpeciesId = "voiddragon"
		} );

		// Level 75 - Garden of Origins (pure Nature primordial creatures)
		_expeditions.Add( new Expedition
		{
			Id = "garden_of_origins",
			Name = "Garden of Origins",
			Description = "Where the first seeds of existence took root",
			RequiredLevel = 75,
			Waves = 9,
			BaseEnemyLevel = 75,
			PossibleSpecies = new() { "eldergrove", "primbloom", "thornveil", "verdantis", "bloomguard", "edenseed" },
			Element = ElementType.Nature,
			GoldReward = 1000,
			XPReward = 7500,
			HasBoss = true,
			BossSpeciesId = "songborne"
		} );

		// Level 85 - Mythweaver's Realm (Epic/Legendary beasts only)
		_expeditions.Add( new Expedition
		{
			Id = "mythweavers_realm",
			Name = "Mythweaver's Realm",
			Description = "The place where legends are born",
			RequiredLevel = 85,
			Waves = 10,
			BaseEnemyLevel = 85,
			PossibleSpecies = new() { "sunforged", "absolutezero", "stormtyrant", "primbloom", "eclipsara", "primeflare", "voidbloom", "aquagenesis" },
			Element = ElementType.Neutral,
			GoldReward = 1750,
			XPReward = 12500,
			HasBoss = true,
			BossSpeciesId = "mythweaver"
		} );

		// Level 100 - Ultimate challenge: Boss Gauntlet (3 boss fights in a row)
		_expeditions.Add( new Expedition
		{
			Id = "origin_void",
			Name = "The Origin Void",
			Description = "Face the primordial bosses in the ultimate gauntlet. No regular enemies - only boss battles.",
			RequiredLevel = 100,
			Waves = 3,
			BaseEnemyLevel = 100,
			PossibleSpecies = new(), // No regular species - boss gauntlet only
			Element = ElementType.Neutral,
			GoldReward = 5000,
			XPReward = 35000,
			HasBoss = true,
			BossSpeciesId = "genesis",
			BackgroundImage = "ui/locations/the_origin_void_background.png",
			IsBossGauntlet = true
		} );

		Log.Info( $"Generated {_expeditions.Count} expeditions" );
	}

	public Expedition GetExpedition( string id )
	{
		return _expeditions.FirstOrDefault( e => e.Id == id );
	}

	public bool CanStartExpedition( string expeditionId )
	{
		var expedition = GetExpedition( expeditionId );
		if ( expedition == null ) return false;

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return false;

		return tamer.Level >= expedition.RequiredLevel;
	}

	public void SelectTeam( List<Monster> team )
	{
		SelectedTeam = team.Take( 3 ).ToList();
	}

	public void StartExpedition( string expeditionId )
	{
		Log.Info( $"StartExpedition called: expeditionId={expeditionId}" );

		if ( SelectedTeam.Count == 0 )
		{
			Log.Warning( "No team selected for expedition!" );
			return;
		}

		// Clean up any stale battle state
		if ( BattleManager.Instance?.IsInBattle == true )
		{
			Log.Info( "Cleaning up stale battle state" );
			BattleManager.Instance.ExitBattle();
		}

		CurrentExpedition = GetExpedition( expeditionId );
		if ( CurrentExpedition == null )
		{
			Log.Warning( $"Unknown expedition: {expeditionId}" );
			return;
		}

		CurrentWave = 0;
		AccumulatedGold = 0;
		AccumulatedXP = 0;
		AccumulatedTokens = 0;
		AccumulatedItems = new();

		// Select boss for this expedition run
		SelectBossForExpedition();

		// Apply default settings from SettingsManager
		if ( SettingsManager.Instance != null )
		{
			var settings = SettingsManager.Instance.Settings;
			AutoBattle = settings.DefaultAutoBattle;
			AutoRetry = settings.DefaultAutoRetry;
			AutoNegotiate = settings.DefaultAutoContract;
			AutoNegotiateStrategy = settings.DefaultNegotiationStrategy;
			UseSpeciesFilter = settings.UseAutoContractSpeciesFilter;
			EnabledSpeciesFilter = SettingsManager.Instance.GetSpeciesFilterList();
		}

		// Reset the player's active monster index for a fresh expedition
		BattleManager.Instance?.ResetPlayerActiveIndex();

		OnExpeditionStarted?.Invoke( CurrentExpedition );

		// Start first wave
		StartNextWave();
	}

	/// <summary>
	/// Enable background expedition mode - battles continue without UI
	/// </summary>
	public void EnableBackgroundMode()
	{
		IsRunningInBackground = true;

		// Ensure we're subscribed to battle end events
		SubscribeToBattleManager();

		Log.Info( $"Expedition running in background mode. IsInBattle={BattleManager.Instance?.IsInBattle}, IsPlaying={BattleManager.Instance?.IsPlaying}, Subscribed={_subscribedToBattleManager}" );

		// Ensure playback is started when entering background mode
		if ( BattleManager.Instance?.IsInBattle == true && !BattleManager.Instance.IsPlaying )
		{
			BattleManager.Instance.PlaybackSpeed = 4.0f; // Fast in background
			BattleManager.Instance.StartPlayback();
			Log.Info( $"Background mode: Started playback immediately" );
		}
	}

	/// <summary>
	/// Disable background mode - UI is now visible
	/// </summary>
	public void DisableBackgroundMode()
	{
		IsRunningInBackground = false;
		Log.Info( "Expedition background mode disabled" );
	}

	/// <summary>
	/// Reset to wave 1 for auto-retry (keeps expedition and team, just resets wave)
	/// </summary>
	public void ResetToWaveOne()
	{
		Log.Info( $"ResetToWaveOne: Resetting from wave {CurrentWave} to 0" );
		CurrentWave = 0;
		AccumulatedGold = 0;
		AccumulatedXP = 0;
		AccumulatedTokens = 0;
		AccumulatedItems = new();
	}

	/// <summary>
	/// Retry the current expedition from wave 1, optionally awarding completion rewards first.
	/// Does NOT reset the player's active monster index.
	/// </summary>
	public void RetryExpedition( bool awardCompletionRewards )
	{
		if ( CurrentExpedition == null )
		{
			Log.Warning( "RetryExpedition: No current expedition to retry!" );
			return;
		}

		Log.Info( $"RetryExpedition: awardRewards={awardCompletionRewards}, CurrentExpedition={CurrentExpedition.Id}" );

		if ( awardCompletionRewards )
		{
			// Update highest cleared
			var tamer = TamerManager.Instance?.CurrentTamer;
			if ( tamer != null )
			{
				int expeditionIndex = _expeditions.IndexOf( CurrentExpedition );
				if ( expeditionIndex >= tamer.HighestExpeditionCleared )
				{
					tamer.HighestExpeditionCleared = expeditionIndex + 1;
				}
			}

			// Award expedition completion rewards (accumulated battle rewards already given per-wave)
			// Apply skill bonuses and hard mode multiplier to rewards
			float goldBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ExpeditionGoldBonus ) ?? 0;
			float xpBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ExpeditionXPBonus ) ?? 0;
			float hardModeMultiplier = GetRewardMultiplier();
			int finalGold = (int)(CurrentExpedition.GoldReward * (1 + goldBonus / 100f) * hardModeMultiplier);
			int finalXP = (int)(CurrentExpedition.XPReward * (1 + xpBonus / 100f) * hardModeMultiplier);
			TamerManager.Instance?.AddGold( finalGold );
			TamerManager.Instance?.AddXP( finalXP );
			Log.Info( $"RetryExpedition: Awarded expedition completion rewards: {finalGold} gold (+{goldBonus}%, x{hardModeMultiplier}), {finalXP} XP (+{xpBonus}%, x{hardModeMultiplier})" );

			// Award Boss Tokens if a boss was defeated
			if ( SelectedBoss != null )
			{
				AwardBossTokens();
			}
		}

		// Award accumulated item drops to inventory before resetting
		AwardAccumulatedItems();

		// Restore PP for all team monsters before starting fresh
		RestoreTeamPP();

		// Reset wave counter but keep the expedition active
		CurrentWave = 0;
		AccumulatedGold = 0;
		AccumulatedXP = 0;
		AccumulatedTokens = 0;
		AccumulatedItems = new();

		// Clear boss gauntlet order so it gets re-shuffled on retry
		GauntletBossOrder = null;

		// Re-select boss for the new run (repopulates GauntletBossOrder for gauntlets)
		SelectBossForExpedition();

		// Start wave 1
		StartNextWave();
	}

	/// <summary>
	/// Cancel the current expedition without rewards
	/// </summary>
	public void CancelExpedition()
	{
		Log.Info( "Cancelling expedition" );

		// Clean up battle state
		if ( BattleManager.Instance?.IsInBattle == true )
		{
			BattleManager.Instance.ExitBattle();
		}

		// Reset state
		CurrentExpedition = null;
		CurrentWave = 0;
		AccumulatedGold = 0;
		AccumulatedXP = 0;
		AccumulatedTokens = 0;
		AccumulatedItems = new();
		IsRunningInBackground = false;
		SelectedTeam.Clear();
	}

	/// <summary>
	/// Handle battle end - works both in background mode and when UI is handling it
	/// </summary>
	private void OnBattleEndBackground( BattleResult result )
	{
		Log.Info( $"[BG] OnBattleEndBackground CALLED! result={result != null}, PlayerWon={result?.PlayerWon}" );

		// Must have an active expedition to handle
		if ( CurrentExpedition == null )
		{
			Log.Info( $"OnBattleEndBackground skipped: No active expedition" );
			return;
		}

		// Only handle automatically if we're in background mode
		// When NOT in background mode, the ExpeditionPanel.OnBattleComplete handles progression
		if ( !IsRunningInBackground )
		{
			Log.Info( $"OnBattleEndBackground: Not in background mode, UI will handle. Wave={CurrentWave}/{CurrentExpedition.Waves}" );
			return;
		}

		Log.Info( $"[BG] Battle ended: PlayerWon={result?.PlayerWon}, Wave={CurrentWave}/{CurrentExpedition.Waves}, AutoRetry={AutoRetry}" );

		if ( result != null )
		{
			// Track accumulated rewards for UI display (actual awarding is done by BattleManager.DistributeRewards)
			AccumulatedGold += result.TotalGold;
			AccumulatedXP += result.TotalXP;
			Log.Info( $"[BG] Accumulated rewards: {result.TotalGold} gold, {result.TotalXP} XP (total: {AccumulatedGold} gold, {AccumulatedXP} XP)" );

			// Track accumulated item drops
			if ( result.ItemDrops?.Count > 0 )
			{
				AddAccumulatedItems( result.ItemDrops );
			}
		}

		bool playerWon = result?.PlayerWon ?? false;

		// Determine if we'll continue to another battle
		bool willContinue = (playerWon && CurrentWave < CurrentExpedition.Waves)
			|| (playerWon && CurrentWave >= CurrentExpedition.Waves && AutoRetry)
			|| (!playerWon && AutoRetry);

		// Use transition-friendly exit for continuous battles, otherwise full exit
		if ( BattleManager.Instance?.IsInBattle == true )
		{
			if ( willContinue )
			{
				BattleManager.Instance.PrepareForNextWave();
				BattleManager.Instance.ExitBattleForTransition();
			}
			else
			{
				BattleManager.Instance.ExitBattle();
			}
		}

		if ( playerWon && CurrentWave < CurrentExpedition.Waves )
		{
			Log.Info( $"[BG] Player won wave {CurrentWave}, continuing to next wave" );
			OnWaveCompleted?.Invoke( CurrentWave, CurrentExpedition.Waves );
			_ = StartNextWaveDelayed();
		}
		else if ( playerWon && CurrentWave >= CurrentExpedition.Waves )
		{
			Log.Info( $"[BG] Player completed all waves! AutoRetry={AutoRetry}" );

			// Give expedition completion rewards (accumulated battle rewards already given per-wave)
			// Apply skill bonuses and hard mode multiplier to rewards
			float goldBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ExpeditionGoldBonus ) ?? 0;
			float xpBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ExpeditionXPBonus ) ?? 0;
			float hardModeMultiplier = GetRewardMultiplier();
			int finalGold = (int)(CurrentExpedition.GoldReward * (1 + goldBonus / 100f) * hardModeMultiplier);
			int finalXP = (int)(CurrentExpedition.XPReward * (1 + xpBonus / 100f) * hardModeMultiplier);
			TamerManager.Instance?.AddGold( finalGold );
			TamerManager.Instance?.AddXP( finalXP );
			Log.Info( $"[BG] Awarded expedition completion rewards: {finalGold} gold (+{goldBonus}%, x{hardModeMultiplier}), {finalXP} XP (+{xpBonus}%, x{hardModeMultiplier})" );

			// Update highest cleared
			var tamer = TamerManager.Instance?.CurrentTamer;
			if ( tamer != null )
			{
				int expeditionIndex = _expeditions.IndexOf( CurrentExpedition );
				if ( expeditionIndex >= tamer.HighestExpeditionCleared )
				{
					tamer.HighestExpeditionCleared = expeditionIndex + 1;
				}
			}

			if ( AutoRetry )
			{
				// Award Boss Tokens before auto-retry resets (otherwise they're lost!)
				if ( SelectedBoss != null )
				{
					AwardBossTokens();
				}

				// Award accumulated items before auto-retry resets them
				AwardAccumulatedItems();

				// Auto-retry: reset to wave 1 and continue
				Log.Info( $"[BG] Auto-retry enabled, restarting expedition from wave 1" );
				AccumulatedGold = 0;
				AccumulatedXP = 0;
				AccumulatedItems = new();
				CurrentWave = 0;
				GauntletBossOrder = null;
				SelectBossForExpedition(); // Re-shuffle bosses for gauntlets
				_ = StartNextWaveDelayed();
			}
			else
			{
				// No auto-retry: complete and exit
				CompleteExpedition( true );
			}
		}
		else if ( !playerWon && AutoRetry )
		{
			Log.Info( $"[BG] Player lost, auto-retry enabled. Resetting to wave 0." );
			// Award any accumulated items before reset
			AwardAccumulatedItems();
			AccumulatedGold = 0;
			AccumulatedXP = 0;
			AccumulatedItems = new();
			CurrentWave = 0;
			GauntletBossOrder = null;
			SelectBossForExpedition(); // Re-shuffle bosses for gauntlets
			_ = StartNextWaveDelayed();
		}
		else
		{
			Log.Info( $"[BG] Player lost, no auto-retry. Completing expedition as failed." );
			CompleteExpedition( false );
		}
	}

	/// <summary>
	/// Start next wave with a small delay to ensure clean battle state
	/// </summary>
	private async Task StartNextWaveDelayed()
	{
		Log.Info( $"[BG] StartNextWaveDelayed: Waiting 50ms before starting next wave. IsBackground={IsRunningInBackground}, CurrentExpedition={CurrentExpedition?.Id}" );

		// Wait a frame to ensure battle cleanup is complete
		await Task.Delay( 50 );

		Log.Info( $"[BG] StartNextWaveDelayed: After delay. IsBackground={IsRunningInBackground}, CurrentExpedition={CurrentExpedition?.Id}" );

		if ( !IsRunningInBackground || CurrentExpedition == null )
		{
			Log.Info( "[BG] StartNextWaveDelayed: No longer in background mode, aborting" );
			return;
		}

		StartNextWave();
		Log.Info( $"[BG] StartNextWaveDelayed: Started wave {CurrentWave}, IsInBattle={BattleManager.Instance?.IsInBattle}" );
	}

	/// <summary>
	/// Select a boss for the current expedition run
	/// </summary>
	private void SelectBossForExpedition()
	{
		SelectedBoss = null;
		CurrentBossState = null;
		IsRareBossEncounter = false;
		GauntletBossOrder = null;

		if ( CurrentExpedition == null ) return;

		var pool = BossPoolDatabase.GetPool( CurrentExpedition.Id );
		if ( pool == null || pool.Bosses.Count == 0 )
		{
			Log.Info( $"No boss pool found for expedition {CurrentExpedition.Id}" );
			return;
		}

		var random = _sharedRandom;

		// For boss gauntlets, use weighted selection to make mythic/legendary bosses rarer
		if ( CurrentExpedition.IsBossGauntlet )
		{
			GauntletBossOrder = SelectWeightedBosses( pool.Bosses, CurrentExpedition.Waves, random );
			Log.Info( $"[Gauntlet] Selected {GauntletBossOrder.Count} bosses: {string.Join( ", ", GauntletBossOrder.Select( b => $"{b.SpeciesId}({b.Tier})" ) )}" );
			return;
		}

		// Check for rare boss spawn (level 50+ only)
		if ( pool.RareBossChance > 0 && pool.RareBosses?.Count > 0 )
		{
			// Apply Lucky Charm bonus to rare boss chance
			float luckyBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.RareEncounterChance ) ?? 0;
			float rareBossChance = pool.RareBossChance * (1 + luckyBonus / 100f);

			if ( random.NextDouble() < rareBossChance )
			{
				// Rare boss spawns!
				SelectedBoss = pool.RareBosses[random.Next( pool.RareBosses.Count )];
				IsRareBossEncounter = true;
				Log.Info( $"RARE BOSS ENCOUNTER! Selected {SelectedBoss.SpeciesId} (Tier: {SelectedBoss.Tier})" );
			}
		}

		// If no rare boss, select from normal pool using weighted selection
		// Higher tier bosses are much rarer to encounter
		if ( SelectedBoss == null )
		{
			SelectedBoss = SelectWeightedBoss( pool.Bosses, random );
			Log.Info( $"Selected boss {SelectedBoss.SpeciesId} (Tier: {SelectedBoss.Tier}) for expedition {CurrentExpedition.Id}" );
		}

		// Initialize boss state for phase tracking
		CurrentBossState = new ActiveBossState
		{
			BossData = SelectedBoss,
			IsRareBoss = IsRareBossEncounter
		};
	}

	/// <summary>
	/// Boss tier weights for weighted selection (non-Mythic tiers only).
	/// Mythic bosses are handled separately with a flat 1% chance.
	/// </summary>
	private static int GetBossTierWeight( BossTier tier ) => tier switch
	{
		BossTier.Normal => 10,
		BossTier.Elite => 6,
		BossTier.Legendary => 2,
		_ => 1
	};

	private const float MYTHIC_BOSS_CHANCE = 0.01f; // 1% chance for a Mythic boss

	/// <summary>
	/// Select a single boss from the pool. Mythic bosses have a flat 1% chance.
	/// If Mythic doesn't proc, picks from non-Mythic bosses using weighted selection.
	/// </summary>
	private BossData SelectWeightedBoss( List<BossData> pool, Random random )
	{
		var mythicBosses = pool.Where( b => b.Tier == BossTier.Mythic ).ToList();
		var nonMythicBosses = pool.Where( b => b.Tier != BossTier.Mythic ).ToList();

		// 1% chance: roll for a Mythic boss
		if ( mythicBosses.Count > 0 && random.NextDouble() < MYTHIC_BOSS_CHANCE )
		{
			return mythicBosses[random.Next( mythicBosses.Count )];
		}

		// 99%: pick from non-Mythic bosses using weighted selection
		var pickPool = nonMythicBosses.Count > 0 ? nonMythicBosses : pool;
		int totalWeight = pickPool.Sum( b => GetBossTierWeight( b.Tier ) );
		int roll = random.Next( totalWeight );

		int cumulative = 0;
		foreach ( var boss in pickPool )
		{
			cumulative += GetBossTierWeight( boss.Tier );
			if ( roll < cumulative )
				return boss;
		}

		return pickPool[random.Next( pickPool.Count )];
	}

	/// <summary>
	/// Select bosses for gauntlet. Mythic bosses have a flat 1% chance per slot.
	/// Non-Mythic bosses use weighted random selection.
	/// </summary>
	private List<BossData> SelectWeightedBosses( List<BossData> pool, int count, Random random )
	{
		var result = new List<BossData>();
		var available = pool.ToList();

		while ( result.Count < count )
		{
			if ( available.Count == 0 )
			{
				available = pool.ToList();
			}

			var mythicBosses = available.Where( b => b.Tier == BossTier.Mythic ).ToList();
			var nonMythicBosses = available.Where( b => b.Tier != BossTier.Mythic ).ToList();

			BossData selected = null;

			// 1% chance for a Mythic boss per slot
			if ( mythicBosses.Count > 0 && random.NextDouble() < MYTHIC_BOSS_CHANCE )
			{
				selected = mythicBosses[random.Next( mythicBosses.Count )];
			}
			else
			{
				// Pick from non-Mythic using weighted selection
				var pickPool = nonMythicBosses.Count > 0 ? nonMythicBosses : available;
				int totalWeight = pickPool.Sum( b => GetBossTierWeight( b.Tier ) );
				int roll = random.Next( totalWeight );

				int cumulative = 0;
				foreach ( var boss in pickPool )
				{
					cumulative += GetBossTierWeight( boss.Tier );
					if ( roll < cumulative )
					{
						selected = boss;
						break;
					}
				}
			}

			if ( selected != null )
			{
				result.Add( selected );
				available.Remove( selected );
			}
		}

		return result;
	}

	public void StartNextWave()
	{
		if ( CurrentExpedition == null )
		{
			Log.Warning( "StartNextWave: CurrentExpedition is null!" );
			return;
		}

		CurrentWave++;
		bool isBossWave = CurrentWave == CurrentExpedition.Waves;
		Log.Info( $"StartNextWave: Wave {CurrentWave}/{CurrentExpedition.Waves}, IsBoss={isBossWave}, SelectedTeam count: {SelectedTeam?.Count ?? 0}" );

		// Generate enemies for this wave
		var enemies = GenerateWaveEnemies();
		Log.Info( $"StartNextWave: Generated {enemies.Count} enemies" );

		if ( enemies.Count == 0 )
		{
			Log.Warning( "StartNextWave: No enemies generated!" );
		}

		// Start battle
		if ( BattleManager.Instance == null )
		{
			Log.Warning( "StartNextWave: BattleManager.Instance is null!" );
			return;
		}

		// Pass boss state to battle manager for phase tracking
		// For gauntlets, every wave is a boss wave; otherwise just the final wave
		bool shouldShowBoss = (CurrentExpedition.IsBossGauntlet || isBossWave) && CurrentBossState != null;
		if ( shouldShowBoss )
		{
			BattleManager.Instance.SetBossState( CurrentBossState );
		}
		else
		{
			BattleManager.Instance.SetBossState( null );
		}

		// Use manual battle mode when auto-battle is off (horde system with target selection)
		if ( !AutoBattle )
		{
			BattleManager.Instance.StartManualBattle( SelectedTeam, enemies );
		}
		else
		{
			BattleManager.Instance.StartBattle( SelectedTeam, enemies );
		}
	}

	private List<Monster> GenerateWaveEnemies()
	{
		var enemies = new List<Monster>();
		var random = _sharedRandom;

		// Determine number of enemies (1-3 based on wave)
		int enemyCount = Math.Min( 3, 1 + (CurrentWave / 2) );

		// Check if this is a boss gauntlet (every wave is a boss)
		if ( CurrentExpedition.IsBossGauntlet && GauntletBossOrder != null )
		{
			int bossIndex = CurrentWave - 1;
			if ( bossIndex < GauntletBossOrder.Count )
			{
				var waveBoss = GauntletBossOrder[bossIndex];

				// Boss level increases with each wave (hard mode adds bonus levels)
				int bossLevel = GetEffectiveEnemyLevel( CurrentExpedition.BaseEnemyLevel ) + (CurrentWave * 2);
				var boss = CreateBossMonster( waveBoss, bossLevel );
				if ( boss != null )
				{
					enemies.Add( boss );
					Log.Info( $"[Gauntlet] Wave {CurrentWave}: Spawned boss {boss.Nickname} Lv.{boss.Level}" );
				}

				// Update boss state for this wave's boss
				CurrentBossState = new ActiveBossState
				{
					BossData = waveBoss,
					IsRareBoss = false
				};
				SelectedBoss = waveBoss;
			}
			return enemies;
		}

		// Check if this is the boss wave (final wave)
		bool isBossWave = CurrentWave == CurrentExpedition.Waves;

		if ( isBossWave && SelectedBoss != null )
		{
			// Boss wave - spawn ONLY the boss (no minions for epic 3v1 feel)
			int bossLevel = GetEffectiveEnemyLevel( CurrentExpedition.BaseEnemyLevel ) + 5;
			var boss = CreateBossMonster( SelectedBoss, bossLevel );
			if ( boss != null )
			{
				enemies.Add( boss );
				Log.Info( $"Spawned boss {boss.Nickname} Lv.{boss.Level} (HP={boss.MaxHP}, ATK={boss.ATK}, DEF={boss.DEF})" );
			}
		}
		else
		{
			// Normal wave - spawn regular enemies (excluding ALL boss species from the pool)
			// Get all boss species IDs from the boss pool for this expedition
			var bossPool = BossPoolDatabase.GetPool( CurrentExpedition.Id );
			var allBossSpeciesIds = new HashSet<string>();

			if ( bossPool != null )
			{
				// Add all regular boss species
				foreach ( var boss in bossPool.Bosses )
					allBossSpeciesIds.Add( boss.SpeciesId );

				// Add all rare boss species too
				if ( bossPool.RareBosses != null )
				{
					foreach ( var rareBoss in bossPool.RareBosses )
						allBossSpeciesIds.Add( rareBoss.SpeciesId );
				}
			}

			// Filter out ALL boss species so they only appear on boss waves
			var availableSpecies = CurrentExpedition.PossibleSpecies
				.Where( s => !allBossSpeciesIds.Contains( s ) )
				.ToList();

			// Fallback if all species were bosses (shouldn't happen, but safety check)
			if ( availableSpecies.Count == 0 )
				availableSpecies = CurrentExpedition.PossibleSpecies;

			for ( int i = 0; i < enemyCount; i++ )
			{
				// Pick random species from filtered pool
				var speciesId = availableSpecies[random.Next( availableSpecies.Count )];

				// Level scales with wave (hard mode adds bonus levels)
				int level = GetEffectiveEnemyLevel( CurrentExpedition.BaseEnemyLevel ) + (CurrentWave - 1) * 2 + random.Next( -1, 2 );
				level = Math.Max( 1, level );

				var enemy = CreateEnemyMonster( speciesId, level );
				if ( enemy != null ) enemies.Add( enemy );
			}
		}

		return enemies;
	}

	/// <summary>
	/// Create a boss monster with tier-based stat multipliers
	/// </summary>
	private Monster CreateBossMonster( BossData bossData, int level )
	{
		var species = MonsterManager.Instance?.GetSpecies( bossData.SpeciesId );
		if ( species == null )
		{
			Log.Warning( $"CreateBossMonster: Species '{bossData.SpeciesId}' not found!" );
			return null;
		}

		// Mark species as "seen" in the beastiary
		BeastiaryManager.Instance?.SeeSpecies( bossData.SpeciesId );

		// Generate stronger genetics for bosses (15-25 range)
		var random = _sharedRandom;
		var genetics = new Genetics
		{
			HPGene = random.Next( 15, 26 ),
			ATKGene = random.Next( 15, 26 ),
			DEFGene = random.Next( 15, 26 ),
			SPDGene = random.Next( 15, 26 ),
			Nature = (NatureType)random.Next( 0, 5 )
		};

		var boss = new Monster
		{
			SpeciesId = bossData.SpeciesId,
			Nickname = species.Name,
			Level = level,
			Genetics = genetics,
			IsBoss = true
		};

		MonsterManager.Instance?.RecalculateStats( boss );

		// Apply tier multipliers
		var (hpMult, atkMult, defMult) = bossData.GetTierMultipliers();
		boss.MaxHP = (int)(boss.MaxHP * hpMult);
		boss.ATK = (int)(boss.ATK * atkMult);
		boss.DEF = (int)(boss.DEF * defMult);

		boss.FullHeal();

		Log.Info( $"CreateBossMonster: Created {boss.Nickname} Lv.{level} with multipliers HP={hpMult}x ATK={atkMult}x DEF={defMult}x" );

		return boss;
	}

	private Monster CreateEnemyMonster( string speciesId, int level )
	{
		var species = MonsterManager.Instance?.GetSpecies( speciesId );
		if ( species == null )
		{
			Log.Warning( $"CreateEnemyMonster: Species '{speciesId}' not found!" );
			return null;
		}

		// Mark species as "seen" in the beastiary (shows image + name only)
		BeastiaryManager.Instance?.SeeSpecies( speciesId );

		// Generate weak genetics for enemies (0-10 range)
		// This makes early game more forgiving
		var random = _sharedRandom;
		var genetics = new Genetics
		{
			HPGene = random.Next( 0, 11 ),
			ATKGene = random.Next( 0, 11 ),
			DEFGene = random.Next( 0, 11 ),
			SPDGene = random.Next( 0, 11 ),
			Nature = (NatureType)random.Next( 0, 5 )
		};

		var enemy = new Monster
		{
			SpeciesId = speciesId,
			Nickname = species.Name,
			Level = level,
			Genetics = genetics
		};

		MonsterManager.Instance?.RecalculateStats( enemy );
		enemy.FullHeal();

		Log.Info( $"CreateEnemyMonster: Created {enemy.Nickname} Lv.{level} with HP={enemy.MaxHP}, ATK={enemy.ATK}" );

		return enemy;
	}

	public void OnWaveComplete( bool playerWon )
	{
		if ( !playerWon )
		{
			// Expedition failed
			CompleteExpedition( false );
			return;
		}

		if ( CurrentWave >= CurrentExpedition.Waves )
		{
			// All waves cleared!
			CompleteExpedition( true );
		}
		else
		{
			// Offer to catch or continue
			// (This would be handled by UI)
		}
	}

	private void CompleteExpedition( bool success )
	{
		Log.Info( $"CompleteExpedition: success={success}, Wave={CurrentWave}, IsBackground={IsRunningInBackground}" );

		if ( success )
		{
			// Update highest cleared
			var tamer = TamerManager.Instance?.CurrentTamer;
			if ( tamer != null )
			{
				int expeditionIndex = _expeditions.IndexOf( CurrentExpedition );
				if ( expeditionIndex >= tamer.HighestExpeditionCleared )
				{
					tamer.HighestExpeditionCleared = expeditionIndex + 1;
				}
			}

			// Award expedition completion rewards (accumulated battle rewards already given per-wave)
			// Apply skill bonuses and hard mode multiplier to rewards
			float goldBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ExpeditionGoldBonus ) ?? 0;
			float xpBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ExpeditionXPBonus ) ?? 0;
			float hardModeMultiplier = GetRewardMultiplier();
			int finalGold = (int)(CurrentExpedition.GoldReward * (1 + goldBonus / 100f) * hardModeMultiplier);
			int finalXP = (int)(CurrentExpedition.XPReward * (1 + xpBonus / 100f) * hardModeMultiplier);
			TamerManager.Instance?.AddGold( finalGold );
			TamerManager.Instance?.AddXP( finalXP );
			Log.Info( $"CompleteExpedition: Awarded expedition completion rewards: {finalGold} gold (+{goldBonus}%, x{hardModeMultiplier}), {finalXP} XP (+{xpBonus}%, x{hardModeMultiplier})" );

			// Track expedition completions for veteran stats
			foreach ( var monster in SelectedTeam )
			{
				if ( monster == null ) continue;
				var ownedMonster = MonsterManager.Instance?.GetMonster( monster.Id );
				if ( ownedMonster != null )
				{
					ownedMonster.ExpeditionsCompleted++;
					ownedMonster.AddJournalEntry(
						$"Completed {CurrentExpedition.Name} expedition!",
						Data.JournalEntryType.Expedition,
						zoneId: CurrentExpedition.Id
					);
				}
			}

			// Award Boss Tokens if a boss was defeated
			if ( SelectedBoss != null )
			{
				AwardBossTokens();
			}
		}

		// Award accumulated item drops to inventory
		AwardAccumulatedItems();

		// Restore PP for all team monsters after expedition
		RestoreTeamPP();

		// Update Lazy demand progress for monsters that rested (weren't in expedition)
		UpdateRestingMonstersContracts();

		OnExpeditionComplete?.Invoke( success );
		CurrentExpedition = null;
		CurrentWave = 0;
		IsRunningInBackground = false;

		Log.Info( $"CompleteExpedition done: CurrentExpedition={CurrentExpedition == null}, CurrentWave={CurrentWave}, IsBackground={IsRunningInBackground}" );
	}

	/// <summary>
	/// Last amount of boss tokens awarded (for UI display)
	/// </summary>
	public int LastAwardedBossTokens { get; private set; } = 0;

	/// <summary>
	/// Award Boss Tokens for defeating the expedition boss
	/// </summary>
	private void AwardBossTokens()
	{
		if ( SelectedBoss == null || CurrentExpedition == null )
			return;

		int baseTokens = SelectedBoss.BaseTokenReward;
		int totalTokens = baseTokens;

		// First-time clear bonus
		var clearedBosses = TamerManager.Instance?.CurrentTamer?.ClearedBosses ?? new List<string>();
		bool isFirstClear = !TamerManager.Instance.HasClearedBoss( CurrentExpedition.Id );
		Log.Info( $"[BossTokens] Checking first clear for '{CurrentExpedition.Id}': isFirstClear={isFirstClear}, ClearedBosses=[{string.Join( ", ", clearedBosses )}]" );

		if ( isFirstClear )
		{
			totalTokens += SelectedBoss.FirstClearBonus;
			TamerManager.Instance.MarkBossCleared( CurrentExpedition.Id );
			Log.Info( $"First-time boss clear bonus: +{SelectedBoss.FirstClearBonus} tokens for expedition '{CurrentExpedition.Id}'" );
		}

		// Rare boss multiplier (3x tokens)
		if ( IsRareBossEncounter )
		{
			totalTokens *= 3;
			Log.Info( $"Rare boss multiplier: 3x tokens" );
		}

		// Token Collector skill bonus
		float tokenBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.BossTokenBonus ) ?? 0;
		if ( tokenBonus > 0 )
		{
			int bonusTokens = (int)(totalTokens * tokenBonus / 100f);
			totalTokens += bonusTokens;
			Log.Info( $"Token Collector bonus: +{bonusTokens} tokens (+{tokenBonus}%)" );
		}

		// Lucky Charm bonus (applies to tokens too)
		float luckyBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.RareEncounterChance ) ?? 0;
		if ( luckyBonus > 0 )
		{
			int bonusTokens = (int)(totalTokens * luckyBonus / 100f);
			totalTokens += bonusTokens;
			Log.Info( $"Lucky Charm bonus: +{bonusTokens} tokens" );
		}

		TamerManager.Instance?.AddBossTokens( totalTokens );
		LastAwardedBossTokens = totalTokens;
		AccumulatedTokens += totalTokens;
		Log.Info( $"Awarded {totalTokens} Boss Tokens (base={baseTokens}, firstClear={isFirstClear}, rare={IsRareBossEncounter})" );
	}

	/// <summary>
	/// Updates contract satisfaction for monsters with Lazy demands that weren't in the expedition
	/// </summary>
	private void UpdateRestingMonstersContracts()
	{
		var allMonsters = MonsterManager.Instance?.OwnedMonsters;
		if ( allMonsters == null ) return;

		var expeditionTeamIds = SelectedTeam?.Select( m => m.Id ).ToHashSet() ?? new HashSet<Guid>();

		foreach ( var monster in allMonsters )
		{
			// Skip monsters that were in the expedition
			if ( expeditionTeamIds.Contains( monster.Id ) ) continue;

			// Skip monsters without contracts
			if ( monster.Contract == null ) continue;

			// Check for Lazy demands and update progress
			foreach ( var demand in monster.Contract.SecondaryDemands.Prepend( monster.Contract.PrimaryDemand ) )
			{
				if ( demand.Type == ContractDemandType.Lazy )
				{
					demand.CurrentProgress++;

					// Check if demand was satisfied
					if ( demand.CurrentProgress >= demand.RequiredAmount )
					{
						int satisfactionGain = demand == monster.Contract.PrimaryDemand ? 10 : 5;
						monster.Contract.UpdateSatisfaction( satisfactionGain );
						demand.CurrentProgress = 0; // Reset for next cycle
					}
				}
			}
		}

		// Save monsters after updating contracts
		MonsterManager.Instance?.SaveMonsters();
	}

	/// <summary>
	/// Restore PP for all monsters in the selected team
	/// Called when completing or retrying an expedition
	/// </summary>
	private void RestoreTeamPP()
	{
		if ( SelectedTeam == null || SelectedTeam.Count == 0 )
			return;

		foreach ( var monster in SelectedTeam )
		{
			if ( monster == null )
				continue;

			monster.RestoreAllPP( MoveDatabase.GetMove );
		}

		Log.Info( $"Restored PP for {SelectedTeam.Count} team monsters" );
	}

	/// <summary>
	/// Store catchable enemies from the last completed battle
	/// Called after a wave victory to enable negotiation
	/// </summary>
	public void StoreCatchableEnemies( List<Monster> enemies )
	{
		CatchableEnemies.Clear();
		if ( enemies == null ) return;

		foreach ( var enemy in enemies )
		{
			if ( enemy == null ) continue;

			var species = MonsterManager.Instance?.GetSpecies( enemy.SpeciesId );
			if ( species != null && species.IsCatchable )
			{
				CatchableEnemies.Add( enemy );
			}
		}

		Log.Info( $"Stored {CatchableEnemies.Count} catchable enemies for negotiation" );
	}

	/// <summary>
	/// Clear catchable enemies (after negotiation or skipping)
	/// </summary>
	public void ClearCatchableEnemies()
	{
		CatchableEnemies.Clear();
	}

	/// <summary>
	/// Get a random catchable enemy for negotiation
	/// </summary>
	public Monster GetRandomCatchableEnemy()
	{
		if ( CatchableEnemies.Count == 0 ) return null;
		var random = _sharedRandom;
		return CatchableEnemies[random.Next( CatchableEnemies.Count )];
	}

	/// <summary>
	/// Attempt to catch a wild monster during expedition
	/// </summary>
	public bool TryCatchMonster( Monster target )
	{
		var species = MonsterManager.Instance?.GetSpecies( target.SpeciesId );
		if ( species == null || !species.IsCatchable ) return false;

		// Check for ink save chance from relics
		bool inkSaved = false;
		float inkSaveChance = ItemManager.Instance?.GetRelicBonus( ItemEffectType.PassiveInkSaver ) ?? 0;
		if ( inkSaveChance > 0 && _sharedRandom.NextDouble() < (inkSaveChance / 100f) )
		{
			inkSaved = true;
			Log.Info( "Relic saved contract ink!" );
		}

		// Check if we have contract ink (skip spend if ink was saved)
		if ( !inkSaved && !TamerManager.Instance.SpendContractInk() )
		{
			Log.Warning( "No contract ink!" );
			return false;
		}

		// Calculate catch chance
		float baseCatchRate = species.BaseCatchRate;

		// HP modifier (lower HP = higher catch rate)
		float hpPercent = (float)target.CurrentHP / target.MaxHP;
		float hpModifier = 1.0f + (1.0f - hpPercent) * 0.5f;

		// Skill bonus
		float catchBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.CatchRateBonus ) ?? 0;

		// Relic catch rate bonus
		float relicCatchBonus = ItemManager.Instance?.GetRelicBonus( ItemEffectType.PassiveCatchRate ) ?? 0;

		float finalCatchRate = baseCatchRate * hpModifier * (1 + catchBonus / 100f) * (1 + relicCatchBonus / 100f);
		finalCatchRate = Math.Min( 0.95f, finalCatchRate ); // Max 95% catch rate

		// Master Ink guarantees capture
		bool hasMasterInk = TamerManager.Instance?.CurrentTamer?.HasMasterInk == true;
		if ( hasMasterInk )
		{
			finalCatchRate = 1.0f;
			TamerManager.Instance.CurrentTamer.HasMasterInk = false;
			TamerManager.Instance.SaveToCloud();
			Log.Info( "Master Ink used! Guaranteed capture." );
		}

		var random = _sharedRandom;
		bool caught = random.NextDouble() < finalCatchRate;

		if ( caught )
		{
			// Create caught monster
			var caughtMonster = MonsterManager.Instance?.CreateMonster( target.SpeciesId, isBred: false, target.Genetics );
			if ( caughtMonster != null )
			{
				// Set level to match the wild monster's level
				caughtMonster.Level = target.Level;
				MonsterManager.Instance?.RecalculateStats( caughtMonster );

				// Refresh moves to match the correct level (CreateMonster uses level 1)
				MonsterManager.Instance?.RefreshMovesForLevel( caughtMonster );

				// Full heal after stat recalculation
				caughtMonster.FullHeal();

				OnMonsterCaught?.Invoke( caughtMonster );
				Log.Info( $"Caught {caughtMonster.Nickname} at level {caughtMonster.Level}!" );
			}
		}

		return caught;
	}
}

/// <summary>
/// Expedition data structure
/// </summary>
public class Expedition
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public int RequiredLevel { get; set; }
	public int Waves { get; set; }
	public int BaseEnemyLevel { get; set; }
	public List<string> PossibleSpecies { get; set; } = new();
	public ElementType Element { get; set; }
	public int GoldReward { get; set; }
	public int XPReward { get; set; }
	public bool HasBoss { get; set; }
	public string BossSpeciesId { get; set; }
	public string BackgroundImage { get; set; }
	public bool IsBossGauntlet { get; set; } // Every wave is a boss fight
}
