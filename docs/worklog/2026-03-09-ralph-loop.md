# Ralph Loop — 2026-03-09

## GJ.1: Soften gravity constraint — unsupported placement + auto-fall
Gravity was previously a hard placement gate — CanPlaceReagent rejected unsupported positions in gravity bags. This made the UX rigid (you had to place bottom-up). Now gravity is advisory: placement succeeds anywhere, GetGravityViolations reports warnings, and PlaceReagentWithFall auto-drops reagents to their resting position. Also removed the Heavy-only gate in MetaGridView so ALL reagents in gravity bags show fall previews and auto-fall, not just Heavy ones. This aligns with the game jam goal of making placement feel more natural and forgiving.
Commit: 40f9824

## GJ.2: Debug + fix volatile explosion indicator
Traced the full volatile explosion indicator chain: ViolationTracker → ChemistryEngine → DamageOverlay → confirm flow. Everything in the core and rendering was correct — the test harness proved the overlay works after placement. The real bug was a one-liner introduced by GJ.1's gravity changes: MetaGridView.UpdateGhostPreview called EvaluateProspective with the raw hover position instead of the gravity-adjusted effectivePos, so the ghost preview showed "safe" even when the reagent would fall next to a hot reagent and explode. Classic case of updating the placement path but missing the prospective check path. Added two net-hot threshold tests as well.
Commit: 1fdd2a0

## GJ.3: Contextual info column — InfoColumnManager
The fixed info column (always showing Objective + Legend + Violations) has been replaced with a contextual system. InfoColumnManager is a VBoxContainer that manages three mutually-exclusive views. The default PuzzleOverview shows the same three panels as before. Clicking a reagent in the tray or on the grid switches to ReagentDetail, showing the reagent's shape, name, flavor text, and aspect breakdown — all the info that was previously in a floating popup. Clicking an aspect in the legend switches to AspectDetail, showing aspect description, reactions, and which tray reagents carry it. Back button or Escape returns to the overview. This eliminates the floating ReagentProfilePanel popup (with its ZIndex=100, fade animations, and viewport clamping) in favor of stable embedded content in the sidebar. Cleaner UX, especially at narrow viewport widths.
Commit: 6533bca
