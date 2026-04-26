# Power Formula

How we calculate a beast's "Power" number — shown in cards, used for sorting, compared in chat showcases.

## Current (legacy, has a bug)

[Monster.cs:161](../../Code/Data/Monster.cs#L161):

```csharp
public int PowerRating =>
    (MaxHP / 10) + ATK + DEF + SpA + SpD + (SPD / 2) + (Level * 5);
```

**Three bugs this has:**

1. **HP divided by 10** — a tank with 200 HP contributes only 20. Pokemon's BST counts HP 1:1. Fix: count HP at full weight.
2. **SPD halved** — speed decides turn order, arguably the most valuable stat. Halving it devalues speedsters.
3. **Level × 5 bonus swamps stat differences** — a Lv 20 beast gets +100 from level alone, regardless of species. This is why a leveled Common can out-Power a fresh Legendary. Fix: remove the level bonus entirely; stat growth already scales Power with level.

Concrete example of the bug:
- A: `180 HP / 110 ATK / 90 DEF / 110 SpA / 90 SpD / 90 SPD` → BST **670**
- B: `100 HP / 130 ATK / 80 DEF / 140 SpA / 80 SpD / 120 SPD` → BST **650**

Current formula at Lv 1: A = 463, B = 500. **B beats A by 37** despite having 20 less total stats — because A's HP gets divided and A's speed gets halved.

## New formulas (canonical)

Two separate concepts, both in [Monster.cs](../../Code/Data/Monster.cs):

```csharp
// Species intrinsic strength — sum of base stats, no level, no rarity.
// Used for: Beastiary tiering, balance design, agent audits.
public int BaseStatTotal =>
    BaseHP + BaseATK + BaseDEF + BaseSpA + BaseSpD + BaseSPD;

// Individual beast current strength — sum of stats AT current level with genes applied.
// Used for: card display, sort order, chat showcase, arena tooltip.
public int PowerRating =>
    MaxHP + ATK + DEF + SpA + SpD + SPD;
```

**Why this works:**

- **1:1 stat weighting.** Every stat contributes equally. Tanks, speedsters, and glass cannons can all look good in Power.
- **Level scales naturally via growth.** No artificial `level × 5` — a stronger level 50 beast is stronger because its growth rates compounded, not because of a flat bonus.
- **No rarity multiplier.** If a Legendary doesn't have higher stats than a Common, fix the stats. Don't hide the bug behind a display multiplier.

**When to use each:**

- `BaseStatTotal` → species-level comparisons. Beastiary sorting. Agent's first audit pass. Stat-budget discussions.
- `PowerRating` → individual beast snapshot. What the player sees on the card. What "My 65-level beast is stronger than yours" means in chat.

## Downstream impact of the fix

Changing the formula affects the displayed value EVERYWHERE `PowerRating` is read:

- [ArenaPanel.razor](../../Code/UI/Panels/ArenaPanel.razor) — tooltip + sort
- [BreedingPanel.razor](../../Code/UI/Panels/BreedingPanel.razor) — sort
- [ExpeditionPanel.razor](../../Code/UI/Panels/ExpeditionPanel.razor) — sort
- [MonsterCard.razor](../../Code/UI/Components/MonsterCard.razor) — card display
- [ChatManager.cs](../../Code/Core/ChatManager.cs) — showcase message
- [GameSettings.cs](../../Code/Data/GameSettings.cs) — `ShowPowerRatings` toggle

No migration needed — these all read `PowerRating` dynamically. Old cached Power numbers in chat messages will remain as-is (which is fine; they're snapshots).

## Agent rule

When proposing changes to `PowerRating` or `BaseStatTotal`: **never** reintroduce stat divisors, multipliers, or level bonuses. If a display problem shows up ("tank looks too strong now"), fix the stat distribution at the species level, not the formula.
