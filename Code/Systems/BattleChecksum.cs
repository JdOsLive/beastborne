using System.Security.Cryptography;
using System.Text;

namespace Beastborne.Systems;

/// <summary>
/// Wire-level integrity check for serialized team JSON exchanged between peers
/// during PvP matchmaking. Both sides hash the same JSON string and compare
/// hex digests — mismatch on receive means the payload was modified mid-flight
/// or by a modded client.
///
/// Cheap insurance, NOT a cheat-proof signature: a modded client that knows
/// the algorithm can compute its own correct hash after tampering. This catches
/// transport corruption and the simplest tampering cases (replace stat fields
/// without rehashing). Authoritative-server validation is the only real
/// cheat-resistance answer; that's Scope B v1.3+ work.
///
/// Used by `CompetitiveManager` for team-RPC payloads today; reusable from
/// any future PvP wire format that sends serialized state between clients.
/// </summary>
public static class BattleChecksum
{
	/// <summary>
	/// SHA-256 over UTF-8 bytes, returned as lowercase hex. Empty/null input
	/// returns the empty string so callers can short-circuit safely.
	/// </summary>
	public static string Compute( string teamJson )
	{
		if ( string.IsNullOrEmpty( teamJson ) ) return string.Empty;
		var bytes = SHA256.HashData( Encoding.UTF8.GetBytes( teamJson ) );
		var sb = new StringBuilder( bytes.Length * 2 );
		foreach ( var b in bytes ) sb.Append( b.ToString( "x2" ) );
		return sb.ToString();
	}

	/// <summary>
	/// Convenience helper: returns true when the recomputed digest of `teamJson`
	/// matches `expectedChecksum`. On mismatch, logs a warning tagged with
	/// `context` and returns false. Callers decide what to do on rejection
	/// (cancel match, drop queue entry, surface UI error, etc.).
	/// </summary>
	public static bool Verify( string teamJson, string expectedChecksum, string context )
	{
		var actual = Compute( teamJson );
		if ( actual == expectedChecksum ) return true;
		Log.Warning( $"[BattleChecksum] Mismatch on {context}: expected {expectedChecksum}, got {actual}. Rejecting payload." );
		return false;
	}
}
