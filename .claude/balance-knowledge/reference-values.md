# Reference Values

Concrete numerical targets the balance agent uses as benchmarks. These are **starting points**; agent adjusts with evidence and logs changes in `decisions-log.md`.

## BST tiers by rarity (launch)

| Rarity | BST range | Notes |
|---|---|---|
| Common — evo-base | 220-260 | Species that evolve into Uncommon. Grow out of this tier via evolution. |
| Common — standalone | 260-310 | Species with no evolution chain. "Final-form Common" — deserves a higher floor since they never evolve up. Lower catch rate (0.55-0.65) compensates. |
| Uncommon | 280-340 | Zone 2 wild pool |
| Uncommon — evolved | 340-400 | Evolved forms of Common base species. Should NOT spawn in their base species' starter zone. |
| Rare | 340-400 | Zone 3 wild pool, final evolutions |
| Starter | 275-290 | Hand-drawn carve-out, slightly above zone-1 Common |

**Evolution gap rule:** evolving from rarity N to rarity N+1 should add +80 to +140 BST. A Common (240) → Uncommon evolution at 340-380 feels earned.

**Wild pool rule:** evolved forms live in LATER zones than their base species, or are acquired only through evolution (never wild). Don't put an Uncommon evolution in a zone-1 Common pool — the player can't compete until they've evolved their own.

**Post-launch only — DO NOT USE AT LAUNCH:**

| Rarity | BST range |
|---|---|
| Epic | 430-490 |
| Legendary | 530-600 |
| Mythic | 620+ |

## Zone pool BST targets

| Zone | Level | Target avg BST | Pool size |
|---|---|---|---|
| Saltmoor Cove | 1 | 240 | 7 species |
| Saltmoor Forest | 15 | 320 | 9 species |
| Old Saltmoor | 30 | 390 | 7 species |

Players face enemies *at the zone's level*, so BST comparisons assume level scaling applies to both sides equally.

## Stat distribution archetypes

Applied within the BST budget. Modifiers are relative to even distribution (BST / 6):

| Archetype | HP | ATK | DEF | SpA | SpD | SPD |
|---|---|---|---|---|---|---|
| Tank | +20% | -15% | +20% | -15% | +20% | -10% |
| Physical attacker | -5% | +30% | +5% | -15% | -5% | +0% |
| Special attacker | -5% | -15% | -5% | +30% | +5% | +0% |
| Speedster | -10% | +5% | -10% | +5% | -10% | +30% |
| Glass cannon | -15% | +25% | -15% | +25% | -15% | +5% |
| All-rounder | ±5% across the board | | | | | |

*Example — tank at BST 260:* even = 43/stat. Tank = ~52 HP, 37 ATK, 52 DEF, 37 SpA, 52 SpD, 38 SPD.

## Growth rates (per level)

Total growth sum scales with rarity:

| Rarity | Growth total | Avg per stat |
|---|---|---|
| Common | 28-32 | ~5 |
| Uncommon | 32-38 | ~6 |
| Rare | 38-44 | ~7 |
| Starter | 30-34 | ~5.5 (slight bump over Common) |

Distribution should roughly match the base stat distribution — a tank's DEF growth is higher than its ATK growth.

## XP yield (Pokemon-style)

Formula when implemented:
```
xpGained = BaseExpYield × defeatedLevel / 7
```

Per-rarity `BaseExpYield` targets:

| Rarity | BaseExpYield |
|---|---|
| Common | 50-80 |
| Uncommon | 120-160 |
| Rare | 200-280 |
| Starter | 100 (flat — they're rarely fought as wild) |

## Fusion math targets (from design memory)

Zero-drift at base, skill investment unlocks the climb:

- Two 20-gene parents, no skills → expected offspring gene 20 (variance ±2)
- Two 25-gene parents, no skills → expected 24 (soft cap pulls down)
- Two 28-gene parents, no skills → expected 27 (heavy diminishing returns)
- Skill investment can push expected +2-3 per gene above base
- Hitting 30/30 in a single gene should require: high parents + skill investment + lucky mutation

## Catch rates (BaseCatchRate on MonsterSpecies)

| Rarity | Catch rate (float 0-1) |
|---|---|
| Common | 0.55-0.70 |
| Uncommon | 0.35-0.50 |
| Rare | 0.18-0.30 |
| Starter | N/A (never wild) |

## Expedition rewards (Gold + XP)

Per-run baseline, scales with wave count:

| Zone | GoldReward | XPReward |
|---|---|---|
| Saltmoor Cove (5 waves) | 50 | 35 |
| Saltmoor Forest (7 waves) | 100 | 130 |
| Old Saltmoor (10 waves) | 115 | 165 |

Current values from `ExpeditionManager.cs`. Agent may propose rebalancing these.

## Red flags (audit triggers)

The agent should flag any of these during a review:

- A Legendary with lower BST than a Common
- A species whose BST is outside its rarity band by >30
- Zone pools with species whose BST spans more than 80 points (too wide)
- Starter BST above 300 or below 250
- Growth rate totals <25 or >50
- A `PowerRating` or damage calculation that reintroduces stat divisors/multipliers
- Any mention of Epic/Legendary/Mythic in launch code before post-launch date
