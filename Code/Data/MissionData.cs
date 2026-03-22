using System;

namespace Beastborne.Data;

/// <summary>
/// Categories of missions that map to different gameplay systems
/// </summary>
public enum MissionCategory
{
	Battle,
	Expedition,
	Collection,
	Economy,
	Social
}

/// <summary>
/// Mission refresh tiers — daily, weekly, or monthly
/// </summary>
public enum MissionTier
{
	Daily,
	Weekly,
	Monthly
}

/// <summary>
/// Static definition of a mission — what the player needs to do and what they earn
/// </summary>
public class MissionDefinition
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public MissionCategory Category { get; set; }
	public MissionTier Tier { get; set; }
	public int Target { get; set; }
	public int GoldReward { get; set; }
	public int XPReward { get; set; }
	public int GemReward { get; set; }
	public int InkReward { get; set; }
	public string ItemReward { get; set; } // item ID or empty
	public string Icon { get; set; } // SVG path
}

/// <summary>
/// Per-player progress state for an active mission
/// </summary>
public class MissionState
{
	public string MissionId { get; set; }
	public int Progress { get; set; }
	public bool Completed { get; set; }
	public bool Claimed { get; set; }
}
