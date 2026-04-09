# Reference: Marvel Snap

**Axis:** Impact (daily/mission UX specifically)
**Why it matters:** Marvel Snap has arguably the best daily mission + reward claim UI in any live-service game. It's the closest direct analog to what Beastborne's DailyPanel is trying to do. Study it specifically for the mission card layout and the claim button feedback.

## Techniques to steal

### Mission card hierarchy
Snap's daily missions are NOT a uniform list — each mission card has subtle differences based on state:
- **In progress** cards are muted, showing just enough info
- **Near-complete** cards (>75% progress) get an amber tint and the progress bar pulses
- **Complete & claimable** cards glow, their CLAIM button pulsates, they almost *demand* attention
- **Claimed** cards collapse to a smaller compact form, fading out of primary attention

The eye is trained to immediately know "which one should I deal with first" without reading a word.

**Apply to Beastborne:** currently DailyPanel mission cards all look identical regardless of state. Add progressive visual weight — near-complete should be tinted, complete should pulse, claimed should visually de-emphasize. The player should be able to glance at the mission list and know their action queue in <1 second.

### Claim button juice
When you click CLAIM on a Snap mission, the button does several things simultaneously:
1. Briefly scales up (~1.1x)
2. Particles burst outward from the button
3. The reward (currency, card key, etc.) flies from the button toward its UI destination in the HUD
4. The destination counter rolls up
5. The mission card itself animates to "claimed" state with a checkmark stamp
6. A satisfying layered sound fires

The whole sequence is maybe 800ms but feels weighty because it's *coordinated*. Nothing in isolation is impressive; the combination is.

**Apply to Beastborne:** this is exactly what DailyPanel's claim action is missing. Right now the claim does one thing (plays a sound). It should do all six. The currency fly-to-HUD is particularly important — it creates a visible cause-effect link between the action and the resource change.

### Seasonal pass style rail
Snap's seasonal pass uses a horizontal rail with tier nodes that have escalating visual weight. Early tiers are small and simple, late tiers are larger with more decoration, the final tier is dramatically larger with a spotlight effect.

**Apply to Beastborne:** your 7-day streak track already uses escalating heights — this is the same pattern, good instinct. What you can add is *per-tier decoration intensity* — Day 1 is a plain node, Day 7 has particles drifting around it even before you reach it. Tease the endpoint constantly.

### "New" badges and notification dots
Snap is aggressive about showing you where there's something new: badges, glowing dots, subtle pulses on buttons. The player never has to ask "did I get something?" because the UI *tells* them through ambient notification.

**Apply to Beastborne:** when missions complete while the panel is closed, the bottom-bar daily button should have a notification badge with the count. Hovering should preview what's claimable. Opening should animate the attention toward the first claimable thing.

### Variant collection flash
When you get a new card variant in Snap, there's a mini-reveal: the variant fades in with a color wash specific to its rarity. Even common variants get a little moment. Rarer variants get more dramatic wash + a longer pause.

**Apply to Beastborne:** adaptable pattern for new beast acquisition (Day 7 legendary, first-time catches). Tier the reveal duration by rarity. Common = 500ms, rare = 1.5s, legendary = 4-6s. Players learn the tier language fast.

### Daily mission refresh indicator
Snap shows "New missions in 4h 23m" prominently. Not just as text — the timer is a subtle visual anchor. Players check it subconsciously to pace their play.

**Apply to Beastborne:** DailyPanel already has "Refresh in Xh Ym" — this is good. You could enhance it by having the text color shift as it approaches zero (green while plenty of time, amber as it gets close, brief pulse at rollover).

## What NOT to borrow

- **The monetization architecture.** Snap is built around selling variants and the season pass. Beastborne isn't. Don't import the "things to buy" pressure that shapes Snap's UI.
- **Card-game-specific flourishes.** Snap's card reveal, hand layout, battleground aesthetic are for its game, not yours.
- **The AAA polish budget.** Snap has full art and animation resources. Aim for the *structure* of their feedback loops, not the fidelity.

## Pairing note

Marvel Snap is the most *directly applicable* reference for DailyPanel. When working on daily/mission UI specifically, open this file first. Pair with Balatro for ambient motion and Hades for the bigger moments.
