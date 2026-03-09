# Code Review — 2026-03-06
**Commits reviewed:** 8c56f06..2f5167b (3 commits)

## Findings

### Fail-Fast — Silent fallback for unknown PuzzleType (P1)
- **src/Core/Puzzle/PuzzleValidator.cs:26** — Default `_` branch in switch expression silently returns `NotAllPlaced` for unrecognized `PuzzleType` values. If a new enum member is added without updating the switch, the validator would return a misleading result instead of surfacing the error.
  Before: `_ => ValidationResult.NotAllPlaced(trayReagents.Count),`
  After: `_ => throw new ArgumentOutOfRangeException(nameof(puzzleType), puzzleType, "Unhandled PuzzleType"),`

### Self-Documenting Code — Unused parameter (P3)
- **src/Core/Puzzle/ValidationResult.cs:22** — `NotAllPlaced(int unplacedCount)` accepts `unplacedCount` but discards it. PuzzleScreen independently calls `CountUnplaced()` for the status message. The parameter is misleading but not harmful.

### Encapsulation — Mutable parameter type on public API (P3)
- **src/Core/Puzzle/PuzzleValidator.cs:19** — `Evaluate()` accepts `List<ReactionRule>` instead of `IReadOnlyList<ReactionRule>`. For a Core API, preferring read-only interfaces communicates intent better.

### Overall Assessment
The PuzzleValidator extraction is well-structured: clean separation of Core logic from Godot UI, good use of factory methods on ValidationResult, and solid test coverage (7 tests covering all three PuzzleType paths including edge cases). The refactoring in PuzzleScreen.ConfirmPuzzle() is cleaner than the original — the switch statement is easier to follow than the nested if/return chain.

## Actions Taken
- Fixed P1: Replaced silent `_` fallback with `ArgumentOutOfRangeException` (commit `fef7e61`)
- P3 issues noted in report only (not enough occurrences to warrant tasks)

## Desloppify
- Before: N/A (tool requires interactive approval)
- After: N/A
- Top issue: N/A

## Status
ISSUES_FIXED
