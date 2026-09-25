using System;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Sandbox;

namespace Beastborne.Core;

/// <summary>
/// Undoes the save server's key rewriting before a save blob is deserialized.
///
/// The API server (public-square-bot, <c>snakeToCamelBody</c>) rewrote EVERY key in
/// every request body — <c>_x</c> → <c>X</c> — including the save blob's DATA keys:
/// item ids (<c>boost_def</c> → <c>boostDef</c>), zone ids (<c>saltmoor_cove</c> →
/// <c>saltmoorCove</c>) and the snake_case JSON names of beast genes / stats
/// (<c>sp_a_gene</c> → <c>spAGene</c>). Loaded back, none of those match: the Bag
/// looks empty, Hard Mode re-locks, and three genes read as 0.
///
/// Our saves never use lower-camelCase keys on purpose (field names are PascalCase,
/// ids and explicit JSON names are snake_case), so any key that starts lowercase and
/// contains an uppercase letter is a rewritten one and maps back exactly: each
/// uppercase letter becomes <c>_</c> + lowercase. Digits were never touched by the
/// server (<c>catch_1</c> stayed <c>catch_1</c>), so they need nothing.
///
/// If a rewritten key collides with a real one in the same object (an item gained
/// again after the damage), numbers are summed, bools OR'd, otherwise the real key
/// wins. Healthy saves pass through unchanged.
/// </summary>
public static class SaveKeyRepair
{
	/// <summary>Repair a raw save blob JSON (the local cache file).</summary>
	public static string RepairBlob( string json )
	{
		try
		{
			var root = JsonNode.Parse( json );
			int fixedKeys = Repair( root );
			if ( fixedKeys == 0 ) return json;
			Log.Info( $"[SaveKeyRepair] Restored {fixedKeys} server-renamed save key(s)." );
			return root.ToJsonString();
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[SaveKeyRepair] skipped: {ex.Message}" );
			return json;
		}
	}

	/// <summary>Repair only the <c>blob</c> of a GET /players/save response
	/// (the wrapper's own fields are the server's and stay as they are).</summary>
	public static string RepairResponse( string json )
	{
		try
		{
			var root = JsonNode.Parse( json );
			if ( root is not JsonObject obj || obj["blob"] is not JsonNode blob ) return json;
			int fixedKeys = Repair( blob );
			if ( fixedKeys == 0 ) return json;
			Log.Info( $"[SaveKeyRepair] Restored {fixedKeys} server-renamed save key(s) in the cloud save." );
			return root.ToJsonString();
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[SaveKeyRepair] skipped: {ex.Message}" );
			return json;
		}
	}

	private static int Repair( JsonNode node )
	{
		int count = 0;
		if ( node is JsonArray arr )
		{
			foreach ( var child in arr ) count += Repair( child );
			return count;
		}
		if ( node is not JsonObject obj ) return 0;

		foreach ( var key in obj.Select( kv => kv.Key ).ToList() )
		{
			var value = obj[key];
			count += Repair( value );
			if ( !IsRewritten( key ) ) continue;

			var original = ToSnake( key );
			obj.Remove( key );
			count++;
			if ( !obj.ContainsKey( original ) )
			{
				obj[original] = value;
				continue;
			}

			var existing = obj[original];
			if ( existing is JsonValue ev && value is JsonValue nv )
			{
				if ( ev.TryGetValue<long>( out var a ) && nv.TryGetValue<long>( out var b ) )
					obj[original] = a + b;
				else if ( ev.TryGetValue<bool>( out var x ) && nv.TryGetValue<bool>( out var y ) )
					obj[original] = x || y;
			}
			// otherwise the real key's value wins
		}
		return count;
	}

	private static bool IsRewritten( string key )
		=> key.Length > 1 && char.IsLower( key[0] ) && key.Any( char.IsUpper );

	private static string ToSnake( string key )
	{
		var sb = new StringBuilder( key.Length + 4 );
		foreach ( var c in key )
		{
			if ( char.IsUpper( c ) ) sb.Append( '_' ).Append( char.ToLowerInvariant( c ) );
			else sb.Append( c );
		}
		return sb.ToString();
	}
}
