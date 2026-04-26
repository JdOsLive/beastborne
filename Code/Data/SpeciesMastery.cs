using System;

namespace Beastborne.Data;

/// <summary>
/// Species-level mastery progress tracked in the Beastbook.
/// Replaces the old per-instance VeteranRank system — mastery is now
/// earned across all beasts of a given species.
/// </summary>
public class SpeciesMasteryData
{
	public string SpeciesId { get; set; }

	/// <summary>Current mastery level, 0-6.</summary>
	public int Level { get; set; } = 0;

	/// <summary>Total beasts of this species defeated in battle.</summary>
	public int DefeatCount { get; set; } = 0;

	/// <summary>Total beasts of this species added to the roster (contracted or bred).</summary>
	public int CaptureCount { get; set; } = 0;

	/// <summary>Whether the player has fused at least one beast of this species.</summary>
	public bool HasFused { get; set; } = false;

	/// <summary>Whether the player has owned a max-form (no further evolution) beast of this species.</summary>
	public bool HasMaxEvolved { get; set; } = false;

	/// <summary>First time this species was ever seen / interacted with (UTC).</summary>
	public DateTime FirstDiscovered { get; set; } = DateTime.UtcNow;
}
