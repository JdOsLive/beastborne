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
	// ACHIEVEMENT DEFINITIONS
	// ═══════════════════════════════════════════════════════════════

	private void InitializeAchievements()
	{
		_achievements.Clear();
		int order = 0;

		// ── COLLECTION ──────────────────────────────────────────────

		AddAchievement( "catch_1", "First Catch", "Contract your first monster", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 500 ) } );

		AddAchievement( "catch_10", "Budding Tamer", "Contract 10 monsters", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "catch_50", "Seasoned Hunter", "Contract 50 monsters", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 50, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ), Reward( AchievementRewardType.ContractInk, 10 ) } );

		AddAchievement( "catch_100", "Master Tamer", "Contract 100 monsters", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 100, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Master Tamer" ) } );

		AddAchievement( "catch_500", "Living Legend", "Contract 500 monsters", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 500, order++,
			new() { Reward( AchievementRewardType.Gems, 25 ) } );

		// (Cut: 11 element catches + 4 rarity catches + own_same_5 — none had
		//  backing trigger code in the game. Re-add when per-element / per-rarity
		//  / OwnedSameSpecies counters are wired in MonsterManager.OnCatch.)

		AddAchievement( "beast_complete", "Beastborne Master", "Discover every species in the Beastiary", AchievementCategory.Collection,
			AchievementRequirement.BeastiaryCompleted, 1, order++,
			new() { Reward( AchievementRewardType.Gems, 25 ), Reward( AchievementRewardType.Title, 0, "Beastborne Master" ) } );

		// ── BATTLE ──────────────────────────────────────────────────

		AddAchievement( "win_1", "First Victory", "Win your first battle", AchievementCategory.Battle,
			AchievementRequirement.TotalBattlesWon, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 500 ) } );

		AddAchievement( "win_10", "Getting Good", "Win 10 battles", AchievementCategory.Battle,
			AchievementRequirement.TotalBattlesWon, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "win_100", "Centurion", "Win 100 battles", AchievementCategory.Battle,
			AchievementRequirement.TotalBattlesWon, 100, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		AddAchievement( "win_1000", "Unbreakable", "Win 1000 battles", AchievementCategory.Battle,
			AchievementRequirement.TotalBattlesWon, 1000, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ) } );

		AddAchievement( "damage_10k", "Heavy Hitter", "Deal 10,000 total damage", AchievementCategory.Battle,
			AchievementRequirement.TotalDamageDealt, 10000, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "damage_100k", "Devastator", "Deal 100,000 total damage", AchievementCategory.Battle,
			AchievementRequirement.TotalDamageDealt, 100000, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		AddAchievement( "damage_1m", "Cataclysm", "Deal 1,000,000 total damage", AchievementCategory.Battle,
			AchievementRequirement.TotalDamageDealt, 1000000, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ) } );

		AddAchievement( "knockouts_10", "Knockout Artist", "Score 10 knockouts", AchievementCategory.Battle,
			AchievementRequirement.TotalKnockouts, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "knockouts_100", "Executioner", "Score 100 knockouts", AchievementCategory.Battle,
			AchievementRequirement.TotalKnockouts, 100, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		AddAchievement( "knockouts_500", "Annihilator", "Score 500 knockouts", AchievementCategory.Battle,
			AchievementRequirement.TotalKnockouts, 500, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ) } );

		// (Cut: flawless_win, type_underdog, all_elem_battle — backing trigger
		//  code missing for WinWithoutLoss / WinWithTypeDisadvantage / UsedEveryElement.
		//  Re-add when those battle-end checks exist.)

		// ── EXPEDITION ──────────────────────────────────────────────

		AddAchievement( "expedition_1", "First Steps", "Clear Expedition 1", AchievementCategory.Expedition,
			AchievementRequirement.HighestExpeditionCleared, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 1000 ) } );

		AddAchievement( "expedition_5", "Into the Wild", "Clear the Weaverwood", AchievementCategory.Expedition,
			AchievementRequirement.HighestExpeditionCleared, 2, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ), Reward( AchievementRewardType.ContractInk, 10 ) } );

		AddAchievement( "expedition_12", "Uncharted Territory", "Clear the Weavermere", AchievementCategory.Expedition,
			AchievementRequirement.HighestExpeditionCleared, 3, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.ContractInk, 20 ) } );

		AddAchievement( "expedition_16", "Conqueror", "Clear all 3 launch expeditions", AchievementCategory.Expedition,
			AchievementRequirement.HighestExpeditionCleared, 3, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Conqueror" ) } );

		// Hard Mode step achievements check HighestHardModeCleared — an
		// order-independent COUNT of distinct expeditions Hard-cleared, not a
		// specific zone. Titles/descriptions must stay count-based to match.
		AddAchievement( "hard_mode_1", "Hard Mode Initiate", "Clear 1 expedition on Hard Mode", AchievementCategory.Expedition,
			AchievementRequirement.HighestHardModeCleared, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "hard_mode_10", "Hard Mode Veteran", "Clear 2 expeditions on Hard Mode", AchievementCategory.Expedition,
			AchievementRequirement.HighestHardModeCleared, 2, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ) } );

		AddAchievement( "hard_mode_16", "Hard Mode Master", "Clear 3 expeditions on Hard Mode", AchievementCategory.Expedition,
			AchievementRequirement.HighestHardModeCleared, 3, order++,
			new() { Reward( AchievementRewardType.Gems, 15 ) } );

		AddAchievement( "expeditions_50", "Seasoned Adventurer", "Complete 50 expeditions", AchievementCategory.Expedition,
			AchievementRequirement.ExpeditionsCompleted, 50, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		AddAchievement( "expeditions_250", "Endless Explorer", "Complete 250 expeditions", AchievementCategory.Expedition,
			AchievementRequirement.ExpeditionsCompleted, 250, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ) } );

		// Boss Slayer / Supreme Tamer — replace the old boss-token store entries.
		// Required count is hardcoded because ExpeditionManager.OnStart (which
		// populates _expeditions) hasn't run when this initializer fires.
		// LAUNCH_BOSS_COUNT must match the # of `HasBoss = true` expeditions
		// in ExpeditionManager.GenerateExpeditions(). Bump when new boss
		// expeditions ship. Currently: saltmoor_forest, old_saltmoor,
		// mini_loomweaver_burrow (the mini-expedition boss is tracked via
		// MarkBossCleared too, so it counts toward "defeat every boss").
		const int LAUNCH_BOSS_COUNT = 3;

		AddAchievement( "boss_first", "Boss Slayer", "Defeat your first expedition boss", AchievementCategory.Expedition,
			AchievementRequirement.BossesCleared, 1, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Boss Slayer" ) } );

		AddAchievement( "boss_all", "Supreme Tamer", "Defeat every expedition boss at least once", AchievementCategory.Expedition,
			AchievementRequirement.BossesCleared, LAUNCH_BOSS_COUNT, order++,
			new() { Reward( AchievementRewardType.Gems, 25 ), Reward( AchievementRewardType.Title, 0, "Supreme Tamer" ) } );

		// (Cut: no_catch_run — ExpeditionWithoutCatch trigger code missing.)

		// ── FUSING ──────────────────────────────────────────────

		AddAchievement( "breed_1", "First Offspring", "Fuse your first monster", AchievementCategory.Breeding,
			AchievementRequirement.TotalMonstersBred, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 1000 ) } );

		AddAchievement( "breed_10", "Growing Family", "Fuse 10 monsters", AchievementCategory.Breeding,
			AchievementRequirement.TotalMonstersBred, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "breed_50", "Genetics Expert", "Fuse 50 monsters", AchievementCategory.Breeding,
			AchievementRequirement.TotalMonstersBred, 50, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ) } );

		AddAchievement( "breed_100", "Master Fuser", "Fuse 100 monsters", AchievementCategory.Breeding,
			AchievementRequirement.TotalMonstersBred, 100, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Master Fuser" ) } );

		AddAchievement( "high_genes", "Good Genes", "Fuse a monster with 25+ total genes", AchievementCategory.Breeding,
			AchievementRequirement.BredHighGenes, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "perfect_gene", "Perfection", "Fuse a monster with a perfect gene (30)", AchievementCategory.Breeding,
			AchievementRequirement.BredPerfectGene, 1, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ) } );

		// (Cut: got_twins, rare_trait — GotTwins + BredRareTrait trigger code missing.)

		// ── ECONOMY ──────────────────────────────────────────────

		AddAchievement( "gold_1k", "First Fortune", "Earn 1,000 total gold", AchievementCategory.Economy,
			AchievementRequirement.TotalGoldEarned, 1000, order++,
			new() { Reward( AchievementRewardType.Gold, 500 ) } );

		AddAchievement( "gold_10k", "Comfortable", "Earn 10,000 total gold", AchievementCategory.Economy,
			AchievementRequirement.TotalGoldEarned, 10000, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "gold_100k", "Wealthy Tamer", "Earn 100,000 total gold", AchievementCategory.Economy,
			AchievementRequirement.TotalGoldEarned, 100000, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ) } );

		AddAchievement( "gold_1m", "Beastborne Millionaire", "Earn 1,000,000 total gold", AchievementCategory.Economy,
			AchievementRequirement.TotalGoldEarned, 1000000, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ) } );

		// (Cut gold_1b — 1B is functionally unreachable in alpha; was aspirational filler.)

		AddAchievement( "items_10", "Shopper", "Buy 10 items from the shop", AchievementCategory.Economy,
			AchievementRequirement.TotalItemsBought, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		// (Cut items_50 — tier trim; items_10 is the meaningful first milestone.)

		AddAchievement( "three_relics", "Fully Equipped", "Equip 3 relics simultaneously", AchievementCategory.Economy,
			AchievementRequirement.EquippedThreeRelics, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 3000 ) } );

		AddAchievement( "server_boost", "Community Spirit", "Use a server boost", AchievementCategory.Economy,
			AchievementRequirement.UsedServerBoost, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "boss_tokens_100", "Token Collector", "Spend 100 Boss Tokens", AchievementCategory.Economy,
			AchievementRequirement.BossTokensSpent, 100, order++,
			new() { Reward( AchievementRewardType.BossTokens, 25 ) } );

		// ── ARENA / RANKED ──────────────────────────────────────────

		AddAchievement( "arena_win_1", "Arena Debut", "Win your first ranked set", AchievementCategory.Arena,
			AchievementRequirement.ArenaWins, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "arena_win_25", "Arena Warrior", "Win 25 ranked sets", AchievementCategory.Arena,
			AchievementRequirement.ArenaWins, 25, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		AddAchievement( "arena_win_100", "Arena Legend", "Win 100 ranked sets", AchievementCategory.Arena,
			AchievementRequirement.ArenaWins, 100, order++,
			new() { Reward( AchievementRewardType.Gems, 15 ), Reward( AchievementRewardType.Title, 0, "Arena Legend" ) } );

		AddRankAchievement( "rank_bronze", "Bronze League", "Reach Bronze rank", "Bronze", order++ );
		AddRankAchievement( "rank_silver", "Silver League", "Reach Silver rank", "Silver", order++ );
		AddRankAchievement( "rank_gold", "Gold League", "Reach Gold rank", "Gold", order++ );
		AddRankAchievement( "rank_platinum", "Platinum League", "Reach Platinum rank", "Platinum", order++ );
		AddRankAchievement( "rank_diamond", "Diamond League", "Reach Diamond rank", "Diamond", order++ );
		AddRankAchievement( "rank_master", "Master League", "Reach Master rank", "Master", order++ );
		AddRankAchievement( "rank_legendary", "Legendary League", "Reach Legendary rank", "Legendary", order++ );
		AddRankAchievement( "rank_mythic", "Mythic League", "Reach Mythic rank", "Mythic", order++ );

		AddAchievement( "win_streak_3", "On a Roll", "Win 3 ranked sets in a row", AchievementCategory.Arena,
			AchievementRequirement.ArenaWinStreak, 3, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "win_streak_10", "Unstoppable", "Win 10 ranked sets in a row", AchievementCategory.Arena,
			AchievementRequirement.ArenaWinStreak, 10, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ) } );

		// (Cut arena_vs_higher — ArenaWinVsHigherRank trigger code missing.)

		AddAchievement( "arena_sets_100", "Arena Veteran", "Complete 100 ranked sets", AchievementCategory.Arena,
			AchievementRequirement.ArenaSetsCompleted, 100, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ) } );

		AddAchievement( "reverse_sweep", "Reverse Sweep", "Come back from a 0-1 deficit to win a ranked set 2-1", AchievementCategory.Arena,
			AchievementRequirement.ArenaReverseSweep, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		// ── SOCIAL / ONLINE ──────────────────────────────────────────

		AddAchievement( "trade_1", "First Trade", "Complete your first trade", AchievementCategory.Social,
			AchievementRequirement.TotalTradesCompleted, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "trade_25", "Merchant", "Complete 25 trades", AchievementCategory.Social,
			AchievementRequirement.TotalTradesCompleted, 25, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ) } );

		AddAchievement( "trade_50", "Trade Baron", "Complete 50 trades", AchievementCategory.Social,
			AchievementRequirement.TotalTradesCompleted, 50, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ) } );

		AddAchievement( "chat_10", "Social Butterfly", "Send 10 chat messages", AchievementCategory.Social,
			AchievementRequirement.ChatMessagesSent, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 1000 ) } );

		AddAchievement( "beast_showcase", "Show and Tell", "Showcase a beast in chat", AchievementCategory.Social,
			AchievementRequirement.BeastShowcased, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 1000 ) } );

		AddAchievement( "cards_10", "Card Collector", "Collect 10 tamer cards", AchievementCategory.Social,
			AchievementRequirement.TamerCardsCollected, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		// ── MASTERY ──────────────────────────────────────────────

		AddAchievement( "level_10", "Apprentice", "Reach Tamer Level 10", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "level_50", "Expert Tamer", "Reach Tamer Level 50", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 50, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		AddAchievement( "level_100", "Centurion Tamer", "Reach Tamer Level 100", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 100, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ) } );

		AddAchievement( "level_200", "Legendary Tamer", "Reach Tamer Level 200", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 200, order++,
			new() { Reward( AchievementRewardType.Gems, 15 ) } );

		AddAchievement( "level_250", "Max Level", "Reach Tamer Level 250", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 250, order++,
			new() { Reward( AchievementRewardType.Gems, 25 ), Reward( AchievementRewardType.Title, 0, "Transcendent" ) } );

		AddAchievement( "skills_10", "Skill Student", "Unlock 10 skills", AchievementCategory.Mastery,
			AchievementRequirement.SkillsUnlocked, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "skills_25", "Skill Master", "Unlock 25 skills", AchievementCategory.Mastery,
			AchievementRequirement.SkillsUnlocked, 25, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ) } );

		AddAchievement( "evolve_5", "Evolution Theory", "Evolve 5 monsters", AchievementCategory.Mastery,
			AchievementRequirement.MonstersEvolved, 5, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "evolve_50", "Evolution Master", "Evolve 50 monsters", AchievementCategory.Mastery,
			AchievementRequirement.MonstersEvolved, 50, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ) } );

		AddAchievement( "veteran_max", "Grandmaster Scholar", "Reach Grandmaster mastery on any species", AchievementCategory.Mastery,
			AchievementRequirement.MonsterVeteranMaxRank, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		AddAchievement( "skill_points_100", "Point Hoarder", "Invest 100 skill points", AchievementCategory.Mastery,
			AchievementRequirement.SkillPointsInvested, 100, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		// (Cut: skill_points_250 — tier trim. skill_points_100 covers the "deep
		//  investment" milestone.)

		// ── SECRET ──────────────────────────────────────────────
		// (All 5 secrets removed — none had backing trigger code. Each one
		//  needs custom detection logic per condition. Re-add when wired:
		//   - Night Owl: total play-time tracking
		//   - Full House: roster-composition check
		//   - Natural Beauty: level-up gate that checks evolved-count
		//   - Mono Master: expedition-clear check with team-element filter
		//   - Lucky Seven: roster check (7 monsters all at exactly level 7))
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

	private void AddRankAchievement( string id, string name, string desc, string rank, int order )
	{
		int rankValue = rank switch
		{
			"Bronze" => 1,
			"Silver" => 2,
			"Gold" => 3,
			"Platinum" => 4,
			"Diamond" => 5,
			"Master" => 6,
			"Legendary" => 7,
			"Mythic" => 8,
			_ => 0
		};

		var rewards = new List<AchievementReward> { Reward( AchievementRewardType.Gold, rankValue * 2000 ) };

		if ( rankValue >= 5 )
			rewards.Add( Reward( AchievementRewardType.Gems, rankValue * 2 ) );

		AddAchievement( id, name, desc, AchievementCategory.Arena, AchievementRequirement.ArenaRankReached, rankValue, order, rewards );
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
		int unlockedCount = tamer.Achievements.Values.Count( p => p.IsUnlocked );
		Stats.SetValue( "achievements-count", unlockedCount );

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
		return tamer.Achievements.Values.Count( p => p.IsUnlocked && !p.IsClaimed );
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
				tamer.Gems += reward.Value;
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
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer?.Achievements == null ) return 0;
		return tamer.Achievements.Values.Count( p => p.IsUnlocked );
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
