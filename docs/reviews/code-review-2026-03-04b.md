# Code Review — 2026-03-04 (b)
**Commits reviewed:** 18b9332..3e35cf1 (R-3 Null bag suppression, R-4 MvpAspects fix, R-5 Stable buffering normalization)

## Summary

Found a P1 logic bug in R-5's "normalization" and dead code left behind by R-3. The bug is currently masked by all affected rules having threshold=0, but the code is misleading and will break if pair rules ever get non-zero thresholds.

## Findings

### Cognitive Load — Degenerate Comparison (P1)
- **ChemistryEngine.cs:123-125** and **ChemistryEngine.cs:207-210** — Stable buffering in Adjacent and SameBag distinct-pair paths uses `if (effectiveThreshold > rule.Threshold)` where `effectiveThreshold = rule.Threshold + stableBuffer`. This simplifies algebraically to `if (stableBuffer > 0)`, making the `effectiveThreshold` variable dead code that creates a false impression of magnitude comparison.
  Before: `int effectiveThreshold = rule.Threshold + stableBuffer; if (effectiveThreshold > rule.Threshold) continue;`
  After: `if (stableBuffer > rule.Threshold) continue;`
  **Why**: The "normalization" in R-5 (3e35cf1) claimed to unify the pattern across all tiers, but the cumulative path compares `totalMagnitude >= effectiveThreshold` (correct) while the pair paths just check if effectiveThreshold differs from threshold (degenerate). The original `stableBuffer > rule.Threshold` was more honest and correct.

### Dead Code — Unused ReagentHasAspect Method (P3)
- **ChemistryEngine.cs:350-358** — R-3 migrated all callers from `ReagentHasAspect` to `ReagentHasUnsuppressedAspect` but left the original method in place. No remaining callers.
  Before: 9-line unused private method.
  After: Removed.

## Actions Taken
- Fixed P1: Reverted pair-path Stable buffering to `stableBuffer > rule.Threshold` (clear, correct)
- Fixed P3: Removed dead `ReagentHasAspect` method
- Verified: `dotnet build` succeeds, all 182 tests pass
- Committed: `review: Fix degenerate Stable buffering check and remove dead code` (7bfa0b4)

## Status
ISSUES_FIXED
