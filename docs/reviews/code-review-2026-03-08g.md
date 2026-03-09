# Code Review — 2026-03-08g
**Commits reviewed:** de3d91a..5adedea (LO.6, LO.7, LO.8 + bookkeeping)

## Summary
Three feature commits adding ReagentProfilePanel popup, wiring it into WebDemoScreen, and responsive layout adaptation. Code is clean — no P0-P2 issues.

## Findings

### Self-Documenting Code — Magic Number (P3)
- **WebDemoScreen.cs:454, 483** — `index * 90` used to estimate tray item pixel position for popup anchoring.
  Before: `trayRect.Position.X + index * 90`
  Suggested: Extract to a constant if a third usage appears (Rule of Three — only 2 occurrences now).

### Cognitive Load — Long Method (P3)
- **ReagentProfilePanel.cs:208-293** — `BuildAspectBreakdown` is ~85 lines of sequential UI construction. Readable and linear, but on the longer side. Not worth splitting for a single-use method.

### Separation of Concerns — Dual Escape Handling (P3)
- **ReagentProfilePanel.cs:166-170** handles Escape in `_UnhandledInput`; **WebDemoScreen.cs:721-725** also handles Escape for the profile in `_UnhandledKeyInput`. In Godot, `_UnhandledKeyInput` fires first, so WebDemoScreen's handler effectively shadows the panel's. The panel's own handler provides standalone reusability — this is intentional design, not a defect.

### Self-Documenting Code — Layout Breakpoints (P4)
- **WebDemoScreen.cs:376, 385** — Viewport breakpoints `1280` and `800` are hardcoded. Common practice for responsive layouts. Would benefit from named constants only if the code grows more complex.

## Positive Observations
- Core purity maintained — all changes in `src/Game/`, zero Godot imports in Core.
- `PlacedReagentClicked` signal follows the existing naming convention.
- `ReagentProfilePanel` is self-contained and reusable (handles its own dismissal).
- `SetCompactMode` on `AspectLegendPanel`/`AspectLegendEntry` uses clean early-return guards.
- `AdaptLayout` correctly uses `_Notification(NotificationResized)`.
- No dead code, no unused imports, no unnecessary abstractions.

## Actions Taken
- No direct fixes needed.
- No tasks added to prd.json.

## Desloppify
- Before: N/A (blocked by shell permission issue)
- After: N/A
- Top issue: N/A — desloppify scan blocked by shell permissions (known issue).

## Status
CLEAN
