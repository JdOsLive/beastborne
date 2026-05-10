using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Sandbox;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// Outcome of a cloud save read. Critical to distinguish "server confirmed
/// no save" (legitimate fresh player) from "transport failed / response
/// unparseable" (we have NO IDEA whether the player has a save). Conflating
/// the two lets the client overwrite a real cloud save with an empty blob
/// after a network blip on a machine without a local cache. See
/// <see cref="SaveService.LoadAsync"/> for how each status is handled.
///
/// Lives at namespace scope (not nested in <see cref="SaveApiClient"/>) so
/// <c>SaveService</c> can reference the unqualified name. Static-class
/// nesting hides the type from outside the class without a qualifier.
/// </summary>
public enum SaveLoadStatus
{
	/// <summary>Server returned a populated blob. <c>Blob</c> is non-null.</summary>
	Loaded,
	/// <summary>Server explicitly confirmed this player has no save row. Safe to start fresh.</summary>
	NoSave,
	/// <summary>Transport failed, response was empty/unparseable, or no Steam id. Presence of a save is UNKNOWN — do not overwrite cloud.</summary>
	Failed
}

/// <summary>Discriminated result for <see cref="SaveApiClient.GetSaveAsync"/>.</summary>
public readonly struct SaveLoadResult
{
	public SaveLoadStatus Status { get; }
	public SaveBlob Blob { get; }
	public SaveLoadResult( SaveLoadStatus status, SaveBlob blob ) { Status = status; Blob = blob; }
}

/// <summary>
/// HTTP client for the Beastborne Save API server. Mirrors <see cref="GuildApiClient"/>:
/// same base URL, same auth scheme (X-API-Key + X-Steam-Id), same snake_case JSON,
/// same try/catch/log/return-null error pattern.
///
/// The Steam id is always pulled from <c>Connection.Local?.SteamId</c> and sent as a
/// header — the server scopes all reads/writes to that player. If no Steam id is
/// available, every method bails early with a warning.
/// </summary>
public static class SaveApiClient
{
	private const string BASE_URL = "http://157.245.10.193.nip.io:3000/api";
	private const string API_KEY = "5ff1f572c6f9a9d78df95bf152a57aeb5558074b503601ce22ff6f20bdf954a4";

	// NOTE: We intentionally do NOT set PropertyNamingPolicy here. The server's
	// global snakeToCamelBody middleware rewrites incoming PUT body keys to
	// camelCase before storing, so any client-side snake_case policy causes a
	// round-trip mismatch on multi-word fields (e.g. `LastSaveTicks` gets
	// serialized as `last_save_ticks`, stored as `lastSaveTicks`, then read
	// back — and `SnakeCaseLower` + `PropertyNameCaseInsensitive` won't match
	// `last_save_ticks` to `lastSaveTicks` because case-insensitive doesn't
	// mean underscore-insensitive). Default PascalCase serialization combined
	// with case-insensitive deserialization round-trips cleanly against the
	// middleware's camelCase storage.
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
	/// True if there's a valid (non-zero) Steam id on the local connection. All
	/// save endpoints require authenticated Steam id — bail when absent.
	/// </summary>
	private static bool HasSteamId()
	{
		return Connection.Local != null && Connection.Local.SteamId != 0;
	}

	/// <summary>
	/// Fetch the authenticated player's save blob.
	///
	/// Returns one of three outcomes (see <see cref="SaveLoadStatus"/>):
	/// <list type="bullet">
	///   <item><c>Loaded</c> — server returned a populated blob; <c>Blob</c> is non-null.</item>
	///   <item><c>NoSave</c> — server explicitly answered "this player has no save".</item>
	///   <item><c>Failed</c> — transport error, missing Steam id, or unparseable response.</item>
	/// </list>
	///
	/// The <c>Failed</c> vs <c>NoSave</c> distinction is load-bearing: a caller
	/// that treats <c>Failed</c> as <c>NoSave</c> may auto-write an empty blob
	/// over the player's real cloud save (the bug that wiped jeremykip's save).
	/// </summary>
	public static async Task<SaveLoadResult> GetSaveAsync()
	{
		if ( !HasSteamId() )
		{
			Log.Warning( "SaveAPI: no steam id" );
			return new SaveLoadResult( SaveLoadStatus.Failed, null );
		}

		string response;
		try
		{
			var url = $"{BASE_URL}/players/save";
			response = await Http.RequestStringAsync( url, "GET", null, GetHeaders() );
		}
		catch ( Exception e )
		{
			Log.Warning( $"SaveAPI GET /players/save failed: {e.Message}" );
			return new SaveLoadResult( SaveLoadStatus.Failed, null );
		}

		// Empty response = transport-level failure (s&box's HTTP client returns
		// "" on most non-2xx and connection errors without throwing). Treat as
		// Failed, NOT NoSave — we cannot know whether this player has a save.
		if ( string.IsNullOrEmpty( response ) )
		{
			Log.Warning( "SaveAPI GET /players/save returned empty response — treating as transport failure" );
			return new SaveLoadResult( SaveLoadStatus.Failed, null );
		}

		try
		{
			var wrapper = JsonSerializer.Deserialize<SaveApiResponse>( response, JsonOptions );
			if ( wrapper == null )
			{
				// Unparseable response that wasn't empty — also a transport/server problem.
				Log.Warning( "SaveAPI GET /players/save: response did not deserialize" );
				return new SaveLoadResult( SaveLoadStatus.Failed, null );
			}

			// Server explicitly answered with `{ blob: null }` — fresh player, confirmed.
			if ( wrapper.Blob == null )
				return new SaveLoadResult( SaveLoadStatus.NoSave, null );

			return new SaveLoadResult( SaveLoadStatus.Loaded, wrapper.Blob );
		}
		catch ( Exception e )
		{
			Log.Warning( $"SaveAPI GET /players/save deserialize failed: {e.Message}" );
			return new SaveLoadResult( SaveLoadStatus.Failed, null );
		}
	}

	/// <summary>
	/// Upsert the authenticated player's save blob. Body is <c>{ "blob": {...} }</c>.
	/// Returns true on success. Returns false on missing Steam id or any failure.
	/// </summary>
	public static async Task<bool> PutSaveAsync( SaveBlob blob )
	{
		if ( !HasSteamId() )
		{
			Log.Warning( "SaveAPI: no steam id" );
			return false;
		}

		if ( blob == null )
		{
			Log.Warning( "SaveAPI PUT /players/save refused: blob is null" );
			return false;
		}

		try
		{
			var url = $"{BASE_URL}/players/save";
			var body = new SaveApiRequest { Blob = blob };
			var json = JsonSerializer.Serialize( body, JsonOptions );
			var content = new StringContent( json, System.Text.Encoding.UTF8, "application/json" );

			var response = await Http.RequestStringAsync( url, "PUT", content, GetHeaders() );
			return !string.IsNullOrEmpty( response );
		}
		catch ( Exception e )
		{
			Log.Warning( $"SaveAPI PUT /players/save failed: {e.Message}" );
			return false;
		}
	}

	/// <summary>
	/// Delete the authenticated player's save blob (used by Reset Game).
	/// Returns true on success. Returns false on missing Steam id or any failure.
	/// </summary>
	public static async Task<bool> DeleteSaveAsync()
	{
		if ( !HasSteamId() )
		{
			Log.Warning( "SaveAPI: no steam id" );
			return false;
		}

		try
		{
			var url = $"{BASE_URL}/players/save";
			var response = await Http.RequestStringAsync( url, "DELETE", null, GetHeaders() );
			return !string.IsNullOrEmpty( response );
		}
		catch ( Exception e )
		{
			Log.Warning( $"SaveAPI DELETE /players/save failed: {e.Message}" );
			return false;
		}
	}
}

// ═══════════════════════════════════════════════════════
// API DTOs (match JSON wire shape)
// ═══════════════════════════════════════════════════════

/// <summary>
/// Response wrapper from GET /api/players/save. The server may add sibling fields
/// (server_timestamp, schema_version, archive flags, etc) without breaking the client.
///
/// Note the explicit <c>[JsonPropertyName("blob")]</c>: the server's
/// snakeToCamelBody middleware only converts keys containing underscores, so a
/// single PascalCase word like <c>Blob</c> would NOT be rewritten to <c>blob</c>
/// by the server. We removed <c>JsonNamingPolicy.SnakeCaseLower</c> from
/// <c>JsonOptions</c> to fix multi-word round-tripping, which means we now have
/// to explicitly tag the wire name of the one-word wrapper field ourselves.
/// Nested <c>SaveBlob</c> contents (<c>Tamer</c>, <c>Monsters</c>, etc.) are
/// multi-word PascalCase so the middleware handles them correctly.
/// </summary>
public class SaveApiResponse
{
	[JsonPropertyName( "blob" )]
	public SaveBlob Blob { get; set; }
}

/// <summary>
/// Request body for PUT /api/players/save. See <see cref="SaveApiResponse"/>
/// for why the <c>blob</c> property is explicitly tagged.
/// </summary>
public class SaveApiRequest
{
	[JsonPropertyName( "blob" )]
	public SaveBlob Blob { get; set; }
}
