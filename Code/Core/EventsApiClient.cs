using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Sandbox;

namespace Beastborne.Core;

/// <summary>
/// HTTP client for the Beastborne live events feed. Matches the style of
/// <see cref="AnnouncementsApiClient"/>: same base URL, same API key, same
/// case-insensitive JSON deserializer. The GET endpoint is <b>public</b> on
/// the server (exempted from X-Steam-Id auth), so no steam id is required —
/// we still forward one if available since it doesn't hurt.
///
/// Used by the main-menu event toast (<c>MainMenu.razor</c>) to surface
/// currently-active server-driven events. Fetch is best-effort — on failure,
/// the toast simply hides.
/// </summary>
public static class EventsApiClient
{
	private const string BASE_URL = "http://157.245.10.193.nip.io:3000/api";
	private const string API_KEY = "5ff1f572c6f9a9d78df95bf152a57aeb5558074b503601ce22ff6f20bdf954a4";

	// Case-insensitive is not enough for multi-word fields (e.g. `startsAt`
	// wire vs `StartsAt` C# property) — the DTO uses explicit
	// [JsonPropertyName] attributes on those, matching the SaveApiClient /
	// GiftApiClient pattern. See the DTO below.
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
	/// Fetch all currently-active live events, ordered server-side by
	/// <c>priority DESC, starts_at DESC</c>. The server already filters out
	/// events outside the <c>starts_at &lt;= now &lt; ends_at</c> window, so
	/// clients only need to worry about timing out during the active window.
	/// Returns an empty list on any transport/parse failure or empty
	/// response — never returns null.
	/// </summary>
	public static async Task<List<LiveEvent>> GetAllAsync()
	{
		try
		{
			var url = $"{BASE_URL}/events";
			var response = await Http.RequestStringAsync( url, "GET", null, GetHeaders() );

			if ( string.IsNullOrEmpty( response ) )
				return new List<LiveEvent>();

			var wrapper = JsonSerializer.Deserialize<LiveEventsResponse>( response, JsonOptions );
			return wrapper?.Events ?? new List<LiveEvent>();
		}
		catch ( Exception e )
		{
			Log.Warning( $"EventsAPI GET /events failed: {e.Message}" );
			return new List<LiveEvent>();
		}
	}
}

// ═══════════════════════════════════════════════════════
// API DTOs (match JSON wire shape — camelCase on the wire,
// PascalCase on the C# side, bridged via PropertyNameCaseInsensitive
// for single-word fields and explicit [JsonPropertyName] for
// multi-word fields — mirrors SaveApiClient / GiftApiClient pattern).
// ═══════════════════════════════════════════════════════

/// <summary>
/// One live event row from the server. <c>Icon</c>, <c>Subtitle</c>,
/// <c>Color</c>, <c>BoostType</c>, and <c>BoostMultiplier</c> may be null —
/// callers should provide sensible defaults. <c>StartsAt</c> and
/// <c>EndsAt</c> are unix-ms timestamps; the server already filters inactive
/// rows, so clients typically only watch <c>EndsAt</c> for countdown display.
/// </summary>
public class LiveEvent
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Icon { get; set; }
	public string Subtitle { get; set; }
	public string Color { get; set; }

	[JsonPropertyName( "boostType" )]
	public string BoostType { get; set; }

	[JsonPropertyName( "boostMultiplier" )]
	public double? BoostMultiplier { get; set; }

	[JsonPropertyName( "startsAt" )]
	public long StartsAt { get; set; }

	[JsonPropertyName( "endsAt" )]
	public long EndsAt { get; set; }

	public int Priority { get; set; }
}

/// <summary>
/// Response wrapper from GET /api/events.
/// </summary>
public class LiveEventsResponse
{
	public List<LiveEvent> Events { get; set; }
}
