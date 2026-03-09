# Code Review — 2026-03-05 (g)
**Commits reviewed:** 718631a..58a0f93 (HINT.5, HINT.6, HINT.7, integration review, docs)

## Findings

### Near-identical border rect methods (P3, note only)
- **src/Game/Inventory/MetaGridView.cs:344-379 vs 381-416** — `CreateHintedBorderRect` and `CreateBorderRect` share identical if/else direction logic with only color and border width differing. Rule of Three not met (2 occurrences), so no abstraction warranted. If a third border type appears, extract a shared `CreateDirectionalBorderRect(cellPixelPos, direction, borderWidth, color, zIndex)` method.

### Magic number in CreateHintedBorderRect (P3, note only)
- **src/Game/Inventory/MetaGridView.cs:347** — `const float bw = 2;` is a local magic number. The bag border equivalent uses `GridConstants.BagBorderWidth` (also 2). Could add `HintedBorderWidth` to GridConstants for consistency. Low impact — it's a local const, clearly scoped.

### Asymmetric cleanup pattern (P4, note only)
- **src/Game/Inventory/MetaGridView.cs:228-232 vs 308-312** — Bag borders are cleared in `RefreshDisplay()` before calling `CreateBagBorders()`, while hinted borders are cleared inside `CreateHintedBorders()` itself. The hinted approach (self-contained cleanup) is arguably cleaner. No functional impact.

### GetMvpRules() repeated calls (P3, carried forward)
- Previously noted in review (f). Still called 7 times in PuzzleScreen. Low impact — small list, infrequent calls.

## Actions Taken
- No fixes needed — no P0-P2 issues found.
- No tasks added to prd.json.

## Status
CLEAN
