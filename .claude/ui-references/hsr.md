# Reference: Honkai: Star Rail (and Genshin)

**Axis:** Impact (single-item reveal grammar)
**Why it matters:** Gacha games have spent billions of dollars perfecting exactly one thing: making a single item pickup feel like a life event. HSR specifically does it better than almost any other game. For Beastborne's **Day 7 legendary reveal**, this is the reference.

## CRITICAL RULE — READ THIS FIRST

**Reference for aesthetic and pacing ONLY. Do not import gacha structure.**

HSR's reveal grammar is designed around one purpose: concealing an RNG roll to maximize anticipation for whatever rarity tier appears. Beastborne's Day 7 legendary is *deterministic* — the player knows they will get a legendary. The reveal should feel *earned*, not *rolled*.

**Forbidden imports:**
- Rate displays, pity counters, "guaranteed" banners
- Rarity tier splashes that imply different outcomes (Beastborne's Day 7 is always a Day 7 — there's no "bad" outcome to conceal)
- "You pulled N copies" framing
- Any UI element that makes the player think "am I getting a good roll?"
- Currency-spent celebration (Day 7 is earned by playing, not paying)
- Pull economy vocabulary ("tickets", "wishes", "warps")

**The test:** would this feel weird if the outcome was known in advance? HSR's reveal structure IS weird in that case. Beastborne's reveal must NOT be. Strip the RNG concealment and what's left — the aesthetics, the pacing, the sound — is what we borrow.

With that said, here's what to borrow:

## Techniques to steal (aesthetic only)

### The light pillar anticipation
Before anything reveals, HSR shows a vertical column of light on a dark background. The color of the pillar telegraphs *something* is about to happen. The player is forced to pause and watch.

**Apply to Beastborne:** Day 7 claim triggers a moment where the normal UI dims and a vertical light column rises from the day-7 node. ~1 second of buildup before the silhouette appears. Uses Beastborne's gold/purple palette, not HSR colors.

### Silhouette reveal
The character doesn't just appear — a silhouette forms first, then resolves into full art. The silhouette stage is ~0.5-1s of "who is it?" tension even when you know.

**Apply to Beastborne:** Day 7 already has a rotating silhouette preview (great instinct) — reuse that silhouette as the initial reveal frame, then have it resolve to the full-color monster sprite. The preview and the reveal use the same asset at different alpha/brightness.

### The name slam
Once the character is revealed, their name slams onto screen in large type with a hard sound cue. The name holds for a beat before the rest of the UI continues. This is the emotional peak.

**Apply to Beastborne:** legendary name (and possibly title/tagline) slams into frame with a percussive sound. Hold 500-800ms before anything else happens. This is the "moment of the sequence."

### Camera-equivalent push-in
HSR isn't a 2D game but it uses simulated camera moves during reveals — push in on the character, rotate slowly, swing around. For Beastborne as a 2D pixel UI, the equivalent is scale transforms + subtle rotation + parallax layers.

**Apply to Beastborne:** during the Day 7 reveal, scale the monster sprite from 80% to 110% over the reveal duration with a subtle rotation (±2 degrees). Multi-layer any background (e.g., light rays in one layer, particles in another) and offset their motion slightly for pseudo-parallax.

### The "moment after"
After the big reveal, HSR doesn't immediately drop you back to the menu — there's a brief "okay, look at it" pause with gentle ambient motion before you can dismiss. The game respects the moment.

**Apply to Beastborne:** after the Day 7 reveal, the "dismiss" button shouldn't appear for ~1.5 seconds. Let the player *look*. Then fade in the continue button. Don't let them click past the moment accidentally.

### Layered sound design
HSR reveals use 3-4 distinct sound layers: anticipation swell, reveal hit, name slam, ambient bed. Each layer has its own timing and the layering creates emotional dynamics that a single sound couldn't.

**Apply to Beastborne:** Day 7 sound should be composed, not a single cue. Buildup drone → reveal chime → name slam percussion → held ambient tail. If you don't have these assets, mark them as needed and use placeholders.

## What NOT to borrow (beyond the critical rule)

- **3D asset quality.** Beastborne is 2D pixel art. Don't try to approximate 3D character showcases.
- **Voice acting.** HSR has extensive VO. Beastborne shouldn't fake it — silence is better than bad VO.
- **Post-reveal nav.** HSR dumps you into a character detail screen after the reveal. Beastborne should just return to the daily panel with the new beast now visible in the collection. Keep it tight.
- **The reveal length for routine rewards.** HSR's full reveal sequence is only for high-rarity pulls. A Day 1 claim in Beastborne should NOT get this treatment — only Day 7 and first-time legendary moments.

## Pairing note

HSR + Hades is the combo for the Day 7 sequence. HSR for the aesthetic grammar (light pillar, silhouette, name slam, camera push-in). Hades for the pacing and earned feeling (held beats, deliberate pauses, "you worked for this" framing). Strip gacha from HSR, strip Greek mythology from Hades, combine what's left.
