using System;
using System.Collections.Generic;

namespace AlchemistCourier.Core;

/// <summary>
/// Deterministic random number generator backed by System.Random.
/// Same seed always produces the same sequence.
/// </summary>
public class SeededRandom
{
    private readonly Random _random;

    public int Seed { get; }

    public SeededRandom(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    public int Next() => _random.Next();

    public int Next(int max) => _random.Next(max);

    public int Next(int min, int max) => _random.Next(min, max);

    public float NextFloat() => (float)_random.NextDouble();

    public void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
