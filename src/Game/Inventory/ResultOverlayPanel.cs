using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;

namespace AlchemistCourier.Game.Inventory;

public partial class ResultOverlayPanel : PanelContainer
{
    private readonly Label _label;

    public ResultOverlayPanel()
    {
        Visible = false;
        SetAnchorsPreset(LayoutPreset.Center);
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.08f, 0.94f),
            ContentMarginLeft = 32,
            ContentMarginRight = 32,
            ContentMarginTop = 20,
            ContentMarginBottom = 20,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            BorderColor = new Color(0.4f, 0.4f, 0.35f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
        };
        AddThemeStyleboxOverride("panel", style);

        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _label.AddThemeFontSizeOverride("font_size", 18);
        AddChild(_label);
    }

    public void ShowWin(string message, int score = 0)
    {
        if (score > 0)
            _label.Text = $"{message}\nScore: {score}";
        else
            _label.Text = message;
        _label.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 0.3f));
        Visible = true;
    }

    public void ShowFailure(List<ReactionResult> reactions)
    {
        var lines = new System.Text.StringBuilder();
        lines.AppendLine("FAILED — Reactions triggered:");
        foreach (var r in reactions)
        {
            lines.AppendLine($"  - {FormatReactionType(r.Type)}");
        }
        _label.Text = lines.ToString().TrimEnd();
        _label.AddThemeColorOverride("font_color", new Color(0.95f, 0.3f, 0.3f));
        Visible = true;
    }

    public static string FormatReactionType(ReactionResultType type)
    {
        return type switch
        {
            ReactionResultType.Explosion => "Explosion!",
            ReactionResultType.Neutralization => "Neutralization",
            ReactionResultType.Breakage => "Breakage",
            ReactionResultType.Overheating => "Overheating",
            ReactionResultType.Hypothermia => "Hypothermia",
            ReactionResultType.Poisoning => "Poisoning",
            ReactionResultType.ToxicCloud => "Toxic Cloud",
            _ => type.ToString(),
        };
    }
}
