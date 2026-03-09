# Architecture Review — 2026-03-03 (Update 2)

**Reviewer:** Claude (architectural review agent)
**Branch:** `dev` (26 commits ahead of `master`)
**Scope:** Phase 1 (Core Chemistry) + Phase 2 (Puzzle Infrastructure) — tasks 1.1–2.4

---

## Executive Summary

The codebase is in **excellent architectural health**. All 9 completed tasks (1.1–1.5, 2.1–2.4) implement their design doc specifications faithfully. The Core/Game separation is rigorously maintained with zero Godot leakage, naming conventions are consistent, and the 149-test suite passes cleanly in 66ms. Two moderate issues and a few minor observations are noted below.

**Verdict: One P2 gap (Stable aspect missing). No blockers for Phase 3.**

---

## Compliance Scorecard

| Area | Status | Notes |
|------|--------|-------|
| Core/Game separation | PASS | Zero `using Godot;` in 27 src/Core/ files |
| Custom Vector2I | PASS | Defined in Core.Grid, never imported from Godot |
| Shape matrix convention | PASS | `bool[rows, cols]` row-major throughout |
| Position convention | PASS | `Vector2I(col, row)` throughout |
| Per-cell aspect indexing | PASS | Row-major traversal of true cells, consistent |
| Namespace pattern | PASS | `AlchemistCourier.Core.{System}` for all Core files |
| File-scoped namespaces | PASS | All 27 Core + 10 Game files |
| Dependency direction | PASS | Core <- Game (no back-references) |
| No side-effect imports | PASS | No System.IO/Net/Threading in Core |
| No circular deps | PASS | Grid -> Bags -> Reagents -> Chemistry -> Puzzle |
| Seeded RNG | PASS | PuzzleGenerator uses SeededRandom exclusively |
| JSON serialization | PASS | PuzzleState uses jagged arrays with conversion helpers |
| Build health | PASS | 0 errors, 1 cosmetic Godot generator warning |
| Test health | PASS | 149/149 passing, 66ms |
| TODO/HACK markers | PASS | None found in Core |

---

## Issues Found

### P2: "Stable" Aspect Missing from Puzzle Generation

**What:** The MVP spec (`docs/mvp-specification.md`) lists the minimum aspect set as: **Hot, Cold, Volatile, Fragile, Toxic, Stable**. However:

1. `PuzzleGenerator.MvpAspects` (line 21) contains `{ "hot", "cold", "volatile", "fragile", "toxic", "sharp" }` — missing **"stable"**, includes **"sharp"** instead.
2. `ChemistryEngine` has no mechanism for Stable's documented behavior: "raises threshold for adjacent reactive pairs." This requires threshold modification logic that doesn't exist yet.
3. `ReactionRuleDatabase` has no rule involving Stable.

**Why it matters:** Stable is the core **buffering mechanic** — the player's primary tool for safely arranging reactive reagents. Without it, puzzles devolve into pure avoidance rather than strategic placement. The Stable aspect is central to tutorials 7+ and all daily puzzles.

**Note:** "Sharp" IS a valid MVP aspect (it has a Breakage rule in the database), so its inclusion is fine. But Stable needs to be added alongside it.

**Recommendation:** Create a task to:
1. Add `"stable"` to `PuzzleGenerator.MvpAspects`
2. Implement threshold-raising logic in `ChemistryEngine` (when a Stable cell is adjacent to a reactive pair, increase the pair's effective threshold)
3. Add unit tests for the buffering mechanic

---

### P3: GridCell Uses `object?` for BagOwner and OccupyingReagent

**What:** `GridCell.cs` declares:
```csharp
public object? BagOwner { get; set; }
public object? OccupyingReagent { get; set; }
```

This loses compile-time type safety. `MetaGrid.cs` casts these on every access (`cell.BagOwner as Bag`, `cell.OccupyingReagent as Reagent`, `cell.OccupyingReagent is Reagent neighbor`).

**Why:** This was likely done to avoid a circular namespace import between `Core.Grid` and `Core.Bags`/`Core.Reagents`. Using `object?` sidesteps the forward reference.

**Impact:** Low — all casts are confined to `MetaGrid.cs` which is the only consumer. The pattern works correctly today but makes the API less discoverable.

**Recommendation:** Consider introducing interfaces (`IOccupant`, `IBagOwner`) in `Core.Grid` that `Bag` and `Reagent` implement. Not urgent — refine when touching GridCell.

---

### P3: PuzzleGenerator Hardcoded Magic Numbers

**What:** Several generation parameters are hardcoded in `PuzzleGenerator.cs` that arguably belong in `DifficultyParams`:

| Line | Value | Purpose |
|------|-------|---------|
| 17 | `MaxRetries = 50` | Retry budget for generation |
| 18 | `MetaGridPadding = 2` | Spacing around/between bags |
| 181 | `r <= 6 && c <= 6` | Max bag dimension |
| 208 | `MaxAspectCount <= 1 ? 2 : 4` | Max reagent cell count derived from aspect count |
| 270 | `0.7f` | Probability of a cell getting an aspect |

**Impact:** Currently acceptable — reasonable defaults. As difficulty tuning happens in Phase 4, these should be exposed in `DifficultyParams` for per-level control.

**Recommendation:** Extract into `DifficultyParams` when Phase 3/4 work begins.

---

## Design Doc Alignment

### Chemistry System (docs/chemistry-system.md)

| Spec Feature | Implemented? | Notes |
|---|---|---|
| Hot+Volatile = Explosion (Adjacent) | Yes | Rule + engine evaluation |
| Hot+Cold = Neutralization (SameBag) | Yes | |
| Sharp+Fragile = Breakage (Adjacent) | Yes | |
| Toxic+Toxic = ToxicCloud (SameBag, threshold=2) | Yes | |
| Cumulative Hot > 4 = Overheating | Yes | WholeInventory tier |
| Cumulative Cold > 4 = Hypothermia | Yes | |
| Cumulative Toxic > 3 = Poisoning | Yes | |
| Stable + reactive = Raised threshold | **No** | See P2 issue above |
| Catalytic + reactive = Lowered threshold | No | Post-MVP (correct omission) |

### Puzzle Generation (docs/mvp-specification.md)

| Spec Feature | Implemented? | Notes |
|---|---|---|
| Disassemble-a-solution algorithm | Yes | Faithful implementation |
| Deterministic seeding | Yes | SeededRandom throughout |
| Difficulty scaling (8 levels) | Yes | DifficultyParams.ForLevel() |
| Chemistry validation during placement | Yes | EvaluateProspective check |
| FitAllSafely puzzle type | Yes | Default type |
| MaximizeValue / MeetRequirement | Enum only | Not wired — correct for Phase 2 |
| "Stable" in generated puzzles | **No** | See P2 issue |

### Core Mechanics (docs/core-mechanics.md)

| Spec Feature | Implemented? | Notes |
|---|---|---|
| Meta-grid with bags | Yes | |
| Bag shapes (rectangular) | Yes | Non-rectangular not yet needed |
| Reagent rotation (4 orientations) | Yes | ShapeUtil + Reagent.Rotation |
| Cardinal adjacency (no diagonal) | Yes | |
| Bag spanning | Yes | GetBagsForReagent() |
| Per-cell aspect indexing | Yes | Row-major through true cells |
| All space from bags (no bare grid) | Yes | IsAvailable checks IsInBag |

---

## Architecture Observations

### Strengths

1. **Clean layering.** The Core -> Game dependency direction is strictly enforced. The Game layer uses type aliasing (`CoreVec`) to bridge coordinates without leaking Godot types upward.

2. **Definition/Instance pattern.** `BagDefinition`/`Bag` and `ReagentDefinition`/`Reagent` cleanly separate immutable templates from mutable runtime state. This will support data-driven content well.

3. **Prospective evaluation.** `ChemistryEngine.EvaluateProspective()` uses try/finally to temporarily place a reagent, evaluate, and always clean up. Correct approach for hover previews at 60fps.

4. **Serialization-first design.** `PuzzleState` uses jagged arrays and string-based aspect IDs, making it JSON-ready without custom converters.

5. **Test discipline.** 149 tests covering all Core subsystems. Tests run in 66ms — well within CI budget.

6. **No static mutable state.** All static members are either readonly (Vector2I.CardinalDirections), factory properties (BagDefinition presets), or pure functions (ChemistryEngine). Thread-safe by design.

### Areas to Watch

1. **DemoPuzzle.cs becomes dead code in Phase 4.** Task 4.2 replaces DemoPuzzle with PuzzleGenerator. It still serves as a useful prototyping tool for now.

2. **ReactionRuleDatabase allocates on every call.** `GetMvpRules()` creates a new `List<ReactionRule>` each invocation. Acceptable now, but consider caching when called in hot loops (puzzle generation retries call it once per attempt — currently fine).

3. **No `Medicinal` or `Heavy` aspects in generator.** The `Aspect` class has `Medicinal()` and `Heavy()` factories, and DemoPuzzle uses them, but `PuzzleGenerator.MvpAspects` doesn't include them. Correct for MVP (no reaction rules for these), but track for post-MVP.

4. **Bag property effects not in ChemistryEngine.** Null suppression, Insulated threshold raising — these need implementation before task 3.1 (Placement Warnings) for accurate previews.

---

## Recommended PRD Task

Based on this review, I recommend adding one new task:

**R-1: Implement Stable aspect buffering mechanic** (Priority 2, Phase: core-chemistry)
- Add "stable" to `PuzzleGenerator.MvpAspects`
- Implement threshold-raising logic in `ChemistryEngine` for Stable-adjacent pairs
- Add unit tests for buffering behavior (Stable next to Hot+Volatile should suppress Explosion)
- Depends on: 1.4 (done)
- Verification: `dotnet test` — new Stable tests pass, generator can produce puzzles with Stable reagents

---

## Summary

| Area | Grade | Notes |
|------|-------|-------|
| Core/Game boundary | A | Pristine — 27 files, zero leakage |
| Convention compliance | A | All conventions followed consistently |
| Design doc alignment | A- | One gap: Stable aspect |
| Chemistry engine | A | All 3 tiers correct, prospective eval sound |
| Puzzle infrastructure | A | Deterministic generation + validation |
| Test quality | A | 149 tests, 66ms, good edge coverage |
| PRD coherence | A | Dependencies valid, no cycles |
| Godot integration | A | Clean bridge pattern |
| Vision alignment | A | Correct priorities for MVP |

**Overall: A- — Stable aspect gap is the only substantive finding. Ready for Phase 3.**
