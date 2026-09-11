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
	QuestItem,   // Special quest items
	Boost,       // Server-wide boosts (XP, Gold, etc.)
	Material     // Signature drops from beasts — Monster Hunter-style themed mats
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
	// Inert — used by Material items which drop from beasts and have no built-in
	// effect. They're consumed by trade nodes / side quests, not used directly.
	None,

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

	// Server boost effects (time-based, max 8 hours)
	ServerTamerXPBoost,    // Tamer XP multiplier
	ServerBeastXPBoost,    // Monster XP multiplier
	ServerGoldBoost,       // Gold multiplier
	ServerLuckyCharm,      // Increased rare item drop rate
	ServerRareEncounter    // Increased rare beast encounter rate
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
	public int TokenPrice { get; set; } // Boss Tokens store price (0 = not sold for tokens)

	/// <summary>
	/// Get a formatted description of the item's effects. EVERY non-zero effect the
	/// definition carries is listed — primary first, then the secondary — joined
	/// with " · " (e.g. "+8% ATK · +8% DEF", "+15% ATK · 10% chance to burn on hit").
	/// Single-effect items read exactly as before. Falls back to Description for
	/// inert items (materials).
	/// </summary>
	public string GetEffectDescription()
	{
		var parts = new List<string>();

		var primary = FormatEffect( EffectType, EffectValue );
		if ( !string.IsNullOrEmpty( primary ) ) parts.Add( primary );

		if ( SecondaryEffectType.HasValue && SecondaryEffectValue != 0 )
		{
			var secondary = FormatEffect( SecondaryEffectType.Value, SecondaryEffectValue );
			if ( !string.IsNullOrEmpty( secondary ) ) parts.Add( secondary );
		}

		return parts.Count > 0 ? string.Join( " · ", parts ) : Description;
	}

	/// <summary>
	/// The same fragments GetEffectDescription() joins, ONE PER SLOT, each tagged with
	/// its sign so a UI can draw a glyph instead of colouring the words (user ruling
	/// 2026-09-11: up = buff, down = penalty, dash = neutral). Positive / Negative come
	/// from the effect TYPE's polarity × the value's sign — never from the text — so a
	/// penalty that prints with a plus (HeldDamageTaken "+20% damage taken") reads as a
	/// penalty and a buff that prints with a minus (HeldPPReduction "-2 PP cost") reads
	/// as a buff. Neutral = text-only effects with no better/worse ("Set nature to X",
	/// "Randomly reroll one trait"). Inert items (materials, EffectType.None) return an
	/// EMPTY list — callers fall back to Description for those. GetEffectDescription()
	/// is unchanged for callers that want the sentence.
	/// </summary>
	public List<(bool Positive, bool Neutral, string Text)> GetEffectParts()
	{
		var parts = new List<(bool Positive, bool Neutral, string Text)>();
		AddPart( EffectType, EffectValue );
		if ( SecondaryEffectType.HasValue && SecondaryEffectValue != 0 )
			AddPart( SecondaryEffectType.Value, SecondaryEffectValue );
		return parts;

		void AddPart( ItemEffectType type, float value )
		{
			var text = FormatEffect( type, value );
			if ( string.IsNullOrEmpty( text ) ) return;
			var (positive, neutral) = Polarity( type, value );
			parts.Add( (positive, neutral, text) );
		}
	}

	/// <summary>
	/// Sign of one effect slot. Default = the value's sign (every "+N% stat" / "-N% stat"
	/// fragment, the grants, heals and multipliers all carry a positive value). The
	/// special cases are the types whose PRINTED sign and real polarity disagree, and
	/// the text-only neutrals.
	/// </summary>
	private static (bool Positive, bool Neutral) Polarity( ItemEffectType type, float value )
	{
		switch ( type )
		{
			case ItemEffectType.NatureChange:
			case ItemEffectType.TraitReroll:
				return (false, true);        // text-only — neither better nor worse
			case ItemEffectType.HeldDamageTaken:
				return (value < 0, false);   // a penalty even when it prints "+20%"
			case ItemEffectType.HeldPPReduction:
				return (value > 0, false);   // prints "-2 PP cost" but is a buff
			default:
				return (value >= 0, false);
		}
	}

	/// <summary>Signed percentage — "+8%" / "-10%".</summary>
	private static string Pct( float value ) => value >= 0 ? $"+{value}%" : $"{value}%";

	/// <summary>
	/// One effect → one plain-words fragment. Shared by the primary and secondary
	/// slots so a stat that appears in either reads the same way.
	/// </summary>
	private string FormatEffect( ItemEffectType type, float value )
	{
		return type switch
		{
			ItemEffectType.BoostATK => $"{Pct( value )} ATK for {EffectDuration} waves",
			ItemEffectType.BoostDEF => $"{Pct( value )} DEF for {EffectDuration} waves",
			ItemEffectType.BoostSPD => $"{Pct( value )} SPD for {EffectDuration} waves",
			ItemEffectType.BoostSpA => $"{Pct( value )} SpA for {EffectDuration} waves",
			ItemEffectType.BoostSpD => $"{Pct( value )} SpD for {EffectDuration} waves",
			ItemEffectType.BoostCrit => $"{Pct( value )} crit chance for {EffectDuration} waves",
			ItemEffectType.CatchRateBoost => EffectDuration > 0 ? $"{Pct( value )} contract rate for {EffectDuration} attempts" : $"{Pct( value )} contract rate",
			ItemEffectType.XPGrant => $"Grant {value:N0} XP to a beast",
			ItemEffectType.GoldBoost => $"{Pct( value )} gold for {EffectDuration} waves",
			ItemEffectType.NatureChange => $"Set nature to {(NatureType)(int)value}",
			ItemEffectType.TraitReroll => "Randomly reroll one trait from species pool",
			ItemEffectType.GeneBoost => $"Boost a random gene by +{(int)value} (max 30)",
			ItemEffectType.MasterInk => "Guarantees your next contract attempt succeeds",
			ItemEffectType.ContractInkGrant => $"Grants {(int)value} Contract Ink",
			ItemEffectType.EliteInkBuff => $"{Pct( value )} contract rate for {EffectDuration} minutes",
			ItemEffectType.PassiveGoldFind => $"{Pct( value )} gold from all sources",
			ItemEffectType.PassiveItemFind => $"{Pct( value )} item drop chance",
			ItemEffectType.PassiveCatchRate => $"{Pct( value )} contract rate",
			ItemEffectType.PassiveXPGain => $"{Pct( value )} beast XP",
			ItemEffectType.PassiveATKBoost => $"{Pct( value )} team ATK",
			ItemEffectType.PassiveDEFBoost => $"{Pct( value )} team DEF",
			ItemEffectType.PassiveSPDBoost => $"{Pct( value )} team SPD",
			ItemEffectType.PassiveHPBoost => $"{Pct( value )} team HP",
			ItemEffectType.PassiveCritRate => $"{Pct( value )} crit chance",
			ItemEffectType.PassiveTamerXP => $"{Pct( value )} tamer XP",
			ItemEffectType.PassiveInkSaver => $"{Pct( value )} ink save chance",
			ItemEffectType.PassiveHealingBoost => $"{Pct( value )} healing",
			ItemEffectType.HeldATKBonus => $"{Pct( value )} ATK",
			ItemEffectType.HeldDEFBonus => $"{Pct( value )} DEF",
			ItemEffectType.HeldSPDBonus => $"{Pct( value )} SPD",
			ItemEffectType.HeldHPBonus => $"{Pct( value )} HP",
			ItemEffectType.HeldSpABonus => $"{Pct( value )} SpA",
			ItemEffectType.HeldSpDBonus => $"{Pct( value )} SpD",
			ItemEffectType.HeldCritChance => $"{Pct( value )} crit chance",
			ItemEffectType.HeldCritDamage => $"{Pct( value )} crit damage",
			ItemEffectType.HeldXPBonus => $"{Pct( value )} XP gain",
			ItemEffectType.HeldGoldBonus => $"{Pct( value )} gold from waves",
			ItemEffectType.HeldElementBoost => $"{Pct( value )} {TargetElement} damage",
			ItemEffectType.HeldDamageTaken => $"{Pct( value )} damage taken",
			ItemEffectType.HeldFirstStrike => "Always move first on turn 1",
			ItemEffectType.HeldPPReduction => $"-{value} PP cost on all moves",
			ItemEffectType.HeldLifesteal => $"Heal {value}% HP on defeating enemy",
			ItemEffectType.HeldRegeneration => $"Heal {value}% max HP per turn",
			ItemEffectType.HeldVsHigherLevel => $"{Pct( value )} damage vs higher level foes",
			ItemEffectType.HeldSurvivalTurns => $"{Pct( value )} all stats after surviving {(int)SecondaryEffectValue} turns",
			ItemEffectType.HeldEvasion => $"{Pct( value )} evasion",
			ItemEffectType.HeldAccuracy => $"{Pct( value )} accuracy",
			ItemEffectType.HeldAllyScaling => $"{Pct( value )} all stats per ally in party",
			ItemEffectType.HeldBurnChance => $"{value}% chance to burn on hit",
			ItemEffectType.ServerTamerXPBoost => $"{value}x Tamer XP for {BoostDurationMinutes / 60}h (Server-wide)",
			ItemEffectType.ServerBeastXPBoost => $"{value}x Beast XP for {BoostDurationMinutes / 60}h (Server-wide)",
			ItemEffectType.ServerGoldBoost => $"{value}x Gold for {BoostDurationMinutes / 60}h (Server-wide)",
			ItemEffectType.ServerLuckyCharm => $"{value}x Rare Item Drops for {BoostDurationMinutes / 60}h (Server-wide)",
			ItemEffectType.ServerRareEncounter => $"{value}x Rare Beast Encounters for {BoostDurationMinutes / 60}h (Server-wide)",
			_ => ""
		};
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
