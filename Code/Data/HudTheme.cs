using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// HUD theme that changes the appearance of GameHUD elements
/// </summary>
public class HudTheme
{
	/// <summary>
	/// Unique identifier for this theme
	/// </summary>
	public string Id { get; set; }

	/// <summary>
	/// Display name for the theme
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Description shown in shop
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// CSS class applied to themed elements
	/// </summary>
	public string CssClass { get; set; }

	/// <summary>
	/// Boss Token cost
	/// </summary>
	public int TokenCost { get; set; }

	/// <summary>
	/// Required boss clear to unlock (null = always purchasable)
	/// </summary>
	public string RequiredBossId { get; set; }

	/// <summary>
	/// Preview colors for shop display
	/// </summary>
	public string PrimaryColor { get; set; }
	public string SecondaryColor { get; set; }
	public string AccentColor { get; set; }
}

/// <summary>
/// Title cosmetic that displays near the player name
/// </summary>
public class TamerTitle
{
	/// <summary>
	/// Unique identifier
	/// </summary>
	public string Id { get; set; }

	/// <summary>
	/// The title text displayed
	/// </summary>
	public string Title { get; set; }

	/// <summary>
	/// Description of how to earn this title
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// Boss Token cost
	/// </summary>
	public int TokenCost { get; set; }

	/// <summary>
	/// Required boss clear to unlock (null = always purchasable)
	/// </summary>
	public string RequiredBossId { get; set; }

	/// <summary>
	/// Color for the title text
	/// </summary>
	public string TitleColor { get; set; } = "#ffffff";
}

/// <summary>
/// Manager for all available themes and titles
/// </summary>
public static class CosmeticDatabase
{
	/// <summary>
	/// All available HUD themes
	/// </summary>
	public static List<HudTheme> Themes { get; } = new()
	{
		new HudTheme
		{
			Id = "default",
			Name = "Default",
			Description = "The classic Beastborne look",
			CssClass = "",
			TokenCost = 0,
			PrimaryColor = "#1a1a2e",
			SecondaryColor = "#0f0f23",
			AccentColor = "#fbbf24"
		},
		new HudTheme
		{
			Id = "fire",
			Name = "Inferno",
			Description = "Burning embers and molten borders",
			CssClass = "theme-fire",
			TokenCost = 250,
			PrimaryColor = "#2d1810",
			SecondaryColor = "#1a0f0a",
			AccentColor = "#ff6b35"
		},
		new HudTheme
		{
			Id = "water",
			Name = "Abyssal",
			Description = "Deep ocean blues and flowing waves",
			CssClass = "theme-water",
			TokenCost = 250,
			PrimaryColor = "#0a1628",
			SecondaryColor = "#051020",
			AccentColor = "#06b6d4"
		},
		new HudTheme
		{
			Id = "nature",
			Name = "Verdant",
			Description = "Lush greens and natural growth",
			CssClass = "theme-nature",
			TokenCost = 250,
			PrimaryColor = "#0f1f14",
			SecondaryColor = "#0a150d",
			AccentColor = "#22c55e"
		},
		new HudTheme
		{
			Id = "shadow",
			Name = "Void",
			Description = "Darkness that consumes light",
			CssClass = "theme-shadow",
			TokenCost = 500,
			RequiredBossId = "shadow_depths",
			PrimaryColor = "#0d0a14",
			SecondaryColor = "#08060e",
			AccentColor = "#a855f7"
		},
		new HudTheme
		{
			Id = "spirit",
			Name = "Celestial",
			Description = "Radiant light and golden halos",
			CssClass = "theme-spirit",
			TokenCost = 500,
			RequiredBossId = "dawn_sanctuary",
			PrimaryColor = "#1a1520",
			SecondaryColor = "#120f18",
			AccentColor = "#f472b6"
		},
		new HudTheme
		{
			Id = "mythic",
			Name = "Mythweaver",
			Description = "The stuff of legends",
			CssClass = "theme-mythic",
			TokenCost = 1500,
			RequiredBossId = "mythweavers_realm",
			PrimaryColor = "#1a1025",
			SecondaryColor = "#0f0a18",
			AccentColor = "#ec4899"
		},
		new HudTheme
		{
			Id = "genesis",
			Name = "Origin",
			Description = "From before existence itself",
			CssClass = "theme-genesis",
			TokenCost = 3000,
			RequiredBossId = "origin_void",
			PrimaryColor = "#000510",
			SecondaryColor = "#000308",
			AccentColor = "#ffffff"
		}
	};

	/// <summary>
	/// All available titles
	/// </summary>
	public static List<TamerTitle> Titles { get; } = new()
	{
		new TamerTitle
		{
			Id = "boss_slayer",
			Title = "Boss Slayer",
			Description = "Defeat your first boss",
			TokenCost = 75,
			TitleColor = "#fbbf24"
		},
		new TamerTitle
		{
			Id = "dragon_tamer",
			Title = "Dragon Tamer",
			Description = "Defeat a dragon-type boss",
			TokenCost = 150,
			RequiredBossId = "elemental_nexus",
			TitleColor = "#f97316"
		},
		new TamerTitle
		{
			Id = "void_walker",
			Title = "Void Walker",
			Description = "Conquer the Shadow Depths",
			TokenCost = 200,
			RequiredBossId = "shadow_depths",
			TitleColor = "#a855f7"
		},
		new TamerTitle
		{
			Id = "legend_hunter",
			Title = "Legend Hunter",
			Description = "Defeat a Legendary boss",
			TokenCost = 400,
			TitleColor = "#fbbf24"
		},
		new TamerTitle
		{
			Id = "myth_breaker",
			Title = "Myth Breaker",
			Description = "Conquer the Mythweaver's Realm",
			TokenCost = 750,
			RequiredBossId = "mythweavers_realm",
			TitleColor = "#ec4899"
		},
		new TamerTitle
		{
			Id = "genesis_witness",
			Title = "Genesis Witness",
			Description = "See what came before existence",
			TokenCost = 1500,
			RequiredBossId = "origin_void",
			TitleColor = "#ffffff"
		},
		new TamerTitle
		{
			Id = "supreme_tamer",
			Title = "Supreme Tamer",
			Description = "Defeat all bosses at least once",
			TokenCost = 2500,
			TitleColor = "#06b6d4"
		},

		// === Achievement-granted titles (TokenCost = 0, earned via achievements) ===

		// Collection
		new TamerTitle { Id = "Collector", Title = "Collector", Description = "Collect 10 unique beasts", TitleColor = "#4ade80" },
		new TamerTitle { Id = "Master Tamer", Title = "Master Tamer", Description = "Collect 30 unique beasts", TitleColor = "#22d3ee" },
		new TamerTitle { Id = "Beast Wrangler", Title = "Beast Wrangler", Description = "Collect 60 unique beasts", TitleColor = "#a78bfa" },
		new TamerTitle { Id = "Living Legend", Title = "Living Legend", Description = "Collect 100 unique beasts", TitleColor = "#fbbf24" },

		// Rarity hunting
		new TamerTitle { Id = "Legend Hunter", Title = "Legend Hunter", Description = "Catch a Legendary beast", TitleColor = "#fbbf24" },
		new TamerTitle { Id = "Mythic Hunter", Title = "Mythic Hunter", Description = "Catch a Mythic beast", TitleColor = "#ec4899" },
		new TamerTitle { Id = "Beastborne Master", Title = "Beastborne Master", Description = "Complete the full collection", TitleColor = "#f59e0b" },

		// Battle
		new TamerTitle { Id = "Veteran Fighter", Title = "Veteran Fighter", Description = "Win 50 battles", TitleColor = "#94a3b8" },
		new TamerTitle { Id = "Warborn", Title = "Warborn", Description = "Win 200 battles", TitleColor = "#ef4444" },
		new TamerTitle { Id = "Unbreakable", Title = "Unbreakable", Description = "Win 500 battles", TitleColor = "#f97316" },

		// Damage
		new TamerTitle { Id = "Devastator", Title = "Devastator", Description = "Deal massive total damage", TitleColor = "#ef4444" },
		new TamerTitle { Id = "Cataclysm", Title = "Cataclysm", Description = "Deal catastrophic total damage", TitleColor = "#dc2626" },

		// KOs
		new TamerTitle { Id = "Executioner", Title = "Executioner", Description = "KO many enemy beasts", TitleColor = "#b91c1c" },
		new TamerTitle { Id = "Annihilator", Title = "Annihilator", Description = "KO a huge number of enemy beasts", TitleColor = "#991b1b" },

		// Flawless & Elements
		new TamerTitle { Id = "Flawless", Title = "Flawless", Description = "Win battles without losing a beast", TitleColor = "#fbbf24" },
		new TamerTitle { Id = "Elemental Master", Title = "Elemental Master", Description = "Win with every element type", TitleColor = "#8b5cf6" },

		// Expedition
		new TamerTitle { Id = "Conqueror", Title = "Conqueror", Description = "Complete many expeditions", TitleColor = "#10b981" },
		new TamerTitle { Id = "Iron Explorer", Title = "Iron Explorer", Description = "Complete expeditions without failures", TitleColor = "#6b7280" },
		new TamerTitle { Id = "Unbreakable Explorer", Title = "Unbreakable Explorer", Description = "Survive the toughest expeditions", TitleColor = "#f97316" },
		new TamerTitle { Id = "Absolute Legend", Title = "Absolute Legend", Description = "Master all expedition content", TitleColor = "#fbbf24" },
		new TamerTitle { Id = "Expedition Veteran", Title = "Expedition Veteran", Description = "Complete a huge number of expeditions", TitleColor = "#059669" },
		new TamerTitle { Id = "Endless Explorer", Title = "Endless Explorer", Description = "Never stop exploring", TitleColor = "#14b8a6" },
		new TamerTitle { Id = "Boss Slayer", Title = "Boss Slayer", Description = "Defeat expedition bosses", TitleColor = "#fbbf24" },

		// Fusion
		new TamerTitle { Id = "Beast Fuser", Title = "Beast Fuser", Description = "Fuse beasts together", TitleColor = "#a855f7" },
		new TamerTitle { Id = "Genetics Expert", Title = "Genetics Expert", Description = "Perform many fusions", TitleColor = "#7c3aed" },
		new TamerTitle { Id = "Master Fuser", Title = "Master Fuser", Description = "Master the art of fusion", TitleColor = "#6d28d9" },
		new TamerTitle { Id = "Perfectionist", Title = "Perfectionist", Description = "Achieve perfect fusion results", TitleColor = "#fbbf24" },

		// Economy
		new TamerTitle { Id = "Wealthy", Title = "Wealthy", Description = "Accumulate significant gold", TitleColor = "#fbbf24" },
		new TamerTitle { Id = "Millionaire", Title = "Millionaire", Description = "Accumulate a million gold", TitleColor = "#f59e0b" },
		new TamerTitle { Id = "Billionaire", Title = "Billionaire", Description = "Accumulate a billion gold", TitleColor = "#d97706" },
		new TamerTitle { Id = "Big Spender", Title = "Big Spender", Description = "Spend a lot of gold", TitleColor = "#fbbf24" },

		// Arena
		new TamerTitle { Id = "Arena Warrior", Title = "Arena Warrior", Description = "Win arena battles", TitleColor = "#ef4444" },
		new TamerTitle { Id = "Arena Champion", Title = "Arena Champion", Description = "Dominate the arena", TitleColor = "#f59e0b" },
		new TamerTitle { Id = "Arena Legend", Title = "Arena Legend", Description = "Become an arena legend", TitleColor = "#fbbf24" },
		new TamerTitle { Id = "On Fire", Title = "On Fire", Description = "Achieve a long win streak", TitleColor = "#ef4444" },
		new TamerTitle { Id = "Unstoppable", Title = "Unstoppable", Description = "Achieve an incredible win streak", TitleColor = "#dc2626" },
		new TamerTitle { Id = "Giant Killer", Title = "Giant Killer", Description = "Defeat higher-ranked opponents", TitleColor = "#10b981" },
		new TamerTitle { Id = "Arena Veteran", Title = "Arena Veteran", Description = "Fight many arena battles", TitleColor = "#6b7280" },
		new TamerTitle { Id = "Comeback King", Title = "Comeback King", Description = "Win from behind", TitleColor = "#8b5cf6" },

		// Ranked titles
		new TamerTitle { Id = "Bronze Champion", Title = "Bronze Champion", Description = "Reach Bronze rank", TitleColor = "#cd7f32" },
		new TamerTitle { Id = "Silver Champion", Title = "Silver Champion", Description = "Reach Silver rank", TitleColor = "#c0c0c0" },
		new TamerTitle { Id = "Gold Champion", Title = "Gold Champion", Description = "Reach Gold rank", TitleColor = "#fbbf24" },
		new TamerTitle { Id = "Platinum Champion", Title = "Platinum Champion", Description = "Reach Platinum rank", TitleColor = "#06b6d4" },
		new TamerTitle { Id = "Diamond Champion", Title = "Diamond Champion", Description = "Reach Diamond rank", TitleColor = "#60a5fa" },
		new TamerTitle { Id = "Master Champion", Title = "Master Champion", Description = "Reach Master rank", TitleColor = "#a855f7" },
		new TamerTitle { Id = "Legendary Champion", Title = "Legendary Champion", Description = "Reach Legendary rank", TitleColor = "#f59e0b" },
		new TamerTitle { Id = "Mythic Champion", Title = "Mythic Champion", Description = "Reach Mythic rank", TitleColor = "#ec4899" },

		// Trading
		new TamerTitle { Id = "Merchant", Title = "Merchant", Description = "Complete trades", TitleColor = "#fbbf24" },
		new TamerTitle { Id = "Trade Baron", Title = "Trade Baron", Description = "Master of trading", TitleColor = "#f59e0b" },

		// Social
		new TamerTitle { Id = "Chatterbox", Title = "Chatterbox", Description = "Send many chat messages", TitleColor = "#60a5fa" },
		new TamerTitle { Id = "Social Network", Title = "Social Network", Description = "Add many friends", TitleColor = "#34d399" },

		// Tamer level
		new TamerTitle { Id = "Expert", Title = "Expert", Description = "Reach a high tamer level", TitleColor = "#22d3ee" },
		new TamerTitle { Id = "Centurion", Title = "Centurion", Description = "Reach tamer level 100", TitleColor = "#6366f1" },
		new TamerTitle { Id = "Grand Tamer", Title = "Grand Tamer", Description = "Reach a very high tamer level", TitleColor = "#8b5cf6" },
		new TamerTitle { Id = "Legendary Tamer", Title = "Legendary Tamer", Description = "Reach an elite tamer level", TitleColor = "#fbbf24" },
		new TamerTitle { Id = "Transcendent", Title = "Transcendent", Description = "Reach the highest tamer level", TitleColor = "#f59e0b" },

		// Misc progression
		new TamerTitle { Id = "Skill Master", Title = "Skill Master", Description = "Master beast skills", TitleColor = "#8b5cf6" },
		new TamerTitle { Id = "Darwin's Heir", Title = "Darwin's Heir", Description = "Evolve many beasts", TitleColor = "#10b981" },
		new TamerTitle { Id = "Evolution Master", Title = "Evolution Master", Description = "Master beast evolution", TitleColor = "#059669" },
		new TamerTitle { Id = "Battle Hardened", Title = "Battle Hardened", Description = "Survive many tough battles", TitleColor = "#94a3b8" },
		new TamerTitle { Id = "Point Master", Title = "Point Master", Description = "Earn many stat points", TitleColor = "#a855f7" },

		// Secret achievements
		new TamerTitle { Id = "Night Owl", Title = "Night Owl", Description = "Play during the late hours", TitleColor = "#6366f1" },
		new TamerTitle { Id = "Full House", Title = "Full House", Description = "Fill your roster completely", TitleColor = "#f59e0b" },
		new TamerTitle { Id = "Naturalist", Title = "Naturalist", Description = "Discover Nature-type beasts", TitleColor = "#22c55e" },
		new TamerTitle { Id = "Mono Master", Title = "Mono Master", Description = "Win with a single-element team", TitleColor = "#ec4899" },
	};

	/// <summary>
	/// Get theme by ID
	/// </summary>
	public static HudTheme GetTheme( string id )
	{
		return Themes.Find( t => t.Id == id );
	}

	/// <summary>
	/// Get title by ID
	/// </summary>
	public static TamerTitle GetTitle( string id )
	{
		return Titles.Find( t => t.Id == id );
	}
}
