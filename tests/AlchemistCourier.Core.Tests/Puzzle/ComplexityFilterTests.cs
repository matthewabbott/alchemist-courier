using System.Linq;
using Xunit;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Puzzle;

namespace AlchemistCourier.Core.Tests.Puzzle;

public class ComplexityFilterTests
{
    [Fact]
    public void GetAspects_Simple_Returns6Aspects()
    {
        var aspects = ComplexityFilter.GetAspects(ComplexityLevel.Simple);

        Assert.Equal(6, aspects.Count);
        Assert.Contains(AspectIds.Hot, aspects);
        Assert.Contains(AspectIds.Cold, aspects);
        Assert.Contains(AspectIds.Volatile, aspects);
        Assert.Contains(AspectIds.Stable, aspects);
        Assert.Contains(AspectIds.Heavy, aspects);
        Assert.Contains(AspectIds.Toxic, aspects);
    }

    [Fact]
    public void GetAspects_Simple_ExcludesSharpAndFragile()
    {
        var aspects = ComplexityFilter.GetAspects(ComplexityLevel.Simple);

        Assert.DoesNotContain(AspectIds.Sharp, aspects);
        Assert.DoesNotContain(AspectIds.Fragile, aspects);
        Assert.DoesNotContain(AspectIds.Catalytic, aspects);
        Assert.DoesNotContain(AspectIds.Medicinal, aspects);
    }

    [Fact]
    public void GetAspects_Intermediate_Returns10Aspects()
    {
        var aspects = ComplexityFilter.GetAspects(ComplexityLevel.Intermediate);

        Assert.Equal(10, aspects.Count);
        // Includes Simple
        Assert.Contains(AspectIds.Hot, aspects);
        Assert.Contains(AspectIds.Volatile, aspects);
        // Plus Intermediate additions
        Assert.Contains(AspectIds.Sharp, aspects);
        Assert.Contains(AspectIds.Fragile, aspects);
        Assert.Contains(AspectIds.Catalytic, aspects);
        Assert.Contains(AspectIds.Medicinal, aspects);
    }

    [Fact]
    public void GetAspects_Advanced_Returns12Aspects()
    {
        var aspects = ComplexityFilter.GetAspects(ComplexityLevel.Advanced);

        Assert.Equal(12, aspects.Count);
        // Includes all Intermediate aspects
        Assert.Contains(AspectIds.Sharp, aspects);
        // Plus Advanced additions
        Assert.Contains("magnetic", aspects);
        Assert.Contains("absorbent", aspects);
    }

    [Fact]
    public void GetBagTypes_Simple_Returns4Types()
    {
        var types = ComplexityFilter.GetBagTypes(ComplexityLevel.Simple);

        Assert.Equal(4, types.Count);
        Assert.Contains(BagPropertyType.None, types);
        Assert.Contains(BagPropertyType.Null, types);
        Assert.Contains(BagPropertyType.Gravity, types);
        Assert.Contains(BagPropertyType.Locked, types);
    }

    [Fact]
    public void GetBagTypes_Simple_ExcludesIntermediateTypes()
    {
        var types = ComplexityFilter.GetBagTypes(ComplexityLevel.Simple);

        Assert.DoesNotContain(BagPropertyType.Reinforced, types);
        Assert.DoesNotContain(BagPropertyType.Insulated, types);
        Assert.DoesNotContain(BagPropertyType.Fragile, types);
    }

    [Fact]
    public void GetBagTypes_Intermediate_Returns7Types()
    {
        var types = ComplexityFilter.GetBagTypes(ComplexityLevel.Intermediate);

        Assert.Equal(7, types.Count);
        Assert.Contains(BagPropertyType.Reinforced, types);
        Assert.Contains(BagPropertyType.Insulated, types);
        Assert.Contains(BagPropertyType.Fragile, types);
    }

    [Fact]
    public void GetBagTypes_Advanced_Returns8Types()
    {
        var types = ComplexityFilter.GetBagTypes(ComplexityLevel.Advanced);

        Assert.Equal(8, types.Count);
        Assert.Contains(BagPropertyType.Levitating, types);
    }

    [Fact]
    public void GetRules_Simple_ExcludesBreakage()
    {
        // Breakage requires Sharp + Fragile (Intermediate aspects)
        var rules = ComplexityFilter.GetRules(ComplexityLevel.Simple);

        Assert.DoesNotContain(rules, r => r.Name == "Breakage");
    }

    [Fact]
    public void GetRules_Simple_IncludesSimpleAspectRules()
    {
        var rules = ComplexityFilter.GetRules(ComplexityLevel.Simple);

        Assert.Contains(rules, r => r.Name == "Explosion");
        Assert.Contains(rules, r => r.Name == "Neutralization");
        Assert.Contains(rules, r => r.Name == "Toxic Cloud");
        Assert.Contains(rules, r => r.Name == "Overheating");
        Assert.Contains(rules, r => r.Name == "Hypothermia");
        Assert.Contains(rules, r => r.Name == "Poisoning");
    }

    [Fact]
    public void GetRules_Simple_Returns6Rules()
    {
        var rules = ComplexityFilter.GetRules(ComplexityLevel.Simple);

        // All 7 MVP rules minus Breakage (Sharp+Fragile are Intermediate)
        Assert.Equal(6, rules.Count);
    }

    [Fact]
    public void GetRules_Intermediate_IncludesBreakage()
    {
        var rules = ComplexityFilter.GetRules(ComplexityLevel.Intermediate);

        Assert.Contains(rules, r => r.Name == "Breakage");
        Assert.Equal(7, rules.Count);
    }

    [Fact]
    public void GetRules_Advanced_IncludesAllRules()
    {
        var rules = ComplexityFilter.GetRules(ComplexityLevel.Advanced);

        Assert.Equal(7, rules.Count);
    }

    [Fact]
    public void ForLevel_Hard_ReturnsSimpleComplexity()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Hard);

        Assert.Equal(ComplexityLevel.Simple, difficulty.ComplexityLevel);
    }

    [Theory]
    [InlineData(DifficultyLevel.Tutorial1)]
    [InlineData(DifficultyLevel.Tutorial2)]
    [InlineData(DifficultyLevel.Tutorial3)]
    [InlineData(DifficultyLevel.Tutorial4)]
    [InlineData(DifficultyLevel.Tutorial5)]
    [InlineData(DifficultyLevel.Easy)]
    [InlineData(DifficultyLevel.Medium)]
    [InlineData(DifficultyLevel.Hard)]
    public void ForLevel_AllLevels_UseSimpleComplexity(DifficultyLevel level)
    {
        var difficulty = DifficultyParams.ForLevel(level);

        Assert.Equal(ComplexityLevel.Simple, difficulty.ComplexityLevel);
    }

    [Fact]
    public void DifficultyParams_DefaultConstructor_IsSimple()
    {
        var difficulty = new DifficultyParams();

        Assert.Equal(ComplexityLevel.Simple, difficulty.ComplexityLevel);
    }

    [Fact]
    public void DifficultyParams_CanSetIntermediate()
    {
        var difficulty = new DifficultyParams(2, 6, 12, 6, 2, 0.3f, false, ComplexityLevel.Intermediate);

        Assert.Equal(ComplexityLevel.Intermediate, difficulty.ComplexityLevel);
    }

    [Fact]
    public void PuzzleGenerator_SimpleComplexity_OnlyUsesSimpleAspects()
    {
        var generator = new PuzzleGenerator();
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var simpleAspects = ComplexityFilter.GetAspects(ComplexityLevel.Simple).ToHashSet();

        // Generate multiple puzzles to check aspect coverage
        for (int seed = 100; seed < 130; seed++)
        {
            var puzzle = generator.Generate(seed, difficulty);
            foreach (var reagent in puzzle.Reagents)
            {
                foreach (var cellAspects in reagent.AspectsPerCell)
                {
                    foreach (var aspectId in cellAspects)
                    {
                        Assert.Contains(aspectId, simpleAspects);
                    }
                }
            }
        }
    }

    [Fact]
    public void PuzzleGenerator_IntermediateComplexity_CanUseIntermediateAspects()
    {
        var generator = new PuzzleGenerator();
        var difficulty = new DifficultyParams(3, 6, 12, 8, 3, 0.2f, true, ComplexityLevel.Intermediate);
        var intermediateOnlyAspects = new[] { AspectIds.Sharp, AspectIds.Fragile, AspectIds.Catalytic, AspectIds.Medicinal }.ToHashSet();
        var allAllowed = ComplexityFilter.GetAspects(ComplexityLevel.Intermediate).ToHashSet();

        bool foundIntermediateAspect = false;

        // Generate many puzzles — at least one should include an Intermediate aspect
        for (int seed = 100; seed < 200; seed++)
        {
            var puzzle = generator.Generate(seed, difficulty);
            foreach (var reagent in puzzle.Reagents)
            {
                foreach (var cellAspects in reagent.AspectsPerCell)
                {
                    foreach (var aspectId in cellAspects)
                    {
                        Assert.Contains(aspectId, allAllowed);
                        if (intermediateOnlyAspects.Contains(aspectId))
                            foundIntermediateAspect = true;
                    }
                }
            }
        }

        Assert.True(foundIntermediateAspect, "Expected at least one Intermediate-tier aspect across 100 generated puzzles");
    }
}
