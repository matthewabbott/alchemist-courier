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
