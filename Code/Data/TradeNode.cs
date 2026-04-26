using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Static definition of a trade node ("request tile" style a la Age of Calamity).
/// Authored in-code; renders as an icon on the world map attached to a zone.
/// Clicking opens a popup that consumes the listed requirements and pays out
/// the rewards in a single instant transaction.
/// </summary>
public class TradeNode
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }

	/// <summary>NPC attribution for flavor ("Old fisherman will trade...") — display only.</summary>
	public string NpcName { get; set; }

	/// <summary>Expedition ID this node sits on. Drives the map placement.</summary>
	public string ZoneId { get; set; }

	/// <summary>World-map icon position as percentages of the map backdrop (0–100).</summary>
	public float MapPosX { get; set; }
	public float MapPosY { get; set; }

	// --- Cost -------------------------------------------------------------------

	/// <summary>Materials / items consumed per transaction. (itemId, count).</summary>
	public List<(string ItemId, int Quantity)> Requirements { get; set; } = new();

	/// <summary>Gold consumed per transaction.</summary>
	public int GoldCost { get; set; }

	// --- Reward -----------------------------------------------------------------

	/// <summary>Items / materials awarded per transaction.</summary>
	public List<(string ItemId, int Quantity)> ItemRewards { get; set; } = new();

	public int GoldReward { get; set; }
	public int XPReward { get; set; }
	public int SkillPointReward { get; set; }

	/// <summary>If false (default), the node is consumed after one successful
	/// exchange. Trade requests are intentionally one-shot — finite content
	/// the player works through, not a grindable money-printer. Flip to true
	/// only for nodes intended as repeatable (none currently exist).</summary>
	public bool Repeatable { get; set; } = false;
}

/// <summary>
/// Per-node save state. Tracks how many times this node has been executed so we can
/// grey out one-shots and surface a count on repeatables.
/// </summary>
public class TradeNodeState
{
	public string Id { get; set; }
	public int TimesCompleted { get; set; }
	public long LastCompletedTicks { get; set; }
}

/// <summary>Save-blob section — mirrors other per-manager DTOs.</summary>
public class TradeNodeSaveData
{
	public List<TradeNodeState> Nodes { get; set; } = new();
}
