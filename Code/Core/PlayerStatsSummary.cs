using System;
using System.Collections.Generic;
using System.Linq;
using Beastborne.Data;

namespace Beastborne.Core;

// Player-wide stat breakdown for the Skill Tree → Stats subtab.
//
// Read-only enumeration of every source that boosts a player-wide stat.
// Pulls from the four LIVE bonus channels (skill tree, equipped relics, shop
// boosts, guild perks) plus event flags (DoubleXP) and timed buffs (Elite
// Ink). UI-only — does NOT replace any existing call-site bonus math; each
// gameplay system still computes its own bonus locally.
//
// When a new bonus channel goes live (e.g. achievement % rewards, daily
// streak multipliers, new guild perks at higher levels), add another
// source enumerator below — the UI re-reads automatically.

public enum StatGroup
{
	Economy,
	Progression,
	Expedition,
	Combat,
	Boss,
	Fusion
}

public enum StatOp { Add, Multiply }

public record StatSource(
	string Label,
	string Tag,            // SKILL / RELIC / BOOST / SERVER / GUILD / EVENT / BUFF
	string TagColor,       // hex used for the tag chip + operator badge
	string Icon,           // iconify icon name
	float Value,           // additive: percent points (15 = +15%); multiplicative: factor (2.0 = ×2)
	StatOp Op,
	string SourceId,       // stable id for diffing / future click-to-locate
	TimeSpan? TimeRemaining
);

public class StatRow
{
	public string Id;
	public string Name;
	public string Description;
	public string Icon;
	public bool IsFlat;        // true: gene bonus etc — value is points, not %
	public StatGroup Group;
	public List<StatSource> Sources = new();

	public bool HasContributions => Sources.Count > 0;

	public float TotalAdditive => Sources
		.Where( s => s.Op == StatOp.Add )
		.Sum( s => s.Value );

	public float TotalMultiplicative => Sources
		.Where( s => s.Op == StatOp.Multiply )
		.Aggregate( 1f, ( acc, s ) => acc * s.Value );

	// Single combined display number. For non-flat stats this is the net
	// percent bonus after additive + multiplicative compose:
	//     net% = ( (1 + add/100) * mult - 1 ) * 100
	public float NetPercentBonus
	{
		get
		{
			if ( IsFlat ) return TotalAdditive;
			return ((1f + TotalAdditive / 100f) * TotalMultiplicative - 1f) * 100f;
		}
	}
}

public static class PlayerStatsSummary
{
	// ─── Public API ──────────────────────────────────────────────────────

	public static List<StatRow> GetAll()
	{
		var rows = new List<StatRow>();

		// ECONOMY
		var goldRow = NewRow( "gold_gain", "Gold Gain",
			"Bonus gold from waves, expeditions, and pickups.",
			"lucide:coins", StatGroup.Economy );
		goldRow.Sources.AddRange( SkillSourcesFor( SkillEffectType.GoldDropBonus ) );
		goldRow.Sources.AddRange( SkillSourcesFor( SkillEffectType.ExpeditionGoldBonus ) );
		goldRow.Sources.AddRange( SkillSourcesFor( SkillEffectType.GoldFromAllSources ) );
		goldRow.Sources.AddRange( RelicSourcesFor( ItemEffectType.PassiveGoldFind ) );
		goldRow.Sources.AddRange( BoostSourcesFor( ShopItemType.GoldBoost ) );
		AddIfNotNull( goldRow, GuildSource_GoldExpedition() );
		rows.Add( goldRow );

		var shopRow = NewRow( "shop_discount", "Shop Discount",
			"Lower prices in the shop.",
			"lucide:tag", StatGroup.Economy );
		shopRow.Sources.AddRange( SkillSourcesFor( SkillEffectType.ShopDiscount ) );
		rows.Add( shopRow );

		var itemRow = NewRow( "item_find", "Item Find",
			"Better drop rates from beasts and chests.",
			"lucide:package", StatGroup.Economy );
		itemRow.Sources.AddRange( SkillSourcesFor( SkillEffectType.ItemFindBonus ) );
		itemRow.Sources.AddRange( RelicSourcesFor( ItemEffectType.PassiveItemFind ) );
		itemRow.Sources.AddRange( BoostSourcesFor( ShopItemType.LuckyCharm ) );
		rows.Add( itemRow );

		rows.Add( SimpleSkillRow( "rare_item", "Rare Item Chance",
			"Higher odds of rare drops in expeditions.",
			"lucide:diamond", StatGroup.Economy,
			SkillEffectType.RareItemChance ) );

		rows.Add( SimpleSkillRow( "double_drop", "Double Drop Chance",
			"Chance for any drop to be doubled.",
			"lucide:copy", StatGroup.Economy,
			SkillEffectType.DoubleDropChance ) );

		// PROGRESSION
		var tamerRow = NewRow( "tamer_xp", "Tamer XP",
			"Bonus XP toward your Tamer level.",
			"lucide:trending-up", StatGroup.Progression );
		tamerRow.Sources.AddRange( RelicSourcesFor( ItemEffectType.PassiveTamerXP ) );
		tamerRow.Sources.AddRange( BoostSourcesFor( ShopItemType.TamerXPBoost ) );
		tamerRow.Sources.AddRange( BoostSourcesFor( ShopItemType.XPBoost ) ); // legacy alias
		AddIfNotNull( tamerRow, EventDoubleXPSource() );
		AddIfNotNull( tamerRow, GuildSource_TamerXP() );
		rows.Add( tamerRow );

		var beastRow = NewRow( "beast_xp", "Beast XP",
			"Bonus XP for your beasts after battles.",
			"lucide:flame", StatGroup.Progression );
		beastRow.Sources.AddRange( RelicSourcesFor( ItemEffectType.PassiveXPGain ) );
		beastRow.Sources.AddRange( SkillSourcesFor( SkillEffectType.ExpeditionXPBonus ) );
		beastRow.Sources.AddRange( BoostSourcesFor( ShopItemType.BeastXPBoost ) );
		AddIfNotNull( beastRow, GuildSource_BeastXP() );
		rows.Add( beastRow );

		// EXPEDITION
		var contractRow = NewRow( "contract_rate", "Contract Rate",
			"Better odds when contracting wild beasts.",
			"lucide:scroll-text", StatGroup.Expedition );
		contractRow.Sources.AddRange( RelicSourcesFor( ItemEffectType.PassiveCatchRate ) );
		AddIfNotNull( contractRow, EliteInkSource() );
		AddIfNotNull( contractRow, GuildSource_CatchRate() );
		rows.Add( contractRow );

		rows.Add( SimpleSkillRow( "encounter_rate", "Encounter Rate",
			"More wild beasts spawn during expeditions.",
			"lucide:eye", StatGroup.Expedition,
			SkillEffectType.EncounterRateBonus ) );

		var rareEnc = NewRow( "rare_encounter", "Rare Encounter",
			"Higher chance of meeting rare beasts.",
			"lucide:sparkles", StatGroup.Expedition );
		rareEnc.Sources.AddRange( BoostSourcesFor( ShopItemType.RareEncounter ) );
		rows.Add( rareEnc );

		// COMBAT
		rows.Add( SimpleSkillRow( "all_atk", "All Beast ATK",
			"Bonus attack to every monster you own.",
			"lucide:swords", StatGroup.Combat,
			SkillEffectType.AllMonsterATKPercent ) );

		rows.Add( SimpleSkillRow( "all_hp", "All Beast HP",
			"Bonus HP to every monster you own.",
			"lucide:heart", StatGroup.Combat,
			SkillEffectType.AllMonsterHPPercent ) );

		var critRow = NewRow( "crit_chance", "Crit Chance",
			"Higher critical-hit chance in battle.",
			"lucide:target", StatGroup.Combat );
		critRow.Sources.AddRange( SkillSourcesFor( SkillEffectType.CritChanceBonus ) );
		critRow.Sources.AddRange( RelicSourcesFor( ItemEffectType.PassiveCritRate ) );
		rows.Add( critRow );

		rows.Add( SimpleRelicRow( "healing", "Healing Boost",
			"Restore more HP from healing items.",
			"lucide:heart-pulse", StatGroup.Combat,
			ItemEffectType.PassiveHealingBoost ) );

		// BOSS
		rows.Add( SimpleSkillRow( "boss_dmg", "Boss Damage",
			"Bonus damage dealt to boss-tier enemies.",
			"lucide:skull", StatGroup.Boss,
			SkillEffectType.BossDamageBonus ) );

		rows.Add( SimpleSkillRow( "boss_dr", "Boss Damage Reduction",
			"Take less damage from bosses.",
			"lucide:shield-check", StatGroup.Boss,
			SkillEffectType.BossDamageReduction ) );

		rows.Add( SimpleSkillRow( "boss_tokens", "Boss Tokens",
			"Earn more boss tokens from victories.",
			"lucide:coins", StatGroup.Boss,
			SkillEffectType.BossTokenBonus ) );

		// FUSION
		var geneRow = NewRow( "gene_bonus", "Gene Bonus",
			"Flat bonus gene points awarded on fusion.",
			"lucide:dna", StatGroup.Fusion );
		geneRow.IsFlat = true;
		geneRow.Sources.AddRange( SkillSourcesFor( SkillEffectType.GeneBonusFlat ) );
		rows.Add( geneRow );

		rows.Add( SimpleSkillRow( "inheritance", "Gene Inheritance",
			"Better odds of inheriting top-tier genes.",
			"lucide:git-branch", StatGroup.Fusion,
			SkillEffectType.GeneticInheritanceBonus ) );

		rows.Add( SimpleSkillRow( "mutation", "Mutation Chance",
			"Chance of beneficial mutations in fusion.",
			"lucide:flask-conical", StatGroup.Fusion,
			SkillEffectType.MutationChance ) );

		rows.Add( SimpleRelicRow( "ink_save", "Ink Save Chance",
			"Chance to refund Contract Ink on use.",
			"lucide:droplet", StatGroup.Fusion,
			ItemEffectType.PassiveInkSaver ) );

		return rows;
	}

	public static int CountActiveRows() => GetAll().Count( r => r.HasContributions );

	// Worked example: net gold from a sample 1000g expedition with current
	// bonuses applied. Single number that grounds every row above it in
	// concrete value.
	public static int GetSampleGoldExample( int baseGold = 1000 )
	{
		var gold = GetAll().FirstOrDefault( r => r.Id == "gold_gain" );
		if ( gold == null ) return baseGold;
		return (int)System.Math.Round( baseGold * (1f + gold.NetPercentBonus / 100f) );
	}

	// ─── Row builders ────────────────────────────────────────────────────

	private static StatRow NewRow( string id, string name, string desc, string icon, StatGroup group )
		=> new() { Id = id, Name = name, Description = desc, Icon = icon, Group = group };

	private static StatRow SimpleSkillRow( string id, string name, string desc, string icon, StatGroup group, SkillEffectType type )
	{
		var row = NewRow( id, name, desc, icon, group );
		row.Sources.AddRange( SkillSourcesFor( type ) );
		return row;
	}

	private static StatRow SimpleRelicRow( string id, string name, string desc, string icon, StatGroup group, ItemEffectType type )
	{
		var row = NewRow( id, name, desc, icon, group );
		row.Sources.AddRange( RelicSourcesFor( type ) );
		return row;
	}

	private static void AddIfNotNull( StatRow row, StatSource source )
	{
		if ( source != null ) row.Sources.Add( source );
	}

	// ─── Source enumerators ──────────────────────────────────────────────

	private static List<StatSource> SkillSourcesFor( SkillEffectType type )
	{
		var result = new List<StatSource>();
		var tamer = TamerManager.Instance;
		if ( tamer?.SkillTree == null || tamer.CurrentTamer?.SkillRanks == null ) return result;

		foreach ( var kvp in tamer.CurrentTamer.SkillRanks )
		{
			int rank = kvp.Value;
			if ( rank <= 0 ) continue;
			var node = tamer.SkillTree.GetNode( kvp.Key );
			if ( node?.Effects == null ) continue;

			foreach ( var effect in node.Effects )
			{
				if ( effect.Type != type ) continue;
				// Element-gated effects (e.g. element-specific boosts) are not
				// player-wide for this view — skip them.
				if ( effect.AffectedElement.HasValue ) continue;

				result.Add( new StatSource(
					Label: $"{node.Name} R{rank}",
					Tag: "SKILL",
					TagColor: GetBranchColor( node.Branch ),
					Icon: GetBranchIcon( node.Branch ),
					Value: effect.Value * rank,
					Op: StatOp.Add,
					SourceId: $"skill:{node.Id}",
					TimeRemaining: null
				) );
			}
		}
		return result;
	}

	private static List<StatSource> RelicSourcesFor( ItemEffectType type )
	{
		var result = new List<StatSource>();
		var relics = TamerManager.Instance?.CurrentTamer?.EquippedRelics;
		if ( relics == null ) return result;

		foreach ( var relicId in relics )
		{
			var item = ItemManager.Instance?.GetItem( relicId );
			if ( item == null ) continue;
			float value = 0;
			if ( item.EffectType == type ) value += item.EffectValue;
			if ( item.SecondaryEffectType == type ) value += item.SecondaryEffectValue;
			if ( value == 0 ) continue;

			result.Add( new StatSource(
				Label: item.Name,
				Tag: "RELIC",
				TagColor: GetRarityColor( item.Rarity ),
				Icon: "lucide:gem",
				Value: value,
				Op: StatOp.Add,
				SourceId: $"relic:{relicId}",
				TimeRemaining: null
			) );
		}
		return result;
	}

	private static List<StatSource> BoostSourcesFor( ShopItemType type )
	{
		var result = new List<StatSource>();
		var boosts = ShopManager.Instance?.GetAllActiveBoosts();
		if ( boosts == null ) return result;

		foreach ( var b in boosts )
		{
			if ( b.Type != type ) continue;
			result.Add( new StatSource(
				Label: GetBoostLabel( b.Type ) + (b.IsServer ? " (Server)" : ""),
				Tag: b.IsServer ? "SERVER" : "BOOST",
				TagColor: b.IsServer ? "#10b981" : "#a855f7",
				Icon: b.IsServer ? "lucide:globe" : "lucide:zap",
				Value: b.Multiplier,
				Op: StatOp.Multiply,
				SourceId: $"boost:{b.Type}{(b.IsServer ? ":server" : "")}",
				TimeRemaining: b.TimeRemaining
			) );
		}
		return result;
	}

	private static StatSource EliteInkSource()
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return null;
		if ( tamer.EliteInkExpiresAt <= DateTime.Now ) return null;
		return new StatSource(
			Label: "Elite Contract Ink",
			Tag: "BUFF",
			TagColor: "#22d3ee",
			Icon: "lucide:droplet",
			Value: 15f,
			Op: StatOp.Add,
			SourceId: "buff:elite_ink",
			TimeRemaining: tamer.EliteInkExpiresAt - DateTime.Now
		);
	}

	private static StatSource EventDoubleXPSource()
	{
		if ( !TamerManager.IsDoubleXPActive ) return null;
		var remaining = TamerManager.EventXPEnd - DateTime.UtcNow;
		return new StatSource(
			Label: "Double XP Event",
			Tag: "EVENT",
			TagColor: "#22d3ee",
			Icon: "lucide:calendar",
			Value: 2.0f,
			Op: StatOp.Multiply,
			SourceId: "event:double_xp",
			TimeRemaining: remaining > TimeSpan.Zero ? remaining : null
		);
	}

	// Guild perk sources — only present when in a guild whose level meets
	// the perk threshold. Values are stored in code as multipliers (1.05f
	// for +5%) but conceptually they're additive % bonuses, so we surface
	// them as additive in the breakdown.
	private static StatSource GuildSource_GoldExpedition()
	{
		var gm = GuildManager.Instance;
		if ( gm == null || !gm.IsInGuild || gm.Guild == null ) return null;
		var mul = gm.GetGoldMultiplier( isExpedition: true );
		if ( mul <= 1.0f ) return null;
		return new StatSource(
			Label: $"Guild Lv{gm.Guild.Level} · Coffer Cut",
			Tag: "GUILD",
			TagColor: "#84cc16",
			Icon: "lucide:flag",
			Value: (mul - 1f) * 100f,
			Op: StatOp.Add,
			SourceId: "guild:gold_exp",
			TimeRemaining: null
		);
	}

	private static StatSource GuildSource_TamerXP()
	{
		var gm = GuildManager.Instance;
		if ( gm == null || !gm.IsInGuild || gm.Guild == null ) return null;
		var mul = gm.GetTamerXPMultiplier();
		if ( mul <= 1.0f ) return null;
		return new StatSource(
			Label: $"Guild Lv{gm.Guild.Level} · Banner Boon",
			Tag: "GUILD",
			TagColor: "#84cc16",
			Icon: "lucide:flag",
			Value: (mul - 1f) * 100f,
			Op: StatOp.Add,
			SourceId: "guild:tamer_xp",
			TimeRemaining: null
		);
	}

	private static StatSource GuildSource_BeastXP()
	{
		var gm = GuildManager.Instance;
		if ( gm == null || !gm.IsInGuild || gm.Guild == null ) return null;
		var mul = gm.GetBeastXPMultiplier();
		if ( mul <= 1.0f ) return null;
		return new StatSource(
			Label: $"Guild Lv{gm.Guild.Level} · Beast Trainer",
			Tag: "GUILD",
			TagColor: "#84cc16",
			Icon: "lucide:flag",
			Value: (mul - 1f) * 100f,
			Op: StatOp.Add,
			SourceId: "guild:beast_xp",
			TimeRemaining: null
		);
	}

	private static StatSource GuildSource_CatchRate()
	{
		var gm = GuildManager.Instance;
		if ( gm == null || !gm.IsInGuild || gm.Guild == null ) return null;
		var bonus = gm.GetCatchRateBonus();
		if ( bonus <= 0 ) return null;
		return new StatSource(
			Label: $"Guild Lv{gm.Guild.Level} · Sharper Eye",
			Tag: "GUILD",
			TagColor: "#84cc16",
			Icon: "lucide:flag",
			Value: bonus,
			Op: StatOp.Add,
			SourceId: "guild:catch_rate",
			TimeRemaining: null
		);
	}

	// ─── Display helpers ─────────────────────────────────────────────────

	private static string GetBranchColor( SkillBranch b ) => b switch
	{
		SkillBranch.Power => "#dc2626",
		SkillBranch.Fortune => "#f59e0b",
		SkillBranch.Fusion => "#ec4899",
		SkillBranch.Expedition => "#0ea5e9",
		SkillBranch.Mastery => "#8b5cf6",
		_ => "#c4b5fd"
	};

	private static string GetBranchIcon( SkillBranch b ) => b switch
	{
		SkillBranch.Power => "lucide:swords",
		SkillBranch.Fortune => "lucide:coins",
		SkillBranch.Fusion => "lucide:flask-conical",
		SkillBranch.Expedition => "lucide:compass",
		SkillBranch.Mastery => "lucide:skull",
		_ => "lucide:sparkles"
	};

	private static string GetRarityColor( ItemRarity r ) => r switch
	{
		ItemRarity.Common => "#9ca3af",
		ItemRarity.Uncommon => "#22c55e",
		ItemRarity.Rare => "#3b82f6",
		ItemRarity.Epic => "#a855f7",
		ItemRarity.Legendary => "#f59e0b",
		_ => "#9ca3af"
	};

	private static string GetBoostLabel( ShopItemType type ) => type switch
	{
		ShopItemType.GoldBoost => "Gold Boost",
		ShopItemType.TamerXPBoost => "Tamer XP Boost",
		ShopItemType.BeastXPBoost => "Beast XP Boost",
		ShopItemType.XPBoost => "XP Boost",
		ShopItemType.LuckyCharm => "Lucky Charm",
		ShopItemType.RareEncounter => "Rare Radar",
		_ => type.ToString()
	};

	public static string GetGroupName( StatGroup g ) => g switch
	{
		StatGroup.Economy => "ECONOMY",
		StatGroup.Progression => "PROGRESSION",
		StatGroup.Expedition => "EXPEDITION",
		StatGroup.Combat => "COMBAT",
		StatGroup.Boss => "BOSS",
		StatGroup.Fusion => "FUSION",
		_ => g.ToString().ToUpper()
	};

	public static string GetGroupIcon( StatGroup g ) => g switch
	{
		StatGroup.Economy => "lucide:coins",
		StatGroup.Progression => "lucide:trending-up",
		StatGroup.Expedition => "lucide:compass",
		StatGroup.Combat => "lucide:swords",
		StatGroup.Boss => "lucide:skull",
		StatGroup.Fusion => "lucide:dna",
		_ => "lucide:circle"
	};

	public static string GetGroupColor( StatGroup g ) => g switch
	{
		StatGroup.Economy => "#f59e0b",
		StatGroup.Progression => "#22d3ee",
		StatGroup.Expedition => "#0ea5e9",
		StatGroup.Combat => "#dc2626",
		StatGroup.Boss => "#8b5cf6",
		StatGroup.Fusion => "#ec4899",
		_ => "#c4b5fd"
	};
}
