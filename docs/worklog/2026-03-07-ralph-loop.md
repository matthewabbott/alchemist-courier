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
