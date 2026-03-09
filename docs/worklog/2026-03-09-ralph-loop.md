# Ralph Loop — 2026-03-09

## GJ.1: Soften gravity constraint — unsupported placement + auto-fall
Gravity was previously a hard placement gate — CanPlaceReagent rejected unsupported positions in gravity bags. This made the UX rigid (you had to place bottom-up). Now gravity is advisory: placement succeeds anywhere, GetGravityViolations reports warnings, and PlaceReagentWithFall auto-drops reagents to their resting position. Also removed the Heavy-only gate in MetaGridView so ALL reagents in gravity bags show fall previews and auto-fall, not just Heavy ones. This aligns with the game jam goal of making placement feel more natural and forgiving.
Commit: 40f9824
