# Code Review — 2026-03-06 (B)
**Commits reviewed:** 9809fb9..eba3d86

## Findings

### Fail-Fast — DateTime.UtcNow Inconsistency (P1)
- **DailyPuzzleScreen.cs:41,75,104,106** — `DateTime.UtcNow` is called 4 times independently in `_Ready()`. `PuzzleDate` is set from the first call (line 41), but subsequent uses re-read the clock. If the method executes across a midnight boundary, the day label, header text, and streak calculation could reference a different date than `PuzzleDate`.
  Before: `$"Day {DateTime.UtcNow.DayOfYear}"` and `SaveInfo.GetEffectiveStreak(DateTime.UtcNow.Date)`
  After: `$"Day {PuzzleDate.DayOfYear}"` and `SaveInfo.GetEffectiveStreak(PuzzleDate)`

### Fail-Fast — Silent Default in GameFlowController (P3, quick win)
- **GameFlowController.cs:59** — `_ => CreateMainMenu()` silently handles unknown `GameScreen` enum values. Previous review (fef7e61) established the pattern of throwing on unhandled enum values.
  Before: `_ => CreateMainMenu(),`
  After: `_ => throw new ArgumentOutOfRangeException(nameof(screen), screen, "Unhandled GameScreen value"),`

### Least Surprise — Unnecessary Base Method Shadow (P3, boy scout)
- **ResultOverlayPanel.cs:69-72** — `public new void Hide()` shadows `CanvasItem.Hide()` with identical behavior (`Visible = false`). The `new` keyword hides the base method, which violates Principle of Least Surprise and adds dead code.
  Before: Custom `Hide()` method with `new` keyword.
  After: Removed — callers now use inherited `CanvasItem.Hide()`.

### Self-Documenting Code — Magic Number (P3, noted only)
- **GameFlowController.cs:111** — `_saveData.CompleteTutorialLesson(5)` hardcodes the tutorial lesson count. Coupled with `SaveData.IsTutorialCompleted()` which also hardcodes `>= 5`. Not a bug since `TutorialCompleted` event is the real trigger, but violates Single Source of Truth.

## Actions Taken
- **Fixed** DateTime.UtcNow inconsistency in DailyPuzzleScreen (P1) — 3 occurrences replaced with `PuzzleDate`.
- **Fixed** Silent default fallback in GameFlowController.TransitionTo (P3) — now throws `ArgumentOutOfRangeException`.
- **Fixed** Redundant `Hide()` shadow in ResultOverlayPanel (P3) — removed 5 lines of dead code.
- No tasks created in prd.json — remaining findings are P3 notes only.

## Desloppify
- Scan skipped (tool requires manual approval).

## Status
ISSUES_FIXED
