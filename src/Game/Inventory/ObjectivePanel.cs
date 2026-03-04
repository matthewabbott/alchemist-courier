using Godot;
using AlchemistCourier.Core.Puzzle;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Displays the current puzzle objective (win condition) as a labeled text panel.
/// </summary>
public partial class ObjectivePanel : VBoxContainer
{
    private Label _objectiveLabel = null!;
    private PuzzleType _puzzleType;

    public string ObjectiveText => _objectiveLabel.Text;

    public void Initialize(PuzzleType puzzleType)
    {
        _puzzleType = puzzleType;

        AddThemeConstantOverride("separation", 4);

        var header = new Label
        {
            Text = "OBJECTIVE:",
        };
        header.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        header.AddThemeFontSizeOverride("font_size", 14);
        AddChild(header);

        _objectiveLabel = new Label
        {
            Text = GetObjectiveText(puzzleType),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(180, 0),
        };
        _objectiveLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.7f));
        _objectiveLabel.AddThemeFontSizeOverride("font_size", 13);
        AddChild(_objectiveLabel);
    }

    public void SetPuzzleType(PuzzleType puzzleType)
    {
        _puzzleType = puzzleType;
        _objectiveLabel.Text = GetObjectiveText(puzzleType);
    }

    public static string GetObjectiveText(PuzzleType puzzleType) => puzzleType switch
    {
        PuzzleType.FitAllSafely => "Place all reagents without triggering reactions.",
        PuzzleType.MaximizeValue => "Place the highest total value of reagents without reactions.",
        PuzzleType.MeetRequirement => "Meet the requirement without triggering reactions.",
        _ => "Complete the puzzle.",
    };
}
