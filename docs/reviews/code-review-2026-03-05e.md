# Code Review — 2026-03-05e
**Commits reviewed:** 949e489..18f605c (H.12, H.13, H.14, integration tests)

## Findings

### DRY — Duplicate constraint highlight logic (P3)
- **`src/Core/Highlight/HighlightEngine.cs:35-66` vs `73-103`** — `ComputeGhostConstraintHighlights` is a near-exact copy of `ComputeConstraintHighlights`, differing only in `HighlightLayer.Constraint` vs `HighlightLayer.GhostOverlay`. Could extract a shared `ComputeConstraintHighlightsForLayer(grid, reagent, position, layer)` helper.
  - **Not flagged**: Only 2 occurrences — Rule of Three not met. The duplication is acknowledged in the H.13 worklog entry ("mirrors ComputeConstraintHighlights exactly"). If a third layer variant appears, extract then.
  - **Side effect**: When `MetaGridView.ComputeAndRenderHighlights()` calls both methods, `GetConstraintViolations()` runs twice for the same inputs. No correctness issue; minor redundant work during hover preview.

### DRY — Repeated GetMvpRules() calls (P3)
- **`src/Game/Inventory/PuzzleScreen.cs`** — `ReactionRuleDatabase.GetMvpRules()` is called 7 times across `BuildUI()` (×3), `ConfirmPuzzle()`, `ToggleEagleSense()`, `RefreshEagleSense()`, and `UpdateStatusThresholds()`.
  Before: `var rules = ReactionRuleDatabase.GetMvpRules();` (repeated inline)
  After: Store as `private List<ReactionRule> _rules = null!;` initialized once in `_Ready()`.
  - **Not flagged**: `GetMvpRules()` is a static factory that returns a new list each call, so this is allocation churn rather than a bug. Low impact — puzzle screen is created once per puzzle. Worth cleaning up next time PuzzleScreen is touched.

### Code quality — Overall assessment (positive)
- **HighlightEngine.cs**: Clean static class, no Godot dependencies in Core, good separation. Test coverage is thorough (30 tests covering constraint, interaction, legend, eagle sense, ghost overlay, and StatusThresholds).
- **PuzzleScreen.cs orchestration (H.12)**: Signal wiring is well-organized — `WireSignals()` is the single connection point. Eagle sense toggle with auto-refresh on place/remove is solid.
- **Integration tests**: `Test_EagleSenseOrchestration` and `Test_LegendGridLinking` properly create fresh PuzzleScreen instances for isolation. Good use of state assertions with descriptive logging.

## Actions Taken
- No direct fixes committed (no P0-P1 issues found).
- No tasks added to prd.json (P3 findings below Rule of Three threshold for task creation).

## Status
CLEAN
