# Beastborne — Machine Handoff Guide

Written 2026-08-19, for moving development from the laptop to the desktop. This covers **getting the environment running** and **restoring Claude Code's local state**. For what the game *is*, read `DEV_ONBOARDING.md`; for project rules + s&box quirks, read `CLAUDE.md`.

---

## 1. What's in this push

This push carries **~255 commits of local-only work** (June–August 2026) that never left the laptop, plus the final uncommitted session. Highlights:

- The full **2026-06 UI overhaul**: Concept C main menu, Persona scene-swap transition, wave video background, living selection cursor
- The **Fable 5 panel sweep**: every in-game panel redone (Beasts Center Stage, Beastbook Specimen Hall, Shop, Bag, Quests, Skill Tree, Online hub, popups)
- **PawPad phone launcher** (bottom bar retired) with in-phone apps (chat/radio/effects/alerts), liquid expansion transitions
- **Cross-species fusion** (`Code/Core/FusionPatterns.cs`) + the Loom-Helix ritual animation
- **Notifications → phone ALERTS** (toasts deleted as a category)
- Keyboard **input grammar** (WASD nav, E confirm, Q back, R power action, Z/X cycle, M phone) via `Code/UI/UiInput.cs`
- The **design-system bundle** (`.claude/design-system/`) + all UI knowledge docs
- Content: beasts #16–20, Weaver Region identity pass, signature moves, balance pass (see `Assets/data/patchnotes-pending.json` for the running player-facing list, target v1.2.1)

**⚠️ This is pushed to GitHub for transfer purposes — it has NOT been released to players.** Pending patch notes have not been rolled. Don't run the `patch-notes` release flow until you're actually shipping.

---

## 2. Desktop setup

### Clone the repo

```powershell
git clone https://github.com/JdOsLive/beastborne.git
```

**Strong recommendation: clone to a plain local path (e.g. `C:\dev\beastborne`), NOT inside OneDrive.** The laptop repo lived in OneDrive and it caused a summer of pain: stale mid-edit file snapshots served to the compiler ("impossible" compile errors), and full-on file dehydration (1,624 files went cloud-only → blue screen, zero compiler output). See `.claude/memory/onedrive-stale-compiles.md`. A fresh clone on a plain path makes that entire class of bug disappear.

### s&box

1. Install s&box via Steam.
2. In the s&box editor, add the project via `megarougelite.sbproj` at the repo root. (The csproj/sbproj name is a relic of the original roguelite design — the game is Beastborne.)
3. Custom fonts are in `Assets/fonts/` at the root (required location — s&box doesn't scan subdirectories) and ship with the repo. Nothing to install.
4. The Iconify addon (lucide icons) is a Library dependency — check `Libraries/` resolves on first open; new icon names need a manual addon refresh.

### Claude Code

1. Open the cloned repo folder in Claude Code (or the VS Code extension). `CLAUDE.md`, `.claude/agents/`, `.claude/commands/` (the skills: patch-notes, wiki-sync, bump-version, monster-prompt, feedback-report), `.claude/settings.local.json`, and `.mcp.json` all ship in the repo — those work immediately.
2. **Restore the memory** (the one thing that doesn't transfer automatically) — see section 3.
3. **MCP**: `.mcp.json` configures the official s&box editor MCP at `http://127.0.0.1:7269/mcp` — approve it at session start when the editor is running. It provides `compile_status`, `read_console`, `camera_screenshot`, etc. The `sbox-oz-legacy` entry (port 8098) is dead and can be deleted from `.mcp.json`.

---

## 3. Restoring Claude's memory

Claude Code keeps a per-project memory at `~/.claude/projects/<path-slug>/memory/`. That directory is machine-local — so a snapshot of the laptop's memory (7 files, taken 2026-08-19) is committed in this repo at **`.claude/memory/`**.

On the desktop, after the first Claude Code session in the cloned repo (which creates the new project directory), copy the snapshot in:

```powershell
# Find the new project dir (slug is derived from the repo path on THIS machine)
$proj = Get-ChildItem "$env:USERPROFILE\.claude\projects" -Directory |
        Where-Object Name -like "*beastborne*" | Select-Object -First 1
New-Item -ItemType Directory -Force "$($proj.FullName)\memory"
Copy-Item ".claude\memory\*.md" "$($proj.FullName)\memory\"
```

Or simply tell Claude in the first session: *"Restore your memory from `.claude/memory/` in this repo."*

What's in the snapshot:
- `MEMORY.md` — the index (one line per memory)
- `ui-redesign-state.md` — **the big one**: the whole June–August UI saga, session by session, with pending items
- `live-vs-dormant-features.md` — PvP/Guild Raids are coded but NOT live
- `living-selection-cursor.md`, `angled-ui-featured-card.md` — main-menu implementation lore
- `sbox-mcp-official.md` — MCP workflow notes
- `onedrive-stale-compiles.md` — laptop-specific; ignore entirely if the desktop clone isn't in OneDrive (recommended)

Going forward the desktop's live memory will drift from this snapshot — that's fine. If you ever switch machines again, re-copy the live memory into `.claude/memory/` and commit before pushing.

---

## 4. Current state / open threads (as of 2026-08-19)

Things in flight when the laptop was parked — the detailed context for all of these lives in `.claude/memory/ui-redesign-state.md` and `.claude/ui-knowledge/learnings.md`:

**Needs live verification (committed, not yet seen running):**
- Fusion end-to-end on a real fuse (inline result centering + ledger layout landed after the last verified capture)
- Keyboard-hands checks MCP couldn't do: M-recovery on skills page, R rotate, fusion tile 2D nav, walkup seams, E-confirm, hold-F reset amber sweep
- Trading / showcases / Tutorial / Credits / Achievements / Daily / GiftInbox panels (compile-clean + static-verified only)

**Pending cleanups:**
- `SWAP_SLOWMO → 1f`, `SWAP_DEBUG → false` in `MainMenu.razor`
- `BuildHash` still carries `SpriteAnimator.GlobalFrame` on the menu → re-renders every frame (cheap fps win)
- Temp dev commands to remove after approval: `dev_skillspage`, `dev_notify`, `dev_alerts`, `dev_beastbook` (keepers: `dev_fusefx`, `dev_patternbook`, `dev_givebeast`)
- Stale "< >" hint chips on Shop/Online (cycle is Z/X now)
- `BreedingPanel` + `CardCollectionPanel` render nowhere = dead code, flagged for deletion

**Open design questions (user rulings needed):**
- Feedback panel says "Gold & Gems" — gems exist in code but guiding-star says no gems
- Chat popup vs in-phone chat app redundancy (consolidation candidate)
- Rolling the Exo2 weight ladder + 550 stat scale + one-cursor model out to remaining panels
- Ranked Arena revival (wants its own dedicated session)

**Standing rules that survive the machine move:**
- BattleView / battle HUD is OFF-LIMITS for restyling
- Don't sweep dormant GuildPanel / ArenaPanel (features not live)
- Restart the game after agents land UI code — hotload lambda debris causes dead onclicks / fps craters
- Canonical style spec: `.claude/ui-knowledge/guiding-star.md`; engine law journal: `.claude/ui-knowledge/learnings.md`; both are current

---

## 5. Where everything lives

| Thing | Location |
|---|---|
| Project rules + s&box CSS quirks table | `CLAUDE.md` |
| Game/systems onboarding | `DEV_ONBOARDING.md` |
| Claude memory snapshot | `.claude/memory/` (restore per section 3) |
| Style spec (canonical) | `.claude/ui-knowledge/guiding-star.md` |
| Engine laws / hard-won lessons | `.claude/ui-knowledge/learnings.md` |
| Panel inventory + sweep state | `.claude/ui-knowledge/panel-inventory.md` |
| Design-system live-HTML bundle | `.claude/design-system/` |
| Balance knowledge + decisions log | `.claude/balance-knowledge/` |
| Custom agents (balance, sbox-ui, input) | `.claude/agents/` |
| Skills / slash commands | `.claude/commands/` |
| Pending patch notes (v1.2.1) | `Assets/data/patchnotes-pending.json` |
| MCP config | `.mcp.json` |
