using System;
using System.Collections.Generic;
using System.Text.Json;
using Sandbox;

namespace Beastborne.Systems;

/// <summary>
/// Manages language localization for the game.
/// Loads translations from JSON files and provides lookup methods.
/// Works statically — no Component instance required.
/// </summary>
public static class LocalizationManager
{
	/// <summary>
	/// Currently active language code ("en", "fr", etc.)
	/// </summary>
	public static string CurrentLanguage { get; private set; } = "en";

	/// <summary>
	/// Fires when language is changed so UI can refresh
	/// </summary>
	public static Action OnLanguageChanged;

	// Cached translations for current language
	private static Dictionary<string, string> _translations = new();

	// English fallback (always loaded)
	private static Dictionary<string, string> _fallback = new();

	private static bool _initialized = false;

	private static readonly string[] SupportedLanguages = { "en", "fr" };

	/// <summary>
	/// Ensure translations are loaded. Called lazily on first Get().
	/// </summary>
	private static void EnsureInitialized()
	{
		if ( _initialized ) return;
		_initialized = true;

		// Load English as fallback
		_fallback = LoadLanguageFile( "en" );
		_translations = _fallback;

		// Load saved language preference
		try
		{
			var savedLang = Cookie.Get( "beastborne.language", "en" );
			if ( savedLang != "en" )
			{
				CurrentLanguage = savedLang;
				var loaded = LoadLanguageFile( savedLang );
				_translations = loaded.Count > 0 ? loaded : _fallback;
			}
		}
		catch
		{
			// Cookies might not be available yet
		}

		Log.Info( $"LocalizationManager initialized. Language: {CurrentLanguage}, Keys: {_translations.Count}" );
	}

	/// <summary>
	/// Get a translated string by key. Falls back to English if not found.
	/// </summary>
	public static string Get( string key )
	{
		EnsureInitialized();

		if ( string.IsNullOrEmpty( key ) ) return "";

		// Try current language
		if ( _translations.TryGetValue( key, out var value ) )
			return value;

		// Fall back to English
		if ( _fallback.TryGetValue( key, out var fallbackValue ) )
			return fallbackValue;

		// Key not found — return the key itself for debugging
		return key;
	}

	/// <summary>
	/// Get a translated string with format arguments.
	/// Uses {0}, {1}, etc. placeholders in translation strings.
	/// </summary>
	public static string Get( string key, params object[] args )
	{
		var template = Get( key );
		try
		{
			return string.Format( template, args );
		}
		catch
		{
			return template;
		}
	}

	/// <summary>
	/// Switch to a different language
	/// </summary>
	public static void SetLanguage( string langCode, bool notify = true )
	{
		EnsureInitialized();

		if ( string.IsNullOrEmpty( langCode ) ) return;
		langCode = langCode.ToLower();

		if ( CurrentLanguage == langCode ) return;

		CurrentLanguage = langCode;
		Cookie.Set( "beastborne.language", langCode );

		if ( langCode == "en" )
		{
			_translations = _fallback;
		}
		else
		{
			var loaded = LoadLanguageFile( langCode );
			_translations = loaded.Count > 0 ? loaded : _fallback;
		}

		Log.Info( $"Language changed to: {langCode} ({_translations.Count} keys)" );

		if ( notify )
			OnLanguageChanged?.Invoke();
	}

	/// <summary>
	/// Get list of supported languages
	/// </summary>
	public static string[] GetSupportedLanguages() => SupportedLanguages;

	/// <summary>
	/// Get display name for a language code
	/// </summary>
	public static string GetLanguageDisplayName( string langCode ) => langCode switch
	{
		"en" => "English",
		"fr" => "Français",
		_ => langCode
	};

	/// <summary>
	/// Load a language JSON file from Assets/localization/
	/// </summary>
	private static Dictionary<string, string> LoadLanguageFile( string langCode )
	{
		// Try multiple file systems and paths
		string[] paths = new[]
		{
			$"localization/{langCode}.json",
			$"Assets/localization/{langCode}.json",
			$"/localization/{langCode}.json"
		};

		// Try FileSystem.Mounted first
		foreach ( var path in paths )
		{
			try
			{
				if ( FileSystem.Mounted.FileExists( path ) )
				{
					var content = FileSystem.Mounted.ReadAllText( path );
					if ( !string.IsNullOrEmpty( content ) )
					{
						var dict = JsonSerializer.Deserialize<Dictionary<string, string>>( content,
							new JsonSerializerOptions { PropertyNameCaseInsensitive = true } );
						if ( dict != null && dict.Count > 0 )
						{
							Log.Info( $"Loaded language file from Mounted: {path} ({dict.Count} keys)" );
							return dict;
						}
					}
				}
			}
			catch ( Exception e )
			{
				Log.Info( $"Mounted read failed for {path}: {e.Message}" );
			}
		}

		// Try FileSystem.Data
		foreach ( var path in paths )
		{
			try
			{
				if ( FileSystem.Data.FileExists( path ) )
				{
					var content = FileSystem.Data.ReadAllText( path );
					if ( !string.IsNullOrEmpty( content ) )
					{
						var dict = JsonSerializer.Deserialize<Dictionary<string, string>>( content,
							new JsonSerializerOptions { PropertyNameCaseInsensitive = true } );
						if ( dict != null && dict.Count > 0 )
						{
							Log.Info( $"Loaded language file from Data: {path} ({dict.Count} keys)" );
							return dict;
						}
					}
				}
			}
			catch ( Exception e )
			{
				Log.Info( $"Data read failed for {path}: {e.Message}" );
			}
		}

		// Try direct file system as last resort
		try
		{
			var projectPath = $"Assets/localization/{langCode}.json";
			if ( System.IO.File.Exists( projectPath ) )
			{
				var content = System.IO.File.ReadAllText( projectPath );
				if ( !string.IsNullOrEmpty( content ) )
				{
					var dict = JsonSerializer.Deserialize<Dictionary<string, string>>( content,
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true } );
					if ( dict != null && dict.Count > 0 )
					{
						Log.Info( $"Loaded language file from IO: {projectPath} ({dict.Count} keys)" );
						return dict;
					}
				}
			}
		}
		catch ( Exception e )
		{
			Log.Info( $"IO read failed: {e.Message}" );
		}

		Log.Warning( $"Could not load language file for '{langCode}' from any source. Tried Mounted, Data, and IO." );
		return new Dictionary<string, string>();
	}
}
