using System.Linq;
using System.Text.RegularExpressions;

namespace Beastborne.Core;

/// <summary>
/// Chat profanity filter — replaces blocked words with asterisks. Applied on both
/// send and receive paths so a modded client cannot bypass the filter for others.
/// </summary>
public static class ProfanityFilter
{
	// Blocked-word list. Whole-word match, case-insensitive. Edit this list to
	// add/remove entries; the regex is rebuilt on first use. Keep entries lowercase.
	private static readonly string[] BlockedWords = new[]
	{
		// Slurs
		"nigger", "nigga", "niggar", "niglet",
		"faggot", "fag", "faggy", "faggit",
		"tranny", "trannie",
		"retard", "retarded", "tard",
		"kike", "spic", "chink", "gook", "wetback", "beaner", "raghead", "towelhead",
		"dyke",
		// Profanity
		"fuck", "fucker", "fucking", "fuk", "fck", "phuck",
		"shit", "shitter", "bullshit",
		"bitch", "biatch",
		"cunt",
		"asshole", "asshat",
		"bastard",
		"dick", "dickhead",
		"pussy",
		"cock", "cocksucker",
		"whore",
		"slut",
		"twat",
		"motherfucker", "mfer",
		// Sexual / predatory
		"rape", "rapist",
		"pedo", "pedophile", "paedo",
		"molest", "molester",
		"cp",
	};

	private static readonly Regex _pattern = new Regex(
		@"\b(" + string.Join( "|", BlockedWords.Select( Regex.Escape ) ) + @")\b",
		RegexOptions.Compiled | RegexOptions.IgnoreCase
	);

	/// <summary>
	/// Replace every blocked word in the input with asterisks of equal length.
	/// </summary>
	public static string Filter( string input )
	{
		if ( string.IsNullOrEmpty( input ) ) return input;
		return _pattern.Replace( input, match => new string( '*', match.Length ) );
	}

	/// <summary>
	/// True if the input contains any blocked word.
	/// </summary>
	public static bool ContainsProfanity( string input )
	{
		if ( string.IsNullOrEmpty( input ) ) return false;
		return _pattern.IsMatch( input );
	}
}
