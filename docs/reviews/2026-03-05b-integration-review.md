# Integration Review — 2026-03-05 (b)

## Scope
Commits: 3f4e36f..354e7af (H.5, H.6, H.7, H.8, code-review-c DRY fix, H.9)

## Build & Unit Tests
- **Build**: PASS (0 errors, 1 cosmetic CS8785 warning on test .csproj)
- **Unit tests**: 461/461 PASS

## Integration Test Harness
- **Result**: 19/19 PASS, 0 failures
- **Screenshots**: 25 total (5 new: highlight_overlay, highlight_overlay_eagle, highlight_constraint, highlight_interaction, rejection_tooltip)

## New Tests Verified

### highlight_overlay (H.7)
- 3 programmatic highlights → 4 rendered visual elements (2 edges for top+right, 1 edge for left, 1 fill) ✓
- Eagle sense dim overlay toggles correctly ✓
- Clear() removes all visuals ✓

### highlight_integration (H.8)
- Sharp reagent on non-Reinforced bag edge → 10 constraint highlights, warning_level=Invalid ✓
- Hot reagent adjacent to Volatile → 4 interaction highlights, warning_level=Danger ✓
- Stable-only reagent hovering in empty area → 0 highlights ✓

### rejection_tooltip (H.9)
- Tooltip starts hidden ✓
- Sharp on non-Reinforced edge → SimulateLeftClick → tooltip visible with reason "Sharp reagent on edge cell of non-Reinforced bag" ✓

## Visual Verification (25 screenshots)

### New screenshots (H.5–H.9)
- **21_highlight_overlay.png**: Colored edge lines (red top+right, orange left) and blue fill rectangle render correctly on grid. No eagle sense dim. ✓
- **22_highlight_overlay_eagle.png**: Same highlights with dark dim overlay behind entire grid. Placed reagent colors appear muted through dim. ✓
- **23_highlight_constraint.png**: Poison Fang (Sharp) selected, hovering at SmallPouch edge. Red/pink constraint highlight fills on affected cells. Ghost preview visible. ✓
- **24_highlight_interaction.png**: Fire Pebble (Hot) selected, hovering adjacent to Blast Jelly (Volatile). Interaction highlights visible on neighbor cells. Status bar shows Heat 1/4. ✓
- **25_rejection_tooltip.png**: Dark-background tooltip positioned near cursor showing "Sharp reagent on edge cell of non-Reinforced bag". Constraint highlights visible on affected cells. ✓

### Pre-existing screenshots (regression check)
All 20 pre-existing screenshots verified — no regressions. DemoPuzzle layout, placement flow, status bar, warnings, primer panel, confirm/reset, win/loss, generated puzzle, tutorial, and daily puzzle all rendering correctly.

## Issues Found: 0

No new issues. All highlight system features (H.5–H.9) are working correctly end-to-end.

## Pre-existing Cosmetic Issues (unchanged)
1. **Tutorial win overlay vertical text** (screenshot 18): "All reagents placed safely!" renders vertically, obscuring game view. Pre-existing since 4.4.
2. **"Heat:" label left-edge clipping** (screenshots 15, 16, 17, 20): When grid is wide (generated/daily puzzles), left-side UI content clips off-screen.
3. **Status bar base thresholds**: Shows base thresholds (e.g., 3/3 Toxicity) rather than effective thresholds accounting for Medicinal/Stable buffers.

## Coverage Assessment
- H.5 (HighlightEngine constraints): Covered via highlight_integration test + 19 unit tests
- H.6 (HighlightEngine interactions/legend/eagle): Interaction covered via highlight_integration; legend/eagle sense tested in unit tests (16 tests); eagle sense overlay tested in highlight_overlay test
- H.7 (HighlightOverlay renderer): Dedicated integration test with edge/fill/eagle sense/clear
- H.8 (MetaGridView wiring): Dedicated integration test with 3 scenarios (constraint, interaction, safe)
- H.9 (Rejection tooltip): Dedicated integration test with tooltip visibility + reason text

No critical coverage gaps. Minor untested paths (legend highlights through MetaGridView, tooltip auto-hide timing, RejectionFlash signal) are either not yet wired to game features (H.10) or behavioral details well-covered by unit tests.
