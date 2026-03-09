# Architectural Review — 2026-03-05c

**Reviewer:** Claude (Architecture Agent)
**Branch:** dev
**Scope:** Full architectural review after highlight-system phase completion (H.1–H.14)
**Build:** PASS (466 unit tests, 19 integration tests, zero failures)

---

## Executive Summary

**VERDICT: EXCELLENT**

The project is in outstanding architectural health. All 54 completed tasks (84% of MVP) have high-fidelity implementations matching PRD descriptions. The Core/Game separation is perfectly maintained with zero Godot dependencies in `src/Core/`. Test coverage is comprehensive and growing steadily. Phase ordering is logical and dependency chains are correct.

The highlight-system phase (H.1–H.14) was completed cleanly, adding a well-layered rendering system on top of the existing Core infrastructure. The codebase is ready for the final MVP phases: hint-system and documentation.

---

## 1. Core/Game Separation

**Status: PERFECT — Zero violations**

- All 46 files in `src/Core/` use only `System.*` and `AlchemistCourier.Core.*` imports
- Custom `Vector2I` in `AlchemistCourier.Core.Grid` properly replaces `Godot.Vector2I`
- No `using Godot;` statements anywhere in Core
- All business logic (chemistry, puzzle generation, highlighting, saves) lives in Core
- `src/Game/` acts as a clean bridge layer, delegating all logic to Core

**Dependency flow is strictly unidirectional:** Game → Core, never Core → Game.

---

## 2. Namespace & Convention Compliance

**Status: PERFECT**

| Convention | Status |
|-----------|--------|
| `AlchemistCourier.Core.{System}` namespaces | All 46 files compliant |
| `AlchemistCourier.Game.{System}` namespaces | All 16 files compliant |
| Shape matrices `bool[rows, cols]` (row-major) | Verified in ShapeUtil, PuzzleState |
| Positions `Vector2I(col, row)` | Consistent throughout |
| Per-cell aspects by row-major flattened order | Verified in ReagentDefinition |
| File-scoped namespaces | All files use `namespace X;` syntax |
| `.csproj`/`.sln` committed with custom settings | Confirmed |

---

## 3. Architecture Quality Assessment

### 3.1 Core Layer (src/Core/)

**Systems and their responsibilities are well-defined:**

| System | Files | Purpose | Health |
|--------|-------|---------|--------|
| Grid | 5 | MetaGrid, GridCell, Vector2I, Bag, BagDefinition | Excellent |
| Chemistry | 8 | ChemistryEngine, Aspect, AspectSet, ReactionRule, AspectMetadata | Excellent |
| Reagents | 3 | Reagent, ReagentDefinition, ShapeUtil | Excellent |
| Puzzle | 7 | Generator, Solver, State, DailyManager, Tutorial, Complexity | Excellent |
| Highlight | 7 | HighlightEngine, CellEdge, HighlightSet, ContiguousBoundary, Violations | Excellent |
| Save | 2 | SaveManager, SaveData | Good |
| Bags | 3 | BagDefinition, BagProperty, BagPropertyType | Excellent |

**No God classes.** The largest Core file is HighlightEngine (~300 lines), which is appropriately complex for its responsibility.

### 3.2 Game Layer (src/Game/)

**Bridge layer is clean and well-organized:**

| Component | Lines | Responsibility | Assessment |
|-----------|-------|----------------|------------|
| MetaGridView | ~700 | Grid rendering + input handling | Large but cohesive |
| PuzzleScreen | ~720 | Screen orchestration + win/loss | Appropriate complexity |
| HighlightOverlay | ~200 | Visual highlight rendering | Clean single-purpose |
| StatusBarPanel | ~220 | Cumulative threshold display | Well-encapsulated |
| AspectLegendPanel | ~150 | Aspect rule reference sidebar | Clean |
| ItemDetailPanel | ~150 | Reagent inspection (Book of Hours) | Clean |
| ReagentTray | ~180 | Reagent list + selection | Clean |

**Observation:** MetaGridView and PuzzleScreen are the largest files (~700 lines each). This is acceptable for MVP — they're orchestrating complex interactive UI. If post-MVP expansion adds significant complexity, consider extracting:
- GridInputHandler from MetaGridView
- GameFlowController from PuzzleScreen

### 3.3 Testing Layer

**Comprehensive and well-structured:**
- 466 unit tests across 21 test files covering all Core systems
- 20 integration test scenarios in TestHarness covering full game flow
- Tests are behavior-focused (testing outcomes, not implementation details)
- Good use of helpers/factories to reduce boilerplate
- Proper isolation (no shared state, IDisposable for cleanup)

---

## 4. Design Document Alignment

### 4.1 Architecture.md Alignment

| Principle | Implemented | Notes |
|-----------|------------|-------|
| Core/Game separation | Yes | Perfectly maintained |
| Data-driven design | Partial | Reaction rules defined in code (GetMvpRules), not resource files. Acceptable for MVP. |
| Prospective evaluation | Yes | EvaluateProspective with place-evaluate-remove pattern |
| Deterministic RNG | Yes | SeededRandom with Fisher-Yates shuffle |
| Web-first | Deferred | No web export testing yet (documented as post-MVP) |
| <16ms chemistry eval | Untested | No performance benchmarks, but scope is small enough for MVP |

### 4.2 Core Mechanics Alignment

| Mechanic | Implemented | Notes |
|---------|------------|-------|
| Two-layer spatial puzzle | Yes | MetaGrid (bags on grid) + bags (reagents in bags) |
| Per-cell bag effects | Yes | Each cell affected only by its bag |
| Cell-contact adjacency | Yes | Cardinal directions, no diagonals |
| 90° rotation only | Yes | ShapeUtil.Rotate90Clockwise |
| Spanning across bags | Yes | Reagents can occupy cells in adjacent bags |
| Weight/carrying capacity | Not yet | Post-MVP feature |

### 4.3 Chemistry System Alignment

| Feature | Implemented | Notes |
|---------|------------|-------|
| 6 Simple aspects | Yes | Hot, Cold, Volatile, Stable, Heavy, Toxic |
| 4 Intermediate aspects | Yes | Sharp, Fragile, Catalytic, Medicinal |
| Sensitivity tiers | Yes | Adjacent, SameBag, WholeInventory |
| Granularity | Yes | PerCell and PerItem |
| Threshold modifiers | Yes | Catalytic (-1), Stable (+1), Medicinal (global) |
| Null bag suppression | Yes | IsAspectSuppressed in ChemistryEngine |
| Complexity levels | Yes | ComplexityFilter with Simple/Intermediate/Advanced |

### 4.4 MVP Specification Alignment

| Feature | Status | Notes |
|---------|--------|-------|
| Date-seeded daily puzzles | Done | YYYYMMDD seed format |
| FitAllSafely puzzle type | Done | Binary pass/fail |
| Disassemble-a-solution gen | Done | 50-retry loop |
| Placeholder colored rectangles | Done | GridConstants color palette |
| Tutorial (5 lessons) | Done | Progressive complexity |
| Save/load (localStorage) | Done | JSON-based SaveManager |
| Win/loss evaluation | Done | ChemistryEngine.Evaluate |
| Highlight system | Done | Full H.1–H.14 implementation |
| Hint system | **Pending** | HINT.1–HINT.8 not started |

---

## 5. PRD Progress

| Phase | Tasks | Done | Status |
|-------|-------|------|--------|
| core-chemistry | 6 | 6 | 100% |
| puzzle-infrastructure | 4 | 4 | 100% |
| ui-chemistry | 5 | 5 | 100% |
| bag-mechanics | 4 | 4 | 100% |
| aspect-rework | 6 | 6 | 100% |
| review-fix | 8 | 8 | 100% |
| game-flow | 6 | 6 | 100% |
| polish | 1 | 1 | 100% |
| highlight-system | 14 | 14 | 100% |
| hint-system | 8 | 0 | 0% |
| documentation | 2 | 0 | 0% |
| **TOTAL** | **64** | **54** | **84%** |

All dependency chains verified correct. No circular dependencies. Phase ordering is logical.

---

## 6. Issues & Recommendations

### 6.1 Known Issues (Carried Forward)

| Issue | Severity | Status | Notes |
|-------|----------|--------|-------|
| StatusBarPanel Null-bag suppression | MEDIUM | Fixed (R-8) | R-8 made IsAspectSuppressed public and StatusBarPanel uses it |
| MetaGrid → Chemistry coupling | LOW | Accepted | MetaGrid knows about Heavy/Sharp/Fragile for placement constraints; intentional for MVP |
| Tutorial win overlay vertical text | LOW | Cosmetic | UI display issue, non-blocking |
| Left-side UI clips on wide grids | LOW | Cosmetic | Only affects generated/daily puzzles with large grids |
| Status bar shows base thresholds | LOW | Deferred | Doesn't account for Stable/Medicinal effective thresholds in display |

### 6.2 Test Coverage Gaps

| Gap | Priority | Recommendation |
|-----|----------|---------------|
| Multi-constraint composition (3+ layers) | HIGH | Add HighlightEngineTests with Sharp+Fragile+Gravity overlaps |
| Cascade reactions (5+ reagents) | HIGH | Add ChemistryEngineTests with 8-10 reagent chain reactions |
| Puzzle solvability validation | MEDIUM | Verify generated puzzles are always solvable |
| Performance benchmarks | LOW | Add timing tests for chemistry eval with 50+ reagents |

### 6.3 Architectural Recommendations

1. **Data-driven reaction rules (Post-MVP):** Currently `GetMvpRules()` returns hardcoded rules. When adding Intermediate/Advanced reactions, consider loading from JSON/resource files to support modding and easier iteration.

2. **MetaGridView size management:** At ~700 lines, MetaGridView is approaching the threshold where extraction would improve maintainability. If hint-system adds significant interaction logic, consider splitting into GridRenderer + GridInputHandler.

3. **Web export testing:** No web export has been tested. Before shipping MVP, set up a CI step or manual process to verify HTML5 export works with the current architecture.

4. **Consider extracting PuzzleFlow:** PuzzleScreen.cs handles both UI construction and game flow (confirm, reset, win/loss). A PuzzleFlowController in Core could make this logic unit-testable.

---

## 7. Highlight System Review (H.1–H.14)

The highlight system was the most recent major addition. It deserves focused review.

**Architecture:** Clean three-layer design:
1. **Core data types** (H.1): CellEdge flags, HighlightLayer, HighlightSet — pure C#, no rendering
2. **Core engine** (H.4–H.6): HighlightEngine computes highlights from game state — pure C#
3. **Game renderer** (H.7–H.8): HighlightOverlay renders using Godot ColorRect pool

**Assessment: EXCELLENT**
- Computation fully in Core, rendering fully in Game
- HighlightEngine is stateless — takes inputs, returns highlights
- ContiguousBoundary correctly handles irregular shapes and disjoint cells
- Edge rendering uses efficient pooling (creates/reuses ColorRect instances)
- Ghost preview composition (H.13) properly layers constraint marks on top of placement ghost

**One concern:** HighlightOverlay uses a flat list of pooled ColorRects. For very large grids (20x20+), this could create 1000+ nodes. Not a problem for MVP grids (6x8 typical), but worth noting for post-MVP scaling.

---

## 8. Verdict

**The Alchemist Courier codebase is in excellent architectural health.**

Strengths:
- Perfect Core/Game separation maintained across 54 completed tasks
- All conventions followed consistently
- Comprehensive test coverage (466 unit + 20 integration)
- Clean dependency flow with no circular references
- PRD accurately tracks project state
- Highlight system adds a well-layered rendering pipeline

Areas to watch:
- MetaGridView and PuzzleScreen size (acceptable now, monitor growth)
- Web export testing (not yet validated)
- Performance benchmarks (not yet measured, likely fine for MVP scope)

**The project is ready to proceed with the hint-system phase (HINT.1–HINT.8).**
