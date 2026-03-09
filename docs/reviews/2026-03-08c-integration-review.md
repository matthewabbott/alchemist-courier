# Integration Review — 2026-03-08c

**Reviewed commits:** 0a9ff10..e2f8961 (GO.7–GO.12, LO.1–LO.4, SB.2)
**Build:** PASS (806/806 unit tests)
**Harness:** 40/40 tests pass, 0 failures (after fixes)
**Visual:** 48 screenshots verified

## Scope

Since last integration review (2026-03-08b), these features were implemented:

- **GO.7** (D.3): Fragile damage visual indicator — DamageOverlay with red tint per violation type
- **GO.8** (E.1): Volatile explosion indicator — extended DamageOverlay with orange tint
- **GO.9** (F.1): Error highlight pulse on confirm — PulseViolations animation before failure overlay
- **GO.10** (J.1): Bag state visual indicators — BagStateOverlay with animated thermal tint
- **GO.11** (J.3 UI): ViolationListPanel — live violation list in WebDemoScreen sidebar
- **GO.12** (J.4): Violation click highlights — clicking violations highlights affected cells/reagents
- **LO.1**: Public panel accessors on WebDemoScreen
- **LO.2**: Layout test infrastructure (LayoutTestHelper + WebDemoLayout test)
- **LO.3**: Baseline responsive viewport tests (1280x720, 1024x768, 800x600)
- **LO.4**: Move reagent tray from sidebar to bottom horizontal strip
- **SB.2**: Sandbox playground mode with catalog picker + interactive grid

## Issues Found: 1 (P0: 0, P1: 0, P2: 1)

### [P2] Test infrastructure: WebDemo tests didn't hide PuzzleScreen
- **Tests affected:** Test_WebDemoLayout, Test_WebDemoResponsive
- **Symptom:** Screenshots showed double button bars and status bars (PuzzleScreen bleeding through)
- **Root cause:** Missing `_puzzleScreen.Visible = false` before creating WebDemoScreen
- **Fix:** Added hide/restore in both tests (same pattern as editor tests)
- **Status:** Fixed

## New Tests Added: 1

### Test_ViolationListPanel
- Creates a controlled puzzle with Hot + Volatile reagents
- Verifies violations_before=0 on empty grid
- Places Hot adjacent to Volatile → verifies violations_after>0 (Explosion + heat imbalance)
- Removes both → verifies violations_cleared=0
- Covers GO.11 (ViolationListPanel rendering) integration

## Coverage Summary

| Feature | Unit Tests | Integration Test | Notes |
|---------|-----------|-----------------|-------|
| GO.7 (Fragile damage) | — | Test_FragileDamageOverlay | |
| GO.8 (Volatile explosion) | — | Test_VolatileExplosionOverlay | |
| GO.9 (Confirm pulse) | — | Indirect via confirm tests | Animation timing not testable |
| GO.10 (Bag state) | — | Test_BagStateOverlay | |
| GO.11 (ViolationListPanel) | — | Test_ViolationListPanel | **NEW** |
| GO.12 (Violation click) | 4 unit tests | Indirect via ViolationListPanel | Click→highlight is Game-layer wiring |
| LO.1 (Public accessors) | — | Used by layout tests | |
| LO.2 (Layout infra) | — | Test_WebDemoLayout | |
| LO.3 (Responsive) | — | Test_WebDemoResponsive | |
| LO.4 (Horizontal tray) | — | Verified via layout tests | |
| SB.2 (Sandbox playground) | — | No integration test | Tool, not gameplay |

## Visual Verification

Screenshots inspected:
- **01_render_initial.png**: Clean PuzzleScreen, 2 bags, aspect legend, status bar. ✓
- **17_generated_puzzle.png**: All reagents placed safely, "Puzzle complete!" message. ✓
- **41_gravity_bag_arrow.png**: Gravity bag with arrow indicator, Heat 1/4. ✓
- **42_fragile_damage_overlay.png**: Sharp+Fragile adjacent, damage overlay visible (pink tint). ✓
- **43_volatile_explosion_overlay.png**: Hot+Volatile adjacent, orange explosion overlay. ✓
- **44_bag_state_overlay.png**: Hot reagent in bag, red thermal tint on 9 bag cells. ✓
- **45_bag_movement.png**: Bag moved+rotated, reagent carried correctly. ✓
- **46_web_demo_layout.png**: Clean WebDemoScreen — no bleed-through after fix. ✓
- **47_responsive_*.png**: Grid and tray visible at all 3 resolutions. ✓
- **48_violation_list_panel.png**: "Issues (2)" header with violation entries. ✓

## Pre-existing Cosmetic Issues (unchanged)
- Heat label left-edge clipping on wide grids
- Status bar shows base thresholds, not effective (Medicinal/Stable adjusted)
