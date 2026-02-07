using System;

namespace Beastborne.Data;

/// <summary>
/// Compact tamer card data for network broadcasting (chat showcase, card exchange)
/// </summary>
public class TamerCardSnapshot
{
	public long SteamId { get; set; }
	public string Name { get; set; }
	public int Level { get; set; }
	public string Title { get; set; }
	public string Gender { get; set; }
	public string ArenaRank { get; set; }
	public int ArenaPoints { get; set; }
	public string FavoriteMonsterSpeciesId { get; set; }
	public int AchievementCount { get; set; }
	public int TotalAchievements { get; set; }
	public float WinRate { get; set; }
	public int TotalPlayTimeMinutes { get; set; }
	public int ArenaWins { get; set; }
	public int ArenaLosses { get; set; }
	public int MonstersCaught { get; set; }
	public int HighestExpedition { get; set; }
}
