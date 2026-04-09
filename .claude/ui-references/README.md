# UI Reference Library

Curated reference games for Beastborne's UI design. Each file captures specific techniques worth stealing — not "game X is good" but "game X does Y, here's how, here's when to use it."

## Organized by axis

Beastborne currently has style in a decent place but needs impact work. When reviewing a panel, figure out which axis the problem lives on before picking references.

### Impact references (primary priority)
The games to study when a panel feels flat, when a reward moment lacks weight, or when routine interactions feel dead.

- **[balatro.md](balatro.md)** — ambient motion, number juice, maximalist feel
- **[hades.md](hades.md)** — reveal sequences, reward pacing, earned moments
- **[vampire-survivors.md](vampire-survivors.md)** — pixel-game juice ceiling, punchy feedback on a budget
- **[marvel-snap.md](marvel-snap.md)** — daily/mission claim UX specifically
- **[hsr.md](hsr.md)** — single-item reveal grammar (aesthetic only, NOT gacha structure)
- **[pokemon.md](pokemon.md)** — monster collector genre vocabulary, catch/evolve/battle feedback
- **[cult-of-the-lamb.md](cult-of-the-lamb.md)** — tonally adjacent pixel juice, reward celebration without AAA budget

### Style references (secondary)
The games to study when a panel is visually generic, needs identity, or when the problem is "looks fine but forgettable."

- **[slay-the-spire.md](slay-the-spire.md)** — minimal clarity, ruthless hierarchy
- **[persona.md](persona.md)** — style+impact coexistence, Strikers and P3 Reload specifically

## Critical rule

**Study the moment, not the model.** Borrow techniques — a reveal sequence, a number tick-up, a particle burst — never import structural mechanics that don't fit Beastborne's design. Specifically:

- HSR is a reference for *how to make a single pickup feel big*, not for gacha rates, pity systems, or pull screens. Beastborne's rewards are deterministic; the reveal should feel *earned*, not *rolled*.
- Balatro is a reference for *ambient liveliness*, not for card-game mechanics.
- Hades is a reference for *reveal pacing*, not for boon systems.

When in doubt, run the test: **would this feel weird if there was only one possible outcome?** If yes, you're borrowing structure, not technique — pull back.

## How to use these files

When the agent is reviewing a UI task:
1. Identify which axis the problem is on (impact or style)
2. Read the 2-3 most relevant reference files for that axis
3. Cite specific techniques by name in proposals (e.g., "Hades-style 3-stage reveal" is clearer than "a nice reveal")
4. Never propose wholesale copying — always adapt to Beastborne's visual language (purple/gold/green, pixel art, s&box CSS constraints)

## Caveat

These notes are written from general knowledge of each game. If a specific technique needs verification, the agent should say so rather than assert it. When in doubt, frame as "Hades does something like X" rather than "Hades does X exactly."
