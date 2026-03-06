# Code Review — 2026-03-05 (j)
**Commits reviewed:** dafa8ea..e29edbc (E.1–E.3)

## Findings

### Dead Code — Unused parameter
- **src/Core/Puzzle/PuzzleSearchSolver.cs:124,183** — `reagents` parameter in `Backtrack()` and `CountBacktrack()` is passed through every recursive call but never read. Only the `ordered` list (which already contains the reagents) is consumed.
  Before: `private static bool Backtrack(MetaGrid grid, List<Reagent> reagents, List<ReactionRule> rules, ...)`
  After: `private static bool Backtrack(MetaGrid grid, List<ReactionRule> rules, ...)`
  Fixed: removed parameter from both methods and all 4 call sites.

### Dead Code — Unused import
- **src/Game/Editor/ShapeEditorWidget.cs:3** — `using AlchemistCourier.Game.Inventory;` is imported but no types from that namespace are used in the file.
  Fixed: removed the import.

### DRY — Locked-reagent pre-placement (noted, not actioned)
- **src/Core/Puzzle/PuzzleSearchSolver.cs:28-56 vs 89-115** — `Solve()` and `CountSolutions()` duplicate ~30 lines of locked-reagent pre-placement and MRV ordering logic. Only 2 occurrences — Rule of Three not met, so no extraction. If a third method is added, extract to a private `PrepareSolve()` helper.

### Observation — PuzzleFile.cs
- Clean, well-structured code. Follows SaveManager's exact serialization pattern. No issues found.

### Observation — Test files
- PuzzleSearchSolverTests: Good coverage (12 tests), good use of helper builders. `Solve_Timeout_ReturnsNull` duplicates `Solve_CancelledToken_ReturnsNull` in mechanism but serves as documentation — acceptable in test code (DAMP).
- PuzzleFileTests: Thorough round-trip testing with proper temp directory cleanup via IDisposable. No issues.

## Actions Taken
- Fixed: Removed dead `reagents` parameter from `Backtrack()` and `CountBacktrack()` (4 call sites updated)
- Fixed: Removed unused `using AlchemistCourier.Game.Inventory` import from ShapeEditorWidget.cs

## Desloppify
- Before: N/A (scan blocked by permission system)
- After: N/A
- Top issue: N/A

## Status
ISSUES_FIXED
