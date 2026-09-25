# Integrating the cloud branch (claude/beastborne-megarougelite-migration-mqhi6q)

**Written 2026-09-25 by the cloud session, for the LOCAL session that has the s&box MCP.**

## What happened
This branch was built in a cloud session starting from GitHub `main` @ `8820029` (the
2026-08-19 laptop handoff). The desktop's local `main` has ~222 commits since then that were
never pushed, so **every code change here was written against stale code**. The Phase 0
audit (`.claude/ui-knowledge/overhaul-audit*.md`) also scored that stale code.

## Recommended order
1. Push local `main` to GitHub first (backup; should fast-forward from `8820029`).
2. Integrate on a new branch off local `main` (e.g. `integrate/cloud-2026-09-25`), **by intent,
   not by blind merge**: bring docs/tools over as-is, then re-check each code commit against the
   current code — skip what local work already solved or rewrote, re-apply the rest. Compile
   (MCP `compile_status`) after each code commit and run its in-editor check.
3. Re-run the Phase 0 audit's quick checks against the real code (scores/line refs may be stale).
4. Remove the TEMP `[bagdbg]` log (commit 2093019) once the Bag/Q issue is understood.

## Commits, by kind (oldest first)

### A. Docs / knowledge / tools — take as-is (merge-check `CLAUDE.md`, `.claude/**` against local edits)
| Commit | What |
|---|---|
| 39fd020 | CLAUDE.md slimmed 36→10 KB; PixelLab + Discord guides moved into `/monster-prompt`, `/patch-notes`; `balance-knowledge/decisions-summary.md`; agent read-lists (grep big files); settings/mcp cleanup |
| d95f544 | `learnings.md` → `learnings-archive.md` (grep-only); `css-quirks.md` archived; references repointed. ⚠ If local appended to `learnings.md` since 08-19, append those entries to `learnings-archive.md` and fold durable ones into `laws.md` |
| 8ee5a3f | New `ui-knowledge/laws.md` (117 distilled rules) |
| b5311d1 | `guiding-star.md` re-synced to 2026-08-19 code (re-check vs local changes since) |
| faae7b5, 2cea461, 1ebf552 | `ui-overhaul-brief.md` + user scope decisions (main menu rebuild last; battle off-limits; Expedition/Quests popup/PawPad unfinished) |
| aff19a4 | CLAUDE.md s&box 26.09 banner + `ui-knowledge/sbox-26-09-changes.md` (engine research) |
| c281680 | `GOALS.md`, `tools/ui_lint.py` (+baseline), `tools/ui_deadcss.py` |
| f64181f | Phase 0 audit (`overhaul-audit.md` + `-details.md`) — scored stale code |
| a8b76ea | Plan: reward system + currency list + pixel-art icons (brief/GOALS) |

### B. Code — re-check against local before applying (each needs compile + in-editor test)
| Commit | Intent | User decision? |
|---|---|---|
| 27dac6d | M closes the PawPad; R on Skills/Beastbook/fusion doesn't also toggle Radio (`GameHUD.PanelHandledPowerKey`) | — |
| a2ffeee | `UiFocusGuard` — clear non-text engine focus each frame (26.09.08 made `<button>` focusable → keybinds die after a click) | — |
| 2c142fc | Quests / daily+weekly bonus / streak pay Tokens (BossTokens), not gems | yes |
| 1e62f94 | Input core: Q at page top-level opens system menu (retires dead `IsNavigatingTabs` mode); skip input on modal-close frame; `PageCapturesInput`; `IsTopModal` false on the frame the top modal changes; retired floating chat no longer opens on T/Enter; R/C → phone apps; leave-expedition keyboard; in-page popups capture keys; ZONE CLEARED before result popup; fusion wording (ABOVE/ON TARGET/BELOW); Shop two-stage buy + Z/X caps; Leaderboard K/M + gold ×1000 + spinner | phone-only chat/radio/effects: yes |
| 8f40af3 + f26bbc6 | Gems → Tokens 1:1 everywhere (achievements, gifts) + save conversion (gated on `Gems > 0`, cap 4000); regional Hard tokens removed → +5 Tokens first Hard clear; Help currency copy | yes |
| 2222d53 | Economy A1–A4: one `MissionManager.RankedEnabled`, no arena quests while off; Token Collector rounds up; boss-shop Ink×5 removed; boss repeat tokens Normal 2 / Elite 3 | yes |
| e94d2ea | Feedback + Trading keyboard; Gift inbox keyboard + `AnyVisible`; tutorial retargeted to PawPad; `Core/BeastborneBuild.cs` version constant; PLAY/starter guards; starter A/D order; Options label; contract number keys → `SelectOption` | — |
| 18d6af8 | `ChatPanel.IsChatFocused` derived from real engine focus (was a sticky flag) | — |
| 2093019 | TEMP `[bagdbg]` diagnostic — remove after use | — |
| 5cbf165 + 4f40e45 | Switching app closes Bag/Quests/Profile; **achievement restart**: 15 fixed-target achievements, one-time restart gated on `Tamer.AchievementSetVersion` (pay unclaimed old rewards, clear progress, strip old titles, retroactive claimable unlocks), new hooks (TributesOffered / BeastbookDiscovered / PatternsDiscovered), login-milestone titles registered | yes |
| 9b26ea5 | Help: Achievements section rewritten for the 15 | — |

Decision records: `.claude/balance-knowledge/decisions-summary.md`, `currency-audit-2026-09.md`,
`achievement-trim-2026-09.md`, and `overhaul-audit.md` §Decisions.
