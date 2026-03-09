# Code Review — 2026-03-04 (d)
**Commits reviewed:** de4a45b..bb21228 (R-6, R-7, bookkeeping)

## Findings

### DRY / Self-Documenting Code — TestHarness string literals (P3)
- **src/Game/Testing/TestHarness.cs:247,253,259,265,272** — `GetAspectTotal("hot")` and `GetAspectTotal("toxic")` use raw string literals instead of `AspectIds.Hot` / `AspectIds.Toxic`. Missed during R-6 refactor.
  Before: `bar.GetAspectTotal("hot")`
  After: `bar.GetAspectTotal(AspectIds.Hot)`
  This is test harness code with relaxed standards; behavior is correct regardless.

### Review of R-7 (Reagent.HasAspect deduplication) — Clean
- `Reagent.HasAspect(string aspectId)` is well-placed, general-purpose, and properly tested (5 tests).
- Both call sites in `MetaGrid.IsHeavyConstraintValid` and `PuzzleGenerator.PlaceReagentsValidly` correctly use the new method with `AspectIds.Heavy`.
- Old private `ReagentHasHeavyAspect` / `HasHeavyAspect` methods cleanly removed.
- `ChemistryEngine.ReagentHasUnsuppressedAspect` is intentionally separate — it's grid-aware and handles Null bag suppression. No duplication.

### Verification of R-6 (AspectIds constants) — Thorough
- All non-comment string literals in `src/Core/` replaced with `AspectIds.*` constants.
- All non-comment string literals in `src/Game/` replaced except TestHarness (P3 above).
- Test files in `tests/` use raw strings as expected (different standards for test code).

## Actions Taken
- No direct fixes committed (no P0-P1 issues).
- No tasks added to prd.json (P3 finding doesn't meet Rule of Three threshold).

## Status
CLEAN
