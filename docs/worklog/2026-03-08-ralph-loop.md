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
