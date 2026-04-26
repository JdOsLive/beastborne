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
	[System.Text.Json.Serialization.JsonPropertyName("sp_a")]
	public int SpA { get; set; }  // Special Attack
	[System.Text.Json.Serialization.JsonPropertyName("sp_d")]
	public int SpD { get; set; }  // Special Defense
	[System.Text.Json.Serialization.JsonPropertyName("speed")]
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

	// Original Trainer (preserved through trades)
	public string OriginalTrainerName { get; set; }
	public long OriginalTrainerId { get; set; }

	// Combat counters (used for achievements, tamer leaderboards, journal milestones).
	// Veteran-rank logic is gone; species-level mastery lives on Tamer.SpeciesMastery.
	public int TotalDamageDealt { get; set; } = 0;
	public int TotalKnockouts { get; set; } = 0;
	public int BossesDefeated { get; set; } = 0;
	public int ExpeditionsCompleted { get; set; } = 0;

	// Journal entries - auto-generated memories
	public List<JournalEntry> Journal { get; set; } = new();

	// Boss flag (for expedition bosses)
	public bool IsBoss { get; set; } = false;

	// Contract battle state (not persisted)
	[System.Text.Json.Serialization.JsonIgnore]
	public bool WasContracted { get; set; } = false;
	[System.Text.Json.Serialization.JsonIgnore]
	public float IntimidatePenalty { get; set; } = 0f;
	[System.Text.Json.Serialization.JsonIgnore]
	public int ContractAttemptsThisBattle { get; set; } = 0;

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

	// Power rating — sum of current (leveled + gene-applied) stats. 1:1 weighting.
	// No divisors, no rarity multiplier, no flat level bonus (growth already scales
	// stats with level). See .claude/balance-knowledge/power-formula.md.
	public int PowerRating => MaxHP + ATK + DEF + SpA + SpD + SPD;

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
			// Combat counters
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
