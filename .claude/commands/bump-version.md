Bump the game version across all UI files.

Ask the user: "What version are we updating to? (e.g. v0.5.0)" and "What's the update name? (e.g. Shop Update)"

Then update ALL of these locations with the new version:

1. **MainMenu.razor** — Alpha badge (line with `class="alpha-badge"`):
   Change to `ALPHA v{VERSION}`

2. **MainMenu.razor** — Splash text array entry that contains the current version:
   Change to `v{VERSION} - {UPDATE_NAME}!`

3. **GameHUD.razor** — Version number display (line with `class="version-number"`):
   Change to `v{VERSION}`

4. **CreditsPanel.razor** — Credits version (line with `class="version-text"`):
   Change to `Beastborne v{VERSION}`

Do NOT modify:
- Roadmap/changelog entries (historical versions like v0.1.5, v0.2.0, etc.)
- Code comments referencing old versions (like migration notes)
- The "ALPHA" label text or the `class="version-alpha"` line

After making changes, show a summary of all files updated.

$ARGUMENTS
