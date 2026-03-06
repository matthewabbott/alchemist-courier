# Art Workflow

The complete art pipeline for Alchemist Courier, from placeholder rectangles to final pixel art sprites integrated into Godot.

## 1. Overview

The art pipeline progresses through four stages:

```
Placeholder Rectangles (current)
  -> RetroDiffusion Generation (AI pixel art)
    -> Human Curation (aesthetic decisions)
      -> Godot Import (resource integration)
        -> Runtime (in-game display)
```

**Current state:** All game visuals use procedurally drawn colored rectangles and geometric shapes. Reagent cells are colored by primary aspect (red=hot, blue=cold, etc.). Bag cells are light gray with borders. No sprite assets exist yet in `art/sprites/`.

**Target state:** 32x32 pixel art sprites generated via RetroDiffusion, curated by a human for style consistency, imported into Godot as textures, and loaded at runtime to replace the placeholder rectangles.

## 2. Godot Built-in Tools

### SpriteFrames Editor
- Used for creating sprite animations (idle shimmer, reaction effects, placement feedback)
- Access via the Animation panel when a `Sprite2D` or `AnimatedSprite2D` node is selected
- Supports frame-by-frame animation with configurable FPS
- Import sprite sheets and define frame regions

### Theme Editor
- Used for UI styling (panel backgrounds, button states, fonts)
- Access via the Inspector when editing a `Theme` resource
- Define `StyleBox` overrides for controls (panels, buttons, labels)
- Set font, font size, and color overrides per control type
- The alchemical parchment aesthetic described in `docs/art-and-style.md` will be implemented here

### Remote Inspector
- Debug tool for inspecting and modifying node properties at runtime
- Launch the game, then use **Debugger > Remote Inspector** in the editor
- Useful for tweaking sprite positions, colors, and scales without restarting
- Changes made in the Remote Inspector are temporary (lost on restart)

### Resource Override for Hot-Swapping
- Godot's resource system allows swapping textures without code changes
- Replace a `.png` file on disk and Godot reimports it automatically
- Use `ResourceLoader.Load<Texture2D>(path)` for dynamic texture loading
- The sprite preview tool (`scenes/sprite_preview.tscn`) uses this pattern to display all sprites in `art/sprites/`

## 3. RetroDiffusion Pipeline

### MCP Server Setup
The RetroDiffusion MCP server is configured in `.mcp.json` and provides sprite generation tools directly from Claude Code. The server runs via Node.js at `scripts/mcp/retrodiffusion-server.js`.

Available MCP tools:
- `generate_sprite` -- Generate a single sprite from a text prompt
- `generate_batch` -- Generate multiple sprites in one call
- `create_custom_style` -- Create a custom RetroDiffusion style from reference images
- `list_styles` -- List available generation styles

### Prompt Guidelines
Follow the conventions from `docs/art-and-style.md`:
- **Resolution:** 32x32 pixels per grid cell (all inventory sprites)
- **Palette:** Warm, saturated colors. Avoid dark or desaturated palettes.
- **Aesthetic:** Cute/chibi for characters, detailed-but-readable for items
- **Background:** Transparent for sprites, contextual for scenes
- **Outlines:** Bold outlines for readability at native resolution
- **Silhouettes:** Each reagent should be identifiable by shape alone
- **Aspect coding:** Fire reagents trend warm, toxic reagents trend green, etc.

### Style Creation Workflow (Human-in-the-Loop)
1. Select 5-9 reference images from curated manuscript sources (see `docs/art-references.md`)
2. Use `create_custom_style` MCP tool with the reference images
3. Generate 3-5 test sprites (e.g., "mushroom", "crystal", "flame herb")
4. Human reviews outputs for style consistency and aesthetic fit
5. Iterate on reference selection and prompt wording until satisfied
6. Record the approved `style_id` in `art/retrodiffusion-styles.json`

### Generated Output Location
- Raw generated images land in `art/generated/` (working directory, not final assets)
- Human-approved sprites are moved to `art/sprites/{category}/` with proper naming
- Rejected generations are deleted or archived for reference

## 4. Sprite Replacement Process

### Naming Conventions
All sprites live under `art/sprites/` with the following naming scheme:

```
art/sprites/
  reagents/       reagent_{NAME}.png          e.g. reagent_fire_moss.png
  bags/           bag_{TYPE}.png              e.g. bag_leather_pouch.png
  icons/          icon_{ASPECT}.png           e.g. icon_hot.png
  character/      char_{STATE}.png            e.g. char_idle.png
  ui/             ui_{ELEMENT}.png            e.g. ui_panel_border.png
```

Names use lowercase with underscores. No spaces or special characters.

### Godot Import Settings
When a `.png` is added to the project, Godot auto-generates an `.import` file. Key settings for pixel art:
- **Filter:** `false` (nearest-neighbor sampling, no blurring)
- **Mipmaps:** `false` (not needed for 2D pixel art)
- **Compress Mode:** Lossless (preserve pixel-perfect quality)
- **Size Limit:** 0 (no downsizing)

These settings are configured per-file in the `.import` sidecar files. Set project-wide defaults in **Project Settings > Rendering > Textures > Default Texture Filter = Nearest**.

### Runtime Texture Loading
The game currently uses procedural drawing (colored rectangles). When sprites are available, the replacement pattern is:

```csharp
// Load a texture by resource path
var texture = GD.Load<Texture2D>("res://art/sprites/reagents/reagent_fire_moss.png");

// Apply to a TextureRect or Sprite2D
spriteNode.Texture = texture;
```

The sprite preview tool (`src/Game/Tools/SpritePreview.cs`) demonstrates this pattern -- it scans `art/sprites/` recursively and displays all found PNGs using `GD.Load<Texture2D>()`.

Replacement will happen incrementally: each sprite type (reagents, bags, icons) can be swapped independently. The procedural drawing code remains as fallback for any missing sprites.

## 5. Quality Checklist

Before a sprite is considered production-ready:

- [ ] **32x32 grid alignment** -- Sprite dimensions are exactly 32x32 pixels (or multiples for multi-cell reagents)
- [ ] **Transparent background** -- No opaque background color; alpha channel is clean
- [ ] **Aspect color coding** -- Primary aspect is reflected in the sprite's dominant color (hot=warm reds/oranges, cold=blues, toxic=greens, volatile=oranges/yellows, stable=earth tones, heavy=grays/browns)
- [ ] **Readability at 1x zoom** -- Sprite is recognizable and distinct at native 32x32 display size
- [ ] **Bold outlines** -- Clear silhouette with dark outlines (1-2px) for readability against varied backgrounds
- [ ] **Distinct silhouette** -- Identifiable by shape alone, even without color (accessibility consideration)
- [ ] **Consistent style** -- Matches the established RetroDiffusion style (see `art/retrodiffusion-styles.json` once created)
- [ ] **No banding artifacts** -- Clean color transitions without visible banding from compression
- [ ] **Warm palette** -- Colors are warm and saturated per the visual identity guidelines

Use the sprite preview tool (`scenes/sprite_preview.tscn`) to verify sprites against multiple background colors (dark gray, white, checkerboard) at both 1x and 2x zoom.

## Related Documents

- `docs/art-and-style.md` -- Visual identity, tone, inspirations, and full style guide
- `docs/art-references.md` -- Historical manuscript sources for reference imagery
- `docs/mvp-specification.md` -- Placeholder art strategy and MVP scope
- `docs/architecture.md` -- Asset organization and Godot resource structure
