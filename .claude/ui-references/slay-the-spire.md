# Reference: Slay the Spire

**Axis:** Style (minimal clarity, ruthless hierarchy)
**Why it matters:** Balatro and Hades teach you how to *add* feel. Slay the Spire teaches you how to *subtract* noise. When a Beastborne panel feels cluttered or when every element has equal weight, StS is the reference for ruthless hierarchy. Its UI has almost no juice — no ambient motion, no particles, barely any animation — but it's still one of the most readable UIs in the genre because of how aggressively it prioritizes.

## Techniques to steal

### One decision per screen
StS's card reward screen shows 3 cards. That's it. No distractions, no secondary options, no ambient UI. The entire screen is organized around the player making one choice. You cannot misread what you're supposed to do.

**Apply to Beastborne:** when a panel has a primary action, make it unmistakable. The daily claim button should dwarf the secondary options. The "new beast" receive moment should have nothing else competing for attention. If the player has to *look* for the primary action, the hierarchy is broken.

### Information on hover, not on screen
StS hides detail (card stats, relic descriptions, enemy intentions) behind hover states. The base screen is clean; hovering reveals depth. Players who want detail get it, players who want the gist aren't overwhelmed.

**Apply to Beastborne:** currently Beastborne's cards often show too much info inline (name + desc + progress + reward + state, all at once). Try hiding secondary info behind hover. The card should be glanceable in its collapsed state; expanding comes from interaction.

### The relic row as ambient reference
StS's top-of-screen relic row is tiny icons with no names. Players learn the icons over time. By mid-game they can read their entire relic situation at a glance. Compression through learned vocabulary.

**Apply to Beastborne:** when displaying owned items or effects in compact spaces, trust the player to learn icons. Don't label everything. Names on hover only. Icon languages are faster to scan than text.

### Negative space as emphasis
StS's UI has significant empty space around important elements. The card reward screen isn't crammed — it's framed. That emptiness forces your eye to the cards.

**Apply to Beastborne:** check current panels for crowding. If every pixel has something in it, nothing is emphasized. Consider adding breathing room around hero elements. The Expedition grid in particular would benefit from more negative space around whatever the "recommended" or "current" expedition is.

### Monochrome status, color as signal
StS is mostly grey-and-dark-red. Color is *reserved* for meaning: green for healing, red for damage, gold for money, blue for mana. Because the base palette is muted, signal colors are genuinely alerting.

**Apply to Beastborne:** Beastborne already uses purple + gold + green well. The risk is over-using them on routine UI, which desaturates their signal value. Audit: where is gold used casually? Where could grey/dark-purple replace gold to restore its "reward" meaning? Reserve color for where it pays off.

### Intention transparency
StS shows you what every enemy will do next turn via clear icons. This is anti-surprise, anti-cheap-death. Players feel in control because the game is honest.

**Apply to Beastborne:** when showing the player consequences or future state (next day reward preview, next milestone target, upcoming expedition difficulty), be explicit and forward-looking. Don't hide information that the player needs to plan. The "hover to preview day reward" interaction in DailyPanel is a good instance of this instinct — extend it.

### Font size = importance
StS's card name is big. The card type is small. The energy cost is huge. Stats are medium. Flavor text is tiny. You can read any card's importance hierarchy without reading a word, just by font sizes.

**Apply to Beastborne:** audit mission cards and monster cards for font size hierarchy. Are the "hero" elements (name, primary stat) visibly larger than secondary elements (description, meta)? If everything is 13-14px, nothing is emphasized.

## What NOT to borrow

- **The static UI.** StS is *famously* un-juicy. It works because its game is turn-based strategy and the decisions carry weight on their own. Beastborne has more action, more progression, more frequent small rewards — it needs more feel than StS. Use StS for clarity, not for motion minimalism.
- **The palette.** StS's muted dark palette is a style choice for its spire dungeon. Beastborne's palette is already established.
- **The specific card layout.** Cards are game-objects in StS. Beastborne's "cards" are UI containers. Different purpose, different rules.

## Pairing note

Slay the Spire is the *counterweight* reference. When an impact reference (Balatro, Vampire Survivors) suggests "add more motion" and it starts to feel cluttered, StS says "cut things until the important ones are obvious." Use StS whenever the critique is "too much" rather than "too little." Pair with Balatro for the add/subtract balance.
