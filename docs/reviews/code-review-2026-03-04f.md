# Code Review — 2026-03-04 (f)
**Commits reviewed:** 29a6911..a28eed1 (3 commits)

## Findings

### DRY — Duplicated Selection-Clearing Logic (P1, quick win)
- **PuzzleScreen.cs:268, 321, 347, 376, 433** — The same 4-line block for clearing selection state was copy-pasted in 5 locations: `OnReagentPlaced`, `CancelSelection`, `ResetPuzzle`, `ConfirmPuzzle`, and `_UnhandledKeyInput`.
  Before:
  ```csharp
  _selectedReagent = null;
  _selectedIndex = -1;
  _gridView.SetSelectedReagent(null);
  _reagentTray.ClearSelection();
  ```
  After: Extracted `private void ClearSelectionState()` method, all 5 call sites now use it. Net -15 lines.

### Magic Numbers in UI — Hardcoded Colors/Margins (P3, noted)
- **PuzzleScreen.cs:218-234** — Result overlay uses hardcoded color values, margins, and corner radii. Acceptable for procedural UI setup in a prototype. No action needed.

### Repeated GetMvpRules() Calls (P3, noted)
- **PuzzleScreen.cs:145, 177, 183, 392** — `ReactionRuleDatabase.GetMvpRules()` called 4 times across init and runtime. These are in different lifecycle phases and the method is lightweight. No action needed.

### Test Cleanup Could Use ResetPuzzle (P4, noted)
- **TestHarness.cs:225, 273, 326** — Three tests use a manual `while` loop to remove all placed reagents. Now that `ResetPuzzle()` exists and handles this case, these could be simplified. However, test code favors DAMP over DRY, and the explicit loop is clear. No action needed.

## Actions Taken
- **Fixed:** Extracted `ClearSelectionState()` in PuzzleScreen.cs (commit 494309a)
- **No tasks created** — remaining findings are P3-P4, below threshold.

## Status
ISSUES_FIXED
