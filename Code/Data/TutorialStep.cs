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
}
