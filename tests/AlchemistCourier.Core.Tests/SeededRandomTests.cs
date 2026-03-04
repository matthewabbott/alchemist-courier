using System.Collections.Generic;
using System.Linq;
using Xunit;
using AlchemistCourier.Core;

namespace AlchemistCourier.Core.Tests;

public class SeededRandomTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var rng1 = new SeededRandom(42);
        var rng2 = new SeededRandom(42);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(rng1.Next(), rng2.Next());
        }
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var rng1 = new SeededRandom(42);
        var rng2 = new SeededRandom(99);

        var seq1 = Enumerable.Range(0, 10).Select(_ => rng1.Next()).ToList();
        var seq2 = Enumerable.Range(0, 10).Select(_ => rng2.Next()).ToList();

        Assert.NotEqual(seq1, seq2);
    }

    [Fact]
    public void Seed_ReturnsConstructorSeed()
    {
        var rng = new SeededRandom(12345);
        Assert.Equal(12345, rng.Seed);
    }

    [Fact]
    public void NextMax_StaysInRange()
    {
        var rng = new SeededRandom(42);

        for (int i = 0; i < 200; i++)
        {
            int value = rng.Next(10);
            Assert.InRange(value, 0, 9);
        }
    }

    [Fact]
    public void NextMinMax_StaysInRange()
    {
        var rng = new SeededRandom(42);

        for (int i = 0; i < 200; i++)
        {
            int value = rng.Next(5, 15);
            Assert.InRange(value, 5, 14);
        }
    }

    [Fact]
    public void NextFloat_StaysInRange()
    {
        var rng = new SeededRandom(42);

        for (int i = 0; i < 200; i++)
        {
            float value = rng.NextFloat();
            Assert.InRange(value, 0.0f, 1.0f);
            Assert.True(value < 1.0f);
        }
    }

    [Fact]
    public void Shuffle_IsDeterministic()
    {
        var list1 = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var list2 = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        new SeededRandom(42).Shuffle(list1);
        new SeededRandom(42).Shuffle(list2);

        Assert.Equal(list1, list2);
    }

    [Fact]
    public void Shuffle_PreservesElements()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        new SeededRandom(42).Shuffle(list);

        Assert.Equal(5, list.Count);
        Assert.Equal(new HashSet<int> { 1, 2, 3, 4, 5 }, new HashSet<int>(list));
    }

    [Fact]
    public void Shuffle_EmptyList_DoesNothing()
    {
        var list = new List<int>();
        new SeededRandom(42).Shuffle(list);
        Assert.Empty(list);
    }

    [Fact]
    public void Shuffle_SingleElement_DoesNothing()
    {
        var list = new List<string> { "only" };
        new SeededRandom(42).Shuffle(list);
        Assert.Single(list);
        Assert.Equal("only", list[0]);
    }
}
