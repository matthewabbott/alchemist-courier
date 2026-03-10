# Code Review — 2026-03-10b
**Commits reviewed:** d1cb459..3029bd6 (VP.1, VP.2, VP.3)

## Findings

### DRY — Duplicated Aspect Icon Creation Pattern (P2)
- **5 locations** share an identical TextureRect-or-ColorRect pattern for aspect tile icons:
  1. `InfoColumnManager.cs:324-344` — PopulateReagentDetail
  2. `InfoColumnManager.cs:489-509` — RebuildAspectIcon
  3. `AspectLegendPanel.cs:201-221` — BuildEntry
  4. `StatusBarPanel.cs:140-154` — AspectBar.Initialize
  5. `ComponentInspector.cs:390-406` — (pre-existing, not from this diff)
- All share: `ExpandMode.IgnoreSize`, `StretchMode.KeepAspect`, `TextureFilter.Nearest`, 32x32 size.
- **Recommendation:** Extract `UIThemeLoader.CreateAspectIcon(string aspectId, float size)` returning `Control`.

### DRY — Duplicated FormatRule/Capitalize Helpers (P2)
- **4 copies** of `FormatRule()` and `Capitalize()`:
  1. `InfoColumnManager.cs:520-538`
  2. `AspectLegendPanel.cs:416-439`
  3. `FloatingAspectTooltip.cs:163-179`
  4. `AspectPrimerPanel.cs:74-89` (this one is `internal static`)
- **Recommendation:** Consolidate into a shared `AspectFormatHelpers` static class.

### Fail-Fast — Missing Fallback Icon in StatusBarPanel (P1, fixed)
- **`StatusBarPanel.cs:140-154`** — When `GetAspectTileTexture()` returned null, no icon was shown at all. Every other site falls back to a `ColorRect` with the aspect color. This was a behavioral inconsistency that would cause missing icons for aspects without sprites.
- **Fixed directly** — added ColorRect fallback matching all other sites.

### Magic Numbers — Icon Size 32 (P3)
- The value `32` for icon size appears 15+ times across InfoColumnManager, AspectLegendPanel, and StatusBarPanel without a named constant. The R1 task (CreateAspectIcon helper) will centralize this.

## Actions Taken
- **Fixed:** Added ColorRect fallback to `StatusBarPanel.AspectBar.Initialize` (P1) — commit `4873d74`
- **Task created:** R1 in prd.json — Extract shared `CreateAspectIcon` helper and `FormatRule`/`Capitalize` helpers (P2, 5+ violations of DRY, exceeds Rule of Three)

## Desloppify
- Scan: unavailable (tool blocked by permissions)

## Status
ISSUES_FIXED
