namespace Beastborne.Core;

/// <summary>
/// Single source of truth for the shipped game version. Every UI surface that
/// prints a version (main-menu pill, Menu popup, Credits, Feedback auto-fill)
/// reads this constant — bump it HERE (see .claude/commands/bump-version.md)
/// and nowhere else. Keep it in step with `target_version` in
/// Assets/data/patchnotes-pending.json. No leading "v" — callers add it.
/// </summary>
public static class BeastborneBuild
{
	public const string Version = "1.2.1";
}
