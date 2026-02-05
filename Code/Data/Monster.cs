using System;
using System.Collections.Generic;
using System.Linq;

namespace Beastborne.Data;

/// <summary>
/// Individual monster instance owned by the player
/// </summary>
public class Monster
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string SpeciesId { get; set; }
	public string Nickname { get; set; }

	// Level and XP
	public int Level { get; set; } = 1;
	public int CurrentXP { get; set; } = 0;

	// Current HP (can be damaged)
	public int CurrentHP { get; set; }

	// Calculated max stats (base + genetics + level)
	public int MaxHP { get; set; }
	public int ATK { get; set; }
	public int DEF { get; set; }
	public int SpA { get; set; }  // Special Attack
	public int SpD { get; set; }  // Special Defense
	public int SPD { get; set; }  // Speed

	// Genetics system
	public Genetics Genetics { get; set; }

	// Traits (passive abilities)
	public List<string> Traits { get; set; } = new();

	// Known moves (max 4)
	public List<MonsterMove> Moves { get; set; } = new();
	public const int MaxMoves = 4;

	// Contract (null if bred - bred monsters are always loyal)
	public Contract Contract { get; set; }

	// Held item (equipment this monster is holding, 1 max)
	public string HeldItemId { get; set; }

	// Is this monster bred (loyal) or caught (has contract)?
	public bool IsBred => Contract == null;
	public bool IsLoyal => IsBred;

	// Lineage tracking for breeding
	public Guid? Parent1Id { get; set; }
	public Guid? Parent2Id { get; set; }
	public int Generation { get; set; } = 0;

	// State
	public bool IsInExpedition { get; set; }
	public bool IsInArenaTeam { get; set; }
	public bool IsFavorite { get; set; }
	public bool HasBeenNotifiedForEvolution { get; set; }
	public DateTime ObtainedAt { get; set; } = DateTime.UtcNow;

	// Veteran tracking - battles, damage, KOs
	public int BattlesFought { get; set; } = 0;
	public int TotalDamageDealt { get; set; } = 0;
	public int TotalKnockouts { get; set; } = 0;
	public int BossesDefeated { get; set; } = 0;
	public int ExpeditionsCompleted { get; set; } = 0;

	// Journal entries - auto-generated memories
	public List<JournalEntry> Journal { get; set; } = new();

	// Boss flag (for expedition bosses)
	public bool IsBoss { get; set; } = false;

	// Alias for ObtainedAt (used by UI)
	public DateTime CaughtAt => ObtainedAt;

	// XP required for next level (easier early, harder late)
	// L1: ~25, L10: ~90, L20: ~260, L50: ~2050, L100: ~9100
	public int XPForNextLevel => (int)(25 + Math.Pow(Level, 2.2) * 0.5);

	// Alias for XPForNextLevel (used by UI)
	public int XPToNextLevel => XPForNextLevel;

	// XP progress as percentage (0-1)
	public float XPProgress => (float)CurrentXP / XPForNextLevel;

	// HP as percentage (0-1)
	public float HPPercent => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0;

	// Add XP and handle level ups, returns true if leveled up
	public bool GainXP( int amount ) => AddXP( amount );
	public bool AddXP( int amount )
	{
		// Don't gain XP if already at max level
		if ( Level >= 100 )
		{
			CurrentXP = 0;
			return false;
		}

		CurrentXP += amount;
		bool leveledUp = false;

		while ( CurrentXP >= XPForNextLevel && Level < 100 )
		{
			CurrentXP -= XPForNextLevel;
			Level++;
			leveledUp = true;
		}

		// Cap XP at 0 if we just hit max level
		if ( Level >= 100 )
			CurrentXP = 0;

		return leveledUp;
	}

	// Heal the monster
	public void Heal( int amount )
	{
		CurrentHP = Math.Min( CurrentHP + amount, MaxHP );
	}

	// Fully heal
	public void FullHeal()
	{
		CurrentHP = MaxHP;
	}

	// Take damage, returns true if knocked out
	public bool TakeDamage( int amount )
	{
		CurrentHP = Math.Max( CurrentHP - amount, 0 );
		return CurrentHP <= 0;
	}

	// Check if can evolve (has evolution and at required level)
	public bool CanEvolve( MonsterSpecies species )
	{
		return !string.IsNullOrEmpty( species?.EvolvesTo ) && Level >= species.EvolutionLevel;
	}

	// Get power rating (rough estimate of strength)
	public int PowerRating => (MaxHP / 10) + ATK + DEF + SpA + SpD + (SPD / 2) + (Level * 5);

	// ============================================
	// BATTLE MASTERY SYSTEM
	// ============================================

	/// <summary>
	/// Get the battle mastery rank based on battles fought
	/// </summary>
	public VeteranRank GetVeteranRank()
	{
		if ( BattlesFought >= 1500 ) return VeteranRank.Legend;
		if ( BattlesFought >= 750 ) return VeteranRank.Champion;
		if ( BattlesFought >= 400 ) return VeteranRank.Elite;
		if ( BattlesFought >= 200 ) return VeteranRank.Veteran;
		if ( BattlesFought >= 100 ) return VeteranRank.Seasoned;
		if ( BattlesFought >= 25 ) return VeteranRank.Trained;
		return VeteranRank.Rookie;
	}

	/// <summary>
	/// Get stat bonus percentage from veteran rank (0-15%)
	/// </summary>
	public float GetVeteranBonusPercent()
	{
		return GetVeteranRank() switch
		{
			VeteranRank.Rookie => 0f,
			VeteranRank.Trained => 0.02f,    // +2%
			VeteranRank.Seasoned => 0.04f,   // +4%
			VeteranRank.Veteran => 0.07f,    // +7%
			VeteranRank.Elite => 0.10f,      // +10%
			VeteranRank.Champion => 0.13f,   // +13%
			VeteranRank.Legend => 0.15f,     // +15%
			_ => 0f
		};
	}

	/// <summary>
	/// Get battles needed for next veteran rank
	/// </summary>
	public int BattlesToNextRank()
	{
		var rank = GetVeteranRank();
		int threshold = rank switch
		{
			VeteranRank.Rookie => 25,
			VeteranRank.Trained => 100,
			VeteranRank.Seasoned => 200,
			VeteranRank.Veteran => 400,
			VeteranRank.Elite => 750,
			VeteranRank.Champion => 1500,
			VeteranRank.Legend => 0, // Max rank
			_ => 0
		};
		return threshold > 0 ? threshold - BattlesFought : 0;
	}

	/// <summary>
	/// Add a journal entry for this monster
	/// </summary>
	public void AddJournalEntry( string content, JournalEntryType type = JournalEntryType.General, string speciesId = null, string zoneId = null )
	{
		Journal ??= new List<JournalEntry>();
		Journal.Add( new JournalEntry
		{
			Timestamp = DateTime.UtcNow,
			Content = content,
			Type = type,
			SpeciesId = speciesId,
			ZoneId = zoneId
		} );

		// Keep journal to reasonable size (last 50 entries)
		if ( Journal.Count > 50 )
			Journal.RemoveAt( 0 );
	}

	/// <summary>
	/// Create a shallow clone of this monster for battle simulation
	/// </summary>
	public Monster Clone()
	{
		return new Monster
		{
			Id = Id,
			SpeciesId = SpeciesId,
			Nickname = Nickname,
			Level = Level,
			CurrentXP = CurrentXP,
			CurrentHP = CurrentHP,
			MaxHP = MaxHP,
			ATK = ATK,
			DEF = DEF,
			SpA = SpA,
			SpD = SpD,
			SPD = SPD,
			Genetics = Genetics,
			Traits = Traits != null ? new List<string>( Traits ) : new List<string>(),
			Moves = Moves != null ? Moves.Select( m => new MonsterMove { MoveId = m.MoveId, CurrentPP = m.CurrentPP } ).ToList() : new List<MonsterMove>(),
			Contract = Contract,
			HeldItemId = HeldItemId,
			Parent1Id = Parent1Id,
			Parent2Id = Parent2Id,
			Generation = Generation,
			IsInExpedition = IsInExpedition,
			IsInArenaTeam = IsInArenaTeam,
			IsFavorite = IsFavorite,
			HasBeenNotifiedForEvolution = HasBeenNotifiedForEvolution,
			ObtainedAt = ObtainedAt,
			IsBoss = IsBoss,
			// Veteran tracking
			BattlesFought = BattlesFought,
			TotalDamageDealt = TotalDamageDealt,
			TotalKnockouts = TotalKnockouts,
			BossesDefeated = BossesDefeated,
			ExpeditionsCompleted = ExpeditionsCompleted,
			Journal = Journal != null ? new List<JournalEntry>( Journal ) : new List<JournalEntry>()
		};
	}

	/// <summary>
	/// Restore PP for all moves (called after expedition)
	/// </summary>
	public void RestoreAllPP( Func<string, MoveDefinition> getMoveDefinition )
	{
		foreach ( var move in Moves )
		{
			var def = getMoveDefinition( move.MoveId );
			if ( def != null )
				move.RestorePP( def.MaxPP );
		}
	}
}

/// <summary>
/// Veteran rank based on battles fought
/// </summary>
public enum VeteranRank
{
	Rookie,     // 0-4 battles
	Trained,    // 5-19 battles
	Seasoned,   // 20-49 battles
	Veteran,    // 50-99 battles
	Elite,      // 100-199 battles
	Champion,   // 200-499 battles
	Legend      // 500+ battles
}

/// <summary>
/// Types of journal entries
/// </summary>
public enum JournalEntryType
{
	General,
	Caught,
	Bred,
	Evolution,
	BossDefeat,
	Milestone,
	Expedition
}

/// <summary>
/// A single journal entry for a monster
/// </summary>
public class JournalEntry
{
	public DateTime Timestamp { get; set; }
	public string Content { get; set; }
	public JournalEntryType Type { get; set; }

	// Optional metadata for displaying images
	public string SpeciesId { get; set; }  // For boss defeats - shows the boss sprite
	public string ZoneId { get; set; }     // For expeditions - shows the zone background
}
