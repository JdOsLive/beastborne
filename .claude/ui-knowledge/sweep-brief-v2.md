# SWEEP BRIEF v2 — the full panel overhaul (2026-07-02)

The user commissioned redoing EVERY in-game panel. The two reference
implementations are the MAIN MENU (`Code/UI/MainMenu.razor`) and the
PAWPAD (`Code/UI/Components/PhoneLauncher.razor`) — both user-blessed.
"Safe reskins are a failure." You may restructure layout, add light
gameplay affordances, and change colors — WITHIN the language below.

## MANDATORY READING before you edit anything
1. `CLAUDE.md` → the s&box CSS quirks table (engine parser laws).
2. `.claude/ui-knowledge/learnings.md` → the LATEST sections first
   (PawPad v4–v7.4): the lean laws don't apply to flat panels, but the
   texture laws, razor generator laws, z-ordering, and alignment
   doctrine DO. Key ones you WILL hit:
   - NEVER write `@if`/`@foreach` inside an HTML comment (razor parses
     @-directives in comments — dangling codegen).
   - NEVER use `$"..."` interpolated strings inside markup attribute
     expressions — compute in a C# helper method, bind `@Helper(x)`.
   - Inline style properties are NEVER removed once emitted — always
     VALUE-SWAP (`box-shadow: @(x ? "8px 8px" : "0px 0px") ...`).
   - Ship UI art as WebP (PNG decode dulls alpha-blended colors);
     lossless WebP keeps alpha, LOSSY drops it.
   - `Panel.BuildHash()` must include every field that affects render.
   - Log compile SUCCESS is silent; only failures print.
3. `.claude/ui-knowledge/guiding-star.md` → button tiers/sizes, radii,
   type scale. USER MANDATE: button sizes follow the guiding-star /
   main-menu tiers exactly.

## THE LANGUAGE (capture, don't copy)
- **Surfaces**: near-black violet-tinted slabs (`#0a0912` bg, `#15121f`
  panel, `#1c1830` card) — SOLID fills, no glows, no hairline outlines
  around everything. Radii ROUNDER: 16 button / 22 card / 26 hero /
  30 slab (guiding-star).
- **Hardware chrome (the PawPad's gift)**: graphite gradient
  (`#3f434e → #24262e`) + **gold trim `#ffce3a`** is the "physical
  object" treatment — use it for hero containers, headers-as-devices,
  or featured cards where a panel wants a tactile anchor. Don't put it
  on everything.
- **THE LIVING VIOLET RING `#9b6cff` — USER MANDATE**: hover/selection
  on interactive cards/tiles = the liquid ring, not color washes. On
  FLAT panels (no 3D lean) the CHEAP recipe is: a `position: absolute`
  bordered div (4px `#9b6cff`, radius = host + 8, inset −10px via
  explicit offsets) that appears on the host's `:hover`/`.selected`
  (opacity swap) — plus the panel's ONE imperative waving ring on the
  keyboard-selected element if the panel has keyboard nav (copy
  UpdateRing/ApplyRingWave from PhoneLauncher; flat panels don't need
  ProjectLean — Box.Rect is layout space and flat panels paint at
  layout). Focused element may pop `translateY(-4~6px) scale(1.06)`;
  if it carries the ring, mirror the pop in the ring rect (SelectedPop
  lockstep).
- **Type**: Exo2Italic 800–900 UPPERCASE for display/headers (kickers
  12px violet, titles 28–52px), Exo2 700 non-italic 10–13px for labels.
- **Angles — USER MANDATE**: use the angled dialect wherever it makes
  sense: skewX(-8°) slab chips (see the new .hud-player-tag), ±3°
  stamps (BbStamp), 45° diamonds (BbDiamond), skewed header seams.
  Straight boxes everywhere = a failed panel. (Counter-skew inner
  content; remember an ancestor transform poisons derived widths —
  keep explicit px inside skewed containers.)
- **Sizing — USER MANDATE**: generous, guiding-star scale. Primary
  CTAs ≈ the menu's PLAY tier; headers 28–52px display; touch targets
  never dinky. When unsure, size UP.
- **Primitives exist — USE THEM**: `Code/UI/Primitives/` BbButton
  (5 tiers × 3 sizes — THE button), BbSectionHeader (kicker+title),
  BbStamp, BbDiamond. `BbTokens.cs` for C# color access.

## PER-PANEL ACCENT — USER MANDATE
Each panel wears ITS PawPad tile color as the page accent (section
rules, active states, CTA fills, header identity). The phone tile, the
launch sweep, and the panel are ONE identity:
- Beasts/roster `#7b4ddb` · Skills `#ff6bd6` · Expedition `#ee5421`
- Online `#3f8fe0` · Beastbook `#2dd4bf` · Shop `#ffce3a`
- Quests `#3fb45e` · Bag `#d9a054` · Chat `#4aa8ff`
- Radio `#c26bff` · Effects `#f7e024` · Alerts `#e0414a`
- Dock tier (Profile/Settings/Guide/Feedback): violet `#9b6cff` on
  dark `#1a1c23`.
Gold `#ffce3a` stays the universal "GO" (primary CTA) unless the panel
IS gold (Shop) — then ink `#2a1605` text on gold, and violet becomes
the secondary.

## LAYOUT REALITY (changed today)
- **The bottom command bar is GONE.** Panels own the full screen except
  a 56px bottom reserve (`GameHUD.razor.scss` ::after rule) for the two
  floating corners (currency chips bottom-left, PawPad chip
  bottom-right). Design full-bleed: big headers, roomy grids.
- Navigation is the PawPad (8) + number hotkeys. Panels do NOT need
  their own nav-to-other-panels chrome.
- Keyboard nav preservation is REQUIRED: every existing hotkey and
  cursor behavior in the panel you touch must still work. UIModalState
  contracts unchanged.

## HARD RULES
- BattleView / battle HUD: OFF-LIMITS.
- GuildPanel + ArenaPanel: do NOT sweep (features dormant).
- Compile-safety: the tree must compile after EVERY file save — write
  incrementally reference-complete (markup that calls a helper lands
  in the same edit as the helper). Mid-edit compiles happen.
- Static data laws: never rename fields on types held in static
  registries (hotload copies by name); behavior branches go in CODE.
- Don't create new asset files unless necessary; the icon/texture bake
  pipeline is `tools/convert-pawpad-icons.py` (Playwright) if truly
  needed — prefer CSS-built visuals.
- After your panel: append a one-line player-facing entry to
  `Assets/data/patchnotes-pending.json` (category "polish" or
  "feature").

## CLEAN-FIRST — USER MANDATE (added mid-sweep)
The game is going pixel-art-heavy (characters, maps, battles). The UI's
job is to FRAME pixel art, never compete with it: generous negative
space, restrained chrome, few strong accents over many weak ones,
sprites displayed BIG with `image-rendering: pixelated` and never
filtered/scaled-blurry (CSS filter on <img> breaks pixelated —
CLAUDE.md). When a decoration fights a sprite, the decoration loses.
You are also EXPLICITLY licensed to exceed the reference designs — if
you see a cleaner, stronger composition than the PawPad/menu dialect
would suggest, build it (keep the ring + accents + type identity).

## HOTKEYS — user allows changes
You may REMAP keys where it genuinely improves flow (document any
change in your report + the patchnotes line). Anything you change must
be discoverable (on-screen cap) and must not collide with: 1-6 tabs,
7/9 quests/bag, 8 phone, 0 profile, T/R/C/N widgets, Q/Esc close,
WASD/arrows nav, Space/Enter confirm.

## EFFICIENCY-FIRST LAYOUT — USER MANDATE (refined after wave 1)
Wave 1 read as "the same panels with a little tweak"; the correction is
NOT novelty for its own sake — it is THE MOST EFFICIENT LAYOUT:
- Start from the panel's JOB: what does the player do here most often?
  The layout that makes that fastest and clearest WINS. Fewer moves to
  the common action, the most important content biggest (usually the
  pixel art), less chrome, less dead space.
- Rebuild the macro-layout ONLY where the old one is inefficient —
  wrong hierarchy, cramped hero content, buried actions. Where the old
  arrangement already IS the efficient one, keep it and perfect it.
- Do NOT redo something just to make it look different. No themed
  gimmicks that cost usability. Litmus: a returning player should find
  everything faster than before AND the panel should look brand-new in
  its craft (type, accents, ring, spacing) even where the structure
  stayed.

## WHAT "REDONE" MEANS (the bar)
A stranger comparing before/after should say "this is a different,
better game" — new composition (not the same boxes recolored), a clear
identity moment (hero header / device chrome / stamp), the living ring
on interactives, the panel's accent used with conviction, motion via
CSS transitions (class-swap snap states; @keyframes only for
first-mount entrances), and STILL fully keyboard + mouse operable.
