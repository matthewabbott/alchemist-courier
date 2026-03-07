# Ralph Loop — 2026-03-06

## ART.4: Manuscript reference curation
Scaffolded the art reference document cataloging five historical alchemy and herbal manuscripts that inspire the game's visual identity. The sources span from the 6th-century Vienna Dioscurides (the gold standard for botanical illustration) through the Aztec Badianus Codex and German alchemical Splendor Solis. Each source maps to specific game elements — botanical illustrations for reagent sprites, alchemical diagrams for UI backdrops, woodcuts for menu art. The style translation notes capture the core tension: preserving the hand-drawn quality and earth-tone palettes of medieval manuscripts while adapting them into readable 32x32 pixel art with a cozy tone. This document feeds directly into ART.5's custom RetroDiffusion style creation pipeline.
Commit: 8c56f06

## CQ.1: Extract PuzzleValidator to Core
Moved puzzle win-condition logic out of PuzzleScreen (Godot-dependent) into a pure C# PuzzleValidator in src/Core/Puzzle/. This is the first step of a two-part refactor (CQ.1 extracts, CQ.2 wires PuzzleScreen to use it). The ConfirmResult enum moved cleanly since all consumer files already had the Core.Puzzle using directive. ValidationResult wraps the enum with reaction details and score, using static factory methods for each outcome. PuzzleValidator.Evaluate dispatches on PuzzleType — FitAllSafely checks all-placed + no-reactions, MaximizeValue skips the unplaced check and sums placed reagent values, MeetRequirement delegates to FitAllSafely with a TODO for future requirements.
Commit: ae7ff2a

## CQ.2: Wire PuzzleScreen to PuzzleValidator
Completed the second half of the validator refactor — PuzzleScreen.ConfirmPuzzle() now delegates entirely to PuzzleValidator.Evaluate() instead of reimplementing win-condition logic inline. The switch statement maps each ConfirmResult to the appropriate UI response (overlay + status message), and MaximizeValue wins now display the score. This shrinks ConfirmPuzzle from 32 lines of branching logic to a clean evaluate-then-display pattern, and means any future PuzzleType additions only need changes in the Core validator.
Commit: 2f5167b

## CQ.3: Add locked reagent visual feedback
Added amber borders around locked reagents in MetaGridView, following the same pattern used for hinted reagent borders. When a player right-clicks a locked reagent to try removing it, they now see a rejection tooltip ("Cannot remove — locked reagent") instead of a silent no-op — previously the MetaGrid.RemoveReagent guard silently blocked the removal but the UI gave no feedback. Also hardened PuzzleScreen.RemoveReagentAt to check the RemoveReagent return value. The integration test creates a minimal PuzzleState with one locked and one normal reagent, verifies the locked one is pre-placed and cannot be removed programmatically.
Commit: 608b943

## Code Review
Reviewed the PuzzleValidator extraction (CQ.1, CQ.2) and art reference curation (ART.4). One P1 finding: the default branch in PuzzleValidator.Evaluate's switch silently returned NotAllPlaced for unknown PuzzleType values instead of throwing. Fixed by replacing with ArgumentOutOfRangeException (fail-fast). Two P3 notes: unused `unplacedCount` parameter on ValidationResult.NotAllPlaced, and `List<ReactionRule>` parameter type on the public API. Overall the extraction is clean — good Core/Game separation, solid test coverage, and the refactored ConfirmPuzzle is more readable than the original.
Commit: fef7e61

## CQ.4: Decompose PuzzleScreen — ResultOverlayPanel and HintOrchestrator
Continued PuzzleScreen decomposition by extracting two self-contained subsystems. ResultOverlayPanel (PanelContainer subclass) owns the win/fail overlay UI — construction, styling, and the ShowWin/ShowFailure/Hide/FormatReactionType methods. HintOrchestrator (plain C# class, deliberately not a Godot node) encapsulates the hint domain logic: auto-pick, snap-back detection, budget tracking, and ExecuteHint orchestration. PuzzleScreen drops from 855 to 761 lines, with the remaining bulk being UI construction (BuildUI) and signal wiring — both inherently screen-level concerns. The orchestrator pattern keeps testable logic out of the Godot scene tree while leaving UI refresh calls where they belong.
Commit: 5c86a24

## GF.1: Create GameFlowController
Built the game flow spine — a GameFlowController that manages screen transitions between MainMenu, Tutorial, DailyPuzzle, and Editor. The controller follows the same pre-init property pattern used by PuzzleScreen: set properties on the new screen, AddChild triggers _Ready. First-launch detection routes to Tutorial when TutorialLessonCompleted == 0. Since GF.2 and GF.3 aren't built yet, I created working stub screens: MainMenuScreen with three navigation buttons and DailyPuzzleScreen wrapping PuzzleScreen with a back button and already-completed guard. Added TutorialCompleted event to TutorialScreen so the controller can respond to tutorial completion. The game.tscn scene is minimal — just a Control root with the controller script, matching main.tscn's pattern.
Commit: fe4c0cb

## Code Review B
Reviewed 9809fb9..eba3d86 (ResultOverlayPanel/HintOrchestrator extraction, GameFlowController, screen stubs). One P1: DailyPuzzleScreen called DateTime.UtcNow 4 times independently — potential midnight-boundary inconsistency; fixed to use PuzzleDate throughout. Two P3 quick wins fixed: GameFlowController default branch now throws (fail-fast, matching PuzzleValidator pattern), and ResultOverlayPanel's redundant `new Hide()` shadow removed. Noted P3 magic number `5` for tutorial lesson count — not worth fixing since the event-based trigger is the real guard.

## GF.2: Create MainMenuScreen
Replaced the GF.1 stub with the full styled implementation. The three buttons now have distinct visual identities: Daily Puzzle in green (showing streak count or "Completed" based on SaveData), Tutorial in blue (showing lesson progress like "Lesson 3/5" or "Complete"), and Editor in neutral gray. Each button uses CreateStyledButton() with StyleBoxFlat for normal/hover/pressed states, matching the pattern established in PuzzleScreen. The streak footer uses GetEffectiveStreak() rather than raw CurrentStreak to avoid displaying stale streaks after a gap day.
Commit: 3b95f1d

## GF.4: Wire main.tscn to GameFlowController
The final game-flow wiring task — updated scenes/main.tscn to use GameFlowController instead of PuzzleScreen. The game now launches into the main menu (or tutorial for first-time players) rather than a standalone puzzle screen. GF.3 (DailyPuzzleScreen) was already fully implemented during GF.1 as a necessary dependency, so this iteration marks both GF.3 and GF.4 as done. Added two integration tests exercising GameFlowController's TransitionTo API across MainMenu, Tutorial, and DailyPuzzle screens, verifying both the enum state and node type after each transition. ART.5 (custom RetroDiffusion style creation) is the only remaining pending task but is blocked as a human-in-the-loop task requiring aesthetic decisions.
Commit: 9d485fb

## VS.1: Fix ComplexityFilter — promote Sharp/Fragile to Simple tier
Sharp and Fragile were incorrectly classified as Intermediate-tier aspects, but design docs treat them as core mechanics central to the danger model. Promoting them to Simple was a two-line change in ComplexityFilter, but the cascade into PuzzleGenerator was the real challenge: Fragile's A.3 mechanic (breaks from ANY adjacent reaction) combined with Sharp's edge-placement constraint makes generation significantly harder when both are in the aspect pool. The fix limits each constrained aspect to one reagent per puzzle and filters Fragile when all bags are Gravity — keeping puzzles interesting without making generation intractable. Simple-tier puzzles can now include Sharp/Fragile Breakage interactions from day one.
Commit: a3dbd20

## VS.2: Create ReagentCatalog with canonical single-aspect reagents
Built the canonical reagent catalog — 72 named reagent definitions covering all 8 Simple-tier species (Firevine, Snowflower, etc.) across 9 standard polyomino shapes from monomino to L-tetromino. This replaces anonymous "reagent_0" identifiers with proper names like "Firevine Domino" and "Sharpstone T-Piece", each with flavor text from the design docs. The catalog is a static lookup table with indices by ID, aspect, and shape, plus a FindMatch() method that VS.3 will use to wire generated puzzles to named catalog entries. Pure Core code, zero Godot deps.
Commit: 197a10f

## VS.4: Implement ActionHistory (undo/redo) in Core
Built the command-pattern undo/redo infrastructure — IGridAction interface with PlaceReagentAction and RemoveReagentAction implementations, managed by an ActionHistory class with dual undo/redo stacks. The key design decision was tracking rotation state: PlaceReagentAction captures and restores the previous rotation on undo, while RemoveReagentAction snapshots position and rotation at construction time so they survive any player modifications while the reagent is unplaced. Failed operations leave stacks unchanged for consistency. VS.5 will wire this into PuzzleScreen with Ctrl+Z/Ctrl+Y shortcuts.
Commit: a04612c

## Code Review C
Reviewed VS.1 (Sharp/Fragile promotion), VS.2 (ReagentCatalog), and VS.4 (ActionHistory) — cf5896c..882a33b. All code is clean: Core purity maintained, Command pattern applied appropriately (not speculative), rotation tracking is correct, and 36 new tests provide thorough coverage. Only P3-P4 findings: silent (0,0) fallback in RemoveReagentAction constructor for unplaced reagents (safe in practice), minor style notes in ReagentCatalog (double lookup, allocation on miss), and one xUnit2013 build warning. No fixes or tasks needed — CLEAN status.

## VS.6: Create VisualStateResolver in Core
First piece of the sprite state system — the pure C# logic layer that answers "what visual condition is this reagent cell in?" For each cell of a placed reagent, the resolver checks cardinal neighbors for Hot/Cold aspects (from OTHER reagents, not self) and checks directly above for Heavy. The resolution priority chain was the interesting design choice: Crushed wins over thermal states (if Heavy is above you, you're being crushed regardless of temperature), ThermalStress only applies to Fragile reagents (non-fragile things can be warm or cold without danger), and when both Hot and Cold are adjacent to a non-fragile reagent they cancel out to Normal. This sets up VS.7's sprite sheet rendering — the Game layer will map these states to sprite regions.
Commit: 7f1a010

## VS.3: Wire PuzzleGenerator to use ReagentCatalog
Bridged the gap between the anonymous "Reagent 0" names and the canonical species catalog from VS.2. Added TryFindCatalogMatch() to PuzzleGenerator — when a reagent has all cells sharing one aspect and its shape matches a catalog entry, the generator uses the catalog definition (e.g. "Firevine Domino" instead of "Reagent 3"). The matching is conservative: multi-aspect, no-aspect, and rotated shapes that don't match catalog matrices fall back to anonymous names. DemoPuzzle's iceShard and inertCrystal now use catalog entries directly (snowflower_domino and nullflower_domino), while multi-aspect demo reagents keep their custom definitions.
Commit: 9a0bcf0

## VS.5: Wire undo/redo into PuzzleScreen UI
Wired the ActionHistory infrastructure (from VS.4) into the actual gameplay loop. All player-initiated placements and removals now flow through ActionHistory.Execute(), enabling full undo/redo. MetaGridView accepts an optional ActionHistory via SetActionHistory() — this keeps the editor and other consumers unaffected since they fall back to direct grid calls. Added Undo/Redo buttons to the button bar alongside Confirm/Reset/Hint, plus Ctrl+Z and Ctrl+Y keyboard shortcuts. The interesting edge case was hints: since HintOrchestrator directly manipulates the grid outside the action history, a successful hint clears the undo stack to prevent inconsistent state when undoing.
Commit: 9ef9131

## Code Review D
Reviewed VS.5 (ActionHistory UI wiring, commits 942efa5..651e894). No P0/P1 issues found. One P2 DRY violation: PuzzleScreen has 6+ near-identical UI refresh sequences across Undo, Redo, OnReagentPlaced, OnReagentRemoved, RemoveReagentAt, and UseHint — created task R1 to extract a RefreshAfterGridChange helper. Core ActionHistory files are clean and Godot-free. Positive notes: correct use of _UnhandledKeyInput for shortcuts, and smart ActionHistory clearing on hint operations.

## R1: Extract RefreshAfterGridChange helper in PuzzleScreen
Quick DRY cleanup from Code Review D — extracted the 6 near-identical refresh sequences (tray + grid + status bar + thresholds + eagle sense + undo/redo buttons) into a single RefreshAfterGridChange(bool updateHints) helper. The UseHint path additionally needs UpdateHintButton, handled by the parameter. ResetPuzzle is left as its own path since it has a different lifecycle (clears history, hides overlay, resets confirmation state) and deliberately omits eagle sense refresh. Net -20 lines, zero behavior change.
Commit: b1cb698

## VS.7: Create SpriteSheetSpec and SpriteStateRenderer
Built the three-layer sprite rendering pipeline from docs/sprite-state-system.md. SpriteSheetSpec in Core is a pure C# data model mapping VisualState enums to sprite sheet rows and frame counts — the DefaultReagentSpec() factory gives the standard 7-row layout (Normal through Exploding, with 4-frame animations for destruction states). ReagentSpriteSheet wraps a Godot Texture2D with a static cache for auto-loading from res://art/sprites/reagents/. SpriteStateRenderer is a TextureRect that shows the correct atlas region for the current VisualState and auto-advances animated states at 8fps. The MetaGridView integration is the interesting piece: UpdateSpriteRenderers() auto-detects sprite sheets for placed reagents by catalog ID, creates SpriteStateRenderer overlays at ZIndex 2, and uses VisualStateResolver to pick the most critical visual state per reagent (Crushed beats ThermalStress beats Heated/Cooled). Reagents without sprites fall back to ColorRect rendering unchanged. No sprites exist yet (that's VS.8's DevSpriteGenerator job), but the pipeline is ready.
Commit: 16d3224

## VS.8: Create DevSpriteGenerator tool
Procedural sprite sheet generator for all 72 ReagentCatalog entries — the tool that fills VS.7's empty sprite pipeline. Each 128×224 PNG has 7 state rows × 4 frame columns at 32×32 per cell. The Normal row uses aspect-colored fills with per-aspect 8×8 icon silhouettes (flame for Hot, snowflake for Cold, etc.), and state overlays get progressively more dramatic: orange corner for Heated, blue corner for Cooled, red-blue diagonal split with crack lines for ThermalStress, darkened with downward arrow for Crushed. The animated rows (Breaking and Exploding) tell 4-frame destruction stories — Breaking goes from hairline cracks to full fragment scatter, while Exploding goes from white flash to radial burst to particle dispersion to fade. Also outputs sprite_specs.json with the SpriteSheetSpec format so the rendering pipeline can consume them. Now running the dev_sprite_generator scene will populate art/sprites/reagents/ with all the assets MetaGridView needs for sprite-based rendering.
Commit: 209cdb4
