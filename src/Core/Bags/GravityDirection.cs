using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Bags;

/// <summary>
/// Direction of gravity in a gravity bag. Default is Down.
/// </summary>
public enum GravityDirection
{
    Down,
    Up,
    Left,
    Right,
}

/// <summary>
/// Extension methods for GravityDirection.
/// </summary>
public static class GravityDirectionExtensions
{
    /// <summary>
    /// Convert a GravityDirection to a Vector2I offset.
    /// </summary>
    public static Vector2I ToVector(this GravityDirection direction) => direction switch
    {
        GravityDirection.Down => Vector2I.Down,
        GravityDirection.Up => Vector2I.Up,
        GravityDirection.Left => Vector2I.Left,
        GravityDirection.Right => Vector2I.Right,
        _ => Vector2I.Down,
    };

    /// <summary>
    /// Get the next gravity direction when rotating 90° clockwise.
    /// </summary>
    public static GravityDirection RotateCW(this GravityDirection direction) => direction switch
    {
        GravityDirection.Down => GravityDirection.Left,
        GravityDirection.Left => GravityDirection.Up,
        GravityDirection.Up => GravityDirection.Right,
        GravityDirection.Right => GravityDirection.Down,
        _ => GravityDirection.Down,
    };
}
