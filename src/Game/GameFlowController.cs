using System;
using Godot;
using AlchemistCourier.Core.Save;
using AlchemistCourier.Game.Editor;

namespace AlchemistCourier.Game;

/// <summary>
/// Manages screen transitions for the game. Creates and destroys screens
/// as the player navigates between main menu, tutorial, daily puzzle, and editor.
/// </summary>
public partial class GameFlowController : Control
{
    private SaveManager _saveManager = null!;
    private SaveData _saveData = null!;
    private Control? _currentScreenNode;

    /// <summary>Current screen enum value.</summary>
    public GameScreen CurrentScreen { get; private set; }

    /// <summary>Current screen node (for testing).</summary>
    public Control? CurrentScreenNode => _currentScreenNode;

    /// <summary>Save data (for testing).</summary>
    public SaveData SaveData => _saveData;

    public override void _Ready()
    {
        AnchorsPreset = (int)LayoutPreset.FullRect;
        SetAnchorsPreset(LayoutPreset.FullRect);

        var savePath = ProjectSettings.GlobalizePath("user://save.json");
        _saveManager = new SaveManager(savePath);
        _saveData = _saveManager.Load();

        if (_saveData.TutorialLessonCompleted == 0)
        {
            TransitionTo(GameScreen.Tutorial);
        }
        else
        {
            TransitionTo(GameScreen.MainMenu);
        }
    }

    /// <summary>
    /// Transition to a new screen, cleaning up the old one.
    /// </summary>
    public void TransitionTo(GameScreen screen)
    {
        RemoveCurrentScreen();

        _currentScreenNode = screen switch
        {
            GameScreen.MainMenu => CreateMainMenu(),
            GameScreen.Tutorial => CreateTutorial(),
            GameScreen.DailyPuzzle => CreateDailyPuzzle(),
            GameScreen.Editor => CreateEditor(),
            _ => throw new ArgumentOutOfRangeException(nameof(screen), screen, "Unhandled GameScreen value"),
        };

        _currentScreenNode.SizeFlagsVertical = SizeFlags.ExpandFill;
        _currentScreenNode.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(_currentScreenNode);
        CurrentScreen = screen;
    }

    private void RemoveCurrentScreen()
    {
        if (_currentScreenNode == null) return;

        RemoveChild(_currentScreenNode);
        _currentScreenNode.QueueFree();
        _currentScreenNode = null;
    }

    private Control CreateMainMenu()
    {
        var menu = new MainMenuScreen { SaveInfo = _saveData };
        menu.ScreenRequested += OnScreenRequested;
        return menu;
    }

    private Control CreateTutorial()
    {
        var tutorial = new TutorialScreen();
        tutorial.TutorialCompleted += OnTutorialCompleted;
        return tutorial;
    }

    private Control CreateDailyPuzzle()
    {
        var daily = new DailyPuzzleScreen { SaveInfo = _saveData };
        daily.DailyCompleted += OnDailyCompleted;
        daily.BackRequested += OnBackRequested;
        return daily;
    }

    private Control CreateEditor()
    {
        return new EditorScreen();
    }

    private void OnScreenRequested(GameScreen target)
    {
        TransitionTo(target);
    }

    private void OnTutorialCompleted()
    {
        _saveData.CompleteTutorialLesson(5);
        _saveManager.Save(_saveData);
        TransitionTo(GameScreen.MainMenu);
    }

    private void OnDailyCompleted()
    {
        _saveData.CompleteDailyPuzzle(DateTime.UtcNow.Date);
        _saveManager.Save(_saveData);
        TransitionTo(GameScreen.MainMenu);
    }

    private void OnBackRequested()
    {
        TransitionTo(GameScreen.MainMenu);
    }
}
