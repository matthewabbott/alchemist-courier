# Architectural Review — 2026-03-06

**Reviewer:** Architecture Review Agent
**Branch:** `dev`
**Build:** 526/526 unit tests passing
**Scope:** Full codebase alignment with design docs, conventions, and game vision

---

## Executive Summary

The Alchemist Courier codebase is in **strong architectural health**. All MVP phases are complete, the editor system works, game flow is wired end-to-end, and recent refactoring (CQ.1–CQ.4) has improved code quality. The Core/Game separation is pristine, deterministic randomness is fully enforced, and namespace conventions are consistent across all 106 C# source files.

Three architectural gaps warrant attention before post-MVP expansion:
1. **Game content is hardcoded** rather than data-driven (acceptable for MVP)
2. **Bags subsystem has zero unit tests** (indirect coverage only)
3. **No save/puzzle file versioning** (will break when schema evolves)

Two code quality concerns are tracked but not blocking:
4. **Three large files** (PuzzleScreen 762, MetaGridView 864, EditorScreen 745 lines) with god-method `BuildUI()` patterns
5. **Sharp/Fragile are still Intermediate tier** (VS.1 task pending to promote to Simple)

**Verdict: PASS** — No blocking issues. Three medium-severity gaps flagged, zero convention violations.

---

## 1. Core/Game Separation — PASS

The foundational constraint — `src/Core/` must have zero Godot dependencies — is **perfectly maintained** across all 55 Core C# files.

- Zero `using Godot;` statements in Core
- Custom `AlchemistCourier.Core.Grid.Vector2I` used consistently (16 files import it)
- All 25 Game files correctly import both Godot and Core namespaces
- Dependency direction is strictly Core ← Game (no reverse dependencies)
- Save system uses `System.Text.Json`, not Godot JSON utilities
- No `GodotObject`, `Node`, `Resource`, or any Godot type referenced in Core

## 2. Namespace Conventions — PASS

All 106 C# source files follow established patterns with file-scoped namespace syntax:

| Pattern | Count | Status |
|---------|-------|--------|
| `AlchemistCourier.Core.{System}` | 55 | Correct |
| `AlchemistCourier.Game.{System}` | 25 | Correct |
| `AlchemistCourier.Core.Tests.{System}` | 26 | Correct |

Core subsystems: Chemistry (8), Grid (3), Reagents (4), Bags (3), Puzzle (12), Highlight (8), Save (2), root (1).
Game subsystems: root (5), Inventory (14), Editor (5), Testing (3).

## 3. Shape Matrix & Position Conventions — PASS

Verified across all relevant files:

- Shape matrices consistently use `bool[rows, cols]` (documented in ReagentDefinition, BagDefinition, ShapeUtil)
- Positions consistently use `Vector2I(col, row)` — verified in ShapeUtil.GetCellPositions(), rotation logic, and all consumers
- Per-cell aspects indexed by row-major traversal of true cells — documented in ReagentDefinition and implemented in Reagent.GetAspectsAtPosition()
- Rotation correctly maps `rotated[col, rows - 1 - row] = shape[row, col]`

## 4. Deterministic Randomness — PASS

All randomness flows through `SeededRandom`:

- `new Random(seed)` appears exactly once — inside `SeededRandom.cs` constructor
- Zero `GD.Randi()` / `GD.Randf()` / `Random.Shared` / unseeded `new Random()` calls
- `PuzzleGenerator.Generate(int seed, ...)` creates `new SeededRandom(seed)` for all random decisions
- `DailyPuzzleManager.DateToSeed()` derives seed deterministically from date (YYYYMMDD format)
- `DateTime.UtcNow` appears once in Core (`DailyPuzzleManager.GenerateToday()`) — a convenience wrapper that immediately converts to a deterministic seed. All other DateTime usage is in Game layer.
- Tests verify: same seed → same sequence, same seed → same puzzle, shuffle determinism

## 5. Data-Driven Design — PARTIAL (Gap)

The architecture doc specifies data-driven content, but **all game content is hardcoded in C#**:

| Content Type | Location | Lines |
|-------------|----------|-------|
| Reaction rules (7) | `ReactionRuleDatabase.GetMvpRules()` | ~130 |
| Aspect metadata (12) | `AspectMetadataDatabase.BuildEntries()` | ~92 |
| Reagent shapes (11) | `PuzzleGenerator` static arrays | ~25 |
| Bag shapes (8) | `PuzzleGenerator` static arrays | ~55 |
| Example reagents (6) | `ReagentDefinition` static properties | ~80 |
| Example bags (5) | `BagDefinition` static properties | ~60 |
| Tutorial lessons (5) | `TutorialData` static data | ~115 |
| Demo puzzle | `DemoPuzzle.cs` inline construction | ~110 |
| Complexity tiers | `ComplexityFilter.cs` static arrays | ~88 |
| `resources/` directory | Empty (`.gitkeep` only) | — |

**Total hardcoded content: ~755 lines** across 9 files.

**Impact:** Content changes require C# recompilation. This blocks modding, designer-friendly authoring, and per-region rule sets (post-MVP Stage 6).

**Recommendation:** Low priority for MVP. Before post-MVP Stage 2 (Puzzle Progression), extract at minimum reaction rules and aspect metadata to JSON. Shape catalogs can follow when the ReagentCatalog (VS.2) is implemented.

## 6. Test Coverage — PARTIAL (Gap)

526 unit tests passing across 26 test files.

| Subsystem | Test Files | Tests | Gap |
|-----------|-----------|-------|-----|
| Chemistry | 5 | ~125 | — |
| Grid | 2 | ~80 | GridCell untested |
| Highlight | 3 | ~65 | StatusThresholds untested |
| Puzzle | 11 | ~148 | — |
| Reagents | 2 | ~13 | ReagentDefinition indirect only |
| Save | 2 | ~36 | — |
| SeededRandom | 1 | ~10 | — |
| **Bags** | **0** | **0** | **Entire subsystem untested** |

**Key gap:** `Bag`, `BagDefinition`, `BagPropertyType` have zero direct unit tests. Coverage is indirect through MetaGrid and PuzzleGenerator tests only.

**Recommendation:** Add `tests/AlchemistCourier.Core.Tests/Bags/BagTests.cs` before post-MVP work adds bag-dependent features.

## 7. Save & Puzzle File Versioning — GAP (New Finding)

Neither `SaveData` nor `PuzzleFile` includes a schema version field.

| File | Version Field | Migration Logic | Error Handling |
|------|--------------|-----------------|----------------|
| `SaveData.cs` | None | None | Returns fresh `SaveData()` on parse error (silent data loss) |
| `SaveManager.cs` | None | None | No upgrade path |
| `PuzzleFile.cs` | None | None | Returns `null` on parse error |

**Impact:** Adding new fields to SaveData (inevitable for post-MVP) risks breaking existing saves. Players lose progress silently — the fallback creates a blank SaveData.

**Recommendation:** Add `int SchemaVersion = 1` to SaveData before any schema extension. Implement a simple migration switch in `SaveManager.Load()`:
```csharp
if (data.SchemaVersion < 2) { /* migrate v1 → v2 */ }
```

## 8. Code Quality — Large Files

Three files exceed 700 lines. None are god objects (responsibilities are coherent), but all contain god-method `BuildUI()` patterns:

| File | Lines | Methods | Fields | God Method |
|------|-------|---------|--------|------------|
| `PuzzleScreen.cs` | 762 | 35 | 27 | `BuildUI()` 186 lines |
| `MetaGridView.cs` | 864 | 32 | 24 | `UpdateGhostPreview()` 59 lines |
| `EditorScreen.cs` | 745 | 24 | 30 | `BuildUI()` 94 lines |

**PuzzleScreen:** `BuildUI()` builds header, grid layout, tray, objective panel, legend, status bar, buttons, and hint UI in one method. State sync pattern repeated 4× (should consolidate into `SynchronizeUI()`). Recent extraction of `ResultOverlayPanel` and `HintOrchestrator` was a good step.

**MetaGridView:** Mixes rendering and input handling. `UpdateGhostPreview()` combines validity checking, warning computation, ghost cell rendering, and highlight rendering. Could extract `GridInputHandler`.

**EditorScreen:** Mixes editor mode and play mode logic. 30 fields split between two modes. Could extract `PlayModeController` to isolate play-mode state and handlers.

**Recommendation:** These are P3 code quality items per deslop.md priority matrix. Address when touching these files for VS-phase work. Do not refactor speculatively.

## 9. Game Layer Architecture — PASS

The Game layer properly bridges Core to Godot:

- **Orchestrator pattern:** PuzzleScreen, EditorScreen compose panels and delegate business logic to Core
- **Signal-driven:** Panels communicate via C# events (`ReagentSelected`, `ReagentPlaced`, `BackRequested`), no direct panel-to-panel coupling
- **State machine:** GameFlowController manages screen transitions (MainMenu → Tutorial → DailyPuzzle → Editor) with clean lifecycle (QueueFree on transition)
- **First-launch detection:** Routes to Tutorial if `TutorialLessonCompleted == 0`
- **Save integration:** GameFlowController owns SaveManager, passes SaveData to screens that need it

No circular dependencies, no business logic in Game layer (all in Core), no god objects.

## 10. Design Doc Alignment — PASS

| Requirement | Status | Notes |
|------------|--------|-------|
| Three puzzle types | Implemented | FitAllSafely, MaximizeValue, MeetRequirement |
| Disassemble-a-solution generation | Implemented | PuzzleGenerator |
| Cardinal-only adjacency | Implemented | MetaGrid.GetAdjacentCells |
| Per-cell bag effect resolution | Implemented | Null suppression per-cell |
| Sensitivity tiers | Implemented | Adjacent, SameBag, WholeInventory |
| Threshold modifiers | Implemented | Catalytic, Stable, Null, Medicinal |
| Per-puzzle complexity budget | Implemented | ComplexityFilter |
| Tutorial progression | Implemented | 5 lessons with progression |
| Daily puzzle with date seed | Implemented | DailyPuzzleManager.DateToSeed |
| Game flow (menu → tutorial → daily) | Implemented | GameFlowController |
| Prospective evaluation < 16ms | Not benchmarked | Architecture supports it |
| Web export | Not verified | No web-incompatible features detected |

**Sharp/Fragile classification mismatch:** The design docs (sprite-state-system.md, core-mechanics.md) treat Sharp and Fragile as core mechanics central to the danger model. ComplexityFilter currently classifies them as Intermediate. Task VS.1 is pending to promote them to Simple tier. This is a known mismatch, not a bug.

## 11. Architectural Risks for Post-MVP

| Risk | Severity | When It Matters |
|------|----------|-----------------|
| Save versioning (no schema version) | Medium | Stage 2 — any new save fields break existing saves |
| Hardcoded content (no JSON catalogs) | Medium | Stage 2 — puzzle catalogs need data-driven content |
| No school extensibility | Low | Stage 4 — Wuxing/Western Alchemy need registry pattern |
| No encounter infrastructure | Low | Stage 3 — Venture Loop needs entirely new systems |
| No performance benchmarks | Low | Web export — need to verify <16ms chemistry evaluation |
| PuzzleFile no versioning | Low | Editor sharing — puzzle format changes break shared files |

---

## Summary

| Area | Verdict | Severity |
|------|---------|----------|
| Core/Game separation | PASS | — |
| Namespace conventions | PASS | — |
| Shape/position conventions | PASS | — |
| Deterministic randomness | PASS | — |
| Data-driven design | PARTIAL | Medium |
| Test coverage (Bags gap) | PARTIAL | Medium |
| Save/puzzle versioning | GAP | Medium |
| Code quality (large files) | NOTED | Low (P3) |
| Game layer architecture | PASS | — |
| Design doc alignment | PASS | — |
| Post-MVP readiness | INFORMATIONAL | — |

**Overall: PASS** — The codebase is architecturally sound for MVP. Three medium-severity gaps (hardcoded content, Bags test coverage, save versioning) should be addressed before post-MVP Stage 2 but do not block the current milestone. Code quality concerns in large files are tracked as P3 items for incremental improvement.

---

## Action Items

### Before Post-MVP (Priority)
1. **Add save versioning:** `SchemaVersion` field + migration logic in SaveManager
2. **Add Bags unit tests:** Direct coverage for Bag, BagDefinition, BagPropertyType
3. **Promote Sharp/Fragile:** VS.1 task already tracked in prd.json

### When Touching Files (Boy Scout)
4. **Extract `BuildUI()` methods** in PuzzleScreen, EditorScreen into smaller builders
5. **Consolidate state sync** in PuzzleScreen into `SynchronizeUI()`
6. **Consider `GridInputHandler`** extraction from MetaGridView

### Before Stage 2
7. **Extract content to JSON:** Start with reaction rules and aspect metadata
8. **Add PuzzleFile versioning:** Schema version for editor-shared puzzles
