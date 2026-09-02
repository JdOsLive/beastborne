namespace Beastborne.UI;

using Sandbox;

/// <summary>
/// Shared keyboard-nav input helpers for UI panels.
/// </summary>
public static class UiInput
{
	/// <summary>
	/// The ONE panel-nav confirm pair (2026-07-12, user: "e can be used as
	/// a confirm?"): Space is the PRIMARY (the key shown on caps/legends),
	/// E is the silent alternate for one-handed WASD+E play, Enter submits.
	/// Scope: panel TickInput nav contexts ONLY — pages that already give E
	/// its own meaning in a zone (collection grid
	/// fusion-toggle) consume E in an earlier branch, so order keeps those
	/// bindings intact. (Fusion's slot-switch left E on 2026-09-01 — it is Z/X
	/// now, so E confirms there like everywhere else.) Battle (E = bag) and
	/// world interact are untouched —
	/// they never route through this helper.
	/// </summary>
	public static bool ConfirmPressed()
		=> Input.Pressed( "Jump" ) || Input.Pressed( "Enter" ) || Input.Pressed( "Use" );
}
