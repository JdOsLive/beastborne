using System;
using System.Collections.Generic;
using System.Linq;

namespace Beastborne.Data;

public enum GuildRole { Wanderer = 0, Tamer = 1, Warden = 2, Beastlord = 3 }
public enum GuildJoinMode { Open, Request, InviteOnly, Closed }

/// <summary>
/// Current player's guild membership — fetched from API server.
/// </summary>
public class GuildMembership
{
	public string GuildId { get; set; }
	public string GuildName { get; set; }
	public string GuildTag { get; set; }
	public GuildRole Role { get; set; } = GuildRole.Wanderer;
	public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Full guild definition — fetched from API server, cached locally.
/// </summary>
public class GuildDefinition
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Tag { get; set; }
	public int Level { get; set; } = 1;
	public long GuildXP { get; set; } = 0;

	// Emblem
	public string EmblemColor { get; set; } = "#7c3aed";
	public string EmblemIcon { get; set; } = "dragon";
	public string EmblemShape { get; set; } = "square";

	// Info
	public string Description { get; set; } = "";
	public string Motd { get; set; } = "";

	// Settings
	public GuildJoinMode JoinMode { get; set; } = GuildJoinMode.Request;
	public int MinLevel { get; set; } = 1;
	public string MinRank { get; set; } = "Unranked";
	public bool AutoKickInactive { get; set; } = false;
	public int InactiveDays { get; set; } = 3;
	public int MaxMembers { get; set; } = 30;

	// Owner
	public long OwnerSteamId { get; set; }
	public string OwnerName { get; set; }

	// Shared achievement counters
	public int TotalCatches { get; set; } = 0;
	public int TotalArenaWins { get; set; } = 0;
	public int TotalExpeditions { get; set; } = 0;
	public int TotalRaidsCompleted { get; set; } = 0;

	// Metadata
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cached info about each guild member — fetched from API server.
/// </summary>
public class GuildMemberInfo
{
	public long SteamId { get; set; }
	public string Name { get; set; }
	public GuildRole Role { get; set; }
	public int Level { get; set; }
	public int ArenaPoints { get; set; }
	public string ArenaRank { get; set; } = "Unranked";
	public DateTime JoinedAt { get; set; }
	public DateTime LastSeen { get; set; } = DateTime.UtcNow;

	// Weekly tracking (reset Monday UTC)
	public int WeeklyRP { get; set; } = 0;
	public int WeeklyGuildXP { get; set; } = 0;
	public int WeeklyRaidDamage { get; set; } = 0;
	public int BestRaidScore { get; set; } = 0;

	// Runtime only (not persisted)
	public bool IsOnline { get; set; } = false;
	public string ConnectionId { get; set; }
}

public class GuildLogEntry
{
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	public string Action { get; set; }
	public string PlayerName { get; set; }
	public string Details { get; set; }
}

public class GuildInvite
{
	public int ApiId { get; set; } // Database row ID from API server
	public string GuildId { get; set; }
	public string GuildName { get; set; }
	public string GuildTag { get; set; }
	public string InviterName { get; set; }
	public long InviterSteamId { get; set; }
	public string InviterConnectionId { get; set; }
	public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

public class GuildJoinRequest
{
	public long SteamId { get; set; }
	public string PlayerName { get; set; }
	public int Level { get; set; }
	public string ArenaRank { get; set; }
	public int ArenaPoints { get; set; }
	public string ConnectionId { get; set; }
	public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Raid boss for the current 2-week period. Score attack format.
/// </summary>
public class GuildRaidBoss
{
	public string BossSpeciesId { get; set; }
	public string BossName { get; set; }
	public ElementType BossElement { get; set; }
	public int BossLevel { get; set; } = 100;
	public int MaxRounds { get; set; } = 10;
	public int PeriodNumber { get; set; }
	public Dictionary<long, RaidAttemptResult> BestScores { get; set; } = new();
	public int GuildTotalScore => BestScores.Values.Sum( s => s.TotalScore );
}

public class RaidAttemptResult
{
	public long SteamId { get; set; }
	public string PlayerName { get; set; }
	public int TotalScore { get; set; }
	public int RawDamage { get; set; }
	public float ComboMultiplier { get; set; }
	public int RoundsUsed { get; set; }
	public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Advertised guild info visible to non-guild players on the same server
/// </summary>
public class GuildAdvertisement
{
	public string GuildId { get; set; }
	public string GuildName { get; set; }
	public string GuildTag { get; set; }
	public int Level { get; set; }
	public int MemberCount { get; set; }
	public int OnlineCount { get; set; }
	public int TotalRP { get; set; }
	public string EmblemColor { get; set; }
	public string EmblemIcon { get; set; }
	public string EmblemShape { get; set; }
	public GuildJoinMode JoinMode { get; set; }
	public int MinLevel { get; set; }
	public string MinRank { get; set; }
	public string OwnerName { get; set; }
	public string Description { get; set; }
}

/// <summary>
/// Guild achievement definition
/// </summary>
public class GuildAchievement
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public int Requirement { get; set; }
	public int GuildXPReward { get; set; }
	public string Icon { get; set; }
}
