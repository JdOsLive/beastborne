using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Sandbox;

namespace Beastborne.Core;

/// <summary>
/// HTTP client for the Beastborne Pending Gifts API. Mirrors
/// <see cref="SaveApiClient"/>: same base URL, same auth scheme
/// (X-API-Key + X-Steam-Id), same try/catch/log/bail pattern.
///
/// The Steam id is always pulled from <c>Connection.Local?.SteamId</c> and sent
/// as a header — the server scopes reads to that player. If no Steam id is
/// available, every method bails early with a warning and returns an empty list.
/// </summary>
public static class GiftApiClient
{
	private const string BASE_URL = "http://157.245.10.193.nip.io:3000/api";
	private const string API_KEY = "5ff1f572c6f9a9d78df95bf152a57aeb5558074b503601ce22ff6f20bdf954a4";

	// Match SaveApiClient — default PascalCase serialization + case-insensitive
	// deserialization. Multi-word JSON wire keys are explicitly tagged via
	// [JsonPropertyName] on the DTOs below so camelCase round-trips cleanly.
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

	private static bool HasSteamId()
	{
		return Connection.Local != null && Connection.Local.SteamId != 0;
	}

	/// <summary>
	/// Fetch the pending (unclaimed) gifts for the authenticated player,
	/// sorted oldest-first by the server. Returns an empty list on missing
	/// Steam id or any transport/parse failure — never returns null.
	/// </summary>
	public static async Task<List<PendingGift>> GetPendingAsync()
	{
		if ( !HasSteamId() )
		{
			Log.Warning( "GiftAPI: no steam id" );
			return new List<PendingGift>();
		}

		try
		{
			var url = $"{BASE_URL}/players/gifts";
			var response = await Http.RequestStringAsync( url, "GET", null, GetHeaders() );

			if ( string.IsNullOrEmpty( response ) )
				return new List<PendingGift>();

			var wrapper = JsonSerializer.Deserialize<PendingGiftsResponse>( response, JsonOptions );
			return wrapper?.Gifts ?? new List<PendingGift>();
		}
		catch ( Exception e )
		{
			Log.Warning( $"GiftAPI GET /players/gifts failed: {e.Message}" );
			return new List<PendingGift>();
		}
	}

	/// <summary>
	/// Claim the specified pending gifts. Returns the full reward payloads on
	/// success, empty list on failure (so the client never double-applies a
	/// reward it can't confirm).
	/// </summary>
	public static async Task<List<PendingGift>> ClaimAsync( List<int> giftIds )
	{
		if ( !HasSteamId() )
		{
			Log.Warning( "GiftAPI: no steam id" );
			return new List<PendingGift>();
		}

		if ( giftIds == null || giftIds.Count == 0 )
			return new List<PendingGift>();

		try
		{
			var url = $"{BASE_URL}/players/gifts/claim";
			var body = new ClaimGiftsRequest { GiftIds = giftIds };
			var json = JsonSerializer.Serialize( body, JsonOptions );
			var content = new StringContent( json, System.Text.Encoding.UTF8, "application/json" );

			var response = await Http.RequestStringAsync( url, "POST", content, GetHeaders() );
			if ( string.IsNullOrEmpty( response ) )
				return new List<PendingGift>();

			var wrapper = JsonSerializer.Deserialize<ClaimGiftsResponse>( response, JsonOptions );
			if ( wrapper == null || !wrapper.Success )
				return new List<PendingGift>();

			return wrapper.Claimed ?? new List<PendingGift>();
		}
		catch ( Exception e )
		{
			Log.Warning( $"GiftAPI POST /players/gifts/claim failed: {e.Message}" );
			return new List<PendingGift>();
		}
	}
}

// ═══════════════════════════════════════════════════════
// API DTOs (match JSON wire shape — server emits camelCase)
// ═══════════════════════════════════════════════════════

/// <summary>
/// A single unclaimed gift row. Multi-word wire keys are tagged explicitly so
/// they round-trip against the server's camelCase without relying on
/// case-insensitive deserialization to bridge the gap.
/// </summary>
public class PendingGift
{
	public int Id { get; set; }
	public int Gold { get; set; }
	public int Gems { get; set; }
	public int Ink { get; set; }

	[JsonPropertyName( "bossTokens" )]
	public int BossTokens { get; set; }

	/// <summary>
	/// item_id → count map. May be null when the gift has no items.
	/// </summary>
	public Dictionary<string, int> Items { get; set; }

	public string Reason { get; set; }

	[JsonPropertyName( "createdAt" )]
	public long? CreatedAt { get; set; }

	[JsonPropertyName( "createdBy" )]
	public string CreatedBy { get; set; }
}

/// <summary>Response from GET /api/players/gifts.</summary>
public class PendingGiftsResponse
{
	public List<PendingGift> Gifts { get; set; }
}

/// <summary>Request body for POST /api/players/gifts/claim.</summary>
public class ClaimGiftsRequest
{
	[JsonPropertyName( "giftIds" )]
	public List<int> GiftIds { get; set; }
}

/// <summary>Response from POST /api/players/gifts/claim.</summary>
public class ClaimGiftsResponse
{
	public bool Success { get; set; }
	public List<PendingGift> Claimed { get; set; }
}
