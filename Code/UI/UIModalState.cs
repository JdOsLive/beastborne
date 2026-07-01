using Beastborne.Core;
using Beastborne.UI.Components;
using Beastborne.UI.Panels;

namespace Beastborne.UI;

/// <summary>
/// Central read-only view of "is a blocking modal open right now?".
///
/// Phase 0 of the keyboard-navigation work. <see cref="GameHUD"/> and
/// <see cref="Beastborne.UI.Panels.ExpeditionPanel"/> early-return from their
/// keyboard handlers when <see cref="AnyModalOpen"/> is true, so a popup that
/// is on screen owns the keyboard instead of fighting the panel underneath it.
///
/// Each popup exposes its visibility differently (static <c>IsVisible</c>,
/// static <c>Instance</c> + instance <c>IsVisible</c>, <c>IsExpanded</c>, ...)
/// so every read here is null-safe and routed through whichever accessor that
/// popup actually has.
///
/// Popups that genuinely block input (centered modals, fullscreen panels) are
/// counted. Non-blocking HUD widgets (chat, radio, notification tray, active
/// effects) are intentionally NOT counted — they don't capture the keyboard.
///
/// Covered: ConfirmDialog, MenuPopup, OptionsPanel, PhoneLauncher, TeamPickerPopup,
/// ExpeditionResultPopup, BeastShowcasePopup, TamerCardShowcasePopup,
/// InventoryPanel, ProfilePanel, AchievementPanel, DailyPanel, QuestPanel,
/// HelpPanel, FeedbackPanel, CreditsPanel, TradingPanel, and the centered
/// (info-mode) TutorialPanel.
///
/// Deliberately skipped: GiftInboxPopup (not mounted in GameHUD's modal
/// stack, surfaced contextually), and all non-blocking HUD widgets.
/// </summary>
public static class UIModalState
{
	/// <summary>
	/// True when any blocking popup/modal is currently on screen. While true,
	/// GameHUD and panel keyboard handlers should early-return.
	/// </summary>
	public static bool AnyModalOpen => TopModal != null;

	/// <summary>
	/// Identifier of the highest-priority open modal, or null if none is open.
	/// Priority is the evaluation order below: the first match wins. A popup's
	/// own <c>Tick()</c> can compare against this to decide if it should react
	/// to the keyboard (only the top modal consumes input).
	///
	/// ConfirmDialog is checked FIRST — it is always a true blocking yes/no and
	/// can legitimately appear stacked on top of another panel.
	/// </summary>
	public static string TopModal
	{
		get
		{
			// ── Highest priority: the confirm yes/no. It can stack on anything. ──
			if ( ConfirmDialog.IsVisible ) return "ConfirmDialog";

			// ── System / menu layer ──
			if ( MenuPopup.Instance?.IsVisible == true ) return "MenuPopup";
			// OptionsPanel is mounted under BOTH MainMenu and GameHUD, so a
			// single Instance.IsVisible read is unreliable — use AnyVisible.
			if ( OptionsPanel.AnyVisible ) return "OptionsPanel";
			// Phone launcher — the experimental nav router. Can only open
			// when no other modal is up (GameHUD's hotkey handler is gated
			// on AnyModalOpen), so it never fights the panels below.
			if ( PhoneLauncher.IsVisible ) return "PhoneLauncher";

			// ── Team picker — blocking modal opened from the world map ──
			if ( TeamPickerPopup.IsVisible ) return "TeamPickerPopup";

			// ── Reward / showcase popups ──
			if ( ExpeditionResultPopup.IsVisible ) return "ExpeditionResultPopup";
			if ( BeastShowcasePopup.Instance?.IsVisible == true ) return "BeastShowcasePopup";
			if ( TamerCardShowcasePopup.Instance?.IsVisible == true ) return "TamerCardShowcasePopup";

			// ── Fullscreen info / collection panels ──
			if ( InventoryPanel.IsVisible ) return "InventoryPanel";
			if ( ProfilePanel.IsVisible ) return "ProfilePanel";
			if ( AchievementPanel.IsVisible ) return "AchievementPanel";
			if ( DailyPanel.IsVisible ) return "DailyPanel";
			if ( QuestPanel.IsVisible ) return "QuestPanel";
			if ( HelpPanel.Instance?.IsVisible == true ) return "HelpPanel";
			if ( FeedbackPanel.Instance?.IsVisible == true ) return "FeedbackPanel";
			if ( CreditsPanel.Instance?.IsVisible == true ) return "CreditsPanel";
			if ( TradingPanel.Instance?.IsVisible == true ) return "TradingPanel";

			// ── Tutorial — only the centered "info" speech bubble is blocking;
			//    the "action" top-banner hint is non-blocking and not counted. ──
			if ( TutorialManager.Instance?.IsTutorialActive == true )
			{
				var step = TutorialManager.Instance.CurrentStep;
				if ( string.IsNullOrEmpty( step?.AdvanceEvent ) ) return "TutorialPanel";
			}

			return null;
		}
	}

	/// <summary>
	/// Convenience: true when <paramref name="modalId"/> is the top-priority
	/// open modal. A popup's <c>Tick()</c> uses this to gate its keyboard input
	/// so a lower modal in the stack stays inert.
	/// </summary>
	public static bool IsTopModal( string modalId ) => TopModal == modalId;
}
