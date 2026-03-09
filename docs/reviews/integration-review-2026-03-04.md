# Integration Review — 2026-03-04

**Reviewed commits:** 36b1865..e610037 (R-2, R-1, 3.1, 3.2, code review)
**Branch:** dev

## Build & Unit Tests

- `dotnet build`: **PASS** (0 errors, 1 warning — CS8785 GodotProjectDir in test project, cosmetic)
- `dotnet test`: **PASS** — 168/168 tests pass

## Test Harness Results

**9 passed, 0 failed**

| Test | Result | Notes |
|------|--------|-------|
| render_initial | PASS | 2 bags, 0 placed, 6 unplaced |
| place_fire_pebble | PASS | 1x1 single cell placement |
| place_ice_shard_rotated | PASS | 1x2 rotated to vertical, placed in Satchel |
| invalid_placement | PASS | Placement outside bags correctly rejected |
| place_poison_fang | PASS | L-shape (3x2) placed in SmallPouch |
| remove_reagent | PASS | Fire Pebble removed, count decremented |
| placement_warnings | PASS | Danger/Warning/Safe levels all correct |
| status_bar | PASS | Heat/Cold/Toxicity track place & remove |
| render_final | PASS | Clean final state |

## Visual Verification

All 11 screenshots inspected. No visual regressions.

- **Grid rendering:** Bags and reagent cells render with distinct colors at correct positions
- **Reagent tray:** Updates correctly on place/remove (items appear/disappear from list)
- **Status bar (3.2):** Heat/Cold/Toxicity bars display with correct values and color shifting:
  - Aspect color at low fill (Heat: red at 2/4)
  - Yellow approaching threshold (Heat: yellow at 3/4)
  - Red at/above threshold (Toxicity: red at 3/3)
- **Placement warnings (3.1):** Three-tier ghost colors working:
  - Danger (red): Fire Pebble adjacent to Blast Jelly (Hot+Volatile = Explosion)
  - Warning (yellow/amber): Fire Pebble far from danger but has reactive "hot" aspect
  - Safe (green): Inert Crystal with only non-reactive "stable" aspect
- **Status text:** Contextual messages update correctly ("Placed X. N remaining.", "Placing: X | R / Right-click = rotate ...", "Removed X.")

## Feature Coverage Assessment

| Recent Feature | Integration Tested | Unit Tests |
|---|---|---|
| R-2: Aspect.FromId refactor | N/A (pure refactor) | 12 tests |
| R-1: Stable buffering mechanic | Indirect (Safe warning uses Stable reagent) | 7 tests |
| 3.1: Placement warnings | Direct (3 warning levels) | N/A (UI feature) |
| 3.2: Status bar panel | Direct (place/remove tracking, 4 state checks) | N/A (UI feature) |

## Issues Found

**None.** All features working as designed.

## Coverage Gaps (Low Priority)

1. **Cinnabar Block (2x2 square)** — only demo reagent never placed in harness. Low risk: placement logic is unit-tested; adding a shape-variant test would provide marginal value.
2. **Stable buffering suppression** — the mechanic (Stable cells adjacent to reactive pairs suppress explosions) is not directly tested in the harness. Demo puzzle layout makes this complex to set up. The 7 unit tests in ChemistryEngineTests thoroughly cover the logic.

## Recommendation

No action needed. The game is in a healthy state for continued development. Next pending tasks (3.3 objective panel, 3.4 confirm/reset buttons) will benefit from the existing test infrastructure — new integration tests should be added alongside those features.
