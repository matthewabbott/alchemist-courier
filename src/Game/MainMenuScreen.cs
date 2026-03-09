using System;
using Godot;
using AlchemistCourier.Core.Save;

namespace AlchemistCourier.Game;

/// <summary>
/// Main menu screen with styled buttons for Daily Puzzle, Tutorial, and Editor.
/// Shows progress info from SaveData when available.
/// </summary>
public partial class MainMenuScreen : Control
{
    /// <summary>Set before adding to scene tree.</summary>
    public SaveData? SaveInfo { get; set; }

    /// <summary>Emitted when the player requests a screen transition.</summary>
    public event Action<GameScreen>? ScreenRequested;

    public override void _Ready()
    {
        AnchorsPreset = (int)LayoutPreset.FullRect;
        SetAnchorsPreset(LayoutPreset.FullRect);

        BuildUI();
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

        // Center container
        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        center.AddChild(vbox);

        // Title
        var title = new Label
        {
            Text = "Alchemist Courier",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        title.AddThemeFontSizeOverride("font_size", 28);
        vbox.AddChild(title);

        // Spacer
        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 16) });

        // Button container
        var buttonBox = new VBoxContainer();
        buttonBox.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(buttonBox);

        // Daily Puzzle button (green, shows completion/streak)
        string dailyText = "Daily Puzzle";
        if (SaveInfo != null)
        {
            int streak = SaveInfo.GetEffectiveStreak(DateTime.UtcNow.Date);
            if (SaveInfo.IsDailyCompleted(DateTime.UtcNow.Date))
                dailyText = "Daily Puzzle (Completed)";
            else if (streak > 0)
                dailyText = $"Daily Puzzle (Streak: {streak})";
        }
        var dailyButton = CreateStyledButton(dailyText, new Color(0.25f, 0.5f, 0.3f));
        dailyButton.Pressed += () => ScreenRequested?.Invoke(GameScreen.DailyPuzzle);
        buttonBox.AddChild(dailyButton);

        // Tutorial button (shows lesson progress)
        string tutorialText = "Tutorial";
        if (SaveInfo != null)
        {
            if (SaveInfo.IsTutorialCompleted())
                tutorialText = "Tutorial (Complete)";
            else
                tutorialText = $"Tutorial (Lesson {SaveInfo.GetNextTutorialLesson()}/5)";
        }
        var tutorialButton = CreateStyledButton(tutorialText, new Color(0.3f, 0.35f, 0.5f));
        tutorialButton.Pressed += () => ScreenRequested?.Invoke(GameScreen.Tutorial);
        buttonBox.AddChild(tutorialButton);

        // Editor button (neutral color)
        var editorButton = CreateStyledButton("Editor", new Color(0.3f, 0.3f, 0.3f));
        editorButton.Pressed += () => ScreenRequested?.Invoke(GameScreen.Editor);
        buttonBox.AddChild(editorButton);

        // Streak footer
        if (SaveInfo != null)
        {
            int effectiveStreak = SaveInfo.GetEffectiveStreak(DateTime.UtcNow.Date);
            if (effectiveStreak > 0)
            {
                var streak = new Label
                {
                    Text = $"Current Streak: {effectiveStreak}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                streak.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.5f));
                streak.AddThemeFontSizeOverride("font_size", 12);
                vbox.AddChild(streak);
            }
        }
    }

    private static Button CreateStyledButton(string text, Color bgColor)
    {
        var button = new Button { Text = text };
        button.CustomMinimumSize = new Vector2(240, 40);
        var normal = new StyleBoxFlat
        {
            BgColor = bgColor,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
        button.AddThemeStyleboxOverride("normal", normal);
        var hover = new StyleBoxFlat
        {
            BgColor = bgColor.Lightened(0.15f),
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
        button.AddThemeStyleboxOverride("hover", hover);
        var pressed = new StyleBoxFlat
        {
            BgColor = bgColor.Darkened(0.15f),
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
        button.AddThemeStyleboxOverride("pressed", pressed);
        return button;
    }
}
