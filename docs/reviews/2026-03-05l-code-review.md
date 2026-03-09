# Code Review — 2026-03-05 (l)
**Commits reviewed:** 9a6d751..2bdeeb8 (E.7 SolverPanel, E.8 Play mode, E.9 Editor tests)

## Findings

### Fail-Fast / Defensive Programming — SolverPanel missing node cleanup (P1)
- **`src/Game/Editor/SolverPanel.cs:240-258`** — `RunAsync` launches a background `Task.Run` whose `ContinueWith` calls `CallDeferred(MethodName.ProcessPendingResult)`. If the SolverPanel is freed (via `QueueFree()`) while a solve is in progress, the continuation attempts `CallDeferred` on a freed node, causing a Godot error.
  Before: No `_ExitTree` override, no guard in continuation.
  After: Added `_ExitTree` calling `CancelSolving()`, and `IsInstanceValid(this)` guard in the continuation.

### DRY — CreateStyledButton repeated 3x (P3, noted only)
- **`EditorScreen.cs:717`**, **`SolverPanel.cs:297`**, **`PuzzleScreen.cs:348`** — Three implementations of `CreateStyledButton` with slightly different margins (16/6, 12/4, 12/4+min size). Hits Rule of Three. However, each serves a different context with intentionally different sizing. Extracting to a shared utility would require multiple parameters for marginal gain. Note only — extract if a 4th occurrence appears.

### Test Accessors — EditorScreen internal properties (P3, noted only)
- **`EditorScreen.cs:66-70`** — `PlayGrid` and `PlayReagentList` added as `internal` for test access. Pragmatic choice for integration testing. The alternative (testing purely through UI simulation) would be significantly more complex. Acceptable trade-off.

### Test Code — Boilerplate repetition (P4, noted only)
- **`TestHarness.cs:1331-1616`** — Four editor tests share identical setup/teardown pattern (hide PuzzleScreen, create EditorScreen, QueueFree, restore). This is test code — DAMP over DRY applies. Each test is self-contained and readable. No action needed.

## Actions Taken
- Fixed P1: Added `_ExitTree` override and `IsInstanceValid` guard to `SolverPanel.cs` (committed as `a2cc0ea`)
- No tasks added to prd.json (no issues warranting P0-P2 task creation)

## Desloppify
- N/A (desloppify tool not available in this environment)

## Status
ISSUES_FIXED
