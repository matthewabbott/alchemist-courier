# Integration Review — 2026-03-08

## Scope
Reviewed commits: `7d7f708..cb0535a` (since last integration review 2026-03-06)

Features covered:
- CR.1-CR.5: ReagentCatalog refactor (72→24 items)
- HF.1-HF.3: Hover frame overlay
- SA.1-SA.2: SpriteAssigner tool
- GO.1: Heavy constraint softened to advisory
- GO.2: MetaGrid.MoveBag and RotateBag methods
- SB.1: SandboxScreen catalog mode
- I.1-I.4: Solution count restriction, hot/cold balance, toxic removal, full nullification
- D.1-D.2: Fragile allowed in gravity bags, thermal fracture/crushing
- A.3: Per-bag gravity direction
- C.1: Aspect outlines rotate with reagent
- J.3: ViolationTracker core

## Build
- **Build:** PASS (0 errors, 1 cosmetic CS8785 warning)
- **Unit tests:** 776/776 pass

## Integration Harness
- **Result:** 32/32 tests pass, 0 failures (after fixes)
- **Screenshots:** 40+ screenshots verified, no visual regressions

## Issues Found & Fixed
3 test infrastructure bugs caused by I.2/I.3 gameplay overhaul changes:

1. **status_bar test** (I.3): Expected Toxic=3 but Poisoning cumulative rule was removed. Fixed by removing Toxic assertion — only Heat and Cold bars remain.

2. **win_loss_evaluation test** (I.2): WIN path returned `HotColdImbalance` because DemoPuzzle has inherent Hot/Cold imbalance per bag (Hot=3 total, Cold=2 total). Fixed by using a PuzzleGenerator-produced puzzle with known-good solution for the WIN path.

3. **hint_system dislodge test** (test logic): `UseHint()` auto-picks via `PickNextHintTarget` which didn't choose the intended target for dislodge. Fixed by calling `HintEngine.ExecuteHint()` directly with specific reagent index — tests the dislodge mechanic without depending on auto-pick ordering.

Commit: `cb0535a`

## Coverage Gaps
Features with unit tests but no integration test coverage:
- **GO.2 (MoveBag/RotateBag)**: 19 unit tests, Core-only methods not yet wired to game UI
- **D.2 (Thermal fracture/crushing)**: 15 unit tests, triggers during chemistry evaluation
- **A.3 (Per-bag gravity direction)**: 27 unit tests, DemoPuzzle/Easy puzzles use default Down
- **J.3 (ViolationTracker)**: 15 unit tests, not yet wired to UI layer

All gaps are for Core features that aren't yet surfaced in the game layer. Unit test coverage is thorough. A future `Test_MediumDifficultyPuzzle` would partially address these.

## Visual Observations
- No new visual regressions detected
- Pre-existing cosmetic issues unchanged:
  1. "Heat:" label left-edge clipping on wide grids (minor)
  2. Status bar shows base thresholds, not effective (Medicinal/Stable adjusted)
  3. Game flow menu screenshot shows test-artifact overlap (not a real bug)
- StatusBarPanel correctly shows only Heat and Cold bars (Toxic bar removed per I.3)
- Hover frame overlay renders correctly on placed reagents
- Generated puzzle WIN path shows balanced Hot/Cold per bag
