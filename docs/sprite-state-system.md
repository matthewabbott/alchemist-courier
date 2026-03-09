# Sprite State System

This document specifies the architecture for managing visual object states across the game. The system handles the "same object in different visual states" problem — a glass reagent that looks different when heated, cooled, stressed, or breaking.

See also: `docs/visual-polish-design.md` (highlight rendering, colors), `docs/art-and-style.md` (art pipeline), `docs/core-mechanics.md` (reagent/bag definitions).

---

## Core Concept: Object × State

Every visual object in the game has two identity dimensions:

1. **Object identity**: What it IS (e.g., "glass tetromino-T", "firevine domino", "player avatar")
2. **Visual state**: What condition it's IN (e.g., Normal, Heated, Cooled, ThermalStress, Crushed)

The sprite rendered depends on both dimensions. The game logic determines state; the rendering system looks up the correct sprite region.

### Examples

| Object | State | Visual |
|--------|-------|--------|
| Glass tetromino-T | Normal | Clear glass sprite |
| Glass tetromino-T | Heated | Glass with orange heat cracks (safe — just warm) |
| Glass tetromino-T | Cooled | Glass with icy blue frost patterns (safe — just cold) |
| Glass tetromino-T | ThermalStress | Red-blue stress fractures (DANGER — will break) |
| Glass tetromino-T | Crushed | Deformed, weight marks from above (DANGER — Heavy above) |
| Burstbloom monomino | Normal | Alchemical bloom sprite |
| Burstbloom monomino | Heated | Glowing, about to pop |
| Player avatar | Normal | Standing idle |
| Player avatar | Encumbered | Hunched, strained |
| Player avatar | Hot | Sweating, flushed |

---

## Architecture

### Core Layer (`src/Core/Visual/` — no Godot dependencies)

#### VisualState Enum

```
Normal         — default appearance
Heated         — adjacent to Hot aspect(s), no Cold
Cooled         — adjacent to Cold aspect(s), no Hot
ThermalStress  — adjacent to BOTH Hot and Cold (Fragile-specific danger)
Crushed        — Heavy aspect directly above (Fragile-specific danger)
Exploding      — volatile reaction triggered (animated)
Breaking       — fragile breakage triggered (animated)
```

Extensible: additional states can be added for future object types (e.g., `Encumbered`, `Injured` for player avatar).

#### VisualStateResolver

A static class that determines the `VisualState` for each cell of a placed reagent:

**Input:** A placed `Reagent` + the `MetaGrid` it's on (for adjacency queries)

**Output:** `VisualState` per cell (indexed by the reagent's local cell positions)

**Resolution logic:**
1. For each cell of the reagent, check cardinal neighbors on the meta-grid
2. Collect adjacent aspect types from neighboring cells (other reagents)
3. Apply state rules:
   - Has adjacent Hot AND adjacent Cold → `ThermalStress` (only for Fragile reagents)
   - Has adjacent Hot (no Cold) → `Heated`
   - Has adjacent Cold (no Hot) → `Cooled`
   - Has Heavy cell directly above → `Crushed` (only for Fragile reagents)
   - Otherwise → `Normal`

**Important:** VisualStateResolver reflects the current state for UI feedback. It does NOT trigger reactions — that's `ChemistryEngine`'s job. States like `Heated` are informational ("this glass is warm") not necessarily dangerous on their own.

**Pure C#, unit-testable.** No Godot imports. Uses `MetaGrid.GetAdjacentCells()` and aspect queries.

#### SpriteSheetSpec

A data class defining the expected layout of a sprite sheet:

```
SpriteSheetSpec:
  - StateRowMap: Dictionary<VisualState, int>   // state → row index
  - FrameCounts: Dictionary<VisualState, int>   // state → number of animation frames
  - CellWidth: int                              // pixels per cell column (32 for monomino, 64 for domino, etc.)
  - CellHeight: int                             // pixels per cell row
  - TotalRows: int                              // total rows in the sheet
  - MaxFrames: int                              // widest row (most frames)
```

**Default reagent spec:**

| Row | State | Frames |
|-----|-------|--------|
| 0 | Normal | 1 |
| 1 | Heated | 1 |
| 2 | Cooled | 1 |
| 3 | ThermalStress | 1 |
| 4 | Crushed | 1 |
| 5 | Breaking | 4 |
| 6 | Exploding | 4 |

7 rows × up to 4 columns. For a monomino (32×32 cell), the full sheet is 128×224 pixels.

### Game Layer (`src/Game/Visual/` — Godot-specific)

#### ReagentSpriteSheet

Wraps a `Texture2D` + `SpriteSheetSpec`:
- Loads the sprite sheet PNG via `GD.Load<Texture2D>()`
- Provides `GetRegion(VisualState state, int frame) → Rect2` for atlas lookup
- One instance per reagent species+shape combo

#### SpriteStateRenderer

A Godot component that replaces `ColorRect` rendering for a cell:
- Input: `ReagentSpriteSheet` + current `VisualState`
- Uses `TextureRect` or `Sprite2D` with `AtlasTexture` to render the correct region
- For animated states: advances frame index over time, emits `AnimationFinished` signal
- For static states: shows frame 0 of the state's row

#### Integration with MetaGridView

`MetaGridView` gains a rendering mode:
- **Legacy mode:** Current `ColorRect` rendering (fallback)
- **Sprite mode:** Each cell that has a sprite sheet uses `SpriteStateRenderer`
- **Auto-detection:** If a sprite sheet exists for the reagent's catalog ID + shape, use sprite mode for those cells; fall back to ColorRect for others
- This allows incremental migration — some reagents get sprites before others

---

## Sprite Sheet Format

### File Layout

Each sprite sheet is a single PNG:

```
art/sprites/reagents/{species}_{shape}.png
```

Examples:
- `art/sprites/reagents/glass_monomino.png` (32×224: 1 cell wide, 7 state rows × 32px)
- `art/sprites/reagents/glass_t.png` (384×224: T-shape bounding box = 96×64, × 4 frames wide, × 7 rows)
- `art/sprites/reagents/firevine_domino.png` (256×224: 64×32 cell, × 4 frames, × 7 rows)

### Multi-Cell Reagents

For shapes larger than a monomino, each "frame" in the sheet covers the shape's bounding box:
- Domino (1×2): each frame is 64×32
- Triomino I-bar (1×3): each frame is 96×32
- Triomino L-bend (2×2): each frame is 64×64
- Tetromino T (2×3): each frame is 96×64
- Tetromino O (2×2): each frame is 64×64
- etc.

Void cells in the bounding box are transparent in the sprite.

### Spec File

A companion JSON (or a single master JSON for all reagents):

```json
{
  "species": "glass",
  "shape": "t",
  "cellSize": [96, 64],
  "states": {
    "Normal": { "row": 0, "frames": 1 },
    "Heated": { "row": 1, "frames": 1 },
    "Cooled": { "row": 2, "frames": 1 },
    "ThermalStress": { "row": 3, "frames": 1 },
    "Crushed": { "row": 4, "frames": 1 },
    "Breaking": { "row": 5, "frames": 4 },
    "Exploding": { "row": 6, "frames": 4 }
  }
}
```

Or a single `art/sprites/reagents/sprite_specs.json` containing all entries, keyed by `"{species}_{shape}"`.

---

## Dev Sprite Sheets

Before real art exists, dev sprite sheets serve as placeholders:

### Visual Treatment

Each cell in a dev sheet is a 32×32 colored block:
- **Fill:** Aspect color from `GridConstants.GetAspectColor()` (e.g., Hot = #E64026)
- **Border:** 1px dark outline
- **Icon:** 8×8 aspect icon silhouette centered (from `visual-polish-design.md` icon designs)

Each state row gets a distinct visual treatment:
- **Normal:** Clean colored rectangle + aspect icon
- **Heated:** Base color + orange corner quadrant overlay + small flame glyph
- **Cooled:** Base color + blue corner quadrant overlay + small snowflake glyph
- **ThermalStress:** Base color + red-blue diagonal split + drawn crack lines
- **Crushed:** Darkened base + downward arrow glyph + crack lines from top edge
- **Breaking (4 frames):** Progressive crack lines → fragment separation → scatter → fade to transparent
- **Exploding (4 frames):** Flash to white → orange/red burst → particle scatter → fade to transparent

### Generation Tool

`src/Game/Tools/DevSpriteGenerator.cs`:
1. Reads `ReagentCatalog` for all species + shape variants
2. For each, generates a PNG sprite sheet using Godot's `Image` API
3. Draws colored rectangles with overlays per the state treatment above
4. Saves to `art/sprites/reagents/` with correct naming
5. Also generates the `sprite_specs.json` master spec file

Run as a Godot tool scene during development. Output committed to repo.

---

## Extensibility

The Object × State system extends to any visual entity:

### Player Avatar (Future)
- Object: "player"
- States: Normal, Hot, Cold, Encumbered, Injured, Poisoned
- Sprite sheet: `art/sprites/character/player.png` with body state rows
- State resolution: checks cumulative thresholds, carry weight, active status effects

### Bag Cells (Future)
- Object: "bag_leather", "bag_null_glass", etc.
- States: Normal, Damaged, Destroyed
- Sprite sheet: `art/sprites/bags/{type}.png` with 32×32 tileable cell textures per state

### Design Intent

The system separates concerns:
- **Core layer** answers "what state should this be in?" (pure logic, testable)
- **Game layer** answers "what does that state look like?" (Godot rendering)
- **Art pipeline** answers "draw each state row" (swappable PNG files)

This means:
- Art can be iterated without code changes (swap the PNG, keep the spec)
- New states require: add to enum + add resolution logic + add spec row + draw art
- New object types require: add resolution logic variant + create spec + create sheet

---

## Relation to Existing Systems

### Highlight System
The highlight system (`src/Core/Highlight/`) shows interaction ZONES — which cells are dangerous. The sprite state system shows per-reagent CONDITION — what's happening to this specific object. They're complementary:
- Highlight: "this area has a Hot+Cold interaction" (zone overlay)
- Sprite state: "this glass is thermally stressed" (object appearance)

### Chemistry Engine
`ChemistryEngine.Evaluate()` determines if reactions TRIGGER. `VisualStateResolver` determines the current visual CONDITION. A Fragile reagent can be `Heated` (visually warm, no danger) without any reaction — it only becomes a reaction when both Hot AND Cold are adjacent.

### GridConstants Colors
`GridConstants.GetAspectColor()` provides the canonical color per aspect. Dev sprite sheets use these colors. The sprite state system doesn't replace the color mapping — it adds state-driven visual variation on top of it.

---

## Related Documents

- `docs/visual-polish-design.md` — Highlight rendering, icon designs, color palette
- `docs/art-and-style.md` — Art pipeline, RetroDiffusion workflow, asset organization
- `docs/art-workflow.md` — Sprite generation process, quality checklist
- `docs/core-mechanics.md` — Reagent catalog, bag types, aspect definitions
- `docs/chemistry-system.md` — Aspect interactions, reaction rules
