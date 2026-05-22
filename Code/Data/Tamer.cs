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
	public List<string> UnlockedTitles { get; set; } = new();
	public string ActiveTitleId { get; set; } = null;
	public string ActiveLevelTitle { get; set; } = null;

	// Set true on any session loaded before <see cref="TamerManager.LaunchDate"/>.
	// Sticky once true. Drives the "Alpha" title grant.
	public bool PlayedDuringAlpha { get; set; } = false;

	// Skill tree state — SkillRanks is the SINGLE SOURCE OF TRUTH for invested
	// ranks (skill ID → rank). SkillPoints is the loose / unspent pool.
	//
	// Invariant (codified in NormalizeSkillState):
	//     TotalEarnedSP(Level) == SkillPoints + SUM(rank × node.CostPerRank)
	//
	// The legacy `UnlockedSkills` rank-blind List<string> view was removed
	// 2026-05-21 — its getter allocated a throwaway list every call, so
	// `.Clear()` / `.Add()` mutations on the property were silent no-ops
	// (the source of the v1.2 shop-reset "free SP, ranks intact" corruption).
	// All callers now go through SkillRanks directly.
	public Dictionary<string, int> SkillRanks { get; set; } = new();
	public int SkillPoints { get; set; } = 1;

	// Inventory system (item ID -> quantity)
	public Dictionary<string, int> Inventory { get; set; } = new();

	// Equipped relics (max 3, tamer passive effects)
	public List<string> EquippedRelics { get; set; } = new();

	// Active consumable boosts
	public List<ActiveItemBoost> ActiveBoosts { get; set; } = new();

	// Progression
	public int HighestExpeditionCleared { get; set; } = 0;

	// Hard Mode per-expedition tracking.
	// HardModeUnlocked: expeditionId → true once the player clears Normal once.
	// HardModeCleared:  expeditionId → true once the player clears Hard once.
	// HighestHardModeCleared: count of distinct expeditions with a Hard clear (for achievements).
	// All three default to empty / 0 for existing saves — no migration needed.
	public Dictionary<string, bool> HardModeUnlocked { get; set; } = new();
	public Dictionary<string, bool> HardModeCleared { get; set; } = new();
	public int HighestHardModeCleared { get; set; } = 0;

	// Hard Mode token currency — one bucket per zone. Awarded 3-5 per Hard clear.
	// Tide  = Weaverton  · Loom = Weaverwood · Dawn = Weavermere · Threaded = Whispering Hollow.
	// Redemption flow ships in a follow-up patch (items-economy lane).
	public int TideTokens { get; set; } = 0;
	public int LoomTokens { get; set; } = 0;
	public int DawnTokens { get; set; } = 0;
	public int ThreadedTokens { get; set; } = 0;

	public string ArenaRank { get; set; } = "Unranked";
	public int ArenaPoints { get; set; } = 0;

	// Bug fix compensation flags
	public bool HasClaimedSkillPointRecovery { get; set; } = false;

	// One-time migration version. Each migration step bumps this and
	// performs cleanup on load. Use for retroactive data fixes that
	// shouldn't run twice. v1 = beta-launch leaderboard reset (zeroes
	// pre-launch inflated battle/damage/KO totals so the open beta
	// starts everyone clean).
	public int MigrationVersion { get; set; } = 0;

	// Legacy field — was a one-time v1.2.1 aggressive respec gate. The respec
	// path was REPLACED by NormalizeSkillState (idempotent, self-healing,
	// surgical) so this bool no longer drives any logic. Kept on the schema
	// (don't break round-trip for any save that already stored true) and
	// reserved for future per-version one-shot data fixes if we need one.
	public bool SkillTreeRespecApplied { get; set; } = false;

	// Stats tracking
	public int TotalBattlesWon { get; set; } = 0;
	public int TotalBattlesLost { get; set; } = 0;
	public int ArenaWins { get; set; } = 0;
	public int ArenaLosses { get; set; } = 0;
	public int TotalMonstersCaught { get; set; } = 0;
	public int TotalMonstersBred { get; set; } = 0;
	public int TotalMonstersEvolved { get; set; } = 0;

	// Extended stats tracking (for achievements & leaderboards)
	public int TotalGoldEarned { get; set; } = 0;
	public int TotalItemsBought { get; set; } = 0;
	public int TotalExpeditionsCompleted { get; set; } = 0;
	public int TotalTradesCompleted { get; set; } = 0;
	public int TotalMiniGamesPlayed { get; set; } = 0;
	public int ChatMessagesSent { get; set; } = 0;
	public int BossTokensSpent { get; set; } = 0;
	public int TotalDamageDealt { get; set; } = 0;
	public int TotalKnockouts { get; set; } = 0;
	public int ArenaWinStreak { get; set; } = 0;
	public int ArenaSetsCompleted { get; set; } = 0;
	public int ArenaPlacementMatchesPlayed { get; set; } = 0;
	public int CurrentSeason { get; set; } = 1;

	// Achievement system
	public Dictionary<string, AchievementProgress> Achievements { get; set; } = new();

	// Species-level mastery progress (keyed by SpeciesId) — replaces per-instance VeteranRank
	public Dictionary<string, SpeciesMasteryData> SpeciesMastery { get; set; } = new();

	// Match history (last 20 sets)
	public List<MatchHistoryEntry> MatchHistory { get; set; } = new();

	// Tamer card
	public string FavoriteMonsterSpeciesId { get; set; }
	public string FavoriteExpeditionId { get; set; }
	public List<string> CardBadges { get; set; } = new();
	public List<CollectedTamerCard> CollectedCards { get; set; } = new();

	// UI popup drag positions (persisted so panels stay where the player moved them)
	public float ChatPanelOffsetX { get; set; } = 0f;
	public float ChatPanelOffsetY { get; set; } = 0f;
	public float RadioWidgetOffsetX { get; set; } = 0f;
	public float RadioWidgetOffsetY { get; set; } = 0f;
	public float EffectsPanelOffsetX { get; set; } = 0f;
	public float EffectsPanelOffsetY { get; set; } = 0f;
	public float NotifPanelOffsetX { get; set; } = 0f;
	public float NotifPanelOffsetY { get; set; } = 0f;

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

	// Maximum tamer level — lv 50 for launch. Scoped to the 3-expedition
	// content and the 15-talent launch skill tree (55 SP max-all).
	// Post-launch updates raise this alongside new talents.
	public const int MaxLevel = 50;

	/// <summary>
	/// Get skill points earned for a level. Flat 1 SP per level for launch
	/// (simple progression, no difficulty cliffs). Combined with 6 starting
	/// SP at lv 1 this gives exactly 55 SP at lv 50, matching the launch
	/// skill tree's 55 SP total. Player maxes the tree at level cap.
	/// </summary>
	public static int GetSkillPointsForLevel( int level )
	{
		return 1;
	}

	/// <summary>
	/// Total skill points a tamer has EARNED by <paramref name="level"/> —
	/// 6 starting SP at level 1, plus 1 per level after. Level 50 → 55 SP,
	/// matching the launch skill tree's 55-SP total.
	/// </summary>
	public static int GetTotalSkillPointsForLevel( int level )
	{
		int capped = level < 1 ? 1 : ( level > MaxLevel ? MaxLevel : level );
		return 6 + ( capped - 1 );
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

	/// <summary>
	/// Self-healing audit of the skill state. Idempotent — safe to call from
	/// hydrate, after every level-up, after every shop reset, after every
	/// in-tree investment. Repairs invariant violations only; does NOT
	/// aggressively respec a tree that's already in a valid state.
	///
	/// The invariant being enforced:
	///   GetTotalSkillPointsForLevel(Level) == SkillPoints + Σ(rank × CostPerRank)
	///
	/// Repairs applied in order:
	///   1. Clamp Level to [1, MaxLevel].
	///   2. Strip keys in SkillRanks that aren't in the catalog (renamed/cut
	///      skills) — refund their full cost.
	///   3. Clamp each remaining rank to its node's MaxRank — refund the
	///      excess.
	///   4. If total spent still exceeds total earned (impossible-but-defensive):
	///      remove cheapest-cost-per-rank skills first until invariant holds.
	///   5. Set SkillPoints = (earned − spent), clamped >= 0.
	///
	/// Returns a struct describing what (if anything) was changed so callers
	/// can show a toast only when there was an actual repair.
	/// </summary>
	public SkillStateRepairResult NormalizeSkillState( SkillTree tree )
	{
		var result = new SkillStateRepairResult();
		if ( tree == null ) return result;

		SkillRanks ??= new();

		// 1. Clamp level.
		int clampedLevel = Level < 1 ? 1 : ( Level > MaxLevel ? MaxLevel : Level );
		if ( clampedLevel != Level )
		{
			Level = clampedLevel;
			result.LevelClamped = true;
		}

		int earned = GetTotalSkillPointsForLevel( Level );

		// 2 + 3. Walk SkillRanks, strip orphans, clamp ranks.
		var toRemove = new List<string>();
		var toClamp = new List<(string id, int newRank)>();
		foreach ( var kv in SkillRanks )
		{
			var node = tree.GetNode( kv.Key );
			if ( node == null )
			{
				toRemove.Add( kv.Key );
				result.OrphanRanksStripped += kv.Value;
				continue;
			}
			if ( kv.Value <= 0 )
			{
				toRemove.Add( kv.Key );
				continue;
			}
			int clamped = Math.Min( kv.Value, node.MaxRank );
			if ( clamped != kv.Value )
			{
				toClamp.Add( (kv.Key, clamped) );
				result.OverRankRefunds += (kv.Value - clamped) * node.CostPerRank;
			}
		}
		foreach ( var id in toRemove ) SkillRanks.Remove( id );
		foreach ( var (id, r) in toClamp ) SkillRanks[id] = r;

		// 4. If still over-budget, remove cheapest ranks first.
		int spent = ComputeSpent( tree );
		while ( spent > earned && SkillRanks.Count > 0 )
		{
			// Pick the cheapest cost-per-rank skill to bleed off first. Ties
			// broken by descending rank so we keep at least one rank in skills
			// the player obviously cared about.
			string victimId = null;
			int victimCost = int.MaxValue;
			int victimRank = 0;
			foreach ( var kv in SkillRanks )
			{
				var n = tree.GetNode( kv.Key );
				if ( n == null ) continue;
				if ( n.CostPerRank < victimCost
					|| ( n.CostPerRank == victimCost && kv.Value > victimRank ) )
				{
					victimId = kv.Key;
					victimCost = n.CostPerRank;
					victimRank = kv.Value;
				}
			}
			if ( victimId == null ) break;

			int oldRank = SkillRanks[victimId];
			int newRank = oldRank - 1;
			if ( newRank <= 0 ) SkillRanks.Remove( victimId );
			else SkillRanks[victimId] = newRank;

			result.OverBudgetRanksStripped++;
			spent -= victimCost;
		}

		// 5. Final SP = earned − spent.
		int newFreeSp = Math.Max( 0, earned - spent );
		if ( newFreeSp != SkillPoints )
		{
			result.SkillPointsBefore = SkillPoints;
			result.SkillPointsAfter = newFreeSp;
			SkillPoints = newFreeSp;
		}
		else
		{
			result.SkillPointsBefore = SkillPoints;
			result.SkillPointsAfter = SkillPoints;
		}

		return result;
	}

	/// <summary>Sum the cost-weighted SP currently invested across SkillRanks.</summary>
	private int ComputeSpent( SkillTree tree )
	{
		if ( tree == null || SkillRanks == null ) return 0;
		int total = 0;
		foreach ( var kv in SkillRanks )
		{
			var n = tree.GetNode( kv.Key );
			if ( n == null ) continue;
			total += kv.Value * n.CostPerRank;
		}
		return total;
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

/// <summary>
/// Result of a <see cref="Tamer.NormalizeSkillState"/> pass. Lets callers decide
/// whether to fire a player-facing notification (only when something was
/// actually repaired). All-zero / all-false means "clean, no-op".
/// </summary>
public struct SkillStateRepairResult
{
	/// <summary>True if Level was clamped into [1, MaxLevel].</summary>
	public bool LevelClamped;

	/// <summary>Total ranks (not cost-weighted) stripped because their skill
	/// id no longer exists in the catalog.</summary>
	public int OrphanRanksStripped;

	/// <summary>Cost-weighted SP refunded by clamping individual ranks to MaxRank.</summary>
	public int OverRankRefunds;

	/// <summary>Number of rank tiers removed by the over-budget bleed-off pass
	/// (cheapest-first). Should always be 0 on a sane save; non-zero indicates
	/// real prior corruption that this pass repaired.</summary>
	public int OverBudgetRanksStripped;

	/// <summary>Free SP pool before the pass.</summary>
	public int SkillPointsBefore;

	/// <summary>Free SP pool after the pass.</summary>
	public int SkillPointsAfter;

	/// <summary>True iff this pass actually changed something — clamped a
	/// level, stripped an orphan or over-rank, bled off an over-budget rank,
	/// or repaired SkillPoints.</summary>
	public bool ChangedAnything =>
		LevelClamped
		|| OrphanRanksStripped > 0
		|| OverRankRefunds > 0
		|| OverBudgetRanksStripped > 0
		|| SkillPointsBefore != SkillPointsAfter;

	public override string ToString()
	{
		if ( !ChangedAnything ) return "no-op";
		return $"orphan={OrphanRanksStripped} overRankRefund={OverRankRefunds} overBudgetBleed={OverBudgetRanksStripped} sp {SkillPointsBefore}→{SkillPointsAfter} levelClamp={LevelClamped}";
	}
}

/// <summary>
/// Record of a ranked set result (Bo3)
/// </summary>
public class MatchHistoryEntry
{
	public string OpponentName { get; set; }
	public int OpponentArenaPoints { get; set; }
	public bool Won { get; set; }
	public int GamesWon { get; set; }
	public int GamesLost { get; set; }
	public int PointsChange { get; set; }
	public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
	public bool IsRanked { get; set; } = true;
}

/// <summary>
/// Snapshot of another tamer's card collected through interaction
/// </summary>
public class CollectedTamerCard
{
	public long SteamId { get; set; }
	public string Name { get; set; }
	public int Level { get; set; }
	public string ArenaRank { get; set; }
	public int ArenaPoints { get; set; }
	public string FavoriteMonsterSpeciesId { get; set; }
	public int AchievementCount { get; set; }
	public float WinRate { get; set; }
	public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
	public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

	// Extended card data
	public string Gender { get; set; } = "Male";
	public string FavoriteExpeditionId { get; set; }
	public string Title { get; set; }
	public string TitleColor { get; set; }
	public int ArenaWins { get; set; }
	public int ArenaLosses { get; set; }
	public int HighestExpedition { get; set; }
	public int MonstersCaught { get; set; }
	public int TotalPlayTimeMinutes { get; set; }
	public int BattlesWon { get; set; }
	public int MonstersBred { get; set; }
	public int MonstersEvolved { get; set; }
	public int TotalExpeditionsCompleted { get; set; }
	public int TotalTradesCompleted { get; set; }
}
