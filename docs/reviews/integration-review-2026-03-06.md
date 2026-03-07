# Integration Review — 2026-03-06

## Scope
Reviewed all changes since the last integration review (2026-03-04, which covered through R-2, R-1, 3.1, 3.2).

Changes reviewed:
- CQ.1–CQ.4: PuzzleValidator extraction, locked reagent feedback, PuzzleScreen decomposition
- GF.1–GF.4: GameFlowController, MainMenuScreen, DailyPuzzleScreen, main.tscn wiring
- VS.1: Sharp/Fragile promotion to Simple tier
- VS.2: ReagentCatalog (72 canonical reagent definitions)
- VS.3: Wire PuzzleGenerator to ReagentCatalog
- VS.4: ActionHistory undo/redo infrastructure (Core-only)
- VS.6: VisualStateResolver (Core-only)
- ART.4: Manuscript reference curation (docs only)
- Code Reviews B and C

## Build
- **dotnet build**: PASS (0 errors, 2 known cosmetic warnings: CS8785 GodotProjectDir in test .csproj, xUnit2013 in ReagentCatalogTests)
- **dotnet test**: PASS — 616/616 unit tests pass

## Integration Test Harness
- **Result**: PASS — 30/30 tests pass, 0 failures
- **Screenshots**: 39 screenshots captured and visually inspected

## Visual Inspection Summary

### Clean renders (no issues)
- `01_render_initial.png` — Puzzle screen with 2 bags, objective, aspects, status bar
- `02_objective_panel.png` — Objective text updates correctly
- `02b_legend_open.png` / `02c_legend_highlight.png` — Aspect legend expand/collapse/highlight
- `02–07` — Placement, rotation, removal, invalid placement all render correctly
- `08–10` — Warning system (Danger/Warning/Safe) color-coded correctly
- `11_status_bar.png` — Heat/Cold/Toxicity bars update with placements
- `12–14` — Reset and confirm flows render correctly
- `15_loss_confirm.png` — Loss state shows "Reactions detected!" with red Toxicity 3/3
- `16_win_confirm.png` — Win state shows "All reagents placed safely! Puzzle complete!"
- `17_generated_puzzle.png` — PuzzleGenerator seed=42, all reagents placed, solution validates
- `18–19` — Tutorial lesson 1 win and lesson 2 load
- `20_daily_puzzle.png` — Daily puzzle with "Meet the requirement" objective, correct layout
- `21–24` — Highlight overlay, eagle sense, constraint and interaction highlights
- `25_rejection_tooltip.png` — "Sharp reagent on edge cell of non-Reinforced bag" tooltip
- `25_locked_removal_blocked.png` — Locked reagent cannot be removed
- `26–28` — Item detail panel for tray, grid, and multi-aspect reagents
- `29_eagle_sense_on.png` — Eagle sense toggle with dimming
- `30_legend_grid_link.png` — Legend selection highlights grid cells
- `31–34` — Hint system: used, snapback, second hint, dislodge
- `35–39` — Editor screen, create/save, solver, play mode

### Known cosmetic issues (pre-existing, not regressions)
1. **Left-edge text clipping**: On some screenshots (15, 16, 17, 20) faint text fragments are visible at the left edge — reagent tray label characters clipping outside the viewport. Pre-existing, documented in MEMORY.md.
2. **Status bar base thresholds**: Shows base thresholds (Heat 0/4, etc.) rather than effective thresholds adjusted by Medicinal/Stable. Pre-existing, documented.

### Test artifacts (not game bugs)
- **Screenshots 26 (game_flow_menu) and 27 (game_flow_tutorial)**: Show overlapping UI — the main menu buttons and daily puzzle screen render on top of the test harness's underlying puzzle screen. This is a test harness artifact: the game flow tests create a GameFlowController as a child of the existing test scene, so both scenes are visible. In the real game, `main.tscn` starts with GameFlowController alone — no overlap occurs.

## Test Coverage Analysis

### Features with integration test coverage (comprehensive)
| Feature | Integration Test(s) |
|---------|-------------------|
| CQ.1–CQ.2 (PuzzleValidator) | `win_loss_evaluation` |
| CQ.3 (Locked reagent feedback) | `locked_reagent_removal_blocked` |
| CQ.4 (PuzzleScreen decomposition) | Covered by all existing PuzzleScreen tests |
| GF.1–GF.4 (GameFlowController) | `game_flow_starts_at_menu`, `game_flow_first_launch_tutorial` |
| VS.1 (Sharp/Fragile promotion) | Indirectly via `generated_puzzle` + 616 unit tests |
| VS.3 (ReagentCatalog wiring) | Indirectly via `generated_puzzle` (solution validates) |

### Core-only features (no integration test needed yet)
| Feature | Reason |
|---------|--------|
| VS.2 (ReagentCatalog) | Pure data lookup, fully unit-tested |
| VS.4 (ActionHistory) | Game-layer wiring (VS.5) not yet built |
| VS.6 (VisualStateResolver) | Game-layer rendering (VS.7) not yet built |

### Missing coverage gaps
**None critical.** All Game-layer features have integration tests. Core-only features awaiting their Game-layer wiring are adequately covered by unit tests.

Minor gap: No integration test specifically verifies that generated puzzles use ReagentCatalog names (e.g., "Firevine Domino" instead of "reagent_0"). This is a cosmetic/naming concern with 4 unit tests in ReagentCatalogTests. Not worth adding a harness test for.

## Issues Found
**0 issues.** No test failures, no new visual regressions, no missing critical coverage.

## Overall Assessment
**CLEAN.** The codebase is in excellent shape:
- 616 unit tests + 30 integration tests all passing
- All Game-layer features since last review have integration coverage
- VS.4 and VS.6 are solid Core foundations awaiting their Game-layer wiring
- No visual regressions — all UI rendering matches expectations
- CQ.1–CQ.4 refactoring left no functional gaps
- GameFlowController screen transitions work correctly (overlap in screenshots is a test artifact)
