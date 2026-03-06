# Integration Review — 2026-03-05 (e)

## Scope
Commits: 851ec25..HEAD (HINT.1–HINT.6: hint data types, engine, generator integration, PuzzleScreen orchestration, visual indicators, save tracking)

## Build
- **dotnet build**: PASS (0 errors, 0 warnings besides CS8785 cosmetic)
- **dotnet test**: PASS — 497/497 unit tests

## Integration Test Harness
- **Result**: 23/23 tests pass, 0 failures
- **Screenshots**: 32 screenshots verified visually, no regressions

### New Tests Added
- `hint_system` — End-to-end hint flow: verifies MaxHints=3 budget from Easy difficulty, UseHint() places a reagent at its solution position, budget increments to 1/3, reagent has IsHinted=true, removal and snap-back re-placement is free (budget stays at 1/3).

### Full Test List (23 tests)
1. render_initial — grid layout, bag count
2. objective_panel — text display, type switching
3. aspect_legend — expand/collapse, highlight, toggle visibility
4. place_fire_pebble — basic placement
5. place_ice_shard_rotated — rotation + placement
6. invalid_placement — out-of-bounds rejection
7. place_poison_fang — multi-cell placement
8. remove_reagent — right-click removal
9. placement_warnings — Danger/Warning/Safe levels
10. status_bar — cumulative aspect tracking
11. confirm_and_reset — button flow, state cleanup
12. win_loss_evaluation — ChemistryEngine confirm flow
13. generated_puzzle — PuzzleGenerator → PuzzleScreen pipeline
14. tutorial_flow — lesson progression, hint text
15. daily_puzzle — DailyPuzzleManager pipeline
16. hint_system — hint budget, use, snap-back (NEW)
17. highlight_overlay — HighlightOverlay direct rendering
18. highlight_integration — constraint + interaction highlights via MetaGridView
19. rejection_tooltip — invalid click tooltip display
20. item_detail_panel — reagent inspection panel
21. eagle_sense_orchestration — PuzzleScreen eagle sense toggle
22. legend_grid_linking — legend click → grid highlight toggle
23. render_final — clean exit state

## Visual Verification (32 screenshots)
All screenshots inspected. No new visual regressions.

### Pre-existing cosmetic issues (unchanged)
1. **Tutorial win overlay vertical text** (screenshot 18) — "All reagents placed safely!" renders character-by-character vertically, overlapping the lesson panel. The "Next Lesson" button also overlaps the title bar.
2. **"Heat:" label left-edge clipping** (screenshots 17, 20) — On wider grids (generated/daily puzzles), the "Heat:" label at bottom-left is partially clipped by the viewport edge.
3. **Status bar shows base thresholds** — Displays base 3/3 Toxicity even when Medicinal or Stable would raise the effective threshold. Cosmetic; actual game logic uses correct effective thresholds.

### New screenshots look correct
- **31_hint_used.png** — Reagent placed at solution position via hint, "Hint 1/3" counter visible, button enabled
- **32_hint_snapback.png** — Same reagent re-placed after removal (snap-back), budget still 1/3

## Issues Found
- **0 P0/P1/P2 issues**

## Coverage Assessment
The hint-system phase (HINT.1–HINT.6) now has integration test coverage:
- HINT.1: HintState/HintResult data types → covered by 4 unit tests + `hint_system` integration test
- HINT.2: HintEngine logic (auto-pick, dislodge, execute) → covered by 15 unit tests + `hint_system` integration test
- HINT.3: DifficultyParams.MaxHints → covered by 10 unit tests + `hint_system` verifies budget=3 from Easy
- HINT.4: PuzzleScreen hint orchestration → `hint_system` test validates UseHint(), budget, snap-back
- HINT.5: Hint visual indicators (grid borders + tray dots) → not directly asserted in test (would need pixel inspection), but screenshots 31/32 show the hint button with counter
- HINT.6: SaveData hint tracking → covered by 2 unit tests (pure data, no UI integration needed)

### Remaining gaps
- HINT.5 hint border rendering on grid and tray dot indicators are not programmatically verified in the integration test. The hint button counter is visible in screenshots, but the purple borders on hinted reagent cells and tray dots require pixel-level inspection to verify. Low priority — the feature is a visual polish enhancement.
