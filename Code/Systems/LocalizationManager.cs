using System;
using System.Collections.Generic;
using System.Text.Json;
using Sandbox;

namespace Beastborne.Systems;

/// <summary>
/// Manages language localization for the game.
/// Loads translations from JSON files and provides lookup methods.
/// </summary>
public sealed class LocalizationManager : Component
{
	public static LocalizationManager Instance { get; private set; }

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

	private static readonly string[] SupportedLanguages = { "en", "fr" };

	protected override void OnAwake()
	{
		Instance = this;
		GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;

		// Load English as fallback
		_fallback = LoadLanguageFile( "en" );
		_translations = _fallback;

		// Load saved language preference
		var savedLang = Game.Cookies.Get( "language", "en" );
		if ( savedLang != "en" )
		{
			SetLanguage( savedLang, notify: false );
		}

		Log.Info( $"LocalizationManager initialized. Language: {CurrentLanguage}, Keys: {_translations.Count}" );
	}

	/// <summary>
	/// Get a translated string by key. Falls back to English if not found.
	/// </summary>
	public static string Get( string key )
	{
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
		if ( string.IsNullOrEmpty( langCode ) ) return;
		langCode = langCode.ToLower();

		if ( CurrentLanguage == langCode ) return;

		CurrentLanguage = langCode;
		Game.Cookies.Set( "language", langCode );

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
		try
		{
			var path = $"localization/{langCode}.json";
			var content = FileSystem.Mounted.ReadAllText( path );

			if ( string.IsNullOrEmpty( content ) )
			{
				Log.Warning( $"Language file empty or not found: {path}" );
				return new Dictionary<string, string>();
			}

			var dict = JsonSerializer.Deserialize<Dictionary<string, string>>( content,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true } );

			return dict ?? new Dictionary<string, string>();
		}
		catch ( Exception e )
		{
			Log.Warning( $"Failed to load language file '{langCode}': {e.Message}" );
			return new Dictionary<string, string>();
		}
	}
}
