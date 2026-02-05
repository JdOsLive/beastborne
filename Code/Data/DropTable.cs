using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// A single entry in a drop table
/// </summary>
public class DropTableEntry
{
	public string ItemId { get; set; }
	public int Weight { get; set; } = 100;
	public int MinQuantity { get; set; } = 1;
	public int MaxQuantity { get; set; } = 1;

	// Minimum monster rarity required to drop this item
	public Rarity MinMonsterRarity { get; set; } = Rarity.Common;

	// Minimum expedition level required for this item to drop
	public int MinExpeditionLevel { get; set; } = 1;
}

/// <summary>
/// A drop table containing possible item drops for a context (expedition, boss, etc.)
/// </summary>
public class DropTable
{
	public string Id { get; set; }
	public List<DropTableEntry> Entries { get; set; } = new();

	// Base chance for any drop to occur (0.0 - 1.0)
	// Default 6% for regular enemies (drops are rare)
	public float BaseDropChance { get; set; } = 0.06f;

	// Element theme for this drop table (optional)
	public ElementType? Element { get; set; }

	// Expedition level range this table applies to
	public int MinLevel { get; set; } = 1;
	public int MaxLevel { get; set; } = 100;
}
