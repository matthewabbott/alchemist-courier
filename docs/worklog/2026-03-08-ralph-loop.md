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

## GO.8: Volatile explosion indicator (pre-confirm)
Extended the DamageOverlay from GO.7 to also render volatile explosion warnings. Since
ViolationTracker already detected VolatileExplosion violations, this was a minimal change —
add the violation type to the filter and give it an orange tint distinct from the red fragile
colors. No volatile placement blocking existed to remove; the design already allows free
placement of dangerous combinations during play. Two-file, ten-line diff.
Commit: bd232fb

## GO.9: Error highlight pulse on confirm with violations
When the player confirms with active violations (explosions, breakage, etc.), the affected
cells now pulse red 3 times over 1 second before the reaction animation plays. This creates
a clear visual beat — "here's what's wrong" (pulse) → "here's what happens" (explosion anim)
→ "you failed" (result overlay). Added PulseViolations() to DamageOverlay using Godot Tween
with modulate alpha oscillation, and a MetaGridView facade that computes violations and
delegates. The pulse chains to ReactionAnimator via callback in both PuzzleScreen and
WebDemoScreen. Clean 67-line change.
Commit: 06046db

## GO.10: Bag state visual indicators (smoldering/icy)
Bags now show live thermal state — a flickering red/orange tint when too hot (more Hot than
Cold magnitude), or a blue tint when too cold. Uses ChemistryEngine.GetBagBalanceViolations()
for per-bag imbalance detection and a new BagStateOverlay class following the DamageOverlay
pattern. The overlay groups rects by bag so _Process() can efficiently update all cells per
group with a smooth sine-wave alpha oscillation (2s period, 0.08–0.25 range). Balanced bags
show no overlay. Placed at ZIndex 2 so the tint sits below hover/highlight/damage overlays.
Commit: e2f390d

## Code Review (d)
Reviewed commits 55aea9c..660f6e9 (GO.11, volatile/bag-state/bag-movement integration tests).
Found and fixed one P2 DRY violation: OnResetPressed and OnHintPressed in WebDemoScreen duplicated
the 5-line refresh sequence already in RefreshAfterGridChange(). Replaced with method calls. 802 tests pass.

## GO.11: ViolationListPanel for live violation display
Added a live violations panel to the WebDemoScreen sidebar — it shows all current placement
issues (explosions, bag imbalances, breakage, etc.) in real time as reagents are placed and
removed. Each entry is color-coded by severity and clickable (emitting a signal for future
GO.12 cell-highlight integration). The panel uses ViolationTracker.ComputeAll() which was
already built as Core infrastructure, so this was a clean Game-layer-only wiring task.
Commit: 55aea9c

## GO.12: Violation click highlights affected cells
Completed the violation panel interaction loop: clicking a violation entry now highlights
the affected cells on the grid with contiguous boundary edges and marks the involved reagents
in the tray. The toggle pattern follows the existing legend aspect selection — clicking the
same violation clears, clicking another replaces. Escape clears everything. The highlight
computation lives in Core (HighlightEngine.ComputeViolationHighlights) for testability, with
4 new unit tests covering edge boundary correctness and empty/single/adjacent cell cases.
Commit: ad79975

## LO.1: Public panel accessors on WebDemoScreen
First task in the layout-overhaul phase. Added public read-only properties to WebDemoScreen
so upcoming layout tests and integration tests can programmatically inspect panel state.
Straightforward accessor work mirroring the existing PuzzleScreen pattern — no behavioral
changes, just exposing the private fields that were already there.
Commit: b650c09

## LO.2: Layout test infrastructure — LayoutTestHelper + WebDemoLayout test
Built the foundation for layout regression testing. LayoutTestHelper provides four reusable
assertions (CheckMinSize, CheckVisible, CheckNoOverlap, CheckContainedIn) that return
structured failure reasons — designed to be composed into any integration test. The first
consumer is Test_WebDemoLayout, which instantiates a WebDemoScreen and verifies that all
key panels (grid, tray, status bar, legend) are visible and correctly sized after Godot's
layout engine settles. This gives us a baseline before the bigger layout changes in LO.4+.
Commit: 89053de

## SB.2: Add playground mode to SandboxScreen
Added the Playground tab to complete the two-mode SandboxScreen. The catalog tab remains
unchanged; the playground offers a free-form 12x12 grid where users can draw custom bags
via the reused BagEditorPanel, pick reagents from a catalog picker, and experiment without
win/lose conditions. Confirm triggers the reaction animator for visual feedback. Undo/Redo
(with keyboard shortcuts) is wired through ActionHistory. Lazy initialization means the
playground only builds its UI on first tab switch, keeping catalog-only usage fast.
Commit: b40fc35

## Code Review (e)
Reviewed LO.1, LO.2, SB.2 commits. Found two DRY violations in SandboxScreen.cs: primary
aspect extraction duplicated 5 times (extracted to GetPrimaryAspectId helper) and card
StyleBoxFlat creation duplicated 4 times (extracted to CreateCardStyle helper). Net -17 lines.
LayoutTestHelper and LO.1 accessors were clean. 806 tests passing.
Commit: bd76392
