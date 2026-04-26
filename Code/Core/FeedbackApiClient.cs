using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// HTTP client for the Beastborne Feedback API server. Mirrors <see cref="SaveApiClient"/> /
/// <see cref="GuildApiClient"/>: same base URL, same auth scheme (X-API-Key + X-Steam-Id),
/// same snake_case JSON, same try/catch/log/return-null error pattern.
/// </summary>
public static class FeedbackApiClient
{
	private const string BASE_URL = "http://157.245.10.193.nip.io:3000/api";
	private const string API_KEY = "5ff1f572c6f9a9d78df95bf152a57aeb5558074b503601ce22ff6f20bdf954a4";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
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
	/// List feedback entries. All filters optional. Returns an empty list on any
	/// transport/parse failure (never null).
	/// </summary>
	public static async Task<List<FeedbackEntry>> ListAsync( string type = null, string tag = null, bool? resolved = null, string sort = "top", int limit = 100, string author = null )
	{
		if ( !HasSteamId() )
		{
			Log.Warning( "FeedbackAPI: no steam id" );
			return new List<FeedbackEntry>();
		}

		try
		{
			var query = new List<string>();
			if ( !string.IsNullOrEmpty( type ) ) query.Add( $"type={type}" );
			if ( !string.IsNullOrEmpty( tag ) ) query.Add( $"tag={tag}" );
			if ( resolved.HasValue ) query.Add( $"resolved={(resolved.Value ? "true" : "false")}" );
			if ( !string.IsNullOrEmpty( sort ) ) query.Add( $"sort={sort}" );
			if ( !string.IsNullOrEmpty( author ) ) query.Add( $"author={author}" );
			query.Add( $"limit={limit}" );

			var url = $"{BASE_URL}/feedback?{string.Join( "&", query )}";
			var response = await Http.RequestStringAsync( url, "GET", null, GetHeaders() );

			if ( string.IsNullOrEmpty( response ) )
				return new List<FeedbackEntry>();

			var wrapper = JsonSerializer.Deserialize<FeedbackListResponse>( response, JsonOptions );
			return wrapper?.Entries ?? new List<FeedbackEntry>();
		}
		catch ( Exception e )
		{
			Log.Warning( $"FeedbackAPI GET /feedback failed: {e.Message}" );
			return new List<FeedbackEntry>();
		}
	}

	/// <summary>
	/// Create a new feedback entry. Returns the created entry on success, null on failure.
	/// </summary>
	public static async Task<FeedbackEntry> CreateAsync( string type, string tag, string title, string body, string authorName )
	{
		if ( !HasSteamId() )
		{
			Log.Warning( "FeedbackAPI: no steam id" );
			return null;
		}

		try
		{
			var url = $"{BASE_URL}/feedback";
			var payload = new FeedbackCreateRequest
			{
				Type = type,
				Tag = tag,
				Title = title,
				Body = body,
				AuthorName = authorName
			};
			var json = JsonSerializer.Serialize( payload, JsonOptions );
			var content = new StringContent( json, System.Text.Encoding.UTF8, "application/json" );

			var response = await Http.RequestStringAsync( url, "POST", content, GetHeaders() );
			if ( string.IsNullOrEmpty( response ) ) return null;
			return JsonSerializer.Deserialize<FeedbackEntry>( response, JsonOptions );
		}
		catch ( Exception e )
		{
			Log.Warning( $"FeedbackAPI POST /feedback failed: {e.Message}" );
			return null;
		}
	}

	/// <summary>
	/// Toggle a vote on the given entry. Returns the updated vote state, or null on failure.
	/// </summary>
	public static async Task<FeedbackVoteResponse> ToggleVoteAsync( int entryId )
	{
		if ( !HasSteamId() )
		{
			Log.Warning( "FeedbackAPI: no steam id" );
			return null;
		}

		try
		{
			var url = $"{BASE_URL}/feedback/{entryId}/vote";
			var content = new StringContent( "{}", System.Text.Encoding.UTF8, "application/json" );
			var response = await Http.RequestStringAsync( url, "POST", content, GetHeaders() );
			if ( string.IsNullOrEmpty( response ) ) return null;
			return JsonSerializer.Deserialize<FeedbackVoteResponse>( response, JsonOptions );
		}
		catch ( Exception e )
		{
			Log.Warning( $"FeedbackAPI POST /feedback/{entryId}/vote failed: {e.Message}" );
			return null;
		}
	}

	/// <summary>
	/// Admin-only: mark entry resolved / re-tag / edit. Returns updated entry or null.
	/// </summary>
	public static async Task<FeedbackEntry> PatchAsync( int entryId, bool? resolved = null, string resolvedNote = null, string tag = null, string type = null )
	{
		if ( !HasSteamId() )
		{
			Log.Warning( "FeedbackAPI: no steam id" );
			return null;
		}

		try
		{
			var url = $"{BASE_URL}/feedback/{entryId}";
			var payload = new FeedbackPatchRequest
			{
				Resolved = resolved,
				ResolvedNote = resolvedNote,
				Tag = tag,
				Type = type
			};
			var json = JsonSerializer.Serialize( payload, JsonOptions );
			var content = new StringContent( json, System.Text.Encoding.UTF8, "application/json" );
			var response = await Http.RequestStringAsync( url, "PATCH", content, GetHeaders() );
			if ( string.IsNullOrEmpty( response ) ) return null;
			return JsonSerializer.Deserialize<FeedbackEntry>( response, JsonOptions );
		}
		catch ( Exception e )
		{
			Log.Warning( $"FeedbackAPI PATCH /feedback/{entryId} failed: {e.Message}" );
			return null;
		}
	}

	/// <summary>
	/// Admin-only: hard delete an entry. Returns true on success.
	/// </summary>
	public static async Task<bool> DeleteAsync( int entryId )
	{
		if ( !HasSteamId() )
		{
			Log.Warning( "FeedbackAPI: no steam id" );
			return false;
		}

		try
		{
			var url = $"{BASE_URL}/feedback/{entryId}";
			var response = await Http.RequestStringAsync( url, "DELETE", null, GetHeaders() );
			return !string.IsNullOrEmpty( response );
		}
		catch ( Exception e )
		{
			Log.Warning( $"FeedbackAPI DELETE /feedback/{entryId} failed: {e.Message}" );
			return false;
		}
	}
}
