using System;

namespace AlchemistCourier.Core.Grid;

/// <summary>
/// A simple 2D integer vector for grid positions.
/// This is our own type (not Godot.Vector2I) so that Core logic has no engine dependencies.
/// </summary>
public readonly struct Vector2I : IEquatable<Vector2I>
{
    public readonly int Col;
    public readonly int Row;

    public Vector2I(int col, int row)
    {
        Col = col;
        Row = row;
    }

    public static Vector2I Zero => new(0, 0);
    public static Vector2I Up => new(0, -1);
    public static Vector2I Down => new(0, 1);
    public static Vector2I Left => new(-1, 0);
    public static Vector2I Right => new(1, 0);

    /// <summary>The four cardinal directions (up, down, left, right).</summary>
    public static readonly Vector2I[] CardinalDirections = { Up, Down, Left, Right };

    public static Vector2I operator +(Vector2I a, Vector2I b) => new(a.Col + b.Col, a.Row + b.Row);
    public static Vector2I operator -(Vector2I a, Vector2I b) => new(a.Col - b.Col, a.Row - b.Row);
    public static bool operator ==(Vector2I a, Vector2I b) => a.Col == b.Col && a.Row == b.Row;
    public static bool operator !=(Vector2I a, Vector2I b) => !(a == b);

    public bool Equals(Vector2I other) => Col == other.Col && Row == other.Row;
    public override bool Equals(object? obj) => obj is Vector2I other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Col, Row);
    public override string ToString() => $"({Col}, {Row})";
}
