namespace Beastborne.UI;

using Sandbox;
using Sandbox.UI;

/// <summary>
/// Keeps engine keyboard focus off everything except text inputs.
///
/// s&amp;box 26.09.08 (Panel focus traversal) made <c>&lt;button&gt;</c> (and Checkbox/Tab)
/// set <c>AcceptsFocus = true</c>: clicking one focuses it, and while a panel with the
/// default <c>ButtonInput</c> (UI) holds focus the engine routes keys to the UI instead
/// of the game — every <c>Input.Pressed</c> hotkey (M, 1–6, WASD nav…) dies until focus
/// moves. Same failure as the CLAUDE.md "never set AcceptsFocus" law, now triggered by
/// every button click. It also made Space/Enter "click" the last-clicked button, which
/// would double-fire with <see cref="UiInput.ConfirmPressed"/>.
///
/// Our keyboard/controller navigation is driven entirely by the Input action system
/// (GameHUD TickInput routing, UIModalState), never by engine focus — so any non-text
/// focus is cleared as soon as it lands. Text inputs (TextEntry — anything accepting IME
/// input) keep focus so typing still works.
///
/// Call once per frame from each top-level UI host (GameHUD, MainMenu). Cheap: one
/// property read when nothing is focused.
/// </summary>
public static class UiFocusGuard
{
	public static void Tick()
	{
		var focused = InputFocus.Current;
		if ( focused is null ) return;
		if ( focused.AcceptsImeInput ) return; // TextEntry — let the player type

		InputFocus.Clear();
	}
}
