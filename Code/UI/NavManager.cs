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

	/// <summary>
	/// Monotonic nav-change counter. Bumps on every committed tab change and
	/// every overlay open routed through the router. Panels hash this.
	/// </summary>
	public static int Version { get; private set; }

	/// <summary>Fires after a tab change commits (post-guard). Arg: new tab id.</summary>
	public static event Action<string> TabChanged;

	// ── Host registration ────────────────────────────────────────────────
	// GameHUD registers on OnStart and unregisters on OnDestroy. The router
	// holds ONE host at a time; a re-register (hotload, scene reload) simply
	// replaces the delegates.
	private static Func<string, bool> _tabHandler;
	private static Func<string, bool> _overlayHandler;

	/// <summary>
	/// Register the chrome that performs navigation. tabHandler receives a
	/// tab id and runs the guarded switch (returns true if it handled the
	/// request — switched OR queued behind a confirm). overlayHandler
	/// receives an overlay id and opens/toggles it.
	/// </summary>
	public static void RegisterHost( Func<string, bool> tabHandler, Func<string, bool> overlayHandler )
	{
		_tabHandler = tabHandler;
		_overlayHandler = overlayHandler;
	}

	/// <summary>Clear the host (only if it's still the registered one — a newer host wins).</summary>
	public static void UnregisterHost( Func<string, bool> tabHandler )
	{
		if ( _tabHandler == tabHandler )
		{
			_tabHandler = null;
			_overlayHandler = null;
		}
	}

	// ── Routing ──────────────────────────────────────────────────────────

	/// <summary>Navigate to a destination tab (guards apply). False if no host / unknown tab.</summary>
	public static bool GoTo( string tab )
	{
		if ( string.IsNullOrEmpty( tab ) || IndexOf( tab ) < 0 ) return false;
		return _tabHandler?.Invoke( tab ) ?? false;
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
		var handled = _overlayHandler?.Invoke( id ) ?? false;
		if ( handled ) Version++;
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
