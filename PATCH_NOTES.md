# Beastborne Patch Notes

---

## Version 0.5.1 - Beast & Balance Update

*"Backwards feet, flame-red hair, and a bad attitude toward poachers."*

### New Content

#### New Beast: Curublast (#82)
- Replaces Funharden — a jungle guardian inspired by the **Curupira** of Brazilian folklore
- **Nature element**, Uncommon rarity
- Found in the **Overgrown Heart** expedition
- Learns Pollen Burst, Nature Shield, and Bloom Burst

#### New Music
- **Moonlight Meadows** — new Chill FM track
- **Fangs and Fury** — new Battle FM track

#### Extended Movesets
- New moves added for every element: Fire, Water, Earth, Wind, Electric, Ice, Nature, Metal, Shadow, Spirit, and Neutral
- Cleaned up duplicate move entries and renamed moves for clarity

### Balance Changes

#### Genetics Rework
- Inherited genes now **average both parents** instead of heavily favoring the higher one
- **Diminishing returns** kick in as genes approach max — no more snowballing to perfect 31s
- Mutations are now 60/40 positive/negative (was 90/10), base chance 10% (was 15%)
- Gene Surge skill has diminishing returns at high gene values
- Flat gene bonuses capped when genes are already high

#### Starter BST Rebalance
- Every starter evolution line received a stat pass for more meaningful early choices

### New Features

#### Move Swapping
- **Swap moves directly** from both Monster Detail and Roster panels
- Tap any move slot to open the move picker
- Shows available learned moves not currently equipped
- Empty slots show "+ Learn Move" when moves are available
- Badge shows count of available moves

### UI & Quality of Life

- **Gold abbreviation** — large numbers display as K/M/B in the HUD
- **Bestiary** — slide-in entrance animation and background image
- **Filter bar** — dropdown icon alignment fixes
- **Expedition panel** — styling cleanup
- **Scroll fixes** — various overflow issues resolved across panels

### Technical
- Version bumped to v0.5.1 across MainMenu, GameHUD, and CreditsPanel

---

## Version 0.3.0 - The Expedition Update

*"The journey is just as important as the destination... but the loot helps."*

### New Features

#### Expedition Progression
- **Stage progression system** - Push deeper into expeditions as you grow stronger
- **Wave-based encounters** - Face multiple waves of enemies per expedition
- **Difficulty scaling** - Enemy levels and team composition scale with stage
- **Highest stage tracking** - Your progress is saved per save slot

#### Boss Battles
- **Boss encounters** at milestone stages
- **Unique boss mechanics** - Bosses have enhanced stats and abilities
- **Boss rewards** - Extra XP and gold for defeating bosses

#### Expedition Rewards
- **Gold rewards** based on waves completed
- **XP distribution** to all participating beasts
- **Bonus rewards** for completing expeditions without losses

#### Evolution Animations
- **Dramatic evolution sequence** when evolving beasts
- **4-phase animation**: Shake → Glow → Transform → Burst
- **Golden particle effects** burst outward on transformation
- **Smooth sprite transition** to evolved form

### Improvements

#### Team Selection UI Overhaul
- **Element filters** - Quickly find beasts by element type
- **Sort options** - Sort by Favorite, Power, Level, Genes, or Rarity
- **Improved layout** - Monster grid on left, team slots on right
- **Visual feedback** - Selected beasts clearly marked

#### Radio & Music
- **Pause persistence** - Music stays paused when switching between views
- **Context-aware stations** - Different music for menus, expeditions, and battles
- **User preference respected** - Manual pause survives scene transitions

#### Quality of Life
- **Multi-select checkbox** moved to top-right corner of cards
- **Improved release flow** - Easier bulk releasing of unwanted beasts
- **Better card layouts** - More readable monster information

### Visual Polish

#### Animations
- **Evolution glow effects** - Seamless golden glow without hard edges
- **Particle burst system** - 8-directional particle explosions
- **Smooth transitions** - All animation phases blend naturally

#### UI Refinements
- **Roadmap updated** - Now shows v0.3.0 as completed
- **Version badge** - Updated to ALPHA v0.3.0

### Bug Fixes
- Fixed evolution button being unresponsive in some cases
- Fixed music restarting when it should stay paused
- Fixed checkbox positioning overlap with card elements

---

## Version 0.2.0 - The Battle Update

*"Your beasts finally learned how to do more than just bonk each other."*

### New Features

#### Moves System
- Beasts now have **4 moves** instead of mindlessly auto-attacking
- **50+ unique moves** across all elements
- Moves have **PP (Power Points)** - use them wisely!
- Three move categories:
  - **Physical** - Uses ATK vs DEF
  - **Special** - Uses SpA vs SpD
  - **Status** - Buffs, debuffs, and conditions

#### New Stats: Special Attack & Special Defense
- **SpA (Special Attack)** - Powers up special moves
- **SpD (Special Defense)** - Reduces special move damage
- All 136 species rebalanced with the new stats
- Genetics now include SpA and SpD genes
- New natures: Mystical, Resolute, Arcane, Warded

#### Tactical Combat
- **STAB Bonus** - Same Type Attack Bonus gives 1.5x damage when move element matches beast element
- **Type effectiveness now applies to MOVES**, not just beast types
- **Stat stages** - Moves can raise/lower stats from -6 to +6
- **Status conditions**: Burn, Freeze, Paralyze, Poison, Sleep
- **Swap mechanic** - Switch your active beast as a turn action

#### Battle AI Improvements
- Smart AI considers type matchups, STAB, and current HP
- AI evaluates when to swap vs when to attack
- Auto-battle now makes intelligent move choices

#### Traits Actually Do Something Now
- **30+ meaningful traits** with real battle effects
- Examples:
  - **Blaze** - Fire moves +50% when HP below 33%
  - **Shadow Step** - +10% evasion
  - **Ember Heart** - Fire moves +15%
- Traits are consistent across evolution lines

### Improvements

#### Battle Visuals
- Move announcements show which move was used
- Element-colored damage numbers
- Status icons displayed on beast cards
- Stat stage indicators (ATK ↑, DEF ↓, etc.)
- Swap animations

#### Quality of Life
- Tamer XP now awarded **per enemy defeated** instead of only on expedition complete
- Move details shown in Monster Detail panel
- PP resets after each expedition

### Balance Changes

#### Stat Rebalancing
- Physical attackers: High ATK, lower SpA
- Special attackers: High SpA, lower ATK
- Tanks now specialize in DEF and/or SpD
- Speed stat unchanged but more impactful with move priority

#### Evolution Move Upgrades
- Evolving now upgrades basic moves to stronger versions
- Example: Ember → Flame Burst → Inferno

### Technical

- Data migration for existing saves (SpA/SpD calculated from existing stats)
- Existing beasts automatically receive starting moves based on level
- New BattleState system tracks stat stages and status effects

---

## Version 0.1.0 - Initial Alpha

*"It begins."*

### Core Systems
- 136 monster species across 10 elements
- Genetics system with 0-31 genes per stat
- Nature modifiers affecting stat growth
- Contract system for caught monsters
- Breeding with genetic inheritance
- Evolution system
- Tamer skill tree (20 nodes)
- 10 expedition stages
- Basic PvP arena
- Bestiary tracking
- Cloud save with 3 save slots

### Elements
- Fire, Water, Earth, Wind, Electric
- Ice, Nature, Metal, Shadow, Spirit

### UI
- Animated pixel art sprites (128x128, 4-frame idle)
- Monster cards with stat bars
- Expedition team selection
- Battle playback with speed controls

---

*Thanks for playing Beastborne! Report bugs on Discord: discord.gg/dJTTyCqKru*
