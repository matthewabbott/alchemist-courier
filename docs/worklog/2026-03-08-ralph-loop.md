# Ralph Loop — 2026-03-08

## SA.1: SpriteAssigner: load PNG + grid overlay + cell clicking
Built the first sprite-tools feature: a standalone Godot tool scene for assigning polyomino
shapes to sprite art. The tool overlays a configurable grid (4x4, 6x6, or 8x8) on top of a
loaded PNG, lets the user click cells to define a shape, and auto-detects which of the 9
canonical polyomino shapes it matches (trying all 4 rotations). This is the foundation for
the human-in-the-loop art pipeline where generated sprites get matched to game shapes.
Commit: 048fe53

## SA.2: SpriteAssigner: aspect/metadata + preview + JSON export
Extended the Sprite Assigner with the full authoring pipeline: aspect and rarity dropdowns,
item name, flavor text, a 32px game-size preview showing the selected shape at actual cell
resolution, and JSON export producing ready-to-import catalog entries. The batch workflow
clears the selection after export but keeps the PNG loaded, so the artist can quickly try
different shape assignments for the same art. This completes the tool's core functionality
for the human-in-the-loop sprite-to-catalog workflow.
Commit: 8cdd8ce

## GO.1: Soften Heavy constraint from hard block to warning
First task in the gameplay-overhaul phase. The Heavy-above-non-Heavy constraint in gravity
bags was a hard placement gate — CanPlaceReagent returned false. The overhaul design calls
for "invalid states freely allowed during play" with advisory visual warnings instead. Removed
the IsHeavyConstraintValid check from the placement path while keeping GetHeavyViolations()
intact for the HighlightEngine to render warnings. Clean 2-file change, all 757 tests pass.
Commit: dde18aa

## SB.1: Create SandboxScreen with catalog mode
First sandbox feature: a dev tool for browsing all 24 catalog reagents with sprite previews.
Reagents grouped by aspect with colored headers, filterable by aspect and shape. Visual state
toggle cycles through all 7 states (Normal through Exploding), updating sprite renderers or
colored rectangle fallbacks in real time. Adapted from original 72-entry spec to the current
24-item art-first catalog. Playground tab stubbed for SB.2.
Commit: 8ffd21d

## Code Review
Reviewed last 6 commits (SA.1, SA.2, GO.1 + chore). Found and removed dead `IsHeavyConstraintValid`
method left behind after GO.1 softened Heavy to advisory. SpriteAssigner.cs (1070 lines) is large
but acceptable as a first-implementation standalone tool. No P0/P1 issues. 757 tests passing.
Commit: 5d48583

## GO.2: MetaGrid.MoveBag and RotateBag methods
Core infrastructure for the bag-manipulation gameplay. MoveBag repositions a bag's cell ownership
with full validation (bounds, overlap), and supports "sliding" moves where old and new footprints
partially overlap. RotateBag adds a Rotation property to Bag (mirroring Reagent's existing pattern)
and rotates the shape via ShapeUtil, updating cell mapping atomically with rollback on failure.
Both methods reject if the bag has placed reagents — GO.3 will extend this with reagent carrying.
Commit: 5ee4923

## GO.3: Bag movement carries contained reagents
Extended MoveBag and RotateBag to carry fully-contained reagents instead of rejecting when
reagents are present. "Fully contained" means all of a reagent's cells lie within the moving
bag's footprint. Spanning reagents (cells in multiple bags) stay in place. MoveBag translates
reagent PlacedPositions by the bag's delta. RotateBag applies the 90° CW cell transform
(newCol = oldRows-1-row, newRow = col), increments reagent Rotation, and recomputes
PlacedPosition as the min-corner of the transformed cells. Added ClearReagentCellOccupancy /
RestoreReagentCellOccupancy helpers to temporarily unblock validation during bag movement.
7 new tests covering multi-cell carrying, spanning reagent exclusion, multiple reagents,
rotation transform, and rollback on invalid moves. 783 tests passing.
Commit: 7594d9e

## Code Review (b)
Reviewed GO.3 reagent carrying implementation in MetaGrid.cs. Found a P1 data integrity issue:
when a bag moves away from a spanning reagent, the spanning reagent's cells in the old bag position
are left with OccupyingReagent set but no BagOwner — an inconsistent state. Created task R3.
Also cleaned up dead code in RotateBag_InvalidRotation test (unused grid/bag/reagent variables).
RotateBag at 88 lines noted as slightly long (P3) but not actionable yet. 783 tests passing.
Commit: 69baf0e

## R3: Handle spanning reagents when bag moves/rotates away
Fixed the P1 data integrity issue from the code review. When MoveBag or RotateBag succeeds,
any spanning reagent left behind now gets force-removed if its cells end up outside any bag.
Added RemoveOrphanedReagents() which scans all non-carried reagents for cells with null
BagOwner, and ForceRemoveReagent() which bypasses the IsLocked check since the bag itself
is gone. Key design choice: only remove reagents with actually-orphaned cells, not all
spanning reagents — a square bag rotation (SmallPouch) doesn't change cell coverage, so a
spanning reagent that still has all cells in bags stays put. 789 tests passing.
Commit: 4695d2d

## GO.4: Invalid placements after bag movement — locked reagent protection
Added a pre-check to MoveBag and RotateBag that rejects the operation if it would orphan a
locked spanning reagent. Before any state is modified, HasLockedOrphanedReagent computes
which cells the bag would "abandon" (old footprint minus new footprint) and scans for locked
reagents in those cells. This protects puzzle integrity — locked reagents placed by the hint
system or puzzle setup can't be silently removed by bag movement. Non-locked spanning
reagents continue to be removed per R3. The pre-check pattern avoids complex rollback by
rejecting before state mutation. 795 tests passing.
Commit: 24d0d89

## GO.5: Cross-bag reagent placement verification tests
Regression test suite confirming cross-bag placement works correctly after all the B.1-B.3
bag movement changes. Seven tests cover the full matrix: spanning adjacent bags succeeds,
non-adjacent bags (gap) rejected, spanning reagent removed on bag move away, spanning
survives overlapping move where cells are still covered, new placement after moving bags
together, and rotation orphan cleanup. A nice edge case: when a Satchel (3r×4c) slides
down one row, a spanning domino at the boundary survives because both its cells remain
within their respective bags. 802 tests passing.
Commit: 96fadc0

## Code Review (c)
Reviewed R3, GO.4, GO.5 commits (63be155..888fdfd). Code is clean — no P0/P1/P2 issues found.
HasLockedOrphanedReagent pre-check pattern is well-designed (rejects before state mutation).
RotateBag growing to ~100 lines noted as P3 but not actionable. 802 tests passing.

## GO.6: Gravity direction indicator arrow on gravity bags
Added a procedural purple triangle arrow to MetaGridView for gravity bags. Overrides _Draw()
to draw a filled polygon centered on each bag that has BagPropertyType.Gravity, pointing in
the bag's GravityDirection. Simple and clean — no new child nodes, just DrawPolygon with three
vertices computed from the bag's bounding box center and direction enum. Semi-transparent purple
matches the dev art style. Supports all four directions.
Commit: ce08417

## GO.7: Fragile damage visual indicator (pre-confirm)
First task in the "live violation feedback" series: fragile reagents now show a red tint overlay
on affected cells when they would be damaged by the current arrangement (crushed by heavy,
thermal fracture from hot+cold adjacency, or pierced by sharp). Created DamageOverlay following
the HighlightOverlay pooled-ColorRect pattern, wired into MetaGridView.RefreshDisplay() via
ViolationTracker.ComputeAll(). The overlay updates automatically on every placement/removal
since RefreshDisplay() is called by RefreshAfterGridChange() in both PuzzleScreen and WebDemoScreen.
Commit: 69b4f6a
