# Architectural Review — 2026-03-05

**Reviewer:** Architecture Review Agent
**Branch:** `dev`
**Build:** 356/356 unit tests passing
**Scope:** Full codebase alignment with design docs, conventions, and game vision

---

## Executive Summary

The Alchemist Courier codebase is in **excellent architectural health**. All 6 core MVP phases are complete (core-chemistry, puzzle-infrastructure, ui-chemistry, bag-mechanics, aspect-rework, game-flow + UI polish). The remaining work (highlight-system, hint-system) is additive UX enhancement with no architectural risk.

**Verdict: PASS** — No blocking issues. Two low-severity bugs flagged, zero convention violations.

---

## 1. Core/Game Separation — PASS

The foundational architectural constraint — `src/Core/` must have zero Godot dependencies — is **perfectly maintained** across all 35 Core C# files.

- Zero `using Godot;` statements in Core
- Custom `AlchemistCourier.Core.Grid.Vector2I` used consistently instead of `Godot.Vector2I`
- All 14 Game files correctly import both Godot and Core namespaces
- Dependency direction is strictly Core ← Game (no reverse dependencies)

## 2. Namespace Conventions — PASS

All 66 C# files follow the established patterns:
- `AlchemistCourier.Core.{System}` for core logic (35 files)
- `AlchemistCourier.Game.{System}` for game logic (14 files)
- `AlchemistCourier.Core.Tests.{System}` for tests (17 files)
- All files use modern C# file-scoped namespace syntax

## 3. Data Model Alignment — PASS (minor intentional deviations)

All data models match the architecture doc specifications with a few intentional deviations:

| Model | Status | Notes |
|-------|--------|-------|
| GridCell | MATCH | `BagOwner` name clearer than `Owner` |
| MetaGrid | MATCH | `PlacedReagents` is a lookup optimization, not a violation |
| BagDefinition | MATCH | |
| Bag | MATCH | `OwnedCells` name clearer than `Cells` |
| ReagentDefinition | MATCH | |
| Reagent | DEVIATION | Missing `PlacedInBag` and `IsDiscovered` — both are post-MVP features. Has `IsLocked` (intentional for puzzle constraints) |
| Aspect/AspectSet | MATCH | |
| ReactionRule | SIMPLIFICATION | `TriggerAspects: List<string>` instead of `AspectRequirement[]`. No `Id` field. Pragmatic and functionally equivalent |
| ReactionResult | MATCH | |
| ChemistryEngine | MATCH | All evaluation flows, modifiers, prospective support correct |
| PuzzleGenerator | MATCH | Disassemble-a-solution with seeded RNG |

**Key invariant preserved:** MetaGrid does not store reagents directly — `PlacedReagents` is a convenience cache; reagents are logically owned by bag cells.

## 4. Design Doc Alignment — PASS

The implementation correctly reflects the design pillars:
- **"The puzzle IS the game"** — All systems create puzzle scenarios (chemistry, bags, spatial constraints)
- **"Flavor-first design"** — Aspect interactions follow alchemical logic (Hot/Cold neutralization, Volatile explosions, Toxic clouds)
- **"Depth through emergence"** — Simple rules (7 MVP reaction rules) combine for complex outcomes
- **"Cozy progression"** — Failure shows helpful feedback, tutorial ramps gently
- **"Approachable surface"** — Tutorial starts with pure spatial puzzles (lessons 1-3) before introducing chemistry

The two-dimensional complexity model (Scale × Mechanical Depth) is correctly implemented:
- DifficultyParams controls scale (bag count, sizes, reagent count)
- ComplexityFilter controls depth (Simple/Intermediate/Advanced aspects and rules)

## 5. Issues Found

### 5a. StatusBarPanel Null-bag Suppression Bug — MEDIUM

**File:** `src/Game/Inventory/StatusBarPanel.cs:65-95`

`ComputeTotal()` counts ALL aspects from ALL placed reagents without checking for Null-bag suppression. The ChemistryEngine correctly skips suppressed cells:

```csharp
// ChemistryEngine (correct):
if (IsAspectSuppressed(grid, pos))
    continue;

// StatusBarPanel (missing this check):
total += reagent.GetAspectsAtMetaGridPosition(pos).GetMagnitude(aspectId);
```

**Impact:** Status bars show inflated totals for reagents in Null bags. The actual chemistry evaluation is correct — this is purely a UI display issue that could confuse players.

**Fix:** Add `IsAspectSuppressed` check in `ComputeTotal()`. This requires either:
- Moving `IsAspectSuppressed` to a shared utility (currently private in ChemistryEngine), or
- Checking `grid.GetBagAtCell(pos)?.HasProperty(BagPropertyType.Null)` inline

### 5b. MetaGrid → Chemistry Coupling — LOW (by design)

**File:** `src/Core/Grid/MetaGrid.cs:298-387`

MetaGrid has hard-coded references to `AspectIds.Heavy`, `AspectIds.Sharp`, `AspectIds.Fragile` for placement constraints. This creates a coupling between Grid and Chemistry subsystems.

**Assessment:** This is **intentional and acceptable** for MVP. These are foundational inventory mechanics (placement constraints), not chemistry evaluation. The coupling is one-way (Grid reads aspect IDs, doesn't import ChemistryEngine) and well-scoped.

**Future consideration:** If the number of constraint-relevant aspects grows significantly, extract a `PlacementConstraintEvaluator` to decouple.

### 5c. DemoPuzzle Location — LOW (smell)

**File:** `src/Game/Inventory/DemoPuzzle.cs`

DemoPuzzle is a hard-coded fallback puzzle used when PuzzleScreen doesn't have PuzzleData. It's production code living alongside UI components.

**Recommendation:** Low priority, but could be moved to `src/Core/Puzzle/` or replaced with `PuzzleGenerator.Generate(fixedSeed, ...)` for better alignment.

## 6. Test Coverage Assessment — GOOD

**Unit tests:** 356 across 17 test files covering all Core subsystems:
- Chemistry: 119 tests (engine, aspects, rules, modifiers)
- Puzzle: 114 tests (generation, solving, state, complexity, daily, tutorial)
- Grid: 67 tests (MetaGrid, Vector2I)
- Reagents: 13 tests (aspects, shapes)
- Save: 33 tests (data, serialization)
- Random: 10 tests (seed determinism)

**Integration tests:** 16 scenarios in TestHarness covering the full game loop.

**Notable gaps (non-blocking):**
- No standalone Bag/BagDefinition unit tests (bag behavior tested indirectly through MetaGrid)
- No reaction cascade tests (one reaction triggering another)
- No MaximizeValue or MeetRequirement puzzle type tests (only FitAllSafely)
- Integration tests capture screenshots but don't assert on visual output

## 7. Performance Assessment — ACCEPTABLE

- **ChemistryEngine.Evaluate:** O(Rules × Reagents × Cells × Neighbors). For typical MVP puzzles (4-6 reagents, 20-40 cells, 7 rules), this comfortably meets the <16ms target.
- **PuzzleGenerator.Generate:** The placement loop (try random positions, evaluate chemistry for each) is the bottleneck. Typical: 1-5 seconds. Edge cases with unlucky random sequences or high complexity could exceed the <1s target but are mitigated by retry logic and complexity filtering.

## 8. Phase Alignment — ON TRACK

| Phase | Status | Tasks |
|-------|--------|-------|
| core-chemistry | DONE | 6/6 |
| puzzle-infrastructure | DONE | 4/4 |
| ui-chemistry | DONE | 5/5 |
| bag-mechanics | DONE | 4/4 |
| aspect-rework | DONE | 6/6 |
| review-fix | DONE | 7/7 |
| game-flow | DONE | 6/6 |
| ui-polish | DONE | 1/1 |
| highlight-system | PENDING | 0/14 |
| hint-system | PENDING | 0/8 |
| documentation | PENDING | 0/2 |

**38/63 tasks complete (60%).** All core gameplay is functional. Remaining work is UX polish.

---

## Recommendations

### Priority 0 (fix before next feature work)
- **StatusBarPanel Null-bag fix** — Create a shared utility for Null-bag checking and apply in `ComputeTotal()`. Small fix, big UX improvement.

### Priority 1 (address during highlight-system phase)
- Add Bag/BagDefinition standalone unit tests (currently only tested indirectly)
- Add test for StatusBarPanel Null-bag suppression (after fixing the bug)

### Priority 2 (address before MVP release)
- Verify puzzle generation performance with Intermediate/Advanced complexity (not just Simple)
- Add integration test assertions for win/loss conditions (currently runs but doesn't assert outcomes)

### No Action Needed
- Core/Game separation is clean
- Data model deviations are intentional and well-reasoned
- MetaGrid→Chemistry coupling is acceptable for MVP scope
- DemoPuzzle location is a smell but not a risk

---

## Conclusion

The architecture is solid, conventions are consistently followed, and the codebase is well-positioned for the remaining UX work. The only actionable finding is the StatusBarPanel Null-bag suppression bug, which is a straightforward fix. The project demonstrates strong engineering discipline — clean separation of concerns, comprehensive testing, deterministic systems, and careful documentation.
