# Architectural Review — 2026-03-04e

**Reviewer:** Claude (architecture review agent)
**Branch:** dev
**State:** 356 unit tests passing, 14/14 integration tests passing
**Phase:** game-flow complete (4.1–4.6 done), polish (5.1) pending

---

## Executive Summary

The project is in excellent shape for MVP delivery. Core layer purity is fully enforced (zero Godot dependencies), the puzzle generation and chemistry systems are well-aligned with design docs, and test coverage is comprehensive. Three issues warrant attention before the polish phase: one correctness bug (StatusBarPanel suppression), one performance optimization (ghost preview), and one documentation clarity issue (Medicinal wording).

**Overall Grade: A-**

---

## 1. Layer Purity — PASS

### Core Layer (src/Core/)
- **35 C# files**, zero `using Godot;` statements — fully engine-agnostic
- Custom `Vector2I` struct correctly replaces Godot's type
- All namespaces follow `AlchemistCourier.Core.{System}` pattern
- Only `System.*` imports used throughout

### Game Layer (src/Game/)
- **14 files** properly bridge Core to Godot via `using Godot;` + `AlchemistCourier.Core.*`
- No back-imports from Game into Core
- Procedural UI construction in `src/Game/Inventory/` is clean and consistent

### Test Layer (tests/)
- **21 test files**, 356 tests — depends only on Core, no Godot imports
- Can compile and run independently of Godot engine

**Verdict:** Layer boundaries are clean and well-enforced.

---

## 2. Dependency Analysis

### Known Circular Dependency: Grid ↔ Chemistry

**Status:** Still present, confirmed as documented in CLAUDE.md.

**The cycle:**
- `MetaGrid.cs` line 4: `using AlchemistCourier.Core.Chemistry;` — needed for `IsHeavyConstraintValid()`, `IsSharpConstraintValid()`, `IsFragileConstraintValid()`
- `ChemistryEngine.cs` line 5: `using AlchemistCourier.Core.Grid;` — needed for MetaGrid evaluation and Vector2I in ReactionResult

**Assessment:** This is an acceptable design tradeoff for MVP. The constraint checks in MetaGrid need Chemistry aspect IDs, and Chemistry needs Grid spatial data. C# compiles this fine, and both modules are tested together anyway.

**Future fix path:** Extract aspect ID constants to a shared `Core.Shared` or `Core.Aspects` module that both Grid and Chemistry can depend on without circular imports.

### Full Dependency Graph (correct)
```
Vector2I (leaf)
  └→ Bags → Grid
  └→ Reagents → Grid, Chemistry
  └→ Grid/MetaGrid → Bags, Chemistry, Reagents  ←──┐
  └→ Chemistry → Grid, Bags, Reagents            ───┘ (circular)
  └→ Puzzle → Grid, Bags, Chemistry, Reagents
  └→ Save (independent, no inter-Core deps)
```

**No other violations found.** Game never imports back into Core, tests depend only on Core.

---

## 3. Design Doc Alignment

### Chemistry System — Mostly Aligned

| Rule | Doc Spec | Implementation | Status |
|------|----------|---------------|--------|
| Net-hot Volatile | sum(Hot) - sum(Cold) > threshold → explosion | ChemistryEngine adjacent eval | CORRECT |
| Null suppression | Per-cell, only cells in Null bag | IsAspectSuppressed() check | CORRECT |
| Stable buffering | +1 to adjacent thresholds | GetAdjacentAspectMagnitude() | CORRECT |
| Catalytic | -1 to adjacent thresholds | GetAdjacentAspectMagnitude() | CORRECT |
| Heavy/Sharp/Fragile constraints | Placement validation | MetaGrid constraint methods | CORRECT |
| Medicinal | "Passively reduces cumulative thresholds" | Adds to effective threshold (protective) | SEE ISSUE #3 |
| Complexity tiers | Simple (6 aspects), Intermediate (+4), Advanced (+2) | ComplexityFilter | CORRECT |

### Puzzle Generation — Aligned

- Disassemble-a-solution algorithm: implemented correctly
- Deterministic seeding (YYYYMMDD format): correct
- Difficulty rotation (3-day cycle): correct per spec
- All daily puzzles use Simple complexity: enforced via ComplexityFilter

### Save System — Aligned

- Tutorial progress tracking: correct
- Daily completion + streak: correct
- JSON serialization: correct (System.Text.Json)
- File-based persistence with web localStorage path documented

---

## 4. Issues Found

### ISSUE 1 — StatusBarPanel Missing Null-Bag Suppression (CORRECTNESS BUG)

**File:** `src/Game/Inventory/StatusBarPanel.cs` lines 65-95
**Severity:** HIGH — affects player-facing information accuracy

The `ComputeTotal()` method sums aspect magnitudes across all placed reagents but does **not** check whether aspects are suppressed by Null bags. A reagent with a Hot cell inside a Null bag will still count toward the Heat threshold bar.

```csharp
// Current code (line 73-76):
foreach (var pos in reagent.GetCurrentMetaGridPositions())
{
    total += reagent.GetAspectsAtMetaGridPosition(pos).GetMagnitude(aspectId);
    // Missing: skip if grid cell's bag is Null
}
```

**Fix:** Check `_grid.GetCell(pos)?.OwnerBag?.Property == BagPropertyType.Null` and skip those cells. The ChemistryEngine already has `IsAspectSuppressed()` as a reference implementation.

**Impact:** Status bars show inflated totals when Null bags suppress aspects, misleading the player about proximity to dangerous thresholds. This directly contradicts the Null bag's core gameplay purpose.

---

### ISSUE 2 — Ghost Preview Recalculated Every Pixel (PERFORMANCE)

**File:** `src/Game/Inventory/MetaGridView.cs` lines 297-310
**Severity:** MEDIUM — could exceed 8ms placement budget on larger grids

`HandleMouseMotion()` calls `UpdateGhostPreview()` on every mouse motion event, even when the cursor hasn't moved to a new grid cell. Each call:
1. Destroys all ghost ColorRects (`ClearGhost()`)
2. Runs `ChemistryEngine.EvaluateProspective()` (temporary place, full eval, remove)
3. Creates new ColorRects for every cell in the shape

**Fix:** Cache `_lastHoverGridPos` and skip `UpdateGhostPreview()` when the cell hasn't changed:
```csharp
if (gridPos.Equals(_lastHoverGridPos)) return;
_lastHoverGridPos = gridPos;
UpdateGhostPreview();
```

**Impact:** On a 60fps display with mouse movement, this could fire 60+ chemistry evaluations per second unnecessarily. The fix is a one-line guard.

---

### ISSUE 3 — Medicinal Wording Ambiguity (DOCUMENTATION)

**File:** `docs/chemistry-system.md` line 88, 129
**File:** `src/Core/Chemistry/ChemistryEngine.cs` line 356-359
**Severity:** LOW — code behavior is correct, documentation is misleading

The doc says Medicinal "reduces cumulative thresholds" which reads as lowering the threshold number (making reactions trigger easier — harmful). The code does the opposite: it **raises** the effective threshold (protective), which is correct for a healing aspect.

The code comment (line 356) correctly says "raises cumulative thresholds."

**Fix:** Update chemistry-system.md line 88 and 129 to say "raises cumulative thresholds" or "reduces cumulative aspect accumulation toward thresholds."

---

### ISSUE 4 — ReactionRules Fetched Redundantly (MINOR)

**File:** `src/Game/Inventory/PuzzleScreen.cs` line 385
**Severity:** LOW — no functional impact, minor inefficiency

`ConfirmPuzzle()` calls `ReactionRuleDatabase.GetMvpRules()` each time confirm is pressed. The rules are already cached in `_reactionRules` at initialization (line 148 area). The confirm path should use the cached field.

---

### ISSUE 5 — Test Harness State Coupling (MAINTENANCE)

**File:** `src/Game/Testing/TestHarness.cs`
**Severity:** LOW — not a bug, but increases fragility

Tests run sequentially and depend on state from previous tests (e.g., `Test_PlaceFirePebble` places a reagent that `Test_PlaceIceShardRotated` expects to be there). If an early test fails, all subsequent tests may cascade-fail.

**Recommendation for future:** Add `ResetPuzzle()` calls at the start of tests that need clean state.

---

### ISSUE 6 — No Save Schema Versioning (FUTURE-PROOFING)

**File:** `src/Core/Save/SaveData.cs`, `SaveManager.cs`
**Severity:** LOW — no issue for MVP, but important before public release

`SaveData` has no version field. If the schema changes after players have saved data, old saves will silently deserialize with default values (or fail). Adding a `Version` int field now is trivial and prevents migration headaches later.

---

## 5. Architecture Strengths

1. **Engine independence is real and tested.** 356 unit tests run without Godot. This is rare for game projects and enables fast CI/CD.

2. **Complexity tier system is well-designed.** The two-dimensional model (scale vs. mechanical depth) elegantly separates daily puzzle difficulty from sequential mode progression.

3. **Deterministic everything.** Seeded RNG, disassemble-a-solution generation, and reproducible puzzles are all correctly implemented. Daily puzzles will be consistent across platforms.

4. **Chemistry engine is clean.** The 3-tier evaluation (Adjacent/SameBag/WholeInventory) with modifiers (Stable/Catalytic/Medicinal/Null) handles the full design spec without over-engineering.

5. **Test harness is excellent for an agent-driven workflow.** Structured stdout, screenshots, and programmatic control APIs enable automated verification loops.

---

## 6. Recommendations Summary

| Priority | Issue | Action | Effort |
|----------|-------|--------|--------|
| P0 | StatusBarPanel suppression | Add Null-bag check in ComputeTotal() | ~30 min |
| P1 | Ghost preview optimization | Guard against same-cell recalculation | ~15 min |
| P1 | Medicinal doc wording | Update chemistry-system.md lines 88, 129 | ~5 min |
| P2 | Cached reaction rules | Use field instead of re-fetching in ConfirmPuzzle() | ~5 min |
| P2 | Save schema versioning | Add Version field to SaveData | ~20 min |
| P3 | Test state isolation | Add ResetPuzzle() to test starts | ~1 hour |
| Deferred | Grid↔Chemistry cycle | Extract shared aspect constants module | Post-MVP |

---

## 7. Verdict

**Ready for 5.1 (UI Polish) phase.** The architecture is sound, conventions are followed, and the codebase aligns well with design documents. The StatusBarPanel suppression bug (Issue #1) should be fixed before merge to master as it directly affects gameplay correctness. The ghost preview optimization (Issue #2) is recommended but not blocking. All other issues are low-priority refinements.

The project is in strong shape for MVP delivery.
