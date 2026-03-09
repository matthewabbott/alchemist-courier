using AlchemistCourier.Core.Visual;
using Xunit;

namespace AlchemistCourier.Core.Tests.Visual;

public class SpriteSheetSpecTests
{
    [Fact]
    public void DefaultReagentSpec_Has7Rows()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        Assert.Equal(7, spec.TotalRows);
    }

    [Fact]
    public void DefaultReagentSpec_Has4MaxFrames()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        Assert.Equal(4, spec.MaxFrames);
    }

    [Fact]
    public void DefaultReagentSpec_MapsAllVisualStates()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        Assert.True(spec.StateRowMap.ContainsKey(VisualState.Normal));
        Assert.True(spec.StateRowMap.ContainsKey(VisualState.Heated));
        Assert.True(spec.StateRowMap.ContainsKey(VisualState.Cooled));
        Assert.True(spec.StateRowMap.ContainsKey(VisualState.ThermalStress));
        Assert.True(spec.StateRowMap.ContainsKey(VisualState.Crushed));
        Assert.True(spec.StateRowMap.ContainsKey(VisualState.Breaking));
        Assert.True(spec.StateRowMap.ContainsKey(VisualState.Exploding));
    }

    [Fact]
    public void DefaultReagentSpec_StaticStatesHave1Frame()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        Assert.Equal(1, spec.FrameCounts[VisualState.Normal]);
        Assert.Equal(1, spec.FrameCounts[VisualState.Heated]);
        Assert.Equal(1, spec.FrameCounts[VisualState.Cooled]);
        Assert.Equal(1, spec.FrameCounts[VisualState.ThermalStress]);
        Assert.Equal(1, spec.FrameCounts[VisualState.Crushed]);
    }

    [Fact]
    public void DefaultReagentSpec_AnimatedStatesHave4Frames()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        Assert.Equal(4, spec.FrameCounts[VisualState.Breaking]);
        Assert.Equal(4, spec.FrameCounts[VisualState.Exploding]);
    }

    [Fact]
    public void GetRegion_NormalState_ReturnsFirstRow()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        var region = spec.GetRegion(VisualState.Normal);
        Assert.NotNull(region);
        Assert.Equal((0, 0, 32, 32), region.Value);
    }

    [Fact]
    public void GetRegion_HeatedState_ReturnsSecondRow()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(64, 32);
        var region = spec.GetRegion(VisualState.Heated);
        Assert.NotNull(region);
        Assert.Equal((0, 32, 64, 32), region.Value);
    }

    [Fact]
    public void GetRegion_BreakingState_Frame2()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        var region = spec.GetRegion(VisualState.Breaking, 2);
        Assert.NotNull(region);
        // Row 5 (Breaking), frame 2: x = 2*32, y = 5*32
        Assert.Equal((64, 160, 32, 32), region.Value);
    }

    [Fact]
    public void GetRegion_InvalidState_ReturnsNull()
    {
        var spec = new SpriteSheetSpec(
            new() { [VisualState.Normal] = 0 },
            new() { [VisualState.Normal] = 1 },
            32, 32);

        Assert.Null(spec.GetRegion(VisualState.Heated));
    }

    [Fact]
    public void GetRegion_FrameOutOfRange_ReturnsNull()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        Assert.Null(spec.GetRegion(VisualState.Normal, 1)); // Only 1 frame
        Assert.Null(spec.GetRegion(VisualState.Breaking, 4)); // Only 4 frames (0-3)
        Assert.Null(spec.GetRegion(VisualState.Normal, -1));
    }

    [Fact]
    public void SheetWidth_MonominoDefault_Is128()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        // 4 max frames * 32 = 128
        Assert.Equal(128, spec.SheetWidth);
    }

    [Fact]
    public void SheetHeight_MonominoDefault_Is224()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        // 7 rows * 32 = 224
        Assert.Equal(224, spec.SheetHeight);
    }

    [Fact]
    public void SheetDimensions_DominoSpec()
    {
        // Domino: 64×32 per frame, 7 rows, 4 max frames
        var spec = SpriteSheetSpec.DefaultReagentSpec(64, 32);
        Assert.Equal(256, spec.SheetWidth);  // 4 * 64
        Assert.Equal(224, spec.SheetHeight); // 7 * 32
    }

    [Fact]
    public void CustomSpec_TotalRowsAndMaxFrames()
    {
        var spec = new SpriteSheetSpec(
            new()
            {
                [VisualState.Normal] = 0,
                [VisualState.Heated] = 1,
                [VisualState.Breaking] = 5,
            },
            new()
            {
                [VisualState.Normal] = 1,
                [VisualState.Heated] = 1,
                [VisualState.Breaking] = 3,
            },
            48, 48);

        Assert.Equal(6, spec.TotalRows);  // row 5 + 1
        Assert.Equal(3, spec.MaxFrames);
        Assert.Equal(48, spec.CellWidth);
        Assert.Equal(48, spec.CellHeight);
    }

    [Fact]
    public void GetRegion_ExplodingState_AllFramesValid()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        for (int f = 0; f < 4; f++)
        {
            var region = spec.GetRegion(VisualState.Exploding, f);
            Assert.NotNull(region);
            Assert.Equal(f * 32, region.Value.X);
            Assert.Equal(6 * 32, region.Value.Y); // Row 6
        }
    }

    [Fact]
    public void DefaultReagentSpec_RowAssignments()
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        Assert.Equal(0, spec.StateRowMap[VisualState.Normal]);
        Assert.Equal(1, spec.StateRowMap[VisualState.Heated]);
        Assert.Equal(2, spec.StateRowMap[VisualState.Cooled]);
        Assert.Equal(3, spec.StateRowMap[VisualState.ThermalStress]);
        Assert.Equal(4, spec.StateRowMap[VisualState.Crushed]);
        Assert.Equal(5, spec.StateRowMap[VisualState.Breaking]);
        Assert.Equal(6, spec.StateRowMap[VisualState.Exploding]);
    }
}
