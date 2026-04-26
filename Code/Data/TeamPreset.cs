using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// A saved team loadout (A/B/C slots on the team picker). Holds monster IDs
/// only — the picker looks up fresh <see cref="Monster"/> instances at load
/// time so HP/level reflect current state, not the state at save time.
/// </summary>
public class TeamPreset
{
	/// <summary>User-editable display name. Defaults to "Team A" / "Team B" / "Team C".</summary>
	public string Name { get; set; }

	public List<Guid> MonsterIds { get; set; } = new();

	/// <summary>UTC ticks when this preset was last saved. Powers a "last saved X ago" hint if we want one.</summary>
	public long UpdatedTicks { get; set; }
}

/// <summary>
/// Save-blob section for the team picker. Three named presets + a per-zone
/// "last team used" memory so reopening the picker on a zone you've visited
/// remembers your previous choice. Mirrors other per-manager DTOs.
/// </summary>
public class TeamPresetSaveData
{
	/// <summary>The three user-managed preset slots.</summary>
	public List<TeamPreset> Presets { get; set; } = new();

	/// <summary>Last team embarked per expedition ID. Auto-updated on successful embark.</summary>
	public Dictionary<string, List<Guid>> LastTeamPerZone { get; set; } = new();
}
