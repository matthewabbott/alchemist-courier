using System.Linq;
using AlchemistCourier.Core.Puzzle;
using Xunit;

namespace AlchemistCourier.Core.Tests.Puzzle;

public class TutorialDataTests
{
    [Fact]
    public void GetLessons_Returns5Lessons()
    {
        var lessons = TutorialData.GetLessons();

        Assert.Equal(5, lessons.Count);
    }

    [Fact]
    public void GetLessons_LessonNumbersAreSequential()
    {
        var lessons = TutorialData.GetLessons();

        for (int i = 0; i < lessons.Count; i++)
        {
            Assert.Equal(i + 1, lessons[i].LessonNumber);
        }
    }

    [Fact]
    public void GetLessons_AllLessonsHaveNonEmptyConceptName()
    {
        var lessons = TutorialData.GetLessons();

        foreach (var lesson in lessons)
        {
            Assert.False(string.IsNullOrWhiteSpace(lesson.ConceptName),
                $"Lesson {lesson.LessonNumber} has empty ConceptName");
        }
    }

    [Fact]
    public void GetLessons_AllLessonsHaveNonEmptyHintText()
    {
        var lessons = TutorialData.GetLessons();

        foreach (var lesson in lessons)
        {
            Assert.False(string.IsNullOrWhiteSpace(lesson.HintText),
                $"Lesson {lesson.LessonNumber} has empty HintText");
        }
    }

    [Fact]
    public void GetLessons_AllLessonsHaveDistinctSeeds()
    {
        var lessons = TutorialData.GetLessons();
        var seeds = lessons.Select(l => l.FixedSeed).ToHashSet();

        Assert.Equal(lessons.Count, seeds.Count);
    }

    [Fact]
    public void GetLessons_AllLessonsAreFitAllSafely()
    {
        var lessons = TutorialData.GetLessons();

        foreach (var lesson in lessons)
        {
            Assert.Equal(PuzzleType.FitAllSafely, lesson.RequiredPuzzleType);
        }
    }

    [Fact]
    public void GetLessons_EarlyLessonsHaveNoAspects()
    {
        var lessons = TutorialData.GetLessons();

        // Lessons 1-3 are pure spatial — no aspects
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(0, lessons[i].DifficultyParams.MaxAspectCount);
        }
    }

    [Fact]
    public void GetLessons_Lesson4And5IntroduceAspects()
    {
        var lessons = TutorialData.GetLessons();

        Assert.True(lessons[3].DifficultyParams.MaxAspectCount >= 1,
            "Lesson 4 should introduce aspects");
        Assert.True(lessons[4].DifficultyParams.MaxAspectCount >= 1,
            "Lesson 5 should introduce aspects");
    }

    [Fact]
    public void GetLessons_NoSpecialBags()
    {
        var lessons = TutorialData.GetLessons();

        // First 5 tutorial lessons don't use special bags
        foreach (var lesson in lessons)
        {
            Assert.False(lesson.DifficultyParams.AllowSpecialBags,
                $"Lesson {lesson.LessonNumber} should not allow special bags");
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void GetLessons_FixedSeedProducesSolvablePuzzle(int lessonNumber)
    {
        var lessons = TutorialData.GetLessons();
        var lesson = lessons[lessonNumber - 1];

        var generator = new PuzzleGenerator();
        var state = generator.Generate(lesson.FixedSeed, lesson.DifficultyParams);

        Assert.NotNull(state);
        Assert.True(PuzzleSolver.IsSolvable(state),
            $"Lesson {lessonNumber} (seed={lesson.FixedSeed}) should produce a solvable puzzle");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void GetLessons_SameSeedProducesSamePuzzle(int lessonNumber)
    {
        var lessons = TutorialData.GetLessons();
        var lesson = lessons[lessonNumber - 1];

        var generator = new PuzzleGenerator();
        var state1 = generator.Generate(lesson.FixedSeed, lesson.DifficultyParams);
        var state2 = generator.Generate(lesson.FixedSeed, lesson.DifficultyParams);

        Assert.Equal(state1.Bags.Count, state2.Bags.Count);
        Assert.Equal(state1.Reagents.Count, state2.Reagents.Count);
        Assert.Equal(state1.Solution.Count, state2.Solution.Count);
    }

    [Fact]
    public void GetLessons_ComplexityIncreasesFromSpatialToChemistry()
    {
        var lessons = TutorialData.GetLessons();

        // Lessons 1-3 are pure spatial (MaxAspectCount=0)
        // Lessons 4-5 introduce chemistry (MaxAspectCount>=1)
        int lastSpatialLesson = lessons.FindIndex(l => l.DifficultyParams.MaxAspectCount > 0);
        Assert.True(lastSpatialLesson >= 3,
            "At least 3 pure spatial lessons should precede chemistry lessons");
    }
}
