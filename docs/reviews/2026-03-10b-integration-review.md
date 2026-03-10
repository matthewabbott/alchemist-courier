# Integration Review — 2026-03-10b

## Scope
Reviewed commits since last integration review (2026-03-10): SI.9, VP.1–VP.4, R1 (shared helper extraction), code reviews (VP.1–VP.3, deslop).

## Build & Unit Tests
- **Build:** PASS (0 errors, 1 known CS8785 warning)
- **Unit tests:** 812/812 pass

## Integration Test Harness
- **Result:** 44/44 tests pass, 0 failures
- **New tests added:** 2
  - `aspect_tiles_on_grid` (VP.4) — verifies per-cell aspect tile TextureRects created on placement (0→1→3→2 lifecycle)
  - `cell_mount_overlay` (GJ.7) — verifies CellMountOverlay RenderedCount on placement (0→1→7→0 lifecycle)
- **New accessor added:** `MetaGridView.AspectTileCount` public property for test observability

## Visual Verification (49 screenshots inspected)
All screenshots render correctly. Key observations:

### Recent Features Working
- **VP.1 (Parchment background):** Tiled parchment visible in WebDemoScreen with warm dark modulate.
- **VP.2 (Tray texture tiling):** Leather tray background tiles naturally in WebDemoScreen.
- **VP.3 (Aspect tile 32x32 scaling):** All aspect tiles render at native 32x32 in legend, status bar, and reagent detail panels. Crisp pixel-art filtering.
- **VP.4 (Per-cell aspect tiles):** Individual cells render their own aspect tile sprites. Monominos show single tiles; dominos show two tiles.
- **GJ.7 (Cell mount overlay):** Monominos get full ornate texture overlay; multi-cell reagents get colored perimeter edges. Internal edges correctly hidden.
- **GJ.8 (Bag border NinePatchRect):** Celtic stone knotwork frames visible on all bags. Tiling works at various bag sizes.
- **SI.9 (UI sprite integration):** 24/24 textures found, 0 missing. Fallback paths verified.
- **R1 (Shared helpers):** No visual regression from deduplication refactor.

### Known Cosmetic Issues (Pre-existing, Unchanged)
1. Left-edge label clipping on generated puzzle left margin.
2. Tutorial vertical text from result overlay width collapse.
3. Status bar shows base thresholds, not effective (Medicinal/Stable adjusted).

## Coverage Assessment
| Feature | Coverage | Notes |
|---------|----------|-------|
| GJ.7 (CellMountOverlay) | **NEW** | `cell_mount_overlay` test covers place/remove lifecycle |
| GJ.8 (Bag borders) | Partial | SI.9 counts NinePatchRects; visual inspection confirms tiling |
| SI.9 (UI integration) | Full | Dedicated test with 24-texture audit |
| VP.1 (Parchment bg) | Indirect | web_demo_layout + visual inspection |
| VP.2 (Tray tiling) | Indirect | web_demo_layout + visual inspection |
| VP.3 (Tile scaling) | Indirect | SI.9 legend tile count + visual inspection |
| VP.4 (Per-cell tiles) | **NEW** | `aspect_tiles_on_grid` test covers placement/removal counts |
| R1 (Helper extraction) | Full | Pure refactor — all existing tests pass |

## Issues Found
**0 issues** (P0: 0, P1: 0, P2: 0)

## Summary
All visual polish features (VP.1–VP.4), sprite integration (SI.9), and code quality improvements (R1) are functioning correctly end-to-end. Two new integration tests added for VP.4 and GJ.7. No regressions detected. Total harness coverage: 44 tests.
