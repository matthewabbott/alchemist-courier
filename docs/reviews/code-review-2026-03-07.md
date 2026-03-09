# Code Review — 2026-03-07
**Commits reviewed:** 966e9f0..bb6c93b (HF.1, HF.2)

## Findings

### DRY — GetPrimaryAspectId duplicated 3 times (P2)
- **HoverFrameResolver.cs:56-63** — private `GetPrimaryAspectId(ReagentDefinition)`
- **HoverFrameOverlay.cs:248-254** — private `GetPrimaryAspectId(Reagent)`
- **DevSpriteGenerator.cs:215-219** — private `GetPrimaryAspect(ReagentDefinition)`

All three extract `def.AspectsPerCell[0].Aspects[0].Id` with null/empty guards. Identical logic, three locations. Natural home is `ReagentDefinition.PrimaryAspectId` as a computed property.

### Design Note — Frame key rotation mismatch (P3)
- **HoverFrameResolver.GetFrameKey** returns keys like `"hot_monomino_r0"` (includes rotation)
- **DevSpriteGenerator.GenerateFrameTextures** produces files named `"hot_monomino.png"` (no rotation)
- **HoverFrameOverlay.CreateFrameVisuals** builds its own `{aspect}_{shape}` key, bypassing the resolver

The resolver's `_r{rotation}` suffix is unused by any texture consumer. This is either premature (rotation-specific textures aren't generated yet) or the resolver's key format should drop rotation for texture lookup. Not a bug — the overlay correctly handles it — but the API is confusing. Noting for future alignment when rotation-specific frames are added.

### Single Source of Truth — CellSize duplication (P3)
- **GridConstants.CellSize = 32** (line 9)
- **DevSpriteGenerator.CellSize = 32** (line 22)

Both define the same 32px cell size independently. Minor concern since the generator is a standalone dev tool that may intentionally decouple from grid display constants.

### Unnecessary cast — GetChildren() (P4)
- **HoverFrameOverlay.cs:226** — `if (child is Node node)` — `GetChildren()` always returns `Node` in Godot 4.x. The cast is defensive but redundant.

## Actions Taken
- **Fixed:** Extracted `ReagentDefinition.PrimaryAspectId` property, removed 3 private duplicates across HoverFrameResolver, HoverFrameOverlay, and DevSpriteGenerator. Removed unused `using AlchemistCourier.Core.Chemistry` from HoverFrameResolver. Commit: a5a6a07.
- **No tasks created.** Remaining findings are P3-P4 (report only).

## Desloppify
- Before: 100.0/100
- After: 100.0/100 (maintenance mode)
- Top issue: none (only workflow step to communicate score)

## Status
ISSUES_FIXED
