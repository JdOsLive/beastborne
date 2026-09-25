# UI BUILD PROTOCOL — the multi-agent page pipeline (2026-08-31)

User mandate: prebuilt, numbered reference docs + one agent per discipline, so pages
mass-generate consistently instead of each build re-deriving the laws.

## The docs (lenses over canonical truth — see lanes/INDEX.md)
DOC-1 layout · DOC-2 color · DOC-3 type/copy · DOC-4 motion · DOC-5 input ·
DOC-6 components · DOC-7 engine laws. Canonical sources stay authoritative
(guiding-star.md, laws.md, CLAUDE.md quirks, design-system cards; learnings-archive.md is the
grep-only dated journal); the DOCs are checklists with pointers. When a new law is learned, the
dated narrative goes in learnings-archive.md and the distilled rule in laws.md (or CLAUDE.md if it
applies to most tasks) FIRST, then the relevant DOC checklist gets one line pointing at it.

## The lanes (standing agents in .claude/agents/)
| # | agent | owns | runs |
|---|-------|------|------|
| 0 | ui-editor | KEEP/MERGE/CUT/ADD ledger (less is more) | before any brief/rebuild |
| 1 | ui-layout | structure: zones, slabs, headers, px math | first build pass |
| 2 | ui-motion | transitions, entrances, ladders, pops | after layout, ∥ with input |
| 3 | ui-input  | key grammar, one cursor, hover, routing | after layout, ∥ with motion |
| 4 | ui-color  | accent discipline, tokens, reservations | after motion+input |
| 5 | ui-audit  | consistency vs DOCs + sibling pages (headers!) | last, before screenshots |

## The pipeline for a page
1. ui-editor ledger (+ a systems-facts pass for anything data-driven) → BRIEF → user approves on Claude Design.
2. ui-layout builds the approved zone map; leaves `// LANE: motion` / `// LANE: input` markers.
3. ui-motion and ui-input run in parallel on the built structure (disjoint concerns, same files —
   sequence them if the page is one giant file and edits would collide; parallel only across files).
4. ui-color pass fixes palette violations in place.
5. ui-audit produces the ledger → responsible lanes fix their findings.
6. MAIN SESSION (always): compile via file-watcher + MCP, screenshot every state (dev_
   freeze commands are worth building), user reviews live, patch notes, commit per pass.

## Standing rules
- One lane per concern; a lane that wants to change another lane's territory reports, never edits.
- Agents cite DOC rules by number in their reports so drift is checkable.
- The main session owns: compiles, screenshots, commits, DesignSync pushes, memory, and
  resolving inter-lane disputes.
- File-collision rule: never two writing agents in the same .razor at once.
