# Worklog — 2026-03-07 Ralph Loop

## CR.1 — Refactor ReagentCatalog to item-first model

### What changed
Rewrote `ReagentCatalog` from 72 combinatorial entries (8 species × 9 shapes) to 24 unique named items. Each item now has its own identity: a single natural polyomino shape, one aspect, and unique flavor text. This follows the item-first design in `docs/item-catalog-design.md`.

### Key decisions
- **Removed `Get(speciesId, shapeName)`** — consumers now use `GetById(id)` with item-specific IDs like `"firevine"`, `"thunder_egg"`, `"razor_fern"`.
- **Added `GetShapeName(ReagentDefinition)`** — reverse lookup from a definition's shape matrix to its canonical shape name.
- **Rarity derived from cell count** — 1-2 cells = Common, 3 = Uncommon, 4 = Rare. No explicit rarity in the items table.

### Shape coverage
| Shape    | Items | Examples                        |
|----------|-------|---------------------------------|
| monomino | 8     | ember_seed, frozen_teardrop ... |
| domino   | 7     | firevine, snowflower ...        |
| i_tri    | 2     | frost_fern, ironbark_root       |
| l_tri    | 2     | magma_slug, burstbloom          |
| i_tet    | 1     | gossamer_thread                 |
| o_tet    | 1     | thunder_egg                     |
| t_tet    | 1     | blight_moss                     |
| s_tet    | 1     | razor_fern                      |
| l_tet    | 1     | anvil_shard                     |

### Bug discovered and fixed
The catalog reduction exposed a hidden coupling in `PuzzleGenerator`. When `TryFindCatalogMatch` found a match in the old 72-entry catalog, the generator used the catalog's `ReagentDefinition` directly — including its `AspectsPerCell` where ALL cells had aspects. But the generator's own aspect assignment gives ~30% of cells empty aspects. With fewer catalog matches (24 vs 72), more reagents kept partial aspects, changing chemical behavior and causing 5 `PuzzleGeneratorTests` failures at seed=22/Hard.

**Fix:** Modified `PuzzleGenerator` to use catalog for naming only (id, name, flavorText), always using the generator's own shape and aspects. This decouples generation behavior from catalog completeness.

### Files modified
- `src/Core/Reagents/ReagentCatalog.cs` — complete rewrite
- `src/Core/Puzzle/PuzzleGenerator.cs` — catalog usage fix (lines 176-191)
- `src/Game/Inventory/DemoPuzzle.cs` — `GetById` instead of `Get`
- `tests/AlchemistCourier.Core.Tests/Reagents/ReagentCatalogTests.cs` — complete rewrite

### Test results
All 655 tests pass.

## CR.2 — Update all unit tests for new catalog structure

No-op. All tests were already updated as part of CR.1. Verified: `ReagentCatalogTests` asserts 24 items, 3 per aspect, all 24 individual IDs. Searched codebase for old combinatorial IDs (e.g., `firevine_domino`) — zero references remain.

## CR.3 — Update PuzzleGenerator, tutorial, and save/load for new catalog

No-op. All consumers are catalog-agnostic by design:
- `PuzzleGenerator` uses `TryFindCatalogMatch` for naming only, no hardcoded IDs
- `TutorialData` uses `DifficultyParams` with fixed seeds, no reagent IDs
- `SaveData` stores lesson numbers and date seeds only
- `DemoPuzzle` uses `GetById("snowflower")` and `GetById("nullflower")`, both in new catalog

## CR.4 — Regenerate dev sprites for 24-item catalog

### What changed
Deleted 72 old sprite PNGs + 72 `.import` files from `art/sprites/reagents/`. Ran `DevSpriteGenerator` via Godot MCP to produce 24 new sprite sheets matching the new catalog item IDs.

### Files modified
- `art/sprites/reagents/` — removed 144 old files, added 24 new PNGs + `sprite_specs.json`

### Test results
All 655 tests pass. Build: 0 errors. No Godot imports in Core.

## CR.5 — Integration test checkpoint

Full integration test run verifying the game works end-to-end after the catalog refactor.

### Results
- **Build:** 0 errors
- **Unit tests:** 655/655 pass
- **Integration harness:** 31/31 pass (`[HARNESS_DONE] passed=31 failed=0`)
- **Screenshots:** Visually verified — grid, bags, reagents, aspect colors, status bars, puzzle generation, tutorials, hints, editor, undo/redo all rendering correctly

### Conclusion
No regressions from the 72→24 item catalog refactor. All systems working correctly with new catalog.

## HF.1 — HoverFrameResolver + frame texture generation

### What changed
Two-part task:

**Part 1 — Core resolver** (`src/Core/Highlight/HoverFrameResolver.cs`):
- `GetFrameKey(Reagent)` → `"{aspect}_{shape}_r{rotation}"` for texture lookup
- `GetPerimeterEdges(Reagent)` → boundary edges per cell, internal edges excluded
- `GetPerimeterEdges(bool[,])` → shape-only overload for texture generation

**Part 2 — Frame texture generation** (`DevSpriteGenerator.cs`):
- Extended to generate 72 frame PNGs (8 aspects × 9 shapes) at `art/sprites/frames/dev/`
- Each frame: 4px aspect-colored outline on perimeter edges, 1px white inner edge, transparent interior
- Rotation 0 only — rotation handled at runtime

### Files created
- `src/Core/Highlight/HoverFrameResolver.cs` — pure C# resolver
- `tests/AlchemistCourier.Core.Tests/Highlight/HoverFrameResolverTests.cs` — 12 tests
- `art/sprites/frames/dev/` — 72 frame PNGs

### Files modified
- `src/Game/Tools/DevSpriteGenerator.cs` — added `GenerateFrameTextures()` + `DrawFrameEdges()`

### Test results
All 667 tests pass (655 existing + 12 new).

## HF.2 — HoverFrameOverlay rendering with pulse animation

Created `src/Game/Inventory/HoverFrameOverlay.cs` — the Godot-side renderer for the hover frame system. It loads pre-generated frame textures (from HF.1) as TextureRects and applies a sinusoidal alpha pulse via the node's `Modulate` property. Three interaction modes provide distinct visual feedback: hover pulses gently (0.25–0.55 alpha), selection pulses slightly brighter (0.35–0.65), and drag holds steady (0.50). Falls back to procedural ColorRect edge rendering when no texture is found. Added 7 frame-related constants to `GridConstants.cs`. Wiring into MetaGridView comes in HF.3.
Commit: fcfb59a

## Code Review — HF.1 + HF.2

Reviewed commits 966e9f0..bb6c93b (HoverFrameResolver, HoverFrameOverlay, DevSpriteGenerator additions). Found one P2 DRY violation: `GetPrimaryAspectId` was duplicated identically across 3 files (Rule of Three triggered). Extracted to `ReagentDefinition.PrimaryAspectId` property, removing 28 lines of duplication. Also noted frame key rotation mismatch (resolver includes `_r{rotation}` but textures don't) as P3 design note. Desloppify: 100/100 maintained. All 667 tests pass.

## HF.3 — Wire HoverFrameOverlay into MetaGridView + integration test

Wired the hover frame overlay into the game's grid view. When a player hovers over a placed reagent, they now see a pulsing aspect-colored perimeter frame around it — the edge-only rendering style avoids obscuring the reagent art while clearly indicating which item is under the cursor. The frame hides automatically when moving away or selecting a new reagent from the tray. Also fixed `SetHoverPosition` to trigger hover detection (it was only updating the ghost preview, not tracking which placed reagent was under the cursor). Integration test confirms the full show/hide cycle.
Commit: 1226c6f
