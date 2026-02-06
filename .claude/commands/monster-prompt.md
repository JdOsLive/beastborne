Generate a PixelLab art prompt for a Beastborne monster.

Follow the CLAUDE.md guidelines exactly:

1. Read the monster's description from `Code/Core/MonsterManager.cs` (search for the species registration by name)
2. Check for evolution line (EvolvesFrom/EvolvesTo) and read all stages
3. Evaluate if the description translates well to visuals — if not, propose an updated description first
4. Extract visual cues from the description text (colors, forms, effects)
5. Generate Description field for PixelLab:

```
[Physical form from description], [key visual features], [colors implied by description], [any magical effects], facing left, 128x128 pixel art, sprite sheet 4 frames idle animation, fantasy monster game art style, dark background
```

Rules:
- The monster's text description is the PRIMARY source — don't force element colors if the description implies something different
- Ensure visual progression makes sense across evolution lines (base=smaller/cuter, mid=more defined, final=majestic/powerful)
- Include: 128x128, 4-frame idle, facing left, dark background
- If the user provides a monster name as an argument, use that. Otherwise ask which monster.

$ARGUMENTS
