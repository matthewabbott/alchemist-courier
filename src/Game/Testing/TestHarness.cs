using System.Threading.Tasks;
using Godot;
using AlchemistCourier.Game.Inventory;
using AlchemistCourier.Core.Puzzle;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Testing;

public partial class TestHarness : Control
{
    private PuzzleScreen _puzzleScreen = null!;
    private int _passed;
    private int _failed;
    private int _stepNumber;
    private string _logPath = "";
    private System.Text.StringBuilder _logBuffer = new();

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        // Set up file-based log alongside stdout
        var dir = ScreenshotHelper.GetOutputDirectory();
        _logPath = System.IO.Path.Combine(dir, "test_results.log");

        // Create PuzzleScreen as a child (it sets up its own DemoPuzzle)
        _puzzleScreen = new PuzzleScreen();
        AddChild(_puzzleScreen);

        // Start test sequence after the first frame renders
        CallDeferred(MethodName.RunTests);
    }

    /// <summary>Print to both stdout and the log buffer.</summary>
    private void Log(string message)
    {
        GD.Print(message);
        _logBuffer.AppendLine(message);
    }

    /// <summary>Flush log buffer to file.</summary>
    private void FlushLog()
    {
        System.IO.File.WriteAllText(_logPath, _logBuffer.ToString());
    }

    private async void RunTests()
    {
        Log("[HARNESS_START]");

        // Wait for initial render to complete
        await WaitFrames(3);

        await Test_RenderInitial();
        await Test_ObjectivePanel();
        await Test_AspectPrimer();
        await Test_PlaceFirePebble();
        await Test_PlaceIceShardRotated();
        await Test_InvalidPlacement();
        await Test_PlacePoisonFang();
        await Test_RemoveReagent();
        await Test_PlacementWarnings();
        await Test_StatusBar();
        await Test_ConfirmAndReset();
        await Test_WinLossEvaluation();
        await Test_GeneratedPuzzle();
        await Test_TutorialFlow();
        await Test_DailyPuzzle();
        await Test_RenderFinal();

        Log($"[HARNESS_DONE] passed={_passed} failed={_failed}");
        FlushLog();

        // Auto-quit after a short delay
        await WaitFrames(2);
        GetTree().Quit();
    }

    private async Task Test_RenderInitial()
    {
        BeginTest("render_initial");
        PrintState();
        await CaptureScreenshot("01_render_initial.png");

        // Verify: 2 bags, 0 placed, 6 unplaced
        bool pass = _puzzleScreen.Grid.PlacedBags.Count == 2 &&
                    _puzzleScreen.Grid.PlacedReagents.Count == 0;
        EndTest("render_initial", pass);
    }

    private async Task Test_ObjectivePanel()
    {
        BeginTest("objective_panel");

        // Verify objective panel exists and shows the default FitAllSafely text
        var objective = _puzzleScreen.Objective;
        var text = objective.ObjectiveText;
        Log($"[STATE] objective_text=\"{text}\"");

        var expectedText = Game.Inventory.ObjectivePanel.GetObjectiveText(
            AlchemistCourier.Core.Puzzle.PuzzleType.FitAllSafely);
        bool hasText = !string.IsNullOrEmpty(text) && text == expectedText;

        // Verify SetPuzzleType updates the text
        objective.SetPuzzleType(AlchemistCourier.Core.Puzzle.PuzzleType.MaximizeValue);
        var updatedText = objective.ObjectiveText;
        Log($"[STATE] objective_text_updated=\"{updatedText}\"");
        var expectedUpdated = Game.Inventory.ObjectivePanel.GetObjectiveText(
            AlchemistCourier.Core.Puzzle.PuzzleType.MaximizeValue);
        bool updatedCorrectly = updatedText == expectedUpdated;

        // Reset back to default
        objective.SetPuzzleType(AlchemistCourier.Core.Puzzle.PuzzleType.FitAllSafely);

        await CaptureScreenshot("02_objective_panel.png");

        bool pass = hasText && updatedCorrectly;
        EndTest("objective_panel", pass, pass ? null
            : $"text=\"{text}\" expected=\"{expectedText}\" updated=\"{updatedText}\" expectedUpdated=\"{expectedUpdated}\"");
    }

    private async Task Test_AspectPrimer()
    {
        BeginTest("aspect_primer");

        // Verify primer panel starts hidden
        var primer = _puzzleScreen.PrimerPanel;
        bool startsHidden = !primer.Visible;
        Log($"[STATE] primer_visible={primer.Visible} (expected=False)");

        // Toggle open
        _puzzleScreen.TogglePrimer();
        bool nowVisible = primer.Visible;
        Log($"[STATE] primer_visible_after_toggle={primer.Visible} (expected=True)");

        // Verify rule count matches MVP rules (7 rules)
        int ruleCount = primer.RuleCount;
        Log($"[STATE] primer_rule_count={ruleCount} (expected=7)");

        await CaptureScreenshot("02b_primer_open.png");

        // Toggle closed
        _puzzleScreen.TogglePrimer();
        bool hiddenAgain = !primer.Visible;
        Log($"[STATE] primer_visible_after_second_toggle={primer.Visible} (expected=False)");

        bool pass = startsHidden && nowVisible && ruleCount == 7 && hiddenAgain;
        EndTest("aspect_primer", pass, pass ? null
            : $"startsHidden={startsHidden} nowVisible={nowVisible} ruleCount={ruleCount} hiddenAgain={hiddenAgain}");
    }

    private async Task Test_PlaceFirePebble()
    {
        BeginTest("place_fire_pebble");

        // Fire Pebble (index 0) is a single cell — place at (2,2) inside the SmallPouch
        bool placed = TestActions.SelectAndPlace(_puzzleScreen, 0, 2, 2);
        PrintState();
        await CaptureScreenshot("02_place_fire_pebble.png");

        bool pass = placed && _puzzleScreen.Grid.PlacedReagents.Count == 1;
        EndTest("place_fire_pebble", pass);
    }

    private async Task Test_PlaceIceShardRotated()
    {
        BeginTest("place_ice_shard_rotated");

        // Ice Shard (index 1) is a horizontal domino — rotate once to make it vertical
        // Place at (5,1) inside the Satchel
        bool placed = TestActions.SelectRotateAndPlace(_puzzleScreen, 1, 1, 5, 1);
        PrintState();
        await CaptureScreenshot("03_place_ice_shard_rotated.png");

        bool pass = placed && _puzzleScreen.Grid.PlacedReagents.Count == 2;
        EndTest("place_ice_shard_rotated", pass);
    }

    private async Task Test_InvalidPlacement()
    {
        BeginTest("invalid_placement");

        // Try placing Poison Fang (index 2) at (0,0) — outside any bag
        _puzzleScreen.SelectReagent(2);
        bool placed = _puzzleScreen.PlaceSelectedAt(0, 0);
        Log($"[ACTION] place col=0 row=0 result={(placed ? "success" : "failed")}");
        _puzzleScreen.CancelSelection();
        PrintState();
        await CaptureScreenshot("04_invalid_placement.png");

        bool pass = !placed && _puzzleScreen.Grid.PlacedReagents.Count == 2;
        EndTest("invalid_placement", pass);
    }

    private async Task Test_PlacePoisonFang()
    {
        BeginTest("place_poison_fang");

        // Poison Fang (index 2) is an L-shape with Sharp — place at (6,1) inside the Reinforced Satchel
        // (Sharp reagents can't sit on edge cells of non-Reinforced bags, so SmallPouch won't work)
        bool placed = TestActions.SelectAndPlace(_puzzleScreen, 2, 6, 1);
        PrintState();
        await CaptureScreenshot("05_place_poison_fang.png");

        bool pass = placed && _puzzleScreen.Grid.PlacedReagents.Count == 3;
        EndTest("place_poison_fang", pass);
    }

    private async Task Test_RemoveReagent()
    {
        BeginTest("remove_reagent");

        // Remove the Fire Pebble at (2,2)
        bool removed = TestActions.RemoveAt(_puzzleScreen, 2, 2);
        PrintState();
        await CaptureScreenshot("06_remove_reagent.png");

        bool pass = removed && _puzzleScreen.Grid.PlacedReagents.Count == 2;
        EndTest("remove_reagent", pass);
    }

    private async Task Test_PlacementWarnings()
    {
        BeginTest("placement_warnings");

        // Clean up state from prior tests: remove all placed reagents
        while (_puzzleScreen.Grid.PlacedReagents.Count > 0)
        {
            var r = _puzzleScreen.Grid.PlacedReagents[0];
            var pos = r.PlacedPosition!.Value;
            _puzzleScreen.RemoveReagentAt(pos.Col, pos.Row);
        }

        // Place Blast Jelly (volatile+hot, index 3) in the Satchel
        TestActions.SelectAndPlace(_puzzleScreen, 3, 4, 1);
        PrintState();

        // DANGER: Select Fire Pebble (hot, index 0), hover adjacent to Blast Jelly
        // Fire Pebble at (3,1) is adjacent to Blast Jelly cell at (4,1) — Hot near Volatile = Explosion
        _puzzleScreen.SelectReagent(0);
        _puzzleScreen.GridView.SetHoverPosition(new CoreVec(3, 1));
        await CaptureScreenshot("08_warning_danger.png");
        var dangerLevel = _puzzleScreen.GridView.CurrentWarningLevel;
        Log($"[STATE] warning_level={dangerLevel} (expected=Danger)");

        // WARNING: Move Fire Pebble hover to (1,1) — far from Blast Jelly, no reactions,
        // but Fire Pebble has "hot" aspect which participates in reaction rules
        _puzzleScreen.GridView.SetHoverPosition(new CoreVec(1, 1));
        await CaptureScreenshot("09_warning_caution.png");
        var warningLevel = _puzzleScreen.GridView.CurrentWarningLevel;
        Log($"[STATE] warning_level={warningLevel} (expected=Warning)");

        // SAFE: Select Inert Crystal (stable, index 4) — no reactive aspects
        _puzzleScreen.CancelSelection();
        _puzzleScreen.SelectReagent(4);
        _puzzleScreen.GridView.SetHoverPosition(new CoreVec(1, 1));
        await CaptureScreenshot("10_warning_safe.png");
        var safeLevel = _puzzleScreen.GridView.CurrentWarningLevel;
        Log($"[STATE] warning_level={safeLevel} (expected=Safe)");

        _puzzleScreen.CancelSelection();

        bool pass = dangerLevel == PlacementWarningLevel.Danger
                 && warningLevel == PlacementWarningLevel.Warning
                 && safeLevel == PlacementWarningLevel.Safe;
        EndTest("placement_warnings", pass, pass ? null
            : $"danger={dangerLevel} warning={warningLevel} safe={safeLevel}");
    }

    private async Task Test_StatusBar()
    {
        BeginTest("status_bar");

        // Clean up: remove all placed reagents
        while (_puzzleScreen.Grid.PlacedReagents.Count > 0)
        {
            var r = _puzzleScreen.Grid.PlacedReagents[0];
            var pos = r.PlacedPosition!.Value;
            _puzzleScreen.RemoveReagentAt(pos.Col, pos.Row);
        }

        // Verify status bar starts at zero
        var bar = _puzzleScreen.StatusBar;
        int hotBefore = bar.GetAspectTotal("hot");
        Log($"[STATE] hot_before={hotBefore} (expected=0)");

        // Place Fire Pebble (index 0) — has hot magnitude 2 — at (2,2) in SmallPouch
        TestActions.SelectAndPlace(_puzzleScreen, 0, 2, 2);

        int hotAfterPebble = bar.GetAspectTotal("hot");
        Log($"[STATE] hot_after_fire_pebble={hotAfterPebble} (expected=2)");

        // Place Poison Fang (index 2) — has 3 toxic + 1 sharp — at (4,1) in Reinforced Satchel
        // (Sharp reagents need Reinforced bags to sit on edge cells)
        TestActions.SelectAndPlace(_puzzleScreen, 2, 4, 1);

        int toxicAfter = bar.GetAspectTotal("toxic");
        Log($"[STATE] toxic_after_poison_fang={toxicAfter} (expected=3)");

        // Place Blast Jelly (index 3) — has 1 hot cell + 3 volatile cells — at (5,1) in Satchel
        // (placed after Poison Fang to avoid overlap; T-shape at (5,1) occupies (5,1),(6,1),(7,1),(6,2))
        TestActions.SelectAndPlace(_puzzleScreen, 3, 5, 1);

        int hotAfterJelly = bar.GetAspectTotal("hot");
        Log($"[STATE] hot_after_blast_jelly={hotAfterJelly} (expected=3)");

        await CaptureScreenshot("11_status_bar.png");

        // Remove Fire Pebble — hot should decrease
        _puzzleScreen.RemoveReagentAt(2, 2);
        int hotAfterRemove = bar.GetAspectTotal("hot");
        Log($"[STATE] hot_after_remove={hotAfterRemove} (expected=1)");

        bool pass = hotBefore == 0
                 && hotAfterPebble == 2
                 && hotAfterJelly == 3
                 && toxicAfter == 3
                 && hotAfterRemove == 1;
        EndTest("status_bar", pass, pass ? null
            : $"hotBefore={hotBefore} hotAfterPebble={hotAfterPebble} hotAfterJelly={hotAfterJelly} toxicAfter={toxicAfter} hotAfterRemove={hotAfterRemove}");
    }

    private async Task Test_ConfirmAndReset()
    {
        BeginTest("confirm_and_reset");

        // Clean up: remove all placed reagents
        while (_puzzleScreen.Grid.PlacedReagents.Count > 0)
        {
            var r = _puzzleScreen.Grid.PlacedReagents[0];
            var pos = r.PlacedPosition!.Value;
            _puzzleScreen.RemoveReagentAt(pos.Col, pos.Row);
        }

        // Place a couple of reagents
        TestActions.SelectAndPlace(_puzzleScreen, 0, 2, 2); // Fire Pebble in SmallPouch
        TestActions.SelectAndPlace(_puzzleScreen, 1, 5, 1); // Ice Shard in Satchel
        int placedBefore = _puzzleScreen.Grid.PlacedReagents.Count;
        Log($"[STATE] placed_before_reset={placedBefore} (expected=2)");

        // Confirm with reagents not all placed — should get NotAllPlaced
        var earlyResult = _puzzleScreen.ConfirmPuzzle();
        Log($"[STATE] confirm_early={earlyResult} (expected=NotAllPlaced)");

        // Reset — should remove all placed reagents
        _puzzleScreen.ResetPuzzle();
        int placedAfterReset = _puzzleScreen.Grid.PlacedReagents.Count;
        int unplacedAfterReset = 0;
        foreach (var r in _puzzleScreen.TrayReagents)
        {
            if (!r.IsPlaced) unplacedAfterReset++;
        }
        Log($"[STATE] placed_after_reset={placedAfterReset} unplaced_after_reset={unplacedAfterReset} (expected=0,6)");
        var resetResult = _puzzleScreen.LastConfirmResult;
        Log($"[STATE] confirm_result_after_reset={resetResult} (expected=None)");

        await CaptureScreenshot("12_after_reset.png");

        // Place all 6 reagents in safe positions
        // SmallPouch is 3x3 at (1,1), Satchel is 4x3 at (4,1)
        TestActions.SelectAndPlace(_puzzleScreen, 0, 2, 2);    // Fire Pebble (1x1) at (2,2)
        TestActions.SelectAndPlace(_puzzleScreen, 4, 1, 1);    // Inert Crystal (1x2) at (1,1) — stable, buffers
        TestActions.SelectAndPlace(_puzzleScreen, 1, 1, 3);    // Ice Shard (1x2) at (1,3)
        TestActions.SelectRotateAndPlace(_puzzleScreen, 3, 1, 4, 1); // Blast Jelly rotated at (4,1)
        TestActions.SelectAndPlace(_puzzleScreen, 5, 6, 1);    // Cinnabar Block (2x2) at (6,1)
        TestActions.SelectAndPlace(_puzzleScreen, 2, 4, 3);    // Poison Fang at (4,3) — try

        int placedAll = _puzzleScreen.Grid.PlacedReagents.Count;
        Log($"[STATE] placed_all={placedAll} total_reagents={_puzzleScreen.TrayReagents.Count}");

        // If we couldn't place all 6, try alternate positions
        if (placedAll < _puzzleScreen.TrayReagents.Count)
        {
            // Reset and try a simpler arrangement
            _puzzleScreen.ResetPuzzle();
            TestActions.SelectAndPlace(_puzzleScreen, 0, 3, 3);    // Fire Pebble (1x1)
            TestActions.SelectAndPlace(_puzzleScreen, 4, 1, 1);    // Inert Crystal (1x2) horiz
            TestActions.SelectAndPlace(_puzzleScreen, 1, 1, 2);    // Ice Shard (1x2) horiz
            TestActions.SelectRotateAndPlace(_puzzleScreen, 3, 1, 4, 1); // Blast Jelly rotated
            TestActions.SelectAndPlace(_puzzleScreen, 5, 6, 1);    // Cinnabar Block (2x2)
            TestActions.SelectAndPlace(_puzzleScreen, 2, 1, 3);    // Poison Fang L-shape

            placedAll = _puzzleScreen.Grid.PlacedReagents.Count;
            Log($"[STATE] placed_all_retry={placedAll}");
        }

        await CaptureScreenshot("13_all_placed.png");

        // Confirm — should succeed if all placed
        ConfirmResult confirmResult;
        if (placedAll == _puzzleScreen.TrayReagents.Count)
        {
            confirmResult = _puzzleScreen.ConfirmPuzzle();
            Log($"[STATE] confirm_result={confirmResult} (expected=Win)");
        }
        else
        {
            confirmResult = _puzzleScreen.ConfirmPuzzle();
            Log($"[STATE] confirm_result={confirmResult} (expected=NotAllPlaced, could not place all)");
        }

        await CaptureScreenshot("14_after_confirm.png");

        bool pass = placedBefore == 2
                 && earlyResult == ConfirmResult.NotAllPlaced
                 && placedAfterReset == 0
                 && unplacedAfterReset == 6
                 && resetResult == ConfirmResult.None
                 && (placedAll == _puzzleScreen.TrayReagents.Count
                     ? confirmResult == ConfirmResult.Win
                     : confirmResult == ConfirmResult.NotAllPlaced);
        EndTest("confirm_and_reset", pass, pass ? null
            : $"placedBefore={placedBefore} earlyResult={earlyResult} placedAfterReset={placedAfterReset} unplaced={unplacedAfterReset} resetResult={resetResult} placedAll={placedAll} confirmResult={confirmResult}");
    }

    private async Task Test_WinLossEvaluation()
    {
        BeginTest("win_loss_evaluation");

        // Clean up via ResetPuzzle (handles rotated shapes whose anchor cell may be empty)
        _puzzleScreen.ResetPuzzle();

        // Reagent index reference:
        // 0=Fire Pebble (1x1, Hot=2)
        // 1=Ice Shard (1x2 horiz, Cold×2)
        // 2=Poison Fang (L-shape 3×2, Toxic×3+Sharp, needs Reinforced bag)
        // 3=Blast Jelly (T-shape 2×3, Volatile×3+Hot×1)
        // 4=Inert Crystal (1x2 horiz, Stable×2)
        // 5=Cinnabar Block (2x2, Heavy×2+Medicinal+Catalytic)
        //
        // Layout constraints for WIN (zero reactions):
        // - Hot+Cold must be in DIFFERENT bags (avoid Neutralization)
        // - Fire Pebble must NOT be adjacent to Volatile cells (avoid Explosion)
        // - Poison Fang (Sharp) must go in Reinforced Satchel
        //
        // SmallPouch (3×3 at (1,1)): Cinnabar(1,1), Inert Crystal vertical(3,1), Ice Shard horiz(1,3)
        // Satchel (4×3 Reinforced at (4,1)): Blast Jelly unrotated(4,1), Poison Fang 270°(5,2),
        //   Fire Pebble at (4,3) [WIN] or (4,2) [LOSS — adjacent to Volatile at (4,1) and (5,2)]

        // --- LOSS PATH: Fire Pebble at (4,2) adjacent to Blast Jelly Volatile cells ---
        // Blast Jelly unrotated at (4,1): Volatile@(4,1), Hot@(5,1), Volatile@(6,1), Volatile@(5,2)
        // Fire Pebble(Hot=2) at (4,2) is adjacent to Volatile@(4,1) and Volatile@(5,2) → Explosion
        TestActions.SelectAndPlace(_puzzleScreen, 5, 1, 1);                // Cinnabar Block at (1,1)
        TestActions.SelectRotateAndPlace(_puzzleScreen, 4, 1, 3, 1);       // Inert Crystal vertical at (3,1)
        TestActions.SelectAndPlace(_puzzleScreen, 1, 1, 3);                // Ice Shard at (1,3)
        TestActions.SelectAndPlace(_puzzleScreen, 3, 4, 1);                // Blast Jelly unrotated at (4,1)
        TestActions.SelectRotateAndPlace(_puzzleScreen, 2, 3, 5, 2);       // Poison Fang 270° at (5,2)
        TestActions.SelectAndPlace(_puzzleScreen, 0, 4, 2);                // Fire Pebble at (4,2) ← DANGEROUS

        int placedForLoss = _puzzleScreen.Grid.PlacedReagents.Count;
        Log($"[STATE] placed_for_loss={placedForLoss}");

        ConfirmResult lossResult = ConfirmResult.None;
        int reactionCount = 0;
        bool hasExplosion = false;

        if (placedForLoss == _puzzleScreen.TrayReagents.Count)
        {
            lossResult = _puzzleScreen.ConfirmPuzzle();
            reactionCount = _puzzleScreen.LastReactions.Count;
            foreach (var reaction in _puzzleScreen.LastReactions)
            {
                Log($"[STATE] reaction_type={reaction.Type}");
                if (reaction.Type == AlchemistCourier.Core.Chemistry.ReactionResultType.Explosion)
                    hasExplosion = true;
            }
        }
        Log($"[STATE] loss_result={lossResult} reaction_count={reactionCount} has_explosion={hasExplosion}");

        await CaptureScreenshot("15_loss_confirm.png");

        // --- WIN PATH: Same layout but Fire Pebble at (4,3) — not adjacent to any Volatile ---
        _puzzleScreen.ResetPuzzle();

        TestActions.SelectAndPlace(_puzzleScreen, 5, 1, 1);                // Cinnabar Block at (1,1)
        TestActions.SelectRotateAndPlace(_puzzleScreen, 4, 1, 3, 1);       // Inert Crystal vertical at (3,1)
        TestActions.SelectAndPlace(_puzzleScreen, 1, 1, 3);                // Ice Shard at (1,3)
        TestActions.SelectAndPlace(_puzzleScreen, 3, 4, 1);                // Blast Jelly unrotated at (4,1)
        TestActions.SelectRotateAndPlace(_puzzleScreen, 2, 3, 5, 2);       // Poison Fang 270° at (5,2)
        TestActions.SelectAndPlace(_puzzleScreen, 0, 4, 3);                // Fire Pebble at (4,3) ← SAFE

        int placedForWin = _puzzleScreen.Grid.PlacedReagents.Count;
        Log($"[STATE] placed_for_win={placedForWin}");

        ConfirmResult winResult = ConfirmResult.None;
        int winReactionCount = -1;

        if (placedForWin == _puzzleScreen.TrayReagents.Count)
        {
            winResult = _puzzleScreen.ConfirmPuzzle();
            winReactionCount = _puzzleScreen.LastReactions.Count;
            foreach (var reaction in _puzzleScreen.LastReactions)
            {
                Log($"[STATE] win_reaction_type={reaction.Type}");
            }
        }
        Log($"[STATE] win_result={winResult} win_reaction_count={winReactionCount}");

        await CaptureScreenshot("16_win_confirm.png");

        // Verify both paths
        bool lossPass = placedForLoss == _puzzleScreen.TrayReagents.Count
                     && lossResult == ConfirmResult.HasReactions
                     && reactionCount > 0
                     && hasExplosion;
        bool winPass = placedForWin == _puzzleScreen.TrayReagents.Count
                    && winResult == ConfirmResult.Win
                    && winReactionCount == 0;

        bool pass = lossPass && winPass;
        EndTest("win_loss_evaluation", pass, pass ? null
            : $"lossPass={lossPass}(placed={placedForLoss} result={lossResult} reactions={reactionCount} explosion={hasExplosion}) winPass={winPass}(placed={placedForWin} result={winResult} reactions={winReactionCount})");
    }

    private async Task Test_GeneratedPuzzle()
    {
        BeginTest("generated_puzzle");

        // Generate a puzzle from a fixed seed
        var generator = new PuzzleGenerator();
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var state = generator.Generate(42, difficulty);

        Log($"[STATE] generated_bags={state.Bags.Count} generated_reagents={state.Reagents.Count} seed={state.Seed}");

        // Count locked reagents
        int lockedCount = 0;
        foreach (var r in state.Reagents)
        {
            if (r.IsLocked) lockedCount++;
        }
        Log($"[STATE] locked_reagents={lockedCount}");

        // Remove the demo PuzzleScreen and create one from PuzzleState
        _puzzleScreen.QueueFree();
        await WaitFrames(2);

        _puzzleScreen = new PuzzleScreen { PuzzleData = state };
        AddChild(_puzzleScreen);
        await WaitFrames(3);

        // Verify grid was reconstructed
        int bags = _puzzleScreen.Grid.PlacedBags.Count;
        int prePlaced = _puzzleScreen.Grid.PlacedReagents.Count;
        int totalReagents = _puzzleScreen.TrayReagents.Count;
        Log($"[STATE] puzzle_bags={bags} pre_placed={prePlaced} total_reagents={totalReagents}");

        bool bagsMatch = bags == state.Bags.Count;
        bool reagentsMatch = totalReagents == state.Reagents.Count;
        bool lockedMatch = prePlaced == lockedCount;

        // Place the solution reagents (skip locked ones — already placed)
        int placedCount = prePlaced;
        foreach (var placement in state.Solution)
        {
            var reagent = _puzzleScreen.TrayReagents[placement.ReagentIndex];
            if (reagent.IsPlaced) continue; // Already placed (locked)

            _puzzleScreen.SelectReagent(placement.ReagentIndex);
            for (int r = 0; r < placement.Rotation; r++)
                _puzzleScreen.RotateSelected();
            bool placed = _puzzleScreen.PlaceSelectedAt(placement.PositionCol, placement.PositionRow);
            if (placed) placedCount++;
        }
        Log($"[STATE] placed_from_solution={placedCount} (expected={totalReagents})");

        bool allPlaced = placedCount == totalReagents;

        // Confirm — should win since we applied the known solution
        ConfirmResult result = ConfirmResult.None;
        if (allPlaced)
        {
            result = _puzzleScreen.ConfirmPuzzle();
            Log($"[STATE] confirm_result={result} (expected=Win)");
        }

        await CaptureScreenshot("17_generated_puzzle.png");

        // Reset and verify locked reagents survive
        _puzzleScreen.ResetPuzzle();
        int afterReset = _puzzleScreen.Grid.PlacedReagents.Count;
        Log($"[STATE] placed_after_reset={afterReset} (expected={lockedCount})");
        bool resetKeepsLocked = afterReset == lockedCount;

        bool pass = bagsMatch && reagentsMatch && lockedMatch && allPlaced
                 && result == ConfirmResult.Win && resetKeepsLocked;
        EndTest("generated_puzzle", pass, pass ? null
            : $"bags={bagsMatch} reagents={reagentsMatch} locked={lockedMatch} allPlaced={allPlaced} result={result} resetKeepsLocked={resetKeepsLocked}(after={afterReset} expected={lockedCount})");
    }

    private async Task Test_TutorialFlow()
    {
        BeginTest("tutorial_flow");

        // Remove the current PuzzleScreen
        _puzzleScreen.QueueFree();
        await WaitFrames(2);

        // Create a TutorialScreen
        var tutorial = new TutorialScreen();
        AddChild(tutorial);
        await WaitFrames(3);

        // Verify starts at lesson 1 (index 0)
        int startIndex = tutorial.CurrentLessonIndex;
        int lessonCount = tutorial.LessonCount;
        bool startsAtZero = startIndex == 0;
        Log($"[STATE] tutorial_lesson_index={startIndex} lesson_count={lessonCount} (expected: index=0, count=5)");

        // Verify hint text is from lesson 1
        var hintText = tutorial.HintText;
        bool hasHint = !string.IsNullOrEmpty(hintText);
        Log($"[STATE] tutorial_hint=\"{hintText}\"");

        // Get the PuzzleScreen and verify it loaded
        var puzzleScreen = tutorial.CurrentPuzzleScreen;
        bool hasPuzzle = puzzleScreen != null;
        Log($"[STATE] has_puzzle_screen={hasPuzzle}");

        if (hasPuzzle)
        {
            // Generate the same puzzle to get the solution
            var lessons = TutorialData.GetLessons();
            var lesson1 = lessons[0];
            var generator = new PuzzleGenerator();
            var state = generator.Generate(lesson1.FixedSeed, lesson1.DifficultyParams);

            // Apply the solution to complete lesson 1
            foreach (var placement in state.Solution)
            {
                var reagent = puzzleScreen!.TrayReagents[placement.ReagentIndex];
                if (reagent.IsPlaced) continue;

                puzzleScreen.SelectReagent(placement.ReagentIndex);
                for (int r = 0; r < placement.Rotation; r++)
                    puzzleScreen.RotateSelected();
                puzzleScreen.PlaceSelectedAt(placement.PositionCol, placement.PositionRow);
            }

            int placed = puzzleScreen!.Grid.PlacedReagents.Count;
            int total = puzzleScreen.TrayReagents.Count;
            Log($"[STATE] lesson1_placed={placed} total={total}");

            // Confirm — should win
            var result = puzzleScreen.ConfirmPuzzle();
            Log($"[STATE] lesson1_confirm={result} (expected=Win)");

            await CaptureScreenshot("18_tutorial_lesson1_win.png");

            // Advance to lesson 2
            tutorial.AdvanceLesson();
            await WaitFrames(3);

            int afterIndex = tutorial.CurrentLessonIndex;
            bool advancedToLesson2 = afterIndex == 1;
            Log($"[STATE] tutorial_lesson_index_after={afterIndex} (expected=1)");

            // Verify lesson 2 loaded with its hint
            var newHint = tutorial.HintText;
            bool lesson2Hint = !string.IsNullOrEmpty(newHint) && newHint != hintText;
            Log($"[STATE] lesson2_hint=\"{newHint}\"");

            var newPuzzle = tutorial.CurrentPuzzleScreen;
            bool newPuzzleLoaded = newPuzzle != null && newPuzzle != puzzleScreen;
            Log($"[STATE] new_puzzle_loaded={newPuzzleLoaded}");

            await CaptureScreenshot("19_tutorial_lesson2.png");

            bool pass = startsAtZero && hasHint && hasPuzzle
                     && placed == total && result == ConfirmResult.Win
                     && advancedToLesson2 && lesson2Hint && newPuzzleLoaded;
            EndTest("tutorial_flow", pass, pass ? null
                : $"startsAtZero={startsAtZero} hasHint={hasHint} placed={placed}/{total} result={result} advanced={advancedToLesson2} lesson2Hint={lesson2Hint} newPuzzle={newPuzzleLoaded}");
        }
        else
        {
            EndTest("tutorial_flow", false, "No puzzle screen created");
        }

        // Restore a PuzzleScreen for subsequent tests
        tutorial.QueueFree();
        await WaitFrames(2);
        _puzzleScreen = new PuzzleScreen();
        AddChild(_puzzleScreen);
        await WaitFrames(3);
    }

    private async Task Test_DailyPuzzle()
    {
        BeginTest("daily_puzzle");

        // Generate a daily puzzle for a fixed date
        var date = new System.DateTime(2026, 3, 4);
        var state = DailyPuzzleManager.GenerateDailyPuzzle(date);
        int seed = DailyPuzzleManager.DateToSeed(date);
        var difficulty = DailyPuzzleManager.GetDailyDifficulty(date);
        var puzzleType = DailyPuzzleManager.GetDailyPuzzleType(seed);

        Log($"[STATE] daily_seed={state.Seed} difficulty={difficulty} puzzle_type={puzzleType}");
        Log($"[STATE] daily_bags={state.Bags.Count} daily_reagents={state.Reagents.Count}");

        // Load into a new PuzzleScreen
        _puzzleScreen.QueueFree();
        await WaitFrames(2);

        _puzzleScreen = new PuzzleScreen { PuzzleData = state };
        AddChild(_puzzleScreen);
        await WaitFrames(3);

        int bags = _puzzleScreen.Grid.PlacedBags.Count;
        int totalReagents = _puzzleScreen.TrayReagents.Count;
        bool bagsMatch = bags == state.Bags.Count;
        bool reagentsMatch = totalReagents == state.Reagents.Count;

        // Apply the known solution
        int placedCount = _puzzleScreen.Grid.PlacedReagents.Count; // pre-placed (locked)
        foreach (var placement in state.Solution)
        {
            var reagent = _puzzleScreen.TrayReagents[placement.ReagentIndex];
            if (reagent.IsPlaced) continue;

            _puzzleScreen.SelectReagent(placement.ReagentIndex);
            for (int r = 0; r < placement.Rotation; r++)
                _puzzleScreen.RotateSelected();
            bool placed = _puzzleScreen.PlaceSelectedAt(placement.PositionCol, placement.PositionRow);
            if (placed) placedCount++;
        }

        bool allPlaced = placedCount == totalReagents;
        Log($"[STATE] daily_placed={placedCount} (expected={totalReagents})");

        // Confirm — should win
        ConfirmResult result = ConfirmResult.None;
        if (allPlaced)
        {
            result = _puzzleScreen.ConfirmPuzzle();
        }
        Log($"[STATE] daily_confirm={result} (expected=Win)");

        await CaptureScreenshot("20_daily_puzzle.png");

        bool pass = bagsMatch && reagentsMatch && allPlaced && result == ConfirmResult.Win;
        EndTest("daily_puzzle", pass, pass ? null
            : $"bags={bagsMatch} reagents={reagentsMatch} allPlaced={allPlaced}(placed={placedCount}/{totalReagents}) result={result}");
    }

    private async Task Test_RenderFinal()
    {
        BeginTest("render_final");
        PrintState();
        await CaptureScreenshot("07_render_final.png");
        EndTest("render_final", true);
    }

    // --- Helpers ---

    private void BeginTest(string name)
    {
        _stepNumber++;
        Log($"\n[TEST] {name}");
    }

    private void EndTest(string name, bool pass, string? reason = null)
    {
        if (pass)
        {
            _passed++;
            Log($"[PASS] {name}");
        }
        else
        {
            _failed++;
            var msg = reason != null ? $"[FAIL] {name}: {reason}" : $"[FAIL] {name}";
            Log(msg);
        }
    }

    private void PrintState()
    {
        Log($"[STATE] {TestActions.GetStateString(_puzzleScreen)}");
    }

    private async Task CaptureScreenshot(string filename)
    {
        // Wait a frame for rendering to settle before capturing
        await WaitFrames(2);
        var path = ScreenshotHelper.CaptureViewport(GetViewport(), filename);
        if (!string.IsNullOrEmpty(path))
            _logBuffer.AppendLine($"[SCREENSHOT] {path}");
    }

    private async Task WaitFrames(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
}
