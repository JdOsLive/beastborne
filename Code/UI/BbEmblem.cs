using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Beastborne.Core;
using Beastborne.Data;

namespace Beastborne.UI;

/// <summary>
/// THE guild-emblem resolver — one shared parse so every render site (hub
/// guild zone, GuildPanel banner/browse/detail/previews, and the coming
/// guild-page rebuild + beast picker) agrees on what an EmblemIcon string
/// means. Render path only: nothing here writes guild state.
///
/// EmblemIcon grammar + FALLBACK CHAIN (top wins):
///   1. "beast:&lt;speciesId&gt;"  → that species' static IconPath sprite, rendered
///      as a NATIVE-PX viewport crop (CropRect/CropStyle below — the trade-slip
///      framed-tile recipe; pixels are never resampled).
///   2. "" / null (no emblem chosen) → the guild LEADER's favorite beast:
///      a) owner is YOU            → CurrentTamer.FavoriteMonsterSpeciesId
///      b) owner is online         → their broadcast profile favorite
///                                   (guild member ConnectionId → ChatManager.PlayerProfiles)
///      c) you hold their TamerCard→ the card's favorite species
///   3. Legacy lucide key ("dragon", "swords", …) → the LucideMap glyph,
///      exactly as before the beast grammar existed.
///   4. Nothing resolves → "lucide:shield" glyph (or the site's own dim
///      placeholder when it has one — the hub's no-guild tile).
///
/// NOTE: GuildManager currently coalesces a null server EmblemIcon to
/// "dragon" (Code/Core — not touched here), so chain step 2 only fires for
/// guilds whose icon is explicitly saved as "". The picker rebuild should
/// stop coalescing so "no emblem chosen" survives to this resolver.
/// </summary>
public static class BbEmblem
{
	// Legacy lucide emblem keys — the single source both GuildPanel's pickers
	// and every glyph fallback read (was duplicated in GuildPanel +
	// OnlineHubPanel before 2026-08-31). Insertion order = picker order.
	public static readonly Dictionary<string, string> LucideMap = new()
	{
		{ "dragon",  "game-icons:dragon-head" },
		{ "swords",  "lucide:swords" },
		{ "star",    "lucide:star" },
		{ "flame",   "lucide:flame" },
		{ "crown",   "lucide:crown" },
		{ "shield",  "lucide:shield" },
		{ "wolf",    "game-icons:wolf-head" },
		{ "phoenix", "game-icons:eagle-emblem" },
		{ "anchor",  "lucide:anchor" },
		{ "skull",   "lucide:skull" }
	};

	/// <summary>
	/// Chain steps 1 + 2 — the sprite half of the resolve. Returns null when
	/// the emblem is a legacy lucide key, an unknown string, or nothing in the
	/// leader-favorite chain lands (caller then renders ResolveLucide's glyph).
	/// ownerSteamId / members may be 0 / null (browse rows only carry
	/// GuildAdvertisement — no owner id, so ads skip the leader default).
	/// </summary>
	public static MonsterSpecies ResolveBeast( string emblemIcon, long ownerSteamId = 0, IReadOnlyList<GuildMemberInfo> members = null )
	{
		var mm = MonsterManager.Instance;
		if ( mm == null ) return null;

		// 1. Explicit beast pick. A beast: id that doesn't resolve falls all
		//    the way to the shield glyph (honest), never to a different beast.
		if ( !string.IsNullOrEmpty( emblemIcon ) )
		{
			if ( !emblemIcon.StartsWith( "beast:" ) ) return null; // legacy / unknown → lucide path
			var s = mm.GetSpecies( emblemIcon.Substring( 6 ) );
			return string.IsNullOrEmpty( s?.IconPath ) ? null : s;
		}

		// 2. No emblem chosen → the guild leader's favorite beast.
		var favId = LeaderFavoriteSpeciesId( ownerSteamId, members );
		if ( string.IsNullOrEmpty( favId ) ) return null;
		var fav = mm.GetSpecies( favId );
		return string.IsNullOrEmpty( fav?.IconPath ) ? null : fav;
	}

	/// <summary>
	/// Chain steps 3 + 4 — the glyph half. Always returns an iconify name.
	/// </summary>
	public static string ResolveLucide( string emblemIcon )
		=> !string.IsNullOrEmpty( emblemIcon ) && LucideMap.TryGetValue( emblemIcon, out var name ) ? name : "lucide:shield";

	// Leader favorite: you → online broadcast profile → collected TamerCard.
	private static string LeaderFavoriteSpeciesId( long ownerSteamId, IReadOnlyList<GuildMemberInfo> members )
	{
		if ( ownerSteamId <= 0 ) return null;

		// a) The owner is the local player.
		long localId = Connection.Local?.SteamId ?? 0L;
		if ( localId == ownerSteamId )
		{
			var mine = TamerManager.Instance?.CurrentTamer?.FavoriteMonsterSpeciesId;
			if ( !string.IsNullOrEmpty( mine ) ) return mine;
		}

		// b) The owner is online — their profile broadcast carries the favorite.
		var connId = members?.FirstOrDefault( m => m != null && m.SteamId == ownerSteamId )?.ConnectionId;
		if ( !string.IsNullOrEmpty( connId )
			&& ChatManager.Instance?.PlayerProfiles?.TryGetValue( connId, out var profile ) == true
			&& !string.IsNullOrEmpty( profile?.FavoriteMonsterSpeciesId ) )
			return profile.FavoriteMonsterSpeciesId;

		// c) Offline owner — fall back to their collected TamerCard, if held.
		var card = TamerManager.Instance?.CurrentTamer?.CollectedCards?
			.FirstOrDefault( c => c != null && c.SteamId == ownerSteamId );
		return string.IsNullOrEmpty( card?.FavoriteMonsterSpeciesId ) ? null : card.FavoriteMonsterSpeciesId;
	}

	// ── Native-px viewport crop (the trade-slip framed-tile recipe) ─────────
	// Moved here from OnlineHubPanel 2026-08-31 so GuildPanel's emblem tiles
	// share it. The tile is a TW×TH window onto the NATIVE sprite: the <img>
	// renders at source size, IN-FLOW, pulled up/left by negative margins so
	// the head + upper body fill the window. No scaling, no filter, no
	// transform — the compositor never touches the pixels. hBias/vBias = the
	// share of the overflow cut from the left/top (0.45/0.22 = the face crop:
	// sprites face LEFT, the head sits upper-left/centre). A source SMALLER
	// than the window on an axis is CENTRED (positive margin).

	private static readonly Dictionary<string, (int w, int h)> _spriteDims = new();

	public static (int w, int h) SpriteDims( MonsterSpecies s )
	{
		if ( s == null || string.IsNullOrEmpty( s.IconPath ) ) return (96, 96);
		if ( !_spriteDims.TryGetValue( s.IconPath, out var d ) )
		{
			d = (96, 96);
			try
			{
				var tex = Texture.LoadFromFileSystem( s.IconPath, FileSystem.Mounted );
				if ( tex != null && tex.Width > 0 && tex.Height > 0 ) d = (tex.Width, tex.Height);
			}
			catch { }
			_spriteDims[s.IconPath] = d;
		}
		return d;
	}

	public static (int w, int h, int ml, int mt) CropRect( MonsterSpecies s, int tw, int th, float hBias = 0.45f, float vBias = 0.22f )
	{
		var d = SpriteDims( s );
		int ml = d.w > tw ? -(int)Math.Round( (d.w - tw) * hBias ) : (tw - d.w) / 2;
		int mt = d.h > th ? -(int)Math.Round( (d.h - th) * vBias ) : (th - d.h) / 2;
		return (d.w, d.h, ml, mt);
	}

	// Inline style for the crop — whole pixels, so no culture formatting risk.
	public static string CropStyle( (int w, int h, int ml, int mt) c )
		=> $"width: {c.w}px; height: {c.h}px; min-width: {c.w}px; margin-left: {c.ml}px; margin-top: {c.mt}px";

	// One-call convenience for render sites: the full inline style for a
	// TW×TH framed tile.
	public static string CropStyleFor( MonsterSpecies s, int tw, int th, float hBias = 0.45f, float vBias = 0.22f )
		=> CropStyle( CropRect( s, tw, th, hBias, vBias ) );
	/// <summary>Pull a "#rrggbb" color t of the way toward the zone slab tone (#15121f)
	/// — the framed emblem tile's fill recipe. Non-hex strings pass through untouched.
	/// Promoted from OnlineHubPanel 2026-09-01 (GuildPanel's rebuild needed it too).</summary>
	public static string BlendTowardSlab( string hex, float t )
	{
		const int sr = 0x15, sg = 0x12, sb = 0x1f;
		if ( string.IsNullOrEmpty( hex ) || hex.Length != 7 || hex[0] != '#' ) return hex;
		try
		{
			int r = Math.Clamp( (int)Math.Round( Convert.ToInt32( hex.Substring( 1, 2 ), 16 ) + ( sr - Convert.ToInt32( hex.Substring( 1, 2 ), 16 ) ) * t ), 0, 255 );
			int g = Math.Clamp( (int)Math.Round( Convert.ToInt32( hex.Substring( 3, 2 ), 16 ) + ( sg - Convert.ToInt32( hex.Substring( 3, 2 ), 16 ) ) * t ), 0, 255 );
			int b = Math.Clamp( (int)Math.Round( Convert.ToInt32( hex.Substring( 5, 2 ), 16 ) + ( sb - Convert.ToInt32( hex.Substring( 5, 2 ), 16 ) ) * t ), 0, 255 );
			return $"#{r:x2}{g:x2}{b:x2}";
		}
		catch { return hex; }
	}

}
