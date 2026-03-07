# Architectural Review — 2026-03-07 (Updated)

**Reviewer:** Architecture Review Agent (second pass)
**Branch:** `dev`
**Build:** 639/639 unit tests passing, 24/24 integration tests passing
**Scope:** Full codebase alignment with design docs, conventions, and game vision
**Prior Review:** 2026-03-06 (526 tests → 639 tests, +113 since last review)
**Method:** Deep automated analysis of all 97 source files + 30 test files + design docs

---

## Executive Summary

The codebase is in **excellent architectural health**. Core/Game separation is pristine across 97 source files (63 Core + 34 Game), all namespace conventions hold, deterministic randomness is fully enforced, and shape/position conventions are consistently applied. The code has grown by 8 Core files and 9 Game files since the last review, with all additions following established patterns.

**Four issues warrant attention:**

1. **P1 — MeetRequirement silently falls back to FitAllSafely** — affects ~1/3 of daily puzzles
2. **P2 — Thermal Stress visual warns of danger the reaction system doesn't enforce** — visual/logic mismatch
3. **P2 — ReactionRuleDatabase.GetMvpRules() allocates on every call** — 28 call sites, some per-frame
4. **P2 — Bags subsystem still has zero unit tests** — flagged in prior review, unchanged

Three spec-vs-implementation divergences are documented but non-blocking for MVP.

**Verdict: PASS** — No blocking issues. One P1 and three P2 items flagged. Zero convention violations.

---

## 1. Core/Game Separation — PASS

The foundational constraint — `src/Core/` must have zero Godot dependencies — is **perfectly maintained** across all 63 Core files.

- Zero `using Godot;` statements in Core
- Custom `AlchemistCourier.Core.Grid.Vector2I` used consistently (only comment reference to `Godot.Vector2I` in the type's own doc comment — intentional)
- All 34 Game files correctly import both Godot and Core namespaces
- Dependency direction: strictly Core ← Game (no reverse dependencies)
- Save system uses `System.Text.Json`, not Godot JSON utilities
- No `GodotObject`, `Node`, `Resource`, or any Godot type in Core

## 2. Namespace Conventions — PASS

All source files follow established patterns with file-scoped namespace syntax:

| Pattern | Count | Status |
|---------|-------|--------|
| `AlchemistCourier.Core.{System}` | 63 | Correct |
| `AlchemistCourier.Game.{System}` | 34 | Correct |
| `AlchemistCourier.Core.Tests.{System}` | 30 | Correct |

Core subsystems: Chemistry (13), Grid (3), Reagents (5), Bags (3), Puzzle (24), Highlight (10), Save (2), Visual (3), root (1).
Game subsystems: root (5), Inventory (15), Editor (5), Animation (2), Testing (3), Tools (2), Visual (2).

100% file-scoped namespace compliance. Zero bracket-style namespaces.

## 3. Shape Matrix & Position Conventions — PASS

Verified across all relevant files:

- Shape matrices consistently use `bool[rows, cols]` — documented in ReagentDefinition, BagDefinition, ShapeUtil
- Positions consistently use `Vector2I(col, row)` — verified in ShapeUtil.GetCellPositions(), rotation logic, all consumers
- Per-cell aspects indexed by row-major traversal of true cells — documented in ReagentDefinition, implemented in Reagent.GetAspectsForRotatedCellIndex()
- Rotation correctly maps `rotated[col, rows - 1 - row] = shape[row, col]`
- PlacedPosition is the anchor (top-left bounding box) — documented and consistent

**Note:** MetaGrid's internal `_cells` array uses `[col, row]` indexing while shape matrices use `[row, col]`. Both are internally consistent and mediated through `Vector2I(col, row)`, but the differing conventions are a potential confusion point for future developers. Worth a doc comment in MetaGrid.

## 4. Deterministic Randomness — PASS

All randomness flows through `SeededRandom`:

- `new Random(seed)` appears exactly once — inside `SeededRandom.cs` constructor
- Zero `GD.Randi()` / `GD.Randf()` / `Random.Shared` / unseeded `new Random()` calls in entire codebase
- `PuzzleGenerator.Generate(int seed, ...)` creates `new SeededRandom(seed)` for all random decisions
- `DailyPuzzleManager.DateToSeed()` derives seed deterministically from date (YYYYMMDD integer)
- Zero static mutable state in src/Core/ (all static fields are `readonly` or `const`)

## 5. Chemistry System Alignment — PASS (with 2 discrepancies)

The ChemistryEngine correctly implements:

| Spec Requirement | Status | Notes |
|-----------------|--------|-------|
| Volatile net-hot (sum Hot − sum Cold in cardinal neighbors) | Correct | Line 208: `if (netHot > effectiveThreshold)` |
| Cardinal-only adjacency | Correct | `Vector2I.CardinalDirections` = Up/Down/Left/Right |
| Threshold modifiers (Stable buffer, Catalytic reduction) | Correct | `effectiveThreshold = Max(0, base + stable - catalytic)` |
| Null bag suppression (per-cell) | Correct | `IsAspectSuppressed` checked in all evaluation paths |
| Sensitivity tiers (Adjacent, SameBag, WholeInventory) | Correct | Three separate evaluation methods |
| Granularity (PerCell, PerItem) | Correct | WholeInventory branches on granularity |
| Fragile vulnerability second pass | Correct | Runs after main evaluation, avoids double-counting |
| Medicinal buffer on WholeInventory | Correct | Added to effective threshold for cumulative rules |
| All 8 MVP aspects | Correct | Hot, Cold, Volatile, Stable, Heavy, Toxic, Sharp, Fragile |
| ReactionRuleDatabase rules | Correct | All 7 MVP rules match spec |

### Discrepancy 1: Cumulative threshold comparison operator

**Spec:** `Hot cumulative > threshold = Overheating` (strictly greater than)
**Implementation:** `if (totalMagnitude >= effectiveThreshold)` (greater than or equal)

With threshold 4, the implementation triggers at 4 cells; the spec implies 5. This is a minor numerical difference (1 unit). Likely intentional tuning, but the spec should be updated to match whichever behavior is desired.

### Discrepancy 2: Thermal Stress Fracture — visual without reaction (P2)

**Spec:** "Fragile + (Hot AND Cold adjacent) = Thermal Stress Fracture: breaks from thermal contradiction"
**Implementation:** `VisualStateResolver` detects and renders `VisualState.ThermalStress`, but no reaction rule in `ReactionRuleDatabase` produces a Breakage result for it. The player sees a danger warning for a hazard that never triggers.

**Recommendation:** Either add a ThermalStress reaction rule or remove/downgrade the visual state to informational-only.

## 6. Spec-vs-Implementation Divergences

### Heavy falls "in ALL bags" vs only Gravity bags

**Spec (core-mechanics.md):** "Heavy falls in ALL bag types, not just Gravity. In Normal bags only Heavy reagents fall."
**Implementation:** `MetaGrid.CalculateFallPosition()` returns immediately if the bag is not Gravity. `GetGravityViolations()` only checks cells in Gravity bags.

**Assessment:** Likely an intentional MVP simplification — Heavy falling in Normal bags with no visual gravity cue would confuse players. The spec should be updated to reflect the current behavior, or this should be deferred to post-MVP explicitly.

### Heavy crushing Fragile — not a reaction

**Spec:** "Heavy directly above Fragile = Crushing" (marked Intermediate)
**Implementation:** Exists as placement constraint (`GetHeavyViolations`) and visual state (`VisualState.Crushed`) but no reaction rule. The spec marks this Intermediate, so it's correctly deferred. Forward-compatible.

### MeetRequirement puzzle type — stub (P1)

**Spec (mvp-specification.md):** Three puzzle types: FitAllSafely, MaximizeValue, MeetRequirement.
**Implementation:** `PuzzleValidator.EvaluateMeetRequirement()` delegates to `EvaluateFitAllSafely()` with a TODO comment. But `DailyPuzzleManager.GetDailyPuzzleType()` routes ~1/3 of daily puzzles to MeetRequirement based on `seed % 3`.

**Impact:** Every 3rd daily puzzle evaluates as FitAllSafely regardless of its displayed type. If the UI shows "MeetRequirement" to the player, this is a user-facing bug. If MeetRequirement isn't intended for MVP, it should be excluded from `GetDailyPuzzleType()`.

**Recommendation:** Either implement MeetRequirement evaluation or restrict `GetDailyPuzzleType()` to return only FitAllSafely for MVP.

## 7. Test Coverage — PARTIAL (Recurring Gap)

639 unit tests passing across 30 test files. Up from 526 tests at last review (+113).

| Subsystem | Test Files | Status |
|-----------|-----------|--------|
| Chemistry | 5 | Covered |
| Grid | 2 | Covered |
| Highlight | 3 | Covered |
| Puzzle | 12 | Covered |
| Reagents | 3 | Covered |
| Save | 2 | Covered |
| Visual | 2 | Covered |
| SeededRandom | 1 | Covered |
| **Bags** | **0** | **Still untested** |

**Recurring gap:** `Bag`, `BagDefinition`, `BagPropertyType` have zero direct unit tests. This was flagged in the 2026-03-06 review and remains unaddressed. Coverage is indirect only (through MetaGrid and PuzzleGenerator tests).

**Recommendation:** Create `tests/AlchemistCourier.Core.Tests/Bags/BagTests.cs` — this is now a repeat finding.

## 8. Code Quality

### Large Files (Growing)

| File | Lines (Now) | Lines (Prior) | Change |
|------|------------|---------------|--------|
| `TestHarness.cs` | 1882 | N/A | Test code — acceptable |
| `MetaGridView.cs` | 960 | 864 | +96 |
| `PuzzleScreen.cs` | 832 | 762 | +70 |
| `EditorScreen.cs` | 744 | 745 | −1 |
| `PuzzleGenerator.cs` | 603 | ~600 | +3 |
| `MetaGrid.cs` | 576 | ~570 | +6 |
| `ChemistryEngine.cs` | 552 | ~550 | +2 |

MetaGridView and PuzzleScreen continue to grow. Both are accumulating UI responsibilities without decomposition. Per deslop.md this is P3, but the trend should be noted.

### ReactionRuleDatabase allocation concern (P2)

`GetMvpRules()` allocates a new `List<ReactionRule>` with 8 `ReactionRule` objects on every call. Found at 34 call sites across the codebase (8 in ReactionRuleDatabase tests, 7 in PuzzleScreen, 7 in PuzzleValidator tests, 5 in Puzzle/*.cs, 2 in ChemistryEngine tests, 2 in HighlightEngine tests, 2 in EditorScreen, 1 internal GetRulesForAspects call). PuzzleScreen calls it during per-frame operations.

**Recommendation:** Cache the rules list. `GetMvpRules()` should return a `static readonly IReadOnlyList<ReactionRule>`, or callers should cache the result.

### Duplicate DateToSeed logic

`SaveData.DateToSeed` (private) and `DailyPuzzleManager.DateToSeed` (public) are identical implementations: `year * 10000 + month * 100 + day`. `SaveData` should call `DailyPuzzleManager.DateToSeed` instead.

### Magic probability constants in PuzzleGenerator

Inline floats control generation behavior with no named constants:
- `0.4f` — 40% chance non-rectangular bag shape
- `0.7f` — 70% chance a cell gets an aspect
- `0.3f` — 30% chance a bag becomes Gravity

These are gameplay-tuning knobs buried in implementation. Consider extracting to named constants or DifficultyParams.

### OrderPair hash fragility

`ChemistryEngine.OrderPair()` uses `GetHashCode()` for ordering: `a.GetHashCode() <= b.GetHashCode() ? (a, b) : (b, a)`. Since `Reagent` doesn't override `GetHashCode()`, this relies on default object identity hashing. While functionally correct for dedup within a single pass, hash collisions could produce non-deterministic ordering. A stable comparison (by ID or list index) would be safer.

## 9. Data-Driven Design — PARTIAL (Unchanged)

All game content remains hardcoded in C# (same as prior review):

| Content | Location | Lines |
|---------|----------|-------|
| Reaction rules (7) | ReactionRuleDatabase | ~130 |
| Aspect metadata (12) | AspectMetadataDatabase | ~92 |
| Reagent shapes (11) | PuzzleGenerator statics | ~25 |
| Bag shapes (8) | PuzzleGenerator statics | ~55 |
| Tutorial lessons (5) | TutorialData | ~115 |
| Complexity tiers | ComplexityFilter | ~88 |

`resources/` directory is still empty (`.gitkeep` only). Acceptable for MVP, but extraction to JSON should precede post-MVP Stage 2.

## 10. Save & Puzzle File Versioning — GAP (Unchanged)

Neither `SaveData` nor `PuzzleFile` includes a schema version field. Flagged in prior review, unchanged. Silent data loss on schema mismatch remains a risk for post-MVP.

## 11. Game Layer Architecture — PASS

The Game layer properly bridges Core to Godot:

- **Orchestrator pattern:** PuzzleScreen/EditorScreen compose panels and delegate to Core
- **Signal-driven:** Panels communicate via signals, minimal direct coupling
- **State machine:** GameFlowController manages screen transitions
- **Action-based commands:** PlaceReagentAction/RemoveReagentAction wrap Core operations for undo/redo
- **No business logic in Game:** All constraint validation, reaction evaluation, and puzzle solving delegated to Core
- **Animation layer:** FallingAnimator and ReactionAnimator read Core results without computing them

No circular dependencies. Clean unidirectional flow: Screens → Orchestrators → Core Logic → Data Models.

---

## Summary

| Area | Verdict | Change from Prior |
|------|---------|-------------------|
| Core/Game separation | PASS | Unchanged |
| Namespace conventions | PASS | Unchanged (63+34+30 = 127 files) |
| Shape/position conventions | PASS | Unchanged |
| Deterministic randomness | PASS | Unchanged |
| Chemistry system alignment | PASS | 2 minor discrepancies documented |
| Test coverage | PARTIAL | +113 tests (526→639), Bags gap persists |
| Data-driven design | PARTIAL | Unchanged |
| Save versioning | GAP | Unchanged |
| Code quality | NOTED | Large files growing, new allocation concern |
| Game layer architecture | PASS | Unchanged |

**Overall: PASS** — Architecturally sound. The codebase has grown cleanly since last review with no new convention violations. One P1 issue (MeetRequirement stub in live code path) and three P2 issues (Thermal Stress visual mismatch, GetMvpRules allocation, Bags test gap) should be tracked.

---

## Action Items

### P1 — Fix Now
1. **MeetRequirement stub** — Either implement the evaluation or restrict `GetDailyPuzzleType()` to exclude it from daily rotation. Currently ~1/3 of daily puzzles silently use wrong evaluation logic.

### P2 — Fix This Sprint
2. **Thermal Stress visual/logic mismatch** — Add a reaction rule for ThermalStress or change the visual to informational-only. Players see a danger cue for a non-existent hazard.
3. **Cache GetMvpRules()** — Return a `static readonly` list instead of allocating 7 objects per call across 28 call sites.
4. **Add Bags unit tests** — Repeat finding. Create `tests/AlchemistCourier.Core.Tests/Bags/BagTests.cs`.

### P3 — When Touching Files
5. **Deduplicate DateToSeed** — `SaveData` should call `DailyPuzzleManager.DateToSeed()`
6. **Extract PuzzleGenerator probability constants** — Name the magic numbers (0.4f, 0.7f, 0.3f)
7. **Add MetaGrid doc comment** about `_cells[col, row]` vs shape `[row, col]` convention
8. **Decompose MetaGridView/PuzzleScreen** — Both trending over 900/800 lines respectively

### Before Post-MVP Stage 2
9. **Add save versioning** — `SchemaVersion` field + migration logic
10. **Extract content to JSON** — Start with reaction rules and aspect metadata
11. **Update spec** — Heavy-in-all-bags behavior, cumulative threshold operator (`>=` vs `>`)

---

## Supplementary Findings (Second Pass)

### A. Static State Audit — CLEAN

Audited all static members across Core. **Zero mutable static state found.** All static fields are `readonly` or `const`. Static methods are pure functions or factories. No thread-safety concerns for future parallelism (e.g., background puzzle solving).

### B. God-Class Analysis

#### MetaGridView.cs (960 lines) — Five Responsibilities

| Responsibility | Approx Lines | Extraction Target |
|---|---|---|
| Visual grid rendering (cells, lines, borders) | ~200 | `BorderRenderer` |
| Sprite sheet management & state resolution | ~100 | `SpriteRendererManager` |
| Ghost preview & placement validation | ~100 | `PreviewRenderer` |
| Mouse/keyboard input handling | ~150 | `InteractionHandler` |
| Rejection tooltip management | ~60 | `TooltipManager` |

Four border creation methods (`CreateBagBorders`, `CreateHintedBorders`, `CreateLockedBorders`, `CreateReagentBorders`) share nearly identical structure — candidate for unification.

#### PuzzleScreen.cs (832 lines) — Four Responsibilities

| Responsibility | Approx Lines | Extraction Target |
|---|---|---|
| UI construction (`BuildUI()`) | 200 | `PuzzleUIBuilder` |
| Game state management (place/remove/confirm/reset) | 200 | `PuzzleStateManager` |
| Hint orchestration + button state | 70 | `HintButtonController` |
| Highlight coordination (eagle sense, legend, status) | 60 | `HighlightCoordinator` |

`BuildUI()` at 200 lines with deep Godot node nesting is the most obvious extraction target.

**Priority:** P3 — both files work correctly. Decomposition would improve maintainability but isn't blocking. Track the growth trend; extract when next modifying these files.

### C. Dependency Graph Verification

Verified the complete dependency flow:

```
Puzzle (16 files)
  ├── Grid (MetaGrid, GridCell, Vector2I)
  ├── Bags (Bag, BagDefinition)
  ├── Reagents (Reagent, ReagentDefinition, ShapeUtil)
  └── Chemistry (ChemistryEngine, ReactionRule, AspectSet)

Highlight (10 files)
  ├── Grid (MetaGrid, GridCell, Vector2I)
  ├── Bags (BagDefinition)
  └── Reagents (Reagent, ShapeUtil)

Visual (3 files)
  ├── Grid (MetaGrid, Vector2I)
  ├── Reagents (Reagent)
  └── Chemistry (AspectIds)

Save (2 files)
  └── (standalone — System.Text.Json only)
```

No circular dependencies. No subsystem reaches "across" — all dependencies flow downward through Grid/Bags/Reagents/Chemistry foundations. This is textbook layered architecture.

### D. MeetRequirement Deep Dive — Confirmed Broken

Verified the complete failure path:
1. `DailyPuzzleManager.GetDailyPuzzleType(seed)` → returns `MeetRequirement` for `seed % 3 == 2`
2. `PuzzleGenerator.Generate()` → always sets `PuzzleType.FitAllSafely`
3. `GenerateDailyPuzzle()` → overrides type to `MeetRequirement`
4. `PuzzleValidator.EvaluateMeetRequirement()` → delegates to `EvaluateFitAllSafely()` (TODO comment)
5. `ObjectivePanel.GetObjectiveText()` → shows "Meet the requirement without triggering reactions"
6. `PuzzleState` → has no `Requirement` field to specify what to meet

The test `MeetRequirement_DelegatesToFitAllSafely()` explicitly tests the stub behavior. **This confirms P1: players see misleading objective text for ~1/3 of daily puzzles.**

**Simplest fix:** Change `DailyPuzzleManager.GetDailyPuzzleType()` to use `seed % 2` and return only FitAllSafely/MaximizeValue until MeetRequirement is fully implemented.

### E. Forward Compatibility Assessment

| Concern | Risk | Notes |
|---|---|---|
| PuzzleState schema | Low | Daily puzzles regenerated from seed; only in-progress saves affected |
| SaveData schema | Low | Web localStorage; users unlikely to carry saves across major versions |
| Reagent catalog | None | Hardcoded; no external compatibility surface |
| Reaction rules | None | Hardcoded; no external compatibility surface |

**Post-MVP risk:** Once daily puzzle archives exist (players revisiting past puzzles), PuzzleState changes become breaking. Add `Version` field before launching archives.

### F. Game Vision Alignment — STRONG

Cross-referenced all design pillars against implementation:

| Design Pillar | Status | Evidence |
|---|---|---|
| "Puzzle IS the game" | Aligned | Core systems revolve around grid/placement/chemistry |
| Flavor-first design | Aligned | Aspect names/metadata rich with alchemical flavor |
| Depth through emergence | Aligned | Chemistry system composes simple rules; new reagents need no code changes |
| Cozy progression | Aligned | Tutorial gradual, hints available, no punishment mechanics |
| Approachable surface | Aligned | 6 simple aspects for MVP, complexity tiers gate advanced mechanics |
| Deterministic daily | Aligned | Seeded RNG fully enforced, same seed = same puzzle |
| Web-first | Partially | System.Text.Json (web-safe), but no actual HTML5 export testing visible |

**One concern:** No evidence of HTML5 export testing or web-specific constraints being validated (e.g., file size, WebGL compatibility, localStorage integration testing). This should be done before MVP launch.
