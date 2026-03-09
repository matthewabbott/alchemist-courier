# Integration Review — 2026-03-06 (b)

## Scope
Reviewed all changes since the previous integration review (2026-03-06a, which covered CQ.1–CQ.4, GF.1–GF.4, VS.1–VS.4, VS.6).

New changes reviewed:
- VS.5: ActionHistory undo/redo wiring into PuzzleScreen UI
- VS.7: SpriteSheetSpec + SpriteStateRenderer (sprite rendering pipeline)
- VS.8: DevSpriteGenerator (procedural sprite sheet generation)
- R1: Extract RefreshAfterGridChange helper in PuzzleScreen
- R2: Deduplicate MetaGridView border creation methods
- Code Reviews D and E

## Build
- **dotnet build**: PASS (0 errors, 2 known cosmetic warnings)
- **dotnet test**: PASS — 632/632 unit tests pass (16 new since previous review)

## Integration Test Harness
- **Result**: PASS — **31/31** tests pass, 0 failures
- **Screenshots**: 40 screenshots captured and visually inspected
- **New test added**: `undo_redo` — validates ActionHistory place/undo/redo lifecycle

### New test: `undo_redo`
Covers VS.4/VS.5 end-to-end through PuzzleScreen API:
1. Initial state: CanUndo=false, CanRedo=false
2. Place 2 reagents → UndoCount=2
3. Undo twice → grid returns to 0 placed, CanRedo=true
4. Redo twice → both reagents restored at original positions, placed count=2
5. All 12 assertions pass

## Visual Inspection

### No regressions
All screenshots match expectations. New features rendering correctly:
- **Undo/Redo buttons** visible in button bar (screenshot 40)
- **Named reagents** from ReagentCatalog in generated puzzles
- **Border rendering** (R2 refactor) — hinted and locked borders render identically to pre-refactor

### Pre-existing cosmetic issues (unchanged)
1. Tutorial win overlay vertical text (width collapse)
2. Heat label left-edge clipping
3. Status bar base thresholds (not Stable/Medicinal-adjusted)

## Coverage Analysis

### Features now covered by integration tests
| Feature | Test(s) |
|---------|---------|
| VS.4/VS.5 (Undo/Redo) | **`undo_redo` (NEW)** |
| R1 (RefreshAfterGridChange) | All existing placement/removal tests |
| R2 (Border dedup) | `locked_reagent_removal_blocked`, `hint_system` |

### Not integration-tested (adequate without)
| Feature | Reason |
|---------|--------|
| VS.7 (SpriteSheetSpec/Renderer) | Visual-only rendering, no game logic impact |
| VS.8 (DevSpriteGenerator) | Build-time tool, not runtime |

## Issues Found
**0 issues.** No test failures, no visual regressions, no missing critical coverage.

## Overall Assessment
**CLEAN.** 632 unit tests + 31 integration tests all pass. The undo/redo integration gap identified in coverage analysis has been closed with a new test. The R1/R2 refactoring introduced no behavioral changes. VS.7/VS.8 sprite pipeline is infrastructure — integration testing will become relevant when sprite-based rendering is exercised in gameplay.
