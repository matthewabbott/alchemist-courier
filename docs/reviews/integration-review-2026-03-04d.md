# Integration Review — 2026-03-04 (d)

## Scope
Commits: 8bcf6ce..ded67ea (B.3, B.4, R-6, R-7, A.1 fix, A.2, A.3, A.4, A.5, A.6, review-fix, 3.5, 4.1)

## Build
- `dotnet build`: PASS (0 errors, 1 cosmetic CS8785 warning)
- `dotnet test`: PASS (289/289 unit tests)

## Test Harness
- **Result: 13/13 tests pass, 0 failures**
- 18 screenshots captured, all visually verified

### Test Results
| Test | Result | Notes |
|------|--------|-------|
| render_initial | PASS | 2 bags, 0 placed, 6 unplaced |
| objective_panel | PASS | FitAllSafely text correct, SetPuzzleType updates |
| aspect_primer | PASS | Starts hidden, toggles open (7 rules), toggles closed |
| place_fire_pebble | PASS | 1x1 at (2,2) in SmallPouch |
| place_ice_shard_rotated | PASS | Rotated domino at (5,1) in Satchel |
| invalid_placement | PASS | (0,0) outside bags rejected |
| place_poison_fang | PASS | L-shape Sharp at (6,1) in Reinforced Satchel |
| remove_reagent | PASS | Fire Pebble removed, count 3→2 |
| placement_warnings | PASS | Danger/Warning/Safe all correct |
| status_bar | PASS | Hot: 0→2→3→1, Toxic: 0→3 |
| confirm_and_reset | PASS | NotAllPlaced → Reset → place all attempts |
| win_loss_evaluation | PASS | LOSS (Explosion) and WIN (0 reactions) both verified |
| render_final | PASS | Final state rendered |

## Visual Verification
All 18 screenshots inspected:
- Grid renders correctly with two distinct bag regions
- Reagent colors are distinguishable and placed in correct cells
- Placement warning colors (red/yellow/green ghost) are clearly different
- Status bar fills and color shifts (aspect color → yellow → red) work correctly
- Primer panel shows all 7 reaction rules with correct color coding
- Result overlays: red "FAILED" with reaction list, green "SUCCESS" both render correctly
- Reset clears grid completely and resets all status bars to 0/N
- No visual regressions from previous reviews

## Coverage Analysis

### Well-covered features
- **3.5 Aspect primer**: Direct test of toggle + rule count
- **4.1 Win/loss evaluation**: Both LOSS (explosion) and WIN paths verified end-to-end
- **A.1 Volatile net-hot**: Explosion detection works in win/loss test (Hot near Volatile)
- **A.2 Sharp edge**: Poison Fang (Sharp) places successfully in Reinforced Satchel
- **A.4/A.5 Catalytic/Medicinal**: Cinnabar Block (Catalytic+Medicinal) participates in win/loss evaluation

### Integration gaps (deferred to 4.2)
These features have thorough unit test coverage but no integration tests because the DemoPuzzle doesn't exercise them:
- **B.1/B.3 Gravity bags + Heavy constraint** (8+8 unit tests)
- **B.2 Locked reagents** (12 unit tests)
- **B.4 Non-rectangular bag shapes** (6 unit tests)
- **A.3 Fragile universal vulnerability** (10 unit tests)
- **A.6 ComplexityLevel filtering** (28 unit tests)
- **R-3 Null bag suppression** (8 unit tests)

These gaps will resolve when task 4.2 (Replace DemoPuzzle with PuzzleGenerator) is complete.

## UX Observations

### Status bar shows base threshold, not effective threshold
In the WIN path of win_loss_evaluation (screenshot 16_win_confirm.png), the Toxicity bar displays **3/3 in red** — yet the puzzle evaluates as WIN with zero reactions. This is because Medicinal on Cinnabar Block raises the *effective* threshold above 3, but the status bar displays raw cumulative value against the *base* threshold.

**Impact**: Players may see a red "at threshold" bar and believe they're failing when they're actually safe. Consider updating the status bar to show effective thresholds (accounting for Medicinal buffers) or adding a visual indicator when Medicinal is active. Recommend addressing in task 5.1 (UI polish).

## Issues
- **P0**: 0
- **P1**: 0
- **P2**: 0
- **UX observations**: 1 (status bar threshold display)

## Summary
All systems working correctly end-to-end. 289 unit tests + 13 integration tests all pass. No regressions from previous reviews. The game successfully supports the full puzzle loop: place reagents with warning feedback, monitor cumulative thresholds, confirm for win/loss evaluation, reset to retry. The aspect interaction primer provides player reference for all 7 reaction rules. Next milestone: task 4.2 (PuzzleGenerator integration) which will unlock integration testing of Gravity bags, locked reagents, non-rectangular shapes, and other advanced mechanics.
