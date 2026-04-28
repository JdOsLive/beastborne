using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox;

namespace Beastborne.Core;

/// <summary>
/// In-session cache of server-driven LiveEvent boosts. Refreshes the live
/// /api/events feed on start and every 5 minutes thereafter, indexes the
/// results by <c>BoostType</c>, and exposes a single
/// <see cref="GetBoostMultiplier"/> lookup for gameplay code (XP, gold, etc.) —
/// same shape as <see cref="ShopManager.GetBoostMultiplier"/>. If multiple
/// active events specify the same boost type, the MAX multiplier wins (no
/// stacking) so admins can run overlapping events without unintentional 4×.
/// </summary>
public sealed class LiveEventManager : Component
{
	public static LiveEventManager Instance { get; private set; }

	private Dictionary<string, double> _boostsByType = new( StringComparer.OrdinalIgnoreCase );
	private float _sinceRefresh = 0f;
	private const float RefreshSeconds = 300f;

	// ─── HARDCODED LAUNCH-WEEK BOOST ─────────────────────────────────
	// Server has no admin POST endpoint for events at launch — bake the
	// open-beta +50% XP boost directly. Sister to <see cref="TamerManager.EventXPEnd"/>.
	// Once the server admin tool exists, drop this and push via /api/events.
	public static readonly DateTime LaunchWeekXPEnd = new DateTime( 2026, 5, 5, 12, 0, 0, DateTimeKind.Utc );
	private const double LaunchWeekXPMultiplier = 1.5;
	public static bool IsLaunchWeekXPActive => DateTime.UtcNow < LaunchWeekXPEnd;

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
		}
		else
		{
			Destroy();
		}
	}

	protected override void OnStart()
	{
		_ = RefreshAsync();
	}

	protected override void OnUpdate()
	{
		_sinceRefresh += Time.Delta;
		if ( _sinceRefresh >= RefreshSeconds )
		{
			_sinceRefresh = 0f;
			_ = RefreshAsync();
		}
	}

	/// <summary>
	/// Lookup the active multiplier for a given <c>BoostType</c> (e.g. "xp",
	/// "gold"). Case-insensitive. Returns 1.0 when no event of that type is
	/// active or when no events have been fetched yet.
	/// </summary>
	public double GetBoostMultiplier( string boostType )
	{
		if ( string.IsNullOrEmpty( boostType ) ) return 1.0;

		double serverMultiplier = _boostsByType.TryGetValue( boostType, out var m ) ? m : 1.0;

		// Hardcoded launch-week XP boost stacks via MAX so a later server-pushed
		// event can override it (1.5× hardcoded, 2× server → players get 2×).
		if ( boostType.Equals( "xp", StringComparison.OrdinalIgnoreCase ) && IsLaunchWeekXPActive )
			return Math.Max( serverMultiplier, LaunchWeekXPMultiplier );

		return serverMultiplier;
	}

	public async Task RefreshAsync()
	{
		try
		{
			var events = await EventsApiClient.GetAllAsync();
			var map = new Dictionary<string, double>( StringComparer.OrdinalIgnoreCase );
			foreach ( var e in events )
			{
				if ( string.IsNullOrWhiteSpace( e?.BoostType ) ) continue;
				if ( !e.BoostMultiplier.HasValue ) continue;
				if ( e.BoostMultiplier.Value <= 0 ) continue;

				if ( !map.TryGetValue( e.BoostType, out var existing ) || e.BoostMultiplier.Value > existing )
					map[e.BoostType] = e.BoostMultiplier.Value;
			}
			_boostsByType = map;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[LiveEventManager] RefreshAsync failed: {ex.Message}" );
		}
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;
		var go = scene.CreateObject();
		go.Name = "LiveEventManager";
		go.Components.Create<LiveEventManager>();
	}
}
