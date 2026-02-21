using Sandbox;
using Sandbox.Audio;
using System;
using System.Collections.Generic;

namespace Beastborne.Systems;

/// <summary>
/// Game context for automatic station switching.
/// </summary>
public enum RadioContext
{
	Menu,       // Browsing menus (monsters, skills, beastiary, shop)
	Expedition, // On an expedition (exploring, between battles)
	Battle      // In active battle (expedition fights, arena)
}

/// <summary>
/// Manages the in-game radio system with multiple stations and tracks.
/// Automatically switches stations based on game context.
/// </summary>
public static class RadioManager
{
	/// <summary>
	/// A single music track.
	/// </summary>
	public class Track
	{
		public string Name { get; set; }
		public string FilePath { get; set; }
	}

	/// <summary>
	/// A radio station containing multiple tracks.
	/// </summary>
	public class Station
	{
		public string Name { get; set; }
		public string Icon { get; set; }
		public RadioContext Context { get; set; }
		public List<Track> Tracks { get; set; } = new();
	}

	// All available stations
	public static List<Station> Stations { get; private set; } = new();

	// Current state
	public static int CurrentStationIndex { get; private set; } = 0;
	public static int CurrentTrackIndex { get; private set; } = 0;
	public static bool IsPlaying { get; private set; } = false;
	public static float Volume { get; set; } = 0.3f;
	public static bool IsShuffle { get; set; } = false;
	public static bool IsRepeat { get; set; } = false;
	public static RadioContext CurrentContext { get; private set; } = RadioContext.Menu;

	// Current music handle
	private static SoundHandle _currentMusic;
	private static bool _initialized = false;
	private static bool _userPaused = false; // Track if user manually paused - persists across context switches

	// Track progress using SoundHandle.Time and SoundFile.Duration
	private static float _trackDuration = 0f;
	public static float TrackPosition => _currentMusic.IsValid() ? _currentMusic.Time : 0f;
	public static float TrackDuration => _trackDuration;

	// Events
	public static Action OnTrackChanged;
	public static Action OnStationChanged;
	public static Action OnContextChanged;

	public static Station CurrentStation => Stations.Count > 0 && CurrentStationIndex < Stations.Count ? Stations[CurrentStationIndex] : null;
	public static Track CurrentTrack => CurrentStation?.Tracks.Count > 0 && CurrentTrackIndex < CurrentStation.Tracks.Count ? CurrentStation.Tracks[CurrentTrackIndex] : null;

	/// <summary>
	/// Initialize the radio system. Call this once on game start.
	/// </summary>
	public static void Initialize()
	{
		if ( _initialized ) return;

		InitializeStations();
		_initialized = true;

		// Start playing Chill FM (Menu context) with a random track
		if ( Stations.Count > 0 )
		{
			// Find the Menu context station (Chill FM)
			var menuStation = Stations.Find( s => s.Context == RadioContext.Menu );
			if ( menuStation != null )
			{
				CurrentStationIndex = Stations.IndexOf( menuStation );
			}
			else
			{
				CurrentStationIndex = 0;
			}

			if ( CurrentStation?.Tracks.Count > 0 )
			{
				CurrentTrackIndex = Random.Shared.Next( CurrentStation.Tracks.Count );
			}
			Play();
		}
	}

	/// <summary>
	/// Initialize all radio stations and their tracks.
	/// Add your tracks here!
	/// </summary>
	private static void InitializeStations()
	{
		Stations.Clear();

		// Chill Station - relaxed, cozy tracks for menu browsing
		Stations.Add( new Station
		{
			Name = "Chill FM",
			Icon = "🎵",
			Context = RadioContext.Menu,
			Tracks = new List<Track>
			{
				new Track { Name = "Cozy Hearth", FilePath = "music/chillfm/cozy_hearth.sound" },
				new Track { Name = "Evening Glow", FilePath = "music/chillfm/evening_glow.sound" },
				new Track { Name = "Fireside Tales", FilePath = "music/chillfm/fireside_tales.sound" },
				new Track { Name = "Tamer's Rest", FilePath = "music/chillfm/tamers_rest.sound" },
				new Track { Name = "Moonlight Meadows", FilePath = "music/chillfm/Moonlight_Meadows.sound" },
			}
		} );

		// Battle Station - high energy tracks for combat
		Stations.Add( new Station
		{
			Name = "Battle FM",
			Icon = "⚔",
			Context = RadioContext.Battle,
			Tracks = new List<Track>
			{
				new Track { Name = "Clash of Bonds", FilePath = "music/battlefm/Clash_of_Bonds.sound" },
				new Track { Name = "Heart of the Storm", FilePath = "music/battlefm/Heart_of_the_Storm.sound" },
				new Track { Name = "Steel and Spirit", FilePath = "music/battlefm/Steel_and_Spirit.sound" },
				new Track { Name = "Unbroken Will", FilePath = "music/battlefm/Unbroken_Will.sound" },
				new Track { Name = "Fangs and Fury", FilePath = "music/battlefm/Fangs_and_Fury.sound" },
			}
		} );

		// Adventure Station - exploration vibes for expeditions
		Stations.Add( new Station
		{
			Name = "Adventure Wave",
			Icon = "🌍",
			Context = RadioContext.Expedition,
			Tracks = new List<Track>
			{
				// Add your adventure/expedition tracks here
				// new Track { Name = "Open Road", FilePath = "sounds/music/adventure/open_road.sound" },
			}
		} );

		// Remove empty stations for now
		Stations.RemoveAll( s => s.Tracks.Count == 0 );
	}

	/// <summary>
	/// Set the current game context. Automatically switches to appropriate station.
	/// Respects user pause state - if user paused music, it stays paused across context switches.
	/// </summary>
	public static void SetContext( RadioContext context )
	{
		if ( CurrentContext == context ) return;

		CurrentContext = context;
		Log.Info( $"RadioManager: Context changed to {context}" );

		// Find station for this context
		var station = Stations.Find( s => s.Context == context );
		if ( station != null )
		{
			int newIndex = Stations.IndexOf( station );
			if ( newIndex != CurrentStationIndex )
			{
				CurrentStationIndex = newIndex;
				CurrentTrackIndex = station.Tracks.Count > 0 ? Random.Shared.Next( station.Tracks.Count ) : 0;

				// Only auto-play if user hasn't manually paused
				if ( !_userPaused )
				{
					Play();
				}
				else
				{
					// Stop current music but keep paused state
					if ( _currentMusic.IsValid() )
					{
						_currentMusic.Stop();
					}
					IsPlaying = false;
				}

				OnStationChanged?.Invoke();
				OnTrackChanged?.Invoke();
			}
		}

		OnContextChanged?.Invoke();
	}

	/// <summary>
	/// Get the station for a specific context.
	/// </summary>
	public static Station GetStationForContext( RadioContext context )
	{
		return Stations.Find( s => s.Context == context );
	}

	/// <summary>
	/// Start playing the current track.
	/// </summary>
	public static void Play()
	{
		if ( CurrentTrack == null ) return;

		Stop();
		_trackDuration = 0f;

		try
		{
			_currentMusic = Sound.Play( CurrentTrack.FilePath );
			if ( _currentMusic.IsValid() )
			{
				// Route through the Music mixer so s&box audio settings work
				var musicMixer = Mixer.FindMixerByName( "Music" );
				if ( musicMixer != null )
				{
					_currentMusic.TargetMixer = musicMixer;
				}

				_currentMusic.Volume = Volume * SoundManager.MasterVolume;
				IsPlaying = true;
			}
		}
		catch ( Exception e )
		{
			Log.Warning( $"RadioManager: Failed to play track '{CurrentTrack.Name}': {e.Message}" );
		}

		// Try to get track duration separately so it never breaks playback
		try
		{
			var soundFile = SoundFile.Load( CurrentTrack.FilePath );
			if ( soundFile != null )
			{
				_trackDuration = soundFile.Duration;
			}
		}
		catch ( Exception )
		{
			// Duration unavailable — progress bar will just show 0:00
		}
	}

	/// <summary>
	/// Stop the current track.
	/// </summary>
	public static void Stop()
	{
		if ( _currentMusic.IsValid() )
		{
			_currentMusic.Stop();
		}
		IsPlaying = false;
	}

	/// <summary>
	/// Toggle play/pause. Tracks user intent for pause persistence across context switches.
	/// </summary>
	public static void TogglePlay()
	{
		if ( IsPlaying )
		{
			_userPaused = true;
			Stop();
		}
		else
		{
			_userPaused = false;
			Play();
		}
	}

	/// <summary>
	/// Toggle shuffle mode.
	/// </summary>
	public static void ToggleShuffle()
	{
		IsShuffle = !IsShuffle;
	}

	/// <summary>
	/// Toggle repeat mode.
	/// </summary>
	public static void ToggleRepeat()
	{
		IsRepeat = !IsRepeat;
	}

	/// <summary>
	/// Skip to the next track in the current station.
	/// </summary>
	public static void NextTrack()
	{
		if ( CurrentStation == null || CurrentStation.Tracks.Count == 0 ) return;

		if ( IsShuffle )
		{
			int newIndex;
			if ( CurrentStation.Tracks.Count > 1 )
			{
				do { newIndex = Random.Shared.Next( CurrentStation.Tracks.Count ); }
				while ( newIndex == CurrentTrackIndex );
			}
			else
			{
				newIndex = 0;
			}
			CurrentTrackIndex = newIndex;
		}
		else
		{
			CurrentTrackIndex = (CurrentTrackIndex + 1) % CurrentStation.Tracks.Count;
		}

		Play();
		OnTrackChanged?.Invoke();
		SoundManager.PlayForward();
	}

	/// <summary>
	/// Go to the previous track in the current station.
	/// </summary>
	public static void PreviousTrack()
	{
		if ( CurrentStation == null || CurrentStation.Tracks.Count == 0 ) return;

		CurrentTrackIndex--;
		if ( CurrentTrackIndex < 0 )
			CurrentTrackIndex = CurrentStation.Tracks.Count - 1;

		Play();
		OnTrackChanged?.Invoke();
		SoundManager.PlayBack();
	}

	/// <summary>
	/// Switch to the next station.
	/// </summary>
	public static void NextStation()
	{
		if ( Stations.Count == 0 ) return;

		CurrentStationIndex = (CurrentStationIndex + 1) % Stations.Count;
		CurrentTrackIndex = 0;

		if ( CurrentStation?.Tracks.Count > 0 )
		{
			CurrentTrackIndex = Random.Shared.Next( CurrentStation.Tracks.Count );
		}

		Play();
		OnStationChanged?.Invoke();
		OnTrackChanged?.Invoke();
		SoundManager.PlayTabSwitch();
	}

	/// <summary>
	/// Switch to the previous station.
	/// </summary>
	public static void PreviousStation()
	{
		if ( Stations.Count == 0 ) return;

		CurrentStationIndex--;
		if ( CurrentStationIndex < 0 )
			CurrentStationIndex = Stations.Count - 1;

		CurrentTrackIndex = 0;

		if ( CurrentStation?.Tracks.Count > 0 )
		{
			CurrentTrackIndex = Random.Shared.Next( CurrentStation.Tracks.Count );
		}

		Play();
		OnStationChanged?.Invoke();
		OnTrackChanged?.Invoke();
		SoundManager.PlayTabSwitch();
	}

	/// <summary>
	/// Set the station directly by index.
	/// </summary>
	public static void SetStation( int index )
	{
		if ( index < 0 || index >= Stations.Count ) return;
		if ( index == CurrentStationIndex ) return;

		CurrentStationIndex = index;
		CurrentTrackIndex = 0;

		if ( CurrentStation?.Tracks.Count > 0 )
		{
			CurrentTrackIndex = Random.Shared.Next( CurrentStation.Tracks.Count );
		}

		Play();
		OnStationChanged?.Invoke();
		OnTrackChanged?.Invoke();
	}

	/// <summary>
	/// Set the track directly by index within current station.
	/// </summary>
	public static void SetTrack( int index )
	{
		if ( CurrentStation == null ) return;
		if ( index < 0 || index >= CurrentStation.Tracks.Count ) return;

		CurrentTrackIndex = index;
		Play();
		OnTrackChanged?.Invoke();
	}

	/// <summary>
	/// Set the music volume.
	/// </summary>
	public static void SetVolume( float volume )
	{
		Volume = volume.Clamp( 0f, 1f );
		if ( _currentMusic.IsValid() )
		{
			_currentMusic.Volume = Volume * SoundManager.MasterVolume;
		}
	}

	/// <summary>
	/// Update the current music volume (call when master volume changes).
	/// </summary>
	public static void UpdateVolume()
	{
		if ( _currentMusic.IsValid() )
		{
			_currentMusic.Volume = Volume * SoundManager.MasterVolume;
		}
	}

	/// <summary>
	/// Add a track to a station dynamically.
	/// </summary>
	public static void AddTrack( string stationName, string trackName, string filePath )
	{
		var station = Stations.Find( s => s.Name == stationName );
		if ( station == null )
		{
			Log.Warning( $"RadioManager: Station '{stationName}' not found" );
			return;
		}

		station.Tracks.Add( new Track { Name = trackName, FilePath = filePath } );
		Log.Info( $"RadioManager: Added track '{trackName}' to station '{stationName}'" );
	}

	/// <summary>
	/// Add a new station dynamically.
	/// </summary>
	public static void AddStation( string name, string icon )
	{
		if ( Stations.Exists( s => s.Name == name ) )
		{
			Log.Warning( $"RadioManager: Station '{name}' already exists" );
			return;
		}

		Stations.Add( new Station { Name = name, Icon = icon, Tracks = new List<Track>() } );
		Log.Info( $"RadioManager: Added station '{name}'" );
	}

	/// <summary>
	/// Check for hotkey input. Call this from OnUpdate.
	/// </summary>
	public static void CheckInput()
	{
		if ( Input.Pressed( "RadioNext" ) )
		{
			NextTrack();
		}
		else if ( Input.Pressed( "RadioPrev" ) )
		{
			PreviousTrack();
		}
	}

	/// <summary>
	/// Tick the radio system. Call this from OnUpdate to handle auto-advance.
	/// </summary>
	public static void Tick()
	{
		// Check if we're supposed to be playing but the sound has ended
		if ( IsPlaying && !_currentMusic.IsValid() )
		{
			// Sound ended, advance to next track
			AdvanceToNextTrack();
		}

		// Keep volume in sync with master volume
		if ( _currentMusic.IsValid() )
		{
			_currentMusic.Volume = Volume * SoundManager.MasterVolume;
		}
	}

	/// <summary>
	/// Advance to the next track without playing UI sounds.
	/// Used for auto-advance when a song ends.
	/// </summary>
	private static void AdvanceToNextTrack()
	{
		if ( CurrentStation == null || CurrentStation.Tracks.Count == 0 ) return;

		if ( IsRepeat )
		{
			// Repeat: replay current track
			Play();
			return;
		}

		if ( IsShuffle )
		{
			int newIndex;
			if ( CurrentStation.Tracks.Count > 1 )
			{
				do { newIndex = Random.Shared.Next( CurrentStation.Tracks.Count ); }
				while ( newIndex == CurrentTrackIndex );
			}
			else
			{
				newIndex = 0;
			}
			CurrentTrackIndex = newIndex;
		}
		else
		{
			CurrentTrackIndex = (CurrentTrackIndex + 1) % CurrentStation.Tracks.Count;
		}

		Play();
		OnTrackChanged?.Invoke();
	}
}
