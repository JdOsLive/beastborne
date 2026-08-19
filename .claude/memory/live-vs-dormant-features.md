---
name: live-vs-dormant-features
description: Which Beastborne features are live/reachable vs. coded-but-dormant — check before polishing or juicing a panel
metadata: 
  node_type: memory
  type: project
  originSessionId: d451fa20-5528-48d8-b237-b196569b2c27
---

As of 2026-06-03, two features exist in code (with full panels) but are NOT live/reachable by players:

- **Online Arena (PvP)** — `ArenaPanel` exists; ranked/casual queue, rank-up, win-streak are all dormant.
- **Guild Raids** — `GuildPanel` has raid UI but raids aren't in the game.

**Everything else is live:** evolution (roster), P2P trading, guilds (join + perks, just not raids), leaderboards, the online hub, mini-expeditions, Hard Mode.

**Why:** `panel-inventory.md` documents panels that exist in *code*, including ones backing unreleased features — so its "target juice tier" notes can point you at dormant surfaces. A batch-2 animation pass juiced `ArenaPanel` (rank-up + win-streak) before realizing arena isn't live; kept it (dormant, ready for launch) but it taught the lesson.

**How to apply:** before a juice/polish pass, confirm the feature is actually switched on. Don't target Online Arena or Guild *raids* as if a player can reach them. Guild *perks* are fine. See [[anim-pass-26-06-03]] for the juice-pass history.
