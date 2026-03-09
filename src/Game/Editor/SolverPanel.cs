using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Game.Inventory;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Editor;

/// <summary>
/// Panel in the puzzle editor that integrates PuzzleSearchSolver.
/// Provides Solve / Count Solutions buttons, status display,
/// and a Show Solution toggle to overlay the found solution on the MetaGridView.
/// Solver runs on a background thread to avoid freezing the UI.
/// </summary>
public partial class SolverPanel : VBoxContainer
{
    private static readonly Color HeaderColor = new(0.9f, 0.85f, 0.7f);
    private static readonly Color LabelColor = new(0.75f, 0.75f, 0.7f);
    private static readonly Color SolvableColor = new(0.3f, 0.85f, 0.4f);
    private static readonly Color UnsolvableColor = new(0.85f, 0.35f, 0.3f);
    private static readonly Color BusyColor = new(0.8f, 0.7f, 0.3f);
    private static readonly Color SolutionOverlayColor = new(0.3f, 0.6f, 0.9f, 0.35f);

    private Label _statusLabel = null!;
    private Button _solveBtn = null!;
    private Button _countBtn = null!;
    private Button _showSolutionBtn = null!;

    private CancellationTokenSource? _cts;
    private List<Placement>? _foundSolution;
    private bool _showingSolution;
    private bool _solving;

    private MetaGridView? _gridView;
    private Func<PuzzleState>? _getState;

    // Nodes created for the solution overlay
    private readonly List<ColorRect> _solutionOverlays = new();

    // Thread-safe result passing
    private object? _pendingResult;
    private Action<object?>? _pendingCallback;

    public override void _ExitTree()
    {
        CancelSolving();
    }

    public void Initialize(MetaGridView gridView, Func<PuzzleState> getState)
    {
        _gridView = gridView;
        _getState = getState;

        AddThemeConstantOverride("separation", 6);

        // Header
        var header = new Label { Text = "Solver" };
        header.AddThemeColorOverride("font_color", HeaderColor);
        header.AddThemeFontSizeOverride("font_size", 14);
        AddChild(header);

        // Button row
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 8);
        AddChild(toolbar);

        _solveBtn = CreateStyledButton("Solve", new Color(0.25f, 0.45f, 0.35f));
        _solveBtn.Pressed += OnSolvePressed;
        toolbar.AddChild(_solveBtn);

        _countBtn = CreateStyledButton("Count Solutions", new Color(0.3f, 0.35f, 0.45f));
        _countBtn.Pressed += OnCountPressed;
        toolbar.AddChild(_countBtn);

        _showSolutionBtn = CreateStyledButton("Show Solution", new Color(0.35f, 0.35f, 0.4f));
        _showSolutionBtn.Pressed += OnShowSolutionToggled;
        _showSolutionBtn.Disabled = true;
        toolbar.AddChild(_showSolutionBtn);

        // Status label
        _statusLabel = new Label { Text = "Ready" };
        _statusLabel.AddThemeColorOverride("font_color", LabelColor);
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        AddChild(_statusLabel);
    }

    /// <summary>
    /// Update the grid view reference (called when EditorScreen rebuilds the grid).
    /// </summary>
    public void UpdateGridView(MetaGridView gridView)
    {
        ClearSolutionOverlay();
        _gridView = gridView;
    }

    /// <summary>
    /// Clear solver results. Called when bags or reagents change in the editor.
    /// </summary>
    public void ClearResults()
    {
        CancelSolving();
        ClearSolutionOverlay();
        _foundSolution = null;
        _showingSolution = false;
        _showSolutionBtn.Disabled = true;
        SetStatus("Ready", LabelColor);
    }

    private void OnSolvePressed()
    {
        if (_solving) return;
        ClearSolutionOverlay();

        var state = _getState!();
        if (state.Reagents.Count == 0)
        {
            SetStatus("No reagents to solve.", LabelColor);
            return;
        }

        RunAsync("Solving...", ct =>
        {
            return (object?)PuzzleSearchSolver.Solve(state, ct);
        }, result =>
        {
            var solution = result as List<Placement>;
            _foundSolution = solution;
            if (solution != null)
            {
                _showSolutionBtn.Disabled = false;
                SetStatus($"Solvable ({solution.Count} placements)", SolvableColor);
            }
            else
            {
                _showSolutionBtn.Disabled = true;
                SetStatus("No solution found", UnsolvableColor);
            }
        });
    }

    private void OnCountPressed()
    {
        if (_solving) return;
        ClearSolutionOverlay();

        var state = _getState!();
        if (state.Reagents.Count == 0)
        {
            SetStatus("No reagents to count.", LabelColor);
            return;
        }

        RunAsync("Counting solutions...", ct =>
        {
            return (object)PuzzleSearchSolver.CountSolutions(state, 100, ct);
        }, result =>
        {
            int count = (int)result!;
            _foundSolution = null;
            _showSolutionBtn.Disabled = true;
            _showingSolution = false;

            if (count > 0)
            {
                string suffix = count >= 100 ? "+" : "";
                SetStatus($"Solvable ({count}{suffix} solutions found)", SolvableColor);
            }
            else
            {
                SetStatus("No solution found", UnsolvableColor);
            }
        });
    }

    private void OnShowSolutionToggled()
    {
        if (_foundSolution == null) return;

        _showingSolution = !_showingSolution;
        if (_showingSolution)
        {
            ShowSolutionOverlay();
            _showSolutionBtn.Text = "Hide Solution";
        }
        else
        {
            ClearSolutionOverlay();
            _showSolutionBtn.Text = "Show Solution";
        }
    }

    private void ShowSolutionOverlay()
    {
        ClearSolutionOverlay();
        if (_foundSolution == null || _gridView == null || _getState == null) return;

        var state = _getState();
        var reagents = PuzzleSolver.ReconstructReagents(state);

        foreach (var placement in _foundSolution)
        {
            if (placement.ReagentIndex < 0 || placement.ReagentIndex >= reagents.Count)
                continue;

            var reagent = reagents[placement.ReagentIndex];
            reagent.Rotation = placement.Rotation;

            var origin = new CoreVec(placement.PositionCol, placement.PositionRow);
            var positions = reagent.GetMetaGridPositions(origin);

            foreach (var pos in positions)
            {
                var pixelX = pos.Col * GridConstants.CellSize;
                var pixelY = pos.Row * GridConstants.CellSize;

                var overlay = new ColorRect
                {
                    Position = new Vector2(pixelX, pixelY),
                    Size = new Vector2(GridConstants.CellSize, GridConstants.CellSize),
                    Color = SolutionOverlayColor,
                    ZIndex = 3,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                };
                _gridView.AddChild(overlay);
                _solutionOverlays.Add(overlay);
            }
        }

        _showingSolution = true;
    }

    private void ClearSolutionOverlay()
    {
        foreach (var overlay in _solutionOverlays)
            overlay.QueueFree();
        _solutionOverlays.Clear();
        _showingSolution = false;
        if (_showSolutionBtn != null)
            _showSolutionBtn.Text = "Show Solution";
    }

    private void RunAsync(string busyMessage, Func<CancellationToken, object?> work, Action<object?> onComplete)
    {
        CancelSolving();
        _solving = true;
        SetStatus(busyMessage, BusyColor);
        SetButtonsEnabled(false);

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        Task.Run(() => work(ct), ct).ContinueWith(task =>
        {
            // Guard against node freed while solving
            if (!IsInstanceValid(this)) return;

            // Store result for main-thread retrieval
            _pendingResult = task.IsCompletedSuccessfully ? task.Result : null;
            _pendingCallback = onComplete;

            // Marshal to main thread via CallDeferred
            CallDeferred(MethodName.ProcessPendingResult);
        });
    }

    private void ProcessPendingResult()
    {
        _solving = false;
        SetButtonsEnabled(true);

        var callback = _pendingCallback;
        var result = _pendingResult;
        _pendingCallback = null;
        _pendingResult = null;

        if (callback != null)
            callback(result);
    }

    private void CancelSolving()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _solving = false;
        SetButtonsEnabled(true);
    }

    private void SetButtonsEnabled(bool enabled)
    {
        _solveBtn.Disabled = !enabled;
        _countBtn.Disabled = !enabled;
    }

    private void SetStatus(string text, Color color)
    {
        _statusLabel.Text = text;
        _statusLabel.RemoveThemeColorOverride("font_color");
        _statusLabel.AddThemeColorOverride("font_color", color);
    }

    private static Button CreateStyledButton(string text, Color bgColor)
    {
        var button = new Button { Text = text };
        var normal = new StyleBoxFlat
        {
            BgColor = bgColor,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
        button.AddThemeStyleboxOverride("normal", normal);

        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = bgColor.Lightened(0.15f);
        button.AddThemeStyleboxOverride("hover", hover);

        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.BgColor = bgColor.Darkened(0.1f);
        button.AddThemeStyleboxOverride("pressed", pressed);

        return button;
    }
}
