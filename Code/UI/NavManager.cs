using System;

namespace Beastborne.UI;

/// <summary>
/// Static navigation router — the seam between "something wants to navigate"
/// and "the chrome that actually mounts panels" (Fable 5 sweep, Phase 1).
///
/// WHY: the guiding-star navigation model (phone launcher, per-destination
/// accents, eventual 2D map) needs nav TRIGGERS decoupled from the bottom bar
/// so the bar can be retired without rewiring every caller. Panels, hotkeys,
/// the command bar, and the PhoneLauncher all route through here; GameHUD
/// stays the single HOST that owns the guarded tab switch (live-expedition
/// confirm, background-mode handoff, tutorial notify) and the overlay
/// open/toggle calls.
///
/// FLOW:
///   caller → GoTo/GoToIndex/OpenOverlay
///          → registered host delegate (GameHUD.RequestTabSwitch / RequestOverlay)
///          → GameHUD's existing guard + switch logic (behavior identical)
///          → host calls NotifyTabChanged() when a switch actually LANDS
///          → CurrentTab/Version update, Changed fires.
///
/// Version is a monotonically increasing change counter — panels that render
/// nav-dependent chrome add it to BuildHash() so they re-render on any nav
/// event without subscribing to the C# event.
/// </summary>
public static class NavManager
{
	/// <summary>
	/// Tab registry, in number-key order (1-6). Must match GameHUD's tabs
	/// array — GameHUD asserts the mapping by registering itself as host.
	/// </summary>
	public static readonly string[] Tabs = { "monsters", "skills", "expedition", "online", "beastiary", "shop" };

	/// <summary>
	/// Overlay ids the host understands. Kept here as the canonical list so
	/// callers don't invent strings:
	///   "phone"        — PhoneLauncher (toggle)
	///   "system-menu"  — MenuPopup (resume / options / quit)
	///   "quests"       — QuestPanel (toggle)
	///   "bag"          — InventoryPanel (toggle)
	///   "profile"      — ProfilePanel
	///   "chat" / "radio" / "effects" / "notifications" — the floating widgets (toggle)
	/// </summary>
	public const string OverlayPhone = "phone";
	public const string OverlaySystemMenu = "system-menu";
	public const string OverlayQuests = "quests";
	public const string OverlayBag = "bag";
	public const string OverlayProfile = "profile";
	public const string OverlayChat = "chat";
	public const string OverlayRadio = "radio";
	public const string OverlayEffects = "effects";
	public const string OverlayNotifications = "notifications";

	/// <summary>The tab currently mounted by the host. Updated via NotifyTabChanged.</summary>
	public static string CurrentTab { get; private set; } = "monsters";

	// ── Same-frame double-fire latch ─────────────────────────────────────
	// Input.Pressed is EDGE-TRIGGERED and reports true to EVERY handler that
	// polls it in the same frame (the PanelHandledBackKey lesson). When two
	// keyboard handlers can both route the same key in one frame (GameHUD's
	// hotkeys + the PhoneLauncher's direct-route keys, across the modal
	// open/close boundary), the SECOND one must yield. Any successful route
	// stamps this; handlers check RoutedThisFrame before acting on nav keys.
	private static float _lastRouteAt = -100f;

	/// <summary>True if a route already fired this frame (Time.Now is constant
	/// within a frame; the small window also absorbs phase boundaries).
	/// ⚠ The delta MUST be sign-checked: statics survive across editor play
	/// sessions while Time.Now resets to ~0 on a new session, so a stale
	/// _lastRouteAt from late in the previous session makes the delta NEGATIVE
	/// — an unchecked "&lt; 0.05f" then wedges TRUE for as long as the previous
	/// session ran, early-returning both keyboard handlers (live symptom:
	/// every number hotkey dead, mouse clicks fine). Negative delta = time
	/// went backward = not this frame.</summary>
	public static bool RoutedThisFrame
	{
		get
		{
			var dt = Time.Now - _lastRouteAt;
			return dt >= 0f && dt < 0.05f;
		}
	}

	/// <summary>
	/// Monotonic nav-change counter. Bumps on every committed tab change and
	/// every overlay open routed through the router. Panels hash this.
	/// </summary>
	public static int Version { get; private set; }

	/// <summary>Fires after a tab change commits (post-guard). Arg: new tab id.</summary>
	public static event Action<string> TabChanged;

	// ── Host registration ────────────────────────────────────────────────
	// HOTLOAD LAW (the PlaneTween lesson): delegates stored by long-lived
	// statics can go stale on s&box hot reload — closures DIE ("Unable to
	// find matching substitution for a lambda method"), method groups
	// substitute cleanly. Contract:
	//   1. Hosts register METHOD GROUPS ONLY (RegisterHost(HandleNavTab, ...)),
	//      never lambdas/closures.
	//   2. Hosts RE-REGISTER every frame from their update tick (idempotent
	//      two-field write) — a hotload-replaced host instance re-owns the
	//      router within one frame, so a stale delegate can never linger.
	//   3. Dispatch is null-tolerant AND exception-tolerant: a throw from a
	//      stale delegate clears the host instead of spamming per-call.
	private static Func<string, bool> _tabHandler;
	private static Func<string, bool> _overlayHandler;

	/// <summary>
	/// Register the chrome that performs navigation. tabHandler receives a
	/// tab id and runs the guarded switch (returns true if it handled the
	/// request — switched OR queued behind a confirm). overlayHandler
	/// receives an overlay id and opens/toggles it.
	/// METHOD GROUPS ONLY — see the hotload law above. Safe (and intended)
	/// to call every frame from the host's update tick.
	/// </summary>
	public static void RegisterHost( Func<string, bool> tabHandler, Func<string, bool> overlayHandler )
	{
		_tabHandler = tabHandler;
		_overlayHandler = overlayHandler;
	}

	/// <summary>Clear the host (only if it's still the registered one — a newer host wins).
	/// Delegate equality compares method + target, so a method group passed here matches
	/// the method group registered earlier by the same instance.</summary>
	public static void UnregisterHost( Func<string, bool> tabHandler )
	{
		if ( _tabHandler == tabHandler )
		{
			_tabHandler = null;
			_overlayHandler = null;
		}
	}

	/// <summary>Exception-tolerant dispatch — a stale (hotload-orphaned) delegate
	/// that throws gets cleared instead of spamming every subsequent call. The
	/// host re-registers on its next frame and routing self-heals.</summary>
	private static bool SafeInvoke( Func<string, bool> handler, string arg )
	{
		if ( handler == null ) return false;
		try
		{
			return handler( arg );
		}
		catch ( Exception e )
		{
			Log.Warning( $"NavManager: host delegate threw ({e.Message}) — clearing host, it re-registers next frame." );
			if ( _tabHandler == handler ) _tabHandler = null;
			if ( _overlayHandler == handler ) _overlayHandler = null;
			return false;
		}
	}

	// ── Routing ──────────────────────────────────────────────────────────

	/// <summary>Navigate to a destination tab (guards apply). False if no host / unknown tab.</summary>
	public static bool GoTo( string tab )
	{
		if ( string.IsNullOrEmpty( tab ) || IndexOf( tab ) < 0 ) return false;
		var handled = SafeInvoke( _tabHandler, tab );
		if ( handled ) _lastRouteAt = Time.Now;
		return handled;
	}

	/// <summary>Navigate by number-key index (0-5).</summary>
	public static bool GoToIndex( int index )
	{
		if ( index < 0 || index >= Tabs.Length ) return false;
		return GoTo( Tabs[index] );
	}

	/// <summary>Open/toggle a non-tab overlay (see Overlay* constants).</summary>
	public static bool OpenOverlay( string id )
	{
		var handled = SafeInvoke( _overlayHandler, id );
		if ( handled )
		{
			Version++;
			_lastRouteAt = Time.Now;
		}
		return handled;
	}

	/// <summary>
	/// Called by the HOST after a tab switch actually lands (post-guard,
	/// post-confirm). Never call from a nav trigger — that would report
	/// switches that a confirm dialog later cancels.
	/// </summary>
	public static void NotifyTabChanged( string tab )
	{
		CurrentTab = tab;
		Version++;
		TabChanged?.Invoke( tab );
	}

	/// <summary>Index of a tab id in number-key order, or -1.</summary>
	public static int IndexOf( string tab )
	{
		for ( int i = 0; i < Tabs.Length; i++ )
			if ( Tabs[i] == tab ) return i;
		return -1;
	}
}
