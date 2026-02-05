using System;

namespace Beastborne.Data;

/// <summary>
/// Types of items available in the shop
/// </summary>
public enum ShopItemType
{
	ContractInk,    // For catching monsters
	XPBoost,        // Temporary XP multiplier (legacy, use TamerXPBoost or BeastXPBoost)
	TamerXPBoost,   // Temporary tamer XP multiplier
	BeastXPBoost,   // Temporary beast/monster XP multiplier
	GoldBoost,      // Temporary gold multiplier
	LuckyCharm,     // Increased rare encounter/catch chance
	SkillReset,     // Reset skill points
	MonsterSlot,    // Increase monster storage
	RareEncounter,  // Increase rare encounter chance
	Revive,         // Revive all monsters in expedition
	ElementOrb,     // Boost a specific element
	PartySlot,      // Increase party size
	StatBoost,      // Permanent stat boost item
	NameChange      // Change monster name
}

/// <summary>
/// Currency type for purchasing
/// </summary>
public enum CurrencyType
{
	Gold,
	Gems
}

/// <summary>
/// Represents an item available for purchase in the shop
/// </summary>
public class ShopItem
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string IconPath { get; set; }
	public ShopItemType Type { get; set; }
	public CurrencyType Currency { get; set; }
	public int Price { get; set; }
	public int Quantity { get; set; } = 1;  // Amount given per purchase

	// For boost items
	public float BoostMultiplier { get; set; } = 1.0f;
	public int BoostDurationMinutes { get; set; } = 0;

	// For element orbs
	public ElementType? TargetElement { get; set; }

	// Limited stock (0 = unlimited)
	public int MaxStock { get; set; } = 0;
	public int CurrentStock { get; set; } = 0;

	// Level requirement
	public int RequiredLevel { get; set; } = 1;

	/// <summary>
	/// Check if player can afford this item
	/// </summary>
	public bool CanAfford( int gold, int gems )
	{
		return Currency switch
		{
			CurrencyType.Gold => gold >= Price,
			CurrencyType.Gems => gems >= Price,
			_ => false
		};
	}

	/// <summary>
	/// Check if item is in stock
	/// </summary>
	public bool IsInStock => MaxStock == 0 || CurrentStock > 0;
}

/// <summary>
/// Active boost effect on the player
/// </summary>
public class ActiveBoost
{
	public ShopItemType Type { get; set; }
	public float Multiplier { get; set; }
	public DateTime ExpiresAt { get; set; }
	public bool IsServerWide { get; set; } = false;
	public string ActivatedBy { get; set; } // Player name who activated it

	public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
	public TimeSpan TimeRemaining => IsExpired ? TimeSpan.Zero : ExpiresAt - DateTime.UtcNow;
}

/// <summary>
/// Server-wide boost that applies to all players
/// </summary>
public class ServerBoost
{
	public ShopItemType Type { get; set; }
	public float Multiplier { get; set; }
	public DateTime ExpiresAt { get; set; }
	public string ActivatedBy { get; set; }
	public long ActivatedBySteamId { get; set; }

	public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
	public TimeSpan TimeRemaining => IsExpired ? TimeSpan.Zero : ExpiresAt - DateTime.UtcNow;
}
