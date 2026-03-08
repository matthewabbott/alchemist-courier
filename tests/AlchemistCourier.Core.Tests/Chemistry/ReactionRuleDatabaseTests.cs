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
        Assert.Equal(4, rules.Count);
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
    public void GetMvpRules_DoesNotContainNeutralizationRule()
    {
        // I.2: Neutralization removed — hot/cold balance is per-bag confirm-time check
        var rules = ReactionRuleDatabase.GetMvpRules();
        Assert.DoesNotContain(rules, r => r.Name == "Neutralization");
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
    public void GetMvpRules_DoesNotContainToxicRules()
    {
        // I.3: Toxic rules removed from MVP set
        var rules = ReactionRuleDatabase.GetMvpRules();
        Assert.DoesNotContain(rules, r => r.Name == "Toxic Cloud");
        Assert.DoesNotContain(rules, r => r.Name == "Poisoning");
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

    // GetMvpRules_PoisoningThresholdIs3 removed — I.3: toxic excluded from MVP

    [Fact]
    public void GetRulesForAspects_HotVolatile_ReturnsExplosionAndOverheating()
    {
        var rules = ReactionRuleDatabase.GetRulesForAspects("hot", "volatile");

        Assert.Contains(rules, r => r.Name == "Explosion");
        Assert.Contains(rules, r => r.Name == "Overheating");
        Assert.Equal(2, rules.Count);
    }

    [Fact]
    public void GetRulesForAspects_HotCold_ReturnsCumulativeOnly()
    {
        // I.2: Neutralization removed — only cumulative threshold rules remain
        var rules = ReactionRuleDatabase.GetRulesForAspects("hot", "cold");

        Assert.DoesNotContain(rules, r => r.Name == "Neutralization");
        Assert.Contains(rules, r => r.Name == "Overheating");
        Assert.Contains(rules, r => r.Name == "Hypothermia");
        Assert.Equal(2, rules.Count);
    }

    [Fact]
    public void GetRulesForAspects_SharpFragile_ReturnsBreakage()
    {
        var rules = ReactionRuleDatabase.GetRulesForAspects("sharp", "fragile");

        var rule = Assert.Single(rules);
        Assert.Equal("Breakage", rule.Name);
    }

    [Fact]
    public void GetRulesForAspects_ToxicToxic_ReturnsEmpty()
    {
        // I.3: Toxic rules removed from MVP set
        var rules = ReactionRuleDatabase.GetRulesForAspects("toxic", "toxic");
        Assert.Empty(rules);
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
