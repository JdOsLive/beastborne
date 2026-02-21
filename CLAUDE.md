# Beastborne - Claude Guidelines

## AI Art Generation (PixelLab)

When the user asks for AI art prompts for any monster, follow these guidelines:

### Core Principle: Mythology & Description First

**Every beast should be based on or inspired by real-world mythology, folklore, or legends.** When creating new beasts, research the source myth and let it inform the creature's design, description, and visual identity.

**The monster's description in MonsterManager.cs is the primary source for visuals.**
- Read the description carefully
- Extract visual cues from the text (colors, forms, effects mentioned)
- The element is secondary - don't force element colors if the description implies something different
- If the description doesn't translate well visually, propose an update first

### Art Style
- **Resolution**: 128x128 pixel art
- **Format**: Sprite sheet with 4 frames for idle animation
- **Facing**: Left (all monsters face left for consistency)
- **Tool**: PixelLab (uses Description + Animation fields)
- **Background**: Dark/transparent background for sprites

### Prompt Structure

**Description field:**
```
[Physical form from description], [key visual features], [colors implied by description], [any magical effects], facing left, 128x128 pixel art, sprite sheet 4 frames idle animation, fantasy monster game art style, dark background
```

**Animation field:**
```
[Idle movement appropriate to creature type], [any effect animations], [secondary motion like tail/wings/wisps]
```

### Element Colors (Reference Only)

These are fallback suggestions if the description doesn't imply specific colors:

| Element | Suggested Colors | Common Effects |
|---------|------------------|----------------|
| Fire | Orange, red, yellow | Flames, embers |
| Water | Blue, cyan, teal | Bubbles, droplets |
| Earth | Brown, tan, gray | Rocks, dust |
| Wind | White, pale green | Swirls, gusts |
| Electric | Yellow, blue | Sparks, arcs |
| Ice | Light blue, white | Frost, crystals |
| Nature | Green, brown, pink | Leaves, vines |
| Metal | Silver, gray, rust | Gears, shine |
| Shadow | Purple, black | Dark wisps |
| Spirit | Pink, gold, white | Halos, glow |

**Important**: These are suggestions, not rules. A Fire monster described as "black flames" should be black, not orange.

### Evolution Lines

When a monster has EvolvesFrom/EvolvesTo:
1. Check all stages' descriptions
2. Ensure visual progression makes sense narratively
3. If descriptions don't connect well, propose updates before generating prompts

**Progression pattern:**
- Base: Smaller, simpler, cuter
- Mid: Larger, more defined, element more visible
- Final: Majestic/powerful, complex details

### Description Quality Check

Before generating prompts, verify the description works visually:

**Good descriptions include:**
- Physical form hints (ghostly, bird-like, veiled, crystalline)
- Color/material cues (golden, cream, translucent, prismatic)
- Behavioral hints that suggest movement (drifts, floats, crawls)

**Bad descriptions need updating:**
- Too abstract ("keeper of the hour before existence")
- No physical form implied
- Contradicts evolution line visually

### Workflow for Any Monster

1. **Read** the description in MonsterManager.cs
2. **Check** for evolution line (EvolvesFrom/EvolvesTo)
3. **Evaluate** if description translates to visuals
4. **If poor fit**: Propose updated description, get approval, update code
5. **Generate** Description + Animation prompts based on the text
6. **Include**: 128x128, 4-frame idle, dark background

### Examples

**Haloveil** - Description drives everything:
> "When a Dawnmote gathers enough light, it condenses into a veiled spirit crowned by a golden halo."

Visual extraction:
- "veiled spirit" → flowing robes/veil
- "golden halo" → halo above head
- "condensed light" → warm glow, cream-gold colors

```
Description: A veiled ghostly spirit with flowing cream-gold robes, single golden halo floating above its head, trailing ribbon-like sash, ethereal angelic form, soft warm glow, facing left, 128x128 pixel art, sprite sheet 4 frames idle animation, fantasy monster game art style, dark background

Animation: Gentle floating drift, veil and robes billow softly, halo rotates slowly with subtle shimmer, trailing sash flows gracefully
```

**Solmara** - Description drives everything:
> "A radiant bird born from gathered dawn-light, crowned by rings of every color sunrise has ever worn."

Visual extraction:
- "radiant bird" → bird/phoenix form
- "rings of every color sunrise" → multiple colorful halos
- "dawn-light" → warm golden body with prismatic accents

```
Description: A radiant bird-phoenix spirit with elegant swan-like pose, multiple colorful halos/rings in pink orange and rainbow, prismatic wing feathers shimmering with unnamed colors, golden-cream body with luminous glow, facing left, 128x128 pixel art, sprite sheet 4 frames idle animation, fantasy monster game art style, dark background

Animation: Majestic slow wing movements, multiple halos rotate at different speeds, prismatic feathers shimmer and shift colors, radiant aura pulses gently
```

### Updating Descriptions

If the existing description doesn't work visually:
1. Propose updated description that keeps the spirit but adds visual clarity
2. Show how it connects to evolution line (if applicable)
3. Get user approval before changing MonsterManager.cs
4. Then generate prompts matching the new description

---

## s&box Razor UI — CSS Quirks & Gotchas

s&box uses a custom CSS engine that behaves differently from browsers. Keep these rules in mind:

| Issue | Details |
|-------|---------|
| **line-height must be extremely high** | For large font sizes (30px+), `line-height` needs to be 18–24+ or the text gets clipped. Normal values like 1.2 or 2 are not enough. Example: `font-size: 42px` needs `line-height: 24` to render fully. |
| **`overflow: hidden` collapses flex children** | Setting `overflow: hidden` on a flex child can cause it to shrink to zero width/height, making content invisible. Avoid it on flex children. |
| **Scroll containers need flat children** | s&box cannot correctly calculate scroll height when a scrollable container has nested flex containers. Scrollable items must be **direct children** of the `overflow-y: scroll` element — do NOT wrap them in an intermediate div. Follow the roster-grid pattern: parent with `display: flex; flex-direction: column; overflow: hidden; height: Xpx;` and scroll child with `flex: 1 1 0; min-height: 0; overflow-y: scroll;` with items as direct children. |
| **`flex-wrap: wrap` miscalculates height** | The container won't compute its height correctly when children wrap. Use explicit row containers instead (e.g., two `.stats-row` divs instead of one flex-wrap grid). |
| **Bare text renders vertically** | Text not wrapped in a `<span>` or other element inside flex containers may render character-by-character vertically. Always wrap text in elements. |
| **`flex: 1` can fail with multiple siblings** | When a flex container has 3+ children, `flex: 1` may not distribute space correctly. Use explicit `width` values instead. |
| **`inline-flex` not supported** | Use `display: flex` only. |
| **`background: none` not supported** | Use `background-color: transparent` instead. |
| **URL quotes in `background-image` not supported** | Use `url(@variable)` not `url('@variable')` in inline styles — s&box doesn't need quotes around URLs. |
| **Empty divs render as visible panels** | Empty `<div>` elements may render as gray rectangles or scrollbar artifacts. Remove wrapper divs that have no content. |
| **`text-overflow: ellipsis` with `overflow: hidden`** | This combination can collapse the element in s&box flex layouts. Avoid using `overflow: hidden` on text elements inside flex containers. |
| **Duplicate UI across panels** | Some UI components (like move-picker, confirm dialogs) are duplicated in both `MonsterRosterPanel` and `MonsterDetailPanel`. When fixing styles, check BOTH `.razor.scss` files. The roster panel is the primary one used in-game. |
| **Custom fonts must be in `Assets/fonts/` root** | s&box only discovers font files placed **directly** in `Assets/fonts/` — NOT in subdirectories. Place TTF files like `Assets/fonts/Exo2-Bold.ttf`, not `Assets/fonts/Exo2/Exo2-Bold.ttf`. Register in SCSS with `Exo2 { font-family: url("fonts/Exo2-Bold.ttf"); }` and use `font-family: Exo2;` (the embedded font family name from TTF metadata, no space). Resources in `.sbproj` must include `fonts/*`. |

---

## Discord Patch Notes Style Guide

When writing patch notes for Discord announcements, follow this format:

### Structure
```
# 🎮 BEASTBORNE [VERSION] - [UPDATE NAME]

[One-liner hook or tagline]

---

## ⚔️ [MAJOR FEATURE 1]
[Brief description of the feature]

- **[Sub-feature]** — [Description]
- **[Sub-feature]** — [Description]

## 🎒 [MAJOR FEATURE 2]
[Brief description]

- **[Sub-feature]** — [Description]

## 🔧 Improvements & Fixes
- [Fix or improvement]
- [Fix or improvement]

---

*Thank you for playing Beastborne! Join our Discord: [link]*
```

### Discord Markdown Reference
- `# Header` — Large header (only works in forum posts/announcements)
- `## Subheader` — Medium header
- `**bold**` — Bold text
- `*italic*` — Italic text
- `__underline__` — Underlined text
- `~~strikethrough~~` — Strikethrough
- `> quote` — Block quote
- `- item` — Bullet point
- `---` — Horizontal divider
- `` `code` `` — Inline code
- Use emojis liberally for visual appeal

### Tone Guidelines
- Exciting but concise
- Lead with the biggest features
- Use action verbs (Added, Improved, Fixed)
- Keep bullet points to one line when possible
- End with community call-to-action

### Emoji Conventions
| Category | Emoji |
|----------|-------|
| Combat/Battle | ⚔️ |
| Items/Inventory | 🎒 |
| Skills/Abilities | ✨ |
| Monsters/Beasts | 🐉 |
| Fixes/Polish | 🔧 |
| New Content | 🆕 |
| Balance | ⚖️ |
| UI/UX | 🎨 |
| Performance | ⚡ |
| Warning/Important | ⚠️ |

---

## Animated Icon Workflow (SVG → WebP)

s&box does NOT support animated SVGs or CSS `@keyframes`. To create animated icons:

1. **Create animated SVGs** with CSS `@keyframes` animations (user does this manually or with a tool)
2. **Place animated SVGs** in `Assets/ui/icons/animated/` with `-animated.svg` suffix
3. **Convert to animated WebP** using Playwright (headless Chromium renders CSS animations frame-by-frame):
   - Install: `pip install playwright Pillow && python -m playwright install chromium`
   - Load each SVG inline in a headless browser page
   - Screenshot each frame at 50ms intervals (20fps) with transparent background
   - Stitch frames into lossless animated WebP using Pillow
   - Output at **128x128px** resolution for quality when scaled down
4. **Reference the `.webp` files** in Razor, NOT the `.svg` files
5. **CSS hover swap pattern**: Both static SVG and animated WebP `<img>` tags sit in the same container. The animated one is `position: absolute; opacity: 0;` and becomes `opacity: 1;` on `:hover`. No state management needed — pure CSS.

### Button icons still need animated SVGs
The 7 bottom-bar button icons (menu, inventory, chat, effects, music, settings, notification) still have Pillow-generated placeholder WebPs. When ready, create animated SVGs for these and convert them using the same Playwright workflow above. Their static SVGs are in `Assets/ui/icons/buttons/`.
