# Code Review — 2026-03-05c
**Commits reviewed:** d0c4e68..c28c85e (H.6, H.7, H.8)

## Findings

### DRY — `_highlightOverlay.Clear()` always paired with `ClearGhost()`
- **MetaGridView.cs:313, 362, 393, 434, 441** — `_highlightOverlay.Clear()` called in 5 separate locations, always immediately after `ClearGhost()`. These two operations are logically coupled: clearing the ghost preview should always clear highlights too. Moving the clear into `ClearGhost()` removes 4 redundant lines and prevents future callers from forgetting to clear highlights.
  Before: `ClearGhost(); _highlightOverlay.Clear();` (5 locations)
  After: `ClearGhost()` calls `_highlightOverlay.Clear()` internally (1 location)

### YAGNI (P3) — `HoveredReagentChanged` signal declared but unconsumed
- **MetaGridView.cs:70** — `HoveredReagentChanged` signal is declared and emitted but nothing subscribes to it. Per the H.8 worklog, this is intentional infrastructure for H.10 (legend panel) and H.11 (item detail panel). Acceptable as lightweight forward scaffolding but should be wired when those features land.

### Consistency (P3) — `SetHoverPosition` doesn't call `UpdateHoveredReagent`
- **MetaGridView.cs:132-137** — The test helper `SetHoverPosition` calls `UpdateGhostPreview()` but not `UpdateHoveredReagent()`, unlike the real mouse motion path. Currently harmless since no tests rely on `HoveredPlacedReagent` being set via the test helper, but creates a subtle divergence between test and production paths.

## Actions Taken
- **Fixed:** DRY violation — moved `_highlightOverlay.Clear()` into `ClearGhost()` (commit c0ae3b0)
- **No tasks created.** P3 findings are noted only; they'll resolve naturally when the consuming features (H.10, H.11) are implemented.

## Status
ISSUES_FIXED
