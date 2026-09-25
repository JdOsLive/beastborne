# Beastborne UI — Guiding Star (canonical style spec)

**Describes the SHIPPED visual language** as of the 2026-08-19 code (Fable 5 panel sweep, Beasts
Center Stage / fd dossier, PawPad, one-cursor + app-color passes). Rewritten 2026-09-25 from a
code audit — the early-July brief it replaces was aspirational and had drifted.

**Precedence:** where this doc and shipped code disagree, **the shipped code wins** (user
ruling) — `MonsterRosterPanel.razor(.scss)` is the living reference; fix this doc. Engine
constraints live in the `CLAUDE.md` quirks table + `laws.md`; component anatomy details in
`style-guide.md` (older — this doc wins on tokens/color/motion); live-HTML swatches in
`.claude/design-system/` (note: several of its cards still show the retired pink fusion
accent — see "Known stale sources" at the end).

> **North star:** *Make the deep feel simple.* Beastborne is genuinely complex (genetics,
> fusion, teams); the UI's job is to make that feel approachable, warm, obvious-at-a-glance.
> **The beasts are the heroes — UI is the frame.**

> **Scope rules (user, 2026-09-25):** the **main menu (`MainMenu.razor`) will be REBUILT on
> the new shared system** as the final phase of the UI overhaul (`ui-overhaul-brief.md`) — until
> then, don't restyle it piecemeal. **BattleView / battle HUD stays OFF-LIMITS.** Don't sweep
> dormant GuildPanel / ArenaPanel (features not live).

---

## Principles (the gut-check)
- **P1 Simple surface, deep system** — lead with one action + the few numbers that matter; tuck genetics/traits/edge stats behind progressive reveal. First read ≠ spreadsheet.
- **P2 Readable at a glance** — big type, high contrast, generous space. If two things fight for "most important," one is wrong.
- **P3 Beasts are the heroes** — chrome stays dark + quiet so sprites/art pop.
- **P4 Color carries meaning** — never decoration. One accent per view: the page's app color.
- **P5 Playful, not noisy** — skew chips, italic energy, dry wit; discipline underneath.
- **P6 The next step is always obvious** — teach through the interface. Every hotkeyed control **wears its key** (`.kb-key` cap). No tooltips as crutches (trait tooltips are retired game-wide — effects are written inline).
- **★ One-line test** — finish "The player is here to ____." If the layout doesn't make that the easiest thing, simplify.

---

## The four signatures

### 1. One cursor — the living violet ring
> ⚠️ **Under review (user, 2026-09-25):** the ring stays only if the overhaul's Phase 1 shows it works as a *universal* selection indicator across mouse, keyboard and controller. If not, it gets replaced. This section describes what ships today.

The selection cursor is **the only perpetually-moving control-level thing on screen.**
- A real bordered element — `border: 4px solid #9b6cff`, transparent fill — **not** box-shadow (s&box distorts shadow corners).
- It is a **root-level absolute element rendered AFTER the content** (a later sibling, so it paints on top), not an `inset:-10px` child of each item. Code writes its `left/top/width/height/border-radius` from the target's `Box.Rect`; it glides on **GLIDE** `0.27s cubic-bezier(0.22, 1, 0.36, 1)` (left/top/width/height only) + opacity 0.15–0.2s. A `.snap` class makes the first lock-on land instantly.
- **Ink-lean:** a continuous per-frame multi-sine skew/rotate wave applied imperatively (never a CSS transition). Menu = full amplitude + liquid stretch on long hops + copies its target's 3D plane; Roster/PawPad = rebalanced wave at ×0.6 with ±1.3% scale breathing. Reference: `MainMenu.razor ~2548-2595`, `MonsterRosterPanel.razor ~2662`, `PhoneLauncher.razor ~776`.
- Radius = host radius + gap (grid card 22 → ring 32; PawPad tile → 30). Size math: see `UpdateGridRing` in `MonsterRosterPanel.razor` (ring-width math is listed as unresolved in `laws.md` — check there before touching it).
- **Scope:** the liquid ring follows **beast/tile selection** (roster grid, PawPad grid, menu, and skewed rings in OnlineHub / Beastbook / SkillTree / Shop / Inventory / MenuPopup). **Controls inside a detail stage/dossier show focus as a `.kb-focused` border recolor** (violet, or the control's own accent on action slabs) with a `kbLand` flash `0.18s` (`#d9c9ff → #9b6cff`) — not a second ring. Hover moves the selection (hover == focus), so there is one cursor, never a separate hover ring.
- **Selected beast card = violet ring + violet fill** (`#2b2250 → #1d1738`, transparent border) in the roster; outside the roster a 2px `#9b6cff` border. **No gold selection rings.**
- **Gold snap:** on PLAY the menu ring snaps to gold `#ffce3a` and slams shut (gap → 2px, 0.09s). Gold is rationed to consequential commits; PLAY is currently the only implemented snap.
- Fusion mode keeps the ring **violet** (the pink fusion accent was retired 2026-07-12).

### 2. Skew dialect (in-game) + angled planes (menu & device)
- **In-game panels speak `skewX`**, not 3D planes: section chips `skewX(-12deg)` with counter-skewed labels; LV / genes pills and FilterBar triggers at `-8deg`; header seam dashes at `-24deg`; rarity pedestal strip `-18deg`; the PawPad accent swipe slants `-12deg`.
- **Receding 3D planes (`perspective() rotateY()`) live only on the main menu** (sidebar `rotateY(13deg)`, cards `10deg`, scene backdrops ±5–7°, Persona pop `translateX(9px) rotateY(13deg) scale(1.04)`) **and the PawPad device lean** (`perspective(1600px) rotateY(-13deg)`, origin right).
- **45° diamonds** remain the action shape (`BbDiamond`, stage `.item-/.fuse-/.release-diamond`, `.detail-diamond`), inner icon counter-rotated.
- ⚠ Engine: a transform-holder can't also carry bg/border/overflow (renders flat under 3D) → surface on a child. Never transform a flex parent whose children rely on flex-grow widths — drive entrances with opacity.

### 3. Color identity — app color per page, violet cursor, rationed gold
- **Every page wears its PawPad app color** — wave header, kicker, seams, chips, section header accent, focus tints on action slabs. Registry = `PhoneLauncher.razor` `Apps` (the source of truth; see Navigation table).
- **Violet `#9b6cff` = the one cursor / "you".** Never used as a page accent substitute.
- **Gold `#ffce3a` (ink `#2a1605`) is rationed:** PLAY, currency, EVOLVE (hero tier), SkillTree INVEST, the `BbButton` primary tier, milestones. *"Gold spent freely is gold worth nothing."* It is NOT the universal primary.
- **Fusion CTA = forge violet** `#7b4ddb` (hover `#6a3fc0`, ready pulse `#7b4ddb ↔ #9b6cff`), two-stage in-button confirm.

### 4. Type voice + stroke discipline + no-glow
- **Editorial voice:** italic 900 display (menu 76/52/38px; panel page title 38px italic), wide-tracked uppercase kickers; hero numerals upright Exo2Black ("record book" register). See Typography.
- **Stroke discipline (revised):** components are filled slabs separated by contrast. Permitted lines: the violet cursor · a left accent bar · the rarity pedestal strip · one panel-signature strip (roster header: 2px `rgba(123,77,219,0.6)`) · **the dossier hairline family** (`#262038` rules, `#2A2340` 1px card borders) and the corner cut. What's banned is the generic *decorative* `rgba(255,255,255,0.1)` hairline on every chip/pill — the "AI dashboard" look.
- **No-glow:** colored halos only as a signal, only outside scroll containers, only in static dark contexts (e.g. the READY CTA breathe, the PawPad HUD chip). Elevation = dark drop shadow (`0 4px 14px rgba(0,0,0,0.45)`), never `inset`.
- **"The concept is the theme, Beastborne is the material":** build from game vocabulary (dark slabs, corner cuts, hairlines, two-tone element light, skew chips, Exo2 italics) — never real-world materials (the April bronze bevel is retired).

---

## Color tokens

**Surfaces (dossier family — current):**
| Token | Hex | Use |
|---|---|---|
| Page / root | `#0A0712` | page bg (menu root `#04060f`) |
| Column | `#130E1D` | containers, corner-cut fill |
| Register card | `#161022` | journal/register rows |
| 1B card | `#1A1428` | corner-cut dossier cards |
| Raised card | `#1C1830` | mini beast card top, BbButton secondary |
| Action slab rest | `#262040` | stage action slabs (hover `#14101f`, press `#0e0b16`) |
| Gauge track | `#221B33` | bar tracks |
| Hairline | `#262038` | rules |
| Card border | `#2A2340` | 1px card borders |
Also: `#0c0a18` modal bg · `#15121f` panel slab · `#131019` empty slot.

**Page accents (PawPad apps):** Beasts `#7b4ddb` · Skills `#ff6bd6` · Expedition `#ee5421` · Online `#3f8fe0` · Beastbook `#2dd4bf` · Shop `#ffce3a` · Quests `#3fb45e` · Bag `#d9a054` · Chat `#4aa8ff` · Radio `#c26bff` · Effects `#f7e024` · Alerts `#e0414a`.

**Semantic:** Violet cursor `#9b6cff` · section chip `#8b5cf6` · kicker/LEARN `#a78bfa` · Gold `#ffce3a` (grad `#ffd95a→#ffb330→#f59312`, ink `#2a1605`) · Red/destructive `#e0414a` · Green/success `#3fb45e` · Blue/info `#3f8fe0` · Discord `#5865f2` (Discord only).

**Text:** warm cream (display) `#F4F1EA` · cool cream (values) `#EFEAF7` · body/prose `#A79DBE` · meta/labels `#7C7295`.

### Element palette
- **Rendering system (the constant):** each element = **dark saturated FILL + bright RIM** badge/glyph tile; the element never floods a card; no rarity-colored card borders. Dual-typed beasts show both, primary first.
- **Reference set = `BbTokens.Element` v1 (fill / rim):** Fire `#7C2410/#FF6A3D` · Water `#1E3A8A/#4AA8FF` · Earth `#6B3F14/#D9A054` · Wind `#0F766E/#2DD4BF` · Electric `#806C00/#F7E024` · Ice `#155E75/#9FE8FF` · Nature `#14532D/#4ADE80` · Metal `#3F4B5C/#C3CDD9` · Shadow `#4A1580/#C26BFF` · Spirit `#86185D/#FF6BD6` · Neutral `#44403C/#A8A29E`. Shadow sits magenta-ward of the cursor violet on purpose. **Wind = teal.**
- ⚠ **Not yet consolidated:** `BbTokens.Element` has no callers; ~16 scss files carry their own element copies (Roster flat set, FilterBar's older `#0d9488/#2dd4bf`-era values, Beastiary tints, Guild). New code should use the BbTokens set; a one-pass consolidation is still open.
- Baked element art: dais platters `ui/dais/dais-<el>.png`, glyph SVGs `ui/icons/elements/background/*.svg`.

### Rarity ladder (pedestal strip, never a border)
Common `#9ca3af` · Uncommon `#22c55e` · Rare `#3b82f6` · Epic `#a855f7` · Legendary `#fbbf24` (dark text) · Mythic `#ec4899`.

### Stat hues
HP `#4ade80` · ATK `#f87171` · DEF `#60a5fa` · SpA `#c084fc` · SpD `#2dd4bf` (newer fd rules; older rules still use cyan `#67e8f9` — known inconsistency) · SPD `#fbbf24`.

### Currency
Gold / Ink / Tokens (`Assets/ui/icons/currency/{money,ink,token}.svg`). **No gems** — `GetGems()` is a dead vestigial field; never surface it.

---

## Typography — Exo 2 weight ladder
Registered families (`GameHUD.razor.scss:1-33`, `MainMenu.razor.scss:1-9`): **`Exo2`** (Bold + BoldItalic via `font-style: italic`), **`Exo2Italic`** (BoldItalic as its own family), **`Exo2Medium`**, **`Exo2SemiBold`**, **`Exo2ExtraBold`** (registered, currently unused), **`Exo2Black`**.

| Role | Family / weight | Size |
|---|---|---|
| Prose (move descriptions, metas, dates) | Exo2Medium 500 | 13px / lh 19px, `#A79DBE` |
| Tile labels, column heads, record labels | Exo2SemiBold 600 uppercase ls 2px | 10px, `#7C7295` |
| Chips, badges, kickers, section labels | Exo2 700 uppercase ls 1.5–3px | 10–11px |
| Move / card names | Exo2Italic 700 | 15px |
| Stat values (right-aligned numeral) | Exo2Italic 700 | 20px / lh 24px |
| CTA voice (ITEM / FUSE / RELEASE) | Exo2Italic 900 uppercase | 15px |
| Sub-view band names | Exo2Italic 900 uppercase | 19px |
| Hero numerals (record tiles, totals) | **Exo2Black 900 upright** | 24px |
| Reveal name (fusion result) | Exo2Italic 900 uppercase | 30px |
| Panel page title | Exo2Italic 900 uppercase | 38px |
| Menu only | Exo2Italic 900 | 52px scene · 76px featured |

Keep dense data roman; italic is the display voice. **`line-height` always in `px`** (unitless = multiplier since 26.06.03); for 30px+ type, line-height ≥ font-size. Full live ladder: `design-system/tokens/type.html`.

---

## Layout, radii & cards
- Designed at **1920×1080**, root scales to fit. Spacing scale 4-based: 4 · 8 · 12 · 16 · 20 · 24 · 32 · 40 · 56.
- **Two card families coexist:**
  - **Dossier "1B" card** (content cards in Roster/Beastbook/SkillTree detail): `#1A1428` fill, `1px solid #2A2340`, **`border-radius: 0`**, with a **corner cut** — a 20×20 `#130E1D` square rotated 45° at top/right −10px with a 1px `#2A2340` bottom edge (`.fd-cut`; SkillTree `.sk-cut`). The cut turns violet on `.kb-focused`.
  - **Rounded chrome:** mini beast card r22, compact card r16, stage collection / journal modal r26, PawPad tiles r22 / screen r30, `BbButton` r16 (size-34 r11), action slabs r14, FUSE CTA r16.
- Ring radius = host radius + gap. Legacy small radii (4/6/8/10/12px) remain in older panels — don't copy them into new work.

### Mini beast card (the atom, 150×180 — `MonsterCard.razor(.scss)`)
- Shell: gradient `#1c1830 → #141021`, static drop shadow `0 4px 14px rgba(0,0,0,.45)`.
- **Top-left:** LV pill + genes/quality pill — `skewX(-8deg)` dark slabs `rgba(8,6,16,.94)`; quality = text color only.
- **Top-right:** favorite star (roster uses an interactive `.card-fav-qt`).
- **Foot:** nameplate with 24px `#262040` element glyph tiles + italic uppercase 12px name.
- **Rarity:** centered 60×4 `skewX(-18deg)` pedestal strip. Never a border, never a flood.
- **No PWR on the mini card** (PWR shows on horizontal/default variants and the detail stage).
- Art is the biggest thing. (A legacy `filter: drop-shadow` still sits on the mini-card sprite — don't copy it; `filter` on `<img>` blurs pixel art.)

---

## Components
- **Buttons — `BbButton` tiers:** primary (gold gradient, ink text — rationed), commit (red `#e0414a`, for Embark-class actions; currently unused), secondary (surface fill + `BbDiamond` icon), ghost, danger. Sizes 56/44/34; ≥44 on main flows. Page CTAs usually wear the page accent instead (e.g. FUSE forge violet).
- **Destructive actions:** inline **two-stage** confirm in the button (FUSE; SkillTree reset = two-stage click OR hold-F ~0.9s). Where a modal is unavoidable (Release), Cancel/Confirm with the **keyboard cursor seeded on Cancel**. `HoldToConfirm` primitive exists but is unused.
- **Keycaps (`.kb-key`):** Exo2Italic 11px/900 `#f4ecd8` on `rgba(8,6,16,.88)` with a 2px black edge. Every hotkeyed control wears one, spatially anchored to the control.
- **Section header:** skew chip (`#8b5cf6`, `skewX(-12deg)`, counter-skewed label) → hairline → skew badge. Shared `BbSectionHeader` (Kicker / Title / AccentColor) on Online, Quests, Bag — accent = app color.
- **Wave header:** `BbHeaderWaves` (baked PNG accent waves, two strips counter-drifting ~30s/18s + opaque header fill) on Roster (violet), Skills (app pink), Beastbook (teal). Kickers are per-page spans (`rh-/sk-/bh-/sh-/mb-kicker`).
- **Sub-view header band:** `.fsub-head` 88px identity band outside the scroll, per-view ambient, back button on Q (Roster journal and move/item pickers).
- **Element dais:** baked PNG platter per element under the stage beast, slow breathe (5.4s) + flare.
- **Background ambients:** `BbIconScroll` glyph drift (Quests, Online, Beastbook, Skills, Shop, Leaderboard, Inventory, Roster).
- **Record tiles / stat rows:** Exo2Black numeral + faint violet ghost watermark; skewed hatched stat bars (`design-system/components/corner-card.html`, `stat-row.html`).
- **Detail stack:** identity (art/name/badges) → lore → actions → stats → moves/traits. Traits as inline rows/chips with the effect text — no hover tooltips.
- **Badges:** element (fill + rim glyph tile), rarity strip, chips (LV, NEW, +N signed delta).

---

## Motion — three layers, one easing language

| Token | Value | Use |
|---|---|---|
| FAST | `0.12s cubic-bezier(0.4, 0, 0.2, 1)` | press / state acks — the sharpest thing on screen |
| STANDARD | `0.2s cubic-bezier(0.4, 0, 0.2, 1)` | focus, hover fills, entrances |
| SLOW | `0.35s cubic-bezier(0.4, 0, 0.2, 1)` | view / mode transitions |
| GLIDE | `0.27s cubic-bezier(0.22, 1, 0.36, 1)` | the cursor ring only (left/top/width/height) |
| SETTLE | `cubic-bezier(0.2, 0.8, 0.3, 1.12)` | entrance overshoot springs (fusion sprite 0.35s, stage pop 0.3s, fav pop 0.14s) |
Also in use: lifts `0.14s ease-out`, `kbLand` 0.18s, PLAY slam 0.09s. (`BbTokens.cs` still lists TSnap 0.14 / TCard 0.16 and the old 16/22/26/30 radius constants — stale; the table above is current.)

| Layer | Owner | Examples |
|---|---|---|
| **FLOW** (ambient) | the scene, never controls | header waves ~30s/18s, dais breathe 5.4s, twinkle 3.2–5.6s, orbit 9s, icon-scroll drift, menu wave video + PLAY sheen 4.8s |
| **ALIVE** | the violet cursor ONLY | GLIDE + per-frame ink-lean |
| **SNAP** (≤150ms) | the control you touched | press dim, `kbLand` border flash, fav pop |

Rules:
- **Hover polarity is light → dark: rest is brightest, hover darker, press darkest** (`.action-v2` `#262040 → #14101f → #0e0b16`; FUSE `#7b4ddb → #6a3fc0`). **Hue never changes between states.** ⚠ `BbButton`, `FilterBar` and `MonsterCard` still brighten on hover — legacy; bring them in line when touched.
- Lifts (`translateY(-3px) scale(1.02)`, press `translateY(1px)`) only OUTSIDE scroll containers. Inside scrolls, fake lift with a lighter top border + darker bottom border.
- **Surfaces are solid.** Panels/buttons/cards never ripple or breathe (the READY CTA's shadow breathe is the sanctioned exception as a signal). ⚠ HARD VETO: no wave/skew motion on a large surface near the player's focus (motion sickness).
- **Player input creates the sharpest motion on screen.**
- **Entrances:** class-toggle transitions for persistent elements; `@keyframes` for fresh mounts. **Staggers are baked into keyframe percentages — never `animation-delay`** (re-renders cancel pending delays; see CLAUDE.md).
- Reserve big setpieces for real milestones (PLAY detonation, fusion ritual). Anti-gacha: celebrate deterministic, visible results; never dramatize an RNG roll.

---

## Navigation — the PawPad
- **The persistent bottom bar is retired** (markup parked under `@if (false)` in `GameHUD.razor`). The only persistent HUD chrome is the **phone button** (bottom-right, wears an `M` keycap, shows a chat + alerts unread badge). Hidden during 3D battle.
- **M or the phone button opens the PawPad** ("PawPad · TamerLink OS"; code `PhoneLauncher.razor`): a right-anchored device tilted `perspective(1600px) rotateY(-13deg)`, sliding in from the right, 3×4 app grid + System Dock. It's a **router, never a content holder** — the one exception being the four in-phone widget apps. Q / Esc / 8 close it.
- **Apps (grid order; key in brackets):**
  | App | Accent | Opens |
  |---|---|---|
  | BEASTS [1] | `#7b4ddb` | tab monsters |
  | SKILLS [2] | `#ff6bd6` | tab skills |
  | EXPEDITION [3] | `#ee5421` | tab expedition |
  | ONLINE [4] | `#3f8fe0` | tab online |
  | BEASTBOOK [5] | `#2dd4bf` | tab beastiary |
  | SHOP [6] | `#ffce3a` | tab shop |
  | QUESTS [7] | `#3fb45e` | QuestPanel overlay |
  | BAG [9] | `#d9a054` | InventoryPanel overlay |
  | CHAT [T] · RADIO [R] · EFFECTS [C] · ALERTS [N] | `#4aa8ff` · `#c26bff` · `#f7e024` · `#e0414a` | in-phone widget apps (no swipe) |
  System Dock: Profile [0] · Settings · Guide · Feedback → popups (no swipe).
- **The accent swipe** (replaced the liquid-expansion circle, which under-covered): a full-screen slab in the app's accent, slanted `-12deg`, 130%×124% of screen — sweeps in from the right (0.22s ease-out cubic), the phone hides and the route fires at full cover, holds 0.03s, sweeps out left (0.20s ease-in cubic); ~450ms total. The tapped icon shoots left 340px / scale 1.12 as it launches. Only tabs + Quests + Bag get the swipe.
- **Router:** `Code/UI/NavManager.cs` — `GoTo(tab)`, `GoToIndex(i)`, `OpenOverlay(id)`, `NotifyTabChanged`, `TabChanged` event, `RoutedThisFrame`/`MarkRouted()` (one press = one route). GameHUD is the registered host.
- **Notifications:** toasts are retired; the phone's **ALERTS** app is the record (last 50). A brief "peek" (≤3 cards, ~3.6s) slides out above the phone button; clicking it opens ALERTS.
- Tab panels keep a 56px `::after` bottom spacer (clears the phone button); Roster uses `margin-bottom: 64px`.

## Input grammar (keyboard)
- **WASD** navigate · **Space / Enter / E** confirm (Space is the displayed key; E a silent alternate — `UiInput.ConfirmPressed()`) · **Q** back (universal) · **R** the page's power action · **Z / X** cycle sections/filters · **M** phone · **1–7, 9, 0** jump to apps · **T / N** open phone to Chat / Alerts · **F** hold-to-reset / page utility.
- Routing: `GameHUD.OnUpdate` → active panel `TickInput()` → `HandleKeyboardInput()`; both stop while `UIModalState.AnyModalOpen`. Modals gate their `Tick()` on `UIModalState.IsTopModal(id)` (ConfirmDialog highest). Register every new blocking popup in `UIModalState`. Never set `AcceptsFocus` on page panels.
- Double-fire guards: a page that consumes Q sets `GameHUD.PanelHandledBackKey`; one that consumes R sets `GameHUD.PanelHandledPowerKey` (both reset each frame); routes and keyboard closes stamp `NavManager.MarkRouted()`. Any new page key that GameHUD also maps needs the same treatment. M toggles the phone both ways.

---

## s&box translation layer (browser CSS that doesn't port)
Authoritative table: `CLAUDE.md`. ⚠️ **s&box 26.09.x (Aug–Sep 2026) changed a lot** (see `sbox-26-09-changes.md`): new layout engine, shader gradients (`transparent`, radial shapes and px stops, conic), `backdrop-filter`, `box-shadow: inset`, `position: fixed`, `display: block/grid`, real blur. Several items below may no longer apply. **Verify in-editor before designing around the new capabilities; until then these are the safe defaults.** The ones that shape this visual language:
- No `backdrop-filter` → near-opaque solid/gradient fills. No `conic-gradient` → linear/solid.
- `radial-gradient` → bare percent stops only; no shape keyword. Circles/wipes = solid scaling/sliding divs (the accent swipe is a skewed solid slab).
- No `transparent` inside gradients → `rgba(...,0)`.
- No `filter` on sprites (`<img>` blurs pixel art) → glow via a behind div, shadow via a wrapper `box-shadow` (never `inset`).
- No CSS border-triangles → iconify or text glyphs. (26.09 adds `border-shape` polygons — verify.)
- `image-rendering: pixelated` on sprites; never fractional-scale pixel art.
- Scroll containers don't clip descendant `box-shadow`/`transform` → no colored glows or lifts on scroll-grid cards; overhanging badges stay inside card bounds.
- No `display: block` / `inline-flex` / `position: fixed` / `box-sizing` (s&box is padding-box: declared size includes padding, borders add on top).
- Replace all emoji with the iconify set.

---

## Voice
UI copy: short, confident, imperative ("Embark", "Out now.", "Five new beasts await."). Lore copy: warm, sensory, a little mythic. Labels UPPERCASE tracked; numbers brag for themselves ("PWR 916"). Dry wit, rare exclamation points, never explain what the player can already see.

---

## Known stale sources (don't follow these parts)
- `.claude/design-system/` cards `cursor.html`, `section-header.html`, `wave-header.html` — still show the retired **pink** fusion ring/chips/waves; fusion CTA shown as gold "go"; `move-slab.html` says "hover is bg-brighten".
- `Code/UI/BbTokens.cs` — TSnap 0.14 / TCard 0.16 and 16/22/26/30 radius constants are stale; `Element`/`Rarity` sets unused.
- `style-guide.md` — April-era cookbook (bronze-bevel buttons, `#8b5cf6` as interaction color, brighten hovers). Use for component anatomy only where it doesn't conflict with this doc.
- Stale code comments: `PhoneLauncher.razor:15-17` (says Slot8 / persistent bar), `GameHUD.razor.scss` + `MonsterRosterPanel.razor.scss:431` ("96px" reserve), FUSE CTA "gold go tier" comment (`MonsterRosterPanel.razor.scss:~6340`).
