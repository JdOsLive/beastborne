using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Gender options for the tamer character
/// </summary>
public enum TamerGender
{
	Male,
	Female
}

/// <summary>
/// Player profile - the tamer data
/// </summary>
public class Tamer
{
	public string Name { get; set; }
	public TamerGender Gender { get; set; } = TamerGender.Male;
	public int Level { get; set; } = 1;
	public int TotalXP { get; set; } = 0;

	// Resources
	public int Gold { get; set; } = 100;
	public int Gems { get; set; } = 0;
	public int ContractInk { get; set; } = 10;
	public int BossTokens { get; set; } = 0;
	public DateTime EliteInkExpiresAt { get; set; } = DateTime.MinValue;
	public bool HasMasterInk { get; set; } = false;

	// Boss progression
	public List<string> ClearedBosses { get; set; } = new();

	// Cosmetics
	public List<string> UnlockedThemes { get; set; } = new() { "default" };
	public List<string> UnlockedTitles { get; set; } = new();
	public string ActiveThemeId { get; set; } = "default";
	public string ActiveTitleId { get; set; } = null;

	// Skill tree state (skill ID -> rank invested)
	public Dictionary<string, int> SkillRanks { get; set; } = new();
	public int SkillPoints { get; set; } = 1;

	// Inventory system (item ID -> quantity)
	public Dictionary<string, int> Inventory { get; set; } = new();

	// Equipped relics (max 3, tamer passive effects)
	public List<string> EquippedRelics { get; set; } = new();

	// Active consumable boosts
	public List<ActiveItemBoost> ActiveBoosts { get; set; } = new();

	// Legacy property for backward compatibility during migration
	public List<string> UnlockedSkills
	{
		get => new List<string>( SkillRanks.Keys );
		set
		{
			// Migration: convert old list to dictionary (assume rank 1 for each)
			if ( value != null )
			{
				foreach ( var skillId in value )
				{
					if ( !SkillRanks.ContainsKey( skillId ) )
						SkillRanks[skillId] = 1;
				}
			}
		}
	}

	// Progression
	public int HighestExpeditionCleared { get; set; } = 0;
	public string ArenaRank { get; set; } = "Unranked";
	public int ArenaPoints { get; set; } = 0;

	// Bug fix compensation flags
	public bool HasClaimedSkillPointRecovery { get; set; } = false;

	// Stats tracking
	public int TotalBattlesWon { get; set; } = 0;
	public int TotalBattlesLost { get; set; } = 0;
	public int ArenaWins { get; set; } = 0;
	public int ArenaLosses { get; set; } = 0;
	public int TotalMonstersCaught { get; set; } = 0;
	public int TotalMonstersBred { get; set; } = 0;
	public int TotalMonstersEvolved { get; set; } = 0;

	// Timestamps
	public DateTime LastLogin { get; set; } = DateTime.UtcNow;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public TimeSpan TotalPlayTime { get; set; }

	/// <summary>
	/// XP required to go from current level to next level.
	/// Uses exponential scaling: base + (level^2 * multiplier)
	/// Level 1->2: 500 XP, Level 10->11: 2,500 XP, Level 50->51: 50,500 XP
	/// </summary>
	public int XPForNextLevel => GetXPForLevel( Level );

	/// <summary>
	/// Calculate XP needed for a specific level transition
	/// </summary>
	public static int GetXPForLevel( int level )
	{
		// Exponential curve: 500 base + level^2 * 20
		// Much harder progression - tamer leveling should feel like an achievement
		return 500 + (level * level * 20);
	}

	// XP progress as percentage (0.0 to 1.0)
	public float XPProgress
	{
		get
		{
			int currentLevelXP = GetTotalXPForLevel( Level );
			int nextLevelXP = GetTotalXPForLevel( Level + 1 );
			int xpIntoLevel = TotalXP - currentLevelXP;
			int xpNeeded = nextLevelXP - currentLevelXP;
			return xpNeeded > 0 ? (float)xpIntoLevel / xpNeeded : 0;
		}
	}

	/// <summary>
	/// Current XP within the current level (for display)
	/// </summary>
	public int CurrentLevelXP => TotalXP - GetTotalXPForLevel( Level );

	// Get total XP required to reach a level from level 1
	private int GetTotalXPForLevel( int level )
	{
		if ( level <= 1 ) return 0;

		int total = 0;
		for ( int i = 1; i < level; i++ )
		{
			total += GetXPForLevel( i );
		}
		return total;
	}

	// Maximum tamer level
	public const int MaxLevel = 250;

	/// <summary>
	/// Get skill points earned for reaching a specific level.
	/// Scales up at higher levels for faster progression later.
	/// Levels 1-80: 1 SP, Levels 81-170: 2 SP, Levels 171-250: 3 SP
	/// Total at max level: 80 + 180 + 240 + 1 starting = 501 SP
	/// </summary>
	public static int GetSkillPointsForLevel( int level )
	{
		if ( level <= 80 ) return 1;
		if ( level <= 170 ) return 2;
		return 3;
	}

	// Add XP and handle level ups
	public bool AddXP( int amount )
	{
		// Don't gain XP if already at max level
		if ( Level >= MaxLevel )
			return false;

		TotalXP += amount;
		bool leveledUp = false;

		while ( TotalXP >= GetTotalXPForLevel( Level + 1 ) && Level < MaxLevel )
		{
			Level++;
			SkillPoints += GetSkillPointsForLevel( Level );
			leveledUp = true;
		}

		return leveledUp;
	}

	// Resource management
	public bool SpendGold( int amount )
	{
		if ( Gold < amount ) return false;
		Gold -= amount;
		return true;
	}

	public bool SpendGems( int amount )
	{
		if ( Gems < amount ) return false;
		Gems -= amount;
		return true;
	}

	public bool SpendContractInk( int amount = 1 )
	{
		if ( ContractInk < amount ) return false;
		ContractInk -= amount;
		return true;
	}

	// Win rate calculation (all battles)
	public float WinRate
	{
		get
		{
			int total = TotalBattlesWon + TotalBattlesLost;
			return total > 0 ? (float)TotalBattlesWon / total : 0;
		}
	}

	// Arena-specific win rate
	public float ArenaWinRate
	{
		get
		{
			int total = ArenaWins + ArenaLosses;
			return total > 0 ? (float)ArenaWins / total : 0;
		}
	}
}
