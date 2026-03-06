using System;
using Godot;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Core.Save;
using AlchemistCourier.Game.Inventory;

namespace AlchemistCourier.Game;

/// <summary>
/// Wrapper around PuzzleScreen for daily puzzle mode.
/// Generates today's puzzle, shows a back button, and emits completion events.
/// </summary>
public partial class DailyPuzzleScreen : Control
{
    private PuzzleScreen? _puzzleScreen;
    private bool _isCompleted;

    /// <summary>Set before adding to scene tree.</summary>
    public SaveData? SaveInfo { get; set; }

    /// <summary>Emitted when the daily puzzle is completed (win).</summary>
    public event Action? DailyCompleted;

    /// <summary>Emitted when the player presses Back.</summary>
    public event Action? BackRequested;

    /// <summary>The PuzzleScreen instance (for testing).</summary>
    public PuzzleScreen? CurrentPuzzleScreen => _puzzleScreen;

    /// <summary>Whether the puzzle has been completed.</summary>
    public bool IsCompleted => _isCompleted;

    /// <summary>The date of the puzzle being played.</summary>
    public DateTime PuzzleDate { get; private set; }

    public override void _Ready()
    {
        AnchorsPreset = (int)LayoutPreset.FullRect;
        SetAnchorsPreset(LayoutPreset.FullRect);

        PuzzleDate = DateTime.UtcNow.Date;

        // Background
        var bg = new ColorRect
        {
            Color = new Color(0.1f, 0.1f, 0.12f),
            AnchorsPreset = (int)LayoutPreset.FullRect,
        };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var mainVBox = new VBoxContainer();
        margin.AddChild(mainVBox);

        // Top bar with back button
        var topBar = new HBoxContainer();
        topBar.AddThemeConstantOverride("separation", 12);
        mainVBox.AddChild(topBar);

        var backButton = new Button { Text = "Back to Menu" };
        backButton.CustomMinimumSize = new Vector2(120, 32);
        backButton.Pressed += () => BackRequested?.Invoke();
        topBar.AddChild(backButton);

        var dayLabel = new Label
        {
            Text = $"Day {DateTime.UtcNow.DayOfYear}",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        dayLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.7f));
        topBar.AddChild(dayLabel);

        // Check if already completed today
        if (SaveInfo?.IsDailyCompleted(PuzzleDate) == true)
        {
            var doneLabel = new Label
            {
                Text = "Already completed today!",
                HorizontalAlignment = HorizontalAlignment.Center,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            doneLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 0.6f));
            doneLabel.AddThemeFontSizeOverride("font_size", 20);
            mainVBox.AddChild(doneLabel);
            _isCompleted = true;
            return;
        }

        // Generate and show daily puzzle
        var puzzle = DailyPuzzleManager.GenerateToday();

        _puzzleScreen = new PuzzleScreen
        {
            PuzzleData = puzzle,
            HeaderDayText = $"Day {DateTime.UtcNow.DayOfYear}",
            HeaderStreakText = SaveInfo != null
                ? $"Streak: {SaveInfo.GetEffectiveStreak(DateTime.UtcNow.Date)}"
                : null,
        };
        _puzzleScreen.SizeFlagsVertical = SizeFlags.ExpandFill;
        _puzzleScreen.PuzzleConfirmed += OnPuzzleConfirmed;
        mainVBox.AddChild(_puzzleScreen);
    }

    private void OnPuzzleConfirmed(ConfirmResult result)
    {
        if (result != ConfirmResult.Win) return;

        _isCompleted = true;
        DailyCompleted?.Invoke();
    }
}
