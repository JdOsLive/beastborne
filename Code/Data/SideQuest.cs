using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// What the player has to do to complete a side quest. One objective per quest for
/// launch simplicity; multi-objective chains can layer on later by upgrading this
/// to a List<QuestObjective> without breaking saves (add a new field).
/// </summary>
public enum SideQuestObjectiveType
{
	/// <summary>Defeat N monsters of a specific species (TargetId = speciesId).</summary>
	DefeatSpecies,

	/// <summary>Collect N of a specific material item (TargetId = itemId, e.g. "mat_sheepot").</summary>
	CollectMaterial,

	/// <summary>Clear a specific expedition N times (TargetId = expeditionId).</summary>
	ExpeditionCleared,

	/// <summary>Complete N fusions (TargetId ignored).</summary>
	FusionsCompleted,

	/// <summary>Catch/contract N monsters (TargetId ignored).</summary>
	MonstersContracted
}

/// <summary>
/// Static definition of a side quest. Authored in-code; never persisted.
/// Matches the Hyrule Warriors "zone request" shape: a small card attached to
/// a zone, optionally attributed to an NPC, that flips an objective counter
/// and pays out a reward bundle when the counter hits Target.
/// </summary>
public class SideQuest
{
	public string Id { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }

	/// <summary>In-world NPC who posted the quest (flavor; no game effect).</summary>
	public string NpcName { get; set; }

	/// <summary>Expedition ID the quest is attached to. Shown on that zone's card.</summary>
	public string ZoneId { get; set; }

	public SideQuestObjectiveType Objective { get; set; }

	/// <summary>Species ID, item ID, or expedition ID depending on <see cref="Objective"/>. Unused for FusionsCompleted / MonstersContracted.</summary>
	public string TargetId { get; set; }

	/// <summary>How many of <see cref="TargetId"/> need to happen.</summary>
	public int Target { get; set; } = 1;

	// --- Rewards ----------------------------------------------------------------

	public int GoldReward { get; set; }
	public int XPReward { get; set; }
	public int SkillPointReward { get; set; }

	/// <summary>Item payouts. Tuple is (itemId, quantity).</summary>
	public List<(string ItemId, int Quantity)> ItemRewards { get; set; } = new();

	/// <summary>If true, the quest can be accepted again after completion (daily/endless). Default = one-shot.</summary>
	public bool Repeatable { get; set; }
}

/// <summary>
/// Persisted per-quest player state. Lives in the save blob.
/// </summary>
public class SideQuestState
{
	public string Id { get; set; }
	public int Progress { get; set; }
	public bool Completed { get; set; }
	public bool RewardClaimed { get; set; }

	/// <summary>UTC ticks when the quest was accepted. Drives HUD "new" highlighting.</summary>
	public long AcceptedTicks { get; set; }
}

/// <summary>
/// Save-blob section. Mirrors other per-manager DTOs.
/// </summary>
public class SideQuestSaveData
{
	public List<SideQuestState> ActiveQuests { get; set; } = new();

	/// <summary>IDs of quests that have been completed + claimed at least once.</summary>
	public List<string> CompletedOneShotIds { get; set; } = new();
}
