# Code Review — 2026-03-05k
**Commits reviewed:** c5a9b51..8465dbc (E.4–E.6 editor implementation)

## Findings

### Fail-Fast / Defensive Programming (P1)
- **EditorScreen.cs:97** — `LoadPuzzle` lacked exception handling for I/O errors. `PuzzleFile.Load` can throw on file permission or access errors, but only null-return was handled. `SavePuzzle` had a try-catch but `LoadPuzzle` did not.
  Before: `var puzzleFile = PuzzleFile.Load(path);`
  After: Wrapped in try-catch with status message on failure.

### DRY — Duplicated Color Constants (P2)
- **EditorScreen.cs:19-20, BagEditorPanel.cs:16-17, ReagentEditorPanel.cs:14-15** — `HeaderColor` and `LabelColor` are identically defined in all 3 editor files. The same color values also appear inline in 6+ files in `src/Game/Inventory/`. Rule of Three is met for the editor files alone.
  Suggestion: Extract shared color constants to a common location (e.g., `EditorColors` or a broader `UIColors` static class).

### Cognitive Load — `_Input` vs `_GuiInput` (P2)
- **ShapeEditorWidget.cs:107** — Uses `_Input` (global event handler) instead of `_GuiInput` (control-scoped). Every ShapeEditorWidget instance processes ALL mouse events globally, even those far outside its bounds. With multiple widgets on screen (each BagEntry and ReagentEntry has one), this is wasteful. Manual bounds-checking prevents incorrect behavior, but `_GuiInput` would be more correct and efficient.
  Note: Migration requires adding `MouseExited` signal handling for hover reset. Needs Godot runtime testing.

### Dead Code (P3)
- **ShapeEditorWidget.cs:98** — `public bool[,] GetShapeRaw()` was never called anywhere in the codebase. Removed.

### Redundant Code (P3)
- **EditorScreen.cs:35-36** — `AnchorsPreset` property set followed by `SetAnchorsPreset()` method call — these are equivalent. Removed the redundant property set.

### Duplicated StyleBoxFlat (P4, noted only)
- **BagEditorPanel.cs:162-178, ReagentEditorPanel.cs:176-192** — Identical 17-line StyleBoxFlat configurations for entry panels. Only 2 occurrences (below Rule of Three threshold). No action taken.

## Actions Taken
- **Fixed P1:** Added try-catch to `EditorScreen.LoadPuzzle` for I/O exceptions.
- **Fixed P3:** Removed redundant `AnchorsPreset` property set in `EditorScreen._Ready`.
- **Fixed P3:** Removed unused `GetShapeRaw()` from `ShapeEditorWidget`.
- Committed as `3df3f7d` on `dev`.

## Desloppify
- Before: N/A (no desloppify tool installed)
- After: N/A
- Top issue: Manual scan performed. No mechanical tool available.

## Status
ISSUES_FIXED
