# Scene Identity v3 — "Alive, fluid, commanding" (2026-06-10, user-approved plan)

The redesign pass for the three menu scenes (Roadmap / Patch Notes / Options docked).
Companions: guiding-star.md (tokens/doctrine), style-guide.md (component recipes),
scene-swap-spec.md (transition system). User mandate: each scene UNIQUE; bigger text;
LESS text; switching feels exciting; hover feels "alive, fluid, but COMMANDING."

## The voice problem being fixed
The scenes used the menu's surfaces but not its VOICE. The menu = huge 900-italic
display type, one hero, minimal words. The scenes = small text, equal boxes,
paragraph soup. Every change below pushes scene typography/density to menu voice.

## Typography scale (all scenes)
- Header title: 52px, 900 italic Exo2Italic, two-tone (white + identity color word)
- Header kicker: 13px, 800 UPPERCASE, letter-spacing 0.16em
- Poster/card titles: 40px, 900 italic
- Body copy: 18px/26px — ONE LINE PER ITEM, ≤8 words. No paragraphs anywhere.
- Stamps/badges: 12px 800 UPPERCASE tracked
- line-height always px (engine rule)

## Per-scene identity (one accent per view — guiding-star P4)
| Scene | Accent | Backdrop lean | Personality |
|---|---|---|---|
| Roadmap | GOLD #FFCE3A | +7° | "what's next" marquee |
| Patch Notes | VIOLET #9B6CFF | **−7° (mirrored!)** | the archive/scroll |
| Options | TEAL #2DD4BF (the Wind teal — fun, already canon) | +5° | the workshop |

**COLORS MUST FEEL FUN (user mandate):** the scene accent is the DOMINANT voice
(one accent per view, P4), but semantic color plays freely underneath:
- Status stamps are colorful by MEANING: IN DEVELOPMENT = violet #9B6CFF,
  COMING UP = sky #3F8FE0, ONGOING = green #3FB45E — saturated tinted fills
  (style-guide pill recipe), not gray-on-gray
- Poster diamond gems tint per feature: Tower Mode = gold, Story = spirit pink
  #FF4FD0, Bug Fixing = green #3FB45E — solid tinted gem fills w/ white glyphs
- Patch Notes keeps its tone-dotted category colors (gold/sky/rose/violet) and
  lets them SATURATE a step
- The test: a screenshot of any scene should read joyful at a glance, never
  corporate-dashboard gray. Dark surfaces stay dark (beasts/art pop); the fun
  lives in accents, stamps, gems, and strips.
Identity carries through: signature strip, accent bars, stamp tints, backdrop slab
tone (subtle hue shift of the dark slab, NOT a bright fill). The alternating lean
= Suto wayfinding: the angle itself says where you are.

## Roadmap — 3-poster marquee (user's cut list, exactly these)
1. TOWER MODE — stamp: IN DEVELOPMENT (pulsing) — copy: "An endless climb. Our biggest mode yet."
2. STORY — stamp: COMING UP — copy: "Weaverton's tale, told properly."
3. BUG FIXING — stamp: ONGOING — copy: "Always. Forever. Relentlessly."
- One row of 3 posters filling the 1700px frame: 3 × ~520px wide, ~560px tall, 40px gaps
- Poster anatomy: 72px diamond gem top-left → 40px italic title → one-liner →
  status stamp TOP-RIGHT rotated −3° (Persona stamp; straightens on hover)
- 4px identity accent edge (left), full height
- DELETE the lead paragraph; kicker carries context ("ROAD TO FULL RELEASE")
- IN DEVELOPMENT stamp gets the live-dot 1.6s pulse (vocabulary item)

## "Commanding" hover/focus (SNAP layer — the user's exact ask)
Posters and interactive rows respond DECISIVELY, ≤0.12s ease-out, no glows:
- bg brightens one surface step (#1C1830 → #241E3E family)
- accent edge flares 4px → 10px
- scale 1.02 (posters are flat + outside scroll containers — transform legal)
- the stamp straightens −3° → 0° (the poster "snaps to attention")
- title color lifts to full white
Posters are FOCUSABLE (ring tracks them; new CursorGeom/Rot entries; SelectedPop
lockstep if any transform rides selection). Keyboard: A/D walks posters + footer
CTAs; ENTER on a poster opens Send Feedback (pre-contextualized if cheap, plain
feedback panel otherwise). Hover == focus == same .selected state (house pattern).

## "Alive + fluid" (FLOW/ALIVE layers — doctrine-compliant)
- The living ring remains the ALIVE carrier; it must track posters perfectly
- IN DEVELOPMENT stamp pulse + OPEN BETA LIVE dot = ambient vocabulary
- Arrival stagger tuned punchier at full speed (stall is dead — the choreography
  finally plays): keep group starts 0/70/140ms, within-group 0/50/100ms
- Signature strip "signs in" on every arrival: width 0 → 100% over 200ms
  (class-toggle width transition — proven pattern, .lh-card-edge animates width)
- NO wave/skew on any surface (hard veto stands)

## Exciting switching
- A/D (and Q-home unchanged) CYCLES DIRECTLY between scenes:
  Roadmap ⇄ Patch Notes ⇄ Options, wrapping — full fly-out/fly-in choreography,
  no menu detour. RunSwap already supports scene→scene (the _pending/interrupt
  paths); wire A/D at the scene level when no inner control consumes it.
  NOTE: Options' docked panel owns A/D for its own columns — only cycle from
  Options via explicit keys ([ ] or Tab?) or skip Options in the cycle; agent's
  call, document it on the footer key-rail.
- Each arrival: signature strip draw + identity stamp of the scene
- Footer key-rail updates to show the cycle keys

## Patch Notes + Options (lighter touch this pass)
- Patch Notes: violet identity; version hero in the rail goes BIG (64px 900 italic
  gold→violet two-tone); section heads up to 24px; body stays (changelogs are
  text by nature) but rail TOC + heads carry the glanceability
- Options: slate identity signature strip + section titles up to 24px; otherwise
  already doctrine-ported

## Bugs riding along (from the live screenshot)
1. RING STRAND: after the scene split, the cross-glide retarget isn't landing —
   ring freezes over the dead sidebar control. Diagnose via swapdbg; likely the
   focus poll (FocusEl 20 resolution through _sceneComp) or the Left>-1000 guard.
2. MISSING GEMS: every diamond icon renders EMPTY in LighthouseScene.razor —
   iconify didn't survive the component move (check the component's using/markup;
   icons worked in MainMenu context).

## Constraints checklist
Engine quirks per CLAUDE.md + scene-swap-spec compliance section. Park, never
display-flip. No perspective in keyframes. Class-toggle transforms only on flat
non-plane elements. transition-delay needs .settled zeroing. SWAP_DEBUG/SLOWMO
stay ON until the user's final verification pass.
