using Sandbox;
using Beastborne.Data;
using System.Linq;

namespace Beastborne.Core;

/// <summary>
/// Manages the item system - definitions, drops, inventory, and effects
/// </summary>
public sealed class ItemManager : Component
{
	public static ItemManager Instance { get; private set; }

	// Item database (all item definitions)
	private Dictionary<string, ItemDefinition> _itemDatabase = new();
	public IReadOnlyDictionary<string, ItemDefinition> ItemDatabase => _itemDatabase;

	// Drop tables by expedition element/level
	private Dictionary<string, DropTable> _dropTables = new();

	// Events
	public Action<ItemDefinition, int> OnItemAdded;
	public Action<ItemDefinition, int> OnItemRemoved;
	public Action<ItemDefinition> OnItemUsed;
	public Action<string> OnRelicEquipped;
	public Action<string> OnRelicUnequipped;

	// Track items the player hasn't seen yet (for NEW badge)
	private HashSet<string> _newItemIds = new();
	public int NewItemCount => _newItemIds.Count;

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			InitializeItemDatabase();
			InitializeDropTables();
			Log.Info( "ItemManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "ItemManager";
		go.Components.Create<ItemManager>();
	}

	// ============================================
	// ITEM DATABASE INITIALIZATION
	// ============================================

	private void InitializeItemDatabase()
	{
		_itemDatabase.Clear();

		// === CONSUMABLES ===
		AddItem( new ItemDefinition
		{
			Id = "boost_atk",
			Name = "Berserk Tonic",
			Description = "A fierce brew that amplifies physical power.",
			IconPath = "ui/items/consumables/berserk_tonic.png",
			Category = ItemCategory.Consumable,
			Rarity = ItemRarity.Uncommon,
			EffectType = ItemEffectType.BoostATK,
			EffectValue = 25,
			EffectDuration = 3,
			BuyPrice = 500,
			SellPrice = 125
		} );

		AddItem( new ItemDefinition
		{
			Id = "boost_def",
			Name = "Iron Skin Oil",
			Description = "A protective coating that hardens scales and hide.",
			IconPath = "ui/items/consumables/iron_skin_oil.png",
			Category = ItemCategory.Consumable,
			Rarity = ItemRarity.Uncommon,
			EffectType = ItemEffectType.BoostDEF,
			EffectValue = 25,
			EffectDuration = 3,
			BuyPrice = 500,
			SellPrice = 125
		} );

		AddItem( new ItemDefinition
		{
			Id = "boost_spd",
			Name = "Quickstep Powder",
			Description = "A stimulant that enhances reaction time.",
			IconPath = "ui/items/consumables/quickstep_powder.png",
			Category = ItemCategory.Consumable,
			Rarity = ItemRarity.Uncommon,
			EffectType = ItemEffectType.BoostSPD,
			EffectValue = 25,
			EffectDuration = 3,
			BuyPrice = 500,
			SellPrice = 125
		} );

		AddItem( new ItemDefinition
		{
			Id = "boost_spa",
			Name = "Arcane Draught",
			Description = "A mystical elixir that sharpens magical focus.",
			IconPath = "ui/items/consumables/arcane_draught.png",
			Category = ItemCategory.Consumable,
			Rarity = ItemRarity.Uncommon,
			EffectType = ItemEffectType.BoostSpA,
			EffectValue = 25,
			EffectDuration = 3,
			BuyPrice = 500,
			SellPrice = 125
		} );

		AddItem( new ItemDefinition
		{
			Id = "boost_spd_def",
			Name = "Warding Salve",
			Description = "A protective ointment that shields against magic.",
			IconPath = "ui/items/consumables/warding_salve.png",
			Category = ItemCategory.Consumable,
			Rarity = ItemRarity.Uncommon,
			EffectType = ItemEffectType.BoostSpD,
			EffectValue = 25,
			EffectDuration = 3,
			BuyPrice = 500,
			SellPrice = 125
		} );

		AddItem( new ItemDefinition
		{
			Id = "boost_crit",
			Name = "Hunter's Focus",
			Description = "A keen awareness potion that reveals weak points.",
			IconPath = "ui/items/consumables/hunters_focus.png",
			Category = ItemCategory.Consumable,
			Rarity = ItemRarity.Rare,
			EffectType = ItemEffectType.BoostCrit,
			EffectValue = 20,
			EffectDuration = 3,
			BuyPrice = 1000,
			SellPrice = 250
		} );

		AddItem( new ItemDefinition
		{
			Id = "catch_lure",
			Name = "Contract Incense",
			Description = "Fragrant smoke that makes beasts more receptive to contracts.",
			IconPath = "ui/items/consumables/contract_incense.png",
			Category = ItemCategory.Consumable,
			Rarity = ItemRarity.Rare,
			EffectType = ItemEffectType.CatchRateBoost,
			EffectValue = 15,
			EffectDuration = 5,
			BuyPrice = 1500,
			SellPrice = 375
		} );

		AddItem( new ItemDefinition
		{
			Id = "catch_prime",
			Name = "Premium Incense",
			Description = "Rare incense crafted from legendary herbs.",
			IconPath = "ui/items/consumables/premium_incense.png",
			Category = ItemCategory.Consumable,
			Rarity = ItemRarity.Epic,
			EffectType = ItemEffectType.CatchRateBoost,
			EffectValue = 30,
			EffectDuration = 3,
			BuyPrice = 5000,
			SellPrice = 1250
		} );

		AddItem( new ItemDefinition
		{
			Id = "xp_treat",
			Name = "Training Treat",
			Description = "A nutritious snack that grants experience.",
			IconPath = "ui/items/consumables/training_treat.png",
			Category = ItemCategory.Consumable,
			Rarity = ItemRarity.Uncommon,
			EffectType = ItemEffectType.XPGrant,
			EffectValue = 500,
			BuyPrice = 750,
			SellPrice = 188
		} );

		AddItem( new ItemDefinition
		{
			Id = "xp_feast",
			Name = "Training Feast",
			Description = "A hearty meal that grants significant experience.",
			IconPath = "ui/items/consumables/training_feast.png",
			Category = ItemCategory.Consumable,
			Rarity = ItemRarity.Rare,
			EffectType = ItemEffectType.XPGrant,
			EffectValue = 2000,
			BuyPrice = 2500,
			SellPrice = 625
		} );

		AddItem( new ItemDefinition
		{
			Id = "gold_bell",
			Name = "Fortune Chime",
			Description = "A lucky charm that attracts wealth.",
			IconPath = "ui/items/consumables/fortune_chime.png",
			Category = ItemCategory.Consumable,
			Rarity = ItemRarity.Rare,
			EffectType = ItemEffectType.GoldBoost,
			EffectValue = 50,
			EffectDuration = 5,
			BuyPrice = 2000,
			SellPrice = 500
		} );

		// === RELICS (Tamer Passives) ===
		AddItem( new ItemDefinition
		{
			Id = "relic_coin",
			Name = "Lucky Coin",
			Description = "An ancient coin that brings fortune to its holder, but its gleam distracts from other treasures.",
			IconPath = "ui/items/relics/lucky_coin.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveGoldFind,
			EffectValue = 10,
			SecondaryEffectType = ItemEffectType.PassiveItemFind,
			SecondaryEffectValue = -5,
			BuyPrice = 10000,
			SellPrice = 2500
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_magnet",
			Name = "Treasure Magnet",
			Description = "A lodestone that draws valuable items closer, but repels loose coins.",
			IconPath = "ui/items/relics/treasure_magnet.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveItemFind,
			EffectValue = 15,
			SecondaryEffectType = ItemEffectType.PassiveGoldFind,
			SecondaryEffectValue = -5,
			BuyPrice = 12000,
			SellPrice = 3000
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_wisdom",
			Name = "Tome of Wisdom",
			Description = "An enlightening text that hastens learning, though its weight slows your beasts' strikes.",
			IconPath = "ui/items/relics/tome_of_wisdom.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveXPGain,
			EffectValue = 10,
			SecondaryEffectType = ItemEffectType.PassiveATKBoost,
			SecondaryEffectValue = -5,
			BuyPrice = 10000,
			SellPrice = 2500
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_charm",
			Name = "Silver Charm",
			Description = "A blessed talisman that soothes wild beasts, but its calming aura dulls your coin purse.",
			IconPath = "ui/items/relics/silver_charm.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveCatchRate,
			EffectValue = 5,
			SecondaryEffectType = ItemEffectType.PassiveGoldFind,
			SecondaryEffectValue = -5,
			BuyPrice = 15000,
			SellPrice = 3750
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_crown",
			Name = "Crown of Avarice",
			Description = "A cursed crown that magnifies greed and reward, but leaves the wearer exposed.",
			IconPath = "ui/items/relics/crown_of_avarice.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Epic,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveGoldFind,
			EffectValue = 20,
			SecondaryEffectType = ItemEffectType.PassiveDEFBoost,
			SecondaryEffectValue = -8,
			BuyPrice = 50000,
			SellPrice = 12500
		} );

		// === UNCOMMON RELICS (Simple single positive effect) ===
		AddItem( new ItemDefinition
		{
			Id = "relic_warrior_sigil",
			Name = "Warrior's Sigil",
			Description = "A battle-worn crest that stirs aggression in nearby beasts.",
			IconPath = "ui/items/relics/warriors_sigil.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveATKBoost,
			EffectValue = 5,
			BuyPrice = 8000,
			SellPrice = 2000
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_guardian_emblem",
			Name = "Guardian's Emblem",
			Description = "A shield-shaped medallion that toughens the skin of your beasts.",
			IconPath = "ui/items/relics/guardians_emblem.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveDEFBoost,
			EffectValue = 5,
			BuyPrice = 8000,
			SellPrice = 2000
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_swift_feather",
			Name = "Swift Feather",
			Description = "A feather so light it seems to pull the wind along with it.",
			IconPath = "ui/items/relics/swift_feather.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveSPDBoost,
			EffectValue = 5,
			BuyPrice = 8000,
			SellPrice = 2000
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_vital_pendant",
			Name = "Vital Pendant",
			Description = "A warm stone pendant that pulses in rhythm with your beasts' hearts.",
			IconPath = "ui/items/relics/vital_pendant.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveHPBoost,
			EffectValue = 5,
			BuyPrice = 8000,
			SellPrice = 2000
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_scholars_lens",
			Name = "Scholar's Lens",
			Description = "A magnifying lens that reveals hidden lessons in every journey.",
			IconPath = "ui/items/relics/scholars_lens.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveTamerXP,
			EffectValue = 8,
			BuyPrice = 8000,
			SellPrice = 2000
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_ink_well",
			Name = "Ink Well",
			Description = "A bottomless inkpot that occasionally refills itself from thin air.",
			IconPath = "ui/items/relics/ink_well.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveInkSaver,
			EffectValue = 10,
			BuyPrice = 9000,
			SellPrice = 2250
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_critical_eye",
			Name = "Critical Eye",
			Description = "A glass eye that spots weak points others would miss.",
			IconPath = "ui/items/relics/critical_eye.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveCritRate,
			EffectValue = 3,
			BuyPrice = 9000,
			SellPrice = 2250
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_healers_brooch",
			Name = "Healer's Brooch",
			Description = "A flower-shaped pin infused with restorative energy.",
			IconPath = "ui/items/relics/healers_brooch.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveHealingBoost,
			EffectValue = 10,
			BuyPrice = 8000,
			SellPrice = 2000
		} );

		// === RARE RELICS (Stronger effects, some with tradeoffs) ===
		AddItem( new ItemDefinition
		{
			Id = "relic_berserker_fang",
			Name = "Berserker's Fang",
			Description = "A jagged fang that drives beasts into a reckless frenzy.",
			IconPath = "ui/items/relics/berserkers_fang.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveATKBoost,
			EffectValue = 12,
			SecondaryEffectType = ItemEffectType.PassiveDEFBoost,
			SecondaryEffectValue = -5,
			BuyPrice = 14000,
			SellPrice = 3500
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_iron_fortress",
			Name = "Iron Fortress",
			Description = "A heavy iron relic that hardens defenses at the cost of agility.",
			IconPath = "ui/items/relics/iron_fortress.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveDEFBoost,
			EffectValue = 12,
			SecondaryEffectType = ItemEffectType.PassiveSPDBoost,
			SecondaryEffectValue = -5,
			BuyPrice = 14000,
			SellPrice = 3500
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_windrunner_cloak",
			Name = "Windrunner's Cloak",
			Description = "A tattered cloak that trades protection for blinding speed.",
			IconPath = "ui/items/relics/windrunners_cloak.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveSPDBoost,
			EffectValue = 12,
			SecondaryEffectType = ItemEffectType.PassiveDEFBoost,
			SecondaryEffectValue = -5,
			BuyPrice = 14000,
			SellPrice = 3500
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_phoenix_feather",
			Name = "Phoenix Feather",
			Description = "A smoldering plume that empowers healing but dampens aggression.",
			IconPath = "ui/items/relics/phoenix_feather.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveHealingBoost,
			EffectValue = 25,
			SecondaryEffectType = ItemEffectType.PassiveATKBoost,
			SecondaryEffectValue = -5,
			BuyPrice = 14000,
			SellPrice = 3500
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_treasure_map",
			Name = "Treasure Map",
			Description = "A faded map marked with beast dens and nesting grounds.",
			IconPath = "ui/items/relics/treasure_map.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveCatchRate,
			EffectValue = 10,
			BuyPrice = 18000,
			SellPrice = 4500
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_golden_fleece",
			Name = "Golden Fleece",
			Description = "A gilded pelt that attracts gold but repels trinkets.",
			IconPath = "ui/items/relics/golden_fleece.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveGoldFind,
			EffectValue = 25,
			SecondaryEffectType = ItemEffectType.PassiveItemFind,
			SecondaryEffectValue = -10,
			BuyPrice = 15000,
			SellPrice = 3750
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_spelunker_lantern",
			Name = "Spelunker's Lantern",
			Description = "A dim lantern that reveals hidden treasures but blinds you to coin.",
			IconPath = "ui/items/relics/spelunkers_lantern.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveItemFind,
			EffectValue = 25,
			SecondaryEffectType = ItemEffectType.PassiveGoldFind,
			SecondaryEffectValue = -10,
			BuyPrice = 15000,
			SellPrice = 3750
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_veterans_medal",
			Name = "Veteran's Medal",
			Description = "Awarded to tamers who've walked a thousand roads. Wisdom follows.",
			IconPath = "ui/items/relics/veterans_medal.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveTamerXP,
			EffectValue = 15,
			SecondaryEffectType = ItemEffectType.PassiveXPGain,
			SecondaryEffectValue = 8,
			BuyPrice = 16000,
			SellPrice = 4000
		} );

		// === EPIC RELICS (Strong with notable tradeoffs) ===
		AddItem( new ItemDefinition
		{
			Id = "relic_blood_oath",
			Name = "Blood Oath Crystal",
			Description = "A crimson crystal sealed with a blood pact. Power demands sacrifice.",
			IconPath = "ui/items/relics/blood_oath_crystal.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Epic,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveATKBoost,
			EffectValue = 20,
			SecondaryEffectType = ItemEffectType.PassiveDEFBoost,
			SecondaryEffectValue = -10,
			BuyPrice = 35000,
			SellPrice = 8750
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_chrono_shard",
			Name = "Chrono Shard",
			Description = "A fragment of frozen time. Move faster, but your body pays the price.",
			IconPath = "ui/items/relics/chrono_shard.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Epic,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveSPDBoost,
			EffectValue = 20,
			SecondaryEffectType = ItemEffectType.PassiveHPBoost,
			SecondaryEffectValue = -10,
			BuyPrice = 35000,
			SellPrice = 8750
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_midas_hand",
			Name = "Midas Hand",
			Description = "Everything you touch turns to gold. Everything else slips away.",
			IconPath = "ui/items/relics/midas_hand.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Epic,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveGoldFind,
			EffectValue = 40,
			SecondaryEffectType = ItemEffectType.PassiveItemFind,
			SecondaryEffectValue = -20,
			BuyPrice = 40000,
			SellPrice = 10000
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_beast_totem",
			Name = "Beast Whisperer Totem",
			Description = "Wild beasts trust you more, but merchants trust you less.",
			IconPath = "ui/items/relics/beast_whisperer_totem.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Epic,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveCatchRate,
			EffectValue = 15,
			SecondaryEffectType = ItemEffectType.PassiveGoldFind,
			SecondaryEffectValue = -15,
			BuyPrice = 38000,
			SellPrice = 9500
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_war_drum",
			Name = "War Drum",
			Description = "Its thundering beat sharpens focus but leaves you exposed.",
			IconPath = "ui/items/relics/war_drum.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Epic,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveCritRate,
			EffectValue = 12,
			SecondaryEffectType = ItemEffectType.PassiveDEFBoost,
			SecondaryEffectValue = -8,
			BuyPrice = 35000,
			SellPrice = 8750
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_life_anchor",
			Name = "Life Anchor",
			Description = "A heavy anchor rune that roots your beasts in vitality but slows them.",
			IconPath = "ui/items/relics/life_anchor.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Epic,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveHPBoost,
			EffectValue = 20,
			SecondaryEffectType = ItemEffectType.PassiveSPDBoost,
			SecondaryEffectValue = -10,
			BuyPrice = 35000,
			SellPrice = 8750
		} );

		// === LEGENDARY RELICS (Powerful with significant tradeoffs) ===
		AddItem( new ItemDefinition
		{
			Id = "relic_abyssal_crown",
			Name = "Abyssal Crown",
			Description = "A crown forged in the abyss. Grants devastating power at a terrible cost.",
			IconPath = "ui/items/relics/abyssal_crown.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Legendary,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveATKBoost,
			EffectValue = 30,
			SecondaryEffectType = ItemEffectType.PassiveDEFBoost,
			SecondaryEffectValue = -15,
			BuyPrice = 75000,
			SellPrice = 18750
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_eternity_glass",
			Name = "Eternity Hourglass",
			Description = "Time bends for those who hold it. Your beasts learn fast, but fortune fades.",
			IconPath = "ui/items/relics/eternity_hourglass.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Legendary,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveXPGain,
			EffectValue = 35,
			SecondaryEffectType = ItemEffectType.PassiveGoldFind,
			SecondaryEffectValue = -20,
			BuyPrice = 75000,
			SellPrice = 18750
		} );

		AddItem( new ItemDefinition
		{
			Id = "relic_soul_mirror",
			Name = "Soul Mirror",
			Description = "Reflects your beasts' killing intent, but fractures their life force.",
			IconPath = "ui/items/relics/soul_mirror.png",
			Category = ItemCategory.Relic,
			Rarity = ItemRarity.Legendary,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.PassiveCritRate,
			EffectValue = 20,
			SecondaryEffectType = ItemEffectType.PassiveHPBoost,
			SecondaryEffectValue = -15,
			BuyPrice = 80000,
			SellPrice = 20000
		} );

		// === HELD ITEMS (Monster Equipment) ===
		InitializeHeldItems();

		// === QUEST ITEMS ===
		AddItem( new ItemDefinition
		{
			Id = "map_nightmare",
			Name = "Nightmare Map",
			Description = "A cursed map revealing paths through nightmares.",
			IconPath = "ui/items/quest/nightmare_map.png",
			Category = ItemCategory.QuestItem,
			Rarity = ItemRarity.Epic,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.UnlockCartographerMode,
			UnlocksModeId = "nightmare_mode",
			SellPrice = 0
		} );

		AddItem( new ItemDefinition
		{
			Id = "map_element",
			Name = "Elemental Compass",
			Description = "A compass that tracks elemental concentrations.",
			IconPath = "ui/items/quest/elemental_compass.png",
			Category = ItemCategory.QuestItem,
			Rarity = ItemRarity.Epic,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.UnlockCartographerMode,
			UnlocksModeId = "element_hunt",
			SellPrice = 0
		} );

		AddItem( new ItemDefinition
		{
			Id = "map_boss",
			Name = "Boss Tracker",
			Description = "A device that detects powerful boss signatures.",
			IconPath = "ui/items/quest/boss_tracker.png",
			Category = ItemCategory.QuestItem,
			Rarity = ItemRarity.Epic,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.UnlockCartographerMode,
			UnlocksModeId = "boss_rush",
			SellPrice = 0
		} );

		AddItem( new ItemDefinition
		{
			Id = "map_den",
			Name = "Rare Den Location",
			Description = "Coordinates to a hidden den of rare beasts.",
			IconPath = "ui/items/quest/rare_den_location.png",
			Category = ItemCategory.QuestItem,
			Rarity = ItemRarity.Legendary,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.UnlockCartographerMode,
			UnlocksModeId = "rare_den",
			SellPrice = 0
		} );

		Log.Info( $"Loaded {_itemDatabase.Count} items" );
	}

	private void InitializeHeldItems()
	{
		// === NEUTRAL/GENERAL ITEMS ===
		AddItem( new ItemDefinition
		{
			Id = "held_training_weight",
			Name = "Training Weight",
			Description = "Heavy weights that build strength and endurance.",
			IconPath = "ui/items/held_items/training_weight.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Common,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldATKBonus,
			EffectValue = 8,
			SecondaryEffectType = ItemEffectType.HeldDEFBonus,
			SecondaryEffectValue = 8,
			BuyPrice = 1000,
			SellPrice = 250
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_growth_ring",
			Name = "Growth Ring",
			Description = "A ring that channels experience more efficiently.",
			IconPath = "ui/items/held_items/growth_ring.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldXPBonus,
			EffectValue = 40,
			BuyPrice = 3000,
			SellPrice = 750
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_fortune_coin",
			Name = "Fortune Coin",
			Description = "A lucky coin that attracts gold.",
			IconPath = "ui/items/held_items/fortune_coin.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldGoldBonus,
			EffectValue = 30,
			BuyPrice = 3000,
			SellPrice = 750
		} );

		// === OFFENSIVE ITEMS (Fire, Electric) ===
		AddItem( new ItemDefinition
		{
			Id = "held_glass_cannon",
			Name = "Glass Cannon",
			Description = "Amplifies damage but leaves the wielder vulnerable.",
			IconPath = "ui/items/held_items/glass_cannon.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldATKBonus,
			EffectValue = 20,
			SecondaryEffectType = ItemEffectType.HeldDamageTaken,
			SecondaryEffectValue = 20,
			BuyPrice = 2500,
			SellPrice = 625
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_hunters_mark",
			Name = "Hunter's Mark",
			Description = "A sigil that reveals critical weak points.",
			IconPath = "ui/items/held_items/hunters_mark.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldCritChance,
			EffectValue = 10,
			BuyPrice = 2500,
			SellPrice = 625
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_ember_fang",
			Name = "Ember Fang",
			Description = "A fang imbued with smoldering fire.",
			IconPath = "ui/items/held_items/ember_fang.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldATKBonus,
			EffectValue = 15,
			SecondaryEffectType = ItemEffectType.HeldBurnChance,
			SecondaryEffectValue = 10,
			BuyPrice = 5000,
			SellPrice = 1250
		} );

		// === DEFENSIVE ITEMS (Water, Earth) ===
		AddItem( new ItemDefinition
		{
			Id = "held_titans_heart",
			Name = "Titan's Heart",
			Description = "A massive heart that grants incredible vitality.",
			IconPath = "ui/items/held_items/titans_heart.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldHPBonus,
			EffectValue = 25,
			SecondaryEffectType = ItemEffectType.HeldSPDBonus,
			SecondaryEffectValue = -10,
			BuyPrice = 5000,
			SellPrice = 1250
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_stone_shell",
			Name = "Stone Shell",
			Description = "A fossilized shell that provides protection.",
			IconPath = "ui/items/held_items/stone_shell.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldDEFBonus,
			EffectValue = 15,
			SecondaryEffectType = ItemEffectType.HeldSpDBonus,
			SecondaryEffectValue = 15,
			BuyPrice = 2500,
			SellPrice = 625
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_arcane_focus",
			Name = "Arcane Focus",
			Description = "A crystal that enhances magical abilities.",
			IconPath = "ui/items/held_items/arcane_focus.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Common,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldSpABonus,
			EffectValue = 8,
			SecondaryEffectType = ItemEffectType.HeldSpDBonus,
			SecondaryEffectValue = 8,
			BuyPrice = 1000,
			SellPrice = 250
		} );

		// === SPEED ITEMS (Wind) ===
		AddItem( new ItemDefinition
		{
			Id = "held_agility_anklet",
			Name = "Agility Anklet",
			Description = "A lightweight anklet that enhances speed.",
			IconPath = "ui/items/held_items/agility_anklet.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Common,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldSPDBonus,
			EffectValue = 12,
			BuyPrice = 1000,
			SellPrice = 250
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_gale_charm",
			Name = "Gale Charm",
			Description = "A charm blessed by the wind spirits.",
			IconPath = "ui/items/held_items/gale_charm.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldFirstStrike,
			EffectValue = 1,
			BuyPrice = 5000,
			SellPrice = 1250
		} );

		// === MAGIC ITEMS (Ice) ===
		AddItem( new ItemDefinition
		{
			Id = "held_frost_lens",
			Name = "Frost Lens",
			Description = "A lens that focuses magical energy.",
			IconPath = "ui/items/held_items/frost_lens.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldSpABonus,
			EffectValue = 15,
			BuyPrice = 2500,
			SellPrice = 625
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_mana_crystal",
			Name = "Mana Crystal",
			Description = "A crystal that reduces the cost of moves.",
			IconPath = "ui/items/held_items/mana_crystal.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldPPReduction,
			EffectValue = 1,
			BuyPrice = 8000,
			SellPrice = 2000
		} );

		// === SUSTAIN ITEMS (Nature) ===
		AddItem( new ItemDefinition
		{
			Id = "held_blood_gem",
			Name = "Blood Gem",
			Description = "A crimson gem that absorbs life force.",
			IconPath = "ui/items/held_items/blood_gem.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldLifesteal,
			EffectValue = 3,
			BuyPrice = 6000,
			SellPrice = 1500
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_life_root",
			Name = "Life Root",
			Description = "A living root that slowly regenerates health.",
			IconPath = "ui/items/held_items/life_root.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Epic,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldRegeneration,
			EffectValue = 2,
			BuyPrice = 15000,
			SellPrice = 3750
		} );

		// === GOLD ITEMS (Metal) ===
		AddItem( new ItemDefinition
		{
			Id = "held_gilded_crest",
			Name = "Gilded Crest",
			Description = "A golden crest that attracts wealth.",
			IconPath = "ui/items/held_items/gilded_crest.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldGoldBonus,
			EffectValue = 50,
			BuyPrice = 8000,
			SellPrice = 2000
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_merchants_sigil",
			Name = "Merchant's Sigil",
			Description = "A sigil that brings fortune in all forms.",
			IconPath = "ui/items/held_items/merchants_sigil.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldGoldBonus,
			EffectValue = 25,
			BuyPrice = 3000,
			SellPrice = 750
		} );

		// === UTILITY ITEMS (Spirit) ===
		AddItem( new ItemDefinition
		{
			Id = "held_contract_seal",
			Name = "Contract Seal",
			Description = "A seal that makes beasts more willing to form contracts.",
			IconPath = "ui/items/held_items/contract_seal.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.CatchRateBoost,
			EffectValue = 15,
			BuyPrice = 10000,
			SellPrice = 2500
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_harmony_bell",
			Name = "Harmony Bell",
			Description = "A bell that strengthens bonds between allies.",
			IconPath = "ui/items/held_items/harmony_bell.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Epic,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldAllyScaling,
			EffectValue = 5,
			BuyPrice = 20000,
			SellPrice = 5000
		} );

		// === CONDITIONAL ITEMS (Shadow) ===
		AddItem( new ItemDefinition
		{
			Id = "held_underdog_badge",
			Name = "Underdog Badge",
			Description = "A badge that empowers those facing stronger foes.",
			IconPath = "ui/items/held_items/underdog_badge.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Uncommon,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldVsHigherLevel,
			EffectValue = 25,
			BuyPrice = 3000,
			SellPrice = 750
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_veterans_medal",
			Name = "Veteran's Medal",
			Description = "A medal that rewards those who endure.",
			IconPath = "ui/items/held_items/veterans_medal.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldSurvivalTurns,
			EffectValue = 15,
			SecondaryEffectValue = 3, // After 3 turns
			BuyPrice = 6000,
			SellPrice = 1500
		} );

		AddItem( new ItemDefinition
		{
			Id = "held_shadow_cloak",
			Name = "Shadow Cloak",
			Description = "A cloak that makes the wearer harder to hit.",
			IconPath = "ui/items/held_items/shadow_cloak.png",
			Category = ItemCategory.HeldItem,
			Rarity = ItemRarity.Rare,
			IsStackable = false,
			MaxStack = 1,
			EffectType = ItemEffectType.HeldEvasion,
			EffectValue = 20,
			SecondaryEffectType = ItemEffectType.HeldAccuracy,
			SecondaryEffectValue = -10,
			BuyPrice = 6000,
			SellPrice = 1500
		} );

		// ============================================
		// SERVER BOOSTS (stored in inventory, used manually)
		// ============================================

		// Tamer XP Boosts
		AddItem( new ItemDefinition
		{
			Id = "boost_tamer_xp_1h",
			Name = "Tamer XP Boost (1h)",
			Description = "Doubles Tamer XP gain for 1 hour. Server-wide!",
			IconPath = "ui/items/boosts/tamer_xp_scroll.png",
			Category = ItemCategory.Boost,
			Rarity = ItemRarity.Rare,
			IsStackable = true,
			MaxStack = 10,
			EffectType = ItemEffectType.ServerTamerXPBoost,
			EffectValue = 2.0f,
			BoostDurationMinutes = 60,
			BuyPrice = 0,
			SellPrice = 0
		} );

		AddItem( new ItemDefinition
		{
			Id = "boost_tamer_xp_2h",
			Name = "Tamer XP Boost (2h)",
			Description = "Doubles Tamer XP gain for 2 hours. Server-wide!",
			IconPath = "ui/items/boosts/tamer_xp_scroll.png",
			Category = ItemCategory.Boost,
			Rarity = ItemRarity.Epic,
			IsStackable = true,
			MaxStack = 10,
			EffectType = ItemEffectType.ServerTamerXPBoost,
			EffectValue = 2.0f,
			BoostDurationMinutes = 120,
			BuyPrice = 0,
			SellPrice = 0
		} );

		// Beast XP Boosts
		AddItem( new ItemDefinition
		{
			Id = "boost_beast_xp_1h",
			Name = "Beast XP Boost (1h)",
			Description = "Doubles Beast XP gain for 1 hour. Server-wide!",
			IconPath = "ui/items/boosts/beast_xp_tome.png",
			Category = ItemCategory.Boost,
			Rarity = ItemRarity.Rare,
			IsStackable = true,
			MaxStack = 10,
			EffectType = ItemEffectType.ServerBeastXPBoost,
			EffectValue = 2.0f,
			BoostDurationMinutes = 60,
			BuyPrice = 0,
			SellPrice = 0
		} );

		AddItem( new ItemDefinition
		{
			Id = "boost_beast_xp_2h",
			Name = "Beast XP Boost (2h)",
			Description = "Doubles Beast XP gain for 2 hours. Server-wide!",
			IconPath = "ui/items/boosts/beast_xp_tome.png",
			Category = ItemCategory.Boost,
			Rarity = ItemRarity.Epic,
			IsStackable = true,
			MaxStack = 10,
			EffectType = ItemEffectType.ServerBeastXPBoost,
			EffectValue = 2.0f,
			BoostDurationMinutes = 120,
			BuyPrice = 0,
			SellPrice = 0
		} );

		// Gold Boosts
		AddItem( new ItemDefinition
		{
			Id = "boost_gold_1h",
			Name = "Gold Boost (1h)",
			Description = "Doubles Gold gain for 1 hour. Server-wide!",
			IconPath = "ui/items/boosts/gold_multiplier.png",
			Category = ItemCategory.Boost,
			Rarity = ItemRarity.Rare,
			IsStackable = true,
			MaxStack = 10,
			EffectType = ItemEffectType.ServerGoldBoost,
			EffectValue = 2.0f,
			BoostDurationMinutes = 60,
			BuyPrice = 0,
			SellPrice = 0
		} );

		AddItem( new ItemDefinition
		{
			Id = "boost_gold_2h",
			Name = "Gold Boost (2h)",
			Description = "Doubles Gold gain for 2 hours. Server-wide!",
			IconPath = "ui/items/boosts/gold_multiplier.png",
			Category = ItemCategory.Boost,
			Rarity = ItemRarity.Epic,
			IsStackable = true,
			MaxStack = 10,
			EffectType = ItemEffectType.ServerGoldBoost,
			EffectValue = 2.0f,
			BoostDurationMinutes = 120,
			BuyPrice = 0,
			SellPrice = 0
		} );

		// Lucky Charm Boosts
		AddItem( new ItemDefinition
		{
			Id = "boost_lucky_1h",
			Name = "Lucky Charm (1h)",
			Description = "+25% catch rate for 1 hour. Server-wide!",
			IconPath = "ui/items/boosts/lucky_clover.png",
			Category = ItemCategory.Boost,
			Rarity = ItemRarity.Rare,
			IsStackable = true,
			MaxStack = 10,
			EffectType = ItemEffectType.ServerLuckyCharm,
			EffectValue = 25f,
			BoostDurationMinutes = 60,
			BuyPrice = 0,
			SellPrice = 0
		} );

		AddItem( new ItemDefinition
		{
			Id = "boost_lucky_2h",
			Name = "Lucky Charm (2h)",
			Description = "+25% catch rate for 2 hours. Server-wide!",
			IconPath = "ui/items/boosts/lucky_clover.png",
			Category = ItemCategory.Boost,
			Rarity = ItemRarity.Epic,
			IsStackable = true,
			MaxStack = 10,
			EffectType = ItemEffectType.ServerLuckyCharm,
			EffectValue = 25f,
			BoostDurationMinutes = 120,
			BuyPrice = 0,
			SellPrice = 0
		} );

		// Rare Encounter Boosts
		AddItem( new ItemDefinition
		{
			Id = "boost_rare_1h",
			Name = "Rare Radar (1h)",
			Description = "+50% rare encounter chance for 1 hour. Server-wide!",
			IconPath = "ui/items/boosts/rare_radar.png",
			Category = ItemCategory.Boost,
			Rarity = ItemRarity.Rare,
			IsStackable = true,
			MaxStack = 10,
			EffectType = ItemEffectType.ServerRareEncounter,
			EffectValue = 50f,
			BoostDurationMinutes = 60,
			BuyPrice = 0,
			SellPrice = 0
		} );

		AddItem( new ItemDefinition
		{
			Id = "boost_rare_2h",
			Name = "Rare Radar (2h)",
			Description = "+50% rare encounter chance for 2 hours. Server-wide!",
			IconPath = "ui/items/boosts/rare_radar.png",
			Category = ItemCategory.Boost,
			Rarity = ItemRarity.Epic,
			IsStackable = true,
			MaxStack = 10,
			EffectType = ItemEffectType.ServerRareEncounter,
			EffectValue = 50f,
			BoostDurationMinutes = 120,
			BuyPrice = 0,
			SellPrice = 0
		} );
	}

	private void AddItem( ItemDefinition item )
	{
		_itemDatabase[item.Id] = item;
	}

	// ============================================
	// DROP TABLE INITIALIZATION
	// ============================================

	private void InitializeDropTables()
	{
		_dropTables.Clear();

		// Base drop table (all expeditions) - consumables
		_dropTables["base"] = new DropTable
		{
			Id = "base",
			BaseDropChance = 0.03f,
			Entries = new()
			{
				new() { ItemId = "boost_atk", Weight = 100 },
				new() { ItemId = "boost_def", Weight = 100 },
				new() { ItemId = "boost_spd", Weight = 100 },
				new() { ItemId = "boost_spa", Weight = 100 },
				new() { ItemId = "boost_spd_def", Weight = 100 },
				new() { ItemId = "xp_treat", Weight = 80 },
				new() { ItemId = "boost_crit", Weight = 40, MinExpeditionLevel = 20 },
				new() { ItemId = "xp_feast", Weight = 30, MinExpeditionLevel = 30 },
				new() { ItemId = "catch_lure", Weight = 25, MinExpeditionLevel = 15 },
				new() { ItemId = "gold_bell", Weight = 20, MinExpeditionLevel = 25 },
				new() { ItemId = "catch_prime", Weight = 10, MinExpeditionLevel = 50 },
			}
		};

		// Neutral expeditions - general held items + XP relics
		_dropTables["neutral"] = new DropTable
		{
			Id = "neutral",
			Element = ElementType.Neutral,
			BaseDropChance = 0.03f,
			Entries = new()
			{
				new() { ItemId = "held_training_weight", Weight = 100 },
				new() { ItemId = "held_growth_ring", Weight = 50, MinExpeditionLevel = 20 },
				new() { ItemId = "held_fortune_coin", Weight = 50, MinExpeditionLevel = 20 },
				new() { ItemId = "relic_scholars_lens", Weight = 15, MinExpeditionLevel = 15 },
				new() { ItemId = "relic_veterans_medal", Weight = 8, MinExpeditionLevel = 35 },
			}
		};

		// Fire expeditions - offensive items + ATK relics
		_dropTables["fire"] = new DropTable
		{
			Id = "fire",
			Element = ElementType.Fire,
			BaseDropChance = 0.03f,
			Entries = new()
			{
				new() { ItemId = "held_glass_cannon", Weight = 80 },
				new() { ItemId = "held_hunters_mark", Weight = 80 },
				new() { ItemId = "held_ember_fang", Weight = 30, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_warrior_sigil", Weight = 15, MinExpeditionLevel = 15 },
				new() { ItemId = "relic_berserker_fang", Weight = 8, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_blood_oath", Weight = 3, MinExpeditionLevel = 50 },
			}
		};

		// Water expeditions - defensive items + DEF relics
		_dropTables["water"] = new DropTable
		{
			Id = "water",
			Element = ElementType.Water,
			BaseDropChance = 0.03f,
			Entries = new()
			{
				new() { ItemId = "held_arcane_focus", Weight = 100 },
				new() { ItemId = "held_stone_shell", Weight = 80 },
				new() { ItemId = "held_titans_heart", Weight = 30, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_guardian_emblem", Weight = 15, MinExpeditionLevel = 15 },
				new() { ItemId = "relic_iron_fortress", Weight = 8, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_life_anchor", Weight = 3, MinExpeditionLevel = 50 },
			}
		};

		// Wind expeditions - speed items + SPD relics
		_dropTables["wind"] = new DropTable
		{
			Id = "wind",
			Element = ElementType.Wind,
			BaseDropChance = 0.03f,
			Entries = new()
			{
				new() { ItemId = "held_agility_anklet", Weight = 100 },
				new() { ItemId = "held_gale_charm", Weight = 30, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_swift_feather", Weight = 15, MinExpeditionLevel = 15 },
				new() { ItemId = "relic_windrunner_cloak", Weight = 8, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_chrono_shard", Weight = 3, MinExpeditionLevel = 50 },
			}
		};

		// Electric expeditions - offensive items + ATK relics
		_dropTables["electric"] = new DropTable
		{
			Id = "electric",
			Element = ElementType.Electric,
			BaseDropChance = 0.03f,
			Entries = new()
			{
				new() { ItemId = "held_glass_cannon", Weight = 80 },
				new() { ItemId = "held_hunters_mark", Weight = 80 },
				new() { ItemId = "relic_warrior_sigil", Weight = 15, MinExpeditionLevel = 15 },
				new() { ItemId = "relic_abyssal_crown", Weight = 1, MinExpeditionLevel = 70 },
			}
		};

		// Earth expeditions - defensive/HP items + exploration relics
		_dropTables["earth"] = new DropTable
		{
			Id = "earth",
			Element = ElementType.Earth,
			BaseDropChance = 0.03f,
			Entries = new()
			{
				new() { ItemId = "held_training_weight", Weight = 100 },
				new() { ItemId = "held_stone_shell", Weight = 80 },
				new() { ItemId = "held_titans_heart", Weight = 30, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_vital_pendant", Weight = 15, MinExpeditionLevel = 15 },
				new() { ItemId = "relic_treasure_map", Weight = 8, MinExpeditionLevel = 35 },
			}
		};

		// Ice expeditions - magic/precision items + crit relics
		_dropTables["ice"] = new DropTable
		{
			Id = "ice",
			Element = ElementType.Ice,
			BaseDropChance = 0.03f,
			Entries = new()
			{
				new() { ItemId = "held_arcane_focus", Weight = 100 },
				new() { ItemId = "held_frost_lens", Weight = 80 },
				new() { ItemId = "held_mana_crystal", Weight = 30, MinExpeditionLevel = 40 },
				new() { ItemId = "relic_critical_eye", Weight = 15, MinExpeditionLevel = 20 },
				new() { ItemId = "relic_war_drum", Weight = 3, MinExpeditionLevel = 50 },
				new() { ItemId = "relic_soul_mirror", Weight = 1, MinExpeditionLevel = 70 },
			}
		};

		// Nature expeditions - sustain items + healing relics
		_dropTables["nature"] = new DropTable
		{
			Id = "nature",
			Element = ElementType.Nature,
			BaseDropChance = 0.03f,
			Entries = new()
			{
				new() { ItemId = "held_growth_ring", Weight = 100 },
				new() { ItemId = "held_blood_gem", Weight = 40, MinExpeditionLevel = 35 },
				new() { ItemId = "held_life_root", Weight = 15, MinExpeditionLevel = 55 },
				new() { ItemId = "relic_healers_brooch", Weight = 15, MinExpeditionLevel = 15 },
				new() { ItemId = "relic_phoenix_feather", Weight = 8, MinExpeditionLevel = 35 },
			}
		};

		// Metal expeditions - gold items + economy relics
		_dropTables["metal"] = new DropTable
		{
			Id = "metal",
			Element = ElementType.Metal,
			BaseDropChance = 0.03f,
			Entries = new()
			{
				new() { ItemId = "held_fortune_coin", Weight = 100 },
				new() { ItemId = "held_merchants_sigil", Weight = 80 },
				new() { ItemId = "held_gilded_crest", Weight = 30, MinExpeditionLevel = 40 },
				new() { ItemId = "relic_golden_fleece", Weight = 8, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_midas_hand", Weight = 3, MinExpeditionLevel = 50 },
			}
		};

		// Spirit expeditions - utility items + catching relics
		_dropTables["spirit"] = new DropTable
		{
			Id = "spirit",
			Element = ElementType.Spirit,
			BaseDropChance = 0.03f,
			Entries = new()
			{
				new() { ItemId = "held_contract_seal", Weight = 50, MinExpeditionLevel = 30 },
				new() { ItemId = "held_harmony_bell", Weight = 15, MinExpeditionLevel = 55 },
				new() { ItemId = "relic_ink_well", Weight = 15, MinExpeditionLevel = 20 },
				new() { ItemId = "relic_beast_totem", Weight = 3, MinExpeditionLevel = 50 },
			}
		};

		// Shadow expeditions - conditional items + hidden reward relics
		_dropTables["shadow"] = new DropTable
		{
			Id = "shadow",
			Element = ElementType.Shadow,
			BaseDropChance = 0.03f,
			Entries = new()
			{
				new() { ItemId = "held_underdog_badge", Weight = 80 },
				new() { ItemId = "held_veterans_medal", Weight = 40, MinExpeditionLevel = 40 },
				new() { ItemId = "held_shadow_cloak", Weight = 40, MinExpeditionLevel = 40 },
				new() { ItemId = "relic_spelunker_lantern", Weight = 8, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_eternity_glass", Weight = 1, MinExpeditionLevel = 70 },
			}
		};

		// Boss drop table - better rewards
		_dropTables["boss"] = new DropTable
		{
			Id = "boss",
			BaseDropChance = 0.25f,
			Entries = new()
			{
				new() { ItemId = "xp_feast", Weight = 100 },
				new() { ItemId = "boost_crit", Weight = 80 },
				new() { ItemId = "catch_lure", Weight = 60 },
				new() { ItemId = "gold_bell", Weight = 50 },
				new() { ItemId = "catch_prime", Weight = 20, MinExpeditionLevel = 40 },
				// Relics from bosses (rare)
				new() { ItemId = "relic_coin", Weight = 10, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_magnet", Weight = 10, MinExpeditionLevel = 35 },
				new() { ItemId = "relic_wisdom", Weight = 10, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_charm", Weight = 8, MinExpeditionLevel = 40 },
				new() { ItemId = "relic_crown", Weight = 3, MinExpeditionLevel = 60 },
				// Uncommon relics
				new() { ItemId = "relic_warrior_sigil", Weight = 12, MinExpeditionLevel = 15 },
				new() { ItemId = "relic_guardian_emblem", Weight = 12, MinExpeditionLevel = 15 },
				new() { ItemId = "relic_swift_feather", Weight = 12, MinExpeditionLevel = 15 },
				new() { ItemId = "relic_vital_pendant", Weight = 12, MinExpeditionLevel = 15 },
				new() { ItemId = "relic_scholars_lens", Weight = 12, MinExpeditionLevel = 20 },
				new() { ItemId = "relic_ink_well", Weight = 12, MinExpeditionLevel = 20 },
				new() { ItemId = "relic_critical_eye", Weight = 12, MinExpeditionLevel = 25 },
				new() { ItemId = "relic_healers_brooch", Weight = 12, MinExpeditionLevel = 20 },
				// Rare tradeoff relics
				new() { ItemId = "relic_berserker_fang", Weight = 8, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_iron_fortress", Weight = 8, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_windrunner_cloak", Weight = 8, MinExpeditionLevel = 30 },
				new() { ItemId = "relic_phoenix_feather", Weight = 8, MinExpeditionLevel = 35 },
				new() { ItemId = "relic_treasure_map", Weight = 7, MinExpeditionLevel = 35 },
				new() { ItemId = "relic_golden_fleece", Weight = 8, MinExpeditionLevel = 35 },
				new() { ItemId = "relic_spelunker_lantern", Weight = 8, MinExpeditionLevel = 35 },
				new() { ItemId = "relic_veterans_medal", Weight = 7, MinExpeditionLevel = 40 },
				// Epic relics
				new() { ItemId = "relic_blood_oath", Weight = 4, MinExpeditionLevel = 50 },
				new() { ItemId = "relic_chrono_shard", Weight = 4, MinExpeditionLevel = 50 },
				new() { ItemId = "relic_midas_hand", Weight = 4, MinExpeditionLevel = 55 },
				new() { ItemId = "relic_beast_totem", Weight = 4, MinExpeditionLevel = 55 },
				new() { ItemId = "relic_war_drum", Weight = 4, MinExpeditionLevel = 50 },
				new() { ItemId = "relic_life_anchor", Weight = 4, MinExpeditionLevel = 50 },
				// Legendary relics
				new() { ItemId = "relic_abyssal_crown", Weight = 1, MinExpeditionLevel = 70 },
				new() { ItemId = "relic_eternity_glass", Weight = 1, MinExpeditionLevel = 70 },
				new() { ItemId = "relic_soul_mirror", Weight = 1, MinExpeditionLevel = 75 },
			}
		};

		// Rare boss drop table - guaranteed good rewards
		_dropTables["boss_rare"] = new DropTable
		{
			Id = "boss_rare",
			BaseDropChance = 1.0f,
			Entries = new()
			{
				new() { ItemId = "catch_prime", Weight = 100 },
				new() { ItemId = "relic_coin", Weight = 50 },
				new() { ItemId = "relic_magnet", Weight = 50 },
				new() { ItemId = "relic_wisdom", Weight = 50 },
				new() { ItemId = "relic_charm", Weight = 40 },
				new() { ItemId = "relic_crown", Weight = 20 },
				// Uncommon relics
				new() { ItemId = "relic_warrior_sigil", Weight = 60 },
				new() { ItemId = "relic_guardian_emblem", Weight = 60 },
				new() { ItemId = "relic_swift_feather", Weight = 60 },
				new() { ItemId = "relic_vital_pendant", Weight = 60 },
				new() { ItemId = "relic_scholars_lens", Weight = 60 },
				new() { ItemId = "relic_ink_well", Weight = 60 },
				new() { ItemId = "relic_critical_eye", Weight = 60 },
				new() { ItemId = "relic_healers_brooch", Weight = 60 },
				// Rare tradeoff relics
				new() { ItemId = "relic_berserker_fang", Weight = 40 },
				new() { ItemId = "relic_iron_fortress", Weight = 40 },
				new() { ItemId = "relic_windrunner_cloak", Weight = 40 },
				new() { ItemId = "relic_phoenix_feather", Weight = 40 },
				new() { ItemId = "relic_treasure_map", Weight = 35 },
				new() { ItemId = "relic_golden_fleece", Weight = 40 },
				new() { ItemId = "relic_spelunker_lantern", Weight = 40 },
				new() { ItemId = "relic_veterans_medal", Weight = 35 },
				// Epic relics
				new() { ItemId = "relic_blood_oath", Weight = 20 },
				new() { ItemId = "relic_chrono_shard", Weight = 20 },
				new() { ItemId = "relic_midas_hand", Weight = 20 },
				new() { ItemId = "relic_beast_totem", Weight = 20 },
				new() { ItemId = "relic_war_drum", Weight = 20 },
				new() { ItemId = "relic_life_anchor", Weight = 20 },
				// Legendary relics
				new() { ItemId = "relic_abyssal_crown", Weight = 5 },
				new() { ItemId = "relic_eternity_glass", Weight = 5 },
				new() { ItemId = "relic_soul_mirror", Weight = 5 },
				// Quest maps from rare bosses
				new() { ItemId = "map_nightmare", Weight = 5, MinExpeditionLevel = 40 },
				new() { ItemId = "map_element", Weight = 5, MinExpeditionLevel = 50 },
				new() { ItemId = "map_boss", Weight = 5, MinExpeditionLevel = 60 },
				new() { ItemId = "map_den", Weight = 2, MinExpeditionLevel = 80 },
			}
		};

		Log.Info( $"Loaded {_dropTables.Count} drop tables" );
	}

	// ============================================
	// ITEM ACCESS METHODS
	// ============================================

	/// <summary>
	/// Get an item definition by ID
	/// </summary>
	public ItemDefinition GetItem( string itemId )
	{
		return _itemDatabase.GetValueOrDefault( itemId );
	}

	/// <summary>
	/// Get all items of a specific category
	/// </summary>
	public IEnumerable<ItemDefinition> GetItemsByCategory( ItemCategory category )
	{
		return _itemDatabase.Values.Where( i => i.Category == category );
	}

	/// <summary>
	/// Get all possible drops for a specific expedition based on its element
	/// </summary>
	public List<ItemDefinition> GetDropsForExpedition( string expeditionId )
	{
		var drops = new List<ItemDefinition>();

		// Get the expedition to determine its element
		var expedition = ExpeditionManager.Instance?.GetExpedition( expeditionId );
		if ( expedition == null )
			return drops;

		// Get element-specific table key
		var elementKey = expedition.Element.ToString().ToLower();

		// Collect items from base table (consumables available everywhere)
		if ( _dropTables.TryGetValue( "base", out var baseTable ) )
		{
			foreach ( var entry in baseTable.Entries )
			{
				if ( _itemDatabase.TryGetValue( entry.ItemId, out var item ) )
				{
					if ( !drops.Any( d => d.Id == item.Id ) )
						drops.Add( item );
				}
			}
		}

		// Collect items from element-specific table
		if ( _dropTables.TryGetValue( elementKey, out var elementTable ) )
		{
			foreach ( var entry in elementTable.Entries )
			{
				if ( _itemDatabase.TryGetValue( entry.ItemId, out var item ) )
				{
					if ( !drops.Any( d => d.Id == item.Id ) )
						drops.Add( item );
				}
			}
		}

		// For neutral expeditions, also include items from multiple elements
		if ( elementKey == "neutral" )
		{
			// Neutral expeditions can drop general items from any table
			foreach ( var tableKey in new[] { "fire", "water", "earth", "wind", "electric", "ice" } )
			{
				if ( _dropTables.TryGetValue( tableKey, out var table ) )
				{
					foreach ( var entry in table.Entries.Take( 2 ) ) // Just first 2 items from each
					{
						if ( _itemDatabase.TryGetValue( entry.ItemId, out var item ) )
						{
							if ( !drops.Any( d => d.Id == item.Id ) )
								drops.Add( item );
						}
					}
				}
			}
		}

		return drops;
	}

	/// <summary>
	/// Get the quantity of an item in the player's inventory
	/// </summary>
	public int GetItemCount( string itemId )
	{
		return TamerManager.Instance?.CurrentTamer?.Inventory?.GetValueOrDefault( itemId, 0 ) ?? 0;
	}

	/// <summary>
	/// Check if player has at least one of an item
	/// </summary>
	public bool HasItem( string itemId )
	{
		return GetItemCount( itemId ) > 0;
	}

	/// <summary>
	/// Check if an item is new (hasn't been seen by the player)
	/// </summary>
	public bool IsNewItem( string itemId )
	{
		return _newItemIds.Contains( itemId );
	}

	/// <summary>
	/// Check if there are any new items
	/// </summary>
	public bool HasNewItems => _newItemIds.Count > 0;

	/// <summary>
	/// Mark all current items as seen (call when opening inventory)
	/// </summary>
	public void MarkAllItemsSeen()
	{
		_newItemIds.Clear();
	}

	/// <summary>
	/// Mark a specific item as seen
	/// </summary>
	public void MarkItemSeen( string itemId )
	{
		_newItemIds.Remove( itemId );
	}

	// ============================================
	// INVENTORY MANAGEMENT
	// ============================================

	/// <summary>
	/// Add items to the player's inventory
	/// </summary>
	/// <param name="itemId">The item ID to add</param>
	/// <param name="quantity">Number of items to add</param>
	/// <param name="markAsNew">Whether to mark the item as "new" in the UI (default true)</param>
	public bool AddItem( string itemId, int quantity = 1, bool markAsNew = true )
	{
		if ( quantity <= 0 ) return false;

		var item = GetItem( itemId );
		if ( item == null )
		{
			Log.Warning( $"Item not found: {itemId}" );
			return false;
		}

		var inventory = TamerManager.Instance?.CurrentTamer?.Inventory;
		if ( inventory == null ) return false;

		int current = inventory.GetValueOrDefault( itemId, 0 );
		int newAmount = Math.Min( current + quantity, item.MaxStack );
		inventory[itemId] = newAmount;

		int actualAdded = newAmount - current;
		if ( actualAdded > 0 )
		{
			// Mark item as new for UI notification (unless explicitly disabled)
			if ( markAsNew )
			{
				_newItemIds.Add( itemId );
			}

			OnItemAdded?.Invoke( item, actualAdded );
			Log.Info( $"Added {actualAdded}x {item.Name} to inventory" );
		}

		return actualAdded > 0;
	}

	/// <summary>
	/// Remove items from the player's inventory
	/// </summary>
	public bool RemoveItem( string itemId, int quantity = 1 )
	{
		if ( quantity <= 0 ) return false;

		var item = GetItem( itemId );
		if ( item == null ) return false;

		var inventory = TamerManager.Instance?.CurrentTamer?.Inventory;
		if ( inventory == null ) return false;

		int current = inventory.GetValueOrDefault( itemId, 0 );
		if ( current < quantity ) return false;

		int newAmount = current - quantity;
		if ( newAmount <= 0 )
			inventory.Remove( itemId );
		else
			inventory[itemId] = newAmount;

		OnItemRemoved?.Invoke( item, quantity );
		Log.Info( $"Removed {quantity}x {item.Name} from inventory" );

		return true;
	}

	/// <summary>
	/// Sell an item from inventory for gold
	/// </summary>
	public bool SellItem( string itemId, int quantity = 1 )
	{
		if ( quantity <= 0 ) return false;

		var item = GetItem( itemId );
		if ( item == null ) return false;

		// Can't sell items with no sell price
		if ( item.SellPrice <= 0 ) return false;

		// Can't sell equipped relics
		var equippedRelics = TamerManager.Instance?.CurrentTamer?.EquippedRelics;
		if ( equippedRelics?.Contains( itemId ) == true ) return false;

		// Check if we have enough
		if ( GetItemCount( itemId ) < quantity ) return false;

		// Remove item and add gold
		if ( RemoveItem( itemId, quantity ) )
		{
			int goldGained = item.SellPrice * quantity;
			TamerManager.Instance?.AddGold( goldGained );
			Log.Info( $"Sold {quantity}x {item.Name} for {goldGained} gold" );
			return true;
		}

		return false;
	}

	// ============================================
	// CONSUMABLE USAGE
	// ============================================

	/// <summary>
	/// Use a consumable item
	/// </summary>
	public bool UseItem( string itemId, Monster target = null )
	{
		var item = GetItem( itemId );
		if ( item == null || item.Category != ItemCategory.Consumable )
			return false;

		if ( !HasItem( itemId ) )
			return false;

		bool success = false;

		switch ( item.EffectType )
		{
			case ItemEffectType.XPGrant:
				if ( target != null )
				{
					target.AddXP( (int)item.EffectValue );
					success = true;
				}
				break;

			case ItemEffectType.BoostATK:
			case ItemEffectType.BoostDEF:
			case ItemEffectType.BoostSPD:
			case ItemEffectType.BoostSpA:
			case ItemEffectType.BoostSpD:
			case ItemEffectType.BoostCrit:
			case ItemEffectType.CatchRateBoost:
			case ItemEffectType.GoldBoost:
				// Add as active boost
				var boosts = TamerManager.Instance?.CurrentTamer?.ActiveBoosts;
				if ( boosts != null )
				{
					boosts.Add( new ActiveItemBoost
					{
						ItemId = itemId,
						EffectType = item.EffectType,
						EffectValue = item.EffectValue,
						RemainingUses = item.EffectDuration
					} );
					success = true;
				}
				break;
		}

		if ( success )
		{
			RemoveItem( itemId, 1 );
			OnItemUsed?.Invoke( item );
		}

		return success;
	}

	/// <summary>
	/// Get the current boost value for an effect type (from active consumables)
	/// </summary>
	public float GetActiveBoostValue( ItemEffectType effectType )
	{
		var boosts = TamerManager.Instance?.CurrentTamer?.ActiveBoosts;
		if ( boosts == null ) return 0;

		return boosts
			.Where( b => b.EffectType == effectType && !b.IsExpired )
			.Sum( b => b.EffectValue );
	}

	/// <summary>
	/// Decrement a boost's remaining uses (call after battle/catch attempt)
	/// </summary>
	public void DecrementBoostUse( ItemEffectType effectType )
	{
		var boosts = TamerManager.Instance?.CurrentTamer?.ActiveBoosts;
		if ( boosts == null ) return;

		foreach ( var boost in boosts.Where( b => b.EffectType == effectType && !b.IsExpired ) )
		{
			boost.RemainingUses--;
		}

		// Remove expired boosts
		boosts.RemoveAll( b => b.IsExpired );
	}

	// ============================================
	// RELIC MANAGEMENT
	// ============================================

	public const int MaxRelics = 3;

	/// <summary>
	/// Equip a relic (tamer passive)
	/// </summary>
	public bool EquipRelic( string itemId )
	{
		var item = GetItem( itemId );
		if ( item == null || item.Category != ItemCategory.Relic )
			return false;

		if ( !HasItem( itemId ) )
			return false;

		var relics = TamerManager.Instance?.CurrentTamer?.EquippedRelics;
		if ( relics == null ) return false;

		if ( relics.Count >= MaxRelics )
			return false;

		if ( relics.Contains( itemId ) )
			return false;

		relics.Add( itemId );
		RemoveItem( itemId, 1 );
		OnRelicEquipped?.Invoke( itemId );

		Log.Info( $"Equipped relic: {item.Name}" );
		return true;
	}

	/// <summary>
	/// Unequip a relic
	/// </summary>
	public bool UnequipRelic( string itemId )
	{
		var item = GetItem( itemId );
		if ( item == null ) return false;

		var relics = TamerManager.Instance?.CurrentTamer?.EquippedRelics;
		if ( relics == null || !relics.Contains( itemId ) )
			return false;

		relics.Remove( itemId );
		AddItem( itemId, 1 );
		OnRelicUnequipped?.Invoke( itemId );

		Log.Info( $"Unequipped relic: {item.Name}" );
		return true;
	}

	/// <summary>
	/// Get total bonus from equipped relics for an effect type
	/// </summary>
	public float GetRelicBonus( ItemEffectType effectType )
	{
		var relics = TamerManager.Instance?.CurrentTamer?.EquippedRelics;
		if ( relics == null ) return 0;

		float total = 0;
		foreach ( var relicId in relics )
		{
			var item = GetItem( relicId );
			if ( item == null ) continue;

			if ( item.EffectType == effectType )
				total += item.EffectValue;

			if ( item.SecondaryEffectType == effectType )
				total += item.SecondaryEffectValue;
		}

		return total;
	}

	// ============================================
	// HELD ITEM MANAGEMENT
	// ============================================

	/// <summary>
	/// Equip a held item to a monster
	/// </summary>
	public bool EquipHeldItem( Monster monster, string itemId )
	{
		if ( monster == null ) return false;

		var item = GetItem( itemId );
		if ( item == null || item.Category != ItemCategory.HeldItem )
			return false;

		if ( !HasItem( itemId ) )
			return false;

		// Unequip current item first
		if ( !string.IsNullOrEmpty( monster.HeldItemId ) )
		{
			UnequipHeldItem( monster );
		}

		monster.HeldItemId = itemId;
		RemoveItem( itemId, 1 );

		// Persist the monster's held item change
		MonsterManager.Instance?.SaveMonsters();

		Log.Info( $"Equipped {item.Name} to {monster.Nickname ?? monster.SpeciesId}" );
		return true;
	}

	/// <summary>
	/// Unequip a held item from a monster
	/// </summary>
	public bool UnequipHeldItem( Monster monster )
	{
		if ( monster == null || string.IsNullOrEmpty( monster.HeldItemId ) )
			return false;

		var item = GetItem( monster.HeldItemId );
		AddItem( monster.HeldItemId, 1, markAsNew: false ); // Don't mark as new when unequipping
		monster.HeldItemId = null;

		// Persist the monster's held item change
		MonsterManager.Instance?.SaveMonsters();

		Log.Info( $"Unequipped {item?.Name ?? "item"} from {monster.Nickname ?? monster.SpeciesId}" );
		return true;
	}

	/// <summary>
	/// Get a stat bonus from a monster's held item
	/// Returns percentage bonus (e.g., 10 for +10%)
	/// </summary>
	public float GetHeldItemBonus( Monster monster, ItemEffectType effectType )
	{
		if ( monster == null || string.IsNullOrEmpty( monster.HeldItemId ) )
			return 0;

		var item = GetItem( monster.HeldItemId );
		if ( item == null ) return 0;

		float bonus = 0;

		if ( item.EffectType == effectType )
			bonus += item.EffectValue;

		if ( item.SecondaryEffectType == effectType )
			bonus += item.SecondaryEffectValue;

		return bonus;
	}

	/// <summary>
	/// Check if monster's held item has a specific effect
	/// </summary>
	public bool HasHeldItemEffect( Monster monster, ItemEffectType effectType )
	{
		if ( monster == null || string.IsNullOrEmpty( monster.HeldItemId ) )
			return false;

		var item = GetItem( monster.HeldItemId );
		if ( item == null ) return false;

		return item.EffectType == effectType || item.SecondaryEffectType == effectType;
	}

	// ============================================
	// SERVER BOOST USAGE
	// ============================================

	/// <summary>
	/// Use a server boost from inventory
	/// Returns: (success, errorMessage)
	/// </summary>
	public (bool Success, string Message) UseBoost( string itemId )
	{
		var item = GetItem( itemId );
		if ( item == null )
			return (false, "Item not found");

		if ( item.Category != ItemCategory.Boost )
			return (false, "This item is not a boost");

		if ( !HasItem( itemId ) )
			return (false, "You don't have this boost");

		// Map ItemEffectType to ShopItemType
		var shopType = item.EffectType switch
		{
			ItemEffectType.ServerTamerXPBoost => ShopItemType.TamerXPBoost,
			ItemEffectType.ServerBeastXPBoost => ShopItemType.BeastXPBoost,
			ItemEffectType.ServerGoldBoost => ShopItemType.GoldBoost,
			ItemEffectType.ServerLuckyCharm => ShopItemType.LuckyCharm,
			ItemEffectType.ServerRareEncounter => ShopItemType.RareEncounter,
			_ => (ShopItemType?)null
		};

		if ( shopType == null )
			return (false, "Unknown boost type");

		// Check if boost can be activated (max 8 hours check is in CanActivateServerBoost)
		var shopManager = ShopManager.Instance;
		if ( shopManager == null )
			return (false, "Shop not available");

		// Check remaining time on existing boost
		var remainingTime = shopManager.GetServerBoostTimeRemaining( shopType.Value );
		if ( remainingTime.TotalMinutes >= 480 ) // 8 hours max
		{
			return (false, "This boost is already at maximum duration (8 hours)!");
		}

		// Try to activate the boost
		bool activated = shopManager.ActivateServerBoost( shopType.Value, item.EffectValue, item.BoostDurationMinutes );
		if ( !activated )
		{
			return (false, "Failed to activate boost");
		}

		// Remove the item from inventory
		RemoveItem( itemId, 1 );

		// Save inventory
		TamerManager.Instance?.SaveToCloud();

		Log.Info( $"Used server boost: {item.Name}" );
		return (true, $"Activated {item.Name}!");
	}

	/// <summary>
	/// Get all boost items in inventory
	/// </summary>
	public List<ItemDefinition> GetAvailableBoosts()
	{
		var boosts = new List<ItemDefinition>();
		var inventory = TamerManager.Instance?.CurrentTamer?.Inventory;
		if ( inventory == null ) return boosts;

		foreach ( var kvp in inventory )
		{
			if ( kvp.Value <= 0 ) continue;
			var item = GetItem( kvp.Key );
			if ( item != null && item.Category == ItemCategory.Boost )
			{
				boosts.Add( item );
			}
		}

		return boosts.OrderBy( i => i.EffectType ).ThenBy( i => i.BoostDurationMinutes ).ToList();
	}

	// ============================================
	// DROP CALCULATION
	// ============================================

	/// <summary>
	/// Calculate item drops from defeating a monster
	/// </summary>
	public List<(string ItemId, int Quantity)> CalculateDrop( Monster defeated, string expeditionId, ElementType expeditionElement, int expeditionLevel, bool isBoss = false, bool isRareBoss = false )
	{
		var drops = new List<(string, int)>();

		// Determine which drop table to use
		string tableKey;
		if ( isRareBoss )
			tableKey = "boss_rare";
		else if ( isBoss )
			tableKey = "boss";
		else
			tableKey = expeditionElement.ToString().ToLower();

		// Get element-specific or base table
		if ( !_dropTables.TryGetValue( tableKey, out var table ) )
		{
			table = _dropTables.GetValueOrDefault( "base" );
		}

		if ( table == null )
		{
			Log.Warning( $"[ItemManager] No drop table found for {tableKey}" );
			return drops;
		}

		// Apply skill bonuses
		float itemFindBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ItemFindBonus ) ?? 0;
		float rareItemBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.RareItemChance ) ?? 0;
		float doubleDropChance = TamerManager.Instance?.GetSkillBonus( SkillEffectType.DoubleDropChance ) ?? 0;

		// Apply relic bonuses
		itemFindBonus += GetRelicBonus( ItemEffectType.PassiveItemFind );

		// Calculate final drop chance
		float dropChance = table.BaseDropChance * (1 + itemFindBonus / 100f);

		// Roll for drop
		double roll = Random.Shared.NextDouble();
		bool dropRollSucceeded = roll <= dropChance;

		if ( !dropRollSucceeded )
		{
			// Element table roll failed - try base table with its own roll
			if ( tableKey != "base" && !isBoss && !isRareBoss )
			{
				var baseTable = _dropTables.GetValueOrDefault( "base" );
				if ( baseTable != null )
				{
					float baseDropChance = baseTable.BaseDropChance * (1 + itemFindBonus / 100f);
					double baseRoll = Random.Shared.NextDouble();
					if ( baseRoll <= baseDropChance )
					{
						drops.AddRange( RollDropTable( "base", expeditionLevel, rareItemBonus, doubleDropChance ) );
					}
				}
			}

			if ( drops.Count > 0 )
			{
				Log.Info( $"[ItemManager] Drop from base table: {string.Join( ", ", drops.Select( d => $"{d.Item1} x{d.Item2}" ) )}" );
			}
			return drops;
		}

		// Roll on the element/boss table
		drops.AddRange( RollDropTable( tableKey, expeditionLevel, rareItemBonus, doubleDropChance ) );

		// Also roll on base table for consumables (unless boss)
		if ( !isBoss && !isRareBoss && tableKey != "base" )
		{
			var baseDropChance = _dropTables["base"].BaseDropChance * (1 + itemFindBonus / 100f);
			if ( Random.Shared.NextDouble() <= baseDropChance )
			{
				drops.AddRange( RollDropTable( "base", expeditionLevel, rareItemBonus, doubleDropChance ) );
			}
		}

		if ( drops.Count > 0 )
		{
			Log.Info( $"[ItemManager] Drops from {tableKey}: {string.Join( ", ", drops.Select( d => $"{d.Item1} x{d.Item2}" ) )}" );
		}

		return drops;
	}

	private List<(string, int)> RollDropTable( string tableKey, int expeditionLevel, float rareItemBonus, float doubleDropChance )
	{
		var drops = new List<(string, int)>();

		if ( !_dropTables.TryGetValue( tableKey, out var table ) )
			return drops;

		// Filter entries by expedition level
		var validEntries = table.Entries
			.Where( e => expeditionLevel >= e.MinExpeditionLevel )
			.ToList();

		if ( validEntries.Count == 0 ) return drops;

		// Calculate total weight with rare item bonus
		int totalWeight = 0;
		foreach ( var entry in validEntries )
		{
			var item = GetItem( entry.ItemId );
			int weight = entry.Weight;

			// Apply rare item bonus to rare+ items
			if ( item != null && item.Rarity >= ItemRarity.Rare )
			{
				weight = (int)(weight * (1 + rareItemBonus / 100f));
			}

			totalWeight += weight;
		}

		// Weighted random selection
		int roll = Random.Shared.Next( totalWeight );
		int cumulative = 0;

		foreach ( var entry in validEntries )
		{
			var item = GetItem( entry.ItemId );
			int weight = entry.Weight;

			if ( item != null && item.Rarity >= ItemRarity.Rare )
			{
				weight = (int)(weight * (1 + rareItemBonus / 100f));
			}

			cumulative += weight;
			if ( roll < cumulative )
			{
				int quantity = Random.Shared.Next( entry.MinQuantity, entry.MaxQuantity + 1 );

				// Double drop chance
				if ( Random.Shared.NextDouble() < (doubleDropChance / 100f) )
				{
					quantity *= 2;
				}

				drops.Add( (entry.ItemId, quantity) );
				break;
			}
		}

		return drops;
	}
}
