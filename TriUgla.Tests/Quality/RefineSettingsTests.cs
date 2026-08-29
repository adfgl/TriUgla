namespace TriUgla.Tests;

public class RefineSettingsTests
{
    [Fact]
    public void DefaultHasExpectedBudgetsAndTolerance()
    {
        RefineSettings settings = RefineSettings.Default;

        Assert.Equal(1_000_000, settings.MaxSteiners);
        Assert.Equal(8, settings.FaceStagnationBudget);
        Assert.Equal(1e-4, settings.ImproveEps);
    }

    [Fact]
    public void SupportsValueEquality()
    {
        RefineSettings settings = new(10, 3, 1e-6);

        Assert.Equal(new RefineSettings(10, 3, 1e-6), settings);
    }
}
