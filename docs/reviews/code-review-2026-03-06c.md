# Code Review — 2026-03-06 (C)
**Commits reviewed:** cf5896c..882a33b (5 commits on dev, 3 substantive)

## Scope
- `src/Core/Puzzle/ActionHistory.cs` — Undo/redo stack manager (84 lines)
- `src/Core/Puzzle/IGridAction.cs` — Reversible action interface (17 lines)
- `src/Core/Puzzle/PlaceReagentAction.cs` — Place command (43 lines)
- `src/Core/Puzzle/RemoveReagentAction.cs` — Remove command (32 lines)
- `src/Core/Reagents/ReagentCatalog.cs` — 72 canonical single-aspect definitions (192 lines)
- `tests/.../ActionHistoryTests.cs` — 16 tests (351 lines)
- `tests/.../ReagentCatalogTests.cs` — 20 tests (233 lines)

## Findings

### P3 — Least Surprise (silent fallback)
- **`RemoveReagentAction.cs:18`** — `PreviousPosition = reagent.PlacedPosition ?? new Vector2I(0, 0)` silently defaults to (0,0) for an unplaced reagent. Since Execute would fail for an unplaced reagent (MetaGrid.RemoveReagent checks IsPlaced), this fallback never causes real issues, but a reader might wonder why (0,0) was chosen.

### P4 — Style (double dictionary lookup)
- **`ReagentCatalog.cs:111-117`** — `ContainsKey` + indexer pattern instead of `TryGetValue`. Only runs in the static constructor so performance is irrelevant.

### P4 — Style (unnecessary allocation on miss)
- **`ReagentCatalog.cs:141,147`** — `GetByAspect` and `GetByShape` allocate `new List<ReagentDefinition>()` on cache miss. Not a hot path.

### P4 — Build warning (xUnit2013)
- **`ReagentCatalogTests.cs:157`** — `Assert.Equal(1, aspectSet.Aspects.Count)` triggers xUnit2013.

## Positive Observations
- **Core purity maintained**: All new files are in `src/Core/` with zero Godot dependencies.
- **Command pattern well-applied**: IGridAction with two implementations is appropriate (not speculative).
- **Rotation tracking is correct**: PlaceReagentAction captures/restores previous rotation; RemoveReagentAction snapshots position + rotation.
- **Test coverage is thorough**: 36 new tests covering happy paths, failures, edge cases, and round-trips.
- **ReagentCatalog is clean data**: 72 definitions with O(1) lookups by ID/aspect/shape.

## Actions Taken
- No direct fixes committed (all findings P3-P4, note-only).
- No tasks added to prd.json.

## Desloppify
- Before: N/A (tool not accessible in current shell session)
- After: N/A
- Top issue: xUnit2013 build warning in ReagentCatalogTests.cs:157

## Status
CLEAN
