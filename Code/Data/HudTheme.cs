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

		// === Achievement-granted (2026-09 restart set) ===
		// Ids are the persisted title strings — never rename. The legacy-only
		// titles (Master Tamer, Conqueror, Arena Legend, Transcendent) were
		// removed with the restart; PruneOrphanTitles drops any stragglers.
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
			Description = "Defeat 3 different expedition bosses",
			TitleColor = "#06b6d4"
		},
		new TamerTitle
		{
			Id = "Beastborne Master",
			Title = "Beastborne Master",
			Description = "Discover 20 species in the Beastbook",
			TitleColor = "#f59e0b"
		},
		new TamerTitle
		{
			Id = "Master Fuser",
			Title = "Master Fuser",
			Description = "Inscribe 2 fusion patterns in the Pattern Book",
			TitleColor = "#a855f7"
		},
		new TamerTitle
		{
			Id = "Unyielding",
			Title = "Unyielding",
			Description = "Clear 3 expeditions on Hard Mode",
			TitleColor = "#ef4444"
		},

		// === Login milestone titles (DailyRewardManager.GetMilestoneReward) ===
		// Ids must match the milestone title strings exactly. Without these
		// entries PruneOrphanTitles stripped them on every load.
		new TamerTitle { Id = "Dedicated", Title = "Dedicated", Description = "Log in on 7 different days", TitleColor = "#34d399" },
		new TamerTitle { Id = "Devoted", Title = "Devoted", Description = "Log in on 30 different days", TitleColor = "#60a5fa" },
		new TamerTitle { Id = "Faithful", Title = "Faithful", Description = "Log in on 60 different days", TitleColor = "#a78bfa" },
		new TamerTitle { Id = "Eternal", Title = "Eternal", Description = "Log in on 100 different days", TitleColor = "#f472b6" },
		new TamerTitle { Id = "Beastborne Veteran", Title = "Beastborne Veteran", Description = "Log in on 365 different days", TitleColor = "#fbbf24" },

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
