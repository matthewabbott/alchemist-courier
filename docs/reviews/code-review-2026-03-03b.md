# Code Review — 2026-03-03 (b)
**Commits reviewed:** baf76cd..ef7d606 (tasks 2.2, 2.3, 2.4)

## Findings

### DRY — `CreateAspect` duplicated 3 times (P2)
- **PuzzleGenerator.cs:296-305** — private static `CreateAspect(string id)` switch expression
- **PuzzleSolver.cs:133-142** — identical private static `CreateAspect(string id)` switch expression
- **PuzzleGeneratorTests.cs:287-296** — identical private static `CreateAspect(string id)` switch expression

All three are byte-for-byte identical switch expressions mapping string IDs ("hot", "cold", etc.) to `Aspect` factory calls. Meets the Rule of Three for extraction. Natural home: `Aspect.FromId(string id)` static method on the existing `Aspect` class.

### DRY — `ReconstructGrid`/`ReconstructReagents` duplicated in test (P3)
- **PuzzleSolver.cs:80-104** — `internal static ReconstructGrid(PuzzleState)`
- **PuzzleSolver.cs:109-131** — `internal static ReconstructReagents(PuzzleState)`
- **PuzzleGeneratorTests.cs:240-285** — local copies of both methods

PuzzleSolver already has these as `internal static`, but the test project can't access them across the assembly boundary. PuzzleSolverTests doesn't duplicate them (it calls PuzzleSolver directly). Two occurrences — below Rule of Three for extraction. Could be resolved by making the methods `public` or adding `InternalsVisibleTo`, but not urgent.

### DRY — `ToShapeMatrix` duplicated in PuzzleBag and PuzzleReagent (P3)
- **PuzzleState.cs:55-67** — `PuzzleBag.ToShapeMatrix()`
- **PuzzleState.cs:118-130** — `PuzzleReagent.ToShapeMatrix()`

Identical loop converting jagged `bool[][]` to `bool[,]`. Two occurrences — below Rule of Three. These classes share no common base and serve different domain roles, so extracting a shared utility is not yet warranted.

### Self-Documenting Code — magic probability constant (P3)
- **PuzzleGenerator.cs:270** — `0.7f` hardcoded probability for whether a cell receives an aspect

Could be a named constant like `AspectProbability`. Minor readability issue.

### Redundant code — duplicate difficulty check (P4)
- **PuzzleGenerator.cs:70** — Special-cases `BagCount == 1 && MaxAspectCount == 0` before calling `InferDifficulty`, but `InferDifficulty` at line 423 already handles this exact case as its first match arm.

Harmless but redundant.

## Actions Taken
- Created task R-2 in `prd.json`: Extract `CreateAspect` to `Aspect.FromId(string)` to resolve 3x duplication.
- No direct code fixes — all findings are P2 or below and none warrant immediate changes.

## Status
ISSUES_FOUND
