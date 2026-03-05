# Art & Style Guide

This document covers the visual direction, audio approach, UI/UX design, and asset pipeline for Alchemist Courier. It serves as a reference for both human artists and AI agents generating or integrating art assets.

## Visual Identity

### Tone
**Cozy core, earnest lore.** The visual style should feel:
- Warm and inviting at first glance (chibi characters, cute items, friendly colors)
- Rich and atmospheric on closer inspection (detailed codex illustrations, evocative backgrounds, alchemical symbolism)
- Never dark or grim — even dangerous situations (explosions, toxic fumes) are rendered in a charming, approachable way

### Art Style
- **RetroDiffusion pixel art** — AI-generated pixel art as the primary asset creation method
- **Chibi/cute character designs** — inspired by Recettear, Stardew Valley, Pokemon Mystery Dungeon
- **Item designs grounded in alchemy** — reagent sprites reference actual alchemical imagery (alembics, philosopher's stones, elemental symbols) but rendered in a cute/fantastical style
- **Codex illustrations** — more detailed, "serious" alchemical artwork for the codex/lore entries (aspirational)

### Inspirations
| Source | What We Take |
|--------|-------------|
| **Core Keeper** | Pixel art style, warm palette, cozy underground aesthetic |
| **Pokemon Mystery Dungeon** | Chibi character proportions, expressive sprite animations, warm color palette |
| **Stardew Valley** | Friendly pixel art, approachable UI, cozy farming/gathering feel |
| **Recettear** | Side-view character presentation, adventuring UI, lighthearted tone |
| **Book of Hours** | Aspect system visual design, alchemical/mystical iconography, rich lore art |
| **Backpack Hero** | Inventory grid aesthetic, item sprite design, bag visual design |

## Sprite Resolution

### Grid Cells
- **32x32 pixels** per grid cell
- This applies to: bag cells, reagent cells, meta-grid cells
- All inventory sprites are designed at this resolution

### Character Sprites
- **Side-view / paper-doll** perspective
- Character sprite size: TBD, but likely 64x64 to 128x128 for the main character portrait
- Chibi proportions: large head, small body (roughly 2:1 or 3:2 head-to-body ratio)
- Map/travel view chibi: smaller sprite, possibly 32x32 or 48x48

### UI Elements
- UI is pixelated for style consistency, but rendered at **higher resolution than game sprites**
- The pixelation is an aesthetic choice, not a functional constraint
- UI pixel size may be 1x or 2x depending on what looks best at the target resolution

## Screen Resolution

**Flexible / resolution-independent.** The game should work at any reasonable screen size:
- Design UI to scale (Godot's UI scaling system)
- Pixel art rendered at its native scale (32x32 cells stay 32x32 pixels or integer multiples)
- UI layout adapts to available space
- Test at common resolutions: 1280x720, 1920x1080, 2560x1440
- Web builds must work at various browser window sizes

### Layout Priority
When space is limited, prioritize in this order:
1. The inventory grid (bags + reagents) — this is where gameplay happens
2. The reagent tray (items available to place)
3. The status panel (stamina, aspect totals, warnings)
4. The objective/quest display
5. Decorative elements (character portrait, background art)

## Character Representation

The character evolves across development stages. See `docs/roadmap.md` for the full feature roadmap and tier definitions.

### MVP: Abstract Status Panel
- No visible character during puzzle gameplay
- Status shown as: text/icons for unmitigated aspects, a stamina bar with cross-hatched locked regions
- Stamina bar segments colored by the locking effect (flame icon for heat, snowflake for cold, red cross for injury, skull for poison)

### Post-MVP: Chibi on Map
- Small chibi character shown on the encounter map during ventures
- Simple idle animation, movement animation between nodes
- No bag visualization yet — just the character

### Aspirational: Paper-Doll with Status
- Side-view paper-doll showing the character with equipped bags visible
- Bags displayed on the character's body roughly matching their meta-grid position
- Status effects visible on the character:
  - Sweating / flushed when overheated
  - Shivering / frost when cold
  - Green tint when poisoned
  - Bandages when injured
  - Looking encumbered when carrying a lot
- **Prospective reactions**: Character reacts to unconfirmed placements
  - Starts sweating if a prospective arrangement would cause overheating
  - Panics if the prospective arrangement would cause an explosion
  - Relaxes / smiles when a safe arrangement is found
- Inspired by Risk of Rain 2's approach of visually showing what the player is carrying/wearing

### Dream: Full Expression
- Character sprite shows ALL equipped bags, carried reagents visible as tiny sprites
- Walking animation changes with encumbrance level
- Environmental reactions (squinting in bright areas, huddling in cold regions)
- NPC-quality expressiveness for cutscenes and events

## RetroDiffusion Workflow

### What is RetroDiffusion?
RetroDiffusion is an AI pixel art generation tool. It's the primary method for creating game art assets. The developer specifically wants to leverage this tool throughout development.

### Workflow: Placeholder → Replace Iteratively
1. **Phase 1 (MVP)**: Use colored rectangles and geometric symbols as placeholders
   - Bag cells: light gray rectangles with borders
   - Reagent cells: colored by primary aspect (red=hot, blue=cold, orange=volatile, green=toxic, etc.)
   - Aspect icons: simple geometric shapes (circle, triangle, skull, etc.)
   - No character art needed for MVP
2. **Phase 2**: Generate core reagent sprites with RetroDiffusion
   - Start with the most common/important reagents
   - Establish the visual style with a batch of 10-20 reagent sprites
   - Iterate on style parameters until the look is right
3. **Phase 3**: Generate bag sprites, UI elements, character sprites
   - Bags should look distinct from each other (leather pouch, glass case, cloth sack, etc.)
   - UI elements: buttons, panels, borders in the alchemical parchment theme
   - Character: main apprentice sprite in multiple states
4. **Phase 4**: Generate environment art, codex illustrations, polish sprites
   - Region backgrounds for the encounter map
   - Detailed codex illustrations (aspirational)
   - Animation frames for key sprites

### RetroDiffusion Prompt Guidelines
When generating sprites, keep these parameters consistent:
- **Style**: 32x32 pixel art (or appropriate size for the asset)
- **Palette**: Warm, saturated colors. Avoid dark/desaturated palettes.
- **Aesthetic**: Cute/chibi for characters, detailed-but-readable for items
- **Background**: Transparent (for sprites) or contextual (for scenes)
- **Consistency**: Use reference sprites to maintain style consistency across batches

### Asset Organization
```
res://
  art/
    sprites/
      reagents/       # Individual reagent sprites (32x32 per cell)
      bags/            # Bag sprites and cell textures
      character/       # Character sprites and animations
      ui/              # UI element sprites
      icons/           # Aspect icons, status icons
    backgrounds/       # Scene/encounter backgrounds
    codex/             # Codex illustrations (aspirational)
  placeholder/         # Colored rectangle placeholders (removed when replaced)
```

## UI/UX Design

### UI Theming
- **Alchemical parchment aesthetic**: Menus and panels styled as worn parchment, leather-bound codex pages, wax-sealed missives
- The main character is an apprentice alchemist / courier — UI elements reference their tools and trade
- Font: Pixel font with good readability. Possibly two fonts: a decorative one for headers and a clean one for body text.
- Color scheme: warm browns (parchment), deep greens (alchemy), gold accents (reagent value), red warnings

### Codex UI Integration
The codex is NOT a separate screen that players must navigate to. Instead, its knowledge is **seamlessly embedded in the gameplay UI**:

- **Aspect labels**: Once discovered, aspect icons appear on reagent cells at all times
- **Placement warnings**: Before confirming a placement, the UI highlights dangers:
  - Green glow: safe
  - Yellow glow: approaching a threshold (warning but OK)
  - Red glow: would trigger a dangerous reaction
  - Animated danger indicators on the specific cells that would react
- **Tooltip/hover info**: Hovering over a reagent shows its known properties
- **Status bar projections**: The cumulative status bars (heat, cold, toxicity) update in real-time as the player moves a reagent around, showing what the state WOULD be if they placed it there
- Players should **never be surprised** by a reaction they've already discovered

### Key UI Screens

#### Puzzle Screen (MVP)
- Central: Meta-grid with bags
- Right sidebar: Reagent tray
- Bottom: Status bar (cumulative aspects, stamina)
- Top: Objective, day counter, streak
- Accessible: Primer/rules reference button

#### Venture Map (Post-MVP)
- Full-screen branching node map
- Chibi character on current node
- Node icons showing encounter type
- Stamina bar prominently displayed
- "Distance home" indicator
- Path highlighting (available next nodes)

#### Base/Home (Aspirational)
- Grid-based home storage view (similar to inventory)
- Tabs or navigation for: Storage, Quest Board, Shop, Workstation
- Warm, cozy background art

#### Main Menu
- Alchemical codex aesthetic
- Options: Continue, New Game, Daily Puzzle, Tutorial, Settings
- Save slot selection

### Accessibility Considerations
- Color is never the ONLY indicator of danger (always paired with icons/shapes)
- Text size should be configurable or at least readable at minimum supported resolution
- Placement warnings use multiple cues (color + icon + animation)
- Controls fully remappable

## Audio Direction

### Music
- **TBD** — not a priority for MVP
- Direction: Cozy, atmospheric. Think Stardew Valley's gentle soundtrack meets Book of Hours' mystical ambiance.
- Different themes for: puzzle mode, venture map, encounters, home base
- Use royalty-free / CC0 placeholder tracks during development

### Sound Effects
- **Placeholder / free assets** for MVP
- Key SFX needed:
  - Reagent pickup / place / rotate
  - Successful placement (satisfying "click")
  - Warning sound (approaching threshold)
  - Explosion (dramatic but not scary — more "poof" than "boom")
  - Reaction trigger (sizzle, crack, hiss)
  - Puzzle complete (celebratory jingle)
  - Puzzle fail (deflating "womp")
  - UI navigation clicks
  - Stamina warning beep

### Audio Priority
1. Core puzzle SFX (placement, reactions, win/lose)
2. UI SFX (navigation, buttons)
3. Ambient music (puzzle mode)
4. Venture music and SFX
5. Full soundtrack and environmental audio

## Reagent Visual Design Philosophy

Reagents should be **visually readable at 32x32**. This means:
- Bold outlines
- Distinct silhouettes (each reagent should be identifiable by shape alone)
- Aspect-coded coloring (fire reagents tend warm, toxic reagents tend green, etc.)
- Cute/fantastical aesthetic over realistic (a "giggle mushroom" looks silly and charming, not botanically accurate)
- Aspect icons are displayed ON the reagent cells as overlaid badges/icons, not encoded solely in the sprite art

### Visual Hierarchy on Reagent Cells
```
+------------------+
|  [Reagent Art]   |  ← The sprite itself (fills the cell)
|             [!]  |  ← Aspect icon badge (small, corner-positioned)
|  [glow/border]   |  ← Reaction warning (when hovering/placing)
+------------------+
```

## Related Documents

- `docs/game-overview.md` — Tone and inspiration context
- `docs/core-mechanics.md` — Grid system that art must represent
- `docs/chemistry-system.md` — Aspect system that drives visual coding
- `docs/mvp-specification.md` — Placeholder art strategy for MVP
- `docs/architecture.md` — Asset organization and Godot resource structure
