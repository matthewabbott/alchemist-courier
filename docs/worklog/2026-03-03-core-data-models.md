# Session: Core Data Models Implementation
**Date:** 2026-03-03
**Branch:** `feature/core-data-models`

## Summary

Implemented the foundational data model layer for the inventory tetris system. This covers the four core systems defined in `docs/architecture.md`: Grid, Bags, Chemistry, and Reagents. All code lives in `src/Core/` with zero Godot dependencies, establishing the pure C# backend that the game's UI and Godot integration will build on top of.

## Changes

### New files
- `src/Core/Grid/Vector2I.cs` — Godot-independent 2D integer vector with arithmetic and cardinal helpers
- `src/Core/Grid/GridCell.cs` — Single cell: position, bag ownership, occupancy, damage/lock state
- `src/Core/Grid/MetaGrid.cs` — Master grid (~300 lines): bag/reagent placement validation, adjacency, spanning
- `src/Core/Bags/BagPropertyType.cs` — Enum for 8 bag properties (Null, Locked, Gravity, etc.)
- `src/Core/Bags/BagDefinition.cs` — Immutable bag template with shape + 5 starter bags
- `src/Core/Bags/Bag.cs` — Runtime bag instance with placement tracking
- `src/Core/Chemistry/SchoolType.cs` — Enum for 4 alchemy schools (Physical is MVP scope)
- `src/Core/Chemistry/Aspect.cs` — Alchemical property with magnitude + 10 Physical factory methods
- `src/Core/Chemistry/AspectSet.cs` — Aspect collection with query/mutation methods
- `src/Core/Reagents/Rarity.cs` — 5-tier rarity enum
- `src/Core/Reagents/ShapeUtil.cs` — Rotation utility for bool[,] shape matrices
- `src/Core/Reagents/ReagentDefinition.cs` — Immutable reagent template with per-cell aspects + 6 starters
- `src/Core/Reagents/Reagent.cs` — Runtime reagent (~186 lines) with rotation-aware aspect mapping
- `scenes/main.tscn` — Minimal root Node scene
- Directory scaffolding (.gitkeep) for art/, audio/, resources/, and future src/ subdirs

### Modified files
- `project.godot` — Added `run/main_scene` pointing to `scenes/main.tscn`

## Design Decisions

- **Custom Vector2I over Godot's:** Keeps `src/Core/` Godot-free. Same API surface we need.
- **Shape convention — `bool[rows, cols]`:** Row-major matrices where `shape[row, col]` maps to `Vector2I(col, row)`. Consistent across bags and reagents.
- **Per-cell aspects indexed by row-major traversal order:** `ReagentDefinition.AspectsPerCell[i]` corresponds to the i-th true cell scanning left-to-right, top-to-bottom. Reagent handles rotation mapping via reverse-rotation lookup.
- **GridCell uses `object?` for BagOwner/OccupyingReagent:** Avoids circular namespace dependencies between Grid, Bags, and Reagents. MetaGrid casts to concrete types.
- **Static factory methods for starter definitions:** `BagDefinition.SmallPouch()`, `ReagentDefinition.SingleCell()`, etc. create new instances each call (no caching). Fine for testing; production should use a registry.
- **MetaGrid separated from GridCell/Vector2I:** MetaGrid imports Bags and Reagents namespaces, so it's committed separately to make the dependency clear.

## Commits

| Hash | Message |
|------|---------|
| `023c2a1` | Add project directory scaffolding |
| `66f90ca` | Add grid primitives: Vector2I and GridCell |
| `1538869` | Add chemistry system: SchoolType, Aspect, AspectSet |
| `858a96f` | Add bag system: BagPropertyType, BagDefinition, Bag |
| `81221c5` | Add reagent system: Rarity, ShapeUtil, ReagentDefinition, Reagent |
| `eb4911b` | Add MetaGrid: master grid for bags and reagents |
| `7c2bbc4` | Add minimal main scene and set as project entry point |
| `4ce9ce0` | Add agent workflow docs, CLAUDE.md, and session worklog |
| `9925837` | Add xUnit test project for Core with initial tests |

## Known Issues

- No `.csproj`/`.sln` in repo (gitignored; Godot generates them). Agents need to open in Godot or run `godot --headless --build-solutions --quit` before `dotnet build` works.
- Static factory methods on BagDefinition/ReagentDefinition create new instances per call — not cached.
- `ShapeUtil.Rotate` creates intermediate arrays for multi-step rotations. Optimize if profiling shows it matters.
- Code has not been compiled or tested yet — needs build verification and unit tests.
- Test project depends on Godot-generated `.csproj` existing before `dotnet test` works.

## Next Steps

- [ ] Verify build compiles (`dotnet build` after Godot generates .csproj)
- [ ] Run and verify xUnit tests pass
- [ ] Implement reaction rules and chemistry engine (`src/Core/Chemistry/`)
- [ ] Wire up basic UI to visualize grid placement
- [ ] Set up GdUnit4 for Godot integration testing (when UI exists)
