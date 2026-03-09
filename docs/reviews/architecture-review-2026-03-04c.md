# Architecture Review — 2026-03-04c

**Reviewer:** Architecture Review Agent
**Branch:** `dev` (55 files changed, ~8,400 lines added since master)
**Scope:** Full codebase alignment check after completion of core-chemistry, puzzle-infrastructure, ui-chemistry, bag-mechanics, and aspect-rework phases

---

## Executive Summary

The codebase has matured significantly since the last architecture review. All core systems (Chemistry Engine, Puzzle Generator/Solver, Bag mechanics, Aspect rework) are implemented and tested. The Core/Game boundary remains pristine (zero Godot violations). 247 unit tests pass. The architecture faithfully implements the design documents with only minor drift.

**However**, two new structural concerns have emerged from the rapid development: a circular dependency between Chemistry and Puzzle subsystems, and MetaGrid reaching upward into Chemistry for placement constraints. These are not bugs but layering violations that will compound as the codebase grows.

**Critical issues:** 0
**Significant issues:** 2 (both new)
**Minor issues:** 5
**Carried forward from previous review:** 2 (S1 OrderPair, S3 AspectSet.Empty)

---

## Significant Issues

### S1. Circular dependency: Chemistry ↔ Puzzle

**Files:**
- `src/Core/Chemistry/ReactionRuleDatabase.cs:3` — `using AlchemistCourier.Core.Puzzle;`
- `src/Core/Puzzle/ComplexityFilter.cs:4` — `using AlchemistCourier.Core.Chemistry;`

**Call chain:**
```
ReactionRuleDatabase.GetRulesForComplexity(level)
  → ComplexityFilter.GetRules(level)
    → ReactionRuleDatabase.GetMvpRules()   // calls back into Chemistry
```

This is a circular static call chain between two subsystems. It works at runtime (no infinite recursion — `GetRulesForComplexity` calls `ComplexityFilter` which calls `GetMvpRules`, not `GetRulesForComplexity`), but it violates the intended layering: Puzzle should depend on Chemistry, not the reverse.

**Why this matters:**
- Cannot compile Chemistry independently of Puzzle
- Cannot test Chemistry rules without Puzzle types on the classpath
- If `ComplexityFilter` needs more Chemistry internals, the coupling deepens
- Confuses the dependency graph for new contributors

**Recommendation:** Break the cycle by inverting the dependency direction:
1. Remove `GetRulesForComplexity` from `ReactionRuleDatabase`
2. Make `ComplexityFilter.GetRules(ComplexityLevel, List<ReactionRule>)` accept rules as a parameter
3. Callers (PuzzleGenerator) pass `ReactionRuleDatabase.GetMvpRules()` into `ComplexityFilter`

This is a small refactor (~10 lines changed) with significant structural benefit.

### S2. MetaGrid depends on Chemistry for placement constraints

**File:** `src/Core/Grid/MetaGrid.cs:4` — `using AlchemistCourier.Core.Chemistry;`

MetaGrid imports `AspectIds` to implement three aspect-aware placement constraints:
- `IsSharpConstraintValid` (line 349) — checks `AspectIds.Sharp`
- `IsHeavyConstraintValid` (line 298) — checks `AspectIds.Heavy`
- `IsFragileConstraintValid` (line 374) — checks `AspectIds.Fragile`

This creates an upward dependency: Grid (infrastructure) → Chemistry (domain logic). The Grid subsystem should be a spatial primitive that higher-level systems build on. Instead, MetaGrid now has baked-in knowledge of specific aspects.

**Why this matters:**
- Adding a new aspect with a placement constraint (e.g., Magnetic repulsion) requires modifying MetaGrid
- MetaGrid's concerns have expanded from "spatial placement" to "aspect-aware placement"
- Tests for placement constraints require Chemistry types

**Recommendation:** Extract constraint validation to a pluggable system:
```csharp
// Option A: Strategy pattern
public class PlacementValidator
{
    public bool CanPlace(MetaGrid grid, Reagent reagent, Vector2I position)
    {
        // Delegates to registered constraint checks
    }
}

// Option B: Simpler — keep in MetaGrid but inject constraints
public bool CanPlaceReagent(Reagent reagent, Vector2I position,
    IEnumerable<IPlacementConstraint>? constraints = null)
```

**Priority:** Medium. Not blocking MVP, but should be addressed before adding more aspect constraints. The current code works correctly.

---

## Minor Issues

### M1. Locked reagent placement data is implicit in PuzzleState

**File:** `src/Core/Puzzle/PuzzleGenerator.cs:220-223`

```csharp
state.Solution = placements;
LockSolutionReagents(rng, difficulty, state);
state.Placements = new List<Placement>();  // empty!
```

Locked reagents are marked via `PuzzleReagent.IsLocked = true`, but their placement positions only exist in `state.Solution`, not in `state.Placements`. Any code that reconstructs the puzzle for the player must:
1. Check each reagent's `IsLocked` flag
2. Find its placement in `Solution` (not `Placements`)
3. Pre-place it on the grid

This is not documented and will be a trap for the PuzzleScreen integration (task 4.2).

**Recommendation:** Either pre-populate `state.Placements` with locked reagent placements (making them explicit), or add a `GetLockedPlacements()` helper method to PuzzleState.

### M2. Insulated bag type exists but has no ChemistryEngine support

`BagPropertyType.Insulated` is in the enum and `ComplexityFilter` classifies it as Intermediate tier, but:
- `ChemistryEngine` has no Insulated logic (only Null bag suppression)
- `PuzzleGenerator.AssignBagProperties` doesn't generate Insulated bags
- `docs/chemistry-system.md` specifies: "Insulated bag raises all thresholds for reagents within"

Not blocking (Intermediate tier, not needed for Simple-complexity MVP puzzles), but the type exists and could be mistakenly used with no effect.

### M3. `ComplexityFilter.AdvancedAspects` uses raw strings

**File:** `src/Core/Puzzle/ComplexityFilter.cs:26-29`

```csharp
private static readonly string[] AdvancedAspects =
{
    "magnetic", "absorbent",  // raw strings, not AspectIds constants
};
```

After R-6 extracted all aspect IDs to `AspectIds`, these two remain as raw strings because they don't have constants in `AspectIds` yet (they're unimplemented Advanced-tier aspects). If/when Magnetic and Absorbent are implemented, these should be added to `AspectIds`.

### M4. PuzzleGenerator hardcodes only FitAllSafely puzzle type

**File:** `src/Core/Puzzle/PuzzleGenerator.cs:131`

```csharp
result.PuzzleType = PuzzleType.FitAllSafely;
```

The MVP spec defines three puzzle types, but only `FitAllSafely` is generated. This is expected at this stage (it's the foundational type), but `MaximizeValue` and `MeetRequirement` will need generator support for variety. Noted for future planning, not actionable now.

### M5. Non-rectangular bag shapes are hardcoded in PuzzleGenerator

**File:** `src/Core/Puzzle/PuzzleGenerator.cs:24-79`

Bag shapes and reagent shapes are embedded as static arrays within PuzzleGenerator. The architecture doc envisions shapes coming from resource definitions, but for MVP this is fine. Should be externalized to data files before adding more shapes (the array is already at 8 bag shapes and 10 reagent shapes).

---

## Carried Forward (From architecture-review-2026-03-04b)

### CF1. `OrderPair` uses `GetHashCode()` — S1 from previous review

Still present at `ChemistryEngine.cs:471-474`. Works correctly for deduplication (reference equality in tuple hash set), but ordering is non-deterministic across runtimes. Low priority.

**Status:** Unchanged. Recommend fixing when reagent IDs are added.

### CF2. `AspectSet.Empty` allocates on every access — S3 from previous review

Still present at `AspectSet.cs:65`. The property `public static AspectSet Empty => new();` creates a new mutable instance each call. Not a correctness bug (each caller gets its own instance), but misleading naming and wasteful allocation in loops.

**Status:** Unchanged. Recommend renaming to `CreateEmpty()` or making immutable.

---

## Observations (Positive — No Action Required)

### O1. Core/Game boundary remains exemplary

Zero `using Godot;` in any of the 30 Core files. The custom `Vector2I` continues to enforce this boundary. `DemoPuzzle.cs` in Game/ avoids Godot imports despite being in the Game project. This is the strongest architectural invariant and it's holding perfectly.

### O2. Stable/Catalytic/Medicinal buffering is consistent

All three modifier tiers use the same `effectiveThreshold` formula:
```
effectiveThreshold = Max(0, rule.Threshold + stableBuffer + medicinalBuffer - catalyticReduction)
```
- Pair reactions (threshold=0): `effectiveThreshold > 0` → binary suppression (semantically correct — pairs either trigger or don't)
- Cumulative reactions (threshold>0): `magnitude >= effectiveThreshold` → additive (correct — magnitude is compared to raised/lowered threshold)
- Medicinal only applies to WholeInventory rules (correct per docs)

This is architecturally clean and matches the chemistry-system.md specification exactly.

### O3. Volatile net-hot implementation is correct and complete

The net-hot formula at `ChemistryEngine.cs:155-218` correctly:
- Excludes same-reagent cells from neighbor counting (line 181)
- Sums Hot magnitudes and subtracts Cold magnitudes (lines 185-194)
- Applies Stable buffering and Catalytic reduction (lines 204-206)
- Respects Null bag suppression (lines 163, 177)

This is the signature chemistry mechanic and it's faithfully implemented.

### O4. Test coverage is comprehensive and well-structured

247 unit tests across 10 test files, with the heaviest coverage on ChemistryEngine (1,808 lines of tests). The test suite covers boundary cases (zero-threshold, exact-threshold, above-threshold), modifier interactions (Stable+Catalytic cancellation), and constraint combinations. This is appropriate investment for the most complex subsystem.

### O5. Previous review recommendations R-6 and R-7 were completed

- R-6 (AspectIds constants class): Implemented. All aspect ID string literals in Core/ now use `AspectIds.Hot`, `AspectIds.Cold`, etc.
- R-7 (Deduplicate ReagentHasHeavyAspect): Implemented. `Reagent.HasAspect(string)` replaces both copies.

---

## Alignment with Game Vision

### Design Pillar Check

| Pillar | Status | Notes |
|--------|--------|-------|
| "The puzzle IS the game" | Strong | All systems serve puzzle creation. Generator + Solver + Engine form a solid pipeline. |
| "Depth through emergence" | Strong | 6 simple aspects + 4 bag types create 7 distinct reaction types + 4 placement constraints. Net-hot calculation enables strategic Cold placement. |
| "Approachable surface" | On track | Simple complexity tier (6 aspects, 4 bags) gates mechanical depth. Intermediate adds 4 more aspects without changing Simple puzzles. |
| "Cozy progression" | Needs work | No tutorial system, no daily mode, no save system yet (tasks 4.3-4.6). Architecture supports it, but implementation is pending. |
| "Deterministic everything" | Strong | SeededRandom used consistently throughout PuzzleGenerator. ComplexityFilter ensures same rules per tier. |

### MVP Readiness

The remaining MVP tasks are primarily integration and game flow:
- 4.1: Win/loss evaluation (wiring ChemistryEngine to Confirm button)
- 4.2: PuzzleGenerator → UI integration (replacing DemoPuzzle)
- 4.3-4.4: Tutorial system
- 4.5: Daily puzzle mode
- 4.6: Save system

None of these require architectural changes to Core. The foundation is solid for building out the game flow layer.

### Forward Compatibility

The architecture is well-positioned for post-MVP features:
- **Puzzle Progression Mode:** PuzzleGenerator + ComplexityLevel already support tiered complexity
- **Venture Loop:** MetaGrid/Bag/Reagent models can be extended with stamina, damage states
- **Advanced Alchemy (multiple schools):** SchoolType enum has Wuxing/WesternAlchemy/Astrology slots; ReactionRule is school-agnostic
- **Quest System:** PuzzleState is serializable; quest constraints can be layered on DifficultyParams

The one concern for forward compatibility is the circular Chemistry↔Puzzle dependency (S1), which should be broken before adding school-specific rule databases.

---

## Summary of Recommendations (Priority Order)

| # | Issue | Effort | Impact | Blocking? |
|---|-------|--------|--------|-----------|
| S1 | Break Chemistry↔Puzzle circular dependency | Small (10 lines) | Structural health, testability | No |
| M1 | Make locked reagent placements explicit in PuzzleState | Small | Prevents bug in task 4.2 | Soft (before 4.2) |
| S2 | Extract aspect-aware placement constraints from MetaGrid | Medium | Layering, extensibility | No |
| M3 | Add Magnetic/Absorbent to AspectIds | Trivial | Consistency | No (when implementing Advanced tier) |
| CF2 | Fix AspectSet.Empty naming/allocation | Small | Clarity, minor perf | No |
| CF1 | Stabilize OrderPair with integer IDs | Small | Cross-runtime determinism | No |
| M2 | Document Insulated bag as unimplemented | Trivial | Prevents confusion | No |

**Overall assessment:** The architecture is in strong shape. The two significant issues (S1, S2) are structural concerns, not correctness bugs. The codebase is ready for the game flow phase (tasks 4.1-4.6) with no blocking architectural issues.
