# Code Review — 2026-03-08b
**Commits reviewed:** 3c764b7..63be155 (3 commits)

Substantive code change: `7594d9e` feat(GO.3): MoveBag and RotateBag carry contained reagents

## Findings

### P1 — Data Integrity: Spanning reagents orphaned after bag move/rotate
- **MetaGrid.cs:202 (MoveBag), MetaGrid.cs:271 (RotateBag)** — When a bag moves/rotates, spanning reagents (those crossing multiple bags) are correctly excluded from carrying. However, their cells in the moved bag's old position now have `OccupyingReagent` set but `BagOwner = null`. This creates an inconsistent state — a reagent occupying cells with no bag owner.
  - **Impact:** Downstream code that assumes reagents are always in bags could produce incorrect results (e.g., `GetBagsForReagent`, constraint checks, reaction evaluation).
  - **Fix:** After a successful move/rotate, check remaining (non-contained) reagents in the old cells. If any reagent now has cells outside any bag, force-remove it.
  - **Action:** Created task R3 in prd.json.

### P3 — Small Functions: RotateBag is 88 lines
- **MetaGrid.cs:271-358** — `RotateBag` exceeds the 50-line guideline. The reagent position transform logic (lines 328-353) mixes coordinate arithmetic with high-level bag operations, violating SLAP (Single Level of Abstraction).
  - **Suggestion:** Extract a `RotateReagentWithBag(reagent, origin, oldShapeRows)` helper.
  - **Action:** Noted only. Single occurrence, code is clear enough. Will revisit if the method grows further.

### P4 — Dead Code in test (fixed)
- **MetaGridTests.cs:RotateBag_InvalidRotation_ReagentsUnchanged** — Test created `grid` (4x4), `bag`, `reagent` variables, then abandoned them and created `grid2` (3x4), `bag2`, `reagent2`. The first set was completely unused dead code.
  - **Fix:** Consolidated to a single 3x4 grid. Quick win — fixed directly.

## Positive Observations
- **Clean helper extraction:** `GetFullyContainedReagents`, `ClearReagentCellOccupancy`, `RestoreReagentCellOccupancy` are well-named, focused, and correctly scoped as private.
- **Thorough test coverage:** 12 new tests covering move/rotate carrying, spanning reagents, multiple reagents, invalid destinations, and edge cases (L-bag rotation, domino position transform).
- **Core/ purity maintained:** No Godot imports in MetaGrid.cs.
- **Transactional pattern:** Both methods correctly restore state on validation failure (clear occupancy before validation, restore on failure path).

## Actions Taken
- Fixed dead code in `RotateBag_InvalidRotation_ReagentsUnchanged` test (commit 69baf0e)
- Created task R3 in prd.json for spanning reagent data integrity issue

## Desloppify
- Scan not available in this session (tool requires interactive approval)

## Status
ISSUES_FOUND — 1 P1 task created (R3), 1 P4 fixed, 1 P3 noted
