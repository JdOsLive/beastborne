using Sandbox;
using System.Text.Json;

namespace Beastborne.Core;

/// <summary>
/// Tracks discovered monster species (Pokedex-style)
/// "Seen" = encountered in expedition (shows image + name only)
/// "Discovered" = caught or owned (shows full details)
/// </summary>
public sealed class BeastiaryManager : Component
{
	public static BeastiaryManager Instance { get; private set; }

	private const string BEASTIARY_COOKIE_KEY = "beastiary-discovered";
	private const string BEASTIARY_SEEN_COOKIE_KEY = "beastiary-seen";

	/// <summary>
	/// Get the full key with slot prefix
	/// </summary>
	private static string GetKey( string key ) => $"{SaveSlotManager.GetSlotPrefix()}{key}";

	public HashSet<string> DiscoveredSpecies { get; private set; } = new();
	public HashSet<string> SeenSpecies { get; private set; } = new();

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
		LoadDiscoveries();
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "BeastiaryManager";
		go.Components.Create<BeastiaryManager>();
	}

	private void LoadDiscoveries()
	{
		// Load discovered species
		var json = Game.Cookies.Get<string>( GetKey( BEASTIARY_COOKIE_KEY ), "[]" );
		try
		{
			var list = JsonSerializer.Deserialize<List<string>>( json ) ?? new();
			DiscoveredSpecies = new HashSet<string>( list );
		}
		catch
		{
			DiscoveredSpecies = new HashSet<string>();
		}

		// Load seen species
		var seenJson = Game.Cookies.Get<string>( GetKey( BEASTIARY_SEEN_COOKIE_KEY ), "[]" );
		try
		{
			var seenList = JsonSerializer.Deserialize<List<string>>( seenJson ) ?? new();
			SeenSpecies = new HashSet<string>( seenList );
		}
		catch
		{
			SeenSpecies = new HashSet<string>();
		}

		Log.Info( $"Loaded {DiscoveredSpecies.Count} discovered, {SeenSpecies.Count} seen species" );
	}

	private void SaveDiscoveries()
	{
		var json = JsonSerializer.Serialize( DiscoveredSpecies.ToList() );
		Game.Cookies.Set( GetKey( BEASTIARY_COOKIE_KEY ), json );
	}

	private void SaveSeen()
	{
		var json = JsonSerializer.Serialize( SeenSpecies.ToList() );
		Game.Cookies.Set( GetKey( BEASTIARY_SEEN_COOKIE_KEY ), json );
	}

	/// <summary>
	/// Reload data from the current save slot
	/// </summary>
	public void ReloadFromSlot()
	{
		LoadDiscoveries();
		Log.Info( $"BeastiaryManager reloaded from slot {SaveSlotManager.Instance?.ActiveSlot}" );
	}

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
}
