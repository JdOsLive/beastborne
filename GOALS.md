# Beastborne — Goals

One page. What we're building toward right now, how we'll know it's done, and how we work.
Update it when a milestone closes or a goal changes. Targets marked *(proposed)* are
Claude's suggestions — confirm or edit them.

## North star
**Make the deep feel simple.** A monster-taming game with real depth (genetics, fusion,
teams) that feels effortless and exciting to navigate — on mouse, keyboard, or controller.
The beasts are the heroes; the UI is the frame.

---

## Current milestone — "One Game" UI overhaul
Plan: `.claude/ui-knowledge/ui-overhaul-brief.md` · Scores: `.claude/ui-knowledge/overhaul-audit.md`

**Done when:**
1. **Every in-scope screen scores ≥ 2 on all four audit axes** (cohesion, motion, input,
   identity), and every tab page scores 3 on input. *(Baseline 2026-09-25: most screens
   score 1–2; Expedition run screen 0/1/0/1; Feedback, Trading, Gift inbox, Quests, Profile
   score 0 on input.)*
2. **The core loop plays start to finish on keyboard only, and on controller only**
   (menu → roster → fusion → expedition → result → shop → back), with no mouse, no dead
   ends, no stuck states. Key caps show the active device's buttons.
3. **One of each shared piece:** one page shell (wave header), one modal shell, one
   selection indicator, one key-cap component, one element palette, one motion kit, one
   input pattern. No screen keeps a private copy.
4. **Shape language decided and applied** — card shape, corner radii, angles, selection
   indicator, button shapes, motion timings picked in one decision session and locked as
   tokens. Skew is *not* assumed: it stays only where the decision session says it earns
   its place (user, 2026-09-25: the skew angles mostly weren't working).
5. **Zero known input bugs** from the audit's quick-fix list.
6. **Main menu rebuilt** on the shared system (final phase). Battle screen untouched.

## Code-health goals (less code, less debt)
Smaller code is cheaper to change, cheaper for Claude to read, and has fewer places to
break. Measured by the scripts in `tools/`.

| Metric | Baseline (2026-09-25) | Target *(proposed)* |
|---|---|---|
| UI code size (`Code/UI` .razor + .scss) | ~141,000 lines (60K razor + 81K scss) | −25% by milestone end, while adding features |
| Probably-dead CSS classes (`tools/ui_deadcss.py`) | 663 (an undercount — classes used only by dead markup still count as used) | < 50 |
| Dead UI files / blocks | BreedingPanel, CardCollectionPanel, TamerCardComponent, GameHUD `@if (false)` bar (~270 razor + CSS), ~1,900 lines of unused Help renderers, ExpeditionPanel dead views, legacy result popup copy, temp `dev_*` commands | all removed (keep the useful `dev_*` freeze tools) |
| `ui_lint` errors (`tools/ui_lint.py --all`) | 196 (all `animation-delay`) | 0 in in-scope screens (battle/dormant excluded) |
| Private copies of shared pieces | cursor ×6+, key caps ×5+ styles, element tables ×16+ files, popup shells ×11 | 1 each |
| Largest file | MonsterRosterPanel: 7.3K razor + ~10K scss | split into sub-components, none > ~2K lines |
| Per-frame full re-renders (`GlobalFrame` in `BuildHash`) | Roster, Beastbook, GameHUD, menu starter select | only where a sprite actually animates |

**Rules of thumb:** delete before you add; a rewrite should end with fewer lines than it
started; no new private copy of a shared piece; run `tools/ui_lint.py` before committing UI
work (it only complains about NEW problems).

## Standing rules
- **Out of scope:** battle screen (BattleView), dormant features (Guild, Arena/PvP),
  balance/gameplay changes during the UI milestone.
- **No gems** anywhere in the UI — currency is Gold / Ink / Tokens.
- **Anti-gacha:** celebrate deterministic, visible results; never dramatize a roll.
- Engine facts live in `CLAUDE.md`; verify s&box behavior in the editor before building on it.

## How we work (thinking smart about credits)
- **One task per session** (one screen, one family, one fix list), then start fresh — long
  sessions re-read everything every turn. Hand off through committed docs, not chat.
- **Every prompt names "done"** (the screen + which checklist items), not "make it better".
- **Decide before building:** anything that changes layout or feel gets a quick mock or
  sketch approved first. Rework is the biggest cost we've had.
- **Visual work in a local session** with the s&box editor + MCP connected, so changes are
  seen, not guessed. Cloud sessions: planning, docs, scripts, mechanical code.
- **Right model for the job:** Opus for design decisions and tricky bugs; Sonnet for
  mechanical work (renames, token swaps, dead-code removal, grep audits). Parallel agents
  only for one-off sweeps like the audit.
- **Batch feedback:** play, collect notes + screenshots, send one list.
- **Machines check what machines can:** `tools/ui_lint.py` (engine-rule violations),
  `tools/ui_deadcss.py` (dead CSS). No credits spent on things a script can catch.
- Commit per step; one PR per phase; don't release mid-overhaul (patch notes keep
  accumulating in `Assets/data/patchnotes-pending.json`).
