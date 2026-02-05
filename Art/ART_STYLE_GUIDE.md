# Beastborne Art Style Guide

## Overview
This document defines the consistent pixel art style for all Beastborne monster sprites. Use this guide when generating AI art prompts to maintain visual cohesion across all creatures.

**IMPORTANT**: Always reference this document when asked to generate AI art prompts for Beastborne creatures.

**Theme**: Creatures are based on myths, folklore, and dark fantasy - cute but with an underlying mysterious/mythological feel. Each creature has lore about its origins.

---

## Core Style Definition

### Base Prompt Template

**Description field:**
```
128x128 pixel art sprite, [CREATURE DESCRIPTION], cute but mysterious [MATERIAL] creature, expressive simple eyes, [ELEMENT-APPROPRIATE ACCENTS], [COLOR PALETTE] tones with darker shading, charming dark fantasy style, white background, clean pixel art
```

**Animation action field:**
```
idle
```

The animation action should be simple - just "idle" works well for the breathing/subtle movement loop.

### Key Visual Elements

1. **Canvas & Format**
   - Sprite sheet format: 4-frame idle animation (for both static icon AND animation)
   - Background: White or light gray (NOT black - makes extraction easier)
   - Clean pixel art with visible pixels
   - Each sprite roughly 64-128px, arranged in 2x2 grid
   - **Workflow**: Frame 1 = static icon, All 4 frames = idle animation

2. **Character Design**
   - **Cute but Mysterious**: Creatures should be charming yet have a mythological/dark fantasy edge
   - **Simple expressive faces**: Dot eyes or simple shapes, personality through body language
   - **Organic materials**: Wood, leaves, water, stone, shadow, flame - clearly readable materials
   - **Element-appropriate accents**: Each creature type has its own accent style (NOT always green leaves)
   - **Rounded forms**: Soft, approachable shapes even for mysterious creatures

3. **Color Palette**
   - **Element-appropriate bases**: Browns for wood, blues for water, oranges for fire, etc.
   - **Darker shading**: Use darker versions of base color for depth and mystery
   - **Accent colors**: Match the creature's element and lore
   - **Avoid pure black outlines**: Use dark element-colored outlines instead

4. **Design Philosophy**
   - **Cute meets dark fantasy**: Endearing but with mythological depth
   - **Clear silhouettes**: Each creature should read clearly at small sizes
   - **Personality in poses**: Show character through body language
   - **Lore-driven design**: Creature appearance reflects its origin story
   - **Consistent proportions**: Slightly chibi/cute proportions work well

5. **Animation Frames**
   Always request a 4-frame idle animation loop:
   - Frame 1: Base idle pose (also used as static icon)
   - Frame 2: Slight movement/breathing
   - Frame 3: Peak of motion or alternate pose
   - Frame 4: Return motion (leads back to frame 1)

   This gives you both the static bestiary icon AND animated sprites in one generation.

---

## Prompt Construction

### Structure

**Description field:**
```
128x128 pixel art sprite, [CREATURE], [MATERIAL/BODY TYPE], [KEY FEATURES], [EXPRESSION STYLE], [COLOR PALETTE], [ELEMENT ACCENTS], charming dark fantasy style, white background, clean pixel art
```

**Animation action field:**
```
idle
```

### Example: Twigsnap (REFERENCE - This is the target style)

**Prompt that generated good results:**
```
pixel art sprite sheet, 4-frame idle animation of a small twig creature, body made of intertwined brown branches and sticks, cute simple dot eyes, small twig arms and legs, green leaf sprouts growing from body, warm woody brown tones with bark texture, charming cartoon forest spirit, white background, clean pixel art, game sprite aesthetic
```

**What makes it work:**
- "pixel art sprite sheet, 4 variations" - gets multiple poses
- "cute simple dot eyes" - keeps expressions charming
- "intertwined brown branches" - material is clear
- "warm woody brown tones" - establishes color palette
- "charming cartoon forest spirit" - sets the tone
- "white background" - easy to extract sprites

---

## Element-Specific Templates

For all elements, use **Animation action: `idle`**

### Neutral/Forest Creatures
**Description:**
```
128x128 pixel art sprite, [CREATURE], [WOOD/PLANT MATERIAL] body, cute expressive eyes, [NATURAL ACCENTS like leaves/moss/bark], warm brown and tan tones, charming forest spirit style, white background, clean pixel art
```

### Fire Creatures
**Description:**
```
128x128 pixel art sprite, [CREATURE], [FLAME/EMBER/ASH MATERIAL] body, glowing ember eyes, [FIRE ACCENTS like flame wisps/sparks/smoke], warm orange and red tones with charcoal accents, dark fantasy style, white background, clean pixel art
```

### Water Creatures
**Description:**
```
128x128 pixel art sprite, [CREATURE], [WATER/BUBBLE/SCALE] body, shiny reflective eyes, [WATER ACCENTS like droplets/bubbles/fins], cool blue and teal tones, mysterious aquatic style, white background, clean pixel art
```

### Wind Creatures
**Description:**
```
128x128 pixel art sprite, [CREATURE], [WISPY/FEATHERY/CLOUD] light body, gentle swirling eyes, [WIND ACCENTS like feathers/wisps/spirals], pale gray and silver tones, ethereal spirit style, white background, clean pixel art
```

### Earth Creatures
**Description:**
```
128x128 pixel art sprite, [CREATURE], [STONE/ROCK/CRYSTAL] solid body, gemstone or pebble eyes, [EARTH ACCENTS like crystals/moss/cracks], gray and brown tones with mineral highlights, sturdy mythic style, white background, clean pixel art
```

### Shadow Creatures
**Description:**
```
128x128 pixel art sprite, [CREATURE], [SHADOW/SMOKE/VOID] wispy body, glowing eyes (purple/white/red), [SHADOW ACCENTS like trailing wisps/dark particles], deep purple and black tones with faint glow, dark fantasy style, white background, clean pixel art
```

### Electric Creatures
**Description:**
```
128x128 pixel art sprite, [CREATURE], [CRACKLING/PLASMA/STATIC] energized body, bright glowing eyes, [ELECTRIC ACCENTS like sparks/bolts/static], yellow and electric blue tones, energetic spirit style, white background, clean pixel art
```

### Ice Creatures
**Description:**
```
128x128 pixel art sprite, [CREATURE], [CRYSTALLINE/FROZEN/FROST] icy body, pale glinting eyes, [ICE ACCENTS like frost particles/icicles/snowflakes], pale blue and white tones with cyan highlights, cold ethereal style, white background, clean pixel art
```

### Nature Creatures
**Description:**
```
128x128 pixel art sprite, [CREATURE], [PLANT/VINE/FLOWER] organic body, gentle expressive eyes, [NATURE ACCENTS like petals/vines/spores], green and earthy tones with floral highlights, living forest spirit style, white background, clean pixel art
```

### Metal Creatures
**Description:**
```
128x128 pixel art sprite, [CREATURE], [METALLIC/RUSTED/MECHANICAL] body, glowing core or eye, [METAL ACCENTS like gears/rivets/rust], steel gray and bronze tones, ancient construct style, white background, clean pixel art
```

### Spirit Creatures
**Description:**
```
128x128 pixel art sprite, [CREATURE], [ETHEREAL/GHOSTLY/LUMINOUS] translucent body, soft glowing eyes, [SPIRIT ACCENTS like light particles/halos/wisps], pale gold and white tones with soft glow, divine spirit style, white background, clean pixel art
```

---

## Quality Checklist

Before finalizing a sprite, verify:

- [ ] Creature is cute but has mythological/mysterious quality
- [ ] Expressive eyes (simple but characterful)
- [ ] Clear silhouette readable at small size
- [ ] Element-appropriate accents (NOT always green leaves)
- [ ] Color palette matches element
- [ ] 4 frames form a smooth idle animation loop
- [ ] Frame 1 works as a standalone static icon
- [ ] White/light background for easy extraction
- [ ] Clean pixel art style (visible pixels, not blurry)
- [ ] Design reflects creature's lore/origin

---

## Common Mistakes to Avoid

1. **Too detailed/realistic** - Keep it simple and charming
2. **Black background** - Use white for easier sprite extraction
3. **Pure cute with no mystery** - Balance charm with dark fantasy edge
4. **No personality** - Each pose should show character
5. **Wrong accents for element** - Don't put leaves on fire creatures, etc.
6. **Too dark to read** - Keep contrast good even for shadow creatures
7. **Single pose only** - Always request 4-frame idle animation
8. **Complex eyes** - Simple shapes work best
9. **Too many colors** - Stick to 3-4 main colors per creature
10. **Ignoring lore** - Design should reflect the creature's origin story

---

## Sprite File Workflow

After generating a 4-frame idle animation sprite sheet:

1. **Extract sprites** from the sheet (remove white background)
2. **Save Frame 1** as static icon: `ui/monsters/{species_id}.png`
3. **Save all 4 frames** as animation: `ui/monsters/{species_id}_1.png` through `ui/monsters/{species_id}_4.png`
4. **Update species JSON** with:
   - `IconPath`: points to static icon
   - `AnimationFrames`: array of all 4 frame paths

This workflow means one AI generation = complete sprite set (icon + animation).

---

## Whispering Woods Creatures (Starter Area)

All use **Animation action: `idle`**

### Twigsnap (REFERENCE CREATURE)
*Lore: A small creature made of fallen branches that mimics the sound of snapping twigs to startle prey*

**Description:**
```
128x128 pixel art sprite, small twig creature, body made of intertwined brown branches and sticks, cute simple dot eyes, small twig arms and legs, green leaf sprouts growing from body, warm woody brown tones with bark texture, charming forest spirit, white background, clean pixel art
```

### Dewdrop
*Lore: Formed from morning dew that collected enough memories to gain consciousness*

**Description:**
```
128x128 pixel art sprite, small water droplet creature, translucent blue teardrop-shaped body, big shiny reflective dot eyes, dewdrops and water sparkles around it, cool blue and teal tones with white highlights, cute and mysterious, white background, clean pixel art
```

### Flickermoth
*Lore: Moths that absorbed moonlight until they became partially luminescent spirits*

**Description:**
```
128x128 pixel art sprite, small moth spirit, fuzzy round body with large luminescent wings, big curious dot eyes, feathery antennae, wings with soft glowing patterns, dusty brown and cream tones with pale yellow glow, gentle dark fantasy style, white background, clean pixel art
```

### Mosscreep
*Lore: Patches of moss that grew over forgotten forest spirits, absorbing their essence*

**Description:**
```
128x128 pixel art sprite, small mossy creature, round body covered in soft green moss, peeking dot eyes barely visible, tiny mushrooms growing on back, stubby legs, earthy green and brown tones, shy and mysterious, white background, clean pixel art
```

### Whiskerwind
*Lore: Wind spirits that took form by gathering floating dandelion seeds and cobwebs*

**Description:**
```
128x128 pixel art sprite, small wind spirit creature, wispy fur-like body that flows like breeze, alert dot eyes, long flowing whisker tendrils, floating seed puffs around it, pale gray and silver tones, ethereal style, white background, clean pixel art
```

### Glimshroom
*Lore: Mushrooms that grew in places where forest spirits died, glowing with residual magic*

**Description:**
```
128x128 pixel art sprite, small mushroom creature, spotted mushroom cap head with stubby stem body, sleepy dot eyes, tiny spore particles floating up, bioluminescent spots on cap, warm tan and orange tones with soft glow, mysterious style, white background, clean pixel art
```

### Branchling
*Lore: Saplings that were blessed by ancient tree spirits to walk and guard the forest*

**Description:**
```
128x128 pixel art sprite, small tree spirit, miniature tree-shaped body with branch arms, knot-hole dot eyes, crown of small leaves on top, root-like feet, warm brown bark tones with green leaf accents, curious ancient style, white background, clean pixel art
```

---

## Starter Monsters

These are the three starter monsters players can choose. All should face left.

All use **Animation action:** `gentle breathing with flames flickering and pulsing softly` (or element-appropriate equivalent)

### Embrik (Fire Starter - Stage 1)
*Lore: Born from dying campfires, these small ember spirits carry the last warmth of forgotten flames*
*Evolution: Embrik → Charrow → Ashenmare*

**Description:**
```
128x128 pixel art sprite, small ember creature born from dying campfires, round body made of warm glowing coals and soft flickering flames, cute simple ember eyes with inner glow, small flame wisps rising from head and back, warm orange and red tones with charcoal black accents, charming dark fantasy fire spirit, facing left, white background, clean pixel art
```

**Animation action:**
```
gentle breathing with flames flickering and pulsing softly
```

### Charrow (Fire - Stage 2)
*Lore: A hollow creature with seven burning eyes. It was once a funeral pyre that refused to die, taking the shape of what it consumed.*

**Description:**
```
128x128 pixel art sprite, medium fire creature transitioning from coal to beast, stocky quadruped body made of cracked black coal and obsidian with glowing lava veins, seven small glowing ember eyes on head, short flame tuft mane starting to form, stubby legs with ember hooves, warm orange and red tones with charcoal black body, like a young coal pony, dark fantasy fire spirit, facing left, white background, clean pixel art
```

**Animation action:**
```
gentle breathing with lava cracks pulsing and small flame mane flickering
```

### Ashenmare (Fire - Stage 3 Final)
*Lore: A beast of obsidian and eternal flame, born when a volcano's heart broke. Its hooves leave glass flowers that bloom into fire.*

**Description:**
```
128x128 pixel art sprite, large majestic fire beast made of obsidian and eternal flame, quadruped horse-like body of cracked black obsidian with molten lava veins glowing through cracks, flowing mane and tail of pure flickering flames, multiple glowing ember eyes, hooves trailing small flames, warm orange and red flames with obsidian black body, powerful but elegant dark fantasy fire spirit, facing left, white background, clean pixel art
```

**Animation action:**
```
powerful breathing with flame mane billowing and lava cracks pulsing with heat
```

---

## Water Starter Line

### Droskul (Water Starter)
*Lore: Formed from tears shed at ancient wells, carrying the sorrow and resilience of generations*
*Evolution: Droskul → ??? → ???*

**Description:**
```
128x128 pixel art sprite, small water spirit born from ancient tears, round droplet-shaped body made of deep blue water with swirling currents inside, cute simple reflective eyes with a hint of melancholy, small water droplets floating around it, cool blue and teal tones with white highlights, charming dark fantasy water spirit, facing left, white background, clean pixel art
```

**Animation action:**
```
gentle bobbing with water rippling and droplets orbiting slowly
```

### Wispryn (Wind Starter)
*Lore: Born from echoes that never faded, these swift spirits carry whispered secrets on the breeze*
*Evolution: Wispryn → ??? → ???*

**Description:**
```
128x128 pixel art sprite, small wind spirit born from fading echoes, wispy translucent body made of swirling pale mist and air currents, cute simple swirling eyes, feathery wisps trailing from body, floating dust motes and tiny leaves around it, pale gray and silver tones with soft white highlights, charming ethereal wind spirit, facing left, white background, clean pixel art
```

**Animation action:**
```
gentle floating with wisps swirling and body softly billowing
```

---

## Version History

- v1.0 - Initial style guide based on Twigsnap redesign
- v1.1 - Updated to match actual generated Twigsnap style (cute, expressive, white background, sprite sheets)
- v1.2 - Refined to emphasize cute + dark/mysterious mythological theme, removed forced green leaf accents, added lore integration
- v1.3 - Changed from "4 variations" to "4-frame idle animation" for streamlined workflow (Frame 1 = static icon, All 4 = animation)
