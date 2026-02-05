# Beastborne - Claude Guidelines

## AI Art Generation (PixelLab)

When the user asks for AI art prompts for any monster, follow these guidelines:

### Core Principle: Description First

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
