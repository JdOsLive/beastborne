# Beastborne UI Feel Principles

Core philosophy for every UI decision. Rules of thumb, not laws — use judgment.

## Core ethos

**Good UI is already the floor. Our job is to raise the ceiling.**

Beastborne already has consistent colors, decent layout, working flows. That's not the bar. The bar is: does opening this panel feel exciting? Does claiming a reward feel like a win? Does hovering feel alive? If not, find the flat moment and make it feel something.

Never treat "it works and matches the style" as done. Working-and-consistent is where we start, not where we stop.

## Impact vs Style — do not confuse these

Two different axes. Fixing one does not fix the other.

**Style** = how it looks. Colors, typography, layout, transitions, visual identity.
**Impact** = how it feels. Weight of actions, payoff of rewards, ambient liveliness, reveal moments.

Beastborne's style is in good shape. The open problem is impact. When reviewing a panel, ask "does this *feel* exciting?" before asking "does this *look* good." A stylish boring panel is still boring. A plain panel with great impact still feels great.

**Specific trap to avoid:** adding more transitions, more animations on transitions, or more visual flair to a panel that is flat at its core reward moments. That's treating style as a proxy for impact. It isn't.

## Juice tiers — allocate by moment frequency

Every interaction belongs in a tier. Each tier must feel clearly bigger than the one below it, or the big moments have nowhere to land.

**Tier 1 — Ambient**
- Always-on subtle motion: breathing idle glow, hover lifts, soft parallax, pulsing indicators
- Present on every panel at all times
- Never celebrates, just keeps the screen alive
- Rule: if static for >3 seconds, UI reads as dead

**Tier 2 — Routine rewards**
- Daily mission claim, individual day claim, minor currency gain, normal UI button press
- Short animation (~200-400ms), small particle burst, standard success sound
- Happens 5-20 times per session — must not fatigue
- Rule: player should notice but not stop what they're doing

**Tier 3 — Milestone rewards**
- Weekly mission complete, streak milestone claim, rare drop, expedition clear
- Longer animation (~600-1000ms), bigger particles, screen flash, distinct sound, number roll-ups
- Happens 1-5 times per session
- Rule: player should pause for a moment and feel it

**Tier 4 — Setpiece moments**
- Day 7 legendary, first-time unlocks, boss defeat, evolution, major progression
- Fullscreen sequence (~3-8 seconds), dedicated music or sound, anticipation + reveal + celebration phases
- Happens <1 time per session, sometimes once ever
- Rule: player should stop, watch, feel it, remember it

**Cardinal mistake:** making Tier 2 feel like Tier 3. When every claim is loud, the big moments are no louder than the small ones, and nothing stands out.

## Anti-gacha guardrail

Beastborne is a monster collector roguelike. It is NOT a gacha. Reward moments should feel *earned through commitment*, never *paid out by randomness*.

**Study gacha reveals for the moment, not the model.** HSR and Genshin have the best single-item pickup celebrations in the industry — the light pillar, silhouette, name slam, pacing. Borrow that. Never borrow:

- Rate-up or pity displays
- "You pulled N copies" framing
- Rarity splashes that imply a pull economy
- Currency-spent celebration
- FOMO timers beyond reasonable daily cadence
- Collection-anxiety language ("complete your collection!" as manipulation)
- "You got lucky!" framing anywhere

**The test:** would this reveal feel weird if there was only one possible outcome? Hades boon reveal works with any boon. Gacha pull screens don't — they're built around RNG concealment. Beastborne rewards are deterministic, so the reveal shape is Hades, not HSR, even if the *aesthetics* come from HSR.

If a proposed effect would make a player think "am I getting a good roll?" — it's wrong. The feeling we want is "I worked toward this and here it is."

## Feedback on every interaction

Dead inputs feel broken even when they work. Every clickable element needs:
- Hover state (color, border, scale, or lift)
- Press state (briefly compressed or darkened)
- Release feedback (sound, particle, or motion)

Missing even one of these makes the button feel cheap. Missing all three makes it feel broken.

## Visual hierarchy — one hero per screen

Panels where every element has equal visual weight read as spreadsheets. Pick one element per screen that should draw the eye first, then calibrate everything else down.

Examples:
- Daily streak tab: the streak counter + 7-day track is the hero. Reward box and milestone are support.
- Missions tab: the next claimable mission (or the monthly challenge) is the hero. Routine dailies are support.
- Expedition grid: the recommended tier or highest-cleared is the hero. The rest is grid.

If you can't point at the hero, the panel doesn't have one — fix that first.

## Restraint makes impact possible

Every juice addition costs headroom. If the daily panel's routine claim already has particles + shake + flash + sound, then Day 7 has nowhere to escalate. Keep Tier 2 tight so Tier 4 can go big.

When in doubt, subtract. The agent should be as willing to remove juice from the wrong place as to add it in the right place.

## Sound is UI

Sound is half of feel and it's easy to forget because we work visually. Every tier should have its own sound vocabulary:
- Tier 1: soft hover/click loops already in `SoundManager`
- Tier 2: standard `PlaySuccess` + small pling
- Tier 3: layered success + chime + softened bass hit
- Tier 4: dedicated cue, possibly with music duck

Missing sound on a good visual effect is a 50% loss. Flag it and propose the addition even if the actual sound file doesn't exist yet — the designer can fill in the gap.

## When the agent is uncertain

Propose the change with reasoning and ask. Taste is the user's call, not the agent's. The agent's job is to surface options and explain tradeoffs clearly, not to enforce its own aesthetic.

Good framing: "I'd suggest X because Y, but Z is also valid if you want to emphasize W."
Bad framing: "X is the correct choice."
