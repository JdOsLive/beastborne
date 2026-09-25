Bump the game version across all UI files.

Ask the user: "What version are we updating to? (e.g. v0.5.0)" and "What's the update name? (e.g. Shop Update)"

Then update these locations:

1. **Code/Core/BeastborneBuild.cs** — `public const string Version = "{VERSION}";` (no leading "v").
   This is the SINGLE source: the main-menu version pill, the featured-card kicker
   (`UPDATE {VERSION}`), the Menu popup version chip, the Credits footer and the Feedback
   form's auto-filled version all read it. Do not hard-code a version string anywhere else.

2. **Code/UI/MainMenu.razor** — `featuredKicker`: update the update NAME after the version
   (`$"UPDATE {BeastborneBuild.Version} · {UPDATE_NAME}"`) and refresh the featured copy fields
   below it if the release has a new headline.

3. **Assets/data/patchnotes-pending.json** — `target_version` should match.

4. **Code/UI/Components/LighthouseScene.razor** — the Patch Notes scene's version hero
   (`lhd-rail-v-num`) is hand-written together with its notes; update it only when the notes
   content is updated for the new release.

Do NOT modify:
- Roadmap/changelog entries (historical versions like v0.1.5, v0.2.0, etc.)
- Code comments referencing old versions (like migration notes)
- The "ALPHA" label text or the `class="version-alpha"` line

After making changes, show a summary of all files updated.

$ARGUMENTS
