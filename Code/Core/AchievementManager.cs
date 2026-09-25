using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Services;
using Beastborne.Data;
using Achievement = Beastborne.Data.Achievement;

namespace Beastborne.Core;

/// <summary>
/// Manages achievement tracking, unlocking, and reward granting.
/// Hooks into other managers to detect progress changes.
/// </summary>
public sealed class AchievementManager : Component
{
	public static AchievementManager Instance { get; private set; }

	// All achievement definitions
	private List<Achievement> _achievements = new();
	public IReadOnlyList<Achievement> AllAchievements => _achievements;

	// Ids of the CURRENT set. tamer.Achievements can hold ids that are no longer
	// defined (cut achievements) — every count must filter through this.
	private HashSet<string> _definedIds = new();
	public bool IsDefined( string achievementId ) => achievementId != null && _definedIds.Contains( achievementId );

	/// <summary>The achievement-set version this build defines (see Tamer.AchievementSetVersion).</summary>
	public const int CURRENT_ACHIEVEMENT_SET = 2;

	// Events
	public Action<Achievement> OnAchievementUnlocked;
	public Action<string, int> OnProgressUpdated; // achievementId, newValue

	// Track if retroactive check has been done this session
	private bool _retroactiveCheckDone = false;

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			GameObject.Tags.Add( "bb-persistent" );
			InitializeAchievements();
			Log.Info( $"AchievementManager initialized with {_achievements.Count} achievements" );
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

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "AchievementManager";
		go.Components.Create<AchievementManager>();
	}

	// ═══════════════════════════════════════════════════════════════
	// ACHIEVEMENT DEFINITIONS — 2026-09 restart (15, Tokens only)
	// ═══════════════════════════════════════════════════════════════
	//
	// Rules for this set (user 2026-09-25):
	//  • 15 max. Each pillar gets a first beat + one capstone. Deterministic,
	//    visible targets only — no RNG gene rolls, no count grinds.
	//  • FIXED numeric targets. Never compute a target from a growing set
	//    (roster size, pattern count, boss count) — more beasts / patterns /
	//    bosses ship over time and an "all of X" target would silently move.
	//    Targets sit at or below what a solo player can reach today:
	//      Beastbook  26 launch species, 20 solo-reachable (the other two
	//                 starter lines need trades) → 10 / 20
	//      Patterns   3 in the book, 2 without a Pagefin (Patient Vow needs
	//                 one) → 1 / 2
	//      Bosses     3 (Weaverwood, Weavermere, Whispering Hollow mini) → 1 / 3
	//      Hard Mode  3 real expeditions (mini excluded) → 1 / 3
	//      Level      cap 50 → 50
	//  • Ids kept where the meaning is unchanged (the s&box platform achievement
	//    ids stay stable). Old ids that aren't redefined here are legacy — see
	//    LegacyRewards (frozen) and ApplyAchievementRestart.

	private void InitializeAchievements()
	{
		_achievements.Clear();
		int order = 0;

		// ── CONTRACTS & BEASTBOOK ──────────────────────────────────

		AddAchievement( "catch_1", "First Contract", "Contract your first beast", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 1, order++,
			new() { Reward( AchievementRewardType.BossTokens, 5 ) } );

		AddAchievement( "tribute_1", "Offering", "Offer Tokens in a Tribute contract", AchievementCategory.Collection,
			AchievementRequirement.TributesOffered, 1, order++,
			new() { Reward( AchievementRewardType.BossTokens, 5 ) } );

		AddAchievement( "beastbook_10", "Field Notes", "Discover 10 species in the Beastbook", AchievementCategory.Collection,
			AchievementRequirement.BeastbookDiscovered, 10, order++,
			new() { Reward( AchievementRewardType.BossTokens, 10 ) } );

		AddAchievement( "beastbook_20", "Beastborne Master", "Discover 20 species in the Beastbook", AchievementCategory.Collection,
			AchievementRequirement.BeastbookDiscovered, 20, order++,
			new() { Reward( AchievementRewardType.BossTokens, 25 ), Reward( AchievementRewardType.Title, 0, "Beastborne Master" ) } );

		// ── FUSION ─────────────────────────────────────────────────

		AddAchievement( "breed_1", "First Weave", "Fuse your first beast", AchievementCategory.Breeding,
			AchievementRequirement.TotalMonstersBred, 1, order++,
			new() { Reward( AchievementRewardType.BossTokens, 5 ) } );

		AddAchievement( "pattern_1", "Pattern Found", "Weave your first cross-species fusion pattern", AchievementCategory.Breeding,
			AchievementRequirement.PatternsDiscovered, 1, order++,
			new() { Reward( AchievementRewardType.BossTokens, 10 ) } );

		AddAchievement( "pattern_2", "Pattern Weaver", "Inscribe 2 fusion patterns in the Pattern Book", AchievementCategory.Breeding,
			AchievementRequirement.PatternsDiscovered, 2, order++,
			new() { Reward( AchievementRewardType.BossTokens, 20 ), Reward( AchievementRewardType.Title, 0, "Master Fuser" ) } );

		// ── EXPEDITIONS, BOSSES & HARD MODE ────────────────────────

		AddAchievement( "expedition_1", "First Steps", "Clear your first expedition", AchievementCategory.Expedition,
			AchievementRequirement.HighestExpeditionCleared, 1, order++,
			new() { Reward( AchievementRewardType.BossTokens, 5 ) } );

		AddAchievement( "boss_first", "Boss Slayer", "Defeat your first expedition boss", AchievementCategory.Expedition,
			AchievementRequirement.BossesCleared, 1, order++,
			new() { Reward( AchievementRewardType.BossTokens, 10 ), Reward( AchievementRewardType.Title, 0, "Boss Slayer" ) } );

		AddAchievement( "boss_3", "Supreme Tamer", "Defeat 3 different expedition bosses", AchievementCategory.Expedition,
			AchievementRequirement.BossesCleared, 3, order++,
			new() { Reward( AchievementRewardType.BossTokens, 25 ), Reward( AchievementRewardType.Title, 0, "Supreme Tamer" ) } );

		// Hard Mode steps check HighestHardModeCleared — an order-independent
		// COUNT of distinct expeditions Hard-cleared, not a specific zone.
		AddAchievement( "hard_mode_1", "Hard Mode Initiate", "Clear 1 expedition on Hard Mode", AchievementCategory.Expedition,
			AchievementRequirement.HighestHardModeCleared, 1, order++,
			new() { Reward( AchievementRewardType.BossTokens, 10 ) } );

		AddAchievement( "hard_mode_16", "Hard Mode Master", "Clear 3 expeditions on Hard Mode", AchievementCategory.Expedition,
			AchievementRequirement.HighestHardModeCleared, 3, order++,
			new() { Reward( AchievementRewardType.BossTokens, 25 ), Reward( AchievementRewardType.Title, 0, "Unyielding" ) } );

		// ── MASTERY ────────────────────────────────────────────────

		AddAchievement( "evolve_1", "First Evolution", "Evolve a beast", AchievementCategory.Mastery,
			AchievementRequirement.MonstersEvolved, 1, order++,
			new() { Reward( AchievementRewardType.BossTokens, 5 ) } );

		AddAchievement( "veteran_max", "Grandmaster Scholar", "Reach Grandmaster mastery on any species", AchievementCategory.Mastery,
			AchievementRequirement.MonsterVeteranMaxRank, 1, order++,
			new() { Reward( AchievementRewardType.BossTokens, 15 ) } );

		AddAchievement( "level_50", "The Summit", "Reach Tamer Level 50", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 50, order++,
			new() { Reward( AchievementRewardType.BossTokens, 20 ) } );

		_definedIds = new HashSet<string>( _achievements.Select( a => a.Id ) );
	}

	// ═══════════════════════════════════════════════════════════════
	// HELPER METHODS FOR DEFINING ACHIEVEMENTS
	// ═══════════════════════════════════════════════════════════════

	private void AddAchievement( string id, string name, string desc, AchievementCategory cat,
		AchievementRequirement req, int reqValue, int order, List<AchievementReward> rewards, bool isSecret = false )
	{
		_achievements.Add( new Achievement
		{
			Id = id,
			Name = name,
			Description = desc,
			Category = cat,
			Requirement = req,
			RequiredValue = reqValue,
			Order = order,
			Rewards = rewards,
			IsSecret = isSecret
		} );
	}

	private static AchievementReward Reward( AchievementRewardType type, int value, string itemOrSpeciesId = null )
	{
		return new AchievementReward
		{
			Type = type,
			Value = value,
			ItemId = type == AchievementRewardType.Item || type == AchievementRewardType.Title ? itemOrSpeciesId : null,
			SpeciesId = type == AchievementRewardType.Monster ? itemOrSpeciesId : null
		};
	}

	// ═══════════════════════════════════════════════════════════════
	// PROGRESS TRACKING & UNLOCKING
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Get the progress for a specific achievement
	/// </summary>
	public AchievementProgress GetProgress( string achievementId )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return null;

		tamer.Achievements ??= new();

		if ( tamer.Achievements.TryGetValue( achievementId, out var progress ) )
			return progress;

		return null;
	}

	/// <summary>
	/// Check and update progress for a requirement type.
	/// Called by other managers when stats change.
	/// </summary>
	public void CheckProgress( AchievementRequirement requirement, int currentValue )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		tamer.Achievements ??= new();

		var matching = _achievements.Where( a => a.Requirement == requirement ).ToList();

		foreach ( var achievement in matching )
		{
			if ( !tamer.Achievements.TryGetValue( achievement.Id, out var progress ) )
			{
				progress = new AchievementProgress { AchievementId = achievement.Id };
				tamer.Achievements[achievement.Id] = progress;
			}

			if ( progress.IsUnlocked ) continue;

			progress.CurrentValue = currentValue;
			OnProgressUpdated?.Invoke( achievement.Id, currentValue );

			if ( currentValue >= achievement.RequiredValue )
			{
				UnlockAchievement( achievement, progress );
			}
		}
	}

	/// <summary>
	/// Check a secret achievement by its specific condition ID
	/// </summary>
	public void CheckSecretAchievement( string achievementId )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		var achievement = _achievements.FirstOrDefault( a => a.Id == achievementId );
		if ( achievement == null ) return;

		tamer.Achievements ??= new();

		if ( !tamer.Achievements.TryGetValue( achievementId, out var progress ) )
		{
			progress = new AchievementProgress { AchievementId = achievementId };
			tamer.Achievements[achievementId] = progress;
		}

		if ( progress.IsUnlocked ) return;

		progress.CurrentValue = achievement.RequiredValue;
		UnlockAchievement( achievement, progress );
	}

	/// <summary>
	/// Unlock an achievement (rewards are NOT auto-granted; player must claim them)
	/// </summary>
	private void UnlockAchievement( Achievement achievement, AchievementProgress progress )
	{
		progress.IsUnlocked = true;
		progress.UnlockedAt = DateTime.UtcNow;

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		// NotificationManager subscribes to OnAchievementUnlocked and handles its own notification.
		// Don't fire AddNotification here to avoid double-firing.
		// Don't broadcast to chat — achievements stay in the notification layer to avoid chat bloat.

		// Fire event for UI
		OnAchievementUnlocked?.Invoke( achievement );

		// Unlock in s&box achievement system
		Sandbox.Services.Achievements.Unlock( achievement.Id );

		// Update leaderboard
		Stats.SetValue( "achievements-count", CountUnlocked( tamer ) );

		// Save
		TamerManager.Instance?.SaveToCloud();

		Log.Info( $"[Achievement] Unlocked: {achievement.Name} (rewards pending claim)" );
	}

	/// <summary>
	/// Claim rewards for an unlocked achievement. Returns true if successfully claimed.
	/// </summary>
	public bool ClaimReward( string achievementId )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return false;

		var achievement = _achievements.FirstOrDefault( a => a.Id == achievementId );
		if ( achievement == null ) return false;

		var progress = GetProgress( achievementId );
		if ( progress == null || !progress.IsUnlocked || progress.IsClaimed ) return false;

		// Grant rewards
		foreach ( var reward in achievement.Rewards )
		{
			GrantReward( tamer, reward );
		}

		progress.IsClaimed = true;
		TamerManager.Instance?.SaveToCloud();

		Log.Info( $"[Achievement] Claimed rewards for: {achievement.Name}" );
		return true;
	}

	/// <summary>
	/// Get count of achievements that are unlocked but not yet claimed
	/// </summary>
	public int GetUnclaimedCount()
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer?.Achievements == null ) return 0;
		// Defined ids only — an unclaimed entry for a cut achievement can never be
		// claimed, so counting it would pin the badge on forever.
		return tamer.Achievements.Count( kvp => IsDefined( kvp.Key ) && kvp.Value.IsUnlocked && !kvp.Value.IsClaimed );
	}

	/// <summary>
	/// Unlocked achievements of the CURRENT set for any tamer (orphan ids from
	/// cut achievements are ignored). Use this instead of counting
	/// tamer.Achievements.Values directly.
	/// </summary>
	public int CountUnlocked( Tamer tamer )
	{
		if ( tamer?.Achievements == null ) return 0;
		return tamer.Achievements.Count( kvp => IsDefined( kvp.Key ) && kvp.Value.IsUnlocked );
	}

	/// <summary>
	/// Grant a single reward to the tamer
	/// </summary>
	private void GrantReward( Tamer tamer, AchievementReward reward )
	{
		switch ( reward.Type )
		{
			case AchievementRewardType.Gold:
				tamer.Gold += reward.Value;
				break;
			case AchievementRewardType.Gems:
				// Legacy: the game has no gems (user 2026-09-25) — any gem reward
				// still defined anywhere pays Tokens 1:1.
				tamer.BossTokens += reward.Value;
				break;
			case AchievementRewardType.BossTokens:
				tamer.BossTokens += reward.Value;
				break;
			case AchievementRewardType.ContractInk:
				tamer.ContractInk += reward.Value;
				break;
			case AchievementRewardType.Title:
				if ( !string.IsNullOrEmpty( reward.ItemId )
					&& Beastborne.Data.CosmeticDatabase.GetTitle( reward.ItemId ) != null
					&& !tamer.UnlockedTitles.Contains( reward.ItemId ) )
				{
					tamer.UnlockedTitles.Add( reward.ItemId );
				}
				break;
			case AchievementRewardType.Item:
				if ( !string.IsNullOrEmpty( reward.ItemId ) )
				{
					if ( tamer.Inventory.ContainsKey( reward.ItemId ) )
						tamer.Inventory[reward.ItemId] += reward.Value;
					else
						tamer.Inventory[reward.ItemId] = reward.Value;
				}
				break;
			case AchievementRewardType.Monster:
				if ( !string.IsNullOrEmpty( reward.SpeciesId ) )
				{
					var species = MonsterManager.Instance?.GetSpecies( reward.SpeciesId );
					if ( species != null )
					{
						var monster = new Monster
						{
							SpeciesId = reward.SpeciesId,
							Nickname = species.Name,
							Level = reward.Value > 0 ? reward.Value : 1,
							Genetics = Genetics.GenerateRandom(),
							OriginalTrainerName = tamer.Name ?? "Unknown",
							OriginalTrainerId = Connection.Local?.SteamId ?? 0
						};
						MonsterManager.Instance?.RecalculateStats( monster );
						monster.FullHeal();
						MonsterManager.Instance?.AddMonster( monster );
					}
				}
				break;
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// RETROACTIVE CHECK
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// On first load after the update, scan all existing tamer stats
	/// and auto-unlock any achievements already earned.
	/// </summary>
	public void RetroactiveCheck()
	{
		if ( _retroactiveCheckDone ) return;
		_retroactiveCheckDone = true;

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		tamer.Achievements ??= new();

		// The achievement-claimed migration must run EXACTLY ONCE per save, not
		// every load. The old achievement system auto-granted rewards on unlock;
		// the new system requires a manual claim. For saves created under the old
		// system we mark already-unlocked achievements as claimed (the rewards
		// were already granted). But under the NEW system an unlocked-but-unclaimed
		// achievement is a legitimate pending-reward state — re-running this
		// migration every session would silently mark those claimed WITHOUT
		// granting the reward, permanently eating the player's rewards.
		// Gate on Tamer.MigrationVersion (persisted across sessions). TamerManager
		// hydration bumps it to 2; this migration is version 3.
		const int ACHIEVEMENT_CLAIM_MIGRATION_VERSION = 3;
		if ( tamer.MigrationVersion >= ACHIEVEMENT_CLAIM_MIGRATION_VERSION )
		{
			// Migration already done on a previous session — nothing to do.
			// (Subsequent unlocks correctly stay unclaimed until the player claims.)
			return;
		}

		// Migrate existing unlocked achievements to claimed (they got auto-rewards
		// from the old system). Runs only on the first load after this update.
		if ( tamer.Achievements.Count > 0 )
		{
			bool migrated = false;
			foreach ( var kvp in tamer.Achievements )
			{
				if ( kvp.Value.IsUnlocked && !kvp.Value.IsClaimed )
				{
					kvp.Value.IsClaimed = true;
					migrated = true;
				}
			}
			tamer.MigrationVersion = ACHIEVEMENT_CLAIM_MIGRATION_VERSION;
			TamerManager.Instance?.SaveToCloud();
			if ( migrated )
				Log.Info( "[Achievement] Migrated existing unlocked achievements to claimed state" );
			return;
		}

		Log.Info( "[Achievement] Running retroactive check for existing player..." );

		int unlocked = 0;

		// Check all stat-based achievements silently (don't spam notifications)
		foreach ( var achievement in _achievements )
		{
			if ( achievement.IsSecret ) continue;

			int currentValue = GetCurrentValueForRequirement( achievement.Requirement, tamer );
			if ( currentValue <= 0 ) continue;

			if ( !tamer.Achievements.TryGetValue( achievement.Id, out var progress ) )
			{
				progress = new AchievementProgress { AchievementId = achievement.Id };
				tamer.Achievements[achievement.Id] = progress;
			}

			progress.CurrentValue = currentValue;

			if ( currentValue >= achievement.RequiredValue && !progress.IsUnlocked )
			{
				progress.IsUnlocked = true;
				progress.IsClaimed = true; // Auto-claim retroactive rewards
				progress.UnlockedAt = DateTime.UtcNow;

				// Grant rewards silently
				foreach ( var reward in achievement.Rewards )
				{
					GrantReward( tamer, reward );
				}

				unlocked++;
			}
		}

		// Mark the achievement-claim migration done so it never runs again — even
		// if no achievements unlocked here. Otherwise the next session (when this
		// player DOES have achievement entries) would re-enter the migration block
		// above and force-claim any legitimately-pending unlocks without rewards.
		tamer.MigrationVersion = ACHIEVEMENT_CLAIM_MIGRATION_VERSION;

		if ( unlocked > 0 )
		{
			NotificationManager.Instance?.AddNotification(
				NotificationType.Success,
				"Achievements Unlocked!",
				$"{unlocked} achievements retroactively unlocked! Check your rewards."
			);

			Stats.SetValue( "achievements-count", tamer.Achievements.Values.Count( p => p.IsUnlocked ) );

			Log.Info( $"[Achievement] Retroactively unlocked {unlocked} achievements" );
		}

		TamerManager.Instance?.SaveToCloud();
	}

	/// <summary>
	/// Get the current value for a requirement from existing tamer stats
	/// </summary>
	private int GetCurrentValueForRequirement( AchievementRequirement req, Tamer tamer )
	{
		return req switch
		{
			AchievementRequirement.TotalMonstersCaught => tamer.TotalMonstersCaught,
			AchievementRequirement.TotalBattlesWon => tamer.TotalBattlesWon,
			AchievementRequirement.TotalMonstersBred => tamer.TotalMonstersBred,
			AchievementRequirement.MonstersEvolved => tamer.TotalMonstersEvolved,
			AchievementRequirement.HighestExpeditionCleared => tamer.HighestExpeditionCleared,
			AchievementRequirement.HighestHardModeCleared => tamer.HighestHardModeCleared,
			AchievementRequirement.ArenaWins => tamer.ArenaWins,
			AchievementRequirement.TamerLevel => tamer.Level,
			AchievementRequirement.TotalGoldEarned => tamer.TotalGoldEarned,
			AchievementRequirement.TotalItemsBought => tamer.TotalItemsBought,
			AchievementRequirement.ExpeditionsCompleted => tamer.TotalExpeditionsCompleted,
			AchievementRequirement.BossesCleared => tamer.ClearedBosses?.Count ?? 0,
			AchievementRequirement.TotalTradesCompleted => tamer.TotalTradesCompleted,
			AchievementRequirement.ChatMessagesSent => tamer.ChatMessagesSent,
			AchievementRequirement.BossTokensSpent => tamer.BossTokensSpent,
			AchievementRequirement.TotalDamageDealt => tamer.TotalDamageDealt,
			AchievementRequirement.TotalKnockouts => tamer.TotalKnockouts,
			AchievementRequirement.ArenaWinStreak => tamer.ArenaWinStreak,
			AchievementRequirement.ArenaSetsCompleted => tamer.ArenaSetsCompleted,
			AchievementRequirement.SkillsUnlocked => tamer.SkillRanks?.Count ?? 0,
			// Must match the live hook (TamerManager.GetTotalSkillPointsSpent) — that
			// is cost-weighted (rank × node.CostPerRank). The old `Values.Sum()` here
			// summed raw ranks, undercounting whenever any node costs >1 SP/rank, so
			// the achievement could fail to unlock retroactively for a player who
			// genuinely invested 100+ SP.
			AchievementRequirement.SkillPointsInvested => TamerManager.Instance?.GetTotalSkillPointsSpent() ?? 0,
			AchievementRequirement.TamerCardsCollected => tamer.CollectedCards?.Count ?? 0,
			AchievementRequirement.ArenaRankReached => GetRankNumericValue( tamer.ArenaRank ),
			AchievementRequirement.BeastiaryCompleted => BeastiaryManager.Instance != null && BeastiaryManager.Instance.GetDiscoveryCount() >= BeastiaryManager.Instance.GetTotalSpeciesCount() && BeastiaryManager.Instance.GetTotalSpeciesCount() > 0 ? 1 : 0,
			_ => 0
		};
	}

	/// <summary>
	/// Convert rank string to numeric for comparison
	/// </summary>
	private static int GetRankNumericValue( string rank )
	{
		return rank switch
		{
			"Mythic" => 8,
			"Legendary" => 7,
			"Master" => 6,
			"Diamond" => 5,
			"Platinum" => 4,
			"Gold" => 3,
			"Silver" => 2,
			"Bronze" => 1,
			_ => 0
		};
	}

	// ═══════════════════════════════════════════════════════════════
	// QUERY HELPERS
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Get all achievements in a category
	/// </summary>
	public List<Achievement> GetByCategory( AchievementCategory category )
	{
		return _achievements.Where( a => a.Category == category ).OrderBy( a => a.Order ).ToList();
	}

	/// <summary>
	/// Get total unlocked count
	/// </summary>
	public int GetUnlockedCount()
	{
		return CountUnlocked( TamerManager.Instance?.CurrentTamer );
	}

	/// <summary>
	/// Get total achievement count
	/// </summary>
	public int GetTotalCount() => _achievements.Count;

	/// <summary>
	/// Check if a specific achievement is unlocked
	/// </summary>
	public bool IsUnlocked( string achievementId )
	{
		var progress = GetProgress( achievementId );
		return progress?.IsUnlocked ?? false;
	}

	/// <summary>
	/// Get progress as a float 0-1 for display
	/// </summary>
	public float GetProgressPercent( string achievementId )
	{
		var achievement = _achievements.FirstOrDefault( a => a.Id == achievementId );
		if ( achievement == null ) return 0;

		var progress = GetProgress( achievementId );
		if ( progress == null ) return 0;
		if ( progress.IsUnlocked ) return 1f;

		return achievement.RequiredValue > 0 ? (float)progress.CurrentValue / achievement.RequiredValue : 0;
	}
}
