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
	// 2026-09 RESTART — legacy payout, wipe, title strip (step A)
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// FROZEN reward table of the pre-restart (legacy, set 0/1) achievements:
	/// id → (gold, ink, tokens). Used ONLY by <see cref="ApplyAchievementRestart"/>
	/// to pay out legacy achievements that were unlocked but never claimed.
	/// Gem rewards are already folded into tokens (1:1). Titles are deliberately
	/// absent — old-achievement titles are removed by the restart. Never edit.
	/// </summary>
	private static readonly Dictionary<string, (int Gold, int Ink, int Tokens)> LegacyRewards = new()
	{
		["catch_1"] = ( 500, 0, 0 ),
		["catch_10"] = ( 2000, 0, 0 ),
		["catch_50"] = ( 10000, 10, 0 ),
		["catch_100"] = ( 0, 0, 5 ),
		["catch_500"] = ( 0, 0, 25 ),
		["beast_complete"] = ( 0, 0, 25 ),
		["win_1"] = ( 500, 0, 0 ),
		["win_10"] = ( 2000, 0, 0 ),
		["win_100"] = ( 10000, 0, 0 ),
		["win_1000"] = ( 0, 0, 10 ),
		["damage_10k"] = ( 2000, 0, 0 ),
		["damage_100k"] = ( 10000, 0, 0 ),
		["damage_1m"] = ( 0, 0, 5 ),
		["knockouts_10"] = ( 2000, 0, 0 ),
		["knockouts_100"] = ( 10000, 0, 0 ),
		["knockouts_500"] = ( 0, 0, 10 ),
		["expedition_1"] = ( 1000, 0, 0 ),
		["expedition_5"] = ( 5000, 10, 0 ),
		["expedition_12"] = ( 0, 20, 5 ),
		["expedition_16"] = ( 0, 0, 10 ),
		["hard_mode_1"] = ( 5000, 0, 0 ),
		["hard_mode_10"] = ( 0, 0, 10 ),
		["hard_mode_16"] = ( 0, 0, 15 ),
		["expeditions_50"] = ( 10000, 0, 0 ),
		["expeditions_250"] = ( 0, 0, 10 ),
		["boss_first"] = ( 0, 0, 5 ),
		["boss_all"] = ( 0, 0, 25 ),
		["breed_1"] = ( 1000, 0, 0 ),
		["breed_10"] = ( 5000, 0, 0 ),
		["breed_50"] = ( 0, 0, 5 ),
		["breed_100"] = ( 0, 0, 10 ),
		["high_genes"] = ( 5000, 0, 0 ),
		["perfect_gene"] = ( 0, 0, 5 ),
		["gold_1k"] = ( 500, 0, 0 ),
		["gold_10k"] = ( 2000, 0, 0 ),
		["gold_100k"] = ( 0, 0, 5 ),
		["gold_1m"] = ( 0, 0, 10 ),
		["items_10"] = ( 2000, 0, 0 ),
		["three_relics"] = ( 3000, 0, 0 ),
		["server_boost"] = ( 2000, 0, 0 ),
		["boss_tokens_100"] = ( 0, 0, 25 ),
		["arena_win_1"] = ( 2000, 0, 0 ),
		["arena_win_25"] = ( 10000, 0, 0 ),
		["arena_win_100"] = ( 0, 0, 15 ),
		["win_streak_3"] = ( 5000, 0, 0 ),
		["win_streak_10"] = ( 0, 0, 10 ),
		["arena_sets_100"] = ( 0, 0, 10 ),
		["reverse_sweep"] = ( 10000, 0, 0 ),
		["trade_1"] = ( 2000, 0, 0 ),
		["trade_25"] = ( 0, 0, 5 ),
		["trade_50"] = ( 0, 0, 10 ),
		["chat_10"] = ( 1000, 0, 0 ),
		["beast_showcase"] = ( 1000, 0, 0 ),
		["cards_10"] = ( 5000, 0, 0 ),
		["level_10"] = ( 2000, 0, 0 ),
		["level_50"] = ( 10000, 0, 0 ),
		["level_100"] = ( 0, 0, 5 ),
		["level_200"] = ( 0, 0, 15 ),
		["level_250"] = ( 0, 0, 25 ),
		["skills_10"] = ( 5000, 0, 0 ),
		["skills_25"] = ( 0, 0, 5 ),
		["evolve_5"] = ( 5000, 0, 0 ),
		["evolve_50"] = ( 0, 0, 10 ),
		["veteran_max"] = ( 10000, 0, 0 ),
		["skill_points_100"] = ( 10000, 0, 0 ),
		["rank_bronze"] = ( 2000, 0, 0 ),
		["rank_silver"] = ( 4000, 0, 0 ),
		["rank_gold"] = ( 6000, 0, 0 ),
		["rank_platinum"] = ( 8000, 0, 0 ),
		["rank_diamond"] = ( 10000, 0, 10 ),
		["rank_master"] = ( 12000, 0, 12 ),
		["rank_legendary"] = ( 14000, 0, 14 ),
		["rank_mythic"] = ( 16000, 0, 16 ),
	};

	/// <summary>
	/// Titles that ONLY the legacy achievements granted. Stripped by the restart
	/// (user 2026-09-25: "we are making a new system"). None has another source —
	/// Alpha/Johnson, guild-raid and login-milestone titles are untouched, and the
	/// level-based "Master Tamer" lives in Tamer.ActiveLevelTitle, not here.
	/// Qualifying players earn Boss Slayer / Supreme Tamer / Beastborne Master /
	/// Master Fuser back through the new set.
	/// </summary>
	private static readonly string[] LegacyAchievementTitles =
	{
		"Boss Slayer", "Supreme Tamer", "Master Tamer", "Beastborne Master",
		"Conqueror", "Arena Legend", "Master Fuser", "Transcendent",
	};

	/// <summary>
	/// One-time restart onto the current achievement set, gated on
	/// Tamer.AchievementSetVersion (its OWN flag — not the shared MigrationVersion).
	/// Pure data, no manager dependencies, so TamerManager.Hydrate can call it:
	///  1. Pay out legacy achievements that were unlocked but unclaimed
	///     (gold / ink / tokens only — no titles).
	///  2. Clear tamer.Achievements. Tokens already earned are NOT touched.
	///  3. Strip legacy-achievement titles; clear ActiveTitleId if it pointed at one.
	///  4. Flag AchievementRetroPending so <see cref="RetroactiveCheck"/> re-unlocks
	///     the new set from lifetime stats once every manager has loaded.
	/// Idempotent: the version bump and the wipe land in the same save.
	/// Returns true if it ran.
	/// </summary>
	public static bool ApplyAchievementRestart( Tamer tamer )
	{
		if ( tamer == null ) return false;
		if ( tamer.AchievementSetVersion >= CURRENT_ACHIEVEMENT_SET ) return false;

		tamer.Achievements ??= new();
		tamer.UnlockedTitles ??= new();

		// 1. Legacy payout.
		long gold = 0, ink = 0, tokens = 0;
		int paid = 0;
		foreach ( var kvp in tamer.Achievements )
		{
			var p = kvp.Value;
			if ( p == null || !p.IsUnlocked || p.IsClaimed ) continue;
			if ( !LegacyRewards.TryGetValue( kvp.Key, out var r ) ) continue;
			gold += r.Gold;
			ink += r.Ink;
			tokens += r.Tokens;
			paid++;
		}
		if ( paid > 0 )
		{
			tamer.Gold = (int)Math.Min( (long)int.MaxValue, (long)tamer.Gold + gold );
			tamer.ContractInk = (int)Math.Min( (long)int.MaxValue, (long)tamer.ContractInk + ink );
			tamer.BossTokens = (int)Math.Min( (long)int.MaxValue, (long)tamer.BossTokens + tokens );
		}

		// 2. Wipe progress. No Token clawback.
		int wiped = tamer.Achievements.Count;
		tamer.Achievements.Clear();

		// 3. Strip legacy-achievement titles.
		int stripped = tamer.UnlockedTitles.RemoveAll( id => LegacyAchievementTitles.Contains( id ) );
		if ( !string.IsNullOrEmpty( tamer.ActiveTitleId ) && LegacyAchievementTitles.Contains( tamer.ActiveTitleId ) )
			tamer.ActiveTitleId = null;

		// 4. Hand off to the retroactive unlock.
		tamer.AchievementRetroPending = true;
		tamer.AchievementSetVersion = CURRENT_ACHIEVEMENT_SET;

		Log.Info( $"[Achievement] Restart to set {CURRENT_ACHIEVEMENT_SET}: wiped {wiped} entries, paid {paid} unclaimed legacy rewards ({gold}g, {ink} ink, {tokens} tokens), stripped {stripped} legacy title(s)." );

		if ( paid > 0 )
		{
			NotificationManager.Instance?.AddNotification(
				NotificationType.Success,
				"Achievements Renewed",
				$"Unclaimed rewards paid out: {gold:N0} Gold, {ink:N0} Ink, {tokens:N0} Tokens.",
				8f );
		}

		return true;
	}

	// ═══════════════════════════════════════════════════════════════
	// RETROACTIVE CHECK (step B)
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Called from GameManager.StartGame, after every manager has loaded
	/// (Beastbook discoveries, Pattern Book, species mastery). When the restart
	/// left AchievementRetroPending set, unlock every current achievement the
	/// player already qualifies for from lifetime stats. Unlocks are CLAIMABLE
	/// (not auto-granted) so each still gets its claim moment; one summary
	/// notification instead of one per achievement.
	/// </summary>
	public void RetroactiveCheck()
	{
		if ( _retroactiveCheckDone ) return;

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;
		_retroactiveCheckDone = true;

		tamer.Achievements ??= new();

		// Belt-and-braces: a save that reached here without passing through
		// TamerManager.Hydrate's restart call still gets restarted exactly once.
		ApplyAchievementRestart( tamer );

		if ( !tamer.AchievementRetroPending ) return;

		int unlocked = 0;
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

			if ( progress.IsUnlocked ) continue; // a live hook already fired this session

			progress.CurrentValue = currentValue;

			if ( currentValue >= achievement.RequiredValue )
			{
				progress.IsUnlocked = true;
				progress.IsClaimed = false; // claimable — rewards land on claim
				progress.UnlockedAt = DateTime.UtcNow;
				Sandbox.Services.Achievements.Unlock( achievement.Id );
				unlocked++;
			}
		}

		tamer.AchievementRetroPending = false;

		if ( unlocked > 0 )
		{
			NotificationManager.Instance?.AddNotification(
				NotificationType.Success,
				"Achievements Unlocked!",
				$"{unlocked} achievement{(unlocked == 1 ? "" : "s")} ready to claim — open Achievements to collect your rewards."
			);
			Log.Info( $"[Achievement] Retroactively unlocked {unlocked} achievements (claimable)" );
		}

		Stats.SetValue( "achievements-count", CountUnlocked( tamer ) );
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
			// is cost-weighted (rank × node.CostPerRank).
			AchievementRequirement.SkillPointsInvested => TamerManager.Instance?.GetTotalSkillPointsSpent() ?? 0,
			AchievementRequirement.TamerCardsCollected => tamer.CollectedCards?.Count ?? 0,
			AchievementRequirement.ArenaRankReached => GetRankNumericValue( tamer.ArenaRank ),
			AchievementRequirement.BeastiaryCompleted => BeastiaryManager.Instance != null && BeastiaryManager.Instance.GetDiscoveryCount() >= BeastiaryManager.Instance.GetTotalSpeciesCount() && BeastiaryManager.Instance.GetTotalSpeciesCount() > 0 ? 1 : 0,
			// 2026-09 restart
			AchievementRequirement.BeastbookDiscovered => BeastiaryManager.Instance?.GetDiscoveryCount() ?? 0,
			AchievementRequirement.PatternsDiscovered => MonsterManager.Instance?.DiscoveredPatterns?.Count ?? 0,
			AchievementRequirement.TributesOffered => tamer.TributesOffered,
			// Mastery level 6 = Grandmaster (see BeastiaryManager mastery table).
			AchievementRequirement.MonsterVeteranMaxRank => tamer.SpeciesMastery != null && tamer.SpeciesMastery.Values.Any( d => d != null && d.Level >= 6 ) ? 1 : 0,
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
