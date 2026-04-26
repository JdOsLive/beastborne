using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Title cosmetic that displays near the player name.
/// Launch set is intentionally tiny — one title per major game pillar plus
/// special grants (Alpha for pre-launch players, Johnson as a flavor title).
/// </summary>
public class TamerTitle
{
	public string Id { get; set; }

	/// <summary>The title text displayed.</summary>
	public string Title { get; set; }

	/// <summary>Description of how to earn this title.</summary>
	public string Description { get; set; }

	/// <summary>Color for the title text.</summary>
	public string TitleColor { get; set; } = "#ffffff";
}

/// <summary>
/// Static catalogue of all titles in the game. Kept tiny on purpose — every
/// listed title must have a real grant path (achievement reward or special
/// grant in TamerManager). If you want to add one, wire its grant FIRST.
/// </summary>
public static class CosmeticDatabase
{
	// Special grants (handled in TamerManager, not via achievements)
	public const string AlphaTitleId = "alpha";
	public const string JohnsonTitleId = "johnson";

	public static List<TamerTitle> Titles { get; } = new()
	{
		// === Special grants ===
		new TamerTitle
		{
			Id = AlphaTitleId,
			Title = "Alpha",
			Description = "Played Beastborne before its full launch. Thank you.",
			TitleColor = "#f472b6"
		},
		new TamerTitle
		{
			Id = JohnsonTitleId,
			Title = "Johnson",
			Description = "Named after one of the funniest guys around. Worn with honor.",
			TitleColor = "#22d3ee"
		},

		// === Achievement-granted (one per major pillar) ===
		new TamerTitle
		{
			Id = "Boss Slayer",
			Title = "Boss Slayer",
			Description = "Defeat your first expedition boss",
			TitleColor = "#fbbf24"
		},
		new TamerTitle
		{
			Id = "Supreme Tamer",
			Title = "Supreme Tamer",
			Description = "Defeat every expedition boss at least once",
			TitleColor = "#06b6d4"
		},
		new TamerTitle
		{
			Id = "Master Tamer",
			Title = "Master Tamer",
			Description = "Catch 100 monsters",
			TitleColor = "#22d3ee"
		},
		new TamerTitle
		{
			Id = "Beastborne Master",
			Title = "Beastborne Master",
			Description = "Discover every species in the Beastiary",
			TitleColor = "#f59e0b"
		},
		new TamerTitle
		{
			Id = "Conqueror",
			Title = "Conqueror",
			Description = "Clear every expedition",
			TitleColor = "#10b981"
		},
		new TamerTitle
		{
			Id = "Arena Legend",
			Title = "Arena Legend",
			Description = "Win 100 ranked sets",
			TitleColor = "#fbbf24"
		},
		new TamerTitle
		{
			Id = "Master Fuser",
			Title = "Master Fuser",
			Description = "Fuse 100 monsters",
			TitleColor = "#a855f7"
		},
		new TamerTitle
		{
			Id = "Transcendent",
			Title = "Transcendent",
			Description = "Reach the highest tamer level",
			TitleColor = "#f59e0b"
		},

		// === Guild Raid titles (granted via raid score milestones) ===
		new TamerTitle
		{
			Id = "Raid Slayer",
			Title = "Raid Slayer",
			Description = "Complete your first guild raid attempt",
			TitleColor = "#ef4444"
		},
		new TamerTitle
		{
			Id = "Boss Hunter",
			Title = "Boss Hunter",
			Description = "Reach a guild raid score of 100,000",
			TitleColor = "#f97316"
		},
		new TamerTitle
		{
			Id = "Worldbreaker",
			Title = "Worldbreaker",
			Description = "Reach a guild raid score of 500,000",
			TitleColor = "#fbbf24"
		},
	};

	/// <summary>Get title by ID. Returns null if no longer in the catalogue.</summary>
	public static TamerTitle GetTitle( string id )
	{
		if ( string.IsNullOrEmpty( id ) ) return null;
		return Titles.Find( t => t.Id == id );
	}
}
