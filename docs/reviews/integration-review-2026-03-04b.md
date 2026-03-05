# Integration Review — 2026-03-04 (b)

**Reviewed commits:** e610037..8bcf6ce (3.3, 3.4, R-3, R-4, R-5, B.1, B.2)

## Build & Unit Tests
- **Build:** PASS (0 errors, 1 warning — CS8785 GodotProjectDir in test project, cosmetic)
- **Unit tests:** PASS — 202/202 pass

## Test Harness
- **Result:** 11/11 tests pass, 0 failures
- **Screenshots:** 14 screenshots captured and visually verified

### Test Results Detail
| Test | Result | Notes |
|------|--------|-------|
| render_initial | PASS | 2 bags, 0 placed, 6 unplaced |
| objective_panel | PASS | FitAllSafely text correct, SetPuzzleType updates correctly |
| place_fire_pebble | PASS | Single cell placed at (2,2), Heat bar 2/4 |
| place_ice_shard_rotated | PASS | Rotation + placement working |
| invalid_placement | PASS | Out-of-bag placement correctly rejected |
| place_poison_fang | PASS | L-shape placed correctly |
| remove_reagent | PASS | Fire Pebble removed, count drops to 2 |
| placement_warnings | PASS | Danger/Warning/Safe all verified programmatically |
| status_bar | PASS | Heat and Toxicity tracking across place/remove cycle |
| confirm_and_reset | PASS | NotAllPlaced → Reset → place 5/6 → NotAllPlaced (graceful) |
| render_final | PASS | Final state rendered correctly |

### Visual Verification
- **01_render_initial:** Grid with 2 bags (SmallPouch 3x3, Satchel 4x3), reagent tray (6 items), objective panel, status bars at 0, Reset/Confirm buttons — all correct
- **02_place_fire_pebble:** Red cell at (2,2), Heat bar shows 2/4 orange fill — correct
- **08_warning_danger:** Red ghost cells when Fire Pebble hovers adjacent to Blast Jelly — correct
- **09_warning_caution:** Yellow/amber ghost cells at safe distance — correct
- **10_warning_safe:** Green ghost cells for non-reactive Inert Crystal — correct
- **11_status_bar:** Heat 3/4 (yellow), Cold 0/4, Toxicity 3/3 (red) — color shifting working correctly
- **12_after_reset:** Grid cleared, all 6 reagents in tray, bars at 0 — correct
- **13_all_placed:** 5/6 placed (Poison Fang won't fit), Heat 3/4, Cold 2/4 — correct
- **14_after_confirm:** "Cannot confirm: 1 reagent(s) not yet placed." — correct

## Coverage Analysis

### Features with integration test coverage
- **3.3 (Objective panel):** Test_ObjectivePanel verifies default text and SetPuzzleType
- **3.4 (Confirm/Reset):** Test_ConfirmAndReset covers early confirm, reset, and place-all flow

### Features with unit-only coverage (adequate)
- **R-3 (Null bag suppression):** 8 unit tests — DemoPuzzle doesn't use Null bags
- **R-4 (Fix MvpAspects):** Internal fix, no behavior change in DemoPuzzle
- **R-5 (Stable buffering normalization):** 6 boundary unit tests, behavior unchanged for threshold-0 pair rules
- **B.1 (Gravity bag type):** 8 unit tests — DemoPuzzle uses normal bags only
- **B.2 (Locked reagent placements):** 12 unit tests — DemoPuzzle has no locked reagents

### Coverage gap
Gravity bags (B.1), locked reagents (B.2), and Null bag suppression (R-3) cannot be integration-tested with the current DemoPuzzle. Task 4.2 (Replace DemoPuzzle with PuzzleGenerator) will enable harness tests with generated puzzles that include these features. Not blocking — unit test coverage is thorough.

### Known limitation
The confirm_and_reset test can only place 5 of 6 DemoPuzzle reagents (Poison Fang L-shape doesn't fit in any remaining space), so the Win path is never exercised. This is expected — task 4.1 (Win/loss evaluation) will add proper win/loss harness tests.

## Issues Found
**0 issues.** All features working correctly end-to-end.

## Summary
Solid state. Build clean, 202 unit tests pass, 11 integration tests pass, no visual regressions. Recent Core-layer features (Gravity, Locked, Null suppression, Stable normalization) are well-covered by unit tests. Integration coverage gap is a known limitation of the hardcoded DemoPuzzle and will resolve with task 4.2.
