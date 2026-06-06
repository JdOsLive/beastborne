using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace Beastborne.Core;

/// <summary>
/// Fetches Discord guild stats (online + total members) for the main-menu
/// Community card.
///
/// IMPORTANT: s&box will NOT let the game call discord.com directly — it's
/// blocked even when added to the .sbproj HttpAllowList. So this routes through
/// the Beastborne backend (the same droplet the other API clients use). A
/// server-side worker — the <b>PublicSquare bot</b>, which already holds the
/// Beastborne backend bits — is responsible for querying Discord and caching
/// the counts.
///
/// ── SERVER CONTRACT (implement on the droplet / PublicSquare bot) ──
///   GET {BASE_URL}/discord-stats        headers: X-API-Key, X-Steam-Id
///   200 -> { "online": 312, "members": 2756 }
///   The bot polls Discord server-side (bot-token gateway for live presence, or
///   the public invites/{code}?with_counts=true endpoint) and caches the result;
///   this client hits it once per menu boot. Until the endpoint exists it simply
///   404s -> null -> the card shows its static "Join the Discord community"
///   fallback, so shipping this client early is harmless.
///
/// Best-effort: returns null on any transport/parse failure.
/// </summary>
public static class DiscordStatsApiClient
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

	public static async Task<DiscordStats> GetAsync()
	{
		try
		{
			var url = $"{BASE_URL}/discord-stats";
			var response = await Http.RequestStringAsync( url, "GET", null, GetHeaders() );

			if ( string.IsNullOrEmpty( response ) )
				return null;

			var stats = JsonSerializer.Deserialize<DiscordStats>( response, JsonOptions );
			// Treat a missing/zero member count as "no data" so the card keeps its
			// static fallback rather than rendering "0 in the Discord".
			return ( stats != null && stats.Members > 0 ) ? stats : null;
		}
		catch ( Exception e )
		{
			Log.Warning( $"DiscordStats GET /discord-stats failed: {e.Message}" );
			return null;
		}
	}
}

/// <summary>Stats from the backend's /discord-stats endpoint.</summary>
public class DiscordStats
{
	public int Online { get; set; }
	public int Members { get; set; }
}
