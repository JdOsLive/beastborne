using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Sandbox;

namespace Beastborne.Core;

/// <summary>
/// Fetches public Discord guild stats (online + total member counts) via the
/// invite-with-counts endpoint — <b>no bot token required</b>:
/// <c>GET https://discord.com/api/v10/invites/{code}?with_counts=true</c>.
///
/// Used by the main-menu Community card (<c>MainMenu.razor</c>) to show live
/// "X online · Y in the Discord". Best-effort: returns null on any transport/
/// parse failure (the card falls back to a static "Join the Discord" line).
///
/// NOTE: <see cref="INVITE_CODE"/> must be a <b>non-expiring</b> invite or the
/// counts stop resolving. It mirrors the code shown on the menu's Discord card.
/// </summary>
public static class DiscordStatsApiClient
{
	private const string INVITE_CODE = "dJTTyCqKru";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public static async Task<DiscordStats> GetAsync()
	{
		try
		{
			var url = $"https://discord.com/api/v10/invites/{INVITE_CODE}?with_counts=true";
			var response = await Http.RequestStringAsync( url, "GET", null, null );

			if ( string.IsNullOrEmpty( response ) )
				return null;

			var stats = JsonSerializer.Deserialize<DiscordStats>( response, JsonOptions );
			// A valid invite always resolves a guild with members; treat a 0/missing
			// member count as a failure so the card keeps its static fallback.
			return ( stats != null && stats.ApproximateMemberCount > 0 ) ? stats : null;
		}
		catch ( Exception e )
		{
			Log.Warning( $"DiscordStats GET invite failed: {e.Message}" );
			return null;
		}
	}
}

/// <summary>Subset of the Discord invite-with-counts response we care about.</summary>
public class DiscordStats
{
	[JsonPropertyName( "approximate_presence_count" )]
	public int ApproximatePresenceCount { get; set; }

	[JsonPropertyName( "approximate_member_count" )]
	public int ApproximateMemberCount { get; set; }
}
