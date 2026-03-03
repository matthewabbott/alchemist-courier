using Godot;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Inventory;

public static class GridCoordinates
{
    public static Vector2 GridToPixel(CoreVec gridPos)
        => new(gridPos.Col * GridConstants.CellSize, gridPos.Row * GridConstants.CellSize);

    public static CoreVec PixelToGrid(Vector2 pixel)
        => new((int)(pixel.X / GridConstants.CellSize), (int)(pixel.Y / GridConstants.CellSize));

    public static Vector2I ToGodotVec(CoreVec v) => new(v.Col, v.Row);

    public static CoreVec ToCoreVec(Vector2I v) => new(v.X, v.Y);
}
