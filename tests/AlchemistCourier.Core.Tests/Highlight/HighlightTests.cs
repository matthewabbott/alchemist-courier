using Xunit;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Highlight;

namespace AlchemistCourier.Core.Tests.Highlight;

public class CellEdgeTests
{
    [Fact]
    public void None_HasValueZero()
    {
        Assert.Equal(0, (int)CellEdge.None);
    }

    [Fact]
    public void All_CombinesAllEdges()
    {
        Assert.Equal(CellEdge.Top | CellEdge.Bottom | CellEdge.Left | CellEdge.Right, CellEdge.All);
    }

    [Fact]
    public void Flags_ComposeCorrectly()
    {
        var topLeft = CellEdge.Top | CellEdge.Left;
        Assert.True(topLeft.HasFlag(CellEdge.Top));
        Assert.True(topLeft.HasFlag(CellEdge.Left));
        Assert.False(topLeft.HasFlag(CellEdge.Bottom));
        Assert.False(topLeft.HasFlag(CellEdge.Right));
    }

    [Fact]
    public void IndividualFlags_AreDistinctBits()
    {
        Assert.Equal(1, (int)CellEdge.Top);
        Assert.Equal(2, (int)CellEdge.Bottom);
        Assert.Equal(4, (int)CellEdge.Left);
        Assert.Equal(8, (int)CellEdge.Right);
    }
}

public class CellHighlightTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var pos = new Vector2I(3, 5);
        var highlight = new CellHighlight(pos, CellEdge.Top | CellEdge.Left, HighlightLayer.Constraint, HighlightSeverity.Danger, "hot", "Too hot!");

        Assert.Equal(pos, highlight.Position);
        Assert.Equal(CellEdge.Top | CellEdge.Left, highlight.Edges);
        Assert.Equal(HighlightLayer.Constraint, highlight.Layer);
        Assert.Equal(HighlightSeverity.Danger, highlight.Severity);
        Assert.Equal("hot", highlight.AspectId);
        Assert.Equal("Too hot!", highlight.Reason);
    }

    [Fact]
    public void Constructor_OptionalFields_DefaultToNull()
    {
        var highlight = new CellHighlight(Vector2I.Zero, CellEdge.None, HighlightLayer.Legend, HighlightSeverity.Info);

        Assert.Null(highlight.AspectId);
        Assert.Null(highlight.Reason);
    }
}

public class HighlightSetTests
{
    [Fact]
    public void NewSet_IsEmpty()
    {
        var set = new HighlightSet();
        Assert.True(set.IsEmpty);
        Assert.Equal(0, set.Count);
    }

    [Fact]
    public void Add_IncreasesCount()
    {
        var set = new HighlightSet();
        set.Add(MakeHighlight(0, 0));
        Assert.Equal(1, set.Count);
        Assert.False(set.IsEmpty);
    }

    [Fact]
    public void AddRange_AddsMultiple()
    {
        var set = new HighlightSet();
        set.AddRange(new[] { MakeHighlight(0, 0), MakeHighlight(1, 1), MakeHighlight(2, 2) });
        Assert.Equal(3, set.Count);
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var set = new HighlightSet();
        set.Add(MakeHighlight(0, 0));
        set.Add(MakeHighlight(1, 1));
        set.Clear();
        Assert.True(set.IsEmpty);
        Assert.Equal(0, set.Count);
    }

    [Fact]
    public void GetAtPosition_ReturnsMatchingHighlights()
    {
        var set = new HighlightSet();
        var target = new Vector2I(2, 3);
        set.Add(MakeHighlight(0, 0));
        set.Add(new CellHighlight(target, CellEdge.Top, HighlightLayer.Constraint, HighlightSeverity.Danger));
        set.Add(new CellHighlight(target, CellEdge.Left, HighlightLayer.Interaction, HighlightSeverity.Warning));
        set.Add(MakeHighlight(5, 5));

        var results = set.GetAtPosition(target);
        Assert.Equal(2, results.Count);
        Assert.All(results, h => Assert.Equal(target, h.Position));
    }

    [Fact]
    public void GetAtPosition_NoMatch_ReturnsEmpty()
    {
        var set = new HighlightSet();
        set.Add(MakeHighlight(0, 0));
        var results = set.GetAtPosition(new Vector2I(9, 9));
        Assert.Empty(results);
    }

    [Fact]
    public void GetByLayer_FiltersCorrectly()
    {
        var set = new HighlightSet();
        set.Add(new CellHighlight(new Vector2I(0, 0), CellEdge.None, HighlightLayer.Constraint, HighlightSeverity.Danger));
        set.Add(new CellHighlight(new Vector2I(1, 0), CellEdge.None, HighlightLayer.Interaction, HighlightSeverity.Warning));
        set.Add(new CellHighlight(new Vector2I(2, 0), CellEdge.None, HighlightLayer.Constraint, HighlightSeverity.Info));
        set.Add(new CellHighlight(new Vector2I(3, 0), CellEdge.None, HighlightLayer.Legend, HighlightSeverity.Safe));

        var constraints = set.GetByLayer(HighlightLayer.Constraint);
        Assert.Equal(2, constraints.Count);
        Assert.All(constraints, h => Assert.Equal(HighlightLayer.Constraint, h.Layer));
    }

    [Fact]
    public void GetByLayer_NoMatch_ReturnsEmpty()
    {
        var set = new HighlightSet();
        set.Add(new CellHighlight(Vector2I.Zero, CellEdge.None, HighlightLayer.Constraint, HighlightSeverity.Info));
        var results = set.GetByLayer(HighlightLayer.GhostOverlay);
        Assert.Empty(results);
    }

    [Fact]
    public void GetReferencedAspects_ReturnsDistinctAspectIds()
    {
        var set = new HighlightSet();
        set.Add(new CellHighlight(new Vector2I(0, 0), CellEdge.None, HighlightLayer.Interaction, HighlightSeverity.Danger, "hot"));
        set.Add(new CellHighlight(new Vector2I(1, 0), CellEdge.None, HighlightLayer.Interaction, HighlightSeverity.Warning, "cold"));
        set.Add(new CellHighlight(new Vector2I(2, 0), CellEdge.None, HighlightLayer.Interaction, HighlightSeverity.Danger, "hot")); // duplicate

        var aspects = set.GetReferencedAspects();
        Assert.Equal(2, aspects.Count);
        Assert.Contains("hot", aspects);
        Assert.Contains("cold", aspects);
    }

    [Fact]
    public void GetReferencedAspects_SkipsNullAspectIds()
    {
        var set = new HighlightSet();
        set.Add(new CellHighlight(Vector2I.Zero, CellEdge.None, HighlightLayer.Constraint, HighlightSeverity.Info));
        set.Add(new CellHighlight(new Vector2I(1, 0), CellEdge.None, HighlightLayer.Interaction, HighlightSeverity.Danger, "toxic"));

        var aspects = set.GetReferencedAspects();
        Assert.Single(aspects);
        Assert.Equal("toxic", aspects[0]);
    }

    [Fact]
    public void GetReferencedAspects_EmptySet_ReturnsEmpty()
    {
        var set = new HighlightSet();
        Assert.Empty(set.GetReferencedAspects());
    }

    private static CellHighlight MakeHighlight(int col, int row)
    {
        return new CellHighlight(new Vector2I(col, row), CellEdge.None, HighlightLayer.Constraint, HighlightSeverity.Info);
    }
}
