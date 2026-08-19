---
name: sbox-mcp-official
description: "Official s&box editor MCP (port 7269) replaced the oz_mcp community addon — approve on session start; gives compile_status/read_console/screenshots, ending the PowerShell capture-drive era."
metadata: 
  node_type: memory
  type: project
  originSessionId: d199b3ba-a286-42bc-aea4-5d0540e2de3d
---

2026-07-06: `.mcp.json` now points the primary `sbox` entry at the OFFICIAL in-editor MCP server (`http://127.0.0.1:7269/mcp`). The old community addon (oz_mcp, port 8098) is kept as `sbox-oz-legacy` — safe to delete that entry plus `Libraries/ozmium.oz_mcp` once the official one is proven.

**Why it matters:** the official server exposes `editor_status`, `read_console`, `compile_status`, `screenshots`, scene editing, and play-mode control — which replaces the fragile PowerShell SetProcessDPIAware/AppActivate/CopyFromScreen capture-drive workflow AND the focus-stealing problem (no more aborting because the user is in Chrome/Roblox). Compile errors become directly readable instead of the user pasting console logs.

**How to apply:** approve the MCP connection when Claude Code prompts on session start in this folder. The server lives in the editor and drives whichever project is open — verify under Editor → Preferences → MCP Server if tools don't appear. Custom game tools can be added with `[McpTool]`.

Caveats from the old server still worth respecting until re-verified: MCP stop/start play-mode once ZOMBIED the editor (see [[onedrive-stale-compiles]] era notes in [[ui-redesign-state]]) — prefer verifying the official server's play-mode controls gently before relying on them.

**2026-07-09 — PROVEN WORKFLOW (first live session):** connected mid-session after the user ran /mcp. The verification loop that ACTUALLY works:
- **UI screenshots**: `console_command` with `screenshot` → file lands in `C:\Program Files (x86)\Steam\steamapps\common\sbox\screenshots\sbox.<timestamp>.png` (path printed to console) → Read it. Full-res, pixel-perfect UI, NO editor focus needed. `camera_screenshot` (even with includeUi:true) does NOT capture screen-space UI — it renders the camera offscreen (empty world only). This retires the PowerShell CopyFromScreen rig for STATE captures; input driving (clicks/keys) still needs focus + SendKeys.
- **Compile**: `compile_status` — per-compiler errors/warnings with file:line; the editor's file watcher recompiles on disk changes WITHOUT focus (no more mtime-bump + AppActivate dance).
- **Console**: `read_console` with minimumLevel/filter — caught a live `lucide:cactus` 404 (nonexistent icon in TraitDatabase barbed_hide → fixed to lucide:shell). Icon 404s surface here; check after adding lucide icons.
- **play_start/play_stop** exist; play_start errors cleanly if already playing (no zombie observed, but stop/start still untested — stay gentle).
- Tools are DYNAMIC (hotload registry): `search_tools` for discovery, `call_tool` to invoke; 52 tools at first connect incl. scene editing, asset read/write, undo.

**CORRECTION (same day, user-prompted):** `camera_screenshot` DOES capture the full screen-space UI — two conditions: (1) the GAME must be playing and you target the RUNNING scene camera (easiest: omit the `camera` arg — it resolves the active scene, which is the play instance while playing; my "blank" attempt had explicitly targeted the EDIT document's parked camera), and (2) **render at the game view's native resolution (1920×1080)** — at mismatched sizes the UI rasterizes wrong (text → grey blocks, pixel-art `<img>` contain-fit draws zoomed/offset chunks; both artifacts vanish at native res). So the ONE-CALL loop is `camera_screenshot {width:1920, height:1080, includeUi:true}` → inline image. `console_command screenshot` → read from the engine screenshots dir remains the fallback/second opinion.
