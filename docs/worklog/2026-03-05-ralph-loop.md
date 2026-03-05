# Ralph Loop — 2026-03-05

## R-8: Fix StatusBarPanel Null bag suppression bug
StatusBarPanel was counting aspect magnitudes from reagents in Null bags, making cumulative
status bars show inflated totals that didn't match ChemistryEngine's actual evaluation (which
correctly suppresses Null bag cells). Made ChemistryEngine.IsAspectSuppressed() public so
StatusBarPanel can reuse the same check, keeping the suppression logic DRY across the codebase.
Commit: 1a11d94

## H.1: Core highlight data types
First step of the highlight-system phase: pure data types in src/Core/Highlight/. CellEdge is a
[Flags] enum for contiguous boundary rendering (only outer edges of a highlight region get drawn).
HighlightLayer separates constraint violations, interaction risks, legend selections, and ghost
overlays into independent rendering layers. HighlightSet is the collection class that downstream
engines will populate and renderers will consume, with position and layer query methods. This
unblocks H.3–H.8 which build the actual highlight logic on top of these types.
Commit: 6c6ed55

## H.2: Aspect metadata database
Extended the chemistry system with a metadata layer for the upcoming legend panel and tooltips.
Each of the 10 MVP aspects now has a human-readable description and optional constraint text
(Sharp can't go on edges, Fragile can't go in Gravity bags, Heavy must sink in Gravity bags).
GetForPuzzle() intelligently filters to only aspects relevant to a specific puzzle, avoiding
information overload. Also added FlavorText to ReagentDefinition for thematic item descriptions
— DemoPuzzle reagents now have short lore blurbs while generated puzzles keep FlavorText null.
Commit: 2c80550

## H.3: Contiguous boundary utility
Pure geometry utility for computing which edges of a cell group form the outer boundary — the
key primitive for non-obstructive highlight rendering. Given a set of cell positions, it checks
each cell's four cardinal neighbors: absent neighbors mean boundary edges. This will be consumed
by HighlightOverlay (H.7) to draw highlight regions as outlined borders rather than filled
rectangles, keeping placed reagents visible beneath highlights.
Commit: 2da34f1

## Code Review
Reviewed 3 commits (1a11d94..2c80550): R-8 StatusBarPanel suppression fix, H.1 highlight data
types, H.2 aspect metadata database. All clean — no P0-P2 issues. Code follows conventions:
no Godot deps in Core, proper dependency direction (Game→Core), minimal data types without
over-engineering. 405 tests passing. Minor P3 notes: GetAll() allocates on each call (low
impact), immutable data classes could be records (style preference only).

## H.4: Constraint violation detail extraction
Refactored MetaGrid's four constraint validation methods from simple bool returns to rich
ConstraintViolation objects with type, affected cells, reason text, and aspect ID. The original
bool methods now delegate to the new Get*Violations methods, keeping backward compatibility
while unlocking the detail H.5's HighlightEngine needs to render constraint highlights with
per-cell granularity. Clean refactor — no logic changes, just information surfacing.
Commit: 3f4e36f

## H.5: HighlightEngine foundation with constraint highlights
The first real computation layer of the highlight system. HighlightEngine reads constraint
violations from MetaGrid (Sharp-on-edge, Fragile-in-Gravity, Heavy-above-non-Heavy, Gravity-
unsupported) and converts them into CellHighlight entries with appropriate severity and boundary
edges via ContiguousBoundary. Sharp violations get Danger severity (hard block); the others get
Warning (softer guidance). StatusThresholds tracks cumulative aspect levels for status bar
integration — mapping current/threshold pairs to Safe/Warning/Danger severity. Both are pure Core
classes ready for H.6 (interaction highlights) and H.7 (rendering overlay) to consume.
Commit: ed2b4b1

## Code Review (b)
Reviewed 3 commits (2da34f1..119d125): H.3 ContiguousBoundary, H.4 constraint violation
extraction, H.5 HighlightEngine + StatusThresholds. All clean — no P0-P2 issues. MetaGrid's
constraint refactoring (private bool → public List<ConstraintViolation>) is well-executed with
thin expression-body wrappers preserving the CanPlaceReagent API. ContiguousBoundary is clean
with proper IReadOnlyCollection input. 445 tests passing.

## H.6: HighlightEngine interaction, legend, and eagle sense methods
The highlight engine now knows about chemistry interactions, not just physical constraints.
ComputeInteractionHighlights derives reactive aspect pairs from Adjacent rules — 2-trigger rules
like Sharp+Fragile yield direct pairs, while the single-trigger Volatile rule generates Hot and
Cold pairs for the net-hot mechanic. This means the highlight system automatically adapts when
new reaction rules are added. Eagle sense is the "show me everything" mode — it merges interaction
highlights from all placed reagents into one view. Legend highlights find all cells of a given
aspect for the upcoming aspect legend panel (H.10). Design choice: severity defaults to Warning
for aspects without StatusThresholds entries (Sharp, Fragile), while tracked aspects (Hot, Cold,
Toxic) get severity from their cumulative threshold level.
Commit: 9668f51
