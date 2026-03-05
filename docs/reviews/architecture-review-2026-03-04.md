# Architecture Review — 2026-03-04

**Reviewer:** Claude (architectural review agent)
**Branch:** `dev` (post R-1, R-2 fixes)
**Scope:** Full codebase review — Phase 1 (Core Chemistry) + Phase 2 (Puzzle Infrastructure) + review fixes R-1, R-2
**Prior review:** architecture-review-2026-03-03.md

---

## Executive Summary

The codebase is in **excellent architectural health**. All Phase 1 and Phase 2 tasks (1.1–2.4) plus both review-fix tasks (R-1, R-2) are complete and well-implemented. The P2 Stable aspect gap identified in the prior review has been fully resolved. The Core/Game separation remains pristine (zero Godot leakage across 27 Core files), and all 168 tests pass in ~133ms.

**Verdict: No blockers. Ready for Phase 3 (UI Chemistry) and Phase 4 (Game Flow).**

---

## Prior Review Follow-Up

| Prior Finding | Status | Notes |
|---|---|---|
| P2: Stable aspect missing | **RESOLVED** | R-1 added Stable to MvpAspects, ChemistryEngine buffering logic, and 7 tests |
| P3: GridCell uses `object?` | **Open (acceptable)** | No change — still low priority. Casts confined to MetaGrid.cs |
| P3: PuzzleGenerator magic numbers | **Open (acceptable)** | Appropriate to address when Phase 4 difficulty tuning begins |
| Info: DemoPuzzle becomes dead code | **Open (expected)** | Will be replaced in task 4.2 |
| Info: ReactionRuleDatabase allocates per call | **Open (acceptable)** | Not a bottleneck at current scale |

---

## Compliance Scorecard

| Area | Status | Notes |
|------|--------|-------|
| Core/Game separation | PASS | Zero `using Godot;` in 27 src/Core/ files |
| Custom Vector2I | PASS | `AlchemistCourier.Core.Grid.Vector2I` used throughout |
| Shape matrix convention | PASS | `bool[rows, cols]` row-major |
| Position convention | PASS | `Vector2I(col, row)` |
| Per-cell aspect indexing | PASS | Row-major traversal of true cells |
| Namespace pattern | PASS | `AlchemistCourier.Core.{System}` consistently |
| File-scoped namespaces | PASS | All files |
| Dependency direction | PASS | Core <- Game (no back-references) |
| No circular deps | PASS | Grid -> Bags -> Reagents -> Chemistry -> Puzzle |
| Seeded RNG | PASS | PuzzleGenerator uses SeededRandom exclusively |
| JSON serialization | PASS | PuzzleState jagged arrays + conversion helpers |
| Build health | PASS | 0 errors, 1 cosmetic Godot generator warning |
| Test health | PASS | 168/168 passing, ~133ms |
| DRY compliance | PASS | R-2 extracted CreateAspect duplication to Aspect.FromId |

---

## Design Doc Alignment

### Chemistry System (docs/chemistry-system.md)

| Spec Feature | Implemented? | Notes |
|---|---|---|
| Hot+Volatile = Explosion (Adjacent) | Yes | |
| Hot+Cold = Neutralization (SameBag) | Yes | |
| Sharp+Fragile = Breakage (Adjacent) | Yes | |
| Toxic+Toxic = ToxicCloud (SameBag, threshold=2) | Yes | |
| Cumulative Hot > 4 = Overheating | Yes | |
| Cumulative Cold > 4 = Hypothermia | Yes | |
| Cumulative Toxic > 3 = Poisoning | Yes | |
| Stable + reactive = Raised threshold | **Yes** | Fixed by R-1. Buffers all 3 tiers |
| Catalytic + reactive = Lowered threshold | No | Post-MVP (correct omission) |
| Null bag suppresses aspects | No | Needed before task 3.1 |
| Insulated bag raises thresholds | No | Post-MVP |

### Core Mechanics (docs/core-mechanics.md)

| Spec Feature | Implemented? | Notes |
|---|---|---|
| Meta-grid with bags | Yes | |
| Bag shapes (rectangular) | Yes | Non-rectangular shapes not yet needed |
| Reagent rotation (4 orientations) | Yes | |
| Cardinal adjacency (no diagonal) | Yes | |
| Bag spanning | Yes | GetBagsForReagent() |
| Per-cell aspect indexing | Yes | |
| All space from bags (no bare grid) | Yes | IsAvailable checks IsInBag |
| Bag properties (Null, Locked, etc.) | Partial | BagPropertyType enum exists; no runtime effects |

### Puzzle System (docs/mvp-specification.md)

| Spec Feature | Implemented? | Notes |
|---|---|---|
| Disassemble-a-solution algorithm | Yes | |
| Deterministic seeding | Yes | |
| Difficulty scaling (8 levels) | Yes | DifficultyParams.ForLevel() |
| Chemistry validation during placement | Yes | EvaluateProspective |
| FitAllSafely puzzle type | Yes | Default |
| MaximizeValue / MeetRequirement | Enum only | Not wired — correct for Phase 2 |
| PuzzleSolver validation | Yes | ValidateSolution + IsSolvable |
| Stable in generated puzzles | **Yes** | Fixed by R-1 |

---

## New Issues Found

### P3: Catalytic Aspect in Aspect.FromId but Not in Game Logic

**What:** `Aspect.FromId("catalytic")` returns a Catalytic aspect (added in R-2), and "catalytic" is a documented MVP aspect. However:
- `PuzzleGenerator.MvpAspects` does not include "catalytic" (so it's never generated)
- `ChemistryEngine` has no threshold-lowering logic for Catalytic

**Impact:** Low. The chemistry-system.md doc marks it as a modifier alongside Stable, but the MVP spec's "minimum set" only lists 6 aspects. Including Stable (which was the higher priority) was correct. Catalytic can wait for Phase 3/4 difficulty tuning.

**Recommendation:** Track for Phase 4. When difficulty tuning begins, add "catalytic" to MvpAspects and implement threshold-lowering logic (mirror of Stable buffering).

---

### P3: Null Bag Property Has No Runtime Effect

**What:** `BagPropertyType.Null` exists in the enum, and `Bag.HasProperty()` can check for it, but:
- `BagDefinition` constructor accepts properties, but generated bags never have any
- `ChemistryEngine` doesn't check bag properties when evaluating reactions
- No aspect suppression occurs for reagents in Null bags

**Impact:** Moderate for Phase 3. Task 3.1 (placement warnings) should accurately preview Null bag suppression. Tutorial lesson 10 introduces Null bags.

**Recommendation:** Implement Null suppression in ChemistryEngine before task 3.1. Needed steps:
1. When checking aspects on a cell, look up that cell's BagOwner
2. If bag HasProperty(Null), return empty AspectSet (suppressed)
3. Add unit tests for Null bag suppression
4. Add Null bags to PuzzleGenerator when `AllowSpecialBags` is true

---

### P4: Heavy + Fragile (Gravity) Crushing Rule Missing

**What:** The chemistry-system.md spec includes "Heavy + Fragile (gravity bag, above) = Crushing" as an example interaction. This is not implemented.

**Impact:** None for MVP. Gravity bags are post-MVP, and this rule specifically requires gravity bag semantics.

**Recommendation:** No action needed now. Track for post-MVP when gravity bags are implemented.

---

### P4: `GetRulesForAspects` Reconstructs Full Rule List Every Call

**What:** `ReactionRuleDatabase.GetRulesForAspects()` calls `GetMvpRules()` internally, allocating a new list of 7 rules on each lookup.

**Impact:** Negligible. Currently only used in tests. If ever called in a hot path, cache the rule list.

**Recommendation:** No action needed unless profiling shows this as a bottleneck.

---

## Architecture Observations

### Strengths

1. **Clean Core/Game separation.** 27 Core files, zero Godot imports. Game layer uses `CoreVec` alias for bridge types. Textbook clean architecture.

2. **Definition/Instance pattern.** `BagDefinition`/`Bag` and `ReagentDefinition`/`Reagent` cleanly separate data from runtime state. Supports data-driven content pipeline.

3. **Stable aspect implementation (R-1).** Well-integrated across all 3 sensitivity tiers (Adjacent, SameBag, WholeInventory). `GetAdjacentStableMagnitude()` deduplicates by position and excludes involved cells. Stacking works correctly.

4. **Aspect.FromId centralization (R-2).** Clean DRY fix. Single factory method with fallback for unknown IDs. 12 dedicated tests.

5. **Test quality.** 168 tests across 11 files, covering all Core subsystems. Theory/InlineData used for parametric coverage. Tests run in ~133ms.

6. **Deterministic puzzle generation.** SeededRandom wraps System.Random correctly. PuzzleGenerator feeds all randomness through it. Same seed provably produces same puzzle.

7. **Prospective evaluation.** try/finally pattern ensures grid cleanup even on exceptions. Sound approach for real-time hover preview.

### Areas to Watch for Phase 3

1. **Null bag effects needed before task 3.1.** Placement warnings (green/yellow/red) require accurate chemistry evaluation including Null suppression.

2. **DemoPuzzle.cs replacement (task 4.2).** Currently hardcoded with 6 specific reagents. Will be replaced by PuzzleGenerator. No issue — just track the transition.

3. **Bag properties in PuzzleGenerator.** `DifficultyParams.AllowSpecialBags` exists but PuzzleGenerator doesn't use it. When Tutorial 4 (Null bag lesson) is implemented (task 4.3), the generator needs to create Null bags.

4. **ReactionResult consequences not modeled.** The engine produces `ReactionResult` objects but nothing applies them (destroying reagents, damaging cells, applying status effects). This is correct for Phase 2 — consequences are UI/game-flow concerns for Phase 4.

---

## PRD Status Summary

| Phase | Tasks | Done | Remaining |
|-------|-------|------|-----------|
| core-chemistry (1.x) | 5 | 5 | 0 |
| puzzle-infrastructure (2.x) | 4 | 4 | 0 |
| review-fix (R-x) | 2 | 2 | 0 |
| ui-chemistry (3.x) | 5 | 0 | 5 |
| game-flow (4.x) | 6 | 0 | 6 |
| polish (5.x) | 1 | 0 | 1 |
| **Total** | **23** | **11** | **12** |

The dependency graph is healthy — all Phase 3 dependencies (1.3, 1.4, 1.5, 2.2) are satisfied. Phase 3 tasks can proceed immediately.

---

## Recommendations

1. **(Before 3.1)** Implement Null bag aspect suppression in ChemistryEngine. This is architecturally simple (check BagOwner.HasProperty in aspect lookup) and necessary for accurate placement warnings.

2. **(Phase 4)** Add Catalytic threshold-lowering to ChemistryEngine as the mirror of Stable buffering.

3. **(Phase 4)** Extract PuzzleGenerator magic numbers into DifficultyParams for per-level tuning.

4. **(Eventually)** Consider interfaces for GridCell.BagOwner/OccupyingReagent to improve type safety.

---

## Summary

| Area | Grade | Notes |
|------|-------|-------|
| Core/Game boundary | A | Pristine — 27 files, zero leakage |
| Convention compliance | A | All conventions followed |
| Design doc alignment | A | Prior Stable gap resolved |
| Chemistry engine | A | All 3 tiers + Stable buffering correct |
| Puzzle infrastructure | A | Deterministic generation + validation |
| Test quality | A | 168 tests, ~133ms, excellent coverage |
| PRD coherence | A | Dependencies valid, 11/23 done, no blockers |
| DRY / code quality | A | R-2 resolved last known duplication |
| Vision alignment | A | Correct MVP priorities maintained |

**Overall: A — All prior issues resolved. Architecture is sound for Phase 3 work.**
