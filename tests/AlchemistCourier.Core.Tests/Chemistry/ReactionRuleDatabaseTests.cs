using System.Linq;
using Xunit;
using AlchemistCourier.Core.Chemistry;

namespace AlchemistCourier.Core.Tests.Chemistry;

public class ReactionRuleDatabaseTests
{
    [Fact]
    public void GetMvpRules_ReturnsCorrectCount()
    {
        var rules = ReactionRuleDatabase.GetMvpRules();
        Assert.Equal(7, rules.Count);
    }

    [Fact]
    public void GetMvpRules_ContainsExplosionRule()
    {
        var rules = ReactionRuleDatabase.GetMvpRules();
        var explosion = rules.Single(r => r.Name == "Explosion");

        Assert.Single(explosion.TriggerAspects);
        Assert.Contains("volatile", explosion.TriggerAspects);
        Assert.Equal(SensitivityTier.Adjacent, explosion.Sensitivity);
        Assert.Equal(Granularity.PerCell, explosion.Granularity);
        Assert.Equal(0, explosion.Threshold);
        Assert.Equal(ReactionResultType.Explosion, explosion.ResultType);
    }

    [Fact]
    public void GetMvpRules_ContainsNeutralizationRule()
    {
        var rules = ReactionRuleDatabase.GetMvpRules();
        var neutralization = rules.Single(r => r.Name == "Neutralization");

        Assert.Contains("hot", neutralization.TriggerAspects);
        Assert.Contains("cold", neutralization.TriggerAspects);
        Assert.Equal(SensitivityTier.SameBag, neutralization.Sensitivity);
        Assert.Equal(Granularity.PerItem, neutralization.Granularity);
        Assert.Equal(0, neutralization.Threshold);
        Assert.Equal(ReactionResultType.Neutralization, neutralization.ResultType);
    }

    [Fact]
    public void GetMvpRules_ContainsBreakageRule()
    {
        var rules = ReactionRuleDatabase.GetMvpRules();
        var breakage = rules.Single(r => r.Name == "Breakage");

        Assert.Contains("sharp", breakage.TriggerAspects);
        Assert.Contains("fragile", breakage.TriggerAspects);
        Assert.Equal(SensitivityTier.Adjacent, breakage.Sensitivity);
        Assert.Equal(Granularity.PerCell, breakage.Granularity);
        Assert.Equal(0, breakage.Threshold);
        Assert.Equal(ReactionResultType.Breakage, breakage.ResultType);
    }

    [Fact]
    public void GetMvpRules_ContainsToxicCloudRule()
    {
        var rules = ReactionRuleDatabase.GetMvpRules();
        var toxicCloud = rules.Single(r => r.Name == "Toxic Cloud");

        Assert.Equal(2, toxicCloud.TriggerAspects.Count(a => a == "toxic"));
        Assert.Equal(SensitivityTier.SameBag, toxicCloud.Sensitivity);
        Assert.Equal(2, toxicCloud.Threshold);
        Assert.Equal(ReactionResultType.ToxicCloud, toxicCloud.ResultType);
    }

    [Fact]
    public void GetMvpRules_OverheatingThresholdIs4()
    {
        var rules = ReactionRuleDatabase.GetMvpRules();
        var overheating = rules.Single(r => r.Name == "Overheating");

        Assert.Equal(4, overheating.Threshold);
        Assert.Equal(SensitivityTier.WholeInventory, overheating.Sensitivity);
        Assert.Equal(ReactionResultType.Overheating, overheating.ResultType);
    }

    [Fact]
    public void GetMvpRules_HypothermiaThresholdIs4()
    {
        var rules = ReactionRuleDatabase.GetMvpRules();
        var hypothermia = rules.Single(r => r.Name == "Hypothermia");

        Assert.Equal(4, hypothermia.Threshold);
        Assert.Equal(SensitivityTier.WholeInventory, hypothermia.Sensitivity);
        Assert.Equal(ReactionResultType.Hypothermia, hypothermia.ResultType);
    }

    [Fact]
    public void GetMvpRules_PoisoningThresholdIs3()
    {
        var rules = ReactionRuleDatabase.GetMvpRules();
        var poisoning = rules.Single(r => r.Name == "Poisoning");

        Assert.Equal(3, poisoning.Threshold);
        Assert.Equal(SensitivityTier.WholeInventory, poisoning.Sensitivity);
        Assert.Equal(ReactionResultType.Poisoning, poisoning.ResultType);
    }

    [Fact]
    public void GetRulesForAspects_HotVolatile_ReturnsExplosionAndOverheating()
    {
        var rules = ReactionRuleDatabase.GetRulesForAspects("hot", "volatile");

        Assert.Contains(rules, r => r.Name == "Explosion");
        Assert.Contains(rules, r => r.Name == "Overheating");
        Assert.Equal(2, rules.Count);
    }

    [Fact]
    public void GetRulesForAspects_HotCold_ReturnsNeutralizationAndCumulative()
    {
        var rules = ReactionRuleDatabase.GetRulesForAspects("hot", "cold");

        Assert.Contains(rules, r => r.Name == "Neutralization");
        Assert.Contains(rules, r => r.Name == "Overheating");
        Assert.Contains(rules, r => r.Name == "Hypothermia");
        Assert.Equal(3, rules.Count);
    }

    [Fact]
    public void GetRulesForAspects_SharpFragile_ReturnsBreakage()
    {
        var rules = ReactionRuleDatabase.GetRulesForAspects("sharp", "fragile");

        var rule = Assert.Single(rules);
        Assert.Equal("Breakage", rule.Name);
    }

    [Fact]
    public void GetRulesForAspects_ToxicToxic_ReturnsToxicCloudAndPoisoning()
    {
        var rules = ReactionRuleDatabase.GetRulesForAspects("toxic", "toxic");

        Assert.Contains(rules, r => r.Name == "Toxic Cloud");
        Assert.Contains(rules, r => r.Name == "Poisoning");
        Assert.Equal(2, rules.Count);
    }

    [Fact]
    public void GetRulesForAspects_UnrelatedPair_ReturnsEmpty()
    {
        var rules = ReactionRuleDatabase.GetRulesForAspects("heavy", "stable");
        Assert.Empty(rules);
    }

    [Fact]
    public void GetRulesForAspects_OrderDoesNotMatter()
    {
        var forward = ReactionRuleDatabase.GetRulesForAspects("hot", "volatile");
        var reverse = ReactionRuleDatabase.GetRulesForAspects("volatile", "hot");

        Assert.Equal(forward.Count, reverse.Count);
        Assert.Equal(
            forward.Select(r => r.Name).OrderBy(n => n),
            reverse.Select(r => r.Name).OrderBy(n => n));
    }

}
