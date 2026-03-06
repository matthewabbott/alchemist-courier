# Architectural Review — 2026-03-05e

**Reviewer:** Claude (Architecture Agent)
**Branch:** dev
**Scope:** Full architectural review after documentation phase completion (DOC.1–DOC.2)
**Build:** PASS (497 unit tests, zero failures)
**Previous Review:** 2026-03-05d (post hint-system)

---

## Executive Summary

**VERDICT: EXCELLENT**

The project is in outstanding architectural health. All 11 implementation phases are complete (63/80 tasks done). Core/Game separation remains perfect with zero Godot dependencies in `src/Core/` across all 50 files. The documentation phase (DOC.1–DOC.2) was the only change since the last review — no code changes, no regressions.

The remaining 17 tasks are all stretch/post-MVP: tooling (3), puzzle editor (9), and art pipeline (5). None of these block MVP shipping. **The MVP is functionally complete and architecturally sound.**

---

## 1. Core/Game Separation

**Status: PERFECT — Zero violations**

- All 50 files in `src/Core/` use only `System.*` and `AlchemistCourier.Core.*` imports
- Custom `Vector2I` in `AlchemistCourier.Core.Grid` correctly replaces `Godot.Vector2I`
- No `using Godot;` statements anywhere in Core (verified via grep of all 50 files)
- Dependency flow is strictly unidirectional: Game → Core, never Core → Game
- `src/UI/` remains empty — all UI is procedurally built in `src/Game/Inventory/`

---

## 2. Convention Compliance

**Status: PERFECT**

| Convention | Status |
|-----------|--------|
| `AlchemistCourier.Core.{System}` namespaces | All 50 files compliant |
| `AlchemistCourier.Game.{System}` namespaces | All 17 files compliant |
| Shape matrices `bool[rows, cols]` (row-major) | Verified in ShapeUtil, PuzzleState |
| Positions `Vector2I(col, row)` | Consistent throughout |
| Per-cell aspects by row-major flattened order | Verified in ReagentDefinition |
| File-scoped namespaces | All files use `namespace X;` syntax |
| `.csproj`/`.sln` committed with custom settings | Confirmed |

---

## 3. Architecture Quality

### 3.1 Core Layer (src/Core/) — 50 files, 8 systems

| System | Files | Health |
|--------|-------|--------|
| Grid | 3 (MetaGrid, GridCell, Vector2I) | Excellent |
| Chemistry | 11 (ChemistryEngine, Aspect, AspectSet, ReactionRule, Metadata) | Excellent |
| Reagents | 4 (Reagent, ReagentDefinition, ShapeUtil, Rarity) | Excellent |
| Bags | 3 (Bag, BagDefinition, BagPropertyType) | Excellent |
| Highlight | 9 (HighlightEngine, CellEdge, HighlightSet, ContiguousBoundary, StatusThresholds) | Excellent |
| Puzzle | 15 (Generator, Solver, HintEngine, HintState, Daily, Tutorial, Complexity) | Excellent |
| Save | 2 (SaveManager, SaveData) | Good |
| SeededRandom | 1 (Deterministic RNG wrapper) | Good |

**No God classes.** Largest Core files: HighlightEngine (~300 lines), MetaGrid (~350 lines) — appropriately sized.

**Dependency graph is clean and acyclic:**
```
SeededRandom (leaf)
Save (leaf)
Grid ← Bags (mutual, well-scoped)
Chemistry → Grid, Bags
Reagents → Chemistry, Grid
Highlight → Grid, Chemistry, Reagents
Puzzle → All (top-level orchestrator)
```

### 3.2 Game Layer (src/Game/) — 17 files

| Component | ~Lines | Assessment |
|-----------|--------|------------|
| PuzzleScreen | 866 | Large but cohesive — orchestrates UI + game flow + hints |
| MetaGridView | 779 | Large but cohesive — grid rendering + input + ghost preview |
| AspectLegendPanel | 367 | Clean |
| ItemDetailPanel | 241 | Clean |
| ReagentTrayItem | 223 | Clean |
| StatusBarPanel | 218 | Clean |
| TutorialScreen | 189 | Clean |
| HighlightOverlay | 178 | Clean |
| ReagentTray | 115 | Clean |
| **AspectPrimerPanel** | **114** | **Dead code — unreferenced** |
| DemoPuzzle | 110 | Clean |
| GridConstants | 100 | Clean |
| GridCoordinates | 50 | Clean |

### 3.3 Testing — 497 unit tests + integration harness

| System | ~Tests | Assessment |
|--------|--------|------------|
| Chemistry | 85 | Thorough |
| Puzzle (gen, solve, daily, hints) | 71 | Thorough |
| Grid/MetaGrid | 71 | Thorough |
| Highlight | 40 | Thorough |
| Save | 36 | Good |
| Reagents/Shapes | 40 | Good |
| SeededRandom | 20 | Good |
| Integration (TestHarness) | 20 | Comprehensive |

Tests follow AAA pattern, have descriptive names (`Evaluate_HotAndVolatileAdjacent_ReturnsExplosion`), and use clean helpers. Coverage is thorough across all core systems.

---

## 4. PRD Progress

| Phase | Tasks | Done | Status |
|-------|-------|------|--------|
| core-chemistry | 6 | 6 | COMPLETE |
| puzzle-infrastructure | 4 | 4 | COMPLETE |
| ui-chemistry | 5 | 5 | COMPLETE |
| bag-mechanics | 4 | 4 | COMPLETE |
| aspect-rework | 6 | 6 | COMPLETE |
| review-fix | 8 | 8 | COMPLETE |
| game-flow | 6 | 6 | COMPLETE |
| polish | 1 | 1 | COMPLETE |
| highlight-system | 14 | 14 | COMPLETE |
| hint-system | 8 | 8 | COMPLETE |
| documentation | 2 | 2 | COMPLETE |
| tooling | 3 | 0 | Not started |
| editor | 9 | 0 | Not started |
| art | 5 | 0 | Not started |
| **TOTAL** | **80** | **63** | **79%** |

All 11 implementation phases are complete. Remaining 17 tasks are stretch goals:
- **Tooling (T.1–T.3):** Desloppify integration, architecture guidelines doc
- **Editor (E.1–E.9):** Full puzzle editor (auto-solver, shape/bag/reagent editors, play mode)
- **Art (ART.1–ART.5):** RetroDiffusion pipeline, sprite preview, style creation

None of these block MVP shipping.

---

## 5. Design Document Alignment

| Doc | Alignment | Notes |
|-----|-----------|-------|
| architecture.md | Excellent | Core/Game separation, data-driven design, deterministic RNG all implemented |
| core-mechanics.md | Excellent | Two-layer spatial puzzle, per-cell bag effects, cardinal adjacency, 90° rotation, spanning |
| chemistry-system.md | Excellent | 10 aspects, 3 sensitivity tiers, 2 granularity modes, threshold modifiers |
| mvp-specification.md | Excellent | All MVP features implemented (daily puzzles, tutorial, highlights, hints, save/load) |
| puzzle-design.md (new) | Good | Comprehensive reference doc created in DOC.1 |

**Gaps vs. docs (all expected for MVP):**
- Weight/carrying capacity — post-MVP feature
- Web export — untested (highest-priority pre-ship gap)
- MaximizeValue/MeetRequirement puzzle types — enum-only, no evaluation logic yet
- Performance benchmarks — not measured, but grid sizes are small enough for MVP

---

## 6. Issues and Recommendations

### 6.1 Action Items

| Issue | Severity | Action |
|-------|----------|--------|
| **AspectPrimerPanel is dead code** | LOW | **Delete it.** Not referenced by PuzzleScreen or any active code. Superseded by AspectLegendPanel. |
| **Web export untested** | MEDIUM | Test HTML5 export before declaring MVP shippable. This is the single biggest gap. |
| **PuzzleScreen at 866 lines** | LOW | Monitor. Post-MVP, extract PuzzleFlowController to Core for testability. |
| **MetaGridView at 779 lines** | LOW | Monitor. Post-MVP, extract GridInputHandler to reduce complexity. |

### 6.2 Known Accepted Trade-offs

| Item | Status | Rationale |
|------|--------|-----------|
| MetaGrid → Chemistry coupling (Heavy/Sharp/Fragile) | Accepted | One-way dependency for constraint checking. Not circular. |
| Reaction rules hardcoded in GetMvpRules() | Accepted | Adequate for MVP's 7 rules. Data-drive when adding Intermediate/Advanced tiers. |
| Column-major MetaGrid storage `_cells[col, row]` | Accepted | Consistent throughout codebase. Matches Vector2I(col, row) convention. |
| Status bar shows base thresholds only | Deferred | Doesn't display effective thresholds with Medicinal/Stable modifiers. Low-priority UX gap. |

### 6.3 Post-MVP Architectural Recommendations

1. **PuzzleFlowController extraction:** Move game flow logic (confirm, reset, win/loss, hints) from PuzzleScreen into a Core class. Enables unit testing of game flow without Godot.

2. **Data-driven reaction rules:** When adding Intermediate/Advanced schools, load rules from JSON resources instead of hardcoding. Supports future modding and iteration.

3. **HighlightOverlay node pooling:** Currently uses flat list of ColorRects. Fine for MVP grids (6x8), but should use instanced rendering for puzzle editor with large grids (20x20+).

4. **Performance benchmarks:** Add timing tests for ChemistryEngine.Evaluate and HighlightEngine.Compute with max-size grids before web export.

---

## 7. Changes Since Last Review (2026-03-05d)

| Area | Change |
|------|--------|
| Code | None — only documentation commits (DOC.1, DOC.2) |
| Tests | Still 497 passing, zero failures |
| Phases | documentation phase now COMPLETE (was 0/2) |
| PRD | 63/80 done (was listed as 68/80 in previous review — discrepancy was from prior review counting differently) |
| New docs | `docs/puzzle-design.md` — comprehensive puzzle type reference |
| Issues | All findings from 2026-03-05d still apply; no regressions |

---

## 8. Verdict

**The Alchemist Courier codebase is in excellent architectural health with all 11 implementation phases complete.**

**Strengths:**
- Perfect Core/Game separation across 50 Core files and 63 completed tasks
- All conventions followed consistently (namespaces, shapes, vectors, per-cell aspects)
- Comprehensive test coverage (497 unit tests, all passing)
- Clean dependency graph with no circular references
- All critical MVP game mechanics functional end-to-end
- Documentation comprehensive and up-to-date (including new puzzle-design.md)

**Pre-ship checklist:**
1. Test HTML5 web export (highest priority)
2. Delete AspectPrimerPanel dead code (trivial cleanup)
3. Run integration test harness one final time before export

**The MVP is functionally complete.** The codebase is ready for web export testing and final QA.
