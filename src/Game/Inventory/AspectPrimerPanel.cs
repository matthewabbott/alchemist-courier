using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Toggleable overlay panel showing aspect interaction rules from the ReactionRuleDatabase.
/// Each rule is displayed as a one-line summary with trigger aspects, sensitivity, and result.
/// </summary>
public partial class AspectPrimerPanel : PanelContainer
{
    private VBoxContainer _content = null!;

    public void Initialize(List<ReactionRule> rules)
    {
        Visible = false;
        CustomMinimumSize = new Vector2(320, 0);

        // Semi-transparent dark background
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f, 0.95f),
            BorderColor = new Color(0.5f, 0.45f, 0.35f, 0.6f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
        };
        AddThemeStyleboxOverride("panel", style);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 4);
        AddChild(_content);

        // Header
        var header = new Label
        {
            Text = "REACTIONS:",
        };
        header.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        header.AddThemeFontSizeOverride("font_size", 14);
        _content.AddChild(header);

        // Add a rule entry for each reaction rule
        foreach (var rule in rules)
        {
            var label = new Label
            {
                Text = FormatRule(rule),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            label.AddThemeColorOverride("font_color", GetRuleColor(rule.ResultType));
            label.AddThemeFontSizeOverride("font_size", 12);
            _content.AddChild(label);
        }
    }

    public void Toggle()
    {
        Visible = !Visible;
    }

    public int RuleCount => _content.GetChildCount() - 1; // minus header

    internal static string FormatRule(ReactionRule rule)
    {
        var aspects = FormatAspects(rule.TriggerAspects);
        var sensitivity = FormatSensitivity(rule.Sensitivity);
        var threshold = rule.Threshold > 0 ? $" >{rule.Threshold}" : "";
        return $"{aspects} ({sensitivity}{threshold}) = {rule.Name}";
    }

    private static string FormatAspects(List<string> aspects)
    {
        if (aspects.Count == 1)
            return Capitalize(aspects[0]);
        if (aspects.Count == 2 && aspects[0] == aspects[1])
            return $"{Capitalize(aspects[0])} + {Capitalize(aspects[1])}";
        return $"{Capitalize(aspects[0])} + {Capitalize(aspects[1])}";
    }

    private static string FormatSensitivity(SensitivityTier tier) => tier switch
    {
        SensitivityTier.Adjacent => "adjacent",
        SensitivityTier.SameBag => "same bag",
        SensitivityTier.WholeInventory => "cumulative",
        _ => tier.ToString(),
    };

    private static Color GetRuleColor(ReactionResultType type) => type switch
    {
        ReactionResultType.Explosion => new Color(1.0f, 0.4f, 0.3f),
        ReactionResultType.Breakage => new Color(0.9f, 0.6f, 0.3f),
        ReactionResultType.ToxicCloud => new Color(0.5f, 0.85f, 0.4f),
        ReactionResultType.Neutralization => new Color(0.5f, 0.7f, 1.0f),
        ReactionResultType.Overheating => new Color(1.0f, 0.5f, 0.3f),
        ReactionResultType.Hypothermia => new Color(0.4f, 0.7f, 1.0f),
        ReactionResultType.Poisoning => new Color(0.6f, 0.9f, 0.4f),
        _ => new Color(0.75f, 0.75f, 0.7f),
    };

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}
