using Sandbox;
using System.Text.Json;

namespace Beastborne.Core;

/// <summary>
/// Manages save slots for multiple characters
/// </summary>
public sealed class SaveSlotManager : Component
{
	public static SaveSlotManager Instance { get; private set; }

	private const string ACTIVE_SLOT_KEY = "active-save-slot";
	private const string SLOT_INFO_KEY = "slot-info-";
	private const int MAX_SLOTS = 3;

	public int ActiveSlot { get; private set; } = 0;
	public SaveSlotInfo[] Slots { get; private set; } = new SaveSlotInfo[MAX_SLOTS];

	public Action OnSlotChanged;

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			LoadSlotInfo();
			Log.Info( "SaveSlotManager initialized" );
		}
		else
		{
			Destroy();
		}
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "SaveSlotManager";
		go.Components.Create<SaveSlotManager>();
	}

	/// <summary>
	/// Get the cookie key prefix for the active slot
	/// </summary>
	public static string GetSlotPrefix()
	{
		int slot = Instance?.ActiveSlot ?? 0;
		return $"slot{slot}-";
	}

	/// <summary>
	/// Load all slot info from cookies
	/// </summary>
	private void LoadSlotInfo()
	{
		ActiveSlot = Game.Cookies.Get<int>( ACTIVE_SLOT_KEY, 0 );

		for ( int i = 0; i < MAX_SLOTS; i++ )
		{
			var json = Game.Cookies.Get<string>( $"{SLOT_INFO_KEY}{i}", "" );
			if ( !string.IsNullOrEmpty( json ) )
			{
				try
				{
					Slots[i] = JsonSerializer.Deserialize<SaveSlotInfo>( json );
				}
				catch
				{
					Slots[i] = null;
				}
			}
			else
			{
				Slots[i] = null;
			}
		}

		Log.Info( $"Loaded save slots. Active slot: {ActiveSlot}" );
	}

	/// <summary>
	/// Save slot info to cookies
	/// </summary>
	public void SaveSlotInfo( int slot )
	{
		if ( slot < 0 || slot >= MAX_SLOTS ) return;

		if ( Slots[slot] != null )
		{
			var json = JsonSerializer.Serialize( Slots[slot] );
			Game.Cookies.Set( $"{SLOT_INFO_KEY}{slot}", json );
		}
		else
		{
			Game.Cookies.Set( $"{SLOT_INFO_KEY}{slot}", "" );
		}
	}

	/// <summary>
	/// Update the active slot's info with current data
	/// Only saves if player has actually started (has at least one monster)
	/// </summary>
	public void UpdateActiveSlotInfo()
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		var monsterCount = MonsterManager.Instance?.OwnedMonsters?.Count ?? 0;

		if ( tamer == null ) return;

		// Don't create slot info for players who haven't actually started yet
		// (haven't picked a starter monster)
		if ( monsterCount == 0 && Slots[ActiveSlot] == null )
		{
			return;
		}

		Slots[ActiveSlot] = new SaveSlotInfo
		{
			TamerName = tamer.Name,
			TamerLevel = tamer.Level,
			Gender = tamer.Gender,
			MonsterCount = monsterCount,
			HighestExpedition = tamer.HighestExpeditionCleared,
			LastPlayed = DateTime.UtcNow
		};

		SaveSlotInfo( ActiveSlot );
	}

	/// <summary>
	/// Check if a slot has save data
	/// </summary>
	public bool HasSaveData( int slot )
	{
		if ( slot < 0 || slot >= MAX_SLOTS ) return false;
		return Slots[slot] != null;
	}

	/// <summary>
	/// Switch to a different save slot (requires game restart/reload)
	/// </summary>
	public void SwitchSlot( int slot )
	{
		if ( slot < 0 || slot >= MAX_SLOTS ) return;
		if ( slot == ActiveSlot ) return;

		// Save current slot info before switching
		UpdateActiveSlotInfo();

		ActiveSlot = slot;
		Game.Cookies.Set( ACTIVE_SLOT_KEY, slot );

		// Reload all managers to pick up the new slot's data
		ReloadManagers();

		Log.Info( $"Switched to save slot {slot}" );
		OnSlotChanged?.Invoke();
	}

	/// <summary>
	/// Reset a save slot (delete all data for that slot)
	/// </summary>
	public void ResetSlot( int slot )
	{
		if ( slot < 0 || slot >= MAX_SLOTS ) return;

		string prefix = $"slot{slot}-";

		// Clear all known keys for this slot
		ClearSlotCookies( prefix );

		// Clear slot info
		Slots[slot] = null;
		Game.Cookies.Set( $"{SLOT_INFO_KEY}{slot}", "" );

		Log.Info( $"Reset save slot {slot}" );

		// If resetting active slot, reload managers
		if ( slot == ActiveSlot )
		{
			ReloadManagers();
		}
	}

	/// <summary>
	/// Clear all cookies for a slot prefix
	/// </summary>
	private void ClearSlotCookies( string prefix )
	{
		// Clear tamer data
		Game.Cookies.Set( $"{prefix}tamer-level", 1 ); // Starting level
		Game.Cookies.Set( $"{prefix}tamer-xp", 0 );
		Game.Cookies.Set( $"{prefix}tamer-gold", 100 ); // Starting gold amount
		Game.Cookies.Set( $"{prefix}tamer-gems", 0 );
		Game.Cookies.Set( $"{prefix}tamer-ink", 10 ); // Starting ink amount
		Game.Cookies.Set( $"{prefix}tamer-skill-points", 1 ); // Starting skill point
		Game.Cookies.Set( $"{prefix}tamer-expedition-cleared", 0 );
		Game.Cookies.Set( $"{prefix}tamer-arena-rank", "" );
		Game.Cookies.Set( $"{prefix}tamer-arena-points", 0 );
		Game.Cookies.Set( $"{prefix}tamer-battles-won", 0 );
		Game.Cookies.Set( $"{prefix}tamer-battles-lost", 0 );
		Game.Cookies.Set( $"{prefix}tamer-arena-wins", 0 );
		Game.Cookies.Set( $"{prefix}tamer-arena-losses", 0 );
		Game.Cookies.Set( $"{prefix}tamer-caught", 0 );
		Game.Cookies.Set( $"{prefix}tamer-bred", 0 );
		Game.Cookies.Set( $"{prefix}tamer-evolved", 0 );
		Game.Cookies.Set( $"{prefix}tamer-skills", "[]" );
		Game.Cookies.Set( $"{prefix}tamer-gender", "" );

		// Clear monster data (use empty array, not empty string, for proper JSON parsing)
		Game.Cookies.Set( $"{prefix}monsters-data", "[]" );
		// Remove max-monsters cookie so it uses the default (50) when loaded
		Game.Cookies.Set( $"{prefix}max-monsters", 50 );

		// Clear beastiary data (note: spelled "beastiary" to match BeastiaryManager)
		Game.Cookies.Set( $"{prefix}beastiary-discovered", "[]" );
		Game.Cookies.Set( $"{prefix}beastiary-seen", "[]" );

		// Clear tutorial data
		Game.Cookies.Set( $"{prefix}tutorial-completed", false );
		Game.Cookies.Set( $"{prefix}tutorial-skipped", false );
		Game.Cookies.Set( $"{prefix}tutorial-step", 0 );

		// Note: Settings are intentionally NOT cleared - they are global preferences
		// that persist across all save slots
	}

	/// <summary>
	/// Reload all managers to pick up the new slot data
	/// </summary>
	private void ReloadManagers()
	{
		// Force TamerManager to reload
		if ( TamerManager.Instance != null )
		{
			TamerManager.Instance.ReloadFromSlot();
		}

		// Force MonsterManager to reload
		if ( MonsterManager.Instance != null )
		{
			MonsterManager.Instance.ReloadFromSlot();
		}

		// Force BeastiaryManager to reload
		if ( BeastiaryManager.Instance != null )
		{
			BeastiaryManager.Instance.ReloadFromSlot();
		}

		// Force TutorialManager to reload
		if ( TutorialManager.Instance != null )
		{
			TutorialManager.Instance.ReloadFromSlot();
		}

		// Note: SettingsManager is NOT reloaded - settings are global preferences
		// that persist across all save slots
	}
}

/// <summary>
/// Info displayed on save slot selection screen
/// </summary>
public class SaveSlotInfo
{
	public string TamerName { get; set; }
	public int TamerLevel { get; set; }
	public Data.TamerGender Gender { get; set; }
	public int MonsterCount { get; set; }
	public int HighestExpedition { get; set; }
	public DateTime LastPlayed { get; set; }
}
