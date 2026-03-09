# Code Review — 2026-03-08
**Commits reviewed:** dde18aa..5650c92 (GO.1 + SA.1 + SA.2 + chore commits)

## Findings

### Dead Code — IsHeavyConstraintValid (P3, quick win → P2)
- **src/Core/Grid/MetaGrid.cs:439** — `IsHeavyConstraintValid` private method left behind after GO.1 removed its only call site in `CanPlaceReagent`.
  Before: Method defined but never called.
  After: Removed. `GetHeavyViolations` (the public API) remains for advisory violation reporting.

### No-op Fragile Constraint Check (P4, intentional stub)
- **src/Core/Grid/MetaGrid.cs:484-496** — `GetFragileViolations` always returns empty list, making `IsFragileConstraintValid` always true. The comment explains this is intentional: "Breaking conditions (crushing, thermal fracture, piercing) are evaluated as live warnings and at confirm time, not as placement blocks." Left as-is; will become meaningful when Fragile breaking conditions are implemented.

### SpriteAssigner.cs — 1070 lines (P4, acceptable for now)
- **src/Game/Tools/SpriteAssigner.cs** — New standalone UI tool combining layout, input, shape detection, metadata, preview, and export. Large file, but:
  - It's a standalone tool in `src/Game/Tools/`, not Core.
  - First implementation — Rule of Three says don't abstract yet.
  - Clear section organization with comment headers.
  - No architectural concerns (Godot deps appropriate for Game layer).
  - `SanitizeFilename` delegates to `SanitizeId` (trivial, not worth flagging).

### Test Updates (Clean)
- **MetaGridTests.cs** — Tests properly updated to reflect advisory Heavy constraint. Tests verify both that placement succeeds AND that violations are still reported. Good coverage pattern.

## Actions Taken
- Removed dead `IsHeavyConstraintValid` method from MetaGrid.cs (committed as `5d48583`).
- No prd.json tasks created — no P0-P1 issues found, and no P2 pattern with 3+ violations.

## Desloppify
- desloppify tool not available in this environment.
- Manual scan performed instead.

## Status
ISSUES_FIXED
