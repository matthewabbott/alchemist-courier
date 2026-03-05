# Visual Polish Design Document

This document captures the full visual polish vision for Alchemist Courier, covering aspect iconography, highlight rendering, sprite pipelines, and UI refinements. It serves as a reference for future implementation — none of this is code yet.

See also: `docs/art-and-style.md` (visual identity), `docs/chemistry-system.md` (aspect definitions), `docs/core-mechanics.md` (grid system).

---

## 1. Per-Aspect Icon Designs

Each aspect gets a distinct icon for use as cell badges, legend labels, and status bar indicators. Icons must be readable at 8x8 pixels (corner badge on a 32x32 cell) and recognizable at a glance.

| Aspect | Icon | Rationale |
|--------|------|-----------|
| **Hot** | Flame | Universal fire symbol. Single upward flame with warm gradient. |
| **Cold** | Snowflake | Six-pointed crystalline snowflake. Distinct from any other icon silhouette. |
| **Volatile** | Yellow warning triangle | Exclamation mark inside a triangle. Communicates "danger" before players learn the mechanic. |
| **Stable** | Shield | Convex shield shape. Visually communicates protection/buffering. |
| **Heavy** | Anvil | Flat-topped anvil silhouette. Reads as "weight" even at small sizes. |
| **Toxic** | Skull and crossbones | Classic poison symbol. Two crossed bones beneath a skull. |
| **Sharp** | Blade | Single-edge blade pointing upward. Distinct from the shield's rounded shape. |
| **Fragile** | Cracked glass | Wine glass or beaker with a crack line. Communicates vulnerability. |
| **Catalytic** | Catalyst flask | Round-bottom flask with bubbles. Reads as "chemical acceleration." |
| **Medicinal** | Herb / leaf | Simple two-leaf herb sprig. Green tint even at monochrome sizes. |

### Design Constraints
- All icons are drawn as 1-bit silhouettes first, then tinted with the aspect color from `GridConstants.GetAspectColor()`.
- Corner badges use the 1-bit version at 8x8. Legend and status bar use a 16x16 version with color fill.
- Icons must pass a "squint test" — distinguishable from each other when blurred to 50% resolution.
- Accessibility: icons are always paired with the aspect name in tooltips. Color is never the sole differentiator.

---

## 2. Highlight Visual Styles

The highlight system (`src/Core/Highlight/`) currently renders colored border edges on cells. This section defines the visual treatment for each highlight context.

### Constraint Violations (Danger Zones) — Crosshatching
- **Style**: Diagonal crosshatch pattern (45-degree lines, 4px spacing) over affected cells.
- **Color**: Severity-driven — red crosshatch for `Danger`, yellow for `Warning`.
- **Boundary**: Contiguous boundary edges (from `ContiguousBoundary.Compute()`) drawn as solid lines around the crosshatched region.
- **Purpose**: Crosshatching is visually distinct from the pulsing glow used for interactions. Players learn that hatching = "you can't do this."
- **Implementation note**: Use a shader or tiled texture for the crosshatch fill. The existing `CellEdge` flags determine which outer edges get the solid border.

### Active Interactions — Pulsing Glow
- **Style**: Soft glow that pulses in intensity (sinusoidal, 1.5s period).
- **Color**: Aspect-colored from `GridConstants.GetAspectColor()`. Alpha oscillates between 0.15 and 0.45.
- **Boundary**: Contiguous boundary edges rendered as a 3px colored line (current `HighlightEdgeWidth`).
- **Purpose**: Pulsing draws attention to active aspect interactions without fully obscuring the reagents beneath.
- **Layers**: `Interaction` layer highlights pulse. `Legend` layer highlights use a steady (non-pulsing) glow at 0.25 alpha.

### Eagle Sense — Animated Dashed Borders
- **Style**: Dashed border lines that animate (marching ants effect, moving at ~30px/sec).
- **Color**: Aspect-colored, full saturation. Background uses the deep overlay from `DimOverlayColor`.
- **Boundary**: Same contiguous boundary computation, but rendered as dashed rather than solid lines.
- **Purpose**: The marching-ants effect distinguishes eagle sense from normal interaction highlights, signaling "this is a special analysis mode."
- **Dash pattern**: 6px dash, 4px gap. Animation shifts the phase over time.

---

## 3. RetroDiffusion Sprite Pipeline

RetroDiffusion is the AI pixel art tool used for all game art. This section documents the pipeline for generating specific asset types.

### Reagent Portraits (32x32 per cell)
1. **Prompt template**: `"pixel art, 32x32, [reagent name], alchemical ingredient, [primary aspect] theme, warm palette, transparent background, cute fantasy style"`
2. **Multi-cell reagents**: Generate a single sprite at (32*W)x(32*H) covering the bounding box, then slice into cells. Ensure visual continuity across cell boundaries.
3. **Aspect tinting**: Base sprite uses neutral colors. The game tints cells at runtime using `GetDominantColor()`, so sprites should avoid strong hue bias.
4. **Batch workflow**: Generate 10-20 reagents per session. Use the first 3-4 as style anchors, then reference them for consistency in subsequent prompts.
5. **Quality gate**: Each sprite must pass the silhouette test — recognizable as a distinct shape when reduced to a single color.

### Bag Textures
1. **Cell fill**: Each bag type gets a distinct cell texture (e.g., leather grain for Normal, frosted glass for Null, riveted metal for Gravity, iron bars for Locked).
2. **Generation**: Create a tileable 32x32 texture for each bag type. The texture tiles across all cells in the bag.
3. **Border**: Bag borders are rendered procedurally (not part of the texture). The texture only fills the cell interior.

### Aspect Icons
1. Generate each icon at 32x32, then downsample to 16x16 and 8x8 variants.
2. Use 1-bit (black on transparent) as the master. Color tinting is applied at runtime.
3. Prompt: `"pixel art, 32x32, [icon description], simple silhouette, black on white, crisp edges, icon style"`

---

## 4. Contiguous Boundary Rendering Refinements

The current `ContiguousBoundary.Compute()` returns `CellEdge` flags per cell. This section describes visual enhancements for boundary rendering.

### Glow Effects
- Outer glow: A 2px soft bloom outside the boundary edge, using the highlight color at 30% alpha.
- Renders beneath the solid boundary line, creating a "neon sign" effect.
- Implementation: Draw the boundary line twice — first pass at 7px width with low alpha (the glow), second pass at 3px width with full alpha (the crisp edge).

### Drop Shadows
- A 1px dark shadow offset (1px right, 1px down) beneath each boundary segment.
- Color: black at 25% alpha.
- Adds depth and makes boundaries readable against both light and dark cell backgrounds.
- Only applied to `Constraint` and `Legend` layers (not `Interaction`, which uses the pulsing glow instead).

### Animated Shimmer
- For `Legend` layer boundaries: a subtle shimmer that travels along the boundary path.
- Implementation: A bright spot (white at 40% alpha, 4px wide) that moves along the boundary edges at ~60px/sec.
- Creates a "magical containment" feel when hovering over an aspect in the legend.
- The shimmer highlight wraps around corners where boundary edges meet at cell corners.

---

## 5. Quiet Mode

Quiet mode reduces highlight intensity for experienced players who no longer need strong visual cues.

### Concept
After a player demonstrates proficiency (e.g., N successful placements without triggering reactions, or an explicit toggle in settings), the highlight system dials back its visual intensity.

### Visual Changes in Quiet Mode
| Element | Normal | Quiet |
|---------|--------|-------|
| Constraint crosshatch | Full opacity, bold lines | 40% opacity, thinner lines |
| Interaction pulse | Alpha 0.15–0.45 | Alpha 0.08–0.20 |
| Boundary edge width | 3px | 2px |
| Eagle sense dash speed | 30px/sec | 15px/sec (calmer) |
| Glow bloom | 2px at 30% | 1px at 15% |

### Earning Quiet Mode
- **Automatic**: Unlocks after 10 consecutive safe placements in a single puzzle session. Resets if a reaction is triggered.
- **Manual**: Toggle in settings menu at any time. Persisted in save data.
- **Per-session**: Automatic quiet mode resets each puzzle. Manual toggle persists.

### Design Rationale
New players need strong visual feedback to learn which placements are dangerous. Experienced players find constant flashing distracting. Quiet mode respects both audiences without requiring separate UI modes.

---

## 6. Sprite Editor UI Concept

A development/debug tool for tweaking highlight parameters without recompiling.

### Purpose
Allow rapid iteration on highlight colors, widths, alpha values, and pulse speeds during development. Could ship as an accessibility/customization panel in the final game.

### Layout
A Godot `@tool` scene with:
- **Left panel**: Live preview of a sample grid with placed reagents and active highlights.
- **Right panel**: Sliders and color pickers for each tunable parameter.

### Tunable Parameters
| Parameter | Type | Range |
|-----------|------|-------|
| Highlight edge width | int | 1–6 px |
| Pulse period | float | 0.5–4.0 sec |
| Pulse alpha min | float | 0.0–0.5 |
| Pulse alpha max | float | 0.1–1.0 |
| Glow bloom width | int | 0–4 px |
| Glow bloom alpha | float | 0.0–0.5 |
| Crosshatch spacing | int | 2–8 px |
| Crosshatch line width | int | 1–3 px |
| Dash length | int | 2–12 px |
| Dash gap | int | 2–8 px |
| Dash animation speed | float | 0–100 px/sec |
| Per-aspect color | Color | full color picker |
| Dim overlay alpha | float | 0.0–1.0 |

### Implementation Approach
- Store all tunable values in a `HighlightTheme` resource (`.tres` file).
- `GridConstants` reads from the active theme resource instead of hardcoded values.
- The sprite editor scene loads/saves theme resources directly.
- Ship a "Default" theme and a "Quiet" theme. Players could create custom themes.

---

## 7. Information Density Guidelines

The UI reveals information progressively. Each level adds more visual detail to the grid.

### Level 0: Clean Grid (Default View)
- Bag cells filled with bag-type textures.
- Reagent cells filled with reagent sprites (or placeholder colors).
- Bag borders rendered as solid lines.
- No aspect icons, no highlights, no overlays.
- **When**: Default state with no selection or hover.

### Level 1: Aspect Icons (Always-On After Discovery)
- Small aspect icon badges appear in the corner of each reagent cell.
- Icons use the 8x8 1-bit silhouette tinted with aspect color.
- Multiple aspects on one cell: stack badges vertically (max 3 visible, "+N" indicator if more).
- **When**: After the player discovers each aspect through the tutorial. Persists permanently.

### Level 2: Edge Highlights (On Hover / Selection)
- When hovering a reagent in the tray or dragging over the grid, highlight edges appear.
- Constraint violations: crosshatched cells with contiguous boundaries.
- Interaction zones: pulsing glow on affected neighbor cells.
- Ghost preview: semi-transparent reagent shown at the cursor position.
- **When**: Player is actively considering a placement.

### Level 3: Full Interaction Map (Eagle Sense)
- All interactions between all placed reagents are highlighted simultaneously.
- Deep overlay dims unaffected cells (`DimOverlayColor`).
- Animated dashed borders on all interaction zones.
- Status bar shows cumulative thresholds with projected values.
- **When**: Player activates eagle sense (toggle button or hotkey).

### Transitions
- Level 0 ↔ 1: Instant (icons appear/disappear as aspects are discovered).
- Level 1 ↔ 2: Fade in over 150ms when hovering begins, fade out over 200ms when hover ends.
- Level 2 ↔ 3: Crossfade over 300ms. The dim overlay fades in while interaction highlights transition from solid to dashed.

---

## 8. Eagle Sense Visual Treatment

Eagle sense is the "show me everything" analysis mode. It overlays all active interactions on the grid simultaneously.

### Background Treatment
- **Deep blue-black overlay**: `Color(0.02, 0.03, 0.08, 0.75)` over all cells that are NOT part of an interaction.
- The overlay creates a "night vision" or "x-ray" aesthetic — the player sees through the surface to the underlying chemistry.
- Bag borders dim to 30% opacity under the overlay.

### Reagent Rendering
- Reagents in interaction zones render at full brightness (no overlay).
- Reagents NOT in any interaction zone render as **silhouettes**: desaturated to grayscale, darkened to 40% brightness. Still recognizable by shape, but visually deprioritized.
- Aspect icon badges on silhouetted reagents dim to match.

### Interaction Lines
- Between each pair of interacting cells, draw a **glowing connection line**.
- Line style: 2px solid core (aspect color), 4px outer glow (same color at 30% alpha).
- Lines connect cell centers and are drawn above the reagent sprites but below the boundary overlays.
- Where multiple interactions converge, lines may overlap. Use additive blending so convergence zones glow brighter (communicating "this area has lots of chemistry happening").

### Animated Dashed Borders
- All interaction zone boundaries use the marching-ants dashed style (see Section 2).
- Different aspects animate at slightly different speeds (Hot: 35px/sec, Cold: 25px/sec, Volatile: 45px/sec) to create visual variety without confusion.

### Status Bar Integration
- While eagle sense is active, the status bar expands to show a breakdown of each cumulative threshold.
- Each threshold shows: current value, projected value if current arrangement is kept, and distance to danger.
- Color-coded progress bars match aspect colors.

---

## 9. Layout Balance Checklist

Proportions and positioning guidelines for the puzzle screen at different viewport sizes.

### Target Layout (1280x720 reference)
```
+---------------------------+----------+
|                           |  Reagent |
|      Meta-Grid            |   Tray   |
|    (bags + reagents)      |          |
|                           |  Legend   |
+---------------------------+----------+
|        Status Bar         | Controls |
+---------------------------+----------+
```

### Proportional Targets
| Element | Width | Height | Notes |
|---------|-------|--------|-------|
| Meta-grid area | 65–70% | 75–80% | Primary gameplay area. Never smaller than 60%. |
| Reagent tray | 25–30% | 50–60% | Scrollable if reagents exceed visible space. |
| Aspect legend | 25–30% | 20–30% | Below tray. Collapsible on small viewports. |
| Status bar | 65–70% | 15–20% | Bottom of grid area. Fixed height (48–64px). |
| Controls | 25–30% | 15–20% | Eagle sense toggle, undo, settings. |

### Responsive Behavior
- **1920x1080+**: Comfortable layout. Extra space goes to grid area (larger cells or more padding).
- **1280x720**: Reference layout. Everything fits with minimal padding.
- **1024x768**: Tray and legend narrow to 20%. Status bar text shrinks. Legend may collapse to icon-only mode.
- **Mobile / <800px wide**: Stack layout — tray moves below the grid. Legend becomes a horizontal strip. Status bar overlays the bottom of the grid with transparency.

### Portrait Panel (Future)
When the character portrait panel is added (post-MVP):
- Portrait occupies a fixed 128x128 area in the top-left or top-right corner.
- On viewports <1280px wide, the portrait hides behind a toggle button.
- Portrait never competes with the grid for space — it overlaps decorative margins only.

### Alignment Rules
- Grid cells always render at integer pixel sizes (32px, 64px). No fractional scaling.
- Bag borders align to the pixel grid. Sub-pixel rendering is not used for game elements.
- UI text uses the pixel font at 1x or 2x scale — never fractional.

---

## 10. Color Palette Reference

Canonical color values for aspects, severity levels, and UI chrome. These correspond to the values in `GridConstants.cs` and serve as the authoritative reference.

### Aspect Colors
| Aspect | RGB (float) | Hex | Visual |
|--------|------------|-----|--------|
| Hot | (0.90, 0.25, 0.15) | #E64026 | Deep red-orange |
| Cold | (0.20, 0.45, 0.90) | #3373E6 | Royal blue |
| Volatile | (0.95, 0.60, 0.15) | #F29926 | Bright orange |
| Toxic | (0.20, 0.75, 0.25) | #33BF40 | Vivid green |
| Fragile | (0.70, 0.70, 0.90) | #B3B3E6 | Pale lavender |
| Heavy | (0.50, 0.40, 0.30) | #80664D | Earthy brown |
| Sharp | (0.80, 0.80, 0.80) | #CCCCCC | Steel gray |
| Medicinal | (0.90, 0.50, 0.70) | #E680B3 | Soft pink |
| Stable | (0.60, 0.60, 0.60) | #999999 | Neutral gray |
| Catalytic | (0.90, 0.85, 0.20) | #E6D933 | Gold-yellow |

### Severity Colors
| Severity | Usage | RGB | Hex |
|----------|-------|-----|-----|
| Safe | Valid placement | (0.20, 0.80, 0.20, 0.45) | #33CC33 @ 45% |
| Warning | Approaching threshold | (0.95, 0.75, 0.10, 0.45) | #F2BF1A @ 45% |
| Danger | Would trigger reaction | (0.80, 0.20, 0.20, 0.45) | #CC3333 @ 45% |
| Info | Neutral highlight (legend) | Aspect color @ 25% alpha | — |

### UI Chrome Colors
| Element | RGB | Hex | Notes |
|---------|-----|-----|-------|
| Empty cell | (0.15, 0.15, 0.20) | #262633 | Darkest — void space |
| Bag cell (empty) | (0.35, 0.35, 0.40) | #595966 | Mid-gray — available space |
| Grid lines | (0.25, 0.25, 0.30) | #40404D | Subtle dividers |
| Bag border | (0.60, 0.60, 0.65) | #9999A6 | Visible but not dominant |
| Dim overlay | (0.00, 0.00, 0.00, 0.60) | #000000 @ 60% | Eagle sense background |
| Eagle overlay | (0.02, 0.03, 0.08, 0.75) | #050814 @ 75% | Deep blue-black |

### Bag Tint Palette
Bags cycle through these tints to remain visually distinct:
1. Purple-gray: (0.38, 0.36, 0.42)
2. Green-gray: (0.36, 0.40, 0.38)
3. Warm gray: (0.40, 0.37, 0.35)
4. Blue-gray: (0.35, 0.38, 0.42)
5. Mauve: (0.40, 0.35, 0.40)

### Color Design Principles
- **Warm bias**: The overall palette skews warm (browns, golds, reds) to maintain the cozy aesthetic.
- **Danger reads hot**: Red/orange = danger. Blue/green = neutral or beneficial. This aligns with common game UI conventions.
- **Aspect colors are saturated**: High saturation makes small icon badges readable against the muted bag/cell backgrounds.
- **UI chrome is desaturated**: Backgrounds and borders use near-neutral grays so that colored highlights pop.
- **Alpha layering**: Highlights use translucent overlays so reagent art is always visible beneath them.
