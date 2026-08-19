---
name: onedrive-stale-compiles
description: "The repo lives in OneDrive; OneDrive mid-write syncs cause phantom compile errors — don't trust \"impossible\" errors, don't re-propose moving."
metadata: 
  node_type: memory
  type: feedback
  originSessionId: d451fa20-5528-48d8-b237-b196569b2c27
---

The Beastborne repo lives under `OneDrive\Documents\s&box projects\beastborne`. OneDrive syncs files mid-write, so the s&box editor sometimes compiles a **half-synced copy** → phantom errors: a field reported "does not exist" that IS declared on disk, `border-style: solid` flagged at line numbers that don't match the current file, `*.tmp` lock files, "file in use by another process."

**Why:** OneDrive uploads/locks files while the editor reads them; the on-disk file is correct but the compiler saw an inconsistent snapshot.

**How to apply:** When a compile error references something that clearly exists/is-correct on disk, treat it as a **stale-sync artifact**, not a real bug — verify against the file with grep/Read, then have the user do a **clean recompile** (restart editor) rather than "fixing" correct code. The user has DECLINED moving the repo out of OneDrive (tried once, the move failed on editor/OneDrive file locks) and says pausing OneDrive won't help — **do not keep proposing the move**. Verification happens on the user's/friend's machine with the live editor; this Claude environment has no s&box editor (sbox MCP only connects when their editor is running). See [[ui-redesign-state]].

**SECOND FAILURE MODE — DEHYDRATION (2026-06-09, much worse):** OneDrive Files-On-Demand silently evicted **1,624 source files to cloud-only placeholders** (Assembly.cs, the csproj, whole Code/ folders). Symptoms: editor boots with a STALL warning, **zero Compiler lines in sbox-dev.log** (compile never runs at all — no CS errors!), `Missing Component: couldn't find Component type Beastborne.*` on every scene object, and the game opens as a **blue screen** (raw scene, no UI). DIAGNOSIS: `Get-ChildItem -Recurse -File | Where { $_.Attributes -match "Offline" }` — any hits = dehydrated. FIX: `attrib +P "<repo>\*" /S /D` (pin = always-keep-on-device; rehydrates in seconds), plus `attrib +P` on `.git\*` and `.sbox\*` separately (attrib skips hidden roots). Then restart the editor. Worth re-checking the Offline count whenever "impossible" boot failures appear.
