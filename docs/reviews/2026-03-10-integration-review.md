# Integration Review — 2026-03-10

## Scope
Commits: ef135c4..88dd632 (LO.10, GJ.1–GJ.8, SI.1, code reviews)

## Build
- **dotnet build**: PASS (0 errors, 1 cosmetic CS8785 warning)
- **dotnet test**: PASS (811/811 unit tests)

## Integration Harness
- **Result**: 41/41 tests pass, 0 failures
- **New test added**: `gravity_fall_placement` (GJ.1 coverage)
- **Screenshots**: 49+ verified, no visual regressions

## Feature Coverage Assessment

| Feature | Integration Test | Status |
|---------|-----------------|--------|
| LO.10 — Touch-friendly sizing | web_demo_layout, web_demo_responsive | Covered |
| GJ.1 — Gravity soft constraint + auto-fall | **gravity_fall_placement** (NEW) | Covered |
| GJ.2 — Volatile explosion indicator fix | volatile_explosion_overlay | Covered |
| GJ.3 — InfoColumnManager | web_demo_layout (partial) | Partial |
| GJ.4 — Bag movement UI | bag_movement | Covered |
| GJ.5/5b — SpriteSwapper art loading | N/A (assets gitignored) | Skip |
| GJ.6 — Monomino sprite generation | N/A (art assets) | Skip |
| GJ.7 — CellMountOverlay | N/A (texture gitignored) | Skip |
| GJ.8 — Bag border NinePatchRect | N/A (texture gitignored) | Skip |
| SI.1 — UIThemeLoader | N/A (assets gitignored) | Skip |

## New Test: gravity_fall_placement
Verifies GJ.1 core behavior:
1. Placement at top of gravity bag succeeds (soft constraint, not hard rejection)
2. `PlaceReagentWithFall()` drops reagent to bottom (row 5)
3. Second reagent stacks above first (row 4)
4. Screenshot confirms visual correctness

## Visual Inspection
All 49+ screenshots verified:
- Grid rendering, reagent placement, aspect colors: OK
- Damage overlays (fragile, volatile): correct red/orange tints
- Bag state overlay (thermal tint): pulsing correctly
- Bag movement: move/rotate with reagent carrying works
- WebDemo layout: responsive breakpoints working (1280, 1024, 700px)
- Info column: overview/reagent detail views switching correctly
- Gravity fall: reagents stack from bottom in gravity bags
- Status bar: Heat/Cold counters update correctly
- Tutorial, daily puzzle, editor, hints: all functioning

## Issues Found
- **0 P0/P1/P2 issues**

## Coverage Gaps (acceptable)
- **GJ.3 InfoColumnManager**: AspectDetail view and back-button navigation not tested. Partial coverage via web_demo_layout (view switching to ReagentDetail verified). Low risk — UI navigation only.
- **GJ.5–GJ.8, SI.1**: Art pipeline features require gitignored textures. Integration testing not feasible. Covered by code review and manual visual inspection. Fallback rendering (procedural ColorRects) is tested implicitly via all harness tests that render the grid.
- **GO.3–GO.5** (from prior review): Bag movement with reagent carrying is Core-only, no Game UI for interactive bag manipulation with reagents. Unit test coverage is comprehensive (789+ tests). Will need integration tests when bag manipulation UI supports carried reagents visually.

## Pre-existing Cosmetic Issues (unchanged)
1. Heat label left-edge clipping on wide grids
2. Status bar shows base thresholds, not effective (Medicinal/Stable adjusted)
