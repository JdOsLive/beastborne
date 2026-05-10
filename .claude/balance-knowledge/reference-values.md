# Reference Values

Concrete numerical targets the balance agent uses as benchmarks. These are **starting points**; agent adjusts with evidence and logs changes in `decisions-log.md`.

## BST tiers by rarity

| Rarity | BST range | Notes |
|---|---|---|
| Common — evo-base | 220-260 | Species that evolve into Uncommon. Grow out of this tier via evolution. |
| Common — late-evo base | 260-285 | Common bases with a high evolution level (Lv 28+) — player holds them longer before they evolve, so they earn a higher BST floor than standard evo-bases. Padlip (282 @ Lv 32 evo) is the canonical example. |
| Common — standalone | 260-310 | Species with no evolution chain. "Final-form Common" — deserves a higher floor since they never evolve up. Lower catch rate (0.55-0.65) compensates. |
| Uncommon | 280-340 | Zone 2 wild pool |
| Uncommon — evolved | 340-400 | Evolved forms of Common base species. Should NOT spawn in their base species' starter zone. |
| Rare | 340-400 | Zone 3 wild pool, final evolutions |
| Rare — starter final evo | 510-525 | Hand-drawn carve-out for the three starter line caps (Manehelm/Lochmaw/Aurael). The player keeps a starter through the entire game; they're the campaign-long ace, not a wild-pool peer, so they sit a full evolution-gap above zone-3 Rares. Do NOT use this band for non-starter Rares. |
| Starter | 275-290 | Hand-drawn carve-out, slightly above zone-1 Common |
| **Epic** | **530-590** | First Epic ships v1.2.0 (Loomweaver, the mini-expedition Stage 2). Epic sits clearly above the starter-Rare carve-out — a player catching one knows it sits above their starter-final ace. Mostly evolution-only / boss-only at acquisition; not in wild capture pools at v1.2.0. |
| **Legendary** | **600-680** | Doc-only at v1.2.0 (no Legendary species shipped yet). 30 BST gap above Epic ceiling, 60-point band. Sized so Epic-vs-Legendary feels like a real step. Reserved for later content. |
| **Mythic** | **700+** | Doc-only at v1.2.0 (no Mythic species shipped yet). Open-ended ceiling — Mythics need their high BST to feel like a tier players actually chase. Flat ladder undermines that. **When the FIRST Mythic ships, ranked-mode design becomes a real v1.x project — solve via queue-layer constraint (BST cap, banlist, or per-beast cap), NOT format normalization.** |

**Evolution gap rule:** evolving from rarity N to rarity N+1 should add +80 to +140 BST. A Common (240) → Uncommon evolution at 340-380 feels earned. Two-tier rarity jumps (e.g. Uncommon → Epic, skipping Rare) earn a wider gap — see Padlip→Liliprince (+226) and Threadlet→Loomweaver (+200) as precedent.

**Wild pool rule:** evolved forms live in LATER zones than their base species, or are acquired only through evolution (never wild). Don't put an Uncommon evolution in a zone-1 Common pool — the player can't compete until they've evolved their own.

## Zone pool BST targets

| Zone | Level | Target avg BST | Pool size |
|---|---|---|---|
| Weaverton (Approach + Pasture) | 1 | 240 | 7 species |
| Weaverwood | 10 | 320 | 9 species |
| Weavermere | 20 | 390 | 7 species |
| Whispering Hollow (mini) | 25 | 410 | mini-roster |

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
| Epic | 42-48 | ~7.5 |
| Legendary | 46-52 | ~8 (doc-only, no shipped reference yet) |
| Mythic | 50+ | ~9+ (doc-only, no shipped reference yet) |

Distribution should roughly match the base stat distribution — a tank's DEF growth is higher than its ATK growth.

## XP yield (Pokemon-style)

Formula:
```
xpGained = BaseExpYield × defeatedLevel / 7 × levelRatio × (1 + skillBonus)
levelRatio = clamp(defeatedLevel / participantLevel, 0.5, 2.0)
```

Per-rarity `BaseExpYield` targets:

| Rarity | BaseExpYield |
|---|---|
| Common | 50-80 |
| Uncommon | 120-160 |
| Rare | 200-280 |
| Starter | 100 (flat — they're rarely fought as wild) |
| Epic | 280-360 |
| Legendary | 360-440 (doc-only) |
| Mythic | 440+ (doc-only) |

Within-band placement: intermediate evos sit at the band floor (player isn't supposed to grind them, they evolve), standalone/final forms sit at the high end (player fights them as a wall). BST adjuster ±5-10 for off-band BSTs.

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
| Epic | 0.10-0.15 (forward-compat — most Epics are evolution-only at launch) |
| Legendary | 0.05-0.10 (doc-only) |
| Mythic | TBD (doc-only — Mythic acquisition design is part of the v1.x ranked-mode project) |

## Expedition rewards (Gold + XP)

Per-run baseline, scales with wave count and `BaseEnemyLevel`:

| Expedition | Level | Waves | GoldReward | XPReward |
|---|---|---|---|---|
| Weaverton Approach | 1 | 3 | 30 | 20 |
| Weaverton Pasture | 1 | 5 | 60 | 45 |
| Weaverwood | 10 | 7 | 220 | 320 |
| Weavermere | 20 | 10 | 480 | 720 |
| Whispering Hollow (mini, optional) | 25 | 4 | 380 | 560 |

Current values from `ExpeditionManager.cs` as of v1.2.0. Whispering Hollow is the new optional mini-expedition (Earth element, hosts threadlet/loomweaver line). Agent may propose rebalancing these.

## Red flags (audit triggers)

The agent should flag any of these during a review:

- A Legendary with lower BST than a Common
- A species whose BST is outside its rarity band by >30
- Zone pools with species whose BST spans more than 80 points (too wide)
- Starter BST above 300 or below 250
- Growth rate totals <25 or >50
- A `PowerRating` or damage calculation that reintroduces stat divisors/multipliers
- Any Mythic species shipping without a corresponding ranked-mode design pass
- Any Epic/Legendary appearing in wild capture pools below its expected acquisition tier
