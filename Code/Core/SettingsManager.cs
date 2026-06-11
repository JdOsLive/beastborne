using System;
using System.Collections.Generic;
using Sandbox;
using Beastborne.Data;
using Beastborne.Systems;

namespace Beastborne.Core;

/// <summary>
/// Manages game settings - loading, saving, and providing access to current settings.
/// Settings are global and persist across all save slots.
/// </summary>
public sealed class SettingsManager : Component
{
	public static SettingsManager Instance { get; private set; }

	// Cookie keys - Settings are global (not per-slot)
	private const string SETTINGS_PREFIX = "global-";

	/// <summary>
	/// Get the full key with global prefix (settings are shared across all save slots)
	/// </summary>
	private static string GetKey( string key ) => $"{SETTINGS_PREFIX}{key}";

	/// <summary>
	/// The current game settings
	/// </summary>
	public GameSettings Settings { get; private set; } = new();

	// Events
	public Action OnSettingsChanged;

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			GameObject.Tags.Add( "bb-persistent" );
			Log.Info( "SettingsManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnStart()
	{
		LoadSettings();
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "SettingsManager";
		go.Components.Create<SettingsManager>();
	}

	/// <summary>
	/// Load settings from cookies
	/// </summary>
	public void LoadSettings()
	{
		Settings = new GameSettings();

		// Battle settings
		Settings.DefaultAutoContract = Game.Cookies.Get<bool>( GetKey( "settings-auto-contract" ), false );
		Settings.UseAutoContractSpeciesFilter = Game.Cookies.Get<bool>( GetKey( "settings-species-filter-enabled" ), false );
		Settings.AutoContractEnabledSpecies = Game.Cookies.Get<string>( GetKey( "settings-species-filter-list" ), "" );
		Settings.SkipBattleAnimations = Game.Cookies.Get<bool>( GetKey( "settings-skip-animations" ), false );
		Settings.ScreenShake = Game.Cookies.Get<bool>( GetKey( "settings-screen-shake" ), true );

		// Audio settings
		Settings.MasterVolume = Game.Cookies.Get<float>( GetKey( "settings-master-volume" ), 1.0f ).Clamp( 0f, 1f );
		Settings.UIVolume = Game.Cookies.Get<float>( GetKey( "settings-ui-volume" ), 0.5f ).Clamp( 0f, 1f );
		Settings.MusicVolume = Game.Cookies.Get<float>( GetKey( "settings-music-volume" ), 0.3f ).Clamp( 0f, 1f );

		// Display settings
		Settings.ShowDamageNumbers = Game.Cookies.Get<bool>( GetKey( "settings-damage-numbers" ), true );
		Settings.ShowTypeEffectiveness = Game.Cookies.Get<bool>( GetKey( "settings-type-effectiveness" ), true );
		Settings.ShowGeneticsOnCards = Game.Cookies.Get<bool>( GetKey( "settings-genetics-on-cards" ), false );
		Settings.ShowPowerRatings = Game.Cookies.Get<bool>( GetKey( "settings-power-ratings" ), true );

		// Notification settings
		Settings.ContractWarningThreshold = Game.Cookies.Get<int>( GetKey( "settings-contract-warning" ), 5 );
		Settings.ShowLevelUpNotifications = Game.Cookies.Get<bool>( GetKey( "settings-levelup-notify" ), true );
		Settings.ShowDiscoveryAlerts = Game.Cookies.Get<bool>( GetKey( "settings-discovery-alerts" ), true );

		// Confirmation settings
		Settings.ConfirmBeforeRelease = Game.Cookies.Get<bool>( GetKey( "settings-confirm-release" ), true );
		Settings.ConfirmBeforeFusion = Game.Cookies.Get<bool>( GetKey( "settings-confirm-fusion" ), true );
		Settings.ConfirmPurchaseThreshold = Game.Cookies.Get<int>( GetKey( "settings-confirm-purchase" ), 1000 );

		// Accessibility settings
		Settings.LargerTextMode = Game.Cookies.Get<bool>( GetKey( "settings-larger-text" ), false );
		Settings.HighContrastMode = Game.Cookies.Get<bool>( GetKey( "settings-high-contrast" ), false );

		// Hydrate the live audio systems from the persisted values — without
		// this, SoundManager/RadioManager boot on their compiled-in defaults
		// and the saved volumes only "exist" in the Options panel.
		ApplyAudioSettings();

		Log.Info( "SettingsManager loaded settings" );
	}

	/// <summary>
	/// Push the persisted audio values into the live audio systems
	/// (SoundManager statics + RadioManager music volume). Called on
	/// settings load/reset; per-setting Set* methods apply individually.
	/// </summary>
	public void ApplyAudioSettings()
	{
		SoundManager.MasterVolume = Settings.MasterVolume;
		SoundManager.UIVolume = Settings.UIVolume;
		RadioManager.Volume = Settings.MusicVolume.Clamp( 0f, 1f );
		RadioManager.UpdateVolume(); // re-level any currently playing track
	}

	/// <summary>
	/// Save current settings to cookies
	/// </summary>
	public void SaveSettings()
	{
		// Battle settings
		Game.Cookies.Set( GetKey( "settings-auto-contract" ), Settings.DefaultAutoContract );
		Game.Cookies.Set( GetKey( "settings-species-filter-enabled" ), Settings.UseAutoContractSpeciesFilter );
		Game.Cookies.Set( GetKey( "settings-species-filter-list" ), Settings.AutoContractEnabledSpecies );
		Game.Cookies.Set( GetKey( "settings-skip-animations" ), Settings.SkipBattleAnimations );
		Game.Cookies.Set( GetKey( "settings-screen-shake" ), Settings.ScreenShake );

		// Audio settings
		Game.Cookies.Set( GetKey( "settings-master-volume" ), Settings.MasterVolume );
		Game.Cookies.Set( GetKey( "settings-ui-volume" ), Settings.UIVolume );
		Game.Cookies.Set( GetKey( "settings-music-volume" ), Settings.MusicVolume );

		// Display settings
		Game.Cookies.Set( GetKey( "settings-damage-numbers" ), Settings.ShowDamageNumbers );
		Game.Cookies.Set( GetKey( "settings-type-effectiveness" ), Settings.ShowTypeEffectiveness );
		Game.Cookies.Set( GetKey( "settings-genetics-on-cards" ), Settings.ShowGeneticsOnCards );
		Game.Cookies.Set( GetKey( "settings-power-ratings" ), Settings.ShowPowerRatings );

		// Notification settings
		Game.Cookies.Set( GetKey( "settings-contract-warning" ), Settings.ContractWarningThreshold );
		Game.Cookies.Set( GetKey( "settings-levelup-notify" ), Settings.ShowLevelUpNotifications );
		Game.Cookies.Set( GetKey( "settings-discovery-alerts" ), Settings.ShowDiscoveryAlerts );

		// Confirmation settings
		Game.Cookies.Set( GetKey( "settings-confirm-release" ), Settings.ConfirmBeforeRelease );
		Game.Cookies.Set( GetKey( "settings-confirm-fusion" ), Settings.ConfirmBeforeFusion );
		Game.Cookies.Set( GetKey( "settings-confirm-purchase" ), Settings.ConfirmPurchaseThreshold );

		// Accessibility settings
		Game.Cookies.Set( GetKey( "settings-larger-text" ), Settings.LargerTextMode );
		Game.Cookies.Set( GetKey( "settings-high-contrast" ), Settings.HighContrastMode );

		Log.Info( "SettingsManager saved settings" );
		OnSettingsChanged?.Invoke();
	}

	/// <summary>
	/// Reload settings from cookies (settings are global, not per-slot)
	/// </summary>
	public void ReloadSettings()
	{
		LoadSettings();
		Log.Info( "SettingsManager reloaded global settings" );
	}

	/// <summary>
	/// Reset all settings to defaults
	/// </summary>
	public void ResetToDefaults()
	{
		Settings = new GameSettings();
		SaveSettings();
		ApplyAudioSettings(); // volumes snap back to defaults audibly, not just on paper
		Log.Info( "SettingsManager reset to defaults" );
	}

	// ============================================
	// CONVENIENCE METHODS
	// ============================================

	/// <summary>
	/// Update a single setting and save
	/// </summary>
	public void SetAutoContract( bool enabled )
	{
		Settings.DefaultAutoContract = enabled;
		SaveSettings();
	}

	public void SetUseSpeciesFilter( bool enabled )
	{
		Settings.UseAutoContractSpeciesFilter = enabled;
		SaveSettings();
	}

	public void SetSpeciesFilterList( HashSet<string> enabledSpecies )
	{
		Settings.AutoContractEnabledSpecies = string.Join( ",", enabledSpecies );
		SaveSettings();
	}

	public HashSet<string> GetSpeciesFilterList()
	{
		if ( string.IsNullOrEmpty( Settings.AutoContractEnabledSpecies ) )
			return new HashSet<string>();
		return new HashSet<string>( Settings.AutoContractEnabledSpecies.Split( ',', StringSplitOptions.RemoveEmptyEntries ) );
	}

	public void ToggleSpeciesInFilter( string speciesId )
	{
		var filter = GetSpeciesFilterList();
		if ( filter.Contains( speciesId ) )
			filter.Remove( speciesId );
		else
			filter.Add( speciesId );
		SetSpeciesFilterList( filter );
	}

	public bool IsSpeciesEnabledForAutoContract( string speciesId )
	{
		if ( !Settings.UseAutoContractSpeciesFilter )
			return true; // Filter disabled = all species enabled
		return GetSpeciesFilterList().Contains( speciesId );
	}

	public void SetSkipAnimations( bool skip )
	{
		Settings.SkipBattleAnimations = skip;
		SaveSettings();
	}

	public void SetScreenShake( bool enabled )
	{
		Settings.ScreenShake = enabled;
		SaveSettings();
	}

	// ── Audio — each setter persists AND applies to the live system ──

	public void SetMasterVolume( float volume )
	{
		Settings.MasterVolume = volume.Clamp( 0f, 1f );
		SaveSettings();
		SoundManager.MasterVolume = Settings.MasterVolume;
		RadioManager.UpdateVolume(); // music handle = MusicVolume * MasterVolume
	}

	public void SetUIVolume( float volume )
	{
		Settings.UIVolume = volume.Clamp( 0f, 1f );
		SaveSettings();
		SoundManager.UIVolume = Settings.UIVolume;
	}

	public void SetMusicVolume( float volume )
	{
		Settings.MusicVolume = volume.Clamp( 0f, 1f );
		SaveSettings();
		// Applies to the playing track; its persistence callback no-ops
		// because Settings.MusicVolume already matches.
		RadioManager.SetVolume( Settings.MusicVolume );
	}

	/// <summary>
	/// Persistence-only sync for music volume — called by RadioManager.SetVolume
	/// so the RadioWidget's drag slider also persists. Writes the single cookie
	/// (no full SaveSettings, no OnSettingsChanged) because drags fire per-frame.
	/// </summary>
	public void NotifyMusicVolumeChanged( float volume )
	{
		volume = volume.Clamp( 0f, 1f );
		if ( Math.Abs( Settings.MusicVolume - volume ) < 0.0001f ) return;
		Settings.MusicVolume = volume;
		Game.Cookies.Set( GetKey( "settings-music-volume" ), volume );
	}

	public void SetShowDamageNumbers( bool show )
	{
		Settings.ShowDamageNumbers = show;
		SaveSettings();
	}

	public void SetShowTypeEffectiveness( bool show )
	{
		Settings.ShowTypeEffectiveness = show;
		SaveSettings();
	}

	public void SetShowGeneticsOnCards( bool show )
	{
		Settings.ShowGeneticsOnCards = show;
		SaveSettings();
	}

	public void SetShowPowerRatings( bool show )
	{
		Settings.ShowPowerRatings = show;
		SaveSettings();
	}

	public void SetContractWarningThreshold( int threshold )
	{
		Settings.ContractWarningThreshold = threshold;
		SaveSettings();
	}

	public void SetShowLevelUpNotifications( bool show )
	{
		Settings.ShowLevelUpNotifications = show;
		SaveSettings();
	}

	public void SetShowDiscoveryAlerts( bool show )
	{
		Settings.ShowDiscoveryAlerts = show;
		SaveSettings();
	}

	public void SetConfirmBeforeRelease( bool confirm )
	{
		Settings.ConfirmBeforeRelease = confirm;
		SaveSettings();
	}

	public void SetConfirmBeforeFusion( bool confirm )
	{
		Settings.ConfirmBeforeFusion = confirm;
		SaveSettings();
	}

	public void SetConfirmPurchaseThreshold( int threshold )
	{
		Settings.ConfirmPurchaseThreshold = threshold;
		SaveSettings();
	}

	public void SetLargerTextMode( bool enabled )
	{
		Settings.LargerTextMode = enabled;
		SaveSettings();
	}

	public void SetHighContrastMode( bool enabled )
	{
		Settings.HighContrastMode = enabled;
		SaveSettings();
	}
}
