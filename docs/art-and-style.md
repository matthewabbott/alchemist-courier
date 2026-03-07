# Art & Style Guide

This document covers the visual direction, audio approach, UI/UX design, and asset pipeline for Alchemist Courier. It serves as a reference for both human artists and AI agents generating or integrating art assets.

## Visual Identity

### Tone
**Cozy core, earnest lore.** The visual style should feel:
- Warm and inviting at first glance (chibi characters, cute items, friendly colors)
- Rich and atmospheric on closer inspection (detailed codex illustrations, evocative backgrounds, alchemical symbolism)
- Never dark or grim — even dangerous situations (explosions, toxic fumes) are rendered in a charming, approachable way

### Art Style
- **"Pixel art Secret of Kells"** — the primary style direction. AI-generated pixel art (RetroDiffusion) rendered in an illuminated manuscript aesthetic:
  - **Bold dark outlines** with organic, flowing curves (not rigid geometric)
  - **Rich, warm palette**: golds, deep greens, warm reds, earthy browns — with pops of vivid blue/purple
  - **Celtic/illuminated manuscript ornamental feel**: knotwork-inspired patterns where possible, even at small scale
  - **Flat-ish shading** with limited gradients (suits pixel art well)
  - **Nature motifs**: spiraling vines, leaves, petals, organic shapes
  - **Warm light bias**: everything looks like it's lit by candlelight or firelight
- **Chibi/cute character designs** — inspired by Recettear, Stardew Valley, Pokemon Mystery Dungeon
- **Item designs grounded in alchemy** — reagent sprites reference actual alchemical imagery (alembics, philosopher's stones, elemental symbols) but rendered in a cute/fantastical style
- **Codex illustrations** — more detailed, "serious" alchemical artwork for the codex/lore entries (aspirational)

The "Secret of Kells" direction connects naturally to the existing alchemical parchment UI aesthetic and the "cozy core, earnest lore" tone. The film's flat-shaded, ornamental visual language translates exceptionally well to 32×32 pixel art — bold outlines, limited color per sprite, and decorative detail within small spaces.

### Inspirations
| Source | What We Take |
|--------|-------------|
| **The Secret of Kells** | Primary style reference — flat shading, bold outlines, Celtic spiral/vine motifs, warm gold/green/red palette, nature imagery merging with ornamental patterns |
| **Book of Kells / Lindisfarne Gospels** | Source manuscripts — knotwork interlace, carpet pages, botanical vine scrolls, zoomorphic decoration |
| **Splendor Solis / Vienna Dioscurides** | Alchemical & botanical manuscript illustrations — ornamental borders, herb drawings with flat outlines, gold accents |
| **Pentiment / Inkulinati** | Games using illuminated manuscript aesthetics — proof that this visual language works in interactive media |
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

## Item-First Art Direction

### Core Principle: Item = One Shape

Each reagent item in the catalog has **ONE natural shape** (or at most two). Instead of 8 species x 9 shapes = 72 entries, the catalog contains ~20-30 unique items, each with a shape that naturally fits its visual design.

If we need an L-triomino cold reagent, we don't make a weirdly bent snowflower — we invent a different cold item like "frost fern" or "magma slug." This is:
- **More authentic** to the game concept (a real alchemist's inventory has diverse items)
- **More visually distinguishable** (each item looks genuinely different)
- **Better for art generation** (freeform prompts produce gorgeous results; shape-forced prompts degrade quality)

See `docs/item-catalog-design.md` for the full item list and design philosophy.

### Art Tiers

1. **Inventory sprites** (32x32 per cell): In-game art. Generated freeform, loosely matching the item's natural shape. Not perfectly grid-aligned but close enough to suggest the shape at a glance.
2. **Codex art** (128x128): Higher-detail versions for lore/tooltip/catalog entries. The "true" beautiful depiction. These are the starting point — beautiful art first, shape assessment after.

### Golden Generation Recipe

The best results come from loose, evocative prompts with no grid/shape demands:

```
Style:     kells-alchemy (user_style_f1887992a9e54751ba878b8c5a0ef3b8)
Model:     rd_fast
Size:      128x128
Remove BG: true

Pattern:   "[item name] alchemical reagent, [evocative visual description],
            [color palette], single item centered"
```

**Canonical examples (produced the best art to date):**

| Item | Prompt |
|------|--------|
| Snowflower | "A snowflower alchemical reagent, a crystalline frost flower with ice-blue petals and silver stems, delicate frozen botanical specimen, cool blues and whites with hints of pale violet, single item centered" |
| Firevine | "A firevine alchemical reagent, a curling smoldering vine with glowing embers and small flame-like leaves, warm reds and oranges with gold highlights, botanical illustration style, single item centered" |
| Sharpstone | "A sharpstone alchemical reagent, an angular mineral crystal with sharp faceted edges, deep purple and grey with metallic glints, gemstone specimen style, single item centered" |

### Generation Workflow

1. **Generate freeform** — beautiful items at 128x128 with loose prompts
2. **For multi-cell items** — add gentle shape suggestions without demanding compliance (e.g., "branching fronds in a line" for an I-tri, "a stem splitting into two blooms" for an L-tri)
3. **Generate many variants** — curate the ones that naturally conform close-enough to a needed polyomino
4. **Assess shape mapping after** — look at the result and decide what polyomino it best maps to
5. **If a shape doesn't work** — invent a NEW item for that aspect+shape combo, or keep the art and assign it to whatever shape it naturally suggests

The goal: **pretty reagent art that is a loose match for a polyomino + hover frames that show the true shape.**

### Hover Frames (Shape Communication)

Frames are NOT permanent backgrounds. They appear on **hover and selection only**:
- A ghostly, aspect-themed translucent frame appears on the perimeter of the polyomino
- **Perimeter only** — internal edges between cells of the same reagent are NOT drawn. Only the convex hull boundary gets the frame.
- **Pulse animation** — on hover, the frame pulses (alpha oscillation). On selection, it stays visible at slightly higher opacity, still pulsing.
- **Aspect-themed** — each aspect gets a distinct frame style (generated via RetroDiffusion; dev art uses colored outlines)
- This clearly shows exact cell occupancy when the player needs it, without cluttering the default view

See `docs/visual-polish-design.md` Section 13 for the full hover frame specification.

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
