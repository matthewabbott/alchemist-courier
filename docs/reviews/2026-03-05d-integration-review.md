# Integration Review — 2026-03-05 (d)

## Scope
Commits: fdc1e46..ed745e4 (H.13 ghost constraint composition, H.14 visual polish design doc, new integration tests)

## Build
- **dotnet build**: PASS (0 errors, 0 warnings besides CS8785 cosmetic)
- **dotnet test**: PASS — 466/466 unit tests

## Integration Test Harness
- **Result**: 22/22 tests pass, 0 failures
- **Screenshots**: 30 screenshots verified visually, no regressions

### New Tests Added
- `eagle_sense_orchestration` — Verifies ToggleEagleSense() flow: starts off, toggle on renders interaction highlights with dim overlay, toggle off clears highlights and dim. Uses Blast Jelly + Fire Pebble adjacent placement for Hot↔Volatile interactions.
- `legend_grid_linking` — Verifies legend-to-grid click orchestration (H.12): EmitSignal AspectSelected("hot") → grid shows legend highlights on Hot cells, second click clears. Verifies LegendSelectedAspect state tracking.

### Full Test List (22 tests)
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
16. highlight_overlay — HighlightOverlay direct rendering
17. highlight_integration — constraint + interaction highlights via MetaGridView
18. rejection_tooltip — invalid click tooltip display
19. item_detail_panel — reagent inspection panel
20. eagle_sense_orchestration — full PuzzleScreen eagle sense toggle (NEW)
21. legend_grid_linking — legend click → grid highlight toggle (NEW)
22. render_final — clean exit state

## Visual Verification (30 screenshots)
All screenshots inspected. No new visual regressions.

### Pre-existing cosmetic issues (unchanged)
1. **Tutorial win overlay vertical text** (screenshot 18) — "All reagents placed safely!" renders character-by-character vertically, overlapping the lesson panel. The "Next Lesson" button also overlaps the title bar.
2. **"Heat:" label left-edge clipping** (screenshots 20-22, 29) — On wider grids (generated/daily puzzles), the "Heat:" label at bottom-left is partially clipped by the viewport edge.
3. **Status bar shows base thresholds** — Displays base 3/3 Toxicity even when Medicinal or Stable would raise the effective threshold. Cosmetic; actual game logic uses correct effective thresholds.

### New screenshots look correct
- **29_eagle_sense_on.png** — Eagle sense active with dim overlay, Blast Jelly + Fire Pebble visible with interaction highlight edges, status text shows "Eagle Sense active" message
- **30_legend_grid_link.png** — Fire Pebble placed, legend highlight showing Hot cells on grid

## Issues Found
- **0 P0/P1/P2 issues**

## Coverage Assessment
The highlight-system phase (H.1–H.14) now has comprehensive integration test coverage:
- H.1–H.4: Pure Core data types (covered by 466 unit tests)
- H.5: HighlightEngine constraints → `highlight_integration` test
- H.6: Interaction/legend/eagle highlights → `eagle_sense_orchestration`, `legend_grid_linking`
- H.7: HighlightOverlay renderer → `highlight_overlay` test
- H.8: MetaGridView integration → `highlight_integration` test
- H.9: Rejection tooltip → `rejection_tooltip` test
- H.10: Aspect legend panel → `aspect_legend` test
- H.11: Item detail panel → `item_detail_panel` test
- H.12: PuzzleScreen orchestration → `eagle_sense_orchestration`, `legend_grid_linking` (NEW)
- H.13: Ghost constraint composition → covered by unit tests (5 tests in HighlightEngineTests)
- H.14: Visual polish design doc → docs only, no test needed

### Remaining gap
- H.13 ghost constraint GhostOverlay layer rendering is tested at the unit level but not visually in the integration harness. The existing `highlight_integration` test covers constraint highlights on hover but doesn't differentiate Constraint vs GhostOverlay layers visually. Low priority — the feature is a visual refinement of existing constraint highlights.
