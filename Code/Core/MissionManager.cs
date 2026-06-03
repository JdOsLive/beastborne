using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sandbox;
using Sandbox.Services;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// Manages daily, weekly, and monthly missions with progress tracking,
/// reward claiming, and automatic refresh on schedule.
/// </summary>
public sealed class MissionManager : Component
{
	public static MissionManager Instance { get; private set; }

	private bool _hasHydrated;

	// ═══════════════════════════════════════════════════════════════
	// ACTIVE MISSIONS STATE
	// ═══════════════════════════════════════════════════════════════

	public List<MissionState> ActiveDailyMissions { get; private set; } = new();
	public List<MissionState> ActiveWeeklyMissions { get; private set; } = new();
	public MissionState ActiveMonthlyChallenge { get; private set; }

	public DateTime DailyRefreshDate { get; private set; }
	public DateTime WeeklyRefreshDate { get; private set; }
	public DateTime MonthlyRefreshDate { get; private set; }

	public bool DailyBonusClaimed { get; private set; }
	public bool WeeklyBonusClaimed { get; private set; }

	// Events
	public Action OnMissionsUpdated;
	public Action<string> OnMissionCompleted; // mission ID
	public Action<string, int, int> OnMissionProgress; // mission ID, current, target

	// ═══════════════════════════════════════════════════════════════
	// MISSION POOL — All possible missions by tier
	// ═══════════════════════════════════════════════════════════════

	private static readonly List<MissionDefinition> DailyPool = new()
	{
		// ── BATTLE ──────────────────────────────────────────────
		new() { Id = "win_battles", Name = "Win Battles", Description = "Win 3 battles", Category = MissionCategory.Battle, Tier = MissionTier.Daily, Target = 3, GoldReward = 5000, XPReward = 1000, Icon = "ui/icons/missions/battle.svg" },
		new() { Id = "battle_master", Name = "Battle Master", Description = "Win 5 battles without losing a monster", Category = MissionCategory.Battle, Tier = MissionTier.Daily, Target = 5, GoldReward = 8000, XPReward = 2000, GemReward = 1, Icon = "ui/icons/missions/battle.svg" },
		new() { Id = "type_hunter", Name = "Type Hunter", Description = "Win a battle using type advantage", Category = MissionCategory.Battle, Tier = MissionTier.Daily, Target = 1, GoldReward = 4000, XPReward = 1000, Icon = "ui/icons/missions/battle.svg" },
		new() { Id = "knockout_king", Name = "Knockout King", Description = "Score 10 knockouts", Category = MissionCategory.Battle, Tier = MissionTier.Daily, Target = 10, GoldReward = 6000, XPReward = 1500, Icon = "ui/icons/missions/battle.svg" },
		new() { Id = "damage_dealer", Name = "Damage Dealer", Description = "Deal 50,000 total damage", Category = MissionCategory.Battle, Tier = MissionTier.Daily, Target = 50000, GoldReward = 5000, XPReward = 1500, Icon = "ui/icons/missions/battle.svg" },

		// ── EXPEDITION ──────────────────────────────────────────
		new() { Id = "explorer", Name = "Explorer", Description = "Complete 2 expeditions", Category = MissionCategory.Expedition, Tier = MissionTier.Daily, Target = 2, GoldReward = 5000, XPReward = 1000, Icon = "ui/icons/missions/expedition.svg" },
		new() { Id = "boss_hunter", Name = "Boss Hunter", Description = "Defeat 1 boss", Category = MissionCategory.Expedition, Tier = MissionTier.Daily, Target = 1, GoldReward = 8000, InkReward = 10, Icon = "ui/icons/missions/expedition.svg" },
		new() { Id = "horde_slayer", Name = "Horde Slayer", Description = "Clear 10 waves", Category = MissionCategory.Expedition, Tier = MissionTier.Daily, Target = 10, GoldReward = 5000, XPReward = 1000, Icon = "ui/icons/missions/expedition.svg" },
		new() { Id = "hard_mode", Name = "Hard Mode Hero", Description = "Complete 1 Hard Mode expedition", Category = MissionCategory.Expedition, Tier = MissionTier.Daily, Target = 1, GoldReward = 12000, XPReward = 3000, GemReward = 2, Icon = "ui/icons/missions/expedition.svg" },
		new() { Id = "gold_rush", Name = "Gold Rush", Description = "Earn 5,000 gold from expeditions", Category = MissionCategory.Expedition, Tier = MissionTier.Daily, Target = 5000, GoldReward = 3000, XPReward = 1500, Icon = "ui/icons/missions/expedition.svg" },

		// ── COLLECTION ──────────────────────────────────────────
		new() { Id = "contractor", Name = "Contractor", Description = "Contract 3 monsters", Category = MissionCategory.Collection, Tier = MissionTier.Daily, Target = 3, GoldReward = 5000, InkReward = 15, Icon = "ui/icons/missions/collection.svg" },
		new() { Id = "element_seeker", Name = "Element Seeker", Description = "Contract an elemental monster", Category = MissionCategory.Collection, Tier = MissionTier.Daily, Target = 1, GoldReward = 6000, InkReward = 10, Icon = "ui/icons/missions/collection.svg" },
		new() { Id = "rare_finder", Name = "Rare Finder", Description = "Contract a Rare or higher monster", Category = MissionCategory.Collection, Tier = MissionTier.Daily, Target = 1, GoldReward = 8000, GemReward = 3, Icon = "ui/icons/missions/collection.svg" },
		new() { Id = "new_discovery", Name = "New Discovery", Description = "Discover a new Beastiary entry", Category = MissionCategory.Collection, Tier = MissionTier.Daily, Target = 1, GoldReward = 8000, XPReward = 2000, Icon = "ui/icons/missions/collection.svg" },
		new() { Id = "fusion_scientist", Name = "Fusion Scientist", Description = "Fuse 1 monster", Category = MissionCategory.Collection, Tier = MissionTier.Daily, Target = 1, GoldReward = 6000, GemReward = 2, Icon = "ui/icons/missions/collection.svg" },

		// ── ECONOMY ──────────────────────────────────────────────
		new() { Id = "shopper", Name = "Shopper", Description = "Buy 3 items from the shop", Category = MissionCategory.Economy, Tier = MissionTier.Daily, Target = 3, GoldReward = 4000, XPReward = 500, Icon = "ui/icons/missions/economy.svg" },
		new() { Id = "big_spender", Name = "Big Spender", Description = "Spend 10,000 gold", Category = MissionCategory.Economy, Tier = MissionTier.Daily, Target = 10000, GoldReward = 5000, XPReward = 1000, Icon = "ui/icons/missions/economy.svg" },
		new() { Id = "item_user", Name = "Item User", Description = "Use 3 consumable items", Category = MissionCategory.Economy, Tier = MissionTier.Daily, Target = 3, GoldReward = 5000, XPReward = 1000, Icon = "ui/icons/missions/economy.svg" },
		new() { Id = "relic_master", Name = "Relic Master", Description = "Equip 3 relics simultaneously", Category = MissionCategory.Economy, Tier = MissionTier.Daily, Target = 3, GoldReward = 6000, XPReward = 1500, Icon = "ui/icons/missions/economy.svg" },
		new() { Id = "boost_activator", Name = "Boost Activator", Description = "Activate a boost item", Category = MissionCategory.Economy, Tier = MissionTier.Daily, Target = 1, GoldReward = 5000, XPReward = 1000, Icon = "ui/icons/missions/economy.svg" },

		// ── SOCIAL ──────────────────────────────────────────────
		new() { Id = "chatterbox", Name = "Chatterbox", Description = "Send 10 chat messages", Category = MissionCategory.Social, Tier = MissionTier.Daily, Target = 10, GoldReward = 3000, XPReward = 500, Icon = "ui/icons/missions/social.svg" },
		new() { Id = "show_off", Name = "Show Off", Description = "Share a beast in chat", Category = MissionCategory.Social, Tier = MissionTier.Daily, Target = 1, GoldReward = 5000, XPReward = 1000, Icon = "ui/icons/missions/social.svg" },
		new() { Id = "arena_challenger", Name = "Arena Challenger", Description = "Play 1 ranked arena set", Category = MissionCategory.Social, Tier = MissionTier.Daily, Target = 1, GoldReward = 8000, XPReward = 2000, GemReward = 2, Icon = "ui/icons/missions/social.svg" },
		new() { Id = "guild_contributor", Name = "Guild Contributor", Description = "Contribute 500 Guild XP", Category = MissionCategory.Social, Tier = MissionTier.Daily, Target = 500, GoldReward = 6000, XPReward = 1500, Icon = "ui/icons/missions/social.svg" },
		new() { Id = "trader", Name = "Trader", Description = "Complete 1 trade", Category = MissionCategory.Social, Tier = MissionTier.Daily, Target = 1, GoldReward = 8000, GemReward = 3, Icon = "ui/icons/missions/social.svg" },
	};

	private static readonly List<MissionDefinition> WeeklyPool = new()
	{
		new() { Id = "expedition_veteran", Name = "Expedition Veteran", Description = "Complete 15 expeditions", Category = MissionCategory.Expedition, Tier = MissionTier.Weekly, Target = 15, GoldReward = 25000, XPReward = 3000, InkReward = 10, Icon = "ui/icons/missions/expedition.svg" },
		new() { Id = "battle_royale", Name = "Battle Royale", Description = "Win 25 battles", Category = MissionCategory.Battle, Tier = MissionTier.Weekly, Target = 25, GoldReward = 30000, XPReward = 4000, GemReward = 3, Icon = "ui/icons/missions/battle.svg" },
		new() { Id = "collection_spree", Name = "Collection Spree", Description = "Contract 20 monsters", Category = MissionCategory.Collection, Tier = MissionTier.Weekly, Target = 20, GoldReward = 25000, InkReward = 20, GemReward = 3, Icon = "ui/icons/missions/collection.svg" },
		new() { Id = "fusion_frenzy", Name = "Fusion Frenzy", Description = "Fuse 10 monsters", Category = MissionCategory.Collection, Tier = MissionTier.Weekly, Target = 10, GoldReward = 35000, GemReward = 5, Icon = "ui/icons/missions/collection.svg" },
		new() { Id = "gold_hoarder", Name = "Gold Hoarder", Description = "Earn 50,000 gold total", Category = MissionCategory.Economy, Tier = MissionTier.Weekly, Target = 50000, GoldReward = 15000, XPReward = 3000, GemReward = 2, Icon = "ui/icons/missions/economy.svg" },
		new() { Id = "arena_warrior", Name = "Arena Warrior", Description = "Complete 10 ranked sets", Category = MissionCategory.Social, Tier = MissionTier.Weekly, Target = 10, GoldReward = 40000, GemReward = 5, Icon = "ui/icons/missions/social.svg" },
		new() { Id = "element_master", Name = "Element Master", Description = "Contract monsters of 5 different elements", Category = MissionCategory.Collection, Tier = MissionTier.Weekly, Target = 5, GoldReward = 30000, GemReward = 3, InkReward = 10, Icon = "ui/icons/missions/collection.svg" },
		new() { Id = "boss_crusher", Name = "Boss Crusher", Description = "Defeat 10 bosses", Category = MissionCategory.Expedition, Tier = MissionTier.Weekly, Target = 10, GoldReward = 40000, GemReward = 5, Icon = "ui/icons/missions/expedition.svg" },
		new() { Id = "evolution_chain", Name = "Evolution Chain", Description = "Evolve 5 monsters", Category = MissionCategory.Collection, Tier = MissionTier.Weekly, Target = 5, GoldReward = 35000, GemReward = 4, Icon = "ui/icons/missions/collection.svg" },
		new() { Id = "hard_specialist", Name = "Hard Mode Specialist", Description = "Complete 5 Hard Mode expeditions", Category = MissionCategory.Expedition, Tier = MissionTier.Weekly, Target = 5, GoldReward = 50000, GemReward = 5, Icon = "ui/icons/missions/expedition.svg" },
		new() { Id = "damage_legend", Name = "Damage Legend", Description = "Deal 500,000 total damage", Category = MissionCategory.Battle, Tier = MissionTier.Weekly, Target = 500000, GoldReward = 35000, GemReward = 4, Icon = "ui/icons/missions/battle.svg" },
		new() { Id = "gene_perfectionist", Name = "Gene Perfectionist", Description = "Fuse a monster with 25+ total genes", Category = MissionCategory.Collection, Tier = MissionTier.Weekly, Target = 1, GoldReward = 40000, GemReward = 3, Icon = "ui/icons/missions/collection.svg" },
	};

	private static readonly List<MissionDefinition> MonthlyPool = new()
	{
		new() { Id = "expedition_master", Name = "Expedition Master", Description = "Complete all 16 expeditions", Category = MissionCategory.Expedition, Tier = MissionTier.Monthly, Target = 16, GoldReward = 150000, GemReward = 20, Icon = "ui/icons/missions/expedition.svg" },
		new() { Id = "beast_collector", Name = "Beast Collector", Description = "Contract 200 monsters", Category = MissionCategory.Collection, Tier = MissionTier.Monthly, Target = 200, GoldReward = 200000, GemReward = 30, Icon = "ui/icons/missions/collection.svg" },
		new() { Id = "battle_legend", Name = "Battle Legend", Description = "Win 100 battles", Category = MissionCategory.Battle, Tier = MissionTier.Monthly, Target = 100, GoldReward = 175000, GemReward = 25, Icon = "ui/icons/missions/battle.svg" },
		new() { Id = "fusion_expert", Name = "Fusion Expert", Description = "Fuse 50 monsters", Category = MissionCategory.Collection, Tier = MissionTier.Monthly, Target = 50, GoldReward = 200000, GemReward = 30, Icon = "ui/icons/missions/collection.svg" },
		new() { Id = "arena_champion", Name = "Arena Champion", Description = "Win 30 ranked sets", Category = MissionCategory.Social, Tier = MissionTier.Monthly, Target = 30, GoldReward = 250000, GemReward = 40, Icon = "ui/icons/missions/social.svg" },
	};

	// ═══════════════════════════════════════════════════════════════
	// LIFECYCLE
	// ═══════════════════════════════════════════════════════════════

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			GameObject.Tags.Add( "bb-persistent" );
			Log.Info( "MissionManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnDestroy()
	{
		// Clear the static so a stale reference past play mode doesn't make the
		// next session's OnAwake self-destruct the new manager.
		if ( Instance == this )
			Instance = null;
	}

	protected override void OnStart()
	{
		if ( SaveService.Instance != null && SaveService.Instance.IsLoaded )
		{
			Hydrate();
			CheckRefresh();
		}
		else if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveLoaded += () =>
			{
				Hydrate();
				CheckRefresh();
			};
		}

		if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveReset += HandleSaveReset;
		}
	}

	private void HandleSaveReset()
	{
		_hasHydrated = false;
		ActiveDailyMissions = new();
		ActiveWeeklyMissions = new();
		ActiveMonthlyChallenge = null;
		DailyBonusClaimed = false;
		WeeklyBonusClaimed = false;
		Hydrate();
		CheckRefresh();
		Log.Info( "[MissionManager] reset" );
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "MissionManager";
		go.Components.Create<MissionManager>();
	}

	// ═══════════════════════════════════════════════════════════════
	// MISSION GENERATION
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Generate 5 daily missions — one per category.
	/// </summary>
	public void GenerateDailyMissions()
	{
		ActiveDailyMissions.Clear();
		DailyBonusClaimed = false;
		var random = new Random();

		foreach ( MissionCategory category in Enum.GetValues<MissionCategory>() )
		{
			var candidates = DailyPool.Where( m => m.Category == category ).ToList();
			if ( candidates.Count == 0 ) continue;

			var picked = candidates[random.Next( candidates.Count )];
			ActiveDailyMissions.Add( new MissionState
			{
				MissionId = picked.Id,
				Progress = 0,
				Completed = false,
				Claimed = false
			} );
		}

		DailyRefreshDate = GetNextMidnightUtc();
		Log.Info( $"[Missions] Generated {ActiveDailyMissions.Count} daily missions, refresh at {DailyRefreshDate}" );
	}

	/// <summary>
	/// Generate 3 weekly missions randomly from the weekly pool.
	/// </summary>
	public void GenerateWeeklyMissions()
	{
		ActiveWeeklyMissions.Clear();
		WeeklyBonusClaimed = false;
		var random = new Random();

		var shuffled = WeeklyPool.OrderBy( _ => random.Next() ).Take( 3 ).ToList();
		foreach ( var def in shuffled )
		{
			ActiveWeeklyMissions.Add( new MissionState
			{
				MissionId = def.Id,
				Progress = 0,
				Completed = false,
				Claimed = false
			} );
		}

		WeeklyRefreshDate = GetNextMondayMidnight();
		Log.Info( $"[Missions] Generated {ActiveWeeklyMissions.Count} weekly missions, refresh at {WeeklyRefreshDate}" );
	}

	/// <summary>
	/// Generate 1 monthly challenge from the monthly pool.
	/// </summary>
	public void GenerateMonthlyChallenge()
	{
		var random = new Random();
		var picked = MonthlyPool[random.Next( MonthlyPool.Count )];

		ActiveMonthlyChallenge = new MissionState
		{
			MissionId = picked.Id,
			Progress = 0,
			Completed = false,
			Claimed = false
		};

		MonthlyRefreshDate = GetFirstOfNextMonth();
		Log.Info( $"[Missions] Generated monthly challenge: {picked.Name}, refresh at {MonthlyRefreshDate}" );
	}

	// ═══════════════════════════════════════════════════════════════
	// REFRESH CHECK
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Check if any mission tier needs refreshing based on current UTC time.
	/// </summary>
	public void CheckRefresh()
	{
		var now = DateTime.UtcNow;
		bool changed = false;

		// Daily refresh (midnight UTC)
		if ( now >= DailyRefreshDate || ActiveDailyMissions.Count == 0 )
		{
			GenerateDailyMissions();
			changed = true;
		}

		// Weekly refresh (Monday midnight UTC)
		if ( now >= WeeklyRefreshDate || ActiveWeeklyMissions.Count == 0 )
		{
			GenerateWeeklyMissions();
			changed = true;
		}

		// Monthly refresh (1st of month midnight UTC)
		if ( now >= MonthlyRefreshDate || ActiveMonthlyChallenge == null )
		{
			GenerateMonthlyChallenge();
			changed = true;
		}

		if ( changed )
		{
			SaveToCookies();
			OnMissionsUpdated?.Invoke();
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// PROGRESS TRACKING
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Update progress for a specific mission by ID. Returns true if the mission was completed by this update.
	/// </summary>
	public bool UpdateProgress( string missionId, int amount )
	{
		var state = FindMissionState( missionId );
		if ( state == null || state.Completed ) return false;

		var def = GetDefinition( missionId );
		if ( def == null ) return false;

		state.Progress += amount;

		if ( state.Progress >= def.Target )
		{
			state.Progress = def.Target;
			state.Completed = true;

			NotificationManager.Instance?.AddNotification(
				NotificationType.Success,
				"Mission Complete!",
				$"{def.Name} — Claim your reward!"
			);

			OnMissionCompleted?.Invoke( missionId );
			Log.Info( $"[Missions] Completed: {def.Name}" );
		}

		OnMissionProgress?.Invoke( missionId, state.Progress, def.Target );
		SaveToCookies();
		return state.Completed;
	}

	/// <summary>
	/// Set absolute progress for a mission (used for missions that track totals, not increments).
	/// </summary>
	public void SetProgress( string missionId, int absoluteValue )
	{
		var state = FindMissionState( missionId );
		if ( state == null || state.Completed ) return;

		var def = GetDefinition( missionId );
		if ( def == null ) return;

		state.Progress = absoluteValue;

		if ( state.Progress >= def.Target )
		{
			state.Progress = def.Target;
			state.Completed = true;

			NotificationManager.Instance?.AddNotification(
				NotificationType.Success,
				"Mission Complete!",
				$"{def.Name} — Claim your reward!"
			);

			OnMissionCompleted?.Invoke( missionId );
		}

		OnMissionProgress?.Invoke( missionId, state.Progress, def.Target );
		SaveToCookies();
	}

	// ═══════════════════════════════════════════════════════════════
	// CONVENIENCE TRACKING METHODS
	// Called by other managers when relevant events happen.
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Track a battle win. Call after any battle victory.
	/// </summary>
	public void TrackBattleWin( bool flawless = false, bool typeAdvantage = false )
	{
		TrackActiveMissions( "win_battles", 1 );
		TrackActiveMissions( "battle_royale", 1 );
		TrackActiveMissions( "battle_legend", 1 );

		if ( flawless )
			TrackActiveMissions( "battle_master", 1 );

		if ( typeAdvantage )
			TrackActiveMissions( "type_hunter", 1 );
	}

	/// <summary>
	/// Track knockouts scored in battle.
	/// </summary>
	public void TrackKnockouts( int count )
	{
		TrackActiveMissions( "knockout_king", count );
	}

	/// <summary>
	/// Track damage dealt in battle.
	/// </summary>
	public void TrackDamageDealt( int amount )
	{
		TrackActiveMissions( "damage_dealer", amount );
		TrackActiveMissions( "damage_legend", amount );
	}

	/// <summary>
	/// Track expedition completion.
	/// </summary>
	public void TrackExpeditionComplete( bool hardMode = false, int goldEarned = 0 )
	{
		TrackActiveMissions( "explorer", 1 );
		TrackActiveMissions( "expedition_veteran", 1 );
		TrackActiveMissions( "expedition_master", 1 );

		if ( hardMode )
		{
			TrackActiveMissions( "hard_mode", 1 );
			TrackActiveMissions( "hard_specialist", 1 );
		}

		if ( goldEarned > 0 )
		{
			TrackActiveMissions( "gold_rush", goldEarned );
			TrackActiveMissions( "gold_hoarder", goldEarned );
		}
	}

	/// <summary>
	/// Track a wave cleared in expedition.
	/// </summary>
	public void TrackWaveCleared()
	{
		TrackActiveMissions( "horde_slayer", 1 );
	}

	/// <summary>
	/// Track a boss defeat.
	/// </summary>
	public void TrackBossDefeated()
	{
		TrackActiveMissions( "boss_hunter", 1 );
		TrackActiveMissions( "boss_crusher", 1 );
	}

	/// <summary>
	/// Track a monster catch.
	/// </summary>
	public void TrackMonsterCaught( bool isRareOrHigher = false, bool isNewDiscovery = false )
	{
		TrackActiveMissions( "contractor", 1 );
		TrackActiveMissions( "element_seeker", 1 );
		TrackActiveMissions( "collection_spree", 1 );
		TrackActiveMissions( "beast_collector", 1 );

		if ( isRareOrHigher )
			TrackActiveMissions( "rare_finder", 1 );

		if ( isNewDiscovery )
			TrackActiveMissions( "new_discovery", 1 );
	}

	/// <summary>
	/// Track a fusion.
	/// </summary>
	public void TrackFusion( int totalGenes = 0 )
	{
		TrackActiveMissions( "fusion_scientist", 1 );
		TrackActiveMissions( "fusion_frenzy", 1 );
		TrackActiveMissions( "fusion_expert", 1 );

		if ( totalGenes >= 25 )
			TrackActiveMissions( "gene_perfectionist", 1 );
	}

	/// <summary>
	/// Track a monster evolution.
	/// </summary>
	public void TrackEvolution()
	{
		TrackActiveMissions( "evolution_chain", 1 );
	}

	/// <summary>
	/// Track a shop purchase.
	/// </summary>
	public void TrackShopPurchase( int goldSpent )
	{
		TrackActiveMissions( "shopper", 1 );
		TrackActiveMissions( "big_spender", goldSpent );
	}

	/// <summary>
	/// Track consumable item usage.
	/// </summary>
	public void TrackItemUsed()
	{
		TrackActiveMissions( "item_user", 1 );
	}

	/// <summary>
	/// Track relic equipping. Pass the total number of equipped relics.
	/// </summary>
	public void TrackRelicsEquipped( int totalEquipped )
	{
		SetProgressForActive( "relic_master", totalEquipped );
	}

	/// <summary>
	/// Track boost activation.
	/// </summary>
	public void TrackBoostActivated()
	{
		TrackActiveMissions( "boost_activator", 1 );
	}

	/// <summary>
	/// Track a chat message sent.
	/// </summary>
	public void TrackChatMessage()
	{
		TrackActiveMissions( "chatterbox", 1 );
	}

	/// <summary>
	/// Track beast shared in chat.
	/// </summary>
	public void TrackBeastShared()
	{
		TrackActiveMissions( "show_off", 1 );
	}

	/// <summary>
	/// Track ranked arena set completed.
	/// </summary>
	public void TrackArenaSet( bool won = false )
	{
		TrackActiveMissions( "arena_challenger", 1 );
		TrackActiveMissions( "arena_warrior", 1 );

		if ( won )
			TrackActiveMissions( "arena_champion", 1 );
	}

	/// <summary>
	/// Track Guild XP contribution.
	/// </summary>
	public void TrackGuildXP( int amount )
	{
		TrackActiveMissions( "guild_contributor", amount );
	}

	/// <summary>
	/// Track a completed trade.
	/// </summary>
	public void TrackTradeComplete()
	{
		TrackActiveMissions( "trader", 1 );
	}

	/// <summary>
	/// Track gold earned (for gold-earning missions like gold_rush, gold_hoarder).
	/// </summary>
	public void TrackGoldEarned( int amount )
	{
		TrackActiveMissions( "gold_rush", amount );
		TrackActiveMissions( "gold_hoarder", amount );
	}

	/// <summary>
	/// Track distinct elements caught this week (for element_master).
	/// </summary>
	public void TrackElementsCaught( int distinctCount )
	{
		SetProgressForActive( "element_master", distinctCount );
	}

	/// <summary>
	/// Helper: increment progress on any active mission matching the ID.
	/// </summary>
	private void TrackActiveMissions( string missionId, int amount )
	{
		var state = FindMissionState( missionId );
		if ( state != null && !state.Completed )
		{
			UpdateProgress( missionId, amount );
		}
	}

	/// <summary>
	/// Helper: set absolute progress on any active mission matching the ID.
	/// </summary>
	private void SetProgressForActive( string missionId, int absoluteValue )
	{
		var state = FindMissionState( missionId );
		if ( state != null && !state.Completed )
		{
			SetProgress( missionId, absoluteValue );
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// REWARD CLAIMING
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Claim the reward for a completed mission. Returns true if successful.
	/// </summary>
	public bool ClaimReward( string missionId )
	{
		var state = FindMissionState( missionId );
		if ( state == null || !state.Completed || state.Claimed ) return false;

		var def = GetDefinition( missionId );
		if ( def == null ) return false;

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return false;

		// Grant rewards
		if ( def.GoldReward > 0 )
			TamerManager.Instance.AddGold( def.GoldReward );

		if ( def.GemReward > 0 )
			TamerManager.Instance.AddGems( def.GemReward );

		if ( def.InkReward > 0 )
			TamerManager.Instance.AddContractInk( def.InkReward );

		if ( def.XPReward > 0 )
			TamerManager.Instance.AddXP( def.XPReward );

		if ( !string.IsNullOrEmpty( def.ItemReward ) )
		{
			if ( tamer.Inventory.ContainsKey( def.ItemReward ) )
				tamer.Inventory[def.ItemReward]++;
			else
				tamer.Inventory[def.ItemReward] = 1;
		}

		state.Claimed = true;

		NotificationManager.Instance?.AddNotification(
			NotificationType.Success,
			"Reward Claimed!",
			$"{def.Name}: {FormatRewardText( def )}"
		);

		SaveToCookies();
		TamerManager.Instance?.SaveToCloud();

		Log.Info( $"[Missions] Claimed reward for: {def.Name}" );
		return true;
	}

	// ═══════════════════════════════════════════════════════════════
	// BONUS REWARDS (all dailies / all weeklies complete)
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Check if all 5 daily missions are completed and claimed.
	/// </summary>
	public bool AreAllDailiesComplete()
	{
		return ActiveDailyMissions.Count > 0 &&
			   ActiveDailyMissions.All( m => m.Completed && m.Claimed );
	}

	/// <summary>
	/// Check if all 3 weekly missions are completed and claimed.
	/// </summary>
	public bool AreAllWeekliesComplete()
	{
		return ActiveWeeklyMissions.Count > 0 &&
			   ActiveWeeklyMissions.All( m => m.Completed && m.Claimed );
	}

	/// <summary>
	/// Claim the daily bonus (10,000 Gold + 5 Gems + 1,000 XP). Returns true if successful.
	/// </summary>
	public bool ClaimDailyBonus()
	{
		if ( !AreAllDailiesComplete() || DailyBonusClaimed ) return false;

		TamerManager.Instance?.AddGold( 10000 );
		TamerManager.Instance?.AddGems( 5 );
		TamerManager.Instance?.AddXP( 1000 );

		DailyBonusClaimed = true;

		NotificationManager.Instance?.AddNotification(
			NotificationType.Success,
			"Daily Bonus!",
			"All dailies complete: 10,000 Gold + 5 Gems + 1,000 XP"
		);

		SaveToCookies();
		TamerManager.Instance?.SaveToCloud();

		Log.Info( "[Missions] Claimed daily bonus reward" );
		return true;
	}

	/// <summary>
	/// Claim the weekly bonus (50,000 Gold + 10 Gems). Returns true if successful.
	/// </summary>
	public bool ClaimWeeklyBonus()
	{
		if ( !AreAllWeekliesComplete() || WeeklyBonusClaimed ) return false;

		TamerManager.Instance?.AddGold( 50000 );
		TamerManager.Instance?.AddGems( 10 );

		WeeklyBonusClaimed = true;

		NotificationManager.Instance?.AddNotification(
			NotificationType.Success,
			"Weekly Bonus!",
			"All weeklies complete: 50,000 Gold + 10 Gems"
		);

		SaveToCookies();
		TamerManager.Instance?.SaveToCloud();

		Log.Info( "[Missions] Claimed weekly bonus reward" );
		return true;
	}

	// ═══════════════════════════════════════════════════════════════
	// QUERY HELPERS
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Find a MissionState by mission ID across all active tiers.
	/// </summary>
	public MissionState FindMissionState( string missionId )
	{
		var state = ActiveDailyMissions.FirstOrDefault( m => m.MissionId == missionId );
		if ( state != null ) return state;

		state = ActiveWeeklyMissions.FirstOrDefault( m => m.MissionId == missionId );
		if ( state != null ) return state;

		if ( ActiveMonthlyChallenge?.MissionId == missionId )
			return ActiveMonthlyChallenge;

		return null;
	}

	/// <summary>
	/// Get the definition for a mission by ID.
	/// </summary>
	public MissionDefinition GetDefinition( string missionId )
	{
		var def = DailyPool.FirstOrDefault( m => m.Id == missionId );
		if ( def != null ) return def;

		def = WeeklyPool.FirstOrDefault( m => m.Id == missionId );
		if ( def != null ) return def;

		def = MonthlyPool.FirstOrDefault( m => m.Id == missionId );
		return def;
	}

	/// <summary>
	/// Get the count of claimable rewards (completed but not yet claimed) for badge display.
	/// </summary>
	public int GetUnclaimedCount()
	{
		int count = 0;

		count += ActiveDailyMissions.Count( m => m.Completed && !m.Claimed );
		count += ActiveWeeklyMissions.Count( m => m.Completed && !m.Claimed );

		if ( ActiveMonthlyChallenge is { Completed: true, Claimed: false } )
			count++;

		if ( AreAllDailiesComplete() && !DailyBonusClaimed )
			count++;

		if ( AreAllWeekliesComplete() && !WeeklyBonusClaimed )
			count++;

		return count;
	}

	/// <summary>
	/// Get time remaining until daily missions refresh.
	/// </summary>
	public TimeSpan GetDailyTimeRemaining()
	{
		var remaining = DailyRefreshDate - DateTime.UtcNow;
		return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
	}

	/// <summary>
	/// Get time remaining until weekly missions refresh.
	/// </summary>
	public TimeSpan GetWeeklyTimeRemaining()
	{
		var remaining = WeeklyRefreshDate - DateTime.UtcNow;
		return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
	}

	/// <summary>
	/// Get time remaining until monthly challenge refreshes.
	/// </summary>
	public TimeSpan GetMonthlyTimeRemaining()
	{
		var remaining = MonthlyRefreshDate - DateTime.UtcNow;
		return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
	}

	/// <summary>
	/// Format reward text for display.
	/// </summary>
	public static string FormatRewardText( MissionDefinition def )
	{
		var parts = new List<string>();
		if ( def.GoldReward > 0 ) parts.Add( $"{def.GoldReward:N0} Gold" );
		if ( def.XPReward > 0 ) parts.Add( $"{def.XPReward:N0} XP" );
		if ( def.GemReward > 0 ) parts.Add( $"{def.GemReward} Gem{(def.GemReward != 1 ? "s" : "")}" );
		if ( def.InkReward > 0 ) parts.Add( $"{def.InkReward} Ink" );
		if ( !string.IsNullOrEmpty( def.ItemReward ) ) parts.Add( def.ItemReward );
		return string.Join( " + ", parts );
	}

	// ═══════════════════════════════════════════════════════════════
	// PERSISTENCE (Cookie-based)
	// ═══════════════════════════════════════════════════════════════

	private void Hydrate()
	{
		if ( _hasHydrated ) return;
		_hasHydrated = true;

		var section = SaveService.Instance?.CurrentBlob?.Missions;
		if ( section == null )
		{
			ActiveDailyMissions = new();
			ActiveWeeklyMissions = new();
			ActiveMonthlyChallenge = null;
			DailyRefreshDate = DateTime.MinValue;
			WeeklyRefreshDate = DateTime.MinValue;
			MonthlyRefreshDate = DateTime.MinValue;
			DailyBonusClaimed = false;
			WeeklyBonusClaimed = false;
			Log.Info( "[Missions] Hydrated: fresh (no existing save)" );
			return;
		}

		ActiveDailyMissions = section.DailyMissions ?? new();
		ActiveWeeklyMissions = section.WeeklyMissions ?? new();
		ActiveMonthlyChallenge = section.MonthlyChallenge;

		DailyRefreshDate = section.DailyRefreshTicks > 0 ? new DateTime( section.DailyRefreshTicks, DateTimeKind.Utc ) : DateTime.MinValue;
		WeeklyRefreshDate = section.WeeklyRefreshTicks > 0 ? new DateTime( section.WeeklyRefreshTicks, DateTimeKind.Utc ) : DateTime.MinValue;
		MonthlyRefreshDate = section.MonthlyRefreshTicks > 0 ? new DateTime( section.MonthlyRefreshTicks, DateTimeKind.Utc ) : DateTime.MinValue;

		DailyBonusClaimed = section.DailyBonusClaimed;
		WeeklyBonusClaimed = section.WeeklyBonusClaimed;

		Log.Info( $"[Missions] Hydrated: {ActiveDailyMissions.Count} daily, {ActiveWeeklyMissions.Count} weekly, monthly={ActiveMonthlyChallenge != null}" );
	}

	/// <summary>
	/// Push mission state back into the save blob. Kept private and named
	/// <c>SaveToCookies</c> so the many internal call sites in this file
	/// keep working without touching them all individually.
	/// </summary>
	private void SaveToCookies()
	{
		var service = SaveService.Instance;
		if ( service == null ) return;
		var blob = service.CurrentBlob;
		if ( blob == null ) return;

		blob.Missions ??= new MissionSaveData();
		blob.Missions.DailyMissions = ActiveDailyMissions ?? new();
		blob.Missions.WeeklyMissions = ActiveWeeklyMissions ?? new();
		blob.Missions.MonthlyChallenge = ActiveMonthlyChallenge;
		blob.Missions.DailyRefreshTicks = DailyRefreshDate.Ticks;
		blob.Missions.WeeklyRefreshTicks = WeeklyRefreshDate.Ticks;
		blob.Missions.MonthlyRefreshTicks = MonthlyRefreshDate.Ticks;
		blob.Missions.DailyBonusClaimed = DailyBonusClaimed;
		blob.Missions.WeeklyBonusClaimed = WeeklyBonusClaimed;
		service.MarkDirty( "missions" );
	}

	// ═══════════════════════════════════════════════════════════════
	// TIME UTILITIES
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Returns tomorrow at midnight UTC.
	/// </summary>
	private static DateTime GetNextMidnightUtc()
	{
		return DateTime.UtcNow.Date.AddDays( 1 );
	}

	/// <summary>
	/// Returns next Monday at midnight UTC.
	/// </summary>
	private static DateTime GetNextMondayMidnight()
	{
		var now = DateTime.UtcNow;
		int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
		if ( daysUntilMonday == 0 ) daysUntilMonday = 7;
		return now.Date.AddDays( daysUntilMonday );
	}

	/// <summary>
	/// Returns the 1st of next month at midnight UTC.
	/// </summary>
	private static DateTime GetFirstOfNextMonth()
	{
		var now = DateTime.UtcNow;
		if ( now.Month == 12 )
			return new DateTime( now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc );
		return new DateTime( now.Year, now.Month + 1, 1, 0, 0, 0, DateTimeKind.Utc );
	}
}
