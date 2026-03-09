# Architecture Review — 2026-03-05h

**Reviewer:** Claude Opus 4.6 (Ralph architectural review agent)
**Branch:** dev
**Scope:** Post-editor-phase (E.1–E.9) architecture assessment
**Previous review:** 2026-03-05g (pre-editor, highlight/hint phases)

---

## Executive Summary

The codebase remains in **excellent architectural health**. The editor phase (E.1–E.9) added 9 new files across Core and Game layers without introducing any convention violations or architectural regressions. Core/Game separation is pristine (zero Godot imports in Core), all 519 unit tests and 27 integration tests pass, and the dependency graph remains acyclic.

Two new observations join the three carried-forward from the previous review:
1. **NEW — PuzzleSearchSolver locked-reagent duplication** (~30 lines between Solve/CountSolutions) (Low)
2. **NEW — PuzzleSearchSolver hardcodes GetMvpRules()** instead of accepting rules parameter (Low)
3. **CARRIED — MetaGrid constraint validators should be extracted** (Medium, post-MVP)
4. **CARRIED — PuzzleScreen.BuildUI() monolith** (Low, post-MVP)
5. **CARRIED — MetaGridView ChemistryEngine coupling** (Low, post-MVP)

**Overall Grade: A-** (unchanged from previous review — editor phase maintained standards)

---

## 1. Core Layer (src/Core/) — PASS

### Godot Independence: VERIFIED
- **52 C# files** (+2 since last review: PuzzleSearchSolver.cs, PuzzleFile.cs), **zero** `using Godot;` imports
- Custom `Vector2I` at `Core.Grid.Vector2I` used exclusively
- All namespaces follow `AlchemistCourier.Core.{System}` with file-scoped syntax

### New Core Files (E.1–E.2)

| File | Lines | Assessment |
|------|-------|------------|
| PuzzleSearchSolver.cs | 228 | Pure C#, backtracking CSP solver. Uses Grid, Chemistry, Reagents correctly. Supports CancellationToken. |
| PuzzleFile.cs | 99 | Pure C#, System.Text.Json serialization. Follows SaveManager.cs pattern (graceful null on bad input). |

Both files are clean: no Godot deps, correct namespace, appropriate visibility, well-documented public API.

### Convention Compliance: VERIFIED
- Shape matrices: `bool[rows, cols]` — confirmed across all 52 files
- Positions: `Vector2I(col, row)` — confirmed in PuzzleSearchSolver backtrack loops
- Per-cell aspects: row-major flattened index — confirmed

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
Puzzle (Grid, Bags, Reagents, Chemistry)  ← PuzzleSearchSolver, PuzzleFile added here
       ↓
Save (System.Text.Json only)
```

No circular dependencies. PuzzleSearchSolver correctly depends on PuzzleSolver (ReconstructGrid/ReconstructReagents) and ChemistryEngine — same dependency direction as existing Puzzle subsystem files.

---

## 2. Game Layer (src/Game/) — PASS

### New Game Files (E.3–E.9)

| File | Lines | Purpose | Assessment |
|------|-------|---------|------------|
| Editor/ShapeEditorWidget.cs | ~150 | Reusable shape matrix editor | Clean, focused |
| Editor/BagEditorPanel.cs | ~250 | Bag editing UI | Proper signal pattern |
| Editor/ReagentEditorPanel.cs | ~300 | Reagent editing UI | Proper signal pattern |
| Editor/EditorScreen.cs | 745 | Main editor + play mode | ⚠ See §4.1 |
| Editor/SolverPanel.cs | ~200 | Solver integration | Background-threaded, CallDeferred marshaling |

### Core-Game Boundary: CLEAN
- All editor files import Core namespaces correctly
- EditorScreen uses PuzzleSolver.ReconstructGrid/ReconstructReagents (same pattern as PuzzleScreen)
- No business logic leakage — shape editing, bag properties, aspect dropdowns are pure UI
- SolverPanel calls PuzzleSearchSolver via Task.Run with proper CancellationToken

### Bridge Patterns: CONSISTENT
The editor files use the same bridge patterns established by PuzzleScreen:
1. **MetaGridView** as the shared grid renderer (reused in both edit and play modes)
2. **GridCoordinates** for Core.Vector2I ↔ Godot.Vector2 conversion
3. **Signal-driven events** (BagsChanged, ReagentsChanged, ShapeChanged)
4. **Data-down, events-up** flow (Core data flows into view properties; signals flow from children to parent)

---

## 3. Test Coverage — PASS

| Metric | Previous (2026-03-05g) | Current | Delta |
|--------|----------------------|---------|-------|
| Unit tests | 497 | 519 | +22 |
| Integration tests | 23 | 27 | +4 |
| Total | 520 | 546 | +26 |
| Failures | 0 | 0 | 0 |

### New Test Coverage (E.1–E.9)

| Area | New Tests | Type |
|------|-----------|------|
| PuzzleSearchSolver | 12 | Unit (solvable, unsolvable, timeout, cancelled, reactive, multi-seed, CountSolutions) |
| PuzzleFile | 10 | Unit (round-trip, null fields, invalid JSON, directory creation, bag property preservation) |
| Editor save/load | 1 | Integration (round-trip through EditorScreen) |
| Editor solver | 1 | Integration (solvable + unsolvable verification) |
| Editor play mode | 1 | Integration (enter/exit preserves state) |
| Editor screen | 1 | Integration (load, edit mode, play mode guard) |

**Gap (unchanged from previous review):** No dedicated tests for Bag.cs/BagDefinition.cs — tested indirectly through MetaGrid and PuzzleGenerator.

**Gap (unchanged):** B.1–B.4 bag mechanics and A.1–A.6 aspect mechanics lack integration tests. All have comprehensive unit tests but are excluded from the Easy-difficulty integration test path. Not blocking, but a Medium-difficulty integration test would improve confidence.

---

## 4. Structural Concerns

### 4.1 EditorScreen Dual-Mode Complexity (Low, post-MVP)

**New finding.** EditorScreen.cs (745 lines) manages two distinct UI modes (Edit and Play) in a single class:

- **Edit mode:** BagEditorPanel, ReagentEditorPanel, SolverPanel, file I/O controls
- **Play mode:** ReagentTray, ObjectivePanel, StatusBarPanel, placement interaction

The class holds lazy-created play-mode widgets (`_playTray?`, `_playObjective?`, `_playStatusBar?`) and switches between modes via `EnterPlayMode()`/`ExitPlayMode()`. This is acceptable for MVP but creates coupling between editor concerns and gameplay concerns.

**Recommendation (post-MVP):** Extract `EditorPlayModeView` as a standalone component. EditorScreen delegates to it, keeping edit-mode responsibility in the main class.

### 4.2 PuzzleSearchSolver Locked-Reagent Duplication (Low)

**New finding.** `Solve()` and `CountSolutions()` share ~30 identical lines for locked-reagent pre-placement:

```csharp
// This block appears nearly identically in both methods
var lockedIndices = new HashSet<int>();
for (int i = 0; i < state.Reagents.Count; i++)
{
    if (!state.Reagents[i].IsLocked) continue;
    var solutionPlacement = state.Solution.Find(p => p.ReagentIndex == i);
    // ... placement logic ...
}
```

**Recommendation:** Extract a private `PreplaceLocked()` helper that returns `(MetaGrid grid, List<Reagent> reagents, HashSet<int> lockedIndices, List<Placement> lockedPlacements)`. Both methods call it, then diverge into their specific backtracking paths.

### 4.3 PuzzleSearchSolver Hardcodes GetMvpRules() (Low)

**New finding.** Both `Solve()` and `CountSolutions()` call `ReactionRuleDatabase.GetMvpRules()` internally rather than accepting rules as a parameter:

```csharp
var rules = ReactionRuleDatabase.GetMvpRules();
```

When the complexity tier system (A.6) is used for post-MVP content, the solver will need to accept `ComplexityLevel` or a pre-filtered rule list. Currently this works because all puzzles use MVP rules, but it's a latent extensibility gap.

**Recommendation:** Add an optional `List<ReactionRule>? rules = null` parameter, defaulting to `GetMvpRules()` when null. Zero-breaking-change refactor.

### 4.4 CARRIED: MetaGrid Constraint Responsibility (Medium, post-MVP)

MetaGrid (520 lines) still contains 4 constraint validation methods embedding aspect/bag-property knowledge. HighlightEngine and CanPlaceReagent both call them independently. Extracting to a `Core.Constraints.ConstraintValidator` would make MetaGrid purely spatial and new constraints additive.

**Status:** Unchanged. Not blocking MVP. Becomes relevant when adding post-MVP bag types (Insulated, Reinforced) or aspects (Magnetic, Absorbent).

### 4.5 CARRIED: PuzzleScreen.BuildUI() Size (Low, post-MVP)

BuildUI() is still ~216 lines. The editor added `EditorScreen.BuildUI()` at similar scale (~200 lines). Both share inline StyleBoxFlat creation patterns.

**Recommendation (post-MVP):** Extract a shared `ThemeHelper` utility with `CreateStyledButton()`, `CreateThemedPanel()`, etc. Both screens can use it.

### 4.6 CARRIED: MetaGridView Chemistry Coupling (Low, post-MVP)

MetaGridView still calls `ChemistryEngine.EvaluateProspective()` directly for ghost preview warnings. Ideally PuzzleScreen would compute the warning level and pass it to the view.

**Status:** Unchanged. Correct and responsive in practice.

---

## 5. Dead Code

### AspectPrimerPanel.cs — Superseded

AspectPrimerPanel (src/Game/Inventory/AspectPrimerPanel.cs) was replaced by AspectLegendPanel in H.10. PuzzleScreen now references AspectLegendPanel exclusively. AspectPrimerPanel appears unused.

**Recommendation:** Delete AspectPrimerPanel.cs. Verify no remaining references.

---

## 6. PRD & Phase Alignment

### Completed Phases
| Phase | Tasks | Status |
|-------|-------|--------|
| core-chemistry | 1.1–1.5 | Done |
| puzzle-infrastructure | 2.1–2.4 | Done |
| ui-chemistry | 3.1–3.5 | Done |
| bag-mechanics | B.1–B.4 | Done |
| aspect-rework | A.1–A.6 | Done |
| review-fix | R-1 through R-8 | Done |
| game-flow | 4.1–4.6 | Done |
| polish | 5.1 | Done |
| highlight-system | H.1–H.14 | Done |
| hint-system | HINT.1–HINT.8 | Done |
| documentation | DOC.1–DOC.2 | Done |
| tooling | T.1–T.3 | Done |
| editor | E.1–E.9 | Done |

### Remaining Tasks
| ID | Title | Phase | Status |
|----|-------|-------|--------|
| ART.1 | RetroDiffusion MCP connector | art | Pending |
| ART.2 | Sprite preview scene | art | Pending |
| ART.3 | Art workflow documentation | art | Pending |
| ART.4 | Art references documentation | art | Pending |
| ART.5 | Style pilot generation | art | Pending |

All remaining tasks are in the `art` phase — external tooling and documentation, not core architecture. The game itself is feature-complete.

---

## 7. Architecture Alignment with Design Docs

| Principle (from docs/) | Status | Evidence |
|------------------------|--------|----------|
| Core has zero Godot deps | PASS | 52 files, 0 imports |
| Shape matrices bool[rows, cols] | PASS | Consistent everywhere |
| Positions Vector2I(col, row) | PASS | Consistent everywhere |
| Per-cell aspects row-major | PASS | ReagentDefinition, PuzzleState |
| Seeded deterministic RNG | PASS | SeededRandom wraps System.Random |
| Chemistry evaluation <16ms | PASS | Static engine, no allocation overhead |
| Data-driven reaction rules | PASS | ReactionRuleDatabase, ComplexityFilter |
| Disassemble-a-solution generation | PASS | PuzzleGenerator, PuzzleSearchSolver validates |
| Simple tier only for daily puzzles | PASS | DifficultyParams.ForLevel() all use Simple |
| File-scoped namespaces | PASS | All 52 Core + ~22 Game files |
| No unnecessary abstractions | PASS | Static engines, concrete types, no DI framework |

---

## 8. Web Export Readiness (Unchanged Risk)

**No HTML5 export has been validated.** This was flagged in previous reviews and remains the single highest-risk item for MVP shipping. The codebase uses:
- System.Text.Json (supported in Godot WASM)
- System.IO for file persistence (needs localStorage shim for web)
- Task.Run in SolverPanel (needs verification in WASM environment — web doesn't support arbitrary threading)

**Recommendation:** Validate web export before considering MVP shippable. SolverPanel's background threading may need a synchronous fallback for web.

---

## 9. Recommendations Summary

| # | Issue | Priority | Type | Action |
|---|-------|----------|------|--------|
| 1 | Extract constraint validators from MetaGrid | Medium (post-MVP) | Carried | Create `Core.Constraints.ConstraintValidator` |
| 2 | PuzzleSearchSolver locked-reagent duplication | Low | New | Extract `PreplaceLocked()` helper |
| 3 | PuzzleSearchSolver hardcodes MVP rules | Low | New | Add optional rules parameter |
| 4 | EditorScreen dual-mode complexity | Low (post-MVP) | New | Extract `EditorPlayModeView` |
| 5 | PuzzleScreen/EditorScreen BuildUI() size | Low (post-MVP) | Carried | Extract shared ThemeHelper |
| 6 | MetaGridView chemistry coupling | Low (post-MVP) | Carried | Pass computed warning level |
| 7 | Delete AspectPrimerPanel dead code | Trivial | New | Remove file, verify no refs |
| 8 | Validate web export | Medium (pre-ship) | Carried | Test HTML5 build, especially threading |

**No P0 issues. No convention violations. No blocking items. Architecture remains sound for MVP ship.**
