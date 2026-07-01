# Beastborne UI — Token layer (single source of colors / space / radii / motion)

The one place UI values are defined. Full rationale in `guiding-star.md`; this file is the
compact, copy-pasteable reference. **Goal:** kill the ~12,400 hardcoded color literals so a
recolor is a one-place edit, not a 56-file find-replace.

---

## ✅ SPIKE VERDICT (run 2026-07-01, in-engine): `var()` does NOT parse — **PATH B.**
Both halves rejected outright by the parser (sbox-dev.log):
`#ff00ff is not valid with --bb-spike` (custom property declaration) and
`var(--bb-spike, #ff8800) is not valid with background-color` (the function).
Per-property recovery meant the rest of each rule survived. Do NOT re-run.
**Path B strategy in force:** (1) `Code/UI/BbTokens.cs` static = the C# source of
truth (inline styles / interpolated Razor / dynamic tints); (2) SHARED PRIMITIVE
COMPONENTS are the real token mechanism for static chrome — a BbButton's SCSS is
written once, so its values ARE tokens; (3) per-file token-header comment banner
for panel SCSS that can't use a primitive, synced to this doc by discipline.

## ~~⚠️ GATING SPIKE~~ (kept for the record — do not re-run)

The token *mechanism* depends on this. s&box's SCSS files are each standalone-scoped
(no `@import`/`@use`/`@forward`, no shared `$variables` across files). So the ONLY way to
get true cross-file tokens usable inside `.scss` class rules is **CSS custom properties**.
As of this writing they are used ZERO times in the codebase and are NOT listed in the
26.06.03 changelog — so we must verify.

**Spike:** on any always-visible panel root (e.g. `GameHUD.razor.scss` root selector), add:
```scss
// TEMP SPIKE — delete after checking
.game-hud { --bb-spike: #ff00ff; }
.game-hud .command-bar { background-color: var(--bb-spike, #00ff00); }
```
Launch the game and look at the command bar + the s&box console:
- **Magenta bar** → `var()` WORKS. Use Path A below. (Best outcome — one-line recolors.)
- **Green bar** (fallback used) or a console `Unknown/Unsupported` error → `var()` does NOT
  work. Use Path B. Then remove the spike.

> Update this section with the result once known, so nobody re-runs the spike.

---

## Path A — CSS custom properties (IF the spike passes)

Define once on the top-most always-mounted root (GameHUD root for in-game, MainMenu root for
menu — or a shared parent if one exists), then reference `var(--bb-*)` in every panel's SCSS.

```scss
// Define on the root panel:
.app-root {
  // Surfaces
  --bb-bg:            #0a0912;   // app/page background
  --bb-bg-root:       #04060f;   // deepest (menu base)
  --bb-panel:         #15121f;   // panel slab
  --bb-card:          #1c1830;   // raised card
  --bb-card-fill:     rgba(20, 20, 35, 0.95);
  --bb-slot:          #131019;   // empty slot

  // Semantic accents (fixed meaning — never recolor for variety)
  --bb-gold:          #ffce3a;
  --bb-gold-lo:       #f59312;   // gold gradient end
  --bb-gold-ink:      #2a1605;   // dark text ON gold
  --bb-violet:        #9b6cff;   // selection / focus / "you"
  --bb-purple-deep:   #7b4ddb;
  --bb-orange:        #ee5421;   // live / embark / alert
  --bb-red:           #e0414a;   // destructive / release
  --bb-green:         #3fb45e;   // success / confirm
  --bb-blue:          #3f8fe0;   // info / neutral action
  --bb-discord:       #5865f2;

  // Text
  --bb-text:          #f4f1ea;
  --bb-text-dim:      rgba(214, 206, 236, 0.6);

  // Radii (rounder new scale)
  --bb-r-chip:        11px;
  --bb-r-button:      16px;
  --bb-r-card:        22px;
  --bb-r-hero:        26px;
  --bb-r-slab:        30px;
  // ring radius = host radius + 10 (concentric); pill = 999px

  // Spacing (4-based)
  --bb-s-xs: 4px;  --bb-s-sm: 8px;  --bb-s-md: 12px;
  --bb-s-lg: 16px; --bb-s-xl: 24px; --bb-s-2xl: 32px;

  // Motion
  --bb-ease-glide: cubic-bezier(0.22, 1, 0.36, 1); // the ring/signature ease
  --bb-t-snap:     0.14s;   // interaction feedback
  --bb-t-card:     0.16s;   // card hover
  --bb-t-glide:    0.27s;   // selector glide
}
```
Consume: `background-color: var(--bb-card);  border-radius: var(--bb-r-button);`.

## Path B — no `var()` (IF the spike fails)

No clean cross-file mechanism exists for `.scss` class rules. Options, best → worst:
1. **Per-file token header** — paste the value block as a comment banner + redeclare the ~15
   values you use at the top of each panel's SCSS. One place PER FILE (not one place total),
   but at least each file has a labeled palette instead of scattered magic numbers. Pair with
   this doc as the master list so all files stay in sync by discipline.
2. **C# `BbTokens` static** (`public const string Violet = "#9b6cff";`) — usable ONLY in
   inline `style=""` / interpolated Razor attributes, NOT in `.scss` class rules. Good for
   dynamic per-species/per-element tints already done inline (matches the existing
   `Hex2Rgba` pattern), useless for static chrome. Limited.
3. Accept documented discipline: this file is the source of truth, panels copy from it.

Either way, **this file's tables are the master list.** Recolor = edit here, propagate.

---

## Element palette — PENDING (Fable 5 to design)

The full 11-element palette (Fire · Water · Earth · Wind · Electric · Ice · Nature · Metal ·
Shadow · Spirit · Neutral) is being redesigned as ONE cohesive set. **Do not add element
tokens here until Fable 5 delivers the set.** Keep the two-tone dark-fill + bright-rim
rendering system. Shipped baseline to iterate from: `BeastiaryPanel.razor.scss:797-807`.

## Rarity ladder (stable)
Common `#9ca3af` · Uncommon `#22c55e` · Rare `#3b82f6` · Epic `#a855f7` · Legendary `#fbbf24` (dark text) · Mythic `#ec4899`.
