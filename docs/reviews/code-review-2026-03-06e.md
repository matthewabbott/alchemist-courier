# Code Review — 2026-03-06e
**Commits reviewed:** a9149e4..13b4c79 (VS.7, VS.8, plus MetaGridView sprite integration)

## Findings

### Encapsulation — Mutable Public Dictionaries (P1)
- **src/Core/Visual/SpriteSheetSpec.cs:12-15** — `StateRowMap` and `FrameCounts` exposed as `Dictionary<,>` (mutable). Since `ReagentSpriteSheet` caches specs via `GetOrLoad`, any caller mutating these dictionaries corrupts all shared instances.
  Before: `public Dictionary<VisualState, int> StateRowMap { get; }`
  After: `public IReadOnlyDictionary<VisualState, int> StateRowMap { get; }`

### Self-Documenting Code — No-op Identity Method (P3)
- **src/Game/Tools/DevSpriteGenerator.cs:140** — `ConvertColor(Godot.Color c) => c` is a no-op since both sides are `Godot.Color`. Adds confusion about type conversions that don't exist.
  Before: `var baseColor = ConvertColor(GridConstants.GetAspectColor(aspectId));`
  After: `var baseColor = GridConstants.GetAspectColor(aspectId);`

### DRY — Triplicated Border Methods (P2, Rule of Three met)
- **src/Game/Inventory/MetaGridView.cs:405-611** — Three near-identical border rect factory methods (`CreateBorderRect`, `CreateHintedBorderRect`, `CreateLockedBorderRect`) differ only in border width and color. Similarly, `CreateHintedBorders` and `CreateLockedBorders` are near-identical iteration loops.
  Fix: Extract `CreateBorderRectWithStyle(pixelPos, direction, width, color)` and `CreateReagentBorders(predicate, color, width)`.

### Single Source of Truth — Duplicated Spec Layout (P3)
- **src/Game/Tools/DevSpriteGenerator.cs:106-115** — The `sprite_specs.json` output hardcodes the same state→row mapping already encoded in `SpriteSheetSpec.DefaultReagentSpec()`. If the layout changes, two locations need updating.
  Note only — the JSON is a data artifact for external consumption (art pipeline), so some duplication is acceptable.

## Actions Taken
- **Fixed** P1: Changed `SpriteSheetSpec.StateRowMap` and `FrameCounts` to `IReadOnlyDictionary<,>` (commit 1cd8331)
- **Fixed** P3: Removed `ConvertColor` no-op method and inlined the direct call (commit 1cd8331)
- **Created task** R2 in prd.json: MetaGridView border method deduplication (P2)

## Desloppify
- Before: N/A (tool requires approval)
- After: N/A
- Top issue: N/A

## Status
ISSUES_FIXED
