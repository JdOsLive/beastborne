using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace Beastborne.Core;

/// <summary>
/// HTTP client for the Beastborne live announcements feed. Matches the style of
/// <see cref="SaveApiClient"/>: same base URL, same API key, same
/// case-insensitive JSON deserializer. This endpoint is <b>public</b> on the
/// server (exempted from X-Steam-Id auth), so no steam id is required — we
/// still forward one if available since it doesn't hurt.
///
/// Used by the main-menu news ticker (<c>MainMenu.razor</c>) to rotate through
/// live announcements instead of a hardcoded array. Fetch is best-effort — on
/// failure, callers fall back to local defaults.
/// </summary>
public static class AnnouncementsApiClient
{
	private const string BASE_URL = "http://157.245.10.193.nip.io:3000/api";
	private const string API_KEY = "5ff1f572c6f9a9d78df95bf152a57aeb5558074b503601ce22ff6f20bdf954a4";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private static Dictionary<string, string> GetHeaders()
	{
		var steamId = Connection.Local?.SteamId.ToString() ?? "0";
		return new Dictionary<string, string>
		{
			["X-API-Key"] = API_KEY,
			["X-Steam-Id"] = steamId,
			["Content-Type"] = "application/json"
		};
	}

	/// <summary>
	/// Fetch the full list of live announcements, ordered server-side by
	/// <c>priority DESC, created_at DESC</c>. Returns an empty list on any
	/// transport/parse failure or empty response — never returns null.
	/// </summary>
	public static async Task<List<Announcement>> GetAllAsync()
	{
		try
		{
			var url = $"{BASE_URL}/announcements";
			var response = await Http.RequestStringAsync( url, "GET", null, GetHeaders() );

			if ( string.IsNullOrEmpty( response ) )
				return new List<Announcement>();

			var wrapper = JsonSerializer.Deserialize<AnnouncementsResponse>( response, JsonOptions );
			return wrapper?.Announcements ?? new List<Announcement>();
		}
		catch ( Exception e )
		{
			Log.Warning( $"AnnouncementsAPI GET /announcements failed: {e.Message}" );
			return new List<Announcement>();
		}
	}
}

// ═══════════════════════════════════════════════════════
// API DTOs (match JSON wire shape — camelCase on the wire,
// PascalCase on the C# side, bridged via PropertyNameCaseInsensitive).
// ═══════════════════════════════════════════════════════

/// <summary>
/// One live announcement row from the server. <c>Icon</c> and <c>Color</c>
/// may be null — callers should provide sensible defaults. <c>ExpiresAt</c>
/// is a nullable unix-ms timestamp; the server already filters expired rows,
/// so clients normally don't need to check it.
/// </summary>
public class Announcement
{
	public int Id { get; set; }
	public string Text { get; set; }
	public string Icon { get; set; }
	public string Color { get; set; }
	public int Priority { get; set; }
	public long? ExpiresAt { get; set; }
}

/// <summary>
/// Response wrapper from GET /api/announcements.
/// </summary>
public class AnnouncementsResponse
{
	public List<Announcement> Announcements { get; set; }
}
