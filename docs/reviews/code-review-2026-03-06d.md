# Code Review — 2026-03-06d
**Commits reviewed:** 942efa5..651e894 (VS.5: Wire undo/redo into PuzzleScreen UI)

## Summary

The ActionHistory UI wiring (VS.5) is well-executed. Core ActionHistory infrastructure (reviewed in code-review-2026-03-06c) remains clean. The UI integration handles edge cases correctly — hints clear history, reset clears history, MetaGridView's nullable ActionHistory supports both PuzzleScreen and standalone/editor usage. No P0 or P1 issues found.

## Findings

### DRY — Repeated UI refresh sequence (P2)
- **`src/Game/Inventory/PuzzleScreen.cs:416-421, 437-441, 545-550, 706-713, 739-746, 755-762`** — The same 5-line refresh sequence appears 6 times across different methods:
  ```csharp
  _reagentTray.RefreshDisplay();
  _gridView.RefreshDisplay();
  _statusBar.Refresh();
  UpdateStatusThresholds();
  RefreshEagleSense();
  UpdateUndoRedoButtons();
  ```
  The Undo/Redo methods are identical copies. OnReagentPlaced, OnReagentRemoved, and RemoveReagentAt share the same 6 calls. UseHint adds UpdateHintButton. ResetPuzzle is a variant without UpdateStatusThresholds/RefreshEagleSense.

  **Suggested fix:** Extract a private helper `RefreshAfterGridChange(bool updateHints = false)` that consolidates the common sequence. Each call site reduces to 1-2 lines.

  ```csharp
  private void RefreshAfterGridChange(bool updateHints = false)
  {
      _reagentTray.RefreshDisplay();
      _gridView.RefreshDisplay();
      _statusBar.Refresh();
      UpdateStatusThresholds();
      RefreshEagleSense();
      UpdateUndoRedoButtons();
      if (updateHints) UpdateHintButton();
  }
  ```

  **Note:** This duplication is pre-existing and was worsened by adding `UpdateUndoRedoButtons()` to 6 locations. Task R1 created.

### MetaGridView nullable ActionHistory branching (P3)
- **`src/Game/Inventory/MetaGridView.cs:611-619, 758-766`** — Two locations with `if (_actionHistory != null) { use history } else { direct grid call }`. Below Rule of Three. The nullable design is justified — MetaGridView must work without ActionHistory for editor and testing contexts. No action needed.

### Keyboard shortcut handling (P4 — positive note)
- Ctrl+Z and Ctrl+Y correctly use `_UnhandledKeyInput` (not `_Input`), ensuring they don't interfere with text input fields. `SetInputAsHandled()` is called correctly. Good.

### ActionHistory clearing on hints (P4 — positive note)
- `UseHint()` correctly clears `_actionHistory` after modifying the grid outside the command pattern. This prevents undo from reaching an inconsistent state. Well-considered edge case handling.

## Actions Taken
- No direct fixes committed (no P0-P1 issues found).
- Task R1 added to prd.json for P2 DRY violation in PuzzleScreen refresh sequences.

## Desloppify
- Before: N/A (tool not accessible in this session)
- After: N/A
- Top issue: N/A

## Status
ISSUES_FOUND
