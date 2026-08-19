using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// A hand-authored cross-species fusion pattern — the ONLY way two different
/// species can be woven together. Same-species fusion ("Gene Weave") needs no
/// pattern; any non-pattern cross pair is refused by the loom.
/// </summary>
public class FusionPattern
{
	/// <summary>Stable id persisted in the save's discovered-patterns list. Never rename.</summary>
	public string Id { get; set; }

	/// <summary>Display name of the pattern (e.g. "The Patient Vow").</summary>
	public string Name { get; set; }

	/// <summary>
	/// Riddle shown in the Pattern Book while undiscovered — hints at the
	/// result without naming it.
	/// </summary>
	public string RiddleHint { get; set; }

	/// <summary>First parent species id. Pairs are UNORDERED — (A,B) == (B,A).</summary>
	public string ParentA { get; set; }

	/// <summary>Second parent species id. Pairs are UNORDERED — (A,B) == (B,A).</summary>
	public string ParentB { get; set; }

	/// <summary>
	/// Species the weave produces. HARD RULE (validator-enforced): must be a
	/// standalone species — no EvolvesFrom AND no EvolvesTo — unless flagged
	/// <see cref="MonsterSpecies.FusionOnly"/>. Evolution must never be
	/// counterfeited by the loom.
	/// </summary>
	public string ResultSpeciesId { get; set; }

	/// <summary>Minimum floor(avg parent level) required to weave. Default 1.</summary>
	public int MinAvgLevel { get; set; } = 1;

	/// <summary>True if the given unordered species pair matches this pattern.</summary>
	public bool Matches( string speciesA, string speciesB )
	{
		if ( string.IsNullOrEmpty( speciesA ) || string.IsNullOrEmpty( speciesB ) ) return false;
		return (ParentA == speciesA && ParentB == speciesB)
			|| (ParentA == speciesB && ParentB == speciesA);
	}
}

/// <summary>
/// THE PATTERN BOOK — static registry of every cross-species fusion pattern.
/// Parents MAY belong to leveling evolution lines; only RESULTS are restricted
/// (standalone-or-FusionOnly, see <see cref="Validate"/>). Discovered pattern
/// ids persist on the save blob via <c>MonsterManager.DiscoveredPatterns</c>.
/// </summary>
public static class FusionPatterns
{
	public static readonly IReadOnlyList<FusionPattern> All = new List<FusionPattern>
	{
		new FusionPattern
		{
			Id = "patient_vow",
			Name = "The Patient Vow",
			RiddleHint = "Duty learns stillness beneath the fountain.",
			ParentA = "pagefin",
			ParentB = "dewdrop",
			ResultSpeciesId = "heartwell",
			MinAvgLevel = 1
		},
		new FusionPattern
		{
			Id = "janus_moon",
			Name = "The Janus Moon",
			RiddleHint = "The garden's keeper meets the garden's thief; the moon shows two faces.",
			ParentA = "gnoll",
			ParentB = "jackacabra",
			ResultSpeciesId = "twincoil",
			MinAvgLevel = 1
		},
		new FusionPattern
		{
			Id = "ears_of_the_wood",
			Name = "Ears of the Wood",
			RiddleHint = "The wood's tidiest keeper and its darkest listener weave a rumor with long ears.",
			ParentA = "gnoll",
			ParentB = "threadlet",
			ResultSpeciesId = "jackacabra",
			MinAvgLevel = 1
		},
	};

	/// <summary>
	/// Find the pattern matching an UNORDERED species pair. Null if the loom
	/// knows no such pattern (cross-species fusion is then refused).
	/// Same-species pairs never match a pattern — that's Gene Weave.
	/// </summary>
	public static FusionPattern Find( string speciesA, string speciesB )
	{
		if ( speciesA == speciesB ) return null;
		return All.FirstOrDefault( p => p.Matches( speciesA, speciesB ) );
	}

	/// <summary>All patterns that use the given species as a parent.</summary>
	public static IEnumerable<FusionPattern> PatternsUsing( string speciesId )
	{
		return All.Where( p => p.ParentA == speciesId || p.ParentB == speciesId );
	}

	/// <summary>
	/// Static load-time validator. HARD RULE: a pattern's result may NEVER be
	/// a species reachable by leveling evolution (must have no EvolvesFrom and
	/// no EvolvesTo), unless the species is flagged FusionOnly. Also verifies
	/// parents/results exist and ids are unique. Logs an error naming every
	/// violation; returns true when the book is clean.
	/// </summary>
	public static bool Validate( IReadOnlyDictionary<string, MonsterSpecies> speciesDatabase )
	{
		bool clean = true;
		var seenIds = new HashSet<string>();

		foreach ( var pattern in All )
		{
			if ( string.IsNullOrEmpty( pattern.Id ) || !seenIds.Add( pattern.Id ) )
			{
				Log.Error( $"[FusionPatterns] Pattern '{pattern.Name}' has a missing or duplicate id '{pattern.Id}'." );
				clean = false;
			}

			if ( !speciesDatabase.ContainsKey( pattern.ParentA ?? "" ) )
			{
				Log.Error( $"[FusionPatterns] Pattern '{pattern.Id}': unknown parent species '{pattern.ParentA}'." );
				clean = false;
			}

			if ( !speciesDatabase.ContainsKey( pattern.ParentB ?? "" ) )
			{
				Log.Error( $"[FusionPatterns] Pattern '{pattern.Id}': unknown parent species '{pattern.ParentB}'." );
				clean = false;
			}

			if ( pattern.ParentA == pattern.ParentB )
			{
				Log.Error( $"[FusionPatterns] Pattern '{pattern.Id}': parents must be two different species (same-species is Gene Weave)." );
				clean = false;
			}

			if ( !speciesDatabase.TryGetValue( pattern.ResultSpeciesId ?? "", out var result ) )
			{
				Log.Error( $"[FusionPatterns] Pattern '{pattern.Id}': unknown result species '{pattern.ResultSpeciesId}'." );
				clean = false;
				continue;
			}

			// THE HARD RULE — the loom may not counterfeit evolution.
			bool inEvolutionLine = !string.IsNullOrEmpty( result.EvolvesFrom ) || !string.IsNullOrEmpty( result.EvolvesTo );
			if ( inEvolutionLine && !result.FusionOnly )
			{
				Log.Error( $"[FusionPatterns] VIOLATION in pattern '{pattern.Id}' ({pattern.Name}): result '{result.Id}' belongs to a leveling evolution line (EvolvesFrom='{result.EvolvesFrom}', EvolvesTo='{result.EvolvesTo}') and is not FusionOnly. Evolution must never be counterfeited by the loom." );
				clean = false;
			}
		}

		if ( clean )
			Log.Info( $"[FusionPatterns] Validated {All.Count} patterns — all results standalone-or-FusionOnly." );

		return clean;
	}
}
