using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Root save document persisted by <see cref="Beastborne.Core.SaveService"/>.
/// One blob per Steam player (cloud-authoritative, local-cached).
///
/// Phase 2: all seven save-owning managers now hydrate from / write back into
/// the section DTOs below. Cookies are no longer the source of truth for
/// gameplay state (settings excluded — <c>SettingsManager</c> intentionally
/// keeps its own cookies since they're global, not per-player).
/// </summary>
public class SaveBlob
{
	/// <summary>Schema version. Bump on any breaking blob shape change so loaders can migrate.</summary>
	public int SchemaVersion { get; set; } = 1;

	/// <summary><see cref="System.DateTime.UtcNow"/>.Ticks at the moment the blob was last written. Used for cloud-vs-cache conflict detection.</summary>
	public long LastSaveTicks { get; set; }

	/// <summary>
	/// Steam id of the player this blob belongs to. Stamped on every write.
	/// The cloud store is already Steam-id scoped server-side, but the local
	/// cache file (<c>save-cache.json</c>) is machine-local and NOT per-account,
	/// so this lets the loader confirm a "newer" local cache actually belongs
	/// to the current player before preferring it. <c>0</c> = legacy unstamped blob.
	/// </summary>
	public long OwnerSteamId { get; set; }

	// --- Section DTOs -----------------------------------------------------------

	/// <summary>Player-level stats, currency, cosmetics, skills.</summary>
	public TamerSaveData Tamer { get; set; }

	/// <summary>All owned monsters. Reuses the in-memory <see cref="Monster"/> class directly since it already round-trips cleanly through <c>System.Text.Json</c>.</summary>
	public List<Monster> Monsters { get; set; } = new();

	/// <summary>Roster cap. Mirrors <c>MonsterManager</c>'s current max-monsters cookie.</summary>
	public int MaxMonsters { get; set; } = 50;

	/// <summary>
	/// Fusion pattern ids the player has discovered (successfully woven at
	/// least once). Owned by <c>MonsterManager.DiscoveredPatterns</c>.
	/// </summary>
	public List<string> DiscoveredPatterns { get; set; } = new();

	/// <summary>Discovery + seen state for the monster encyclopedia.</summary>
	public BeastiarySaveData Beastiary { get; set; }

	/// <summary>Tutorial completion / skip / step flags.</summary>
	public TutorialSaveData Tutorial { get; set; }

	/// <summary>Mission progress and daily refresh state.</summary>
	public MissionSaveData Missions { get; set; }

	/// <summary>Local guild state (hop cooldown, weekly reset marker). Most guild data is server-owned via GuildApiClient.</summary>
	public GuildSaveData Guild { get; set; }

	/// <summary>Daily login streak + last-claim timestamp.</summary>
	public DailyRewardSaveData DailyReward { get; set; }

	/// <summary>Side quest progress + completion history.</summary>
	public SideQuestSaveData SideQuests { get; set; }

	/// <summary>Trade node transaction history (times-completed per node).</summary>
	public TradeNodeSaveData TradeNodes { get; set; }

	/// <summary>Team picker presets (A/B/C) + per-zone last-team memory.</summary>
	public TeamPresetSaveData TeamPresets { get; set; }
}

// ---------------------------------------------------------------------------
// Section DTOs. Shape mirrors the cookie-backed fields each manager used to
// own directly. System.Text.Json round-trips all of these cleanly.
// ---------------------------------------------------------------------------

/// <summary>
/// Tamer state. Populated by <c>TamerManager</c>. Embeds the entire
/// <see cref="Tamer"/> object since it already contains every persisted
/// field (currency, XP, inventory, skills, achievements, match history,
/// card collection, species mastery, cosmetics, etc).
/// </summary>
public class TamerSaveData
{
	/// <summary>The full Tamer object. Serialized as-is.</summary>
	public Tamer Tamer { get; set; }
}

/// <summary>
/// Beastiary discovery state. Populated by <c>BeastiaryManager</c>.
/// </summary>
public class BeastiarySaveData
{
	/// <summary>Species the player has fully discovered (caught or owned).</summary>
	public List<string> DiscoveredSpecies { get; set; } = new();

	/// <summary>Species the player has seen (encountered in expedition).</summary>
	public List<string> SeenSpecies { get; set; } = new();
}

/// <summary>
/// Tutorial progress. Populated by <c>TutorialManager</c>.
/// </summary>
public class TutorialSaveData
{
	public bool Completed { get; set; }
	public bool Skipped { get; set; }
	public int StepIndex { get; set; }
}

/// <summary>
/// Daily/weekly/monthly mission state. Populated by <c>MissionManager</c>.
/// </summary>
public class MissionSaveData
{
	public List<MissionState> DailyMissions { get; set; } = new();
	public List<MissionState> WeeklyMissions { get; set; } = new();
	public MissionState MonthlyChallenge { get; set; }

	/// <summary>When the daily missions next refresh (midnight UTC tomorrow). Stored as UTC ticks.</summary>
	public long DailyRefreshTicks { get; set; }

	/// <summary>When weekly missions next refresh (next Monday midnight UTC). Stored as UTC ticks.</summary>
	public long WeeklyRefreshTicks { get; set; }

	/// <summary>When the monthly challenge next refreshes (first of next month midnight UTC). Stored as UTC ticks.</summary>
	public long MonthlyRefreshTicks { get; set; }

	public bool DailyBonusClaimed { get; set; }
	public bool WeeklyBonusClaimed { get; set; }
}

/// <summary>
/// Local guild state — everything that lives on the player's client rather
/// than on the guild server. Membership, roster, RP, contributions, etc.
/// are all fetched fresh from <c>GuildApiClient</c> on every boot; only
/// these two values need to persist between sessions.
/// </summary>
public class GuildSaveData
{
	/// <summary>UTC ticks at which the player last left a guild. Used for the guild-hop cooldown.</summary>
	public long LeaveTicks { get; set; }

	/// <summary>UTC ticks at which the local weekly reset last ran (marks the Monday boundary that was processed).</summary>
	public long LastWeeklyResetTicks { get; set; }
}

/// <summary>
/// Daily login / streak state. Populated by <c>DailyRewardManager</c>.
/// </summary>
public class DailyRewardSaveData
{
	public int DailyStreak { get; set; }
	public int TotalLoginDays { get; set; }
	public bool StreakShieldUsed { get; set; }
	public bool TodayRewardClaimed { get; set; }

	/// <summary>UTC ticks of the last claimed login (0 if never claimed).</summary>
	public long LastLoginTicks { get; set; }

	/// <summary>UTC ticks for when the streak shield resets (next Monday midnight UTC).</summary>
	public long StreakShieldResetTicks { get; set; }

	/// <summary>Milestone day-counts already claimed (7, 14, 30, etc).</summary>
	public List<int> MilestonesClaimed { get; set; } = new();
}
