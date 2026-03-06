using System;
using Godot;
using AlchemistCourier.Core.Save;

namespace AlchemistCourier.Game;

/// <summary>
/// Main menu screen with buttons for Daily Puzzle, Tutorial, and Editor.
/// Stub implementation — GF.2 will add full UI.
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

        // Buttons
        AddMenuButton(vbox, "Daily Puzzle", GameScreen.DailyPuzzle);
        AddMenuButton(vbox, "Tutorial", GameScreen.Tutorial);
        AddMenuButton(vbox, "Editor", GameScreen.Editor);

        // Streak footer
        if (SaveInfo != null && SaveInfo.CurrentStreak > 0)
        {
            var streak = new Label
            {
                Text = $"Current Streak: {SaveInfo.GetEffectiveStreak(DateTime.UtcNow.Date)}",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            streak.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.5f));
            streak.AddThemeFontSizeOverride("font_size", 12);
            vbox.AddChild(streak);
        }
    }

    private void AddMenuButton(VBoxContainer parent, string text, GameScreen target)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(200, 40),
        };
        button.Pressed += () => ScreenRequested?.Invoke(target);
        parent.AddChild(button);
    }
}
