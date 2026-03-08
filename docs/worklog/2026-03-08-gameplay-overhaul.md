# Session: Gameplay Overhaul — A.3, D.2, J.3 + Prior Session Commits

**Date:** 2026-03-08
**Branch:** `dev`

## Summary

Continued implementing the gameplay & UI overhaul plan (logical-crunching-mochi). Completed 3 new tasks (A.3, D.2, J.3) and committed all prior session work (D.1, A.1, A.2, C.1, I.1-I.4) that had accumulated uncommitted across multiple sessions. Test count: 700 → 757 (57 new tests, 0 failures).

This session was a continuation of a conversation that ran out of context. The prior conversation had completed I.2 (neutralization removal + hot/cold balance), I.1 (solution count restriction), and started A.3 (data model only). This session finished A.3 (MetaGrid methods), implemented D.2 (thermal fracture + crushing), and created J.3 (ViolationTracker core).

## Changes

### A.3 — Per-bag gravity direction
- `src/Core/Bags/GravityDirection.cs` — NEW: GravityDirection enum (Down/Up/Left/Right) with ToVector() and RotateCW()
- `src/Core/Bags/BagDefinition.cs` — Added GravityDirection property (default Down)
- `src/Core/Bags/Bag.cs` — Added GravityDirection convenience property
- `src/Core/Grid/MetaGrid.cs` — GetGravityViolations(), GetHeavyViolations(), CalculateFallPosition() all use bag's gravity direction. New GetGravityDirectionForPositions() helper.
- `tests/.../Grid/MetaGridTests.cs` — 27 new tests: bearing cells with Up/Left/Right gravity, violations in all 4 directions, fall positions, heavy violations, enum extensions, BagDefinition/Bag properties.

### D.2 — Thermal fracture and crushing for fragile
- `src/Core/Chemistry/ReactionResultType.cs` — Added ThermalFracture, Crushing
- `src/Core/Chemistry/ChemistryEngine.cs` — New EvaluateFragileThermalFracture() (hot+cold adjacency) and EvaluateFragileCrushing() (heavy bearing cells on fragile). Both called in second pass before existing EvaluateFragileVulnerability.
- `tests/.../Chemistry/ChemistryEngineTests.cs` — 15 new tests: thermal fracture with/without both hot+cold, nullified fragile/hot, multi-cell fragile. Crushing with heavy/non-heavy/buffer/nullified/left-gravity/multiple.

### J.3 — ViolationTracker (core)
- `src/Core/Highlight/ViolationType.cs` — NEW: ViolationType enum (8 types)
- `src/Core/Highlight/ViolationEntry.cs` — NEW: Unified violation entry with Type, Description, Severity, AffectedCells, AspectId
- `src/Core/Highlight/ViolationTracker.cs` — NEW: Static ComputeAll() aggregating bag balance + chemistry reactions into List<ViolationEntry>
- `tests/.../Highlight/ViolationTrackerTests.cs` — NEW: 15 tests covering all violation types and properties

### Prior session work (committed this session)
- Web demo scene, floating tooltips, hover debounce, aspect legend, grid constants, MetaGridView updates
- I.1: MaxSolutions, solution count check, distinct rotation deduplication
- I.2: Neutralization removal, BagBalanceViolation, per-bag balance checks
- D.1: Fragile allowed in gravity bags
- C.1: Aspect outlines rotate with reagent
- I.3: Toxic removed from puzzle set
- I.4: Full nullification

## Design Decisions

1. **Crushing only in gravity-direction**: Crushing uses the bag's gravity direction (or default Down for non-gravity bags) to determine which cells are "below" heavy bearing cells. Non-heavy objects between heavy and fragile act as protective insulation.

2. **Heavy constraint vs. crushing tension**: In gravity bags, the Heavy constraint blocks placing heavy above non-heavy (hard placement constraint). This means crushing in gravity bags can only be tested when the heavy constraint is relaxed. Tests use normal bags for basic crushing scenarios. Future work should soften the Heavy constraint to a warning (matching the plan's "invalid states freely allowed during play").

3. **Thermal fracture scans entire footprint**: A multi-cell fragile reagent checks all its cells' adjacencies for hot AND cold presence. Hot on one end + cold on the other end still triggers fracture.

4. **ViolationTracker is stateless**: ComputeAll() computes everything fresh each call — no caching. This matches the existing HighlightEngine pattern. UI layer should call it on each state change.

## Commits

| Hash | Message |
|------|---------|
| `8aa84cc` | feat: Add web demo scene, tooltips, and UI infrastructure (D.1, C.1, I.3, I.4) |
| `6bc473c` | feat: I.1 solution count restriction + I.2 per-bag hot/cold balance |
| `e613b1c` | feat(A.3): Per-bag gravity direction for fall, support, and heavy checks |
| `77ad9f1` | feat(D.2): Thermal fracture and crushing for fragile reagents |
| `361c6d9` | feat(J.3): ViolationTracker for live placement violation aggregation |

## Known Issues

1. **Heavy constraint is too strict for crushing**: The Heavy placement constraint (`GetHeavyViolations` → blocks `CanPlaceReagent`) prevents placing heavy above non-heavy in gravity bags. The plan says "invalid states freely allowed during play" — this constraint should be softened to a warning. Without this, crushing can only practically occur in non-gravity bags.

2. **art/manual_generated/ not committed**: Experiment art PNGs from prior session are untracked. Not relevant to gameplay.

## Next Steps

- Re-enter plan mode to assess remaining 21 tasks and plan Ralph loop
- Core-testable tasks remaining: B.1-B.4 (bag movement), A.5 (rotatable gravity bags)
- UI/Godot tasks: D.3, E.1, F.1, G.1-G.4, H.1-H.4, J.1, J.2, J.4 (need game layer work)
- Consider softening Heavy constraint from hard block → warning (prerequisite for realistic crushing)
- Prepare Ralph loop prompts for overnight autonomous implementation
