# Code Review — 2026-03-10d
**Commits reviewed:** d134000..5f5d304 (VP.6, VP.7, VP.8)

## Findings

### Fail-Fast — QueueFree race condition (P1)
- **InfoColumnManager.cs:487-492** — `RebuildAspectIcon` calls `QueueFree()` then immediately creates and adds a new icon child. `QueueFree` defers removal to the next idle frame, so both old and new icons briefly coexist in the tree, causing potential layout flickering.
  Before: `_adIcon.QueueFree();`
  After: `_adIconCenter.RemoveChild(_adIcon); _adIcon.Free();`

### Fail-Fast — Timer callback on potentially freed node (P1)
- **AspectLegendPanel.cs:384-385** — `Flash()` creates a 0.4s timer whose callback captures `_headerPanel`. If the entry is freed before timeout, the callback would operate on a freed object.
  Before: `timer.Timeout += () => _headerPanel.AddThemeStyleboxOverride(...);`
  After: Wrapped with `if (IsInstanceValid(_headerPanel))` guard.

### DRY — Aspect aggregation logic duplicated 4x (P2)
- **InfoColumnManager.cs:325-335**, **ItemDetailPanel.cs:151-167**, **ReagentProfilePanel.cs (BuildAspectBreakdown)**, **FloatingDetailTooltip.cs:137-153** — All four files contain identical aspect aggregation: iterate `GetLocalCellPositions()`, call `GetAspectsAtLocalPosition`, accumulate into `Dictionary<string, (Aspect, int)>`. Should be extracted to a shared helper.

### DRY — ShapePreview _Draw duplicated 2x (P3, noted only)
- **ItemDetailPanel.cs:216** (`DetailShapePreview`) and **ReagentProfilePanel.cs:312** (`ProfileShapePreview`) — Functionally identical `_Draw` implementations, differing only in `CellSize` constant (16 vs 24). Only 2 occurrences, so below the Rule of Three threshold.

### Robustness — Initialize not idempotent (P3, noted only)
- **AspectLegendPanel.SetPuzzleContext**, **ObjectivePanel.Initialize**, **StatusBarPanel.Initialize**, **ViolationListPanel.Initialize** — Calling these methods twice would create duplicate child nodes. In practice they are only called once during puzzle setup, so this is a latent issue rather than an active bug.

### Self-Documenting Code — StyleBoxFlat allocation churn (P3, noted only)
- **ViolationListPanel.cs:107** — `BuildEntryRow` creates 3 new `StyleBoxFlat` objects per violation per `Refresh()` call. Minor GC pressure. Could pre-cache per-severity styles, but not impactful for MVP.

## Actions Taken
- **Fixed** P1 QueueFree race in `InfoColumnManager.cs` (commit 833daba)
- **Fixed** P1 timer callback safety in `AspectLegendPanel.cs` (commit 833daba)
- **Created** task R2 in `prd.json` for P2 DRY aspect aggregation extraction

## Desloppify
- Before: N/A (desloppify not available as CLI tool)
- After: N/A
- Top issue: DRY aspect aggregation (4 copies) — tracked as R2

## Status
ISSUES_FIXED
