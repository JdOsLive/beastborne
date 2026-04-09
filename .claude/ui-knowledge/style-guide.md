# Beastborne Style Guide

Concrete visual vocabulary extracted from `DailyPanel`, `BattleView`, `MonsterCard`, and `MonsterRosterPanel`. This is the "looks native to the game" reference — every new UI should use these tokens unless there's a specific reason not to.

Colors are hardcoded per-component in the project (no global `_variables.scss`). When proposing new UI, either reuse these exact values or flag that we're introducing a new token and justify why.

## Color palette

### Primary accents
| Role | Hex | Where used |
|------|-----|------------|
| Primary purple | `#8b5cf6` | Active tabs, primary buttons, hover borders, progress bars |
| Purple light | `#c4b5fd` | Button text on purple backgrounds, subtle glows |
| Purple dim | `#7a5ab0` | Muted purple accents, secondary states |
| Reward gold | `#fbbf24` | Streak numbers, milestone bars, Day 7 highlight |
| Gold deep | `#c0962a` | Day 7 border tint, gold shadows |
| Gold accent | `#f59e0b` | Milestone gradient end, speed stats, warnings |
| Success green | `#34d399` | Claimed rewards, checkmarks, completion badges |

### Backgrounds
| Role | Hex | Where used |
|------|-----|------------|
| Modal background | `#0c0a18` | Outer modal containers |
| Card background | `#0e0c1a` | Mission cards, content panels |
| Card elevated | `#12101e` | Slightly raised panels |
| Dark overlay | `#100e1c` | Locked/disabled states |

### Text
| Role | Color | Where used |
|------|-------|------------|
| High contrast | `#ffffff` | Headers, primary labels |
| Medium (body) | `rgba(255,255,255,0.8)` | Body text, descriptions |
| Medium dim | `rgba(255,255,255,0.6)` | Secondary labels |
| Low (meta) | `rgba(255,255,255,0.4)` | Tertiary labels, timestamps, tiny meta |
| Locked/dim | `#3d3358` | Disabled state text |

### Overlays and transparency tokens
| Purpose | Value |
|---------|-------|
| Header dark overlay | `rgba(8,6,18,0.5)` |
| Modal backdrop | `rgba(0,0,0,0.85)` |
| Subtle border | `rgba(255,255,255,0.06)` |
| Light border | `rgba(255,255,255,0.15)` |
| Purple border | `rgba(139,92,246,0.25)` |
| Purple border hover | `rgba(139,92,246,0.35)` |
| Purple fill subtle | `rgba(139,92,246,0.08)` |
| Purple fill hover | `rgba(139,92,246,0.18)` |

## Typography

No custom font unless specified — uses default s&box UI font via SCSS.

| Element | Size | Weight | Line-height | Notes |
|---------|------|--------|-------------|-------|
| Panel title | 18px | 700 | default | H1 of a panel |
| Section header | 15px | 700 | default | "Daily Missions", "Next Milestone" |
| Card title/name | 14px | 600 | default | Monster name, mission name |
| Body text | 13px | 400-600 | default | Descriptions |
| Small labels | 11-12px | 600-700 | default | "DAY 1", "CLAIM", meta |
| Micro labels | 9-10px | 600 | default | Timestamps, tiny counts |
| Amounts (monospace) | 14px | 700 | default | Currency amounts |
| Hero numbers | 42px | 800 | **42** | Streak number, big displays |

**s&box quirk:** font-size ≥ 30px needs line-height numerically ≥ the font-size, sometimes more. 42px font-size with line-height:42 is the current working value for streak numbers. Smaller line-height causes clipping.

## Spacing tokens

Effective spacing scale inferred from recurring values:

| Token | Value | Use |
|-------|-------|-----|
| xs | 4-6px | Icon gaps, tight inline spacing |
| sm | 8px | Small padding, flex gaps |
| md | 12-16px | Standard card padding, section gaps |
| lg | 20-24px | Panel padding, major section breaks |
| xl | 32-40px | Top-level layout spacing |

## Border radii

| Token | Value | Use |
|-------|-------|-----|
| sm | 5px | Small buttons, claim buttons |
| md | 8-10px | Icons, sort buttons, mission chips |
| lg | 12-14px | Mission cards, content sections |
| xl | 16px | Modal containers |

## Shadows (progressive depth)

| Tier | Value | Use |
|------|-------|-----|
| Subtle | `0 2px 12px rgba(0,0,0,0.3)` | Header bars, light depth |
| Medium | `0 4px 16px rgba(0,0,0,0.5)` | Cards, panels |
| Strong | `0 20px 60px rgba(0,0,0,0.5)` | Modals, heavy floating elements |
| Glow subtle | `0 0 12px rgba(139,92,246,0.2)` | Active states |
| Glow strong | `0 0 20px rgba(139,92,246,0.4)` | Hovered selections |

## Button patterns

### Primary claim button (unclaimed state)
```scss
background-color: rgba(139, 92, 246, 0.18);
border: 1px solid rgba(139, 92, 246, 0.35);
color: #c4b5fd;
border-radius: 5px;
padding: 8px 0;
transition: all 0.15s ease;
&:hover { background-color: rgba(139, 92, 246, 0.25); }
```

### Claimed state (disabled success)
```scss
background-color: rgba(52, 211, 153, 0.1);
border: 1px solid rgba(52, 211, 153, 0.22);
color: #34d399;
cursor: default;
```

### Locked state
```scss
background-color: #100e1c;
border: 1px solid rgba(255, 255, 255, 0.06);
color: #3d3358;
cursor: default;
```

### Tab/filter button (active)
```scss
background-color: rgba(88, 48, 150, 0.5);
border: 1px solid rgba(139, 92, 246, 0.5);
color: #d4bfff;
```

## Card patterns

### Standard content card
```scss
background-color: #0e0c1a;
border: 1px solid rgba(255, 255, 255, 0.08);
border-radius: 12px;
padding: 14px 16px;
```

### Card hover lift
```scss
background-color: rgba(139, 92, 246, 0.08);
border-color: rgba(139, 92, 246, 0.25);
box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3);
transform: translateY(-2px);
transition: all 0.15s ease;
```

## Progress bars

### Mission progress (purple gradient)
```scss
background: linear-gradient(90deg, #8b5cf6, #a78bfa);
border-radius: 3px;
transition: width 0.3s ease;
```

### Milestone progress (gold gradient)
```scss
background: linear-gradient(90deg, #fbbf24, #f59e0b);
border-radius: 3px;
```

## Particle pattern (CSS-based)

Project uses CSS `@keyframes` for UI particles, not a runtime particle system. Reference implementation: `MonsterRosterPanel.razor.scss:1811` `.helix-particle` class.

Basic recipe:
```scss
.burst-particle {
  position: absolute;
  width: 16px;
  height: 16px;
  border-radius: 50%;
  background: linear-gradient(135deg, $color1, $color2);
  box-shadow: 0 0 20px rgba(<color>, 0.8);
  animation: burst-fly 1.4s ease-in forwards;
  pointer-events: none;
}

@keyframes burst-fly {
  0%   { opacity: 1; transform: translate(0, 0) scale(1); }
  50%  { opacity: 1; transform: translate(80px, -20px) scale(1.2); }
  100% { opacity: 0; transform: translate(120px, 40px) scale(0.8); }
}
```

Spawn multiple with stagger via `animation-delay` for a burst effect. For coin/currency bursts, use gold palette; for mission completes, use purple.

## Signature moves (use intentionally)

These are Beastborne's visual fingerprints. Reuse them when appropriate; introduce new ones sparingly.

- **Escalating heights** — the 7-day streak track uses progressively taller day nodes (90→165px) to create visual anticipation toward Day 7. Good pattern for any "build toward something" sequence.
- **Rotating silhouette** — Day 7 shows a legendary species silhouette that rotates through 4 monsters every 3 seconds. Good pattern for "mystery prize" teasers.
- **Purple lift on hover** — cards rise 2-4px + purple border intensification + soft shadow. Standard interactive feedback across all panels.
- **Gold escalation for rewards** — gold intensifies as rewards grow (normal `#fbbf24` → deep `#c0962a` for top-tier). Use for visual hierarchy in reward lists.

## s&box CSS quirks (critical, always honor)

See `CLAUDE.md` and `.claude/ui-knowledge/css-quirks.md` for the full list. Quick hits:
- `line-height` must be numerically high for large font-sizes (30px+ → line-height ≥ font-size)
- `overflow: hidden` on flex children can collapse them
- `flex-wrap: wrap` miscalculates container height — use explicit row divs
- Scrollable children must be direct children of the scroll container
- `inline-flex`, `background: none`, `flex: unset`, `border-style: dashed` all unsupported
- Custom fonts live at `Assets/fonts/` root only, no subdirs
- Empty `<div>` elements can render as visible artifacts
