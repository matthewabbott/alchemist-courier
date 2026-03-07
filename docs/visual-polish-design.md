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

## 10. Reaction Animations

When a reaction is triggered (on puzzle confirmation), the game plays a short animation making the reaction VISIBLE before showing the result overlay. This is critical for player comprehension — without animation, "FAILED: Explosion" is opaque. With animation, the player SEES the volatile reagent detonate.

### Animation Sequence

On `PuzzleValidator.Evaluate()` returning `HasReactions`:

1. **Queue** all `ReactionResult` objects as animation events
2. **Play** each animation (sequentially or parallel for independent reactions)
3. **Wait** for all animations to complete
4. **Show** the result overlay with the reaction summary text

### Animation Specifications

| Event | Trigger | Duration | Visual |
|-------|---------|----------|--------|
| **Volatile explosion** | `ReactionResultType.Explosion` | ~0.6s | Affected cells flash white (0.1s) → orange/red burst expands from center (0.2s) → particle scatter outward (0.2s) → fade to empty (0.1s) |
| **Fragile breakage (Sharp)** | `ReactionResultType.Breakage` | ~0.5s | Crack lines appear across affected cells (0.15s) → fragments separate outward (0.2s) → scatter/fade (0.15s) |
| **Fragile thermal stress** | `ReactionResultType.ThermalStressFracture` | ~0.5s | Red-blue stress lines pulse once (0.15s) → crack pattern (0.15s) → shatter outward (0.2s) |
| **Heavy crushing Fragile** | Placement constraint / `ReactionResultType.Crushing` | ~0.4s | Heavy reagent visually presses down (0.1s) → target Fragile cell crack lines (0.15s) → crush deformation (0.15s) |
| **Heavy falling** | Placement of Heavy reagent with empty space below | ~0.3s | Reagent slides smoothly from drop position to resting position (Tween, ease-out) |
| **Toxic cloud** | `ReactionResultType.ToxicCloud` | ~0.5s | Green haze particles emanate from toxic cells (0.3s) → spread to adjacent cells (0.2s) |

### Heavy Falling (Placement-Time Animation)

Unlike reaction animations (which play on Confirm), Heavy falling plays immediately when a Heavy reagent is placed:

1. Player drops Heavy reagent at cursor position
2. Game logic instantly calculates the final resting position (gravity settling)
3. The reagent is logically placed at the final position immediately
4. The visual representation animates from the drop position to the final position
5. Uses Godot `Tween` with `EaseType.Out` and `TransitionType.Bounce` for satisfying weight feel
6. Duration: ~0.3s

### Implementation Architecture

**`src/Game/Animation/ReactionAnimator.cs`:**
- Input: `List<ReactionResult>` from `PuzzleValidator`
- For each result, spawns temporary visual nodes on the grid at `AffectedCells` positions
- Manages animation sequencing (sequential for chained reactions, parallel for independent)
- Emits `AllAnimationsComplete` signal when done → PuzzleScreen shows result overlay
- Uses Godot `Tween` for interpolation, temporary `Node2D` children for particle effects
- Animations are rendered ON TOP of the grid (above reagent layer, below UI)

**`src/Game/Animation/FallingAnimator.cs`:**
- Input: reagent reference + drop position + final position
- Creates a visual-only tween that moves the reagent's rendered cells from drop to final
- Self-destructs after animation completes
- Does NOT affect game state (logic placement is instant)

### SFX Hooks

Each animation type emits a signal at start, enabling audio wiring:
- `ExplosionStarted` → boom/poof SFX
- `BreakageStarted` → glass crack/shatter SFX
- `CrushStarted` → heavy impact SFX
- `FallingStarted` → whoosh/thud SFX
- `ToxicCloudStarted` → hiss/sizzle SFX

Audio files are TBD (placeholder or free assets). The hooks should be wired first.

### Sprite State Integration

Reaction animations use the sprite sheet's animated state rows (see `docs/sprite-state-system.md`):
- **Breaking:** Plays frames from the "Breaking" row of the affected reagent's sprite sheet
- **Exploding:** Plays frames from the "Exploding" row
- For reagents without sprite sheets (legacy ColorRect mode), use simple procedural animations (flash + fade)

---

## 11. Undo/Redo Visual Feedback

When the player undoes or redoes a placement:
- **Undo:** The removed reagent fades out of its grid position (0.15s) and the tray item re-appears
- **Redo:** The reagent fades into its grid position (0.15s) from the tray
- Subtle, quick — not as dramatic as reaction animations
- Keyboard shortcuts: Ctrl+Z (Undo), Ctrl+Y (Redo)
- UI buttons in the controls area (bottom-right of puzzle screen)

---

## 12. Color Palette Reference

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

---

## 13. Hover Frames (Aspect-Themed Shape Overlays)

Hover frames communicate the exact polyomino shape of a reagent when the player hovers or selects it. They are NOT permanent — the reagent art speaks for itself during normal play.

### Concept

Each reagent has an associated **frame texture** that appears as a translucent overlay on hover/selection:
- **Perimeter only** — internal edges between cells of the same reagent are NOT drawn. Only the convex hull boundary of the polyomino gets the frame outline.
- **Aspect-themed** — each aspect gets a distinct frame style (e.g., frosty crystalline border for Cold, ember-vine border for Hot)
- **Two adjacent reagents** with the same aspect are visually distinct because their frames have a gap between them (perimeter-only means no shared edges)

### Visual Treatment

| State | Alpha | Behavior |
|-------|-------|----------|
| **Default** | 0% (hidden) | No frame visible. Art + aspect badges only. |
| **Hover** | Pulsing 0.25–0.55 | Frame fades in, then pulses. 1.5s period, sinusoidal. |
| **Selected** | Pulsing 0.35–0.65 | Slightly more opaque than hover. Still pulses. |
| **Dragging** | Steady 0.50 | No pulse during active drag. Clean readability. |

### Frame Texture Design

**Dev art (procedural):**
- 4px colored outline on perimeter edges only
- Interior is fully transparent
- Aspect color from `GridConstants.GetAspectColor()` with white inner edge (1px) for contrast
- Generated per shape by `DevSpriteGenerator`

**Production art (RetroDiffusion, future):**
- Aspect-themed decorative borders (Celtic knotwork for general, ice crystals for Cold, vine tendrils for Hot, etc.)
- Generated at 128x128 per shape, downsampled to 32x32 per cell
- Must maintain clear perimeter edges for shape readability

### Implementation Architecture

```
src/Core/Highlight/HoverFrameResolver.cs    — Pure C#. Given a Reagent, returns frame key
                                               (shape name + rotation + aspect ID).
src/Game/Inventory/HoverFrameOverlay.cs      — Godot node. TextureRect positioned over
                                               hovered/selected reagent. Manages fade-in,
                                               pulse animation (Tween), and fade-out.
src/Game/Tools/DevSpriteGenerator.cs         — Extended to generate frame textures
                                               (perimeter-only outlines per shape).
src/Game/Inventory/MetaGridView.cs           — Wires HoverFrameOverlay to hover/selection
                                               state changes.
src/Game/Inventory/GridConstants.cs           — Frame pulse parameters (alpha min/max,
                                               period, fade duration).
```

### Frame Texture Organization

```
art/sprites/frames/
  dev/                    # Procedural dev art frames
    hot_monomino.png
    hot_domino.png
    cold_monomino.png
    ...
  production/             # RetroDiffusion frames (future)
    hot_monomino.png
    ...
```

### Boundary Computation

Frame perimeters use the same `ContiguousBoundary.Compute()` logic as the existing highlight system:
- Input: list of cell positions occupied by the reagent
- Output: per-cell `CellEdge` flags indicating which edges are on the perimeter
- For frame textures: edges with the `CellEdge` flag get the 4px outline; edges without it are transparent

### Relationship to Other Highlights

Hover frames are a **separate rendering layer** from the highlight overlay. They do not use `HighlightLayer` — instead, they are rendered as a `TextureRect` directly in MetaGridView's scene tree, positioned between the reagent sprites and the highlight overlay:

```
Z-order:
  0: Cell backgrounds
  1: Grid lines
  2: Reagent sprites (SpriteStateRenderer)
  3: Hover frame overlay (HoverFrameOverlay)  ← NEW
  4: Ghost cells (placement preview)
  5: Highlight overlay (constraint/interaction edges)
  10: Rejection tooltip
```

This ensures the frame is visible but doesn't interfere with constraint/interaction highlights, which are more important for gameplay feedback.
