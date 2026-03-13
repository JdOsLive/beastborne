using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace Beastborne.Core;

/// <summary>
/// HTTP client for the Beastborne Guild API server.
/// Wraps Sandbox.Http for all guild-related API calls.
/// </summary>
public static class GuildApiClient
{
	private const string BASE_URL = "http://157.245.10.193.nip.io:3000/api";
	private const string API_KEY = "5ff1f572c6f9a9d78df95bf152a57aeb5558074b503601ce22ff6f20bdf954a4";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
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
	/// GET request, returns deserialized JSON response.
	/// </summary>
	public static async Task<T> GetAsync<T>( string path ) where T : class
	{
		try
		{
			var url = $"{BASE_URL}/{path}";
			var response = await Http.RequestStringAsync( url, "GET", null, GetHeaders() );

			if ( string.IsNullOrEmpty( response ) )
				return null;

			return JsonSerializer.Deserialize<T>( response, JsonOptions );
		}
		catch ( Exception e )
		{
			Log.Warning( $"GuildAPI GET /{path} failed: {e.Message}" );
			return null;
		}
	}

	/// <summary>
	/// POST request with JSON body, returns deserialized response.
	/// </summary>
	public static async Task<T> PostAsync<T>( string path, object body ) where T : class
	{
		try
		{
			var url = $"{BASE_URL}/{path}";
			var json = JsonSerializer.Serialize( body, JsonOptions );
			var content = new StringContent( json, System.Text.Encoding.UTF8, "application/json" );

			var response = await Http.RequestStringAsync( url, "POST", content, GetHeaders() );

			if ( string.IsNullOrEmpty( response ) )
				return null;

			return JsonSerializer.Deserialize<T>( response, JsonOptions );
		}
		catch ( Exception e )
		{
			Log.Warning( $"GuildAPI POST /{path} failed: {e.Message}" );
			return null;
		}
	}

	/// <summary>
	/// PUT request with JSON body, returns deserialized response.
	/// </summary>
	public static async Task<T> PutAsync<T>( string path, object body ) where T : class
	{
		try
		{
			var url = $"{BASE_URL}/{path}";
			var json = JsonSerializer.Serialize( body, JsonOptions );
			var content = new StringContent( json, System.Text.Encoding.UTF8, "application/json" );

			var response = await Http.RequestStringAsync( url, "PUT", content, GetHeaders() );

			if ( string.IsNullOrEmpty( response ) )
				return null;

			return JsonSerializer.Deserialize<T>( response, JsonOptions );
		}
		catch ( Exception e )
		{
			Log.Warning( $"GuildAPI PUT /{path} failed: {e.Message}" );
			return null;
		}
	}

	/// <summary>
	/// PATCH request with JSON body, returns deserialized response.
	/// </summary>
	public static async Task<T> PatchAsync<T>( string path, object body ) where T : class
	{
		try
		{
			var url = $"{BASE_URL}/{path}";
			var json = JsonSerializer.Serialize( body, JsonOptions );
			var content = new StringContent( json, System.Text.Encoding.UTF8, "application/json" );

			var response = await Http.RequestStringAsync( url, "PATCH", content, GetHeaders() );

			if ( string.IsNullOrEmpty( response ) )
				return null;

			return JsonSerializer.Deserialize<T>( response, JsonOptions );
		}
		catch ( Exception e )
		{
			Log.Warning( $"GuildAPI PATCH /{path} failed: {e.Message}" );
			return null;
		}
	}

	/// <summary>
	/// DELETE request, returns true on success.
	/// </summary>
	public static async Task<bool> DeleteAsync( string path )
	{
		try
		{
			var url = $"{BASE_URL}/{path}";
			var response = await Http.RequestStringAsync( url, "DELETE", null, GetHeaders() );
			return !string.IsNullOrEmpty( response );
		}
		catch ( Exception e )
		{
			Log.Warning( $"GuildAPI DELETE /{path} failed: {e.Message}" );
			return false;
		}
	}

	/// <summary>
	/// POST request, returns true on success (for fire-and-forget calls like heartbeat).
	/// </summary>
	public static async Task<bool> PostAsync( string path, object body = null )
	{
		try
		{
			var url = $"{BASE_URL}/{path}";
			HttpContent content = null;

			if ( body != null )
			{
				var json = JsonSerializer.Serialize( body, JsonOptions );
				content = new StringContent( json, System.Text.Encoding.UTF8, "application/json" );
			}

			var response = await Http.RequestStringAsync( url, "POST", content, GetHeaders() );
			return !string.IsNullOrEmpty( response );
		}
		catch ( Exception e )
		{
			Log.Warning( $"GuildAPI POST /{path} failed: {e.Message}" );
			return false;
		}
	}
}

// ═══════════════════════════════════════════════════════
// API Response DTOs (match JSON from server)
// ═══════════════════════════════════════════════════════

/// <summary>
/// Response from GET /api/players/:steamId/guild
/// </summary>
public class MyGuildResponse
{
	public GuildApiData Guild { get; set; }
	public MembershipData Membership { get; set; }
	public List<MemberApiData> Members { get; set; } = new();
	public List<LogApiData> Log { get; set; } = new();
	public List<RequestApiData> Requests { get; set; } = new();
}

/// <summary>
/// Response from GET /api/guilds (browse)
/// </summary>
public class GuildListResponse
{
	public List<GuildApiData> Guilds { get; set; } = new();
}

/// <summary>
/// Response from GET /api/guilds/:id
/// </summary>
public class GuildDetailResponse
{
	public GuildApiData Guild { get; set; }
	public List<MemberApiData> Members { get; set; } = new();
	public List<LogApiData> Log { get; set; } = new();
	public List<RequestApiData> Requests { get; set; } = new();
}

/// <summary>
/// Response from POST /api/guilds (create)
/// </summary>
public class CreateGuildResponse
{
	public GuildApiData Guild { get; set; }
	public List<MemberApiData> Members { get; set; } = new();
}

/// <summary>
/// Response from PUT /api/guilds/:id (update)
/// </summary>
public class UpdateGuildResponse
{
	public GuildApiData Guild { get; set; }
}

/// <summary>
/// Response from member operations
/// </summary>
public class MembersResponse
{
	public bool Success { get; set; }
	public List<MemberApiData> Members { get; set; } = new();
}

/// <summary>
/// Response from GET /api/players/:steamId/invites
/// </summary>
public class InvitesResponse
{
	public List<InviteApiData> Invites { get; set; } = new();
}

/// <summary>
/// Response from POST /api/invites/:id/accept
/// </summary>
public class AcceptInviteResponse
{
	public bool Success { get; set; }
	public string GuildId { get; set; }
	public List<MemberApiData> Members { get; set; } = new();
}

/// <summary>
/// Response from GET /api/guilds/:id/raid
/// </summary>
public class RaidResponse
{
	public RaidApiData Raid { get; set; }
	public List<RaidScoreApiData> Scores { get; set; } = new();
	public int TotalScore { get; set; }
	public int TodayAttempts { get; set; }
	public int MaxAttemptsPerDay { get; set; }
}

/// <summary>
/// Response from POST /api/guilds/:id/raid/score
/// </summary>
public class RaidScoreResponse
{
	public bool Success { get; set; }
	public bool IsNewBest { get; set; }
	public int BestScore { get; set; }
	public int GuildTotalScore { get; set; }
	public int TodayAttempts { get; set; }
}

/// <summary>
/// Response from POST /api/guilds/:id/xp
/// </summary>
public class XpResponse
{
	public int GuildXp { get; set; }
	public int Level { get; set; }
	public bool LeveledUp { get; set; }
}

// ═══════════════════════════════════════════════════════
// API Data Objects (match database column names in snake_case)
// ═══════════════════════════════════════════════════════

public class GuildApiData
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Tag { get; set; }
	public int Level { get; set; }
	public long GuildXp { get; set; }
	public string EmblemColor { get; set; }
	public string EmblemIcon { get; set; }
	public string EmblemShape { get; set; }
	public string Description { get; set; }
	public string Motd { get; set; }
	public string JoinMode { get; set; }
	public int MinLevel { get; set; }
	public string MinRank { get; set; }
	public int AutoKickInactive { get; set; }
	public int InactiveDays { get; set; }
	public int MaxMembers { get; set; }
	public string OwnerSteamId { get; set; }
	public string OwnerName { get; set; }
	public int TotalCatches { get; set; }
	public int TotalArenaWins { get; set; }
	public int TotalExpeditions { get; set; }
	public int TotalRaidsCompleted { get; set; }
	public string CreatedAt { get; set; }
	public string UpdatedAt { get; set; }
	public int MemberCount { get; set; } // Only in browse results
}

public class MemberApiData
{
	public string GuildId { get; set; }
	public string SteamId { get; set; }
	public string Name { get; set; }
	public int Role { get; set; }
	public int Level { get; set; }
	public int ArenaPoints { get; set; }
	public string ArenaRank { get; set; }
	public string JoinedAt { get; set; }
	public string LastSeen { get; set; }
	public int WeeklyRp { get; set; }
	public int WeeklyGuildXp { get; set; }
	public int WeeklyRaidDamage { get; set; }
	public int BestRaidScore { get; set; }
}

public class MembershipData
{
	public string GuildId { get; set; }
	public string GuildName { get; set; }
	public string GuildTag { get; set; }
	public int Role { get; set; }
	public string JoinedAt { get; set; }
}

public class LogApiData
{
	public int Id { get; set; }
	public string GuildId { get; set; }
	public string Timestamp { get; set; }
	public string Action { get; set; }
	public string PlayerName { get; set; }
	public string Details { get; set; }
}

public class RequestApiData
{
	public string GuildId { get; set; }
	public string SteamId { get; set; }
	public string PlayerName { get; set; }
	public int Level { get; set; }
	public string ArenaRank { get; set; }
	public int ArenaPoints { get; set; }
	public string RequestedAt { get; set; }
}

public class InviteApiData
{
	public int Id { get; set; }
	public string GuildId { get; set; }
	public string GuildName { get; set; }
	public string GuildTag { get; set; }
	public string TargetSteamId { get; set; }
	public string InviterName { get; set; }
	public string InviterSteamId { get; set; }
	public string SentAt { get; set; }
}

public class RaidApiData
{
	public string GuildId { get; set; }
	public int PeriodNumber { get; set; }
	public string BossSpeciesId { get; set; }
	public string BossName { get; set; }
	public string BossElement { get; set; }
	public int BossLevel { get; set; }
	public int MaxRounds { get; set; }
}

public class RaidScoreApiData
{
	public string GuildId { get; set; }
	public int PeriodNumber { get; set; }
	public string SteamId { get; set; }
	public string PlayerName { get; set; }
	public int TotalScore { get; set; }
	public int RawDamage { get; set; }
	public float ComboMultiplier { get; set; }
	public int RoundsUsed { get; set; }
	public string AttemptedAt { get; set; }
}
