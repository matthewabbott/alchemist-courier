using Xunit;
using AlchemistCourier.Core.Chemistry;

namespace AlchemistCourier.Core.Tests.Chemistry;

public class AspectSetTests
{
	[Fact]
	public void Empty_HasNoAspects()
	{
		var set = AspectSet.Empty;
		Assert.True(set.IsEmpty);
		Assert.Equal(0, set.Count);
	}

	[Fact]
	public void Constructor_WithAspects_HasCorrectCount()
	{
		var set = new AspectSet(Aspect.Hot(), Aspect.Cold());
		Assert.Equal(2, set.Count);
		Assert.False(set.IsEmpty);
	}

	[Fact]
	public void Has_ReturnsTrueForPresentAspect()
	{
		var set = new AspectSet(Aspect.Hot(), Aspect.Volatile());
		Assert.True(set.Has("hot"));
		Assert.True(set.Has("volatile"));
		Assert.False(set.Has("cold"));
	}

	[Fact]
	public void Get_ReturnsAspectById()
	{
		var set = new AspectSet(Aspect.Hot(3));
		var hot = set.Get("hot");
		Assert.NotNull(hot);
		Assert.Equal(3, hot!.Magnitude);
	}

	[Fact]
	public void Get_ReturnsNullForMissing()
	{
		var set = new AspectSet(Aspect.Hot());
		Assert.Null(set.Get("cold"));
	}

	[Fact]
	public void GetMagnitude_ReturnsZeroForMissing()
	{
		var set = new AspectSet(Aspect.Hot());
		Assert.Equal(0, set.GetMagnitude("cold"));
	}

	[Fact]
	public void TotalMagnitude_SumsAll()
	{
		var set = new AspectSet(Aspect.Hot(2), Aspect.Cold(3));
		Assert.Equal(5, set.TotalMagnitude());
	}

	[Fact]
	public void GetBySchool_FiltersCorrectly()
	{
		var set = new AspectSet(Aspect.Hot(), Aspect.Cold(), Aspect.Volatile());
		var physical = set.GetBySchool(SchoolType.Physical);
		Assert.Equal(3, physical.Count);

		var wuxing = set.GetBySchool(SchoolType.Wuxing);
		Assert.Empty(wuxing);
	}

	[Fact]
	public void Add_IncreasesCount()
	{
		var set = new AspectSet();
		set.Add(Aspect.Toxic());
		Assert.Equal(1, set.Count);
		Assert.True(set.Has("toxic"));
	}

	[Fact]
	public void Remove_RemovesById()
	{
		var set = new AspectSet(Aspect.Hot(), Aspect.Cold());
		bool removed = set.Remove("hot");
		Assert.True(removed);
		Assert.Equal(1, set.Count);
		Assert.False(set.Has("hot"));
	}

	[Fact]
	public void Remove_ReturnsFalseForMissing()
	{
		var set = new AspectSet(Aspect.Hot());
		Assert.False(set.Remove("cold"));
	}
}
