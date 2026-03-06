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

## H.7: HighlightOverlay Godot renderer
The first Godot-side piece of the highlight system: a Control node that turns HighlightSet data
into visible colored rectangles. Edge highlights render as thin 3px lines along cell boundaries
(using CellEdge flags), while cells with no edges get semi-transparent fills. Colors come from
the existing GetAspectColor() mapping, modulated by severity alpha — Danger is nearly opaque
at 0.9 while Safe is subtle at 0.4. Eagle sense mode adds a dark overlay behind everything to
make highlights pop. The overlay renders layer-by-layer (Constraint → Interaction → Legend →
GhostOverlay) and deduplicates fills per position. This is the bridge between the pure-Core
highlight engine (H.5-H.6) and the visual game — H.8 will wire it into MetaGridView.
Commit: 8b22c2a

## Code Review (c)
Reviewed H.6, H.7, H.8 (d0c4e68..c28c85e). One P2 DRY fix: `_highlightOverlay.Clear()` was
always paired with `ClearGhost()` in 5 locations — moved into `ClearGhost()` to eliminate 4
redundant calls. Two P3 notes: `HoveredReagentChanged` signal declared but not yet consumed
(planned for H.10/H.11), and `SetHoverPosition` test helper doesn't call `UpdateHoveredReagent`
(harmless for current tests). 461 tests pass after fix.

## H.8: MetaGridView highlight integration
The culmination of H.1–H.7: wiring the highlight system into the actual game view. HighlightOverlay
now lives as a child of MetaGridView at ZIndex 4 (above ghost preview at Z3), and UpdateGhostPreview
automatically computes constraint + interaction highlights whenever a reagent hovers. Sharp reagent
over a non-Reinforced bag edge? Red boundary highlights on the violating cells. Hot reagent near
Volatile? Interaction highlights on the neighbor cells. PuzzleScreen builds StatusThresholds from
the status bar after each placement so highlight severity reflects the current cumulative state.
Added HoveredReagentChanged signal for future use by the item detail panel (H.11) and legend (H.10).
Commit: d8474fe

## H.9: Rejection tooltip display
The missing UX feedback for "why can't I place here?" — when a player clicks an invalid position,
a tooltip now appears near the cursor showing the constraint violation reason (e.g. "Sharp: can't
place on bag edges") with text colored by the constraint's aspect. The tooltip auto-hides after 2
seconds or on mouse move, and constraint highlights flash briefly to draw attention. Refactored
HandleMouseClick to extract HandleLeftClick for both real input and programmatic test access via
SimulateLeftClick(). Added RejectionFlash signal as infrastructure for H.10's legend panel linking.
Commit: 354e7af

## H.10: Aspect legend panel
Replaced the static AspectPrimerPanel with a richer AspectLegendPanel — a collapsible sidebar
showing each puzzle-relevant aspect with a colored icon, name, description, constraint text, and
associated reaction rules. This is the informational backbone the player needs to reason about
aspect interactions without memorizing rules. Designed with highlight/flash methods that the
upcoming orchestration task (H.12) will wire to grid hover and rejection events.
Commit: 1f75d7c

## Code Review (d)
Reviewed H.9–H.11 (354e7af..6e0632c). Very clean batch — no P0-P2 issues. Fixed one unused
constant (`PreviewCellSize` in ItemDetailPanel, superseded by inner class's own `CellSize`) and
two stale comments claiming bold/italic styling that wasn't applied. Two similar `_Draw()` methods
(ShapePreview vs DetailShapePreview) noted but not abstracted per Rule of Three.

## H.11: Item detail panel (Book of Hours style)
Built the persistent item inspection panel — a sidebar that always shows info about whatever
reagent the player is hovering over, whether in the tray or on the grid. Inspired by the Book
of Hours UI pattern where inspection info lives in a fixed location rather than following the
cursor. The panel shows an enlarged shape preview (16px vs the tray's 8px), the reagent name,
flavor text if available, and a full aspect breakdown with colored squares and magnitudes. Also
added small colored aspect dots to each tray item for at-a-glance reagent identification.
Commit: 6e0632c

## H.12: PuzzleScreen orchestration and eagle sense
This is the "wiring" task that connects all the new highlight-system UI components together into
a cohesive interactive experience. Clicking an aspect in the legend now highlights all placed cells
with that aspect on the grid (toggle behavior — click again to clear). Hovering a placed reagent
on the grid highlights its aspects in the legend sidebar. The eagle sense toggle (Tab key) renders
ALL interaction highlights simultaneously with a dim overlay, letting the player see every dangerous
adjacency at once — and it auto-refreshes when reagents are placed or removed. The rejection flash
from invalid placements now visually pulses the relevant legend entry. Small but important: exposed
the StatusThresholds property on MetaGridView for the eagle sense computation.
Commit: b1ec43e

## H.13: Ghost preview constraint composition
Added a second constraint highlight pass that renders directly on ghost cells via the GhostOverlay
layer. Previously, constraint violations only showed on the Constraint layer (rendered behind ghost
cells) — now the ghost cells themselves get edge markers when hovering over invalid positions. The
implementation mirrors ComputeConstraintHighlights exactly but targets GhostOverlay, keeping the
two layers visually distinct: Constraint marks the grid, GhostOverlay marks the ghost. Clean task
since the GhostOverlay layer enum value and renderer support were already in place from H.7.
Commit: fdc1e46

## Code Review (e)
Reviewed H.12–H.14 and integration tests (949e489..18f605c). Clean — no P0-P2 issues.
Two P3 notes: ComputeGhostConstraintHighlights duplicates ComputeConstraintHighlights logic
(only layer differs, Rule of Three not met), and GetMvpRules() called 7 times in PuzzleScreen
(could cache as field, low impact). Overall code quality is strong — HighlightEngine is well-tested
(30 unit tests), PuzzleScreen orchestration is cleanly wired through signals, integration tests
properly isolate with fresh PuzzleScreen instances. 466 tests passing.

## H.14: Visual polish design document
The capstone of the highlight-system phase: a 10-section design document (docs/visual-polish-design.md)
capturing the full visual polish vision for future implementation. Covers per-aspect icon designs
(flame, snowflake, skull, etc.), highlight visual styles (crosshatching for danger zones, pulsing
glow for interactions, animated dashed borders for eagle sense), the RetroDiffusion sprite pipeline
for generating reagent portraits and bag textures, contiguous boundary refinements (glow, shadow,
shimmer), a "quiet mode" for experienced players, a sprite editor tool concept for iterating on
highlight parameters without recompiling, information density guidelines (4 progressive disclosure
levels from clean grid to full eagle sense), eagle sense visual treatment (deep overlay, glowing
connection lines, silhouetted reagents), layout balance at different viewport sizes, and a
canonical color palette reference matching the existing GridConstants values.
Commit: 83cf900

## HINT.1: Core hint data types (HintState, HintResult, Reagent.IsHinted)
Kicked off the hint-system phase with pure data types. HintState tracks hint budget (max vs used)
and which reagents have been hinted via a HashSet of indices. HintResult captures the full outcome
of a hint attempt — success, budget exhausted, locked blockers, etc. — providing all the info
HINT.2's engine will need. Also threaded IsHinted through Reagent (parallel to IsLocked) and
MaxHints through PuzzleState for serialization. Clean foundation for the engine logic next.
Commit: a25909b

## HINT.2: HintEngine core logic (auto-pick, dislodge, execute)
Built the brain of the hint system — four static methods that handle the full lifecycle of a hint.
The key design choice is the two-pass auto-pick: snap-back candidates (previously hinted, now
unplaced) get priority because they're free, preserving the player's hint budget for genuinely new
hints. ComputeDislodgeList temporarily rotates the target to compute accurate solution positions,
then restores — a pattern borrowed from ChemistryEngine's prospective evaluation. ExecuteHint
carefully orchestrates removal → dislodge → rotate → place → flag, resetting dislodged reagents
to rotation=0 and clearing their IsHinted flags so they return cleanly to the tray.
Commit: caa1f1a

## HINT.3: DifficultyParams MaxHints + generator integration
Straightforward wiring task: added MaxHints to DifficultyParams with per-level scaling
(tutorials/easy=3, medium=2, hard=1) and a one-line addition in PuzzleGenerator.TryGenerate
to propagate the value to the generated PuzzleState. Also updated TutorialData's five
manually-constructed DifficultyParams to include maxHints: 3 so tutorial puzzles get hints too.
Commit: e6ddc8e

## HINT.4: PuzzleScreen hint orchestration
Wired HintEngine into PuzzleScreen — the bridge between the Core hint logic and the Godot UI.
OnHintPressed calls PickNextHintTarget for auto-pick, checks snap-back status to decide if the
hint costs budget, then calls ExecuteHint and refreshes all UI components (tray, grid, status
bar, eagle sense). Added UseHint() for programmatic testing. The hint button uses a purple
theme to distinguish it from Confirm/Reset, with a "N/M" budget counter. Button is hidden
entirely when MaxHints==0 (old puzzles, demo mode), and disabled when budget is exhausted
unless a snap-back candidate exists. ResetPuzzle clears IsHinted on all reagents but preserves
the hint budget — no refunds.
Commit: 82eb71b

## Code Review (f)
Reviewed HINT.3–HINT.4 (e6ddc8e..f8e4117). Found one P1 DRY violation: OnHintPressed and UseHint
duplicated ~40 lines of identical hint orchestration logic with a subtle behavioral inconsistency
(UseHint refreshed UI on failure, OnHintPressed didn't). Fixed by making OnHintPressed delegate to
UseHint and only refreshing UI on success. Net -24 lines. 495 tests pass.

## HINT.5: Hint visual indicators (grid + tray)
Added the visual feedback layer for the hint system. Hinted reagents on the grid now get a soft
purple (0.55, 0.45, 0.75) contiguous border — only outer edges are drawn, matching the bag border
pattern but with a distinct color that reads as "system-assisted placement." In the tray, previously
hinted reagents that have been removed show a small purple dot next to their name, signaling that
snap-back is available at no budget cost. Wired HintState from PuzzleScreen into ReagentTray via
a SetHintState() setter so BuildItems() can query hint history for each reagent index.
Commit: 25298b2

## HINT.6: SaveData hint tracking
Tiny but necessary persistence addition — added DailyHintsUsed dictionary to SaveData so hint
usage survives sessions. Maps date seeds to hint counts, following the same pattern as
CompletedDailies. Two methods: RecordDailyHints for storage, GetDailyHintsUsed for retrieval
(-1 for unknown seeds, matching the "no record" semantics of daily completion checks). This
enables the future hint-aware scoring layer: zero-hint completions can earn extra stars.
Commit: ba72035

## HINT.7: Hint button UI in PuzzleScreen
The hint button UI was already wired up during HINT.4 (orchestration) — button creation,
count label, MaxHints==0 hiding, and enable/disable logic were all in place. This task
finalized the visual polish by adding a `disabled` StyleBox to CreateStyledButton, giving
all styled buttons (including Hint) a properly darkened background and dimmed text when
disabled. Small change, but it closes the visual gap in the hint system UI.
Commit: 0560de9

## Code Review (g) — HINT.5-HINT.7
Reviewed 8 commits (718631a..58a0f93) covering HINT.5 visual indicators, HINT.6 save data
tracking, and HINT.7 disabled button styling. All clean — no P0-P2 issues. Three P3 notes:
CreateHintedBorderRect/CreateBorderRect near-duplication (Rule of Three not met), local magic
number for hint border width, and GetMvpRules() repeated calls (carried forward from review f).
One P4 cosmetic: asymmetric cleanup pattern for bag vs hinted borders in RefreshDisplay.

## HINT.8: Integration test: full hint system
Enhanced the existing Test_HintSystem stub to a comprehensive integration test covering the
full hint lifecycle. The test exercises budget initialization, first-use placement with
IsHinted verification, removal and snap-back (free re-placement of previously hinted reagent),
a second hint on a different reagent consuming budget, the dislodge scenario (placing a reagent
at another's solution position then hinting to force it back to tray), and budget preservation
across puzzle reset. The dislodge test dynamically searches for overlapping solution pairs,
gracefully skipping if no overlap exists for the generated puzzle.
Commit: 29ed603

## DOC.1: Create docs/puzzle-design.md
First documentation-phase task: created the comprehensive puzzle design reference that
roadmap.md has been cross-referencing as "(pending DOC.1)" throughout. The document covers
the full taxonomy of puzzle types across all four project tiers, from the currently-shipping
FitAllSafely through aspirational destruction/transmutation and DLC-tier actual alchemy. The
most important section is probably the scoring philosophy — time + hints for secondary scoring,
with move count explicitly excluded. The design pillar "never punish experimentation" has
concrete implications here: the timer pauses when reading rules, reset is free, and hints
reduce stars but don't prevent completion. This doc fills a real gap — several other docs
were referencing it by name before it existed.
Commit: 1b300c1

## DOC.2: Update existing docs with puzzle-design cross-references
Companion to DOC.1 — now that puzzle-design.md exists, the three docs that should reference
it needed updating. mvp-specification.md got the hint system added to its Nice-to-Have list
(it was implemented but not listed), plus a Related Documents cross-ref. game-overview.md got
a sentence after the roadmap section pointing readers to detailed puzzle type designs.
chemistry-system.md's stub "Future: Brewing and Mixing" section was replaced with a compact
cross-reference to puzzle-design.md sections 6 and 7 where those topics are covered in depth.
Commit: 801a640

## Code Review (h)
Reviewed ec3c12a..1efadb8 (HINT.8 test expansion + DOC.1/DOC.2 docs). Only code change was
TestHarness.cs hint_system test enhancement — well-structured with numbered sections, good
coverage of budget, snap-back, second hint, dislodge, and reset. One dead variable (P2) and
one FQ type (P4) noted but not fixed. CLEAN status — no P0-P1 issues.

## T.1: Install desloppify and run baseline scan
Installed desloppify 0.9.1 as the project's first mechanical code quality tracker. The C#
baseline came in at 84.3/100 objective (mechanical detectors only) — strong code quality
(99.2%) and security (99.4%), but test health dragged at 44.6% due to coverage gaps in
Game-layer code (which is tested via integration harness, not xUnit). 93 issues total, all
T3 severity — largest files are PuzzleScreen (865 LOC), TestHarness (1376 LOC), and
MetaGridView (778 LOC). The strict score of 33.7 is artificially low because 20 subjective
review dimensions haven't been assessed yet. No P0 issues. The `update-skill claude` command
hits a Windows cp1252 encoding bug (non-blocking). Added `.desloppify/` state files to git
and `.bak` files to `.gitignore`.
Commit: 4249356

## T.2: Update review-code prompt with desloppify step
Added mechanical code scanning to the review agent's workflow. The new step 5 runs
`desloppify scan` for a baseline score, `desloppify next` for the highest-priority issue,
fixes any P0-P1 quick wins, and re-scans to verify. The qualitative deslop.md review
stays as the human-insight complement — desloppify handles naming/formatting/dead code,
while the manual review catches architectural drift and game-specific design concerns.
Commit: acf41ab

## Code Review (i)
Reviewed 5d3c5d4..7ed6878 (T.1–T.3). Only C# change was TestHarness.cs hint_system test from HINT.8 — found and removed one unused variable (`aPlacedBefore`). No P0-P1 issues. Desloppify scan was blocked by permission system. 497 tests pass.

## T.3: Create game architecture guidelines doc
Wrote `docs/game-architecture.md` — a reference for game-specific structural patterns
as the project grows beyond MVP. Covers the six areas that matter most: how screens
work (pre-init property + deferred _Ready, procedural UI over .tscn), Godot signal
and node conventions, web export performance budgets (draw calls, GC pressure, WASM
memory), the placeholder-to-real-art pipeline, the select-then-place input architecture,
and the three-layer testing strategy. Every pattern references actual codebase files and
methods so it stays grounded, not aspirational.
Commit: 861a1d0

## E.1: Auto-solver — PuzzleSearchSolver with backtracking
First post-MVP task: a pure C# backtracking solver that can find valid solutions for any
puzzle. The solver treats each puzzle as a constraint-satisfaction problem — assign each
reagent a (position, rotation) tuple such that nothing overlaps, everything sits in a bag,
and ChemistryEngine reports zero reactions. Uses the same MRV heuristic as PuzzleGenerator
(Heavy first, then Sharp, then largest) to minimize dead-end exploration. Reuses
PuzzleSolver's grid/reagent reconstruction and handles locked reagents by pre-placing them
before the search begins. CancellationToken support enables timeout for the editor's
solve-on-demand feature. This is the foundation for E.7 (editor solve integration).
Commit: dafa8ea

## E.2: PuzzleFile wrapper with editor metadata
PuzzleFile separates authoring concerns from gameplay data — PuzzleState stays focused on
what the player sees, while PuzzleFile adds Name, Description, Author, and CreatedAt for
the editor workflow. Followed SaveManager's exact serialization pattern (System.Text.Json,
WriteIndented, graceful null returns) to keep the codebase consistent. Both file I/O
(Save/Load) and in-memory (SaveToString/LoadFromString) APIs are provided, since the editor
will use files but tests and clipboard features will want string serialization.
Commit: cb69213

## E.3: ShapeEditorWidget reusable control
The first visual piece of the puzzle editor — a toggleable grid widget for drawing bool[,]
shape matrices. Following MetaGridView's procedural pattern (ColorRect cells + grid lines),
but with its own configurable cell pixel size (24px default vs the game's 32px) since editor
controls need to be more compact. GetShape() trims to the bounding box of active cells so
consumers get a tight shape matrix regardless of where the user clicked within the 6x6 grid.
This widget serves double duty: bag shape editing and reagent shape editing in the editor UI.
Commit: a09deaa

## Code Review (j)
Reviewed E.1–E.3 (dafa8ea..e29edbc). Found and fixed two dead code issues: unused `reagents`
parameter threaded through PuzzleSearchSolver's Backtrack/CountBacktrack methods (never read,
only forwarded recursively), and unused `using AlchemistCourier.Game.Inventory` import in
ShapeEditorWidget. Noted DRY duplication in Solve/CountSolutions locked-reagent setup but
Rule of Three not met (2 occurrences). 519 tests pass after fixes.

## E.4: BagEditorPanel
Built the first of two editor panels — the bag editor. Each bag gets a collapsible panel with
a ShapeEditorWidget for drawing the bag shape, SpinBox controls for grid position, and property
checkboxes (Null, Gravity, Locked, etc.). The panel composes ShapeEditorWidget from E.3 and
PuzzleBag from E.2, bridging the editor data model to the Godot UI. Followed PuzzleScreen's
procedural UI conventions: VBoxContainer layout, theme color overrides, signal-based change
propagation. New bags default to a 3x3 filled rectangle at (0,0). The nested BagEntry class
keeps each bag's state encapsulated with its own Changed/RemoveRequested signals bubbling up
to the parent's BagsChanged signal. E.5 (ReagentEditorPanel) follows the same pattern.
Commit: bf1017c

## E.5: ReagentEditorPanel
The second editor panel, mirroring BagEditorPanel's structure for reagent editing. The interesting
design challenge was the per-cell aspect assignment: each true cell in the shape gets an
AspectCellRow with an OptionButton dropdown populated from AspectMetadataDatabase (10 aspects +
None). When the shape changes — cells toggled on or off — SyncAspectRowsToShape() dynamically
adds or removes aspect rows to match the true-cell count, preserving existing selections. New
reagents default to a simple 1x1 cell. The smaller 4x4 shape grid (vs bags' 6x6) reflects that
reagents are typically smaller than bags. Weight, Value, and IsLocked round out the per-reagent
properties. With E.4 and E.5 done, E.6 (EditorScreen) can wire both panels into the main editor.
Commit: 73cccaf

## E.6: EditorScreen main layout and file I/O
The main editor screen brings together all the editor infrastructure built so far. EditorScreen
follows PuzzleScreen's procedural UI pattern exactly: full-rect Control, BuildUI() constructing the
entire node tree, StyleBoxFlat theming, and signal-based change propagation. The layout splits into
a MetaGridView on the left (showing the puzzle state live) and a scrollable right column with
BagEditorPanel and ReagentEditorPanel. When either panel changes, OnEditorDataChanged() syncs the
PuzzleState, rebuilds the MetaGrid from scratch (mirroring PuzzleSolver.ReconstructGrid), and
replaces the MetaGridView. File I/O uses PuzzleFile for JSON serialization to user://puzzles/.
One interesting hurdle: Vector2I ambiguity between Core.Grid and Godot — resolved by partial
qualification for Core types and full Godot qualification for the FileDialog's PopupCentered call.
Commit: c5a9b51

## Code Review (k) — E.4–E.6
Reviewed the three new editor files (EditorScreen, BagEditorPanel, ReagentEditorPanel) plus
ShapeEditorWidget. Fixed one P1: LoadPuzzle was missing try-catch for I/O exceptions (SavePuzzle
had one but LoadPuzzle didn't). Removed dead GetShapeRaw() method and redundant AnchorsPreset
setter. Noted duplicated color constants across 3 editor files (Rule of Three met) and
ShapeEditorWidget's use of `_Input` vs `_GuiInput` as P2 items for future attention.
Commit: 3df3f7d

## E.7: SolverPanel with auto-solve integration
The editor now has a solver panel below the MetaGridView. Three buttons: Solve finds one valid
placement, Count Solutions enumerates up to 100, and Show Solution toggles a blue cell overlay on
the grid. The interesting design choice was thread safety — solving runs on Task.Run with a
CancellationToken, and results marshal back to Godot's main thread via CallDeferred with pending
result fields. The panel auto-clears when bags or reagents change, preventing stale solutions from
lingering. Also cleaned up unused parameters in PuzzleSearchSolver's private backtrack methods.
Commit: 9a6d751

## E.8: Play mode toggle in editor
The puzzle editor can now be played — a "Play" button in the header bar switches the entire
screen from edit mode to play mode. The implementation builds a fresh MetaGrid and reagent list
from the current editor PuzzleState using PuzzleSolver.ReconstructGrid/ReconstructReagents,
swaps the grid view, hides editor panels, and shows a ReagentTray with ObjectivePanel and
StatusBarPanel. Reagent selection, placement, and removal work identically to PuzzleScreen via
MetaGridView's existing signals. The key design choice was stateless play mode: when you click
"Back to Edit," all placements are discarded and the editor state is fully restored. This
avoids any state synchronization complexity between edit and play modes. Refactored the grid
view replacement into a shared ReplaceGridView(MetaGrid) method, which simplified both the
editor's OnEditorDataChanged flow and the play mode transitions.
Commit: 3d233b9

## E.9: Editor integration tests
Three focused integration tests close the editor phase with end-to-end coverage.
Test_EditorCreateAndSave verifies the full save/load pipeline: build a PuzzleState with 2 bags
and 3 reagents, load into EditorScreen, save from the editor, load back via PuzzleFile.Load,
and confirm bag/reagent counts and shape dimensions survive the round-trip. Test_EditorSolver
validates the solver against editor-format puzzles — a solvable 3x3 bag with two 1x1 reagents
returns 2 placements, while an unsolvable 1x1 bag with two reagents correctly returns null.
Test_EditorPlayMode is the most interesting: it enters play mode, programmatically places a
reagent on the play grid (via internal test accessors), exits play mode, and verifies the
editor state is unchanged through before/after file comparison. The internal PlayGrid and
PlayReagentList properties were the only production code additions — minimal surface for testing.
Commit: 376dcbb
