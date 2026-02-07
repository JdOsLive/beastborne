using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Categories of items in the inventory system
/// </summary>
public enum ItemCategory
{
	Consumable,  // Temporary boosts, catch rate, XP grants
	Relic,       // Tamer passive effects (max 3 equipped)
	HeldItem,    // Monster equipment (1 per monster)
	QuestItem,   // Special unlocks (Cartographer modes)
	Boost        // Server-wide boosts (XP, Gold, etc.)
}

/// <summary>
/// Rarity tiers for items
/// </summary>
public enum ItemRarity
{
	Common,
	Uncommon,
	Rare,
	Epic,
	Legendary
}

/// <summary>
/// Types of effects items can have
/// </summary>
public enum ItemEffectType
{
	// Consumable effects (temporary battle boosts)
	BoostATK,
	BoostDEF,
	BoostSPD,
	BoostSpA,
	BoostSpD,
	BoostCrit,
	CatchRateBoost,
	XPGrant,
	GoldBoost,
	NatureChange,      // Set monster nature (EffectValue = (int)NatureType)
	TraitReroll,       // Reroll a random trait from species pool
	GeneBoost,         // Boost a random gene (IV) by EffectValue, max 30
	MasterInk,         // Guarantees next capture succeeds
	ContractInkGrant,  // Adds (int)EffectValue contract ink
	EliteInkBuff,      // +catch rate buff for EffectDuration minutes

	// Relic passive effects (tamer-wide)
	PassiveGoldFind,
	PassiveItemFind,
	PassiveCatchRate,
	PassiveXPGain,
	PassiveATKBoost,
	PassiveDEFBoost,
	PassiveSPDBoost,
	PassiveHPBoost,
	PassiveCritRate,
	PassiveTamerXP,
	PassiveInkSaver,
	PassiveHealingBoost,

	// Held item passive effects (monster-specific, apply in battle)
	HeldATKBonus,
	HeldDEFBonus,
	HeldSPDBonus,
	HeldHPBonus,
	HeldSpABonus,
	HeldSpDBonus,
	HeldCritChance,
	HeldCritDamage,
	HeldXPBonus,
	HeldGoldBonus,
	HeldElementBoost,
	HeldDamageTaken,      // For Glass Cannon (increase damage taken)
	HeldFirstStrike,      // Always move first on turn 1
	HeldPPReduction,      // Reduce PP cost
	HeldLifesteal,        // Heal on defeating enemy
	HeldRegeneration,     // Heal % HP per turn
	HeldVsHigherLevel,    // Bonus damage vs higher level
	HeldSurvivalTurns,    // Bonus after X turns
	HeldEvasion,
	HeldAccuracy,
	HeldAllyScaling,      // Bonus per ally in party
	HeldBurnChance,       // Chance to apply burn status

	// Quest item effects
	UnlockCartographerMode,

	// Server boost effects (time-based, max 8 hours)
	ServerTamerXPBoost,    // Tamer XP multiplier
	ServerBeastXPBoost,    // Monster XP multiplier
	ServerGoldBoost,       // Gold multiplier
	ServerLuckyCharm,      // Increased catch/rare chance
	ServerRareEncounter    // Increased rare encounter chance
}

/// <summary>
/// Definition of an item type (template/blueprint)
/// </summary>
public class ItemDefinition
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string IconPath { get; set; }
	public ItemCategory Category { get; set; }
	public ItemRarity Rarity { get; set; }
	public bool IsStackable { get; set; } = true;
	public int MaxStack { get; set; } = 99;

	// Primary effect
	public ItemEffectType EffectType { get; set; }
	public float EffectValue { get; set; }
	public int EffectDuration { get; set; } // In battles or uses
	public int BoostDurationMinutes { get; set; } // For Boost category items (time-based)

	// Secondary effect (for items with multiple effects like Glass Cannon)
	public ItemEffectType? SecondaryEffectType { get; set; }
	public float SecondaryEffectValue { get; set; }

	// For element-specific items
	public ElementType? TargetElement { get; set; }

	// Shop/economy
	public int BuyPrice { get; set; }
	public int SellPrice { get; set; }

	// For quest items - what mode does this unlock
	public string UnlocksModeId { get; set; }

	/// <summary>
	/// Get a formatted description of the item's effects
	/// </summary>
	public string GetEffectDescription()
	{
		var desc = EffectType switch
		{
			ItemEffectType.BoostATK => $"+{EffectValue}% ATK for {EffectDuration} battles",
			ItemEffectType.BoostDEF => $"+{EffectValue}% DEF for {EffectDuration} battles",
			ItemEffectType.BoostSPD => $"+{EffectValue}% SPD for {EffectDuration} battles",
			ItemEffectType.BoostSpA => $"+{EffectValue}% SpA for {EffectDuration} battles",
			ItemEffectType.BoostSpD => $"+{EffectValue}% SpD for {EffectDuration} battles",
			ItemEffectType.BoostCrit => $"+{EffectValue}% crit chance for {EffectDuration} battles",
			ItemEffectType.CatchRateBoost => EffectDuration > 0 ? $"+{EffectValue}% catch rate for {EffectDuration} attempts" : $"+{EffectValue}% catch rate",
			ItemEffectType.XPGrant => $"Grant {EffectValue} XP to a monster",
			ItemEffectType.GoldBoost => $"+{EffectValue}% gold for {EffectDuration} battles",
			ItemEffectType.NatureChange => $"Set nature to {(NatureType)(int)EffectValue}",
			ItemEffectType.TraitReroll => "Randomly reroll one trait from species pool",
			ItemEffectType.GeneBoost => $"Boost a random gene by +{(int)EffectValue} (max 30)",
			ItemEffectType.MasterInk => "Guarantees your next capture attempt succeeds",
			ItemEffectType.ContractInkGrant => $"Grants {(int)EffectValue} Contract Ink",
			ItemEffectType.EliteInkBuff => $"+{EffectValue}% catch rate for {EffectDuration} minutes",
			ItemEffectType.PassiveGoldFind => $"+{EffectValue}% gold from all sources",
			ItemEffectType.PassiveItemFind => $"+{EffectValue}% item drop chance",
			ItemEffectType.PassiveCatchRate => $"+{EffectValue}% catch rate",
			ItemEffectType.PassiveXPGain => $"+{EffectValue}% monster XP",
			ItemEffectType.PassiveATKBoost => $"+{EffectValue}% team ATK",
			ItemEffectType.PassiveDEFBoost => $"+{EffectValue}% team DEF",
			ItemEffectType.PassiveSPDBoost => $"+{EffectValue}% team SPD",
			ItemEffectType.PassiveHPBoost => $"+{EffectValue}% team HP",
			ItemEffectType.PassiveCritRate => $"+{EffectValue}% crit chance",
			ItemEffectType.PassiveTamerXP => $"+{EffectValue}% tamer XP",
			ItemEffectType.PassiveInkSaver => $"+{EffectValue}% ink save chance",
			ItemEffectType.PassiveHealingBoost => $"+{EffectValue}% healing",
			ItemEffectType.HeldATKBonus => $"+{EffectValue}% ATK",
			ItemEffectType.HeldDEFBonus => $"+{EffectValue}% DEF",
			ItemEffectType.HeldSPDBonus => $"+{EffectValue}% SPD",
			ItemEffectType.HeldHPBonus => $"+{EffectValue}% HP",
			ItemEffectType.HeldSpABonus => $"+{EffectValue}% SpA",
			ItemEffectType.HeldSpDBonus => $"+{EffectValue}% SpD",
			ItemEffectType.HeldCritChance => $"+{EffectValue}% crit chance",
			ItemEffectType.HeldCritDamage => $"+{EffectValue}% crit damage",
			ItemEffectType.HeldXPBonus => $"+{EffectValue}% XP gain",
			ItemEffectType.HeldGoldBonus => $"+{EffectValue}% gold from battles",
			ItemEffectType.HeldElementBoost => $"+{EffectValue}% {TargetElement} damage",
			ItemEffectType.HeldFirstStrike => "Always move first on turn 1",
			ItemEffectType.HeldPPReduction => $"-{EffectValue} PP cost on all moves",
			ItemEffectType.HeldLifesteal => $"Heal {EffectValue}% HP on defeating enemy",
			ItemEffectType.HeldRegeneration => $"Heal {EffectValue}% max HP per turn",
			ItemEffectType.HeldVsHigherLevel => $"+{EffectValue}% damage vs higher level foes",
			ItemEffectType.HeldSurvivalTurns => $"+{EffectValue}% all stats after surviving {(int)SecondaryEffectValue} turns",
			ItemEffectType.HeldEvasion => $"+{EffectValue}% evasion",
			ItemEffectType.HeldAllyScaling => $"+{EffectValue}% all stats per ally in party",
			ItemEffectType.HeldBurnChance => $"{EffectValue}% chance to burn on hit",
			ItemEffectType.UnlockCartographerMode => $"Unlocks {UnlocksModeId} expedition mode",
			ItemEffectType.ServerTamerXPBoost => $"{EffectValue}x Tamer XP for {BoostDurationMinutes / 60}h (Server-wide)",
			ItemEffectType.ServerBeastXPBoost => $"{EffectValue}x Beast XP for {BoostDurationMinutes / 60}h (Server-wide)",
			ItemEffectType.ServerGoldBoost => $"{EffectValue}x Gold for {BoostDurationMinutes / 60}h (Server-wide)",
			ItemEffectType.ServerLuckyCharm => $"+{EffectValue}% Catch Rate for {BoostDurationMinutes / 60}h (Server-wide)",
			ItemEffectType.ServerRareEncounter => $"+{EffectValue}% Rare Encounters for {BoostDurationMinutes / 60}h (Server-wide)",
			_ => Description
		};

		// Add secondary effect if present
		if ( SecondaryEffectType.HasValue )
		{
			var sign = SecondaryEffectValue >= 0 ? "+" : "";
			var secondary = SecondaryEffectType.Value switch
			{
				ItemEffectType.HeldDamageTaken => $"+{SecondaryEffectValue}% damage taken",
				ItemEffectType.HeldSPDBonus when SecondaryEffectValue < 0 => $"{SecondaryEffectValue}% SPD",
				ItemEffectType.HeldAccuracy when SecondaryEffectValue < 0 => $"{SecondaryEffectValue}% accuracy",
				ItemEffectType.PassiveGoldFind => $"{sign}{SecondaryEffectValue}% gold find",
				ItemEffectType.PassiveItemFind => $"{sign}{SecondaryEffectValue}% item find",
				ItemEffectType.PassiveCatchRate => $"{sign}{SecondaryEffectValue}% catch rate",
				ItemEffectType.PassiveXPGain => $"{sign}{SecondaryEffectValue}% monster XP",
				ItemEffectType.PassiveATKBoost => $"{sign}{SecondaryEffectValue}% team ATK",
				ItemEffectType.PassiveDEFBoost => $"{sign}{SecondaryEffectValue}% team DEF",
				ItemEffectType.PassiveSPDBoost => $"{sign}{SecondaryEffectValue}% team SPD",
				ItemEffectType.PassiveHPBoost => $"{sign}{SecondaryEffectValue}% team HP",
				ItemEffectType.PassiveCritRate => $"{sign}{SecondaryEffectValue}% crit chance",
				ItemEffectType.PassiveTamerXP => $"{sign}{SecondaryEffectValue}% tamer XP",
				ItemEffectType.PassiveInkSaver => $"{sign}{SecondaryEffectValue}% ink save",
				ItemEffectType.PassiveHealingBoost => $"{sign}{SecondaryEffectValue}% healing",
				_ => ""
			};
			if ( !string.IsNullOrEmpty( secondary ) )
				desc += $", {secondary}";
		}

		return desc;
	}
}

/// <summary>
/// An item instance in the player's inventory
/// </summary>
public class InventoryItem
{
	public string ItemId { get; set; }
	public int Quantity { get; set; } = 1;
	public DateTime ObtainedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Active consumable boost effect
/// </summary>
public class ActiveItemBoost
{
	public string ItemId { get; set; }
	public ItemEffectType EffectType { get; set; }
	public float EffectValue { get; set; }
	public int RemainingUses { get; set; } // Battles or catch attempts remaining
	public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;

	public bool IsExpired => RemainingUses <= 0;
}
