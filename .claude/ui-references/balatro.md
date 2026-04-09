# Reference: Balatro

**Axis:** Impact (ambient motion + number juice)
**Why it matters:** Balatro is the gold standard for making a screen feel *alive* when the player isn't doing anything. Almost every static UI problem Beastborne has, Balatro has solved. If a panel feels dead, ask "what would Balatro do" and the answer is usually "add subtle motion everywhere."

## Techniques to steal

### Ambient card float
Cards and jokers in Balatro never sit still. They have a constant gentle bob — roughly ±2-4px Y translation on a ~2-3 second sine ease-in-out loop. It's almost imperceptible per-card but collectively makes the screen feel like it's breathing.

**Apply to Beastborne:** monster cards in roster, day-nodes in streak track, mission card icons. NOT every element — just the ones that represent "living" things (monsters, key rewards). Too much and it becomes distracting.

### Number pop-ups on score
When scoring happens in Balatro, numbers fly off cards — bouncing in from the card, scaling up, then falling into the total with a counter tick. The total itself number-rolls rapidly rather than snapping to the new value.

**Apply to Beastborne:** currency changes in HUD after a claim. When you click CLAIM and get 1000 gold, the gold counter should roll up over ~400-600ms, not snap. A "+1000" text should briefly spawn near the button and float up toward the currency bar before dissipating.

### Joker reveal jiggle
When a joker is acquired, it doesn't just appear — it slams in with a slight rotation overshoot, settles with a couple shake frames. Combined with a distinct sound, the acquisition *feels* like getting something, not like an inventory update.

**Apply to Beastborne:** mission completion, day-node claim state change, milestone unlock. The element that changed state should briefly overshoot + settle, not just swap classes.

### Score chain amplification
Balatro's biggest insight: routine scoring events cascade into bigger and bigger reactions. A single chip glows, then a pair glows harder, then the multiplier pulses, then the total explodes. Each step adds weight to the last.

**Apply to Beastborne:** when multiple rewards claim in sequence (e.g., claim daily + trigger "all dailies complete" bonus), chain the animations. Don't fire them simultaneously — stagger them so each reward feels like it's triggering the next. The last one should feel biggest.

### Font weight as emphasis
Balatro uses varying font weight on numbers dramatically. A "routine" score is thin; a critical score is thick, colored, larger. The *same number* can have wildly different visual weight based on importance.

**Apply to Beastborne:** rewards of different tiers should look visibly different even before animation. A 100-gold mission reward vs a 10000-gold milestone should use different font sizes and weights, not just different numbers.

## What NOT to borrow

- **The maximalism.** Balatro's entire screen is in constant motion because it's a card game where *everything* is a Balatro joke. Beastborne has utility UI (settings, inventory, etc.) that needs to be calm. Apply ambient motion only to "celebration zones" — reward panels, streak tracks, roster showcases — not to every panel universally.
- **The chromatic intensity.** Balatro uses saturated rainbow colors freely. Beastborne has a curated purple/gold/green palette and should keep it.
- **Sound density.** Balatro has a sound for every single chip, pair, card, multiplier. That would fatigue players fast in a longer-session game like Beastborne.

## Pairing note

Balatro + Hades is the dream combo for impact work. Balatro teaches you how to make the *ambient* layer live; Hades teaches you how to make *moments* land. Use both: Balatro for the constant background liveliness, Hades for the stop-and-watch big reveals.
