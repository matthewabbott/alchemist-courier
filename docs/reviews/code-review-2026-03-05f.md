# Code Review — 2026-03-05 (f)
**Commits reviewed:** e6ddc8e..f8e4117 (HINT.3, HINT.4, docs update)

## Findings

### DRY — Duplicated hint execution logic (P1)
- **src/Game/Inventory/PuzzleScreen.cs:654-754** — `OnHintPressed()` and `UseHint()` duplicated ~40 lines of identical core logic: pick target → get solution → check snap-back → check budget → execute → update state → refresh UI.
  - Bug risk: modifying hint logic in one method but not the other.
  - Behavioral inconsistency: `UseHint()` called UI refresh methods (ClearSelectionState, RefreshDisplay, etc.) even on failure paths where no state changed, while `OnHintPressed` correctly skipped refresh on failure.

  Before: Two methods with ~40 lines of duplicated hint orchestration logic.
  After: `OnHintPressed` is a thin wrapper calling `UseHint()` and translating the result to status messages. `UseHint()` only refreshes UI on success. Net -24 lines.

### GetMvpRules() called repeatedly (P3, note only)
- **src/Game/Inventory/PuzzleScreen.cs** — `ReactionRuleDatabase.GetMvpRules()` is called in 7 places (ConfirmPuzzle, UpdateStatusThresholds, RefreshEagleSense, ToggleEagleSense, etc.). Could cache as a field. Low impact — returns a fresh list each time but it's small and infrequent.

## Actions Taken
- Fixed P1 DRY violation: refactored `OnHintPressed` to delegate to `UseHint()`, eliminated 48 lines of duplicated code (net -24 lines after adding status message translation). Also fixed `UseHint()` to only refresh UI on success.
- Commit: 718631a

## Status
ISSUES_FIXED
