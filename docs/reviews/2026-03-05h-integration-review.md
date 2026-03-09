# Integration Review — 2026-03-05 (h)

## Scope
Reviewed commits: `1c7cdcc..e29edbc` (T.2, T.3, code-review-i fix, E.1, E.2, E.3)

## Build
- **dotnet build**: PASS (1 cosmetic CS8785 warning in test .csproj — known)
- **dotnet test**: PASS — **519/519 unit tests** (22 new since last integration review: 12 PuzzleSearchSolver + 10 PuzzleFile)

## Integration Test Harness
- **Result**: **23/23 tests pass, 0 failures**
- All tests complete without errors or timeouts

## Visual Verification
**34 screenshots inspected**, no regressions from previous review.

### Screenshots verified:
| Screenshot | Feature | Status |
|---|---|---|
| 01_render_initial | Initial layout, grid, bags, panels | OK |
| 02_objective_panel | Objective text display | OK |
| 02b_legend_open | Aspect legend with expanded Hot entry | OK |
| 02c_legend_highlight | Legend highlight state | OK |
| 02_place_fire_pebble | Placement with status bar update | OK |
| 03_place_ice_shard_rotated | Rotated reagent placement | OK |
| 04_invalid_placement | Invalid placement rejection | OK |
| 05_place_poison_fang | Multi-aspect reagent placement | OK |
| 06_remove_reagent | Reagent removal | OK |
| 07_render_final | Final render state | OK |
| 08-10_warnings | Danger/Warning/Safe warning levels | OK |
| 11_status_bar | Status bar accumulation | OK |
| 12-14_confirm_reset | Reset and confirm flow | OK |
| 15_loss_confirm | Loss with explosion reaction | OK |
| 16_win_confirm | Win with all placed safely | OK |
| 17_generated_puzzle | Seeded puzzle generation + solve | OK |
| 18_tutorial_lesson1_win | Tutorial win + Next Lesson | OK (pre-existing vertical text) |
| 19_tutorial_lesson2 | Tutorial lesson progression | OK |
| 20_daily_puzzle | Daily puzzle generation + solve | OK |
| 21-22_highlight_overlay | Highlight rendering + eagle sense dim | OK |
| 23_highlight_constraint | Constraint highlight (Sharp on edge) | OK |
| 24_highlight_interaction | Interaction highlight (Hot near Volatile) | OK |
| 25_rejection_tooltip | Rejection tooltip with reason text | OK |
| 26_item_detail_tray | INSPECT panel from tray hover | OK |
| 27_item_detail_grid | INSPECT panel from grid hover | OK |
| 28_item_detail_multi_aspect | Multi-aspect reagent detail | OK |
| 29_eagle_sense_on | Eagle sense active with dim overlay | OK |
| 30_legend_grid_link | Legend-to-grid aspect linking | OK |
| 31-34_hint_system | Hint use, snap-back, second hint, dislodge | OK |

## New Features Since Last Review

### E.1 — PuzzleSearchSolver (Pure Core)
- Backtracking constraint-satisfaction solver with MRV heuristic
- 12 unit tests covering solvable/unsolvable/timeout/cancelled/reactive scenarios
- **No integration test needed** — not wired to UI yet (editor infrastructure)

### E.2 — PuzzleFile (Pure Core)
- PuzzleState wrapper with authoring metadata, JSON file I/O
- 10 unit tests covering serialization, null fields, corrupt JSON, directory creation
- **No integration test needed** — not wired to UI yet (editor infrastructure)

### E.3 — ShapeEditorWidget (Godot UI)
- Reusable NxN grid editor control for bool[,] shape matrices
- No unit tests (Godot widget), no integration test
- **No integration test needed** — standalone editor control, not in game scene

### T.2/T.3 — Documentation + Tooling
- Review prompt updated with desloppify step, game architecture doc created
- No code changes affecting game behavior

## Issues Found
**0** (P0: 0, P1: 0, P2: 0)

## Pre-Existing Cosmetic Issues (unchanged)
1. **Tutorial vertical text**: Win overlay renders "All reagents placed safely! Puzzle complete!" as vertical single-character-per-line text
2. **Heat label left-edge clipping**: On wider generated/hint puzzles, "Heat:" label is pushed partially off the left screen edge
3. **Status bar base thresholds**: Status bar shows base threshold values, not effective thresholds adjusted for Null bag suppression

## Coverage Gaps (unchanged from review f)
- B.1-B.4 (bag mechanics: Gravity, Reinforced, Null, Fragile) and A.1-A.6 (aspect mechanics) have comprehensive unit tests but are not directly exercised by integration tests (Easy difficulty avoids special bags and complex constraints)
- Recommend future `Test_MediumDifficultyPuzzle` test when Medium/Hard difficulty is used in game flow

## Summary
All systems stable. E.1-E.3 are editor-infrastructure features with proper unit test coverage but no game-facing behavior to integration-test yet. No regressions, no new issues. Unit test count grew from 497 to 519 (+22). Integration harness unchanged at 23/23.
