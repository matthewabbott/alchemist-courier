# Code Review — 2026-03-05d
**Commits reviewed:** a7a119e..6e0632c..1f75d7c (H.9 through H.11)

## Findings

### Dead Code (YAGNI)
- **ItemDetailPanel.cs:15** — Unused constant `PreviewCellSize = 16`. The inner `DetailShapePreview` class defines its own `CellSize = 16` constant and `PreviewCellSize` is never referenced.
  Before: `private const int PreviewCellSize = 16;`
  After: *(removed)*
  Priority: P3 (quick win → P2)

### Stale Comments (Documentation Discipline)
- **ItemDetailPanel.cs:64** — Comment says "bold white" but no bold styling is applied.
  Before: `// Name label (bold white)`
  After: `// Name label`
  Priority: P4
- **ItemDetailPanel.cs:71** — Comment says "italic gray" but no italic styling is applied.
  Before: `// Flavor text (italic gray)`
  After: `// Flavor text`
  Priority: P4

### Not Flagged (Considered & Dismissed)
- **ShapePreview._Draw() / DetailShapePreview._Draw()**: Nearly identical drawing logic differing only by cell size constant. Rule of Three: only 2 occurrences, don't abstract.
- **Aspect iteration in BuildAspectBreakdown vs BuildAspectDots**: Similar iteration patterns but different purposes (aggregate magnitudes vs collect unique IDs). Incidental similarity, not duplication.
- **Procedural UI construction style**: Long constructors with inline StyleBoxFlat creation. This is the established pattern for the project's procedural UI — consistent with existing code in PuzzleScreen, StatusBarPanel, etc.

## Actions Taken
- Fixed: Removed unused `PreviewCellSize` constant and corrected two stale comments (commit 9c072a4)
- No tasks created in prd.json — no P0-P1 issues found

## Status
ISSUES_FIXED
