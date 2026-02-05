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
		}
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
