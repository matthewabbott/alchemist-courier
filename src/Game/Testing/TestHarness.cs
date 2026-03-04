using System.Threading.Tasks;
using Godot;
using AlchemistCourier.Game.Inventory;
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
        await Test_PlaceFirePebble();
        await Test_PlaceIceShardRotated();
        await Test_InvalidPlacement();
        await Test_PlacePoisonFang();
        await Test_RemoveReagent();
        await Test_PlacementWarnings();
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

        // Poison Fang (index 2) is an L-shape — place at (1,1) inside the SmallPouch
        bool placed = TestActions.SelectAndPlace(_puzzleScreen, 2, 1, 1);
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
