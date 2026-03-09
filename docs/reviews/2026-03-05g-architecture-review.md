# Architecture Review — 2026-03-05g

**Reviewer:** Claude Opus 4.6 (Ralph architectural review agent)
**Branch:** dev
**Scope:** Full project architecture assessment — MVP ~95% complete

---

## Executive Summary

The Alchemist Courier codebase is in **excellent architectural shape**. The Core/Game separation is pristine, all 497 unit tests and 23 integration tests pass, and no critical bugs were found. The codebase has zero TODO/FIXME/HACK comments — a sign of disciplined development.

Two structural concerns emerged that will matter for post-MVP extensibility:
1. **MetaGrid has constraint validation logic that belongs in a separate validator** (Medium priority)
2. **PuzzleScreen.BuildUI() is a 216-line monolith** (Low priority for MVP, medium for post-MVP)

**Overall Grade: A-** (excellent for MVP stage; minor structural debt that won't block shipping)

---

## 1. Core Layer (src/Core/) — PASS

### Godot Independence: VERIFIED
- **50 C# files**, zero `using Godot;` imports
- Custom `Vector2I` at `Core.Grid.Vector2I` used throughout
- All namespaces follow `AlchemistCourier.Core.{System}` pattern with file-scoped syntax

### Convention Compliance: VERIFIED
- Shape matrices: `bool[rows, cols]` — confirmed in ShapeUtil, ReagentDefinition, BagDefinition
- Positions: `Vector2I(col, row)` — confirmed in ShapeUtil.GetCellPositions
- Per-cell aspects: row-major flattened index through true cells — confirmed in ReagentDefinition

### Dependency Graph: CLEAN (Acyclic)
```
SeededRandom, Enums, Vector2I  (no deps)
       ↓
Grid (Vector2I)
       ↓
Bags (Grid)     Reagents (Grid, Chemistry)
       ↓              ↓
Chemistry (Grid, Bags, Reagents)
       ↓
Highlight (Grid, Chemistry, Reagents)
       ↓
Puzzle (Grid, Bags, Reagents, Chemistry)
       ↓
Save (all above)
```

No circular dependencies. Fan-in hotspots (MetaGrid, Vector2I, Aspect) are appropriate for their roles as foundational types.

### Design Quality
- **Data Definition / Runtime Instance separation**: BagDefinition↔Bag, ReagentDefinition↔Reagent — clean
- **Static engines** (ChemistryEngine, HighlightEngine): stateless, testable, appropriate
- **No over-engineering**: No unnecessary interfaces, factories, or abstraction layers
- **Proper encapsulation**: Private fields, readonly where appropriate, IReadOnlyList returns

---

## 2. Game Layer (src/Game/) — PASS

### Core-Game Boundary: CLEAN
- All 17 Game files properly import Core namespaces
- Core types wrapped for Godot consumption (MetaGrid→MetaGridView, Reagent→ReagentTrayItem, etc.)
- No business logic leakage from Core into Game (chemistry evaluation calls are at appropriate orchestration points)
- Dependency flow is strictly one-way: Game → Core

### Procedural UI: WORKING
- All UI built in code (src/Game/Inventory/*.cs)
- src/UI/ is empty (.gitkeep only) — confirmed
- No .tscn scene files except test_harness.tscn

### Signal Architecture: CLEAN
- Signals flow child → parent (MetaGridView → PuzzleScreen)
- Clear naming convention (OnReagentPlaced, OnHintPressed, etc.)
- No cross-component tangling

---

## 3. Test Coverage — PASS

| Subsystem | Tests | Assessment |
|-----------|-------|-----------|
| Chemistry | ~125 | Comprehensive (ChemistryEngine: 85 tests) |
| Grid | ~80 | Heavy coverage (MetaGrid: 71 tests) |
| Puzzle | ~118 | Excellent (HintEngine, DailyPuzzleManager, PuzzleSolver all covered) |
| Highlight | ~65 | Good (constraint violations, boundary detection) |
| Save | ~35 | Full coverage (roundtrips, streaks, file I/O) |
| Reagents | ~13 | Adequate (complex behavior tested via integration) |
| Utilities | ~10 | SeededRandom determinism verified |
| **Total** | **497** | All passing, zero failures |

**Integration tests:** 23/23 passing (placement, rotation, warnings, hints, eagle sense, tutorial, daily mode)

**Gap (low risk):** No dedicated test files for Bag.cs and BagDefinition.cs, but these are well-tested indirectly through MetaGrid and PuzzleGenerator tests.

---

## 4. Structural Concerns

### 4.1 MetaGrid Constraint Responsibility (Medium Priority)

**Issue:** MetaGrid (520 lines) contains 4 constraint validation methods that embed chemistry/aspect knowledge:
- `GetGravityViolations()` — knows about gravity bag behavior
- `GetHeavyViolations()` — knows about AspectIds.Heavy
- `GetSharpViolations()` — knows about AspectIds.Sharp, BagPropertyType.Reinforced
- `GetFragileViolations()` — knows about AspectIds.Fragile, BagPropertyType.Null

This means MetaGrid has knowledge of specific aspects and bag properties. Adding a new constraint requires modifying MetaGrid rather than adding a new validator.

**Impact:** HighlightEngine and MetaGrid.CanPlaceReagent() both call these methods independently — two entry points into the same validation logic.

**Recommendation (post-MVP):** Extract to `Core.Constraints.ConstraintValidator` static class. MetaGrid becomes purely spatial (cells, bags, occupancy). Callers use ConstraintValidator for placement validation. This keeps MetaGrid focused and makes new constraints additive rather than invasive.

**Not blocking MVP:** Current implementation is correct and well-tested. This is about extensibility for post-MVP content (new bag types, new aspects with placement constraints).

### 4.2 PuzzleScreen.BuildUI() Size (Low Priority)

**Issue:** `BuildUI()` is 216 lines — a monolithic method that constructs the entire screen hierarchy.

**Impact:** Hard to navigate when making UI changes. Theme/style code (StyleBoxFlat creation) is repeated ~10 times.

**Recommendation (post-MVP):** Split into sub-builders (`BuildHeader()`, `BuildMainContent()`, `BuildBottomControls()`, `BuildResultOverlay()`). Extract theme helpers (`CreateThemedPanel()`, `CreateThemedButton()`).

**Not blocking MVP:** UI works correctly and is tested.

### 4.3 MetaGridView Chemistry Coupling (Low Priority)

**Issue:** MetaGridView directly calls `ChemistryEngine.EvaluateProspective()` for ghost preview warning colors. This embeds domain logic in a view component.

**Better pattern:** PuzzleScreen computes prospective reactions and passes a `WarningLevel` to MetaGridView.

**Not blocking MVP:** Current implementation is correct and responsive.

---

## 5. Known Issue Resolution

### StatusBarPanel Null-Bag Suppression — RESOLVED

The memory notes flagged "StatusBarPanel doesn't apply Null-bag suppression." **This is no longer accurate.** StatusBarPanel.cs lines 75 and 87 both call `ChemistryEngine.IsAspectSuppressed(_grid, pos)` before accumulating aspect magnitudes. The suppression is correctly applied.

### Grid→Chemistry Dependency — ACKNOWLEDGED

ChemistryEngine depends on MetaGrid (queries grid state). MetaGrid contains constraint methods that reference aspect IDs. This is bidirectional awareness but **not a circular dependency** — MetaGrid doesn't import ChemistryEngine. It's a cohesion concern (§4.1 above), not a correctness issue.

---

## 6. MVP Completion Assessment

| Category | Status | Notes |
|----------|--------|-------|
| Core Chemistry | COMPLETE | All 7 MVP reaction rules + modifiers |
| Puzzle Generation | COMPLETE | Disassemble-a-solution, daily seeding |
| Tutorial System | COMPLETE | 5+ lessons, progressive complexity |
| Hint System | COMPLETE | Auto-pick, snap-back, budget tracking |
| Highlight System | COMPLETE | Constraints, interactions, eagle sense, legend |
| Save System | COMPLETE | Tutorial progress, daily completions, streaks |
| Win/Loss Evaluation | COMPLETE | FitAllSafely puzzle type |
| UI | COMPLETE | Procedural, all panels working |
| Test Coverage | STRONG | 497 unit + 23 integration tests |

**MVP is feature-complete.** Remaining work is polish and the structural improvements noted in §4.

---

## 7. Recommendations Summary

| # | Issue | Priority | Action |
|---|-------|----------|--------|
| 1 | Extract constraint validators from MetaGrid | Medium (post-MVP) | Create `Core.Constraints.ConstraintValidator` |
| 2 | Split PuzzleScreen.BuildUI() | Low (post-MVP) | Break into sub-builder methods |
| 3 | Decouple MetaGridView from ChemistryEngine | Low (post-MVP) | Pass computed warning level instead of rules |
| 4 | Add direct Bag/BagDefinition tests | Low | Optional — already tested indirectly |
| 5 | Factor repeated StyleBoxFlat creation | Low | Create theme helper methods |

**No P0 issues. No blocking items. No convention violations. Ship the MVP.**
