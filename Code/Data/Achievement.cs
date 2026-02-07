using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Categories for organizing achievements
/// </summary>
public enum AchievementCategory
{
	Collection,
	Battle,
	Expedition,
	Breeding,
	Economy,
	Social,
	Arena,
	Mastery,
	Secret
}

/// <summary>
/// Types of rewards an achievement can grant
/// </summary>
public enum AchievementRewardType
{
	Gold,
	Gems,
	BossTokens,
	ContractInk,
	Monster,
	Item,
	Title,
	Theme
}

/// <summary>
/// What stat or condition to check for achievement progress
/// </summary>
public enum AchievementRequirement
{
	// Collection
	TotalMonstersCaught,
	CaughtElementFire,
	CaughtElementWater,
	CaughtElementEarth,
	CaughtElementWind,
	CaughtElementElectric,
	CaughtElementIce,
	CaughtElementNature,
	CaughtElementMetal,
	CaughtElementShadow,
	CaughtElementSpirit,
	CaughtElementNeutral,
	CaughtRarityRare,
	CaughtRarityEpic,
	CaughtRarityLegendary,
	CaughtRarityMythic,
	BeastiaryCompleted,
	OwnedSameSpecies,

	// Battle
	TotalBattlesWon,
	TotalDamageDealt,
	TotalKnockouts,
	WinWithoutLoss,
	WinWithTypeDisadvantage,
	UsedEveryElement,

	// Expedition
	HighestExpeditionCleared,
	HighestHardModeCleared,
	ExpeditionsCompleted,
	ExpeditionWithoutCatch,
	AllBossesDefeated,

	// Breeding
	TotalMonstersBred,
	BredHighGenes,
	BredPerfectGene,
	GotTwins,
	BredRareTrait,

	// Economy
	TotalGoldEarned,
	TotalItemsBought,
	EquippedThreeRelics,
	UsedServerBoost,
	BossTokensSpent,

	// Arena
	ArenaWins,
	ArenaRankReached,
	ArenaWinStreak,
	ArenaWinVsHigherRank,
	ArenaSetsCompleted,
	ArenaReverseSweep,

	// Social
	TotalTradesCompleted,
	ChatMessagesSent,
	BeastShowcased,
	MiniGamesPlayed,
	TamerCardsCollected,

	// Mastery
	TamerLevel,
	SkillsUnlocked,
	MonstersEvolved,
	MonsterVeteranMaxRank,
	SkillPointsInvested,

	// Secret
	SecretCondition
}

/// <summary>
/// A single reward granted by an achievement
/// </summary>
public class AchievementReward
{
	public AchievementRewardType Type { get; set; }
	public int Value { get; set; }
	public string ItemId { get; set; }
	public string SpeciesId { get; set; }
}

/// <summary>
/// Static definition of an achievement
/// </summary>
public class Achievement
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public AchievementCategory Category { get; set; }
	public string IconPath { get; set; }
	public AchievementRequirement Requirement { get; set; }
	public int RequiredValue { get; set; }
	public List<AchievementReward> Rewards { get; set; } = new();
	public bool IsSecret { get; set; } = false;
	public int Order { get; set; } = 0;
}

/// <summary>
/// Player's progress toward a specific achievement
/// </summary>
public class AchievementProgress
{
	public string AchievementId { get; set; }
	public int CurrentValue { get; set; } = 0;
	public bool IsUnlocked { get; set; } = false;
	public bool IsClaimed { get; set; } = false;
	public DateTime UnlockedAt { get; set; } = DateTime.MinValue;
}
