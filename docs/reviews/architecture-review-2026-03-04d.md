# Architecture Review — 2026-03-04d

**Reviewer:** Architecture Review Agent (Ralph loop)
**Branch:** `dev`
**Build:** 0 errors, 289 unit tests passing
**Phase:** All pre-game-flow phases complete (core-chemistry, puzzle-infrastructure, ui-chemistry, bag-mechanics, aspect-rework, review-fix). Phase `game-flow` (4.1–4.6) and `polish` (5.1) pending.

---

## Executive Summary

The project is in strong architectural shape heading into the game-flow phase. The Core/Game boundary is cleanly enforced, shape conventions are correct throughout, and the chemistry engine faithfully implements the design docs. The main concerns are: (1) a stale MEMORY.md with incorrect metadata, (2) the Grid→Chemistry dependency created by aspect-aware placement constraints in MetaGrid, (3) StatusBarPanel computing cumulative totals without Null-bag suppression (diverging from ChemistryEngine), and (4) several pieces of dead code accumulated from iterative development.

**Verdict: GREEN — proceed to game-flow phase.** All issues are low/medium severity. Two should be fixed before game-flow work begins (StatusBarPanel suppression, MEMORY.md staleness). The rest are technical debt to track.

---

## 1. Design Document Alignment

### Correctly Implemented
- **Two-dimensional complexity model**: ComplexityLevel enum + ComplexityFilter correctly enforce Simple-only for all DifficultyParams.ForLevel() outputs. Daily/standalone puzzles use scale variation, not mechanical depth — exactly matching chemistry-system.md.
- **Disassemble-a-solution algorithm**: PuzzleGenerator follows the documented flow (generate valid solution → record → remove → present).
- **Volatile net-hot specification**: Correctly implemented per chemistry-system.md. Cold acts as heat shield. Self-reaction bug was caught and fixed.
- **Per-cell bag effects for spanning reagents**: Null suppression applies per-cell, not per-reagent. Verified in ChemistryEngine across all three sensitivity tiers.
- **Shape conventions**: `bool[rows, cols]`, `Vector2I(col, row)`, row-major aspect indexing — all consistent.
- **Adjacency**: Cardinal-only, cell-contact-based. Correctly excludes diagonals and self-adjacency.

### Document Inconsistencies Found

| Issue | Documents | Impact |
|-------|-----------|--------|
| **MEMORY.md stale** | MEMORY.md vs CLAUDE.md | MEMORY.md says Godot 4.3, net6.0, .csproj gitignored — all wrong. Should say Godot 4.6, net8.0, .csproj committed. Misleads any agent reading it. |
| **BagPropertyType.Locked misnomer** | architecture.md vs implementation | Locked is per-reagent (Reagent.IsLocked), not per-bag. The enum value still exists but is unused. architecture.md doesn't reflect this. |
| **ReactionResultType divergence** | architecture.md vs implementation | Doc lists AspectAmplification/AspectSuppression/Transmutation (post-MVP). Impl has Overheating/Hypothermia/Poisoning/ToxicCloud instead. StatusEffect enum value exists but is unused. |
| **AspectRequirement type** | architecture.md vs implementation | Doc specifies an AspectRequirement class with per-aspect min counts. Impl uses flat `List<string> TriggerAspects` + single `Threshold`. Simpler but less expressive. |
| **Serialization format** | architecture.md vs PuzzleState | Shapes use jagged arrays for JSON compat, not 2D arrays. Runtime uses 2D arrays with converters. |

**Recommendation**: Update architecture.md to match implementation for BagPropertyType.Locked, ReactionResultType, and AspectRequirement. These are intentional simplifications, not bugs, but the doc creates confusion. Fix MEMORY.md immediately — it actively misleads agents.

---

## 2. Layer Architecture

### Core Layer (src/Core/) — 28 files

**Zero Godot dependencies: PASS.** No `using Godot;` anywhere. Custom `Vector2I` used correctly.

**Namespace conventions: PASS.** All follow `AlchemistCourier.Core.{System}`.

**Dependency direction issue:**
- `MetaGrid` (Grid namespace) imports `AlchemistCourier.Core.Chemistry` for `AspectIds.Heavy`, `AspectIds.Sharp`, `AspectIds.Fragile` to enforce placement constraints.
- Expected direction: Chemistry → Grid, not Grid → Chemistry.
- Impact: Adding any new placement-affecting aspect requires modifying MetaGrid. The Grid layer "knows" about specific alchemy concepts.
- **Severity: Medium.** Workable for MVP, but should be refactored before Intermediate-tier aspects ship. A `PlacementConstraintEvaluator` in the Chemistry namespace (or a new Constraints namespace) could hold this logic and be invoked by MetaGrid via a delegate/interface.

**GridCell type safety:**
- `BagOwner` and `OccupyingReagent` are `object?` instead of typed references.
- Forces runtime casts (`as Bag`, `is Reagent r`) at 6+ call sites.
- Root cause: avoiding circular dependencies between Grid/Bags/Reagents namespaces.
- **Severity: Low.** Interfaces (`IBagOwner`, `IReagentOccupant`) in the Grid namespace would solve this without introducing circular deps. Not urgent.

### Game Layer (src/Game/) — 13 files

**Core/Game boundary: PASS.** Game layer correctly bridges Core types to Godot nodes. No Core logic re-implemented.

**src/UI/ is empty.** All UI lives in `src/Game/Inventory/`. This is fine for MVP — splitting to src/UI/ would be premature.

### Scene Structure
Two scenes exist: `main.tscn` (PuzzleScreen) and `test_harness.tscn`. Both are minimal root Control nodes with scripts attached. All UI is built procedurally. This is acceptable for MVP but means adding screens (tutorial, daily mode selection, settings) in phase 4 will require either more scenes or a screen-switching mechanism in PuzzleScreen.

---

## 3. Architectural Risks for Game-Flow Phase

### Risk 1: StatusBarPanel cumulative total diverges from ChemistryEngine (HIGH)
`StatusBarPanel.ComputeTotal()` sums aspect magnitudes across all placed reagents but does **not** check for Null-bag suppression. `ChemistryEngine` correctly applies `IsAspectSuppressed()` before counting. Result: status bar shows higher totals than the engine uses, giving players **incorrect risk information** when Null bags are in play.

**Action needed before 4.1**: StatusBarPanel must call `IsAspectSuppressed` (or equivalent) when computing displayed totals. This is a functional bug, not just code quality.

### Risk 2: PuzzleScreen hard-wired to DemoPuzzle
Task 4.2 addresses this. PuzzleScreen._Ready() creates a fixed DemoPuzzle. The Initialize-from-PuzzleState path doesn't exist yet. All game-flow tasks (4.1–4.6) depend on this being flexible. 4.2 should be done early or in parallel with 4.1.

### Risk 3: No screen navigation system
Tasks 4.3–4.5 introduce tutorial screen, daily mode, and screen transitions. Currently there's one scene (main.tscn) with one script (PuzzleScreen). A lightweight screen manager (or scene switching via SceneTree.ChangeSceneToFile) will be needed. This is not explicitly a PRD task but is infrastructure for 4.4 and 4.5.

### Risk 4: Save system scope creep
Task 4.6 spec is appropriately minimal (tutorial progress, daily history, streak). architecture.md describes a much larger schema (codex, settings, daily puzzle history with attempts). Stay with the task spec, not the architecture doc.

---

## 4. Code Quality Issues

### Medium Priority

| File | Issue |
|------|-------|
| `ChemistryEngine.cs:473` | `OrderPair` uses `GetHashCode()` on `Reagent` (no override) — pair deduplication is memory-address-based, non-deterministic between runs. Not a correctness bug (only affects which of two identical reactions is reported), but fragile. |
| `PuzzleScreen.cs` | `ReactionRuleDatabase.GetMvpRules()` called 3 separate times in `BuildUI()`. Cache once in a local variable. |
| `MetaGridView.cs` + `PuzzleScreen.cs` | Escape key handled in both `_Input()` and `_UnhandledKeyInput()` — both fire, causing redundant `CancelSelection()`. Let PuzzleScreen own cancellation exclusively. |

### Low Priority (Dead Code)

| File | Dead Code |
|------|-----------|
| `PuzzleScreen.cs` | `_selectedIndex` field — set but never read |
| `ReagentTrayItem.cs` | `_isSelected` field — set but never read |
| `ObjectivePanel.cs` | `_puzzleType` field — stored but never read |
| `MetaGridView.cs` | `using System.Linq` — unused import |
| `ReagentTrayItem.cs` | `rows`/`cols` in `ShapePreview._Draw()` — declared but unused |
| `PuzzleGenerator.cs:526-534` | `CellCount(bool[,])` duplicates `ShapeUtil.CellCount` |
| `ReactionResultType.cs` | `StatusEffect` enum value — no rule produces it |
| `AspectPrimerPanel.cs:86-88` | `FormatAspects()` has dead branch — both arms produce identical output |

### Low Priority (Robustness)

| File | Issue |
|------|-------|
| `Aspect.cs:59-72` | `FromId()` doesn't handle `AspectIds.Magnetic`/`Absorbent` — they get lowercase-ID display names |
| `PuzzleGenerator.cs` | Unnamed probability constants (0.4f, 0.7f, 0.3f) — should be named |
| `PuzzleGenerator.cs:537-546` | `InferDifficulty()` can never return `Tutorial5` — unreachable switch arm |
| `ChemistryEngine.cs:388` | `involvedCells.Contains()` is O(n) on a List — should be HashSet |

---

## 5. Phase Readiness Assessment

### Completed Phases: All GREEN
- **core-chemistry** (1.1–1.5): Solid. Adjacency, rules, engine, prospective eval all working.
- **puzzle-infrastructure** (2.1–2.4): Solid. Deterministic RNG, puzzle gen with solution validation.
- **ui-chemistry** (3.1–3.5): Working. Warnings, status bar, objective, confirm/reset, primer.
- **bag-mechanics** (B.1–B.4): Working. Gravity, locked, Heavy, non-rectangular shapes.
- **aspect-rework** (A.1–A.6): Working. Net-hot, Sharp, Fragile, Catalytic, Medicinal, ComplexityFilter.
- **review-fix** (R-1–R-7): All resolved.

### Upcoming Phase: game-flow (4.1–4.6)
**Ready to begin with one caveat**: Fix StatusBarPanel Null-bag suppression before or during 4.1 (it affects win/loss evaluation display accuracy).

Recommended task order: 4.2 → 4.1 → 4.3 → 4.4 → 4.5 → 4.6 (wire PuzzleState first, then confirm logic, then tutorial data, then screens, then daily mode, then save).

---

## 6. Recommended Actions

### Fix Now (before game-flow)
1. **StatusBarPanel Null-bag suppression** — functional bug affecting player-facing information
2. **MEMORY.md update** — stale metadata actively misleads agents (Godot version, target framework, .csproj tracking)

### Fix During game-flow
3. **Cache GetMvpRules()** in PuzzleScreen.BuildUI() — trivial fix, do alongside any BuildUI changes
4. **Escape key double-handling** — consolidate to PuzzleScreen only
5. **Dead code cleanup** — remove unused fields/imports/methods in a small cleanup pass

### Track for Later
6. **Grid→Chemistry dependency** — refactor placement constraints out of MetaGrid before Intermediate-tier work
7. **GridCell type safety** — introduce typed interfaces when adding new cell features
8. **Architecture.md sync** — update data model sections to match implementation
