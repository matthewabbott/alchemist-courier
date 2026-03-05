using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Game.Inventory;

namespace AlchemistCourier.Game;

/// <summary>
/// Manages the tutorial lesson flow. Shows one PuzzleScreen per lesson,
/// displays hint text, and advances to the next lesson on win.
/// </summary>
public partial class TutorialScreen : Control
{
    private List<TutorialLesson> _lessons = null!;
    private int _currentLessonIndex;
    private PuzzleScreen? _puzzleScreen;

    private VBoxContainer _mainVBox = null!;
    private Label _lessonLabel = null!;
    private Label _hintLabel = null!;
    private PanelContainer _hintPanel = null!;
    private CenterContainer _nextBar = null!;
    private Button _nextButton = null!;

    // --- Public state inspection ---
    public int CurrentLessonIndex => _currentLessonIndex;
    public PuzzleScreen? CurrentPuzzleScreen => _puzzleScreen;
    public int LessonCount => _lessons.Count;
    public bool IsComplete => _currentLessonIndex >= _lessons.Count;
    public string HintText => _hintLabel?.Text ?? string.Empty;

    public override void _Ready()
    {
        AnchorsPreset = (int)LayoutPreset.FullRect;
        SetAnchorsPreset(LayoutPreset.FullRect);

        _lessons = TutorialData.GetLessons();
        _currentLessonIndex = 0;

        BuildUI();
        LoadCurrentLesson();
    }

    private void BuildUI()
    {
        // Background
        var bg = new ColorRect
        {
            Color = new Color(0.1f, 0.1f, 0.12f),
            AnchorsPreset = (int)LayoutPreset.FullRect,
        };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Main layout
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        _mainVBox = new VBoxContainer();
        margin.AddChild(_mainVBox);

        // Hint panel at top
        _hintPanel = new PanelContainer();
        var hintStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.18f, 0.22f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
        _hintPanel.AddThemeStyleboxOverride("panel", hintStyle);
        _mainVBox.AddChild(_hintPanel);

        var hintVBox = new VBoxContainer();
        hintVBox.AddThemeConstantOverride("separation", 4);
        _hintPanel.AddChild(hintVBox);

        _lessonLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _lessonLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        _lessonLabel.AddThemeFontSizeOverride("font_size", 18);
        hintVBox.AddChild(_lessonLabel);

        _hintLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _hintLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 0.85f));
        _hintLabel.AddThemeFontSizeOverride("font_size", 14);
        hintVBox.AddChild(_hintLabel);

        // PuzzleScreen placeholder — will be added in LoadCurrentLesson
        // (occupies main area with ExpandFill)

        // Next lesson bar at bottom (hidden until win)
        _nextBar = new CenterContainer();
        _nextBar.Visible = false;
        _mainVBox.AddChild(_nextBar);

        _nextButton = new Button { Text = "Next Lesson" };
        _nextButton.CustomMinimumSize = new Vector2(160, 36);
        _nextButton.Pressed += OnNextPressed;
        _nextBar.AddChild(_nextButton);
    }

    private void LoadCurrentLesson()
    {
        // Remove old puzzle screen
        if (_puzzleScreen != null)
        {
            _puzzleScreen.PuzzleConfirmed -= OnPuzzleConfirmed;
            _mainVBox.RemoveChild(_puzzleScreen);
            _puzzleScreen.QueueFree();
            _puzzleScreen = null;
        }

        if (_currentLessonIndex >= _lessons.Count)
        {
            _lessonLabel.Text = "Tutorial Complete!";
            _hintLabel.Text = "You've finished all available lessons.";
            _nextBar.Visible = false;
            return;
        }

        var lesson = _lessons[_currentLessonIndex];

        // Generate puzzle from lesson parameters
        var generator = new PuzzleGenerator();
        var state = generator.Generate(lesson.FixedSeed, lesson.DifficultyParams);
        state.PuzzleType = lesson.RequiredPuzzleType;

        _puzzleScreen = new PuzzleScreen { PuzzleData = state };
        _puzzleScreen.SizeFlagsVertical = SizeFlags.ExpandFill;
        _puzzleScreen.PuzzleConfirmed += OnPuzzleConfirmed;

        // Insert before the next-lesson bar
        _mainVBox.AddChild(_puzzleScreen);
        _mainVBox.MoveChild(_puzzleScreen, _mainVBox.GetChildCount() - 2);

        // Update lesson display
        _lessonLabel.Text = $"Lesson {lesson.LessonNumber}: {lesson.ConceptName}";
        _hintLabel.Text = lesson.HintText;
        _nextBar.Visible = false;
    }

    private void OnPuzzleConfirmed(ConfirmResult result)
    {
        if (result != ConfirmResult.Win) return;

        if (_currentLessonIndex < _lessons.Count - 1)
        {
            _nextButton.Text = "Next Lesson";
        }
        else
        {
            _nextButton.Text = "Tutorial Complete!";
        }
        _nextBar.Visible = true;
    }

    private void OnNextPressed()
    {
        AdvanceLesson();
    }

    /// <summary>
    /// Advance to the next lesson. Called by the "Next Lesson" button
    /// or programmatically for testing.
    /// </summary>
    public void AdvanceLesson()
    {
        _currentLessonIndex++;
        LoadCurrentLesson();
    }
}
