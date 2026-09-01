# Guild Systems Review — facts on the ground (2026-08-31, HEAD 18cd3c4)

Feeding the Guild page redesign. Code-verified; every claim has a file:line receipt.
Files: `Code/Core/GuildManager.cs` (2511 ln), `Code/Core/GuildApiClient.cs` (408 ln),
`Code/Data/Guild.cs` (219 ln), `Code/UI/Panels/GuildPanel.razor` (3443 ln),
`Code/UI/Panels/GuildPanel.razor.scss` (9428 ln). Hosted inside OnlineHubPanel's
"guild" section (`OnlineHubPanel.razor:1165`, input dispatched at `:2267`).

Master switches: `GuildManager.UiEnabled = true`, `RaidsEnabled = false` (GuildManager.cs:30-31).
`CurrentRaidBoss` getter returns null while raids are off (GuildManager.cs:79-84), which
blanks every raid consumer.

---

## 1. WHAT EXISTS AND RUNS

Backend: HTTP API at `http://157.245.10.193.nip.io:3000/api` (GuildApiClient.cs:16),
X-API-Key + X-Steam-Id headers (GuildApiClient.cs:25-34). GuildManager is a
DontDestroyOnLoad singleton that loads from API at boot (`LoadFromApi`, GuildManager.cs:271-326)
and refreshes after every mutation (`RefreshGuildData`, :331-352). Real-time sync between
same-lobby players is s&box `[Rpc.Broadcast]` (presence, refresh pokes, kicks — :2160-2420).

| System | Where | Server? | State |
|---|---|---|---|
| **Create guild** (50k gold, Lv10 min, name 3-24, tag 2-5) | GuildManager.cs:521-611 | POST `/guilds`, refund on fail | **Works** |
| **Join: open/request/invite modes** | RequestToJoin :855-997, AcceptInvite :776-847 | POST `/guilds/:id/members`, `/requests`, `/invites/:id/accept` | **Works** |
| **Leave / disband / 24h hop cooldown** | :617-685, cooldown :2048-2073 (local SaveBlob) | DELETE member / DELETE guild | **Works** |
| **Roles** — Wanderer/Tamer/Warden/Beastlord (Guild.cs:7); promote/demote/kick/transfer/claim-inactive-leader(30d) | :1001-1168 | PATCH member role, server handles ownership transfer | **Works** |
| **Guild level + XP** — curve `200 + 30L³`, L50 = 3.75M (multi-year by design) | GetXPForGuildLevel :1290-1298, AddGuildXP :1300-1339 | POST `/guilds/:id/xp` — server returns level, `LeveledUp` | **Works.** Live sources: +10/catch (ExpeditionManager.cs:1926), +20/expedition — **retry path only, see §5 quirks** (ExpeditionManager.cs:871), weekly-goal claims (5000-7500 ea, GuildManager.cs:1472-1483). +50/arena (CompetitiveManager.cs:890) and +50/raid (:1993) are behind dormant features |
| **Weekly goals** — 3 of a 6-goal pool, Monday UTC reset, deterministic local seed + server-authoritative overlay | pool :1370-1378, seed :1394-1417, track :1423-1466, claim :1472-1505, refresh :1508-1546 | GET/POST `/guilds/:id/goals`, `/goals/track`, `/goals/claim` — client is defensive ("endpoint not deployed yet" fallbacks :1451-1453, :1495) | **Works client-side even offline.** Rendered in **QuestPanel** (:207-1140) and the hub guild zone (OnlineHubPanel.razor:573-579) — **NOT on GuildPanel at all** |
| **Perks** (10, Lv 5-50) | effect hooks :1560-1643; display list GuildPanel code `GetPerkList` (razor line ~3390) | Level from server | **Partially wired — see §5** |
| **Announcements (MOTD) + description** | UpdateMotd :1200-1222, UpdateDescription :1224-1244 | PUT `/guilds/:id` | **Works**, officer-gated, inline pencil-edit on plaza (razor:947-998) |
| **Settings** — join mode, min level, min rank, auto-kick+days | :1172-1198 | PUT `/guilds/:id` | **Round-trips, but auto-kick is never enforced client-side** (only mapped :378,:453 — pure stored toggle) and min-rank gates on a dormant arena |
| **Emblem** — color(8)/icon(10 keys)/shape(3) | UpdateEmblem :1246-1268; option tables GuildPanel razor code lines 1646-1678 | PUT `/guilds/:id` | **Works** (see §"Emblem render sites" below) |
| **Member list + presence** | Members state :71; heartbeat 60s → level/RP/last_seen (:203-215); IsOnline via same-lobby RPC only (:2160-2196, INetworkListener :222-251) | POST `/members/:id/heartbeat` | **Half-works**: `IsOnline` is true only for players in the *same s&box lobby*. Server `last_seen` exists on every member (Guild.cs:77) but **is never rendered** — a guildmate playing in another lobby shows plain offline |
| **Invites picker** — online lobby + CollectedCards contacts | GetInvitableCandidates :2445-2511 | invite POST persists offline | **Works** |
| **Guild chat** | ChatPanel.razor:38 (dock tab), PhoneLauncher.razor:353 (PawPad); ChatManager.cs:682-707 | **No.** Pure RPC broadcast, same-lobby only, zero history/persistence | **Half-wired** — offline/other-lobby members never see messages; lives entirely off the Guild page |
| **Voice rooms** | — | — | **Do not exist for guilds.** VoiceChatManager has zero guild references |
| **Keyboard nav** (summer pass) | TickInput razor code :1816+; plaza zones `tiles→info→roster→perks→danger` (:2070-2280); every view + all 5 modals have handlers; `guild-kb-selected` cursor scss :9270-9428 | — | **Fully wired**, dispatched from OnlineHubPanel.razor:2267 |

## 2. DORMANT / DEAD PATHS

- **Raids — the whole loop is coded and gated off.** Boss rotation (10 launch-roster species,
  14-day periods, GuildManager.cs:49-63, :1706-1760), combo system (:1837-1931), attempts/day,
  score submit + gold + titles (:1936-2040), full UI: raid scene (razor:597-773), team select
  (:776-862), monument (:1000-1029), battle overlay (:1430-1436), results (:1443-1465).
  `RaidsEnabled=false` nulls it all. **Standing rule: stays hidden; geometry may be reserved, nothing rendered.**
- **Guild leaderboard never populates.** LeaderboardPanel category `guild-ranking` reads stat
  `guild-rp-launch` (LeaderboardPanel.razor:333), but the ONLY `Stats.SetValue` for it is inside
  `CompleteRaidAttemptAsync`, leader-only (GuildManager.cs:2026-2027) — i.e. behind the raid gate.
  Dormant by transitivity.
- **Donations: confirmed never implemented.** Zero donation code anywhere — no method, no endpoint
  call, no UI. Referenced only in design comments (Guild.cs:186-193 "donation conversion",
  GuildManager.cs:27-29, :1352-1355). Memory was right.
- **Activity log: server data arrives, UI never shows it.** The guild fetch returns `Log` and it's
  mapped into `ActivityLog` (GuildManager.cs:417-423, :488-494, cap 50); `OnLogUpdated` fires; the
  local `AddLogEntry` helper (:2112) has **zero callers**; `ActivityLog` has **zero occurrences in
  GuildPanel.razor**. Helpers `GetLogIcon`/`GetLogColor`/`FormatLogTime`/`FormatLastSeen` are dead.
  → A feed exists server-side for free (join/leave/kick events at minimum).
- **Guild achievements: defined, tracked, never shown.** `AllAchievements` (5 entries,
  GuildManager.cs:1645-1651), counters sync via POST `/guilds/:id/stats` (:1652-1670), but
  `GetAchievementProgress`/`IsAchievementComplete` have no UI callers, and the shared counters
  (TotalCatches/ArenaWins/Expeditions/Raids, Guild.cs:55-58) render nowhere.
- **GuildRankTier (Bronze→Legendary by summed ArenaPoints)** (GuildManager.cs:98-107): arena is
  dormant, so every guild is Bronze forever. Already stripped from the plaza banner (razor:915-916
  comment) but still shown on the landing hero (:123) and computed-unused at :592 (`rankColor`).
- **Dead UI code in GuildPanel:** `activeTab`/`SetTab`, `memberSearch`/`memberSearchInput`/
  `GetFilteredMembers` (no search field rendered), `showPermissionMatrix`/`GetPermissionMatrix`
  (matrix never rendered), plus a ~700-line legacy "MEMBERS TAB" scss block (scss:3813-4530) from
  the pre-plaza tab layout.

## 3. THE PAGE TODAY — zone map (W/S flow order)

Three views on `guildView` state (`landing` / `browse` / `myguild`); in-guild players skip
straight to `myguild` on open (razor code :1690-1697).

**LANDING** (no-guild, or reachable via Q-back from plaza when in guild):
1. Level gate takeover if Tamer < Lv10 (razor:32-43) — icon + rule text. Sparse by design.
2. Pending-invites banner w/ accept/deny rows (:46-79) — only when invites exist.
3. Hero card — in-guild variant: emblem + name/tag + Lv/members/online/**rank-tier** stats + ENTER
   chevron (:83-134); no-guild variant: "Create a Guild" + cost/cap/perk-count stats (:137-178).
4. Actions row — Browse card (+ found-count sub), placeholder "Invites · none" card when zero (:181-211).
5. Hop-cooldown notice (:213-219).
   Visual state: polished P5 halftone hero; the "Invites · none" card is placeholder-shaped.

**BROWSE** (:227-437): topbar = title slab + search (TextEntry poll) + refresh + back; then 3-col
card grid (emblem, tag pill, name, Lv stamp, members chip, locked chip). Data: API browse
(`BrowseGuildsFromApi` GuildManager.cs:1779-1806) merged with same-lobby RPC `VisibleGuilds`
(razor code :2912-2946). **Detail subview** (:261-357): emblem header, description, stats row
(members / online / **Total RP**), requirements line, member chips (live fetch of `/guilds/:id`,
code :2878-2900), Join/Back. Quirk: API-sourced rows carry **no OnlineCount and no TotalRP**
(mapping GuildManager.cs:1788-1805 sets neither) — those read 0 unless the guild happens to be
broadcasting in your lobby.

**CREATE** (:441-590): header stamp, Lv10 gate, name+tag fields, color/icon/shape emblem pickers,
live preview + 50,000g cost + FOUND cta, error + cooldown notes. Dense, finished.

**MYGUILD = "the plaza"** (:867-1160), one scene, no tabs — W/S keyboard zones in this order
(`_plazaZones` = tiles → info → roster → perks → danger, code :2070):
1. **Action tiles** (absolute top-right, :873-893): Browse · Invites (w/ request-count badge,
   officer) · Settings (officer).
2. **Banner** (:897-931): emblem slab + name + [TAG] + LV stamps, XP slab with fill bar showing
   BOTH percent AND raw `in-level / needed` numbers.
3. **Monuments row** (:945-1029): Announcement stone (MOTD, officer pencil-edit inline) +
   Description stone (same pattern) + raid monument (renders only if `raid != null` — never, today).
   With raids off this row is two similar gray text cards side by side.
4. **Roster** (:1036-1076): "X / Y ONLINE" chip + portrait grid — avatar, online dot, name,
   role-color stripe, role text. Sorted online→role→RP. Click = member action modal.
5. **Perks shrine** (:1078-1120): all 10 perk rows, locked/unlocked, scrollable. For a young guild
   this is 8-10 locked rows of future promises.
6. **Danger bar** (:1123-1158): CLAIM (conditional) / TRANSFER / LEAVE / DISBAND slab buttons under
   a "DANGER ZONE" label — permanently visible.

**Overlays:** settings drawer (right slide-in: emblem editor + membership rules, :1163-1308),
member action modal (:1312-1363), join-requests modal (:1367-1427), invite/transfer picker
(:1468-1530), error toast (:1535-1540). Raid scene/team-select/results markup exists but is
unreachable.

**What the summer keyboard pass wired:** everything — per-view TickInput with blocking-modal
priority (code :1816-1850), zone cycling on Tab, W/S cross-zone flow where roster is the only
2D grid (:2086-2140), settings-drawer row cursor with deferred scroll-follow (:1900-1920),
and a full `guild-kb-selected` cursor vocabulary respecting the scroll-clip laws (scss:9270-9428).

## 4. SERVER SURFACE

Endpoints the client already calls (all via GuildApiClient; receipts = call sites in GuildManager.cs):

| Endpoint | Used by |
|---|---|
| GET `/players/:steamId/guild` (guild+membership+members+log+requests) | boot load :282, post-join :594 |
| GET `/players/:steamId/invites` | :1809-1830 |
| GET `/guilds?page&search` (browse, w/ member_count) | :1779 |
| GET `/guilds/:id` (detail+members+log+requests) | refresh :338, browse detail razor :2884 |
| POST `/guilds` (create) | :575 |
| PUT `/guilds/:id` (settings/motd/description/emblem — partial updates fine) | :1180, :1208, :1231, :1254 |
| DELETE `/guilds/:id` (disband), DELETE `/guilds/:id/members/:sid` (leave/kick) | :675, :639, :1061 |
| POST `/guilds/:id/members` (direct join), PATCH member (role/transfer) | :890, :1025, :1084, :1132 |
| POST `/guilds/:id/members/:sid/heartbeat` (level, RP, rank; bumps last_seen) | :208 |
| POST `/guilds/:id/invites`, POST `/invites/:id/accept`, DELETE `/invites/:id` | :750, :788, :851 |
| POST `/guilds/:id/requests`, `/requests/:sid/approve`, DELETE request | :931, :963, :994 |
| POST `/guilds/:id/xp` (returns xp/level/leveled_up) | :1309 |
| POST `/guilds/:id/stats` (shared counters) | :1669 |
| POST `/guilds/:id/weekly-reset` | :2101 |
| GET/POST `/guilds/:id/goals`, `/goals/track`, `/goals/claim` | :1531, :1446, :1489 — client has "not deployed yet" fallbacks; deploy status unverified from repo |
| GET `/guilds/:id/raid`, POST `/guilds/:id/raid/score` | :1765, :1951 (raid-gated) |

**A redesign can lean on TODAY, zero server work:** the activity log (already returned on both
guild fetches — just render it); per-member `last_seen` (heartbeat keeps it fresh — "last seen 2h
ago" presence, "recently online" grouping); weekly RP / weekly guild XP per member (Guild.cs:80-81,
synced by heartbeat) for a tiny "this week" contribution readout; shared lifetime counters; MOTD/
description/emblem writes; goals (with the local-seed fallback already handling an absent endpoint).

**Needs new server work:** guild chat history/persistence (RPC-only today), cross-lobby *live*
presence (last_seen approximates it), donations (no endpoint), richer feed event types (log write
API is server-internal — client never writes entries), any beast-emblem validation, guild
leaderboard population outside the raid path.

## 5. GAPS A PLAYER WOULD FEEL (5-member friend guild, 1-2 online at once)

Ranked by visibility:

1. **The page is a statue — no reason to open it daily.** Nothing on the plaza changes between
   visits except the XP bar. The three things that DO move daily — weekly goals, guild chat,
   missions — all live elsewhere (QuestPanel, chat dock/PawPad). The guild's own page is the one
   place its weekly goals are *not* shown.
2. **The guild looks dead even when it isn't.** `IsOnline` requires same-lobby; `last_seen` is
   never rendered. Your 4 friends show identical gray "offline" whether they played 10 minutes ago
   or quit in June. In a 5-member world this is the difference between "my guild" and "an empty room."
3. **No pulse / history.** No feed of "Josh contracted a Lochmaw", "goal claimed", "Alex joined" —
   despite the server already shipping a log array on every fetch (§2). Nothing accumulates; nothing
   to scroll back through.
4. **Nothing to do *together*.** Raids (the co-op surface) are hidden; goals are invisible here;
   chat is same-lobby-only. Two friends online simultaneously have zero guild verbs to point at
   each other beyond the invite picker.
5. **Weak identity.** The emblem is a lucide glyph on a colored square. The beast-sprite emblem is
   committed but only the hub renders it (via a treatment the user rejected) — and GuildPanel's own
   `GetEmblemIcon` doesn't understand `beast:` keys, so a beast emblem set today renders as a
   fallback shield across the entire Guild page (see render-site table below).
6. **Progression reads as a wall.** With Lv50 multi-year (by design — do not re-curve), the visible
   progression surface is one XP bar plus 10 perk rows, most locked, several fake (below). Small
   guilds see a list of things they won't have for months and no near-term "next unlock" framing.

**Mechanical quirks found while auditing (facts, flagging only):**
- `CompleteExpedition` (the normal completion path, ExpeditionManager.cs:1536-1595) tracks weekly
  goals but **never calls `AddGuildXP(20)`/`IncrementAchievement("expedition")`** — only the
  `RetryExpedition` path does (:871-872). The advertised per-expedition trickle fires on retry only.
- **Lv5 "Coffer Cut" is dead code**: `GetGoldMultiplier(isExpedition:true)` has no gameplay caller.
  TamerManager.AddGold:577 calls it with the default `false` (always 1.0); ExpeditionManager instead
  applies its own inline **+5% at guild Lv≥2** (:859, :1550) — behavior and perk copy disagree.
- **Lv20 "Beast Trainer" (+10% beast XP) is unwired** — `GetBeastXPMultiplier` is only read by the
  PlayerStatsSummary display (:427), never by any XP-award path.
- **Lv35 (banner FX) / Lv40 (animated frames, custom titles) / Lv50 (hall of fame)** cosmetic halves
  have no implementation anywhere — pure copy.
- Stale comments in TamerManager (:576 "Lv8/Lv15", :704 "Lv4/Lv10") describe a perk table that
  doesn't exist.

**Emblem render sites** (for replacing the treatment everywhere; pennant/flag on the HUB is REJECTED):
- GuildPanel: landing hero :94-98 · browse card :391-396 · browse detail :266-268 · create pickers
  :507-536 + preview :543-545 · plaza banner :902-904 · settings drawer preview+pickers :1203-1233;
  icon maps + `GetEmblemIcon` fallback at razor code :1646-1678, :3332-3336 (**no `beast:` support**).
- OnlineHubPanel: **gz-banner pennant :528-563 (the rejected flag)** · `beast:` sprite resolver
  :3274-3283 · duplicated icon map `HubEmblemIcons` :2659-2679 · guild-color tamer text :890-891.
- ProfilePanel.razor:52 — guild pill colored by EmblemColor.

## 6. CUT CANDIDATES — "does a 5-member friend guild ever use this?"

User ruling: *"remove anything that would not be necessary. less is more."* Verdicts for every
zone/control, ranked by visual noise removed:

| # | Element (receipt) | Verdict | Why |
|---|---|---|---|
| 1 | **Perks shrine — all 10 rows** (razor:1078-1120) | **MERGE → one "next unlock" chip + unlocked count** | Biggest permanent block of locked promises on the page; 4 of 10 perks are unimplemented or mis-wired (§5) and 2 more are raid-gated. Never render a perk that doesn't work. |
| 2 | **Danger bar** (:1123-1158) | **MERGE → bottom of settings drawer** | Leave/Transfer/Disband are used approximately once per guild lifetime; a permanent "DANGER ZONE" strip is pure noise. Keep CLAIM surfacing conditionally (it's time-sensitive). |
| 3 | **Description stone on the plaza** (:975-998) | **MERGE → settings drawer** | Description is recruiting copy for *browsers*; members don't need it beside the MOTD daily. Two near-identical gray text cards collapse to one (Announcement stays — it's the officer's voice). |
| 4 | **Raid markup kept mounted-but-null** (:597-862, :1000-1029, :1430-1465 + huge scss share) | **KEEP CODE, render nothing** | Standing rule. Reserve monument-row geometry at most; today `raid==null` already suppresses it. |
| 5 | **Role text label under every portrait** (:1071) | **CUT** | Role is already triple-encoded: frame color + colored stripe + text. Icon/stripe suffices; text repeats "Wanderer" ×N in a friend guild. |
| 6 | **XP slab % + raw numbers together** (:922-926) | **MERGE** | Same fact twice. One representation (bar + "Lv 7" or bar + XP-to-next) — per constraints, don't touch the curve, just the duplication. |
| 7 | **Browse tile inside your own plaza** (:874-878) | **CUT** | A member browsing other guilds is a guild-hopper edge case; Browse belongs to the no-guild flow. (Landing keeps it.) |
| 8 | **Landing "Invites · none" placeholder card** (:197-211) | **CUT** | Placeholder-shaped: a card whose content is the word "none". Invites already take over as a banner when they exist. |
| 9 | **Landing in-guild hero** (:83-134) | **CUT the view** | In-guild players skip landing on open (code :1690-1697); it's only reachable by Q-backing out of the plaza. Make Q exit to the hub; delete the intermediate summary card. |
| 10 | **GuildRankTier remnants** (landing hero :121-124, unused `rankColor` :592, GuildManager.cs:98-107) | **CUT** | Arena is dormant — every guild reads "Bronze" forever. Already deleted from the plaza banner; finish the job. |
| 11 | **Emblem SHAPE picker** (create :527-536, settings :1224-1233, `EmblemShape` everywhere) | **CUT** | Three barely-distinguishable squares; the committed beast-sprite emblem makes shape moot (beasts are square). One less picker row ×2 surfaces. |
| 12 | **Min-Rank requirement** (settings :1265-1274, browse detail :306, create) | **CUT** | Gates on the dormant arena — always "Unranked". Meaningless filter in a 5-guild world. |
| 13 | **Auto-kick toggle + inactive-days row** (settings :1276-1295) | **CUT** | Not enforced anywhere client-side (§1); a friend guild never auto-kicks. Placeholder-shaped setting. |
| 14 | **Total RP stat on browse detail** (:295-298) | **CUT** | API rows never populate it (§3) — shows 0; and RP is arena-fed (dormant). Same for OnlineCount on browse rows unless RPC-local. |
| 15 | **Browse refresh button** (:250-252) | **CUT** | Load on view-open (already happens); manual refresh is dev furniture. |
| 16 | **RPC `VisibleGuilds` merge in browse** (code :2912-2946, GuildManager.cs:2166-2185) | **MERGE → API only** | Dual-source dictionary merge to slightly improve online counts for same-lobby guilds; complexity nobody perceives. |
| 17 | **Requests modal + invite picker as separate overlays** (:1367-1427, :1468-1530) | **MERGE** | One "people" overlay (requests atop, invite candidates below) — the requests modal already embeds an Invite button (:1380). |
| 18 | **Member search plumbing, tab system, permission matrix, log helpers** (code :1552-1556, :1574, :2952-2960, :3390-3440 matrix, log/lastseen helpers) | **CUT (dead code)** | Zero render sites — pure weight in a 3.4k-line file. Ditto the ~700-line legacy "MEMBERS TAB" scss block (scss:3813-4530). |
| 19 | Plaza roster grid, banner identity, Announcement stone, settings drawer, member action modal, invites banner, create flow, browse grid | **KEEP** | The actual daily surface of a friend guild: who's here, what's new, who leads, join/found. Roster is the heart — spend the reclaimed space on presence (last_seen) and a feed, not more chrome. |

Net effect of cuts 1-3 + 5-8: the plaza drops from six zones to essentially **banner + announcement
+ roster (+ reserved raid slot)** — which is the correct shape for the game's real social scale,
and frees the space the feed/presence work in §5 needs.
