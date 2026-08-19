# Beastborne — Dev Onboarding

A high-level resume of the project for a new coder coming on. Pair this with `CLAUDE.md` (project conventions + s&box quirks) and the Claude memory decision log (`.claude/memory/MEMORY.md` in-repo snapshot; live copy in `~/.claude/projects/<slug>/memory/` — see `HANDOFF.md`) for the full picture.

---

## What is Beastborne

A monster-tamer game for **s&box** (Facepunch's Source 2 modding platform). Solo-dev project by **jschoeb** (jdosLive). Currently in **v1.2.0 OPEN BETA**.

Genre: turn-based monster collector with story expeditions, fusion, evolution, online tamer-vs-tamer battles, guilds, and (planned) a roguelite mode.

The project's csproj is named `megarougelite` — a relic of the original "rougelike monster tamer" design. The game has since pivoted to story-mode-first with the roguelite as a planned later mode.

Internal terminology rules (player-facing copy must follow these):
- **"Contract"**, never "catch" — capturing a beast is signing it to a contract
- **No "loyalty"** language — the loyalty system was retired pre-launch
- **Tamer**, not "trainer" or "player" — that's the role name in the world

---

## Tech stack

- **s&box (Source 2)** — Facepunch's mod platform; C# game scripting, Razor + SCSS for UI
- **.NET 8**
- **Custom REST backend** at `http://157.245.10.193.nip.io:3000/api` (player saves, guilds, leaderboards via Sandbox.Services)
- **s&box peer-to-peer RPC** for in-lobby comms (chat, trades, voice, dormant PvP)
- **Steam ID** as the auth/identity layer
- **Iconify** via a custom s&box addon (lucide icon set; new icon names need manual addon refresh)
- **Custom font** Exo 2 Bold registered via SCSS

UI is Razor-with-SCSS panels under `Code/UI/Panels/` and `Code/UI/Components/`. There's a substantial **Persona-5-inspired stamp dialect** (italic skewX text, gold accents, dark panels with offset shadows). Honor it; don't introduce new visual languages.

---

## Core game loop

1. **Pick a starter** — three lines (fire/water/wind), each three-stage with a Rare final form
2. **Run expeditions** in story zones — wave-based encounters against wild beasts and a boss
3. **Contract beasts** during expeditions (Inks consumed; success rate based on Catch Rate × HP%)
4. **Level beasts** — gain XP from defeated wilds + expedition completion bonus
5. **Evolve** at level thresholds (varies by line)
6. **Fuse** beasts to combine traits/genes (Persona-style fusion math)
7. **Build party** of up to 6, send a 3-beast active team into expeditions
8. **Buy items / trade with players / join guilds** as ambient progression

Difficulty scales through three story zones; further content is mini-expeditions, daily missions, and (planned) roguelite mode.

---

## Story mode — Saltmoor / The Weaver Region

Three launch zones share a single regional identity: **the Weaver Region** (wool, dye, looms, mythology of stories woven into thread).

| Zone | Internal ID | Display | Player Lv unlock | Waves | Theme |
|---|---|---|---|---|---|
| 1 | `saltmoor_cove` | **Weaverton** | 1 | 5 (boss-free) | Wool-and-loom village built around the Sheepot trade |
| 2 | `saltmoor_forest` | **Weaverwood** | 10 | 7 | Dye-leaf forest where herders summer their flocks |
| 3 | `old_saltmoor` | **Weavermere** | 20 | 10 | Mirror-still pond where painters and dye-masters chase impossible colors |

Internal IDs predate the rename; they stay as-is to avoid SaveBlob churn.

**Mini-expeditions** (new in v1.2.0): short side-content branches off the main path. First one is **Whispering Hollow** (`mini_loomweaver_burrow`, 4 waves, Lv 25) — a haunted cave on the forest road where travelers used to camp. Hosts the new **Threadlet → Loomweaver** beast line (Anansi mythology subtext, kept out of player-facing copy).

The mini-expedition framework (`Expedition.IsMiniExpedition`, `SpeciesWeights` dict for per-species spawn rates, gating via `HasClearedExpedition()` predicate) is the post-launch content surface — drop new mini-expeditions here without bothering the main story path.

**Side quests** + **trade nodes** are layered on top of zones. Trade nodes are NPC vendors with thematic exchanges (e.g., the Weavermere's three artist NPCs trade Padlip pondlight motes for relics, dye recipes, and verses). Side quest framework is in `Code/Core/SideQuestManager.cs`.

---

## Possible roguelite mode (planned, not built)

Project name `megarougelite` hints at the original design intent. Roguelite mode isn't shipped yet but is a planned post-launch surface. Design space:

- Procedurally generated wave runs vs hand-authored story expeditions
- Per-run beast acquisition (no permanent contracts)
- Death = wipe; meta-progression via tamer-level perks or roguelite-specific currency
- Could leverage existing wave-encounter generation in `ExpeditionManager` with a different gating layer

**Note for new devs**: don't add "roguelite mode" content in v1.2.x. Until the design is locked, build new mini-expeditions and beasts within the story-mode framework. Roguelite work would be a substantial new feature lane.

---

## Online & multiplayer

### What's live (v1.2.0)
- **Save sync** to custom REST backend (`SaveApiClient.cs`) — per-player Steam ID scope, soft size limit 500KB / hard 2MB, cloud-write quarantine + 3-state load (Loaded/NoSave/Failed) to prevent the wipe class of bug
- **Trades** between players in the same s&box lobby (`TradingManager.cs`) — `[Rpc.Broadcast]` based
- **Voice chat** (lobby-only, no multi-room — multi-room cut behind a kill-switch)
- **Chat** with player profiles (`ChatManager.cs`)
- **Tamer cards** — collectible profile cards from each tamer you interact with (powers offline-friend invites)
- **Guilds** — XP/levels (Lv 50 = 3.75M XP, multi-year aspirational), perks unlock at Lv 5/10/15/20/25/30/35, raids gated behind a kill-switch for now, **join requests + invites** (now correctly visible from the Invite badge)
- **Leaderboards** via `Sandbox.Services` (Tamer XP, Beasts contracted, etc. — arena leaderboards removed pending real PvP)

### Online battles — special case

**The architecture is asynchronous parallel singleplayer with shared seed**, NOT real lockstep PvP. Both clients run BattleAI piloting the OPPONENT's serialized team locally; a shared `BattleSeed` is supposed to make AI decisions match. There are **no per-turn move-submission RPCs**.

**Currently HIDDEN behind kill-switches** (`OnlineHubPanel.RankedEnabled = false`, `LeaderboardPanel.RankedEnabled = false`). The full Arena UI rebuild + match-config wire format + checksum + ruleset (1v1/2v2/3v3, level cap, banned rarities) IS in code (`CompetitiveManager.cs`, `Code/Systems/BattleChecksum.cs`) — just unreachable.

**Real PvP comes in v1.3+** as "Scope B" — authoritative-client model, lower-connection-ID peer runs `BattleSimulator` on both submitted actions and broadcasts state. 6.5–8 person-day estimate. See memory: `project_pvp_scope_decision.md`.

### Backend endpoints (`SaveApiClient` + `GuildApiClient`)
- `GET/PUT/DELETE /api/players/{steamId}/save`
- `POST /api/guilds`, `GET /api/guilds`, `GET /api/guilds/{id}`, `POST /api/guilds/{id}/invites`
- `GET /api/players/{steamId}/guild`, `/invites`
- **No** `players/search?q=` or `players/list` — invite picker uses lobby + CollectedCards as the addressable pool

---

## Combat system

`Code/Systems/BattleSimulator.cs` is the canonical reference (~2800 LOC). Key design:

- **Six stats**: HP / ATK / DEF / SpA / SpD / SPD
- **11 elements**: Neutral, Fire, Water, Earth, Wind, Electric, Ice, Nature, Metal, Spirit, Shadow
- **Dual-typing supported**; defense uses dual-type effectiveness multiplier
- **Damage formula** (Pokemon-spec linear): `((4×Lv/5 + 2) × Power × ATK/DEF / 50 + 2) × multipliers`
- **Status effects**: Burn (1/15 MaxHP per turn), Freeze, Paralyze, Poison (1/10 MaxHP per turn), Sleep, Confuse, Flinch
- **STAB** (1.5× when move type matches user's type)
- **Crits, accuracy, priority moves, swap mechanics, status-can-act gates** all standard genre fare
- **AI** in `Code/Systems/BattleAI.cs` — uses `BattleSimulator.CurrentRandom` as the single RNG (load-bearing for v1.3+ deterministic PvP; never `new Random()` in combat code)

### Stat growth formula
`Stat = BaseStat + Level × Growth × 0.6 + Gene` (linear; replaced an old sqrt formula in v1.0.3 — playtest-validated, do not regress to sqrt). Natures apply as a final multiplier (×1.1 / ×0.9 on two stats).

### Rarity → BST bands (locked in `reference-values.md`)
- **Common** evo-base 220-260, late-evo base 260-285, standalone 260-310
- **Uncommon** 280-340 / evolved 340-400
- **Rare** wild 340-400, starter-final carve-out 510-525
- **Epic** 530-590 (first ships v1.2.0 — Loomweaver)
- **Legendary** 600-680 (none shipped yet)
- **Mythic** 700+ (none shipped; ranked needs queue-layer cap/banlist when first Mythic ships)

---

## Beast progression

- **Levels 1-100** (cap 50 in PvP normalization); story content tuned around Lv 1-30 currently
- **Genes** per stat (-31 to +31 ish; Pokemon IV analog) — randomized at contract time
- **Natures** apply ×1.1/×0.9 to a pair of stats
- **Traits** — passive abilities; species has a `PossibleTraits` pool, the contracted instance gets one
- **Evolution** at fixed level thresholds (can be branched conditionally — Padlip → Liliprince at Lv 32)
- **BaseExpYield** per species (curated v1.2.0; Common 50-80, Uncommon 120-160, Rare 200-280, Epic 280-360)
- **Catch Rate** per species (0.05 hardest, 0.45 easiest)
- **Signature moves** — one or two per species, learned at specific levels, often the species' identity move

**`MonsterManager.ValidateAndRepairMonster`** (renamed in v1.2.0) is an ongoing runtime data-repair pass — handles renamed moves, pruned traits, missing move regeneration. Runs every monster load. Don't delete it; future patches that rename moves depend on it.

---

## Tamer progression

- **Tamer level** (separate from beast levels) — unlocks zones, skill tree nodes, content gates
- **Tamer XP** from expedition completion + per-KO + daily login streak
- **Skill tree** with branches (`SkillTreePanel.razor`) — passive bonuses per skill point
- **Daily login + missions** — 7-day cycle, streak shield, milestone rewards at 7/14/30/60/100/365 days
- **Side quests + achievements** — meta-progression layers
- **Gold** currency for shop purchases
- **Per-beast inventory** of held items (one slot each)
- **Tamer inventory** — consumables, materials, relics, held-items-not-equipped

---

## UI dialect

The "Persona stamp" recipe is the visual identity. It's used **sparingly** — never on every element, only on hero CTAs and titles to preserve weight:

```scss
font-weight: 900;
letter-spacing: 2px;
text-transform: uppercase;
font-style: italic;
transform: skewX(-8deg);
text-shadow: 1px 1px 0 rgba(0, 0, 0, 0.85);
```

Plus some buttons add a 4px hard offset shadow + dark border + colored slab body for the "slab" look (Achievement claim-all-btn, Shop buy-btn, Embark button, etc).

**Color palette**:
- Backgrounds — pitch black to deep violet (`rgba(15, 15, 25, 0.98)` is the standard panel fill)
- Borders — 2px solid black for chrome, violet (`rgba(139, 92, 246, ...)`) for accents
- Gold (`#fcd34d`) for hero text + accents
- Type colors per element (fire orange, water blue, etc — see `MonsterCard` element pills)

**s&box CSS quirks** are documented in `CLAUDE.md`'s big quirks table — read it before any SCSS work. Common gotchas: `transform: translate(-50%, -50%)` on a modal ancestor poisons descendant flex chains; `:first-of-type` aborts the whole stylesheet; `lucide:tent-tree` and other newer icons need manual addon refresh.

**Reference panels** to copy from when building new UI:
- **GuildPanel** — for information-dense card-driven layouts (header strip + info row + content body + danger zone)
- **`.picker-dialog`** in GuildPanel.razor.scss — canonical popup chrome (sharp corners, violet offset shadow, dark fill)
- **AchievementPanel + ShopPanelContent** — examples of tasteful Persona stamping (one stamp per panel, never compounding)
- **WhisperingHollow detail card** — for lore-flavored expedition reveals

---

## Items / economy

`Code/Core/ItemManager.cs` defines ~110 items at init:

- **Consumables** (11) — atk/def/spd/spa/spd_def/crit boosts, catch_lure/prime, xp_treat/xp_feast, gold_bell
- **Server-boosts** (8) — global effects when activated
- **Relics** (~30) — held items with passive effects, rarity uncommon → legendary
- **Held items** (~26) — equipped to one beast
- **Quest maps** (4) — unlock specific encounters
- **Boss-shop consumables** (10) — bought from boss vendors
- **Nature runes** (16) — change beast nature
- **Beast signature materials** (auto-registered per species at runtime as `mat_<speciesId>`)

**Drop tables** (`zone_<id>` per zone, `boss`, `boss_rare` for boss kills, plus per-element tables). **Drop-table split rule** (locked design): medicines / XP-treats / catch-lures are SHOP-only at launch. Zone drops = beast materials + a few held items/relics + gold. Don't add medicines back to zone drops.

**Shop** has a Daily Spotlight mechanic with a 15% discount on a featured item. 31 items in the catalog including contract Inks (3 tiers), tamer/beast XP boosts, gold boosts, rare radar, lucky charm, monster slot expansions, and the consumables.

**Contract negotiation** (`ContractNegotiationPanel.razor`) — the "catch" interaction. Player offers Ink + optional treats; success roll based on Catch Rate × HP%.

---

## Live ops & community

- **Patch notes** tracked AT ship time in `Assets/data/patchnotes-pending.json` — the `patch-notes` skill rolls pending → versioned and generates Discord copy at release. Don't summarize from `git log` retrospectively.
- **Discord** is the player community. Patch notes go there in the Persona-stamp markdown style (see `CLAUDE.md`).
- **Feedback panel** in-game (`FeedbackPanel.razor`) — players can submit bugs/suggestions
- **Live events scaffolding** exists — currently dormant
- **Leaderboards** via `Sandbox.Services` — Tamer XP, beasts contracted, etc.

---

## Devtools

- `BeastbookEdit` — in-game beast data editor (debug-mode tool)
- `SlotEdit` — debug roster manipulation
- **MCP server integration** — when running, the s&box editor exposes ~95 automation tools (scene inspection, screenshots, console commands, log fetching). Useful for in-game playtest validation. Currently not always reachable; verify before relying on it.

---

## Important files / hotspots

| Area | File |
|---|---|
| **Combat sim** | `Code/Systems/BattleSimulator.cs` |
| **AI** | `Code/Systems/BattleAI.cs` (always uses `BattleSimulator.CurrentRandom`) |
| **Species** | `Code/Core/MonsterManager.cs` (143 species; 20 launch + retired AI-gen kept for save compat) |
| **Moves** | `Code/Core/MoveDatabase.cs` |
| **Expeditions** | `Code/Core/ExpeditionManager.cs` (mini-expedition flag, weighted spawn pool) |
| **Bosses** | `Code/Data/BossPoolDatabase.cs` |
| **Items / drops** | `Code/Core/ItemManager.cs` |
| **Save** | `Code/Data/SaveBlob.cs`, `Code/Core/SaveService.cs`, `Code/Core/SaveApiClient.cs` (schema v1) |
| **Guilds** | `Code/Core/GuildManager.cs` |
| **PvP (gated)** | `Code/Core/CompetitiveManager.cs`, `Code/Systems/BattleChecksum.cs` |
| **Side quests** | `Code/Core/SideQuestManager.cs` |
| **Daily missions** | `Code/Core/DailyRewardManager.cs` |
| **Tutorial** | `Code/Core/TutorialManager.cs` |
| **Skill tree** | `Code/Data/SkillTreeData.cs` + `Code/UI/Panels/SkillTreePanel.razor` |
| **Trades** | `Code/Core/TradingManager.cs` |
| **Chat** | `Code/Core/ChatManager.cs` |
| **Sound** | `Code/Core/SoundManager.cs` |
| **Music** | `Code/Systems/RadioManager.cs` |
| **HUD root** | `Code/UI/GameHUD.razor`, `Code/UI/MainMenu.razor` |

---

## Conventions a new dev MUST know

1. **Read `CLAUDE.md`** before touching anything — has the s&box CSS quirks table (50+ rows) and project rules
2. **Patch notes go in `Assets/data/patchnotes-pending.json` AT ship time** — append a one-line player-facing entry as you merge
3. **Terminology rules**: "Contract" not "catch", no "loyalty" anywhere
4. **Memory system** — Claude's live decision log lives at `~/.claude/projects/<path-slug>/memory/` on the dev machine; a committed snapshot is at `.claude/memory/` (see `HANDOFF.md` for restoring it on a new machine). Read `MEMORY.md` as the index
5. **Decisions log at `.claude/balance-knowledge/decisions-log.md`** — newest-first, full reasoning for every balance + architecture call
6. **Iconify** new lucide names need manual addon refresh; flag every new name per session
7. **Never `new Random()` in combat code** — use `BattleSimulator.CurrentRandom` (deterministic seed-replay contract; load-bearing for v1.3+ PvP)
8. **Never break the SaveBlob schema** without a migration path — old player saves must load
9. **MonsterManager.ValidateAndRepairMonster runs every load** — handles ongoing post-launch drift (renamed moves, pruned traits). Don't delete it.
10. **Don't undo v1.0.3 combat formula choices** (linear stats, Pokemon-spec damage, status fractions) — playtest-validated and load-bearing
11. **Don't introduce a new visual dialect** — Persona stamps + Beastborne dark/violet/gold palette is the language

---

## What's not built (planned later)

- **Real lockstep / authoritative-client PvP** — Scope B for v1.3+ (current "PvP" is async AI-mirror, kill-switched in v1.2.0)
- **Roguelite mode** — design space exists, no implementation
- **Ranked + seasons** — code paths exist behind kill-switches; needs PvP architecture work first
- **Boss Gauntlet / Hard Mode / Cartographer** — features in code, not surfaced in player UI
- **Animated 4-frame idle sheets** for several species (single-frame placeholders pending Jet)
- **Backend player search** — invite picker uses lobby + collected cards; real `players?q=` endpoint deferred
- **Mythic-tier ranked balance** — when first Mythic ships, ranked needs queue-layer cap/banlist (`MatchConfig.BannedRarities` infrastructure already in place)

---

That's the lay of the land. Start with `CLAUDE.md` for the project rules, then poke through `Code/` from the file table above. When in doubt, check the project memory (`.claude/memory/` snapshot, or the live copy at `~/.claude/projects/<path-slug>/memory/`) — most non-obvious "why is it like this?" answers live there.
