namespace Beastborne.Data;

public enum TutorialPosition
{
	Center,
	TopLeft,
	TopRight,
	BottomLeft,
	BottomRight
}

/// <summary>
/// Defines a single step in the tutorial sequence
/// </summary>
public class TutorialStep
{
	/// <summary>
	/// Unique identifier for this step (e.g., "welcome", "monsters_intro")
	/// </summary>
	public string Id { get; set; }

	/// <summary>
	/// Title displayed in the tutorial tooltip
	/// </summary>
	public string Title { get; set; }

	/// <summary>
	/// Main message/description for this step
	/// </summary>
	public string Message { get; set; }

	/// <summary>
	/// Which tab this step relates to (optional, for highlighting)
	/// </summary>
	public string TargetTab { get; set; }

	/// <summary>
	/// Background image path for visual context
	/// </summary>
	public string BackgroundImage { get; set; }

	/// <summary>
	/// Where the tooltip should appear on screen
	/// </summary>
	public TutorialPosition Position { get; set; } = TutorialPosition.Center;

	/// <summary>
	/// If true, wait for player to perform an action before allowing "Next"
	/// </summary>
	public bool RequiresAction { get; set; }

	/// <summary>
	/// Hint text for what action the player should take (e.g., "Click MONSTERS tab")
	/// </summary>
	public string ActionHint { get; set; }

	// ============================================================
	// PHASE-2 EXTENSIONS — interactive walkthrough fields
	// ============================================================

	/// <summary>
	/// CSS-style class selector for the UI element to spotlight + point at.
	/// Examples: ".embark-btn", ".close-btn", ".help-subtab.active".
	/// Empty/null = no highlight, just centered info bubble.
	/// </summary>
	public string TargetSelector { get; set; }

	/// <summary>
	/// Event id that triggers automatic advance to the next step. Systems call
	/// `TutorialManager.NotifyEvent("foo")` when relevant actions happen; if it
	/// matches the current step's AdvanceEvent, the tutorial advances.
	/// Empty/null = "info-only" step, advance via Next button.
	/// Examples: "expedition.embarked", "battle.move-selected", "contract.confirmed".
	/// </summary>
	public string AdvanceEvent { get; set; }

	/// <summary>
	/// If true, certain systems should rig their outcome to succeed during this step.
	/// Currently used by ContractGenerator (force success) and ExpeditionManager
	/// (force easy enemy spawn).
	/// </summary>
	public bool RiggedSuccess { get; set; }
}
