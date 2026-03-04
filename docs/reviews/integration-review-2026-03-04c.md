# Integration Review — 2026-03-04 (c)

## Scope
Reviewed commits: ebe0fe1..61bbbaa (A.1, B.3, B.4, R-6, R-7 + fixes)

## Build & Unit Tests
- **Build**: PASS (0 errors, 0 warnings)
- **Unit tests**: 229/229 pass (228 existing + 1 new regression test)

## Test Harness Results
- **Result**: 11/11 PASS, 0 failures
- **Screenshots**: 15 verified, no visual regressions

### Test breakdown
| Test | Status |
|------|--------|
| render_initial | PASS |
| objective_panel | PASS |
| place_fire_pebble | PASS |
| place_ice_shard_rotated | PASS |
| invalid_placement | PASS |
| place_poison_fang | PASS |
| remove_reagent | PASS |
| placement_warnings | PASS (was FAIL before fix) |
| status_bar | PASS |
| confirm_and_reset | PASS |
| render_final | PASS |

## Issues Found & Fixed

### 1. Volatile net-hot self-reaction bug (P0 — fixed)
**Symptom**: `placement_warnings` test failed — all three hover positions returned `Danger` instead of `Danger`/`Warning`/`Safe`.

**Root cause**: A.1 (`EvaluateVolatileNetHotRule`) omitted the `neighbor == reagent` exclusion present in the 2-trigger pair evaluation path. This caused reagents with both Hot and Volatile cells (like the DemoPuzzle's Blast Jelly: Volatile, Hot, Volatile, Volatile) to self-explode. Any `EvaluateProspective` call with Blast Jelly already placed would detect reactions regardless of where the new reagent was being placed.

**Fix**: Added `|| neighbor == reagent` to the null check at line 175 of `ChemistryEngine.cs`, matching the existing pattern at line 97 for pair-trigger rules.

**Commits**:
- `8c44c4a` fix: Exclude same-reagent cells from Volatile net-hot calculation
- `61bbbaa` test: Add unit test for Volatile same-reagent self-explosion exclusion

## Visual Verification
All 15 screenshots inspected:
- Grid layout renders correctly with two bags (SmallPouch 3x3, Satchel 4x3)
- Reagent colors differentiated (red=hot, blue=cold, green=toxic, orange=volatile, grey=stable)
- Placement warnings show correct three-tier colors (red=danger, yellow=warning, green=safe)
- Status bar shows correct fill levels and color gradients (yellow approaching threshold, red at threshold)
- Objective panel text renders correctly
- Reset/Confirm buttons visible and functional

## Coverage Gaps
- **B.3 (Heavy in Gravity bags)**: No integration test — DemoPuzzle uses Normal bags only. Covered by 9 unit tests. Gap resolves with task 4.2 (PuzzleGenerator integration).
- **B.4 (Non-rectangular bags)**: No integration test — DemoPuzzle uses rectangular bags. Covered by 6 unit tests. Same gap as B.3.
- **A.1 (Volatile net-hot)**: Now indirectly covered by placement_warnings test (Blast Jelly has volatile+hot, placement evaluation exercises net-hot path). 8 unit tests including new regression test.
- **R-6, R-7**: Pure refactors, no new integration coverage needed. All 229 unit tests pass.

## Notes
- The `confirm_and_reset` test can only place 5/6 reagents in both primary and fallback layouts. This is expected — the DemoPuzzle was designed for feature demonstration, not guaranteed "fit all" solvability. The test handles this gracefully.
- SameBag distinct-pair Stable buffering (line 283-284) still uses the old `stableBuffer > rule.Threshold` skip pattern rather than the normalized `effectiveThreshold` pattern. R-5 normalized Adjacent and WholeInventory tiers but missed this code path. The behavior is equivalent for threshold=0 rules (any stable suppresses), but inconsistent. Low priority — no functional impact for current rule set.
