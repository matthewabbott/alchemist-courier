# Architecture Review — 2026-03-04b

**Reviewer:** Architecture Review Agent
**Branch:** `dev` (30+ commits ahead of master)
**Scope:** Full codebase alignment with design documents, conventions, and game vision

---

## Executive Summary

The codebase is in **strong shape** for its development stage. The Core/Game separation is clean (zero Godot imports in Core), data models faithfully implement the design docs, and the 216 passing unit tests provide solid coverage. The disassemble-a-solution puzzle generator is working. The architecture is well-positioned to support the remaining MVP tasks.

**Critical issues:** 0
**Significant issues:** 3
**Minor issues:** 5
**Observations:** 4

---

## Significant Issues

### S1. `OrderPair` uses `GetHashCode()` for reagent deduplication

**File:** `src/Core/Chemistry/ChemistryEngine.cs:359-362`

```csharp
private static (Reagent, Reagent) OrderPair(Reagent a, Reagent b)
{
    return a.GetHashCode() <= b.GetHashCode() ? (a, b) : (b, a);
}
```

**Problem:** `Reagent` doesn't override `GetHashCode()`, so this uses `object.GetHashCode()` which is non-deterministic across .NET runtimes and can produce collisions (two different reagents with the same hash). A collision means:
- The pair `(a, b)` and `(b, a)` could hash to the same key, but they could also NOT be normalized consistently
- More importantly for correctness: if `a.GetHashCode() == b.GetHashCode()`, the ordering is stable for that call, but the pair identity in the `HashSet<(Reagent, Reagent)>` relies on tuple equality which uses `object.ReferenceEquals` — so this actually works for dedup. The ordering concern is cosmetic.

**Revised assessment:** Lower severity than initially thought. The dedup works because `HashSet` uses tuple structural equality (reference equality for each element). The `OrderPair` just ensures `(a,b)` and `(b,a)` normalize to the same tuple. If hash codes collide, both `a.GetHashCode() <= b.GetHashCode()` calls return the same ordering — so it's still deterministic *within a process*.

**Recommendation:** Replace with a stable ordering (e.g., assign each reagent a unique integer ID and compare those) for clarity and cross-runtime determinism, but this is not a correctness bug.

### S2. `EvaluateProspective` temporarily mutates shared state

**File:** `src/Core/Chemistry/ChemistryEngine.cs:20-35`

```csharp
grid.PlaceReagent(reagent, position);
try { return Evaluate(grid, rules); }
finally { grid.RemoveReagent(reagent); }
```

**Problem:** This mutates the MetaGrid in place and relies on a `finally` block to restore it. While correct in single-threaded use, this pattern:
1. Makes it impossible to safely evaluate multiple prospective placements concurrently
2. Could cause issues if `Evaluate` throws (the `finally` restores state, but the exception propagates with potentially misleading context)
3. Violates the architecture doc's spirit of "prospective evaluation" being side-effect-free

**Current impact:** Low — the game is single-threaded and all callers are sequential. But this is a footgun for future development (e.g., parallel puzzle solving or AI opponents).

**Recommendation:** For now, document the single-threaded requirement clearly. Before post-MVP, consider a `MetaGrid.Snapshot()`/`MetaGrid.Clone()` approach for true side-effect-free evaluation.

### S3. `AspectSet.Empty` returns a new mutable instance each call

**File:** `src/Core/Chemistry/AspectSet.cs:65`

```csharp
public static AspectSet Empty => new();
```

**Problem:** This is a property that creates a new `AspectSet` on every access, not a singleton. Code like `PuzzleGenerator.AssignAspects` calls `AspectSet.Empty` in loops, creating garbage. More importantly, since `AspectSet` is mutable (has `Add`/`Remove`), callers could accidentally mutate what they think is a shared empty set. The current pattern avoids this by accident (each call gets a fresh instance), but:
1. It's wasteful — dozens of empty sets created during puzzle generation
2. It misleads readers into thinking `Empty` is a shared constant (like `string.Empty`)

**Recommendation:** Either:
- Make it `public static readonly AspectSet Empty = new();` and make `AspectSet` immutable (preferred — aligns with value-object design)
- Or rename to `CreateEmpty()` to signal that it allocates

---

## Minor Issues

### M1. Duplicate `ReagentHasHeavyAspect` implementations

**Files:** `src/Core/Grid/MetaGrid.cs:337-345` and `src/Core/Puzzle/PuzzleGenerator.cs:536-544`

Both contain identical `private static bool ReagentHasHeavyAspect(Reagent reagent)` methods. This is a DRY violation. If the Heavy aspect ID or detection logic changes, both must be updated.

**Recommendation:** Move to `Reagent` as a public method (e.g., `reagent.HasAspect("heavy")`) or to a shared utility. This is a natural fit for `Reagent` since it's querying the reagent's own aspects.

### M2. PuzzleGenerator doesn't generate Null bags

**File:** `src/Core/Puzzle/PuzzleGenerator.cs:505-523`

`AssignBagProperties` only assigns Gravity bags (30% chance when `AllowSpecialBags` is true). The MVP spec's tutorial progression includes lesson 10 (Null bag — aspect suppression), but the generator can't produce Null bag puzzles.

**Impact:** Tutorial 10 can't be generated procedurally — it would need hand-crafted puzzle state. This is fine for now if tutorials are scripted, but blocks procedural tutorial generation.

**Recommendation:** Add Null bag generation to `AssignBagProperties` when needed for tutorial/daily puzzles. Track as a dependency for task 4.3 (tutorial system).

### M3. `GridCell.BagOwner` and `OccupyingReagent` typed as `object?`

**File:** `src/Core/Grid/GridCell.cs:13-14`

This was a deliberate choice to avoid circular namespace dependencies, but it forces downcasting in `MetaGrid.GetBagAtCell()` and `GetReagentAtCell()`. It also means the compiler can't catch type errors at the GridCell level.

**Recommendation:** Consider using a generic `GridCell<TBag, TReagent>` or interface-based typing (`IBagOwner`, `IReagentOccupant`) to recover type safety without creating circular dependencies. Low priority — the current code works and the cast sites are few.

### M4. Hardcoded aspect IDs as string literals throughout

Aspect IDs (`"hot"`, `"cold"`, `"volatile"`, etc.) appear as magic strings in:
- `ChemistryEngine.cs` (Stable buffering: `"stable"`)
- `PuzzleGenerator.cs` (`MvpAspects` array, `"heavy"` check)
- `MetaGrid.cs` (`"heavy"` check)
- `ReactionRuleDatabase.cs` (all rule definitions)
- `GridConstants.cs` (color mapping)

If an aspect ID changes, it's easy to miss an occurrence. `Aspect.FromId()` partially addresses this, but the IDs themselves are still stringly-typed.

**Recommendation:** Add `public static class AspectIds` with `const string Hot = "hot";` etc., and use these constants everywhere. This is a small change with high discoverability benefit.

### M5. PuzzleGenerator always uses `PuzzleType.FitAllSafely`

**File:** `src/Core/Puzzle/PuzzleGenerator.cs:134`

The generator hardcodes `FitAllSafely` as the puzzle type. The MVP spec defines three types (FitAllSafely, MaximizeValue, MeetRequirement), but only FitAllSafely is generated.

**Impact:** This is expected at this stage — FitAllSafely is the simplest and most important type. MaximizeValue and MeetRequirement require additional generator logic (partial placement, requirement evaluation). Not a bug, just a scope note.

---

## Observations (No Action Required)

### O1. Stable buffering uses two different patterns (intentional)

After three code review passes (R-5, code-review-2026-03-04, code-review-2026-03-04b), the Stable buffering was deliberately settled on two patterns:
- **Pair reactions** (Adjacent, SameBag distinct): `if (stableBuffer > rule.Threshold) continue;` — binary suppression
- **Cumulative reactions** (SameBag same-aspect, WholeInventory): `effectiveThreshold = rule.Threshold + stableBuffer` — additive

This is architecturally sound: pair reactions are binary triggers (they either fire or don't), while cumulative reactions have magnitude to compare against. No change needed.

### O2. Test coverage distribution is healthy

216 tests across Chemistry (182), Grid (placeholders via MetaGrid tests), Puzzle (generator + solver), SeededRandom, and Aspect. The chemistry engine has the deepest coverage, appropriate for the most complex subsystem.

### O3. Core/Game boundary is exemplary

Zero violations of the Godot-free Core constraint. The bridge types (`GridCoordinates`, `GridConstants`) cleanly map between Core and Godot types. `DemoPuzzle` in Game/ avoids Godot imports despite being in the Game project. This separation will pay off for unit testing and potential engine migration.

### O4. Architecture doc alignment is strong

Every data model described in `docs/architecture.md` has a corresponding implementation:
- GridCell ✓, MetaGrid ✓, Bag/BagDefinition ✓, Reagent/ReagentDefinition ✓
- Aspect ✓, AspectSet ✓, ReactionRule ✓, ReactionResult ✓
- ChemistryEngine ✓ (with Evaluate + EvaluateProspective)
- PuzzleState ✓, PuzzleGenerator ✓, PuzzleSolver ✓, SeededRandom ✓

The implementation matches the documented structure with no drift.

---

## Alignment with Game Vision

### What's working well
1. **"The puzzle IS the game"** pillar is solid — all systems (grid, bags, chemistry, generation) serve puzzle creation
2. **"Depth through emergence"** — simple rules (6 aspects, 4 bag types) already create complex interactions via the chemistry engine
3. **Deterministic everything** — SeededRandom + seed-based generation ensures reproducible puzzles
4. **Web-first** — no dependencies that would block HTML5 export; .NET 8 is compatible with Godot web builds

### What needs attention before MVP
1. **Tutorial system** (task 4.3) — the current hardcoded DemoPuzzle needs to be replaced with generated puzzles that teach one concept at a time
2. **Volatile net-hot** (task A.1) — the signature MVP interaction (Hot near Volatile = explosion, but Cold mitigates) isn't implemented yet. Without this, the chemistry system feels flat.
3. **Win/loss evaluation** (task 4.1) — the Confirm button currently only checks "all placed"; it doesn't evaluate chemistry results to determine win/loss
4. **PuzzleGenerator → UI integration** (task 4.2) — critical path item; without this, there's no playable game loop

### Tone alignment
The architecture supports the "cozy core, earnest lore" tone through:
- Graduated difficulty (tutorial → easy → medium → hard)
- Prospective evaluation (players see what WOULD happen before committing)
- Non-punitive design (reset button, placement warnings)

---

## Summary of Recommendations (Priority Order)

| # | Issue | Effort | Impact |
|---|-------|--------|--------|
| M4 | Extract string aspect IDs to constants | Small | Prevents bugs across systems |
| M1 | Deduplicate `ReagentHasHeavyAspect` | Small | DRY, prevents drift |
| S3 | Fix `AspectSet.Empty` allocation pattern | Small | Correctness + performance |
| S1 | Stabilize `OrderPair` ordering | Small | Robustness |
| M2 | Add Null bag generation | Medium | Needed for tutorial 10 |
| S2 | Document single-thread requirement for EvaluateProspective | Small | Prevents future bugs |
| M3 | Type-safe GridCell owners | Medium | Code quality, low priority |

None of these are blocking — the codebase is well-structured and ready for the next phase of MVP tasks (4.1–4.6).
