# Balance Principles

Core philosophy for Beastborne's mechanical balance. When a change seems to violate one of these, push back before proposing.

## 1. BST is the budget

Every species has a **Base Stat Total** — sum of all six base stats. BST is the single most important balance knob. Design goes:

- Pick the target BST for a species based on **rarity + role in the progression arc** (zone 1 / zone 2 / zone 3 / starter / post-launch).
- Distribute the BST across six stats according to the species' **archetype** (tank, glass cannon, speedster, all-rounder).
- The archetype chooses the SHAPE. The BST chooses the SIZE.

Higher rarity ⇒ higher BST. Full stop. If a Legendary has lower BST than a Common, that's a budget bug — fix it at the stat definition, don't paper over it with multipliers.

## 2. Every stat counts 1:1

No stat has a divisor or multiplier in core formulas (Power, damage, comparisons). HP is not worth less than ATK. SPD is not worth half. If we want a stat to "feel" more valuable in gameplay, adjust the **damage formula or move design**, never the stat weighting in display math.

## 3. No artificial rarity multipliers in Power display

Power should reflect **actual stats at current level**. If a Legendary looks weaker than a Common in the Power column, it's because the Legendary's stats are worse — not a math quirk. Fix the stats.

## 4. Zone-tiered progression

The three launch zones give us three stat plateaus. Wild pool at each zone should land in its BST band (see `reference-values.md`). Gaps matter — they force the player to grow their team between zones rather than walk straight through.

## 5. Archetype differentiation over inflation

When making two beasts "feel different," change their **stat distribution**, not their BST. Two 260-BST Commons can feel radically different (one 90-HP tank, one 90-SPD speedster) while remaining balanced against each other.

## 6. Fusion zero-drift (from design memory)

Base fusion math is zero-drift. Skill investments are the player's path to gene gains. Any change to `GeneticsCalculator` must preserve these target curves:

- Two 20-gene parents (no skills) → expected 20 (variance ±2)
- Two 25-gene parents → expected 24 (soft cap)
- Two 28-gene parents → expected 27 (heavy diminishing returns)

## 7. Starter carve-out

Starters sit at a deliberate **275-290 BST** — slightly above zone-1 Common (220-260) so they feel meaningful on day one, but low enough that zone-1 catches catch up by zone 2 via their own growth. Starters are NOT legendaries. Their specialness comes from being hand-drawn and chosen once per save, not from dominating stats.

## 8. Propose before editing (always)

The balance agent never silently edits. Every change passes through:

1. Read current values
2. Cross-reference benchmarks
3. Propose concrete numbers + reasoning + impact analysis
4. Wait for user approval
5. Edit
6. Log to `decisions-log.md`

No exceptions.

## 9. Respect the launch roster constraint

At launch we ship with 30 species: 4 handmade (3 starters + pollenpuff) + ~26 AI-holdover. Balance decisions are on the 30-species roster. Do NOT propose changes to cut species — the list is frozen for launch.
