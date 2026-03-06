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
