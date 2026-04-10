# sbox-ui Agent Learnings

A growing file of things the agent has learned while working on Beastborne UI. Every invocation reads this; every invocation may append to it.

## Rules for writing to this file

1. **Append at the end**, under the correct section (project conventions / user preferences / s&box quirks / style observations / feedback responses). Don't reorganize existing entries.
2. **Every entry MUST be dated** in `YYYY-MM-DD` format.
3. **Every entry MUST cite the source** — file path + line number if from code, task description if from user feedback.
4. **Include the why** — a bare rule is useless later. Future-you needs to know *why* it was learned so you can judge edge cases.
5. **Check for duplicates before writing.** If a similar entry exists, update it instead of adding a new one.
6. **Flag promote-worthy entries** with `[PROMOTE]` tag. These are items important enough that the user should consider moving them to `CLAUDE.md` (agent does NOT write to CLAUDE.md directly).
7. **Mark entries as outdated** with `[OUTDATED]` tag rather than deleting, so there's a record of what changed. Prune `[OUTDATED]` entries older than 3 months on occasion.
8. **Hard cap: ~100 active entries.** If the file is bloating, compress or remove low-value entries.

## Rules for reading this file

- Treat entries as observations with context, not laws.
- An entry dated 6 months ago about a file that no longer exists is worth less than an entry from last week.
- When acting on an entry, verify it's still current (file still exists, rule still applies).
- If an entry contradicts current code reality, flag it and propose updating or removing the entry.

---

## Project conventions

_(Patterns specific to how Beastborne is structured — naming, file locations, state management conventions)_

- **2026-04-09:** Daily popup panel exists and is production-polished at `Code/UI/Panels/DailyPanel.razor` (542 lines) with `DailyPanel.razor.scss` (927 lines). Project memory `project_daily_missions.md` previously said "ready for implementation" but that's stale — the system is live. Source: Explore agent task, 2026-04-09.
- **2026-04-09:** UI particles are implemented via CSS `@keyframes`, NOT a runtime particle system. Reference pattern: `MonsterRosterPanel.razor.scss:1811` `.helix-particle` class. Follow this convention for any new UI burst effects. Why: user confirmed the project has a particle system, but for UI work the established pattern is CSS-based. Source: user confirmation + code grep 2026-04-09.
- **2026-04-09:** No global SCSS variables file (`_variables.scss` or `theme.scss`) exists. Colors are hardcoded per-component. When adding new UI, either reuse values from `style-guide.md` or flag the introduction of a new token with justification. Why: consistency depends on designer discipline, not tooling. Source: Explore agent task.
- **2026-04-09:** Hover sounds use `onmouseover` (NOT `onmouseenter` — unsupported in s&box) with a state guard to prevent repeat-firing. See `feedback_sbox_hover_sound.md` in user memory for the full pattern. Source: user auto-memory.
- **2026-04-09:** `DailyPanel.razor` uses `popVersion` counter on `.pop-@popVersion` CSS class to retrigger the modal open animation each time Show() is called. Source: `DailyPanel.razor:12, 338-339`.

## User preferences

_(Taste calls the user has made — what they like, what they don't, what they've explicitly said)_

- **2026-04-09:** User wants UI to feel *exciting to open*, not just functional. Specifically flagged the Expedition panel as "doesn't feel satisfying or exciting to open" despite being visually consistent. The bar is emotional feel, not style consistency. Source: user message 2026-04-09.
- **2026-04-09:** User is aware of Persona 5's UI and specifically noted that "sometimes it doesn't have the impact" — tells us user distinguishes style from impact and prioritizes impact for Beastborne. Prefer Persona 5 Strikers and P3 Reload over base P5 as references. Source: user message 2026-04-09.
- **2026-04-09:** **Anti-gacha guardrail**: user explicitly stated "we also dont want the game to feel too gatcha at the same time" when approving gacha-adjacent references (HSR). HSR and Genshin are references for aesthetic moments ONLY, never structure. See `ui-references/hsr.md` for full rules. Source: user message 2026-04-09.
- **2026-04-09:** User explicitly wants the agent to keep learning over time and to be able to handle all Beastborne UI, not just specific panels. This file is the learning record. Source: user message 2026-04-09.
- **2026-04-09:** User has a human designer making monsters/beasts — do NOT propose monster designs, descriptions, or mythology picks. The agent's scope is UI only. Source: user message 2026-04-09.

## s&box CSS quirks

_(Things learned about s&box's CSS engine that aren't in CLAUDE.md's quirks table yet)_

- **2026-04-09:** `DailyPanel.razor.scss:770` has `line-height: 40px` on a `font-size: 40px` element — this is flagged as a potential clipping risk by the morning agent dry-run. CLAUDE.md rule says line-height should be numerically ≥ font-size for 30px+. Need to verify empirically if 40px/40px actually clips or if it's fine at this exact ratio. If it clips, this is a real bug to fix during the juice pass. Source: morning agent dry-run 2026-04-09.
- **2026-04-09:** `flex-wrap: wrap` is present in multiple modified razor.scss files (Beastiary 3, Expedition 6, MonsterRoster 8 instances flagged). CLAUDE.md says wrap miscalculates height — but it's widely used in working code. Possibly the quirk only triggers under certain conditions (specific child types? fixed-vs-auto parent height?). Needs investigation before declaring all uses broken. Source: morning agent dry-run 2026-04-09.

## Style observations

_(Observations about Beastborne's visual identity, recurring patterns, signature elements)_

- **2026-04-09:** Beastborne's core color identity: purple `#8b5cf6` (interaction), gold `#fbbf24` (reward), green `#34d399` (success). Backgrounds are dark gray-purple (`#0c0a18` to `#12101e`). This is the palette — new UI should respect it unless there's a specific reason. Source: style-guide.md, extracted from `DailyPanel.razor.scss`, `BattleView.razor.scss`, `MonsterCard.razor.scss`.
- **2026-04-09:** Signature move: escalating heights on day nodes (90→165px across days 1-7). Good pattern for "building toward something" visuals. Source: `DailyPanel.razor.scss:323-348`.
- **2026-04-09:** Signature move: rotating silhouette on Day 7 cycles through 4 legendary species every 3 seconds as a teaser. Could be generalized to "mystery prize" teasers elsewhere. Source: `DailyPanel.razor:75, 362-372`.

## Feedback responses

_(What the user approved, rejected, or refined when reviewing agent proposals)_

- **2026-04-09:** **The proven s&box staircase reveal recipe (after 4 failed attempts):**
  ```csharp
  // C# side
  private int revealStage = 0;
  private async Task PlayRevealSequence() {
      revealStage = 0;
      await GameTask.DelaySeconds(0.12f);
      revealStage = 1;
      await GameTask.DelaySeconds(0.10f);
      revealStage = 2;
      // ... 100ms minimum between stages
  }
  // BuildHash must include revealStage
  ```
  ```razor
  <!-- Razor side -->
  <div class="my-element @(revealStage >= 1 ? "entered" : "")">
  ```
  ```scss
  /* SCSS side */
  .my-element {
      transition: opacity 0.25s ease-out;  /* PLAIN transition, no delay */
  }
  .my-element:not(.entered) {
      opacity: 0;
  }
  ```
  This recipe has been validated against s&box's CSS engine and works. Avoid: `transition-delay`, `:nth-child`/`:nth-of-type`, `animation forwards` for visibility, intervals < 100ms. Source: `DailyPanel.razor` `PlayStreakRevealSequence()` 2026-04-09 (final working version after 3 broken attempts).

- **2026-04-10:** **CRITICAL: `:not(.entered) → .entered` reveal pattern flickers when the panel's BuildHash re-hashes every frame.** Cause: when `BuildHash()` includes a value that increments per frame (like `SpriteAnimator.GlobalFrame` for sprite animations), the panel re-renders every frame. Each re-render causes Razor to re-evaluate conditional class expressions like `@(stage >= N ? "entered" : "")`. Even if the class string is identical, s&box's diff appears to remove-and-re-add the class, restarting the CSS transition. Result: the section flickers on/off rapidly during what should be a smooth fade-in. **Symptoms:** "entered" sub-sections flicker visibly during the reveal; only ONE section reliably animates (the one whose stage flips during the right render-frame window).

  **Where this matters:** any panel that needs sprite-animated child elements (monster cards, idle frames, etc.) and ALSO wants a staged reveal of its UI sections. The two are incompatible with the standard `:not(.entered)` pattern because the per-frame re-render fights the staged class flip.

  **Workarounds:**
  1. **Use a single-stage reveal only** (parent slides in, children are always visible). This is what works in MonsterRosterPanel.
  2. **Remove the per-frame BuildHash addition** and re-render only when state actually changes. Risky — breaks sprite animations.
  3. **Drive the reveal from `Style.SetClass()` in C# code-behind** instead of Razor conditionals — once-only application means no per-frame re-evaluation. Untested but theoretically immune to the issue.

  Source: tested in MonsterRosterPanel 2026-04-10, sub-section staircase reveal failed twice with flicker. Reverted to single-stage. The Daily panel's reveal works because its BuildHash doesn't include a per-frame value. Reference: `MonsterRosterPanel.razor:3140` adds `SpriteAnimator.GlobalFrame` to BuildHash for monster card sprite animation; same file's earlier `BuildHash` was where the conflict originated.

- **2026-04-10:** **CRITICAL TASTE / IDENTITY: There is a recognizable "Claude UI signature" that real players can spot.** Player feedback verbatim: *"Look at the borders. It looks so claude generated. Claude likes those types of borders and those colors."* The signature is composed of:
  - **2px semi-transparent borders** (`border: 2px solid rgba(255, 255, 255, 0.1)` or similar)
  - **Rounded corners on everything** (`border-radius: 12-16px`)
  - **Layered drop shadows** (`box-shadow: 0 4px 16px rgba(0, 0, 0, 0.5)`)
  - **Purple accents everywhere** (`#8b5cf6` / `#a78bfa` / `#c4b5fd`)
  - **Dark cards with low-alpha overlays** (`rgba(20, 20, 35, 0.95)`)
  - **Lucide vector icons** sprinkled as decoration

  This composition is the AI-default "polished modern web" look, NOT a pixel-art monster collector look. Used systematically across an entire UI, it becomes a visual fingerprint that says "AI generated this."

  **The fix is NOT to ban these properties** — they're fine in moderation and on a few elements. **The fix is to STOP applying them uniformly to every container as a reflex.** Real designers vary their visual register: some elements get borders, some don't. Some have shadows, some are flat. Some are purple, most aren't. The variance is what makes UI feel hand-built.

  **For Beastborne specifically (pixel-art monster collector), consider these alternatives:**
  - Hard 1-2px solid color borders (not rgba) using palette colors from the actual game art
  - Pixel-bevel effect (`border-top: 2px solid #lighter; border-bottom: 2px solid #darker`)
  - Sharp corners (radius 0-4px) on game-content elements
  - Solid color blocks instead of semi-transparent overlays
  - Reserve rounded corners + shadows + glow for moments that EARN them
  - Use colors from the existing game palette (element colors, monster sprite colors) not generic UI purples

  **Before adding ANY border/shadow/glow/radius to a new element, ask:** "Am I doing this because the element needs it, or because every other element has one?" If the answer is the latter, leave it off. Heterogeneity is more valuable than consistency for avoiding the AI tell.

  **Source:** real player feedback 2026-04-10 from "matt944" and "kellie" on Beastborne UI work done in this session. Multiple panels (DailyPanel, MonsterRosterPanel, multi-release confirmation) had been built with the signature pattern. The player feedback specifically called out borders + colors as the tell.

- **2026-04-10:** **TASTE: `glow` (drop-shadow / box-shadow with colored alpha) is over-used as a "make it look polished" reflex. The user explicitly called this out as a tell that UI was AI-coded.** Glow should be a SIGNAL (this thing is active, this thing is the hero of the screen, this thing demands attention), not decoration. Sprinkling glow on every element flattens its meaning — when everything glows, nothing glows. **Rules of thumb:**
  1. **Hero/active elements only** — the selected card, the primary action button, the breathing portrait. Not every icon, button, or container.
  2. **Hover states are fine** — glow as an interaction signal ("you can click this") is meaningful. Idle glow is decoration.
  3. **Element identification icons, currency badges, level pills, small UI chrome** — should look clean, not glowing. Size + opacity + color does the job.
  4. **If you find yourself adding glow to "make it pop more"** — first try increasing size, contrast, or weight. Those are the legible alternatives.
  5. **The exception:** signature game-feel moments (the day-7 legendary reveal, a critical hit, a level-up) are appropriate places for big glow. Routine UI is not.

  Source: user feedback 2026-04-10 on the bulk release element icon glow halo (`filter: drop-shadow` with white alpha) — looked over-decorated and "AI-based." Removed it; the icons read fine at full opacity + 18px size alone.

- **2026-04-10:** **`calc()` for vertical positioning behaves unexpectedly in s&box.** Specifically `bottom: calc(100% + 8px)` on an absolutely-positioned tooltip placed the tooltip MUCH higher than the expected 8px above the parent — it appeared at the very top of a distant ancestor container instead. `calc(100% + Npx)` works for `width:` (e.g. ArenaPanel.razor.scss:1267 uses `width: calc(100% + 40px)` successfully) but not reliably for `bottom:`. **Use hardcoded pixel values for tooltip vertical positioning** instead of percentage-based calc. Source: tooltip placement experiment 2026-04-10 in MonsterRosterPanel.razor.scss `.pill-tooltip`.

- **2026-04-10:** **CRITICAL: do NOT use `transform: scale(...)` on hover for cards in a dense grid.** Causes a hover-flicker loop: hovered card scales up → overlaps neighboring card → cursor is now technically over both cards → browser alternates which one is hovered → both cards flicker between hover and rest states forever. **Safe alternative:** `translateY(-Npx)` only + `box-shadow` + `border-color` for the lift effect. Vertical motion can't push the card sideways into its neighbor so no overlap can occur. Daily panel day-cards get away with `scale(1.18)` because they live in a horizontal `flex: 1` strip with explicit gap, but a dense vertical grid like the monster roster has cards too close together for any scale-up to be safe. Source: bug found in MonsterCard.razor.scss `&:hover` 2026-04-10, user reported flickering when hovering grid cards. Fix: remove the `scale(1.06)` from the hover transform, keep `translateY(-6px)` only.

- **2026-04-10:** `[PROMOTE candidate]` **CRITICAL: hover sound spam from `onmouseover` bubbling — fix with `pointer-events: none` on children, NOT a state guard.** When a card has nested children (icons, text spans, progress bars, reward pips, etc.) and you attach `onmouseover=@(() => SoundManager.PlayHover())` to the parent card, the sound fires constantly as the mouse moves across child element boundaries because each child fires its own mouseover and they all bubble up. The user has reported this issue multiple times across different panels. **The fix is structural, not state-based:**
  ```scss
  .my-card {
      pointer-events: all;
      > * {
          pointer-events: none;
      }
      // Re-enable on any interactive child (claim buttons, etc):
      .claim-btn { pointer-events: all; }
  }
  ```
  This makes the parent card the only event target — child mouseovers don't fire at all, so the sound only plays once when the mouse crosses the card's outer boundary. **Always prefer this over a JavaScript-side state guard** (`if hoveredX != current { play; set; }`) because the structural fix has zero per-frame cost and doesn't require BuildHash plumbing.

  **Reference working pattern:** `.day-node` in DailyPanel.razor.scss has used this since the original implementation — that's why day-node hover never had the spam issue while mission cards did. **Apply this rule preemptively to ANY card-style UI element that plays a hover sound and has nested children.** Source: bug found and fixed 2026-04-10 on mission cards in DailyPanel; same root cause as previous similar reports across other panels.

- **2026-04-09:** `[PROMOTE candidate]` **CRITICAL: s&box does NOT support `transition-delay`.** Confirmed via runtime log: `[6] Didn't handle transition style: transition-delay`. This means staggered reveals via `transition-delay` (the standard CSS technique used in every browser) are impossible in s&box. Workarounds:
  1. **C# state increments** — the MainMenu `entranceStage` pattern, `await Task.Delay(...)` between flag flips, each element keys off `>= N`. **WARNING:** very short delays (<100ms) may be missed by s&box's render/Tick cadence — observed in DailyPanel where 40ms intervals between days resulted in only the final stage being visible. Use ≥120ms intervals to be safe.
  2. **`animation-delay` on keyframe animations** — s&box appears to support `animation-delay` since several files use it. BUT see the separate `animation forwards` learning — combining `opacity:0 + animation-delay + forwards` is unreliable. Test empirically before relying on this approach.
  3. **Multiple staggered class adds via separate state fields** — set `streakRevealStage1`, `streakRevealStage2`, etc. as separate booleans flipped at known-good intervals. More verbose but more predictable.
  Source: 2026-04-09 DailyPanel reveal attempt, runtime log.

- **2026-04-09:** `[PROMOTE candidate]` **CRITICAL: s&box does NOT support `:nth-of-type()` (and likely `:nth-child()` as well).** Confirmed via runtime log: `Error parsing stylesheet: Unsupported Pseudo Class "nth-of-type(1)"`. **This parse error is catastrophic** — when s&box hits an unsupported selector, it appears to stop parsing the rest of the stylesheet, which is why DailyPanel stopped opening entirely after my staircase block introduced `:nth-of-type` selectors. **Symptom:** entire panel goes blank or stops rendering, not just the affected rule. **Workaround:** add explicit per-element classes via Razor (`<div class="my-section section-1">`, `section-2`, etc.) and target `.section-1`, `.section-2` in SCSS instead of `:nth-of-type`. This is more verbose but it's the proven project pattern (e.g. `.day-1`, `.day-2`, ..., `.day-7` in DailyPanel.razor.scss). **Verification needed:** test `:nth-child()` empirically — the previous agent claimed it was "verified used in GuildPanel" but that claim was hallucinated (zero matches in actual grep). Source: 2026-04-09 DailyPanel reveal attempt.

- **2026-04-09:** `[PROMOTE candidate]` **s&box does NOT parse `radial-gradient` AT ALL.** Verified by runtime logs across multiple forms:
  - `radial-gradient(circle at 40% 35%, ...)` → `Cannot read a color from 'circle at 40% 35%'`
  - `radial-gradient(ellipse at center, ...)` → `Cannot read a color from 'ellipse at center'`
  - `radial-gradient(ellipse, ...)` → `Cannot read a color from 'ellipse'`

  **Every radial-gradient form tested so far has failed.** Do NOT use `radial-gradient` in s&box SCSS. For radial glow effects, use **layered `box-shadow`** with increasing blur/spread instead — that's the proven project pattern and it composes cleanly for spotlight/glow effects:
  ```scss
  .spotlight-div {
    width: 40px; height: 8px;
    background-color: rgba(...);
    border-radius: 50%;
    box-shadow:
      0 0 30px 10px rgba(139, 92, 246, 0.35),
      0 0 60px 20px rgba(139, 92, 246, 0.18),
      0 0 90px 35px rgba(139, 92, 246, 0.08);
  }
  ```
  For directional gradients use `linear-gradient(90deg, ...)` or `linear-gradient(135deg, ...)` — those work fine. Source: `DailyPanel.razor.scss` flyer styling + spotlight 2026-04-09, multiple runtime parse failures documented.

- **2026-04-09:** `[PROMOTE candidate]` **For staggered reveals: drive from C# state, NOT from CSS animation delays.** The proven working pattern is MainMenu's `entranceStage` recipe (`MainMenu.razor:370, 493-525`):
  1. C# field `private int revealStage = 0`
  2. `async void PlayRevealSequence()` that ticks `revealStage` up via `await Task.Delay(...)` between each step, calling `StateHasChanged()` after each increment
  3. Razor adds class via conditional: `@(revealStage >= N ? "entered" : "")`
  4. SCSS uses `transition` (NEVER `animation forwards`) on the property + `&.entered` flips to the visible state
  5. Hash-add `revealStage` to `BuildHash()` so the panel re-renders when stage changes

  The key win over `animation: ... forwards` is that **`transition` interpolates from the current computed state to the new state**, which s&box implements correctly. `animation` keyframes appear to have unreliable replay/completion behavior.

  Pattern is now used in `DailyPanel.razor` `PlayStreakRevealSequence()` / `PlayMissionsRevealSequence()` after the animation-based attempt failed twice. When you need to gate elements with existing transitions (like `.day-node` which has `transition: all 0.15s ease`), use `&:not(.entered)` for the hidden state instead of declaring a competing transition. Source: this conversation.

- **2026-04-09:** `[PROMOTE candidate]` **CRITICAL: Never make UI visibility depend on a CSS animation completing in s&box.** Pattern that DOES NOT WORK:
  ```scss
  .element {
    opacity: 0;
    animation: fadeIn 0.3s ease-out 0.2s forwards;
  }
  ```
  In standard browsers this works because `animation-fill-mode: forwards` keeps the element at the final keyframe state. In s&box, *something* in this chain is unreliable — possibly `forwards` not sticking, possibly the animation never firing because the parent class-toggle doesn't replay, possibly delay-based animations starting wrong. The result: elements stay invisibly stuck at `opacity: 0` forever, and the panel renders with nothing in it. **Symptom:** panel background visible (or partially rendering), but text/widgets/cards completely missing. User reports "doesn't pop up properly" or "background showing but no content."

  **The rule:** **Base state of every element must be the visible state.** Animations are additive — they enhance an already-visible element, never reveal an invisible one. If you want a fade-in, do it via the parent's `transition: opacity 0.2s ease` on a class-toggle (the existing `DailyPanel { opacity: 0; &.visible { opacity: 1; } }` pattern at line 1-19 works perfectly because the `transition` interpolates from current state, not a fixed keyframe).

  **For staggered staircase reveals specifically:** there is currently NO known-working pattern in this project. The MainMenu uses class-toggle staging (`@(entranceStage >= 1 ? "entered" : "")`) — that's the closest working analogue. To do a staggered reveal in s&box, drive it from C# state changes that toggle classes, NOT from CSS animation delays on `opacity: 0` base elements. ProfilePanel uses the broken pattern too (`opacity:0 + animation forwards`) at lines 64-93 — it's worth verifying empirically whether ProfilePanel's staircase actually works in-game or whether it has the same silent failure.

  Source: 2026-04-09 DailyPanel Phase 1 reveal staircase. Two attempts to fix it (different selectors) both failed. Removing the `opacity: 0` base rules entirely was what fixed the panel — at the cost of the staircase animation not playing. Trade-off was correct: working panel without animation > broken panel with animation.

- **2026-04-09:** `[PROMOTE candidate]` **s&box modal entrance pattern: use the element-name selector + `&.visible`, NOT a class-name selector on `<root>`.** s&box panels expose their root via the element name (the C# class — e.g. `DailyPanel`, `ProfilePanel`, `AchievementPanel`). The `.visible` class is added to that element when `IsVisible = true` and the SCSS pattern that works is:
  ```scss
  DailyPanel {
    opacity: 0;
    &.visible {
      opacity: 1;
      .child-element { opacity: 0; animation: fadeIn 0.3s ease-out 0.1s forwards; }
    }
  }
  ```
  Adding a class like `class="daily-panel-root visible"` to a child `<root>` div and then writing `.daily-panel-root.visible .descendant { ... }` does NOT work reliably for staggered children — the descendants get stuck at their base `opacity: 0` because the animation either fails to apply or `forwards` doesn't stick. Reference working pattern: `ProfilePanel.razor.scss:31-93`. Symptom of getting it wrong: panel renders (background visible) but all child content stays invisible. Source: bug introduced 2026-04-09 in DailyPanel staircase reveal block, fixed by switching to the element-name selector pattern. **When a panel uses both an element-name root selector AND a `<root class="...">` class on the inner div, ALWAYS use the element-name selector for state-driven animations — that's where `.visible` gets toggled by s&box's panel system.**
- **2026-04-09:** `[PROMOTE candidate]` **s&box Razor scoping rule — `@{...}` block-scope is NOT shared with sibling `@foreach`/`@for` blocks.** Declaring a counter in `@{ int _idx = 0; }` and trying to mutate it from inside a sibling `@foreach (var x in list) { var i = _idx++; }` does NOT compile in s&box Razor. Each `@{...}` block compiles to its own scope; the counter is invisible to the next `@`-block. Symptom: the .razor file silently fails to compile, the panel object never instantiates, `Show()`/`Toggle()` flip `IsVisible` but nothing renders, and the user reports "click does nothing." Verified by: DailyPanel was the only file in the entire `Code/UI/` tree (1 of 25 using `@{...}`) attempting this counter pattern, and the panel didn't render until the pattern was replaced. **Correct pattern:** cache the list inside `@{...}` and use `@for (int i = 0; i < cached.Count; i++) { var item = cached[i]; ... }`. Both the cache and `@for` index are correctly scoped this way. The file's existing day-track loop at `DailyPanel.razor:63` already uses this pattern — when needing an index in a foreach, ALWAYS prefer `for` with a cached list. Source: bug at `DailyPanel.razor:207, 254` introduced 2026-04-09, fixed same day after user reported clicking the MISSIONS button did nothing.

---

## GameHUD currency roll-up — 2026-04-08

- **2026-04-08:** GameHUD `OnUpdate` already calls `SpriteAnimator.Update()` + `StateHasChanged()` every frame that sprites are animating, AND `BuildHash()` includes `SpriteAnimator.GlobalFrame`. This means the HUD is effectively re-rendering per frame whenever anything in the scene is animating — any displayed value mutated during `OnUpdate` will flush to the template naturally. For the currency roll-up lerp this was free; no extra `StateHasChanged()` call needed. Source: `Code/UI/GameHUD.razor:431-434, 1333`.
- **2026-04-08:** Pattern: **lerp-toward-real-value via OnUpdate + float display state** works well in s&box Razor. Use `k = 7.49 / duration` (i.e. `-ln(0.05)/duration`) and `alpha = 1 - exp(-k * dt)` for an ease-out exponential lerp that closes ~95% of the gap over the target duration and is frame-rate independent. Snap when the remaining gap is < 0.5 to prevent fractional tails. This is the reusable pattern for any future "animate a discrete value" work (XP bars, stat gains, health, etc). Source: `Code/UI/GameHUD.razor` `TickCurrencyAnimation()`.
- **2026-04-08:** Icon pulse via CSS class toggle (`.pulse-up` / `.pulse-down`) works — the C# side holds a timer, sets `_pulseDir != 0` while active, hash picks it up, Razor applies the class, the CSS `@keyframes animation: ...` fires once. When the timer expires, direction resets to 0, class is removed. Rapid repeat triggers within the animation duration will NOT retrigger (class is already applied) — acceptable for ambient feedback but note it for any future "streak counter" rapid-fire pulses where you'd need a version counter in the class name like `pulse-up-@pulseVersion`. Source: `Code/UI/GameHUD.razor.scss` `.resource.pulse-up/down`.
- **2026-04-08:** `filter: drop-shadow(...)` with RGBA colors is widely used in the project (57 occurrences across 13 files) and works in s&box. Safe for icon glow effects. Source: grep across `Code/UI/`.
- **2026-04-08:** Always **initialize lerp state lazily to the real value on first frame**, not to zero. The GameHUD roll-up uses `_displayedGold = -1f` as a sentinel meaning "uninitialized, snap on first read/tick" — this prevents the awful "roll from 0 to 50000 gold every time you load a save" bug. Any future lerp state should follow this pattern. Source: `Code/UI/GameHUD.razor` `GetDisplayedGold()` / `TickCurrencyAnimation()`.
- **2026-04-08:** Currency counters in `GameHUD.razor` are three: Gold (`money.svg`), Contract Ink (`ink.svg`), Boss Tokens (`token.svg`). NO Gems display in the HUD top bar even though `TamerManager.CurrentTamer.Gems` exists and has a `GetGems()` helper. If Gems ever become a HUD currency, they'd need to be added to the same roll-up path. Source: `Code/UI/GameHUD.razor:99-131, 893-896`.
- **2026-04-08:** GameHUD `.currency-pill` lives in the top-right of the `.unified-top-bar`, padded `4px 12px`, dark background `rgba(15,12,28,0.85)`, thin purple border. It's a clean anchor for the upcoming DailyPanel particle fly-to-HUD animation — no `overflow: hidden` on the pill, so particles landing on the icons can escape the pill bounds cleanly.

---

## Scan learnings — 2026-04-09 (initial panel inventory)

- **2026-04-09:** Show/Close/Toggle pattern is nearly universal across modal panels — `public static bool IsVisible` + `SoundManager.PlayPopup/PlayPopdown`. Only `DailyPanel` uses the `popVersion++` counter to retrigger entrance animations on repeat-opens. This is the cleanest pattern for "play the open animation every time" and should be promoted to any panel where we want the modal to feel fresh each open (Profile, Shop, Achievement etc). Source: `DailyPanel.razor:338-346` vs `InventoryPanel.razor:385-388`. **[PROMOTE candidate]** — if the user wants a convention, `popVersion++` is the one.
- **2026-04-09:** Currency counters in `GameHUD.razor` top-bar are a static value-snap — no roll-up or pulse when amounts change. This is a project-wide flat moment that affects every reward claim. A Balatro-style roll-up on currency change would upgrade DailyPanel, ShopPanel, mission claims, and expedition rewards in one shared place. Source: `GameHUD.razor:1-40` skim. Implication: propose GameHUD-level counter animation as a shared dependency before per-panel claim juice.
- **2026-04-09:** `MenuPopup.razor:14-15` and `ShopPanel.razor:15` use bare emoji icons (⚔️, 🛒) in headers while the rest of the project uses `ui/icons/*.svg`. Minor consistency drift — worth cleanup when touching those panels.
- **2026-04-09:** `DailyPanel.razor.scss:671` — `.missions-content` has `padding-bottom: 200px`. This is almost certainly a workaround for the s&box scroll-height quirk (scroll containers miscalculating content height with nested flex children). Don't remove it without verifying scroll behavior. Source: direct file read. Related to CLAUDE.md "scroll containers need flat children" quirk.
- **2026-04-09:** `DailyPanel.razor.scss` has TWO `overflow: hidden` declarations on `.daily-modal` (line 39 AND 48). Redundant but harmless — noting in case the second one is a leftover from a stale edit. Source: `DailyPanel.razor.scss:31-49`.

---

## DailyPanel Phase 1 — 2026-04-08 (coordinated claim + stagger reveal + mission card hierarchy)

- **2026-04-08:** **Deferred-manager-call pattern for coordinated claim sequences.** When you want a reward particle to visually "arrive" at a GameHUD counter and trigger its roll-up + pulse, defer the actual `Manager.Instance?.Claim()` call by the flyer's flight duration using `await GameTask.DelaySeconds( 0.45f )` inside an `async void` click handler. The GameHUD currency roll-up (lerps over ~400ms on real-value change) will then start exactly when the flyer crosses the top-right viewport corner, creating a visible cause-effect link. Source: `DailyPanel.razor:455-487` `ClaimDailyReward()`. Why it matters: the previous agent flagged this tradeoff — the deferred-call approach was cleaner than overlapping the roll-up with mid-flight, and `GameTask.DelaySeconds` is already a project idiom (see `MonsterRosterPanel.razor:2807`).

- **2026-04-08:** **Particle layer must be a SIBLING of `.daily-modal`, not a child.** `.daily-modal` has `overflow: hidden` which would clip any fly-to-HUD particles. The fix is to spawn `<div class="claim-particle-layer">` INSIDE `<root>` but OUTSIDE `.daily-modal`, with `position: absolute; top/left/width/height 100%; z-index: 1000; pointer-events: none`. This pattern generalizes: any panel that has `overflow: hidden` on its modal box but needs particles to escape (for fly-to-HUD, for celebration confetti) should use this sibling-layer pattern. Source: `DailyPanel.razor:340-357`, `DailyPanel.razor.scss` `.claim-particle-layer`.

- **2026-04-08:** **Fly-to-HUD via fixed translate works "well enough" without pixel-perfect coordinate measurement.** The DailyPanel reward flyers use `@keyframes flyToHudGold` with a hardcoded `translate(480px, -420px)` at 100% — aiming roughly at the top-right corner from the reward box center. This is sloppy in the sense that on wide ultra-wide monitors the flyer will under-shoot the HUD currency pill, but the player reads it as "it went that way" and the moment GameHUD's own `.pulse-up` fires on the value change, the brain stitches the two events into "the particle landed and the counter pulsed." Not worth measuring pixel-perfect positions with Razor C# layout queries (fragile in s&box). Source: `DailyPanel.razor.scss` `@keyframes flyToHud*`.

- **2026-04-08:** **Stagger reveal retrigger via `.daily-panel-root.visible` class gate.** To replay a staircase entrance animation every time a panel opens, gate the animation selectors under `.daily-panel-root.visible .daily-modal <child>`. When `Close()` removes `.visible` from the root and `Show()` re-adds it, s&box re-evaluates the child selectors and replays the CSS animations. This is cleaner than the `popVersion++` class-cycle pattern for child animations — the pop counter is still useful as a force-redraw trick on the modal itself, but children just need the `.visible` gate. The missions tab staircase similarly retriggers every time the user swaps into the tab because the `@if(activeTab=="missions")` block creates a fresh DOM subtree. Source: `DailyPanel.razor.scss` "PHASE 1 - STAGGERED CONTENT REVEAL" block.

- **2026-04-08:** **`:nth-of-type` and `:nth-child` both work in s&box** and are used extensively (GuildPanel has 29 `animation-delay` instances, most keyed by nth-child). Safe to use for mission card section cascades. When sections can conditionally disappear (DailyPanel's monthly challenge is null-guarded), `:nth-of-type(N)` correctly skips absent siblings — verified by reading GuildPanel's pattern. Source: `GuildPanel.razor.scss:675-684`, `DailyPanel.razor.scss` missions reveal block.

- **2026-04-08:** **Sound layering works — no fancy mixer needed.** Stacking three SoundManager calls in sequence (`PlayClick()` + `PlaySuccess()` + `PlayGoldReward()`) inside the claim handler produces a satisfying layered cue without any special scheduling. The sounds fire on the same frame and the audio system mixes them. Pattern generalizes: for any Tier 2+ claim, layer click (initial tap) + success (finalize) + a reward-specific cue (coin/gem/xp). Source: `DailyPanel.razor:462-464`.

- **2026-04-08:** **Mission card state hierarchy = 3 new state classes** applied via Razor conditionals on `GetMissionPercent() >= 75f`. `.near-complete` gets amber tint border + subtle purple progress-fill pulse. `.completed` gets a green glow box-shadow + the inner claim button pulses 1.0→1.04 every 1.8s. `.claimed` (existing class, extended) now collapses to `max-height: 64px`, `scale(0.96)`, `opacity: 0.35`. Reads at a glance exactly like Marvel Snap daily missions. Source: `DailyPanel.razor.scss` "MISSION CARD STATE HIERARCHY" block.

- **2026-04-08:** **`async void` click handlers work fine in s&box Razor** for coordinated sequences — no special dispatch needed. Use `_ = SomeAsyncTask();` for fire-and-forget inside a non-async context. See MonsterRosterPanel for precedent (`async void SwitchView` at 1271). Source: `DailyPanel.razor:455`, `MonsterRosterPanel.razor:1271,2150`.

### Phase 2 prep notes (Day 7 setpiece)

- **Day 7 detection in current code:** `GetTodayReward().IsDay7 == (GetStreakDayInCycle() == 7)` at `DailyPanel.razor:447`. Phase 1 sets `claimBurstIsMilestone = todayReward.IsDay7` in the claim sequence — Phase 2 should detect this BEFORE calling the routine claim path and instead launch a fullscreen setpiece that bypasses the routine particle/flyer burst.
- **Rotating silhouette DOM location:** `.legendary-sil` at `DailyPanel.razor:75` — rendered INSIDE the `.day-7` node, positioned absolutely at `top: 25px`, opacity 0.12, z-index 0. It rotates through 4 species via `RealTime.Now / 3f % 4` in `GetLegendarySilhouetteIndex()` (`DailyPanel.razor:412`). For Phase 2, the silhouette should be pulled out of the day node into a fullscreen setpiece layer when Day 7 claim fires — OR keep the day-node silhouette and have the setpiece spawn a NEW `.legendary-reveal` element at a higher z-index that resolves from silhouette → lit portrait via the HSR light-pillar + name-slam pattern (see ui-references/hsr.md and hades.md).
- **Headroom for escalation:** routine claim is now 12 gold particles + 1-3 flyers + 450ms flight + ~500ms linger. Day 7 setpiece should clearly dwarf this — target 3-6 second fullscreen sequence with a ramped anticipation phase, resolved reveal, and celebration phase. The current Tier 2 particle count is deliberately held to 12 (not 30-50) to leave room for Phase 2 to go bigger without feeling redundant.
- **Milestone purple particles:** `.claim-particle-layer.milestone` is already wired to tint p2/p5/p8/p11 purple for Day 7 / milestone claims — can be reused or overridden in Phase 2.
- **Reward flyer colors:** the `.fly-gold` / `.fly-ink` / `.fly-token` flyers each use a radial-gradient sphere with a matching box-shadow glow. If Phase 2 wants a new "legendary monster egg" flyer for Day 7, extend with `.fly-legendary` using the gold palette + larger size.
- **GameHUD pulse-up class timing:** fires once when the currency value changes, classes auto-clear. Does NOT retrigger if the same currency changes again within the pulse window. The Phase 1 claim stagger is safe because gold/ink/token are three different classes that won't collide. Phase 2's bigger payoff can count on pulse-up firing predictably.
- **`GetTotalUnclaimedCount()` static helper** at `DailyPanel.razor:511` already exposes the total unclaimed count for badge display — the GameHUD daily button badge pattern (Marvel Snap reference) can hook into this.

## Agent self-check before writing a new entry

Before adding to this file, ask:
1. Is this observation already captured (in this file, style-guide, css-quirks, feel-principles, or CLAUDE.md)?
2. Is the source concrete (file:line or specific user quote)?
3. Will this be useful in a future task, or is it ephemeral to this one?
4. If someone else read this in 3 months with no context, would they understand?

If any answer is "no", don't write the entry.
