using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Shared formatting helpers for aspect/reaction display across UI panels.
/// </summary>
public static class AspectFormatHelpers
{
    public static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

    public static string FormatAspects(List<string> aspects)
    {
        if (aspects.Count == 1)
            return Capitalize(aspects[0]);
        return $"{Capitalize(aspects[0])} + {Capitalize(aspects[1])}";
    }

    public static string FormatSensitivity(SensitivityTier tier) => tier switch
    {
        SensitivityTier.Adjacent => "adjacent",
        SensitivityTier.SameBag => "same bag",
        SensitivityTier.WholeInventory => "cumulative",
        _ => tier.ToString(),
    };

    public static string FormatRule(ReactionRule rule)
    {
        var aspects = FormatAspects(rule.TriggerAspects);
        var sensitivity = FormatSensitivity(rule.Sensitivity);
        var threshold = rule.Threshold > 0 ? $" >{rule.Threshold}" : "";
        return $"{aspects} ({sensitivity}{threshold}) = {rule.Name}";
    }

    public static Color GetRuleColor(ReactionResultType type) => type switch
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
}
