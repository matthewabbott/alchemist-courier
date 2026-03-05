# Code Review — 2026-03-04 (c)
**Commits reviewed:** 8bcf6ce..e204a80 (B.2, B.3, and PRD/progress updates)

## Findings

### DRY — Duplicated HasHeavyAspect helper
- **MetaGrid.cs:337-345** and **PuzzleGenerator.cs:464-472** — identical logic iterating `reagent.Definition.AspectsPerCell` and calling `aspectSet.Has("heavy")`.
  Before (two locations): `ReagentHasHeavyAspect(reagent)` / `HasHeavyAspect(reagent)`
  After (if 3rd occurrence appears): Move to `Reagent.HasAspect(string id)` or `ReagentDefinition.HasAspect(string id)`.
  **Severity: P3** — Only 2 occurrences. Rule of Three says don't abstract yet. Note for future reference.

### Self-Documenting Code — Magic string "heavy"
- **MetaGrid.cs:341** and **PuzzleGenerator.cs:468** — The string literal `"heavy"` is used directly. The `Aspect` class has `Aspect.Heavy()` factory but no `const string` for the ID.
  **Severity: P3** — Consistent with existing codebase pattern (all aspect IDs are bare strings). Not worth changing in isolation.

### Separation of Concerns — Grid layer knows about aspects
- **MetaGrid.cs:289-332** — `IsHeavyConstraintValid` and `ReagentHasHeavyAspect` give the grid layer direct knowledge of the "heavy" aspect. The grid now has chemistry-specific logic.
  **Severity: P3** — Acceptable for MVP. The constraint is tightly tied to placement validation, which is MetaGrid's responsibility. Abstracting to a constraint plugin system would be YAGNI. If more aspect-based placement rules emerge (3+), consider extracting a `PlacementConstraint` interface.

## Positive Observations
- **Clean constraint separation**: Heavy validation lives in MetaGrid (where placement rules belong), while PuzzleGenerator only handles placement ordering. Good single-responsibility split.
- **Bidirectional checking**: `IsHeavyConstraintValid` checks both directions (Heavy looking down, non-Heavy looking up), making the constraint robust regardless of placement order.
- **Lazy HashSet in IsGravityValid**: Existing gravity check only allocates the HashSet when multi-cell self-support is needed. Efficient.
- **Thorough test coverage**: 8 new MetaGrid tests + 1 PuzzleGenerator test covering key scenarios (sinking, stacking, non-gravity passthrough, multi-cell).
- **Generator sorts Heavy-first**: Simple and effective approach — Heavy reagents placed first naturally land at the bottom without backtracking.
- **No Godot dependencies in Core**: Convention maintained across all changed files.

## Actions Taken
- No direct fixes needed (no P0/P1 issues found).
- No tasks added to prd.json.

## Status
CLEAN
