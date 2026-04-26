using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// Tracks discovered monster species (Pokedex-style)
/// "Seen" = encountered in expedition (shows image + name only)
/// "Discovered" = caught or owned (shows full details)
/// </summary>
public sealed class BeastiaryManager : Component
{
	public static BeastiaryManager Instance { get; private set; }

	public HashSet<string> DiscoveredSpecies { get; private set; } = new();
	public HashSet<string> SeenSpecies { get; private set; } = new();

	private bool _hasHydrated;

	// Events
	public Action<string> OnSpeciesDiscovered;
	public Action<string> OnSpeciesSeen;

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Log.Info( "BeastiaryManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnStart()
	{
		if ( SaveService.Instance != null && SaveService.Instance.IsLoaded )
		{
			Hydrate();
		}
		else if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveLoaded += Hydrate;
		}

		if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveReset += HandleSaveReset;
		}
	}

	private void HandleSaveReset()
	{
		_hasHydrated = false;
		DiscoveredSpecies?.Clear();
		SeenSpecies?.Clear();
		Hydrate();
		Log.Info( "[BeastiaryManager] reset to empty" );
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "BeastiaryManager";
		go.Components.Create<BeastiaryManager>();
	}

	private void Hydrate()
	{
		if ( _hasHydrated ) return;
		_hasHydrated = true;

		var blob = SaveService.Instance?.CurrentBlob;
		var section = blob?.Beastiary;

		DiscoveredSpecies = section?.DiscoveredSpecies != null
			? new HashSet<string>( section.DiscoveredSpecies )
			: new HashSet<string>();

		SeenSpecies = section?.SeenSpecies != null
			? new HashSet<string>( section.SeenSpecies )
			: new HashSet<string>();

		// Migrate renamed species IDs
		bool migrated = false;
		foreach ( var (oldId, newId) in MonsterManager.OldSpeciesIdMap )
		{
			if ( DiscoveredSpecies.Remove( oldId ) )
			{
				DiscoveredSpecies.Add( newId );
				migrated = true;
			}
			if ( SeenSpecies.Remove( oldId ) )
			{
				SeenSpecies.Add( newId );
				migrated = true;
			}
		}

		if ( migrated )
		{
			SaveDiscoveries();
			SaveSeen();
			Log.Info( "Migrated beastiary entries for renamed species" );
		}

		Log.Info( $"[BeastiaryManager] Hydrated: {DiscoveredSpecies.Count} discovered, {SeenSpecies.Count} seen" );
	}

	private void WriteSnapshot()
	{
		var service = SaveService.Instance;
		if ( service == null ) return;
		var blob = service.CurrentBlob;
		if ( blob == null ) return;

		blob.Beastiary ??= new BeastiarySaveData();
		blob.Beastiary.DiscoveredSpecies = DiscoveredSpecies.ToList();
		blob.Beastiary.SeenSpecies = SeenSpecies.ToList();
		service.MarkDirty( "beastiary" );
	}

	private void SaveDiscoveries() => WriteSnapshot();
	private void SaveSeen() => WriteSnapshot();

	/// <summary>
	/// Mark a species as seen (encountered in expedition)
	/// Shows image and name in beastiary, but not full details
	/// </summary>
	public void SeeSpecies( string speciesId )
	{
		if ( string.IsNullOrEmpty( speciesId ) ) return;
		if ( SeenSpecies.Contains( speciesId ) ) return;

		SeenSpecies.Add( speciesId );
		SaveSeen();

		Log.Info( $"Saw new species: {speciesId}" );
		OnSpeciesSeen?.Invoke( speciesId );
	}

	/// <summary>
	/// Mark a species as fully discovered (caught or owned)
	/// Shows all details in beastiary
	/// </summary>
	public void DiscoverSpecies( string speciesId )
	{
		if ( string.IsNullOrEmpty( speciesId ) ) return;

		// Also mark as seen
		if ( !SeenSpecies.Contains( speciesId ) )
		{
			SeenSpecies.Add( speciesId );
			SaveSeen();
		}

		if ( DiscoveredSpecies.Contains( speciesId ) ) return;

		DiscoveredSpecies.Add( speciesId );
		SaveDiscoveries();

		Log.Info( $"Discovered new species: {speciesId}" );
		OnSpeciesDiscovered?.Invoke( speciesId );

		// Show discovery notification if enabled
		if ( SettingsManager.Instance?.Settings?.ShowDiscoveryAlerts != false )
		{
			var species = MonsterManager.Instance?.GetSpecies( speciesId );
			if ( species != null )
			{
				NotificationManager.Instance?.AddNotification(
					NotificationType.Success,
					"New Discovery!",
					$"You discovered {species.Name}!"
				);
			}
		}

		// Check bestiary completion achievement
		int total = GetTotalSpeciesCount();
		if ( total > 0 && DiscoveredSpecies.Count >= total )
		{
			AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.BeastiaryCompleted, 1 );
		}
	}

	public bool IsDiscovered( string speciesId )
	{
		return DiscoveredSpecies.Contains( speciesId );
	}

	public bool IsSeen( string speciesId )
	{
		return SeenSpecies.Contains( speciesId );
	}

	public int GetDiscoveryCount()
	{
		return DiscoveredSpecies.Count;
	}

	public int GetSeenCount()
	{
		return SeenSpecies.Count;
	}

	public int GetTotalSpeciesCount()
	{
		return MonsterManager.Instance?.SpeciesDatabase.Count ?? 0;
	}

	public float GetCompletionPercent()
	{
		int total = GetTotalSpeciesCount();
		if ( total == 0 ) return 0;
		return (float)DiscoveredSpecies.Count / total;
	}

	public string GetCompletionText()
	{
		return $"{DiscoveredSpecies.Count}/{GetTotalSpeciesCount()}";
	}

	// ============================================
	// SPECIES MASTERY SYSTEM
	// ============================================
	//
	// Mastery is a tamer-level progression per species (not per beast).
	// Thresholds are OR-gated and strictly sequential — you must clear
	// level N's criteria (even if you already met higher criteria) to
	// advance past level N.
	//
	//   Level 0: not contracted yet — 0% bonus, "Unbound"
	//   Level 1: Contract 1       — +2%, "Novice"
	//   Level 2: Defeat 10        — +4%, "Adept"
	//   Level 3: Contract 3       — +6%, "Veteran"
	//   Level 4: Defeat 50        — +8%, "Elite"
	//   Level 5: Fuse 1           — +10%, "Master"
	//   Level 6: Reach max evo    — +12%, "Grandmaster"
	//
	// Data lives on Tamer.SpeciesMastery (saved with the tamer).

	private static readonly string[] _masteryTitles =
	{
		"Unbound",      // 0
		"Novice",       // 1
		"Adept",        // 2
		"Veteran",      // 3
		"Elite",        // 4
		"Master",       // 5
		"Grandmaster",  // 6
	};

	private static readonly float[] _masteryBonuses =
	{
		0.00f, 0.02f, 0.04f, 0.06f, 0.08f, 0.10f, 0.12f,
	};

	/// <summary>
	/// Get (or lazily create) the mastery record for a species.
	/// </summary>
	public SpeciesMasteryData GetMastery( string speciesId )
	{
		if ( string.IsNullOrEmpty( speciesId ) ) return new SpeciesMasteryData { SpeciesId = speciesId };

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return new SpeciesMasteryData { SpeciesId = speciesId };

		tamer.SpeciesMastery ??= new Dictionary<string, SpeciesMasteryData>();
		if ( !tamer.SpeciesMastery.TryGetValue( speciesId, out var data ) )
		{
			data = new SpeciesMasteryData
			{
				SpeciesId = speciesId,
				FirstDiscovered = DateTime.UtcNow,
			};
			tamer.SpeciesMastery[speciesId] = data;
		}
		return data;
	}

	/// <summary>
	/// Multiplier (0.00–0.12) applied to a species' combat stats based on current mastery level.
	/// </summary>
	public float GetMasteryBonus( string speciesId )
	{
		int lvl = GetMasteryLevel( speciesId );
		if ( lvl < 0 || lvl >= _masteryBonuses.Length ) return 0f;
		return _masteryBonuses[lvl];
	}

	/// <summary>
	/// Current mastery level 0-6.
	/// </summary>
	public int GetMasteryLevel( string speciesId )
	{
		if ( string.IsNullOrEmpty( speciesId ) ) return 0;
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer?.SpeciesMastery == null ) return 0;
		return tamer.SpeciesMastery.TryGetValue( speciesId, out var data ) ? data.Level : 0;
	}

	/// <summary>
	/// Species-specific title, e.g. "Emberhound Master". If the level is 0, returns just the unbound tag.
	/// </summary>
	public string GetMasteryTitle( string speciesId )
	{
		int lvl = GetMasteryLevel( speciesId );
		var species = MonsterManager.Instance?.GetSpecies( speciesId );
		var speciesName = species?.Name ?? speciesId ?? "Unknown";
		var titleWord = (lvl >= 0 && lvl < _masteryTitles.Length) ? _masteryTitles[lvl] : _masteryTitles[0];
		return $"{speciesName} {titleWord}";
	}

	/// <summary>
	/// Return the title word only (e.g. "Novice", "Grandmaster") for the current level.
	/// </summary>
	public string GetMasteryTitleWord( string speciesId )
	{
		int lvl = GetMasteryLevel( speciesId );
		return (lvl >= 0 && lvl < _masteryTitles.Length) ? _masteryTitles[lvl] : _masteryTitles[0];
	}

	/// <summary>
	/// Progress tuple toward the NEXT level. Returns (current, required, description).
	/// If already at max level (6), returns (1, 1, "Max mastery achieved").
	/// </summary>
	public (int current, int required, string description) GetProgressToNext( string speciesId )
	{
		var data = GetMastery( speciesId );
		var species = MonsterManager.Instance?.GetSpecies( speciesId );
		var speciesName = species?.Name ?? "beast";

		int lvl = data.Level;
		switch ( lvl )
		{
			case 0:
				// Needs: contract 1
				return (data.CaptureCount, 1, $"Contract a {speciesName}");
			case 1:
				// Needs: defeat 10
				return (Math.Min( data.DefeatCount, 10 ), 10, $"Defeat {speciesName} ({data.DefeatCount}/10)");
			case 2:
				// Needs: contract 3 total
				return (Math.Min( data.CaptureCount, 3 ), 3, $"Contract {speciesName} ({data.CaptureCount}/3)");
			case 3:
				// Needs: defeat 50 total
				return (Math.Min( data.DefeatCount, 50 ), 50, $"Defeat {speciesName} ({data.DefeatCount}/50)");
			case 4:
				// Needs: fuse 1
				return (data.HasFused ? 1 : 0, 1, $"Fuse a {speciesName}");
			case 5:
				// Needs: reach max evolution form
				return (data.HasMaxEvolved ? 1 : 0, 1, $"Reach {speciesName}'s final form");
			default:
				return (1, 1, "Max mastery achieved");
		}
	}

	/// <summary>
	/// Called when a beast of this species is added to the roster (contracted or bred).
	/// Increments CaptureCount and checks Lv1/Lv3 thresholds.
	/// </summary>
	public void OnBeastContracted( string speciesId )
	{
		if ( string.IsNullOrEmpty( speciesId ) ) return;
		var data = GetMastery( speciesId );
		data.CaptureCount++;
		CheckLevelUp( speciesId );
	}

	/// <summary>
	/// Called when a wild/enemy beast of this species is knocked out by the player.
	/// Increments DefeatCount and checks Lv2/Lv4 thresholds.
	/// </summary>
	public void OnBeastDefeated( string speciesId )
	{
		if ( string.IsNullOrEmpty( speciesId ) ) return;
		var data = GetMastery( speciesId );
		data.DefeatCount++;
		CheckLevelUp( speciesId );
	}

	/// <summary>
	/// Called when a fusion completes consuming a beast of this species.
	/// </summary>
	public void OnBeastFused( string speciesId )
	{
		if ( string.IsNullOrEmpty( speciesId ) ) return;
		var data = GetMastery( speciesId );
		if ( !data.HasFused )
		{
			data.HasFused = true;
			CheckLevelUp( speciesId );
		}
	}

	/// <summary>
	/// Called when a beast reaches its final (max) evolution form for this species.
	/// </summary>
	public void OnBeastMaxEvolved( string speciesId )
	{
		if ( string.IsNullOrEmpty( speciesId ) ) return;
		var data = GetMastery( speciesId );
		if ( !data.HasMaxEvolved )
		{
			data.HasMaxEvolved = true;
			CheckLevelUp( speciesId );
		}
	}

	/// <summary>
	/// Evaluate all milestones in sequence and update Level to the highest achieved.
	/// Fires a notification and saves the tamer on level-up.
	/// </summary>
	private void CheckLevelUp( string speciesId )
	{
		var data = GetMastery( speciesId );
		int oldLevel = data.Level;
		int newLevel = oldLevel;

		// Sequentially unlock each level — must clear level N to reach level N+1.
		while ( newLevel < 6 )
		{
			bool passed = newLevel switch
			{
				0 => data.CaptureCount >= 1,     // Lv1: contract 1
				1 => data.DefeatCount >= 10,     // Lv2: defeat 10
				2 => data.CaptureCount >= 3,     // Lv3: contract 3
				3 => data.DefeatCount >= 50,     // Lv4: defeat 50
				4 => data.HasFused,              // Lv5: fuse 1
				5 => data.HasMaxEvolved,         // Lv6: max evolve
				_ => false
			};
			if ( !passed ) break;
			newLevel++;
		}

		if ( newLevel > oldLevel )
		{
			data.Level = newLevel;

			var species = MonsterManager.Instance?.GetSpecies( speciesId );
			var speciesName = species?.Name ?? speciesId;
			var titleWord = (newLevel >= 0 && newLevel < _masteryTitles.Length) ? _masteryTitles[newLevel] : "";
			int pct = (int)(_masteryBonuses[newLevel] * 100);

			NotificationManager.Instance?.AddNotification(
				NotificationType.Success,
				"Beastbook Mastery!",
				$"{speciesName} — now a {titleWord} (+{pct}% stats)"
			);

			// Fire achievement for reaching max mastery on any species
			if ( newLevel >= 6 )
			{
				AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.MonsterVeteranMaxRank, 1 );
			}

			// Persist
			TamerManager.Instance?.SaveToCloud();

			Log.Info( $"Species mastery: {speciesName} -> Lv{newLevel} ({titleWord})" );
		}
	}
}
