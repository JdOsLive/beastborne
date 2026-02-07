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

		AddAchievement( "catch_1", "First Catch", "Catch your first monster", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 500 ) } );

		AddAchievement( "catch_10", "Budding Tamer", "Catch 10 monsters", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "catch_25", "Monster Collector", "Catch 25 monsters", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 25, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ), Reward( AchievementRewardType.Title, 0, "Collector" ) } );

		AddAchievement( "catch_50", "Seasoned Hunter", "Catch 50 monsters", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 50, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ), Reward( AchievementRewardType.ContractInk, 10 ) } );

		AddAchievement( "catch_100", "Master Tamer", "Catch 100 monsters", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 100, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Master Tamer" ) } );

		AddAchievement( "catch_250", "Beast Wrangler", "Catch 250 monsters", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 250, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Beast Wrangler" ) } );

		AddAchievement( "catch_500", "Living Legend", "Catch 500 monsters", AchievementCategory.Collection,
			AchievementRequirement.TotalMonstersCaught, 500, order++,
			new() { Reward( AchievementRewardType.Gems, 25 ), Reward( AchievementRewardType.Title, 0, "Living Legend" ) } );

		// Element catches
		AddElementAchievement( "catch_fire", "Flame Finder", "Catch a Fire element monster", AchievementRequirement.CaughtElementFire, order++ );
		AddElementAchievement( "catch_water", "Wave Rider", "Catch a Water element monster", AchievementRequirement.CaughtElementWater, order++ );
		AddElementAchievement( "catch_earth", "Ground Breaker", "Catch an Earth element monster", AchievementRequirement.CaughtElementEarth, order++ );
		AddElementAchievement( "catch_wind", "Storm Chaser", "Catch a Wind element monster", AchievementRequirement.CaughtElementWind, order++ );
		AddElementAchievement( "catch_electric", "Lightning Rod", "Catch an Electric element monster", AchievementRequirement.CaughtElementElectric, order++ );
		AddElementAchievement( "catch_ice", "Frost Seeker", "Catch an Ice element monster", AchievementRequirement.CaughtElementIce, order++ );
		AddElementAchievement( "catch_nature", "Green Thumb", "Catch a Nature element monster", AchievementRequirement.CaughtElementNature, order++ );
		AddElementAchievement( "catch_metal", "Iron Will", "Catch a Metal element monster", AchievementRequirement.CaughtElementMetal, order++ );
		AddElementAchievement( "catch_shadow", "Shadow Walker", "Catch a Shadow element monster", AchievementRequirement.CaughtElementShadow, order++ );
		AddElementAchievement( "catch_spirit", "Spirit Guide", "Catch a Spirit element monster", AchievementRequirement.CaughtElementSpirit, order++ );
		AddElementAchievement( "catch_neutral", "Plain Sight", "Catch a Neutral element monster", AchievementRequirement.CaughtElementNeutral, order++ );

		// Rarity catches
		AddAchievement( "catch_rare", "Rare Find", "Catch a Rare monster", AchievementCategory.Collection,
			AchievementRequirement.CaughtRarityRare, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 3000 ) } );

		AddAchievement( "catch_epic", "Epic Discovery", "Catch an Epic monster", AchievementCategory.Collection,
			AchievementRequirement.CaughtRarityEpic, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ), Reward( AchievementRewardType.ContractInk, 5 ) } );

		AddAchievement( "catch_legendary", "Legendary Encounter", "Catch a Legendary monster", AchievementCategory.Collection,
			AchievementRequirement.CaughtRarityLegendary, 1, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Legend Hunter" ) } );

		AddAchievement( "catch_mythic", "Myth Made Real", "Catch a Mythic monster", AchievementCategory.Collection,
			AchievementRequirement.CaughtRarityMythic, 1, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Mythic Hunter" ) } );

		AddAchievement( "beast_complete", "Beastborne Master", "Discover every species in the Beastiary", AchievementCategory.Collection,
			AchievementRequirement.BeastiaryCompleted, 1, order++,
			new() { Reward( AchievementRewardType.Gems, 25 ), Reward( AchievementRewardType.Title, 0, "Beastborne Master" ) } );

		AddAchievement( "own_same_5", "Dedicated Fuser", "Own 5 monsters of the same species", AchievementCategory.Collection,
			AchievementRequirement.OwnedSameSpecies, 5, order++,
			new() { Reward( AchievementRewardType.Gold, 3000 ) } );

		// ── BATTLE ──────────────────────────────────────────────────

		AddAchievement( "win_1", "First Victory", "Win your first battle", AchievementCategory.Battle,
			AchievementRequirement.TotalBattlesWon, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 500 ) } );

		AddAchievement( "win_10", "Getting Good", "Win 10 battles", AchievementCategory.Battle,
			AchievementRequirement.TotalBattlesWon, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "win_50", "Veteran Fighter", "Win 50 battles", AchievementCategory.Battle,
			AchievementRequirement.TotalBattlesWon, 50, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ), Reward( AchievementRewardType.Title, 0, "Veteran Fighter" ) } );

		AddAchievement( "win_100", "Centurion", "Win 100 battles", AchievementCategory.Battle,
			AchievementRequirement.TotalBattlesWon, 100, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		AddAchievement( "win_500", "Warborn", "Win 500 battles", AchievementCategory.Battle,
			AchievementRequirement.TotalBattlesWon, 500, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Warborn" ) } );

		AddAchievement( "win_1000", "Unbreakable", "Win 1000 battles", AchievementCategory.Battle,
			AchievementRequirement.TotalBattlesWon, 1000, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Unbreakable" ) } );

		AddAchievement( "damage_10k", "Heavy Hitter", "Deal 10,000 total damage", AchievementCategory.Battle,
			AchievementRequirement.TotalDamageDealt, 10000, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "damage_100k", "Devastator", "Deal 100,000 total damage", AchievementCategory.Battle,
			AchievementRequirement.TotalDamageDealt, 100000, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ), Reward( AchievementRewardType.Title, 0, "Devastator" ) } );

		AddAchievement( "damage_1m", "Cataclysm", "Deal 1,000,000 total damage", AchievementCategory.Battle,
			AchievementRequirement.TotalDamageDealt, 1000000, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Cataclysm" ) } );

		AddAchievement( "knockouts_10", "Knockout Artist", "Score 10 knockouts", AchievementCategory.Battle,
			AchievementRequirement.TotalKnockouts, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "knockouts_50", "Ring Master", "Score 50 knockouts", AchievementCategory.Battle,
			AchievementRequirement.TotalKnockouts, 50, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "knockouts_100", "Executioner", "Score 100 knockouts", AchievementCategory.Battle,
			AchievementRequirement.TotalKnockouts, 100, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ), Reward( AchievementRewardType.Title, 0, "Executioner" ) } );

		AddAchievement( "knockouts_500", "Annihilator", "Score 500 knockouts", AchievementCategory.Battle,
			AchievementRequirement.TotalKnockouts, 500, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Annihilator" ) } );

		AddAchievement( "flawless_win", "Flawless Victory", "Win a battle without losing a monster", AchievementCategory.Battle,
			AchievementRequirement.WinWithoutLoss, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ), Reward( AchievementRewardType.Title, 0, "Flawless" ) } );

		AddAchievement( "type_underdog", "Against the Odds", "Win a battle with a type disadvantage", AchievementCategory.Battle,
			AchievementRequirement.WinWithTypeDisadvantage, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 3000 ) } );

		AddAchievement( "all_elem_battle", "Elemental Master", "Use every element type in battle", AchievementCategory.Battle,
			AchievementRequirement.UsedEveryElement, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ), Reward( AchievementRewardType.Title, 0, "Elemental Master" ) } );

		// ── EXPEDITION ──────────────────────────────────────────────

		AddAchievement( "expedition_1", "First Steps", "Clear Expedition 1", AchievementCategory.Expedition,
			AchievementRequirement.HighestExpeditionCleared, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 1000 ) } );

		AddAchievement( "expedition_3", "Trailblazer", "Clear Expedition 3", AchievementCategory.Expedition,
			AchievementRequirement.HighestExpeditionCleared, 3, order++,
			new() { Reward( AchievementRewardType.Gold, 3000 ) } );

		AddAchievement( "expedition_5", "Into the Wild", "Clear Expedition 5", AchievementCategory.Expedition,
			AchievementRequirement.HighestExpeditionCleared, 5, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ), Reward( AchievementRewardType.ContractInk, 10 ) } );

		AddAchievement( "expedition_8", "Deep Explorer", "Clear Expedition 8", AchievementCategory.Expedition,
			AchievementRequirement.HighestExpeditionCleared, 8, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		AddAchievement( "expedition_12", "Uncharted Territory", "Clear Expedition 12", AchievementCategory.Expedition,
			AchievementRequirement.HighestExpeditionCleared, 12, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.ContractInk, 20 ) } );

		AddAchievement( "expedition_16", "Conqueror", "Clear all 16 Expeditions", AchievementCategory.Expedition,
			AchievementRequirement.HighestExpeditionCleared, 16, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Conqueror" ) } );

		AddAchievement( "hard_mode_1", "Hard Knocks", "Clear Hard Mode Expedition 1", AchievementCategory.Expedition,
			AchievementRequirement.HighestHardModeCleared, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "hard_mode_5", "Iron Explorer", "Clear Hard Mode Expedition 5", AchievementCategory.Expedition,
			AchievementRequirement.HighestHardModeCleared, 5, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Iron Explorer" ) } );

		AddAchievement( "hard_mode_10", "Unbreakable Explorer", "Clear Hard Mode Expedition 10", AchievementCategory.Expedition,
			AchievementRequirement.HighestHardModeCleared, 10, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Unbreakable Explorer" ) } );

		AddAchievement( "hard_mode_16", "Absolute Legend", "Clear all 16 Hard Mode Expeditions", AchievementCategory.Expedition,
			AchievementRequirement.HighestHardModeCleared, 16, order++,
			new() { Reward( AchievementRewardType.Gems, 15 ), Reward( AchievementRewardType.Title, 0, "Absolute Legend" ) } );

		AddAchievement( "expeditions_50", "Seasoned Adventurer", "Complete 50 expeditions", AchievementCategory.Expedition,
			AchievementRequirement.ExpeditionsCompleted, 50, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		AddAchievement( "expeditions_100", "Expedition Veteran", "Complete 100 expeditions", AchievementCategory.Expedition,
			AchievementRequirement.ExpeditionsCompleted, 100, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Expedition Veteran" ) } );

		AddAchievement( "expeditions_250", "Endless Explorer", "Complete 250 expeditions", AchievementCategory.Expedition,
			AchievementRequirement.ExpeditionsCompleted, 250, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Endless Explorer" ) } );

		AddAchievement( "no_catch_run", "Pacifist Run", "Complete an expedition without catching anything", AchievementCategory.Expedition,
			AchievementRequirement.ExpeditionWithoutCatch, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "all_bosses", "Boss Slayer", "Defeat every boss", AchievementCategory.Expedition,
			AchievementRequirement.AllBossesDefeated, 1, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.BossTokens, 50 ), Reward( AchievementRewardType.Title, 0, "Boss Slayer" ) } );

		// ── FUSING ──────────────────────────────────────────────

		AddAchievement( "breed_1", "First Offspring", "Fuse your first monster", AchievementCategory.Breeding,
			AchievementRequirement.TotalMonstersBred, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 1000 ) } );

		AddAchievement( "breed_10", "Growing Family", "Fuse 10 monsters", AchievementCategory.Breeding,
			AchievementRequirement.TotalMonstersBred, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "breed_25", "Beast Fuser", "Fuse 25 monsters", AchievementCategory.Breeding,
			AchievementRequirement.TotalMonstersBred, 25, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ), Reward( AchievementRewardType.Title, 0, "Beast Fuser" ) } );

		AddAchievement( "breed_50", "Genetics Expert", "Fuse 50 monsters", AchievementCategory.Breeding,
			AchievementRequirement.TotalMonstersBred, 50, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Genetics Expert" ) } );

		AddAchievement( "breed_100", "Master Fuser", "Fuse 100 monsters", AchievementCategory.Breeding,
			AchievementRequirement.TotalMonstersBred, 100, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Master Fuser" ) } );

		AddAchievement( "high_genes", "Good Genes", "Fuse a monster with 25+ total genes", AchievementCategory.Breeding,
			AchievementRequirement.BredHighGenes, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "perfect_gene", "Perfection", "Fuse a monster with a perfect gene (30)", AchievementCategory.Breeding,
			AchievementRequirement.BredPerfectGene, 1, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Perfectionist" ) } );

		AddAchievement( "got_twins", "Double Trouble", "Get twins from fusing", AchievementCategory.Breeding,
			AchievementRequirement.GotTwins, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "rare_trait", "Lucky Genes", "Fuse a monster with a rare or higher trait", AchievementCategory.Breeding,
			AchievementRequirement.BredRareTrait, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		// ── ECONOMY ──────────────────────────────────────────────

		AddAchievement( "gold_1k", "First Fortune", "Earn 1,000 total gold", AchievementCategory.Economy,
			AchievementRequirement.TotalGoldEarned, 1000, order++,
			new() { Reward( AchievementRewardType.Gold, 500 ) } );

		AddAchievement( "gold_10k", "Comfortable", "Earn 10,000 total gold", AchievementCategory.Economy,
			AchievementRequirement.TotalGoldEarned, 10000, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "gold_100k", "Wealthy Tamer", "Earn 100,000 total gold", AchievementCategory.Economy,
			AchievementRequirement.TotalGoldEarned, 100000, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Wealthy" ) } );

		AddAchievement( "gold_1m", "Beastborne Millionaire", "Earn 1,000,000 total gold", AchievementCategory.Economy,
			AchievementRequirement.TotalGoldEarned, 1000000, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Millionaire" ) } );

		AddAchievement( "gold_1b", "Beastborne Billionaire", "Earn 1,000,000,000 total gold", AchievementCategory.Economy,
			AchievementRequirement.TotalGoldEarned, 1000000000, order++,
			new() { Reward( AchievementRewardType.Gems, 100 ), Reward( AchievementRewardType.Title, 0, "Billionaire" ), Reward( AchievementRewardType.Monster, 50, "chromedragon" ) } );

		AddAchievement( "items_10", "Shopper", "Buy 10 items from the shop", AchievementCategory.Economy,
			AchievementRequirement.TotalItemsBought, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "items_50", "Big Spender", "Buy 50 items from the shop", AchievementCategory.Economy,
			AchievementRequirement.TotalItemsBought, 50, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ), Reward( AchievementRewardType.Title, 0, "Big Spender" ) } );

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

		AddAchievement( "arena_win_5", "Arena Regular", "Win 5 ranked sets", AchievementCategory.Arena,
			AchievementRequirement.ArenaWins, 5, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "arena_win_25", "Arena Warrior", "Win 25 ranked sets", AchievementCategory.Arena,
			AchievementRequirement.ArenaWins, 25, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ), Reward( AchievementRewardType.Title, 0, "Arena Warrior" ) } );

		AddAchievement( "arena_win_50", "Arena Champion", "Win 50 ranked sets", AchievementCategory.Arena,
			AchievementRequirement.ArenaWins, 50, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Arena Champion" ) } );

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

		AddAchievement( "win_streak_5", "Hot Streak", "Win 5 ranked sets in a row", AchievementCategory.Arena,
			AchievementRequirement.ArenaWinStreak, 5, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ), Reward( AchievementRewardType.Title, 0, "On Fire" ) } );

		AddAchievement( "win_streak_10", "Unstoppable", "Win 10 ranked sets in a row", AchievementCategory.Arena,
			AchievementRequirement.ArenaWinStreak, 10, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Unstoppable" ) } );

		AddAchievement( "arena_vs_higher", "Giant Killer", "Win a ranked set against a higher-ranked player", AchievementCategory.Arena,
			AchievementRequirement.ArenaWinVsHigherRank, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ), Reward( AchievementRewardType.Title, 0, "Giant Killer" ) } );

		AddAchievement( "arena_sets_100", "Arena Veteran", "Complete 100 ranked sets", AchievementCategory.Arena,
			AchievementRequirement.ArenaSetsCompleted, 100, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Arena Veteran" ) } );

		AddAchievement( "reverse_sweep", "Reverse Sweep", "Come back from a 0-1 deficit to win a ranked set 2-1", AchievementCategory.Arena,
			AchievementRequirement.ArenaReverseSweep, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ), Reward( AchievementRewardType.Title, 0, "Comeback King" ) } );

		// ── SOCIAL / ONLINE ──────────────────────────────────────────

		AddAchievement( "trade_1", "First Trade", "Complete your first trade", AchievementCategory.Social,
			AchievementRequirement.TotalTradesCompleted, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "trade_5", "Trading Partner", "Complete 5 trades", AchievementCategory.Social,
			AchievementRequirement.TotalTradesCompleted, 5, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "trade_25", "Merchant", "Complete 25 trades", AchievementCategory.Social,
			AchievementRequirement.TotalTradesCompleted, 25, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Merchant" ) } );

		AddAchievement( "trade_50", "Trade Baron", "Complete 50 trades", AchievementCategory.Social,
			AchievementRequirement.TotalTradesCompleted, 50, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Trade Baron" ) } );

		AddAchievement( "chat_10", "Social Butterfly", "Send 10 chat messages", AchievementCategory.Social,
			AchievementRequirement.ChatMessagesSent, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 1000 ) } );

		AddAchievement( "chat_50", "Chatterbox", "Send 50 chat messages", AchievementCategory.Social,
			AchievementRequirement.ChatMessagesSent, 50, order++,
			new() { Reward( AchievementRewardType.Gold, 3000 ), Reward( AchievementRewardType.Title, 0, "Chatterbox" ) } );

		AddAchievement( "beast_showcase", "Show and Tell", "Showcase a beast in chat", AchievementCategory.Social,
			AchievementRequirement.BeastShowcased, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 1000 ) } );

		AddAchievement( "cards_10", "Card Collector", "Collect 10 tamer cards", AchievementCategory.Social,
			AchievementRequirement.TamerCardsCollected, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "cards_25", "Social Network", "Collect 25 tamer cards", AchievementCategory.Social,
			AchievementRequirement.TamerCardsCollected, 25, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Social Network" ) } );

		// ── MASTERY ──────────────────────────────────────────────

		AddAchievement( "level_10", "Apprentice", "Reach Tamer Level 10", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 2000 ) } );

		AddAchievement( "level_25", "Journeyman", "Reach Tamer Level 25", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 25, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "level_50", "Expert Tamer", "Reach Tamer Level 50", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 50, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ), Reward( AchievementRewardType.Title, 0, "Expert" ) } );

		AddAchievement( "level_100", "Centurion Tamer", "Reach Tamer Level 100", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 100, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Centurion" ) } );

		AddAchievement( "level_150", "Grand Tamer", "Reach Tamer Level 150", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 150, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Grand Tamer" ) } );

		AddAchievement( "level_200", "Legendary Tamer", "Reach Tamer Level 200", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 200, order++,
			new() { Reward( AchievementRewardType.Gems, 15 ), Reward( AchievementRewardType.Title, 0, "Legendary Tamer" ) } );

		AddAchievement( "level_250", "Max Level", "Reach Tamer Level 250", AchievementCategory.Mastery,
			AchievementRequirement.TamerLevel, 250, order++,
			new() { Reward( AchievementRewardType.Gems, 25 ), Reward( AchievementRewardType.Title, 0, "Transcendent" ) } );

		AddAchievement( "skills_10", "Skill Student", "Unlock 10 skills", AchievementCategory.Mastery,
			AchievementRequirement.SkillsUnlocked, 10, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "skills_25", "Skill Master", "Unlock 25 skills", AchievementCategory.Mastery,
			AchievementRequirement.SkillsUnlocked, 25, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Skill Master" ) } );

		AddAchievement( "evolve_5", "Evolution Theory", "Evolve 5 monsters", AchievementCategory.Mastery,
			AchievementRequirement.MonstersEvolved, 5, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ) } );

		AddAchievement( "evolve_25", "Darwin's Heir", "Evolve 25 monsters", AchievementCategory.Mastery,
			AchievementRequirement.MonstersEvolved, 25, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Darwin's Heir" ) } );

		AddAchievement( "evolve_50", "Evolution Master", "Evolve 50 monsters", AchievementCategory.Mastery,
			AchievementRequirement.MonstersEvolved, 50, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Evolution Master" ) } );

		AddAchievement( "veteran_max", "Battle Hardened", "Have a monster reach maximum Veteran Rank", AchievementCategory.Mastery,
			AchievementRequirement.MonsterVeteranMaxRank, 1, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ), Reward( AchievementRewardType.Title, 0, "Battle Hardened" ) } );

		AddAchievement( "skill_points_100", "Point Hoarder", "Invest 100 skill points", AchievementCategory.Mastery,
			AchievementRequirement.SkillPointsInvested, 100, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ) } );

		AddAchievement( "skill_points_250", "Point Master", "Invest 250 skill points", AchievementCategory.Mastery,
			AchievementRequirement.SkillPointsInvested, 250, order++,
			new() { Reward( AchievementRewardType.Gems, 10 ), Reward( AchievementRewardType.Title, 0, "Point Master" ) } );

		// ── SECRET ──────────────────────────────────────────────

		AddAchievement( "secret_night_owl", "Night Owl", "Play for 24 hours total", AchievementCategory.Secret,
			AchievementRequirement.SecretCondition, 1, order++,
			new() { Reward( AchievementRewardType.Title, 0, "Night Owl" ) }, isSecret: true );

		AddAchievement( "secret_full_team", "Full House", "Have a full team of 3 monsters all the same species", AchievementCategory.Secret,
			AchievementRequirement.SecretCondition, 2, order++,
			new() { Reward( AchievementRewardType.Gold, 5000 ), Reward( AchievementRewardType.Title, 0, "Full House" ) }, isSecret: true );

		AddAchievement( "secret_no_evolve", "Natural Beauty", "Reach Tamer Level 50 without evolving any monster", AchievementCategory.Secret,
			AchievementRequirement.SecretCondition, 3, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ), Reward( AchievementRewardType.Title, 0, "Naturalist" ) }, isSecret: true );

		AddAchievement( "secret_mono", "Mono Master", "Clear Expedition 5 with a team of only one element", AchievementCategory.Secret,
			AchievementRequirement.SecretCondition, 4, order++,
			new() { Reward( AchievementRewardType.Gold, 10000 ), Reward( AchievementRewardType.Title, 0, "Mono Master" ) }, isSecret: true );

		AddAchievement( "secret_lucky7", "Lucky Seven", "Have 7 monsters all at level 7", AchievementCategory.Secret,
			AchievementRequirement.SecretCondition, 5, order++,
			new() { Reward( AchievementRewardType.Gems, 5 ) }, isSecret: true );
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

	private void AddElementAchievement( string id, string name, string desc, AchievementRequirement req, int order )
	{
		AddAchievement( id, name, desc, AchievementCategory.Collection, req, 1, order,
			new() { Reward( AchievementRewardType.Gold, 1000 ) } );
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

		rewards.Add( Reward( AchievementRewardType.Title, 0, $"{rank} Champion" ) );

		AddAchievement( id, name, desc, AchievementCategory.Arena, AchievementRequirement.ArenaRankReached, rankValue, order, rewards );
	}

	private static AchievementReward Reward( AchievementRewardType type, int value, string itemOrSpeciesId = null )
	{
		return new AchievementReward
		{
			Type = type,
			Value = value,
			ItemId = type == AchievementRewardType.Item || type == AchievementRewardType.Title || type == AchievementRewardType.Theme ? itemOrSpeciesId : null,
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

		// Show notification
		NotificationManager.Instance?.AddNotification(
			NotificationType.Success,
			"Achievement Unlocked!",
			$"{achievement.Name} — Claim your rewards!"
		);

		// Announce in chat
		var playerName = TamerManager.Instance?.CurrentTamer?.Name ?? "Player";
		ChatManager.Instance?.AnnounceMilestone( playerName,
			$"unlocked the achievement: {achievement.Name}!"
		);

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
				if ( !string.IsNullOrEmpty( reward.ItemId ) && !tamer.UnlockedTitles.Contains( reward.ItemId ) )
					tamer.UnlockedTitles.Add( reward.ItemId );
				break;
			case AchievementRewardType.Theme:
				if ( !string.IsNullOrEmpty( reward.ItemId ) && !tamer.UnlockedThemes.Contains( reward.ItemId ) )
					tamer.UnlockedThemes.Add( reward.ItemId );
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

		// Migrate existing unlocked achievements to claimed (they got auto-rewards from old system)
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
			if ( migrated )
			{
				TamerManager.Instance?.SaveToCloud();
				Log.Info( "[Achievement] Migrated existing unlocked achievements to claimed state" );
			}
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

		if ( unlocked > 0 )
		{
			NotificationManager.Instance?.AddNotification(
				NotificationType.Success,
				"Achievements Unlocked!",
				$"{unlocked} achievements retroactively unlocked! Check your rewards."
			);

			Stats.SetValue( "achievements-count", tamer.Achievements.Values.Count( p => p.IsUnlocked ) );
			TamerManager.Instance?.SaveToCloud();

			Log.Info( $"[Achievement] Retroactively unlocked {unlocked} achievements" );
		}
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
			AchievementRequirement.ArenaWins => tamer.ArenaWins,
			AchievementRequirement.TamerLevel => tamer.Level,
			AchievementRequirement.TotalGoldEarned => tamer.TotalGoldEarned,
			AchievementRequirement.TotalItemsBought => tamer.TotalItemsBought,
			AchievementRequirement.ExpeditionsCompleted => tamer.TotalExpeditionsCompleted,
			AchievementRequirement.TotalTradesCompleted => tamer.TotalTradesCompleted,
			AchievementRequirement.MiniGamesPlayed => tamer.TotalMiniGamesPlayed,
			AchievementRequirement.ChatMessagesSent => tamer.ChatMessagesSent,
			AchievementRequirement.BossTokensSpent => tamer.BossTokensSpent,
			AchievementRequirement.TotalDamageDealt => tamer.TotalDamageDealt,
			AchievementRequirement.TotalKnockouts => tamer.TotalKnockouts,
			AchievementRequirement.ArenaWinStreak => tamer.ArenaWinStreak,
			AchievementRequirement.ArenaSetsCompleted => tamer.ArenaSetsCompleted,
			AchievementRequirement.SkillsUnlocked => tamer.SkillRanks?.Count ?? 0,
			AchievementRequirement.SkillPointsInvested => tamer.SkillRanks?.Values.Sum() ?? 0,
			AchievementRequirement.TamerCardsCollected => tamer.CollectedCards?.Count ?? 0,
			AchievementRequirement.ArenaRankReached => GetRankNumericValue( tamer.ArenaRank ),
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
