namespace TriUgla.Tests;

public class BarycentricTests
{
    private static readonly Vec2 A = new(0, 0);
    private static readonly Vec2 B = new(2, 0);
    private static readonly Vec2 C = new(0, 2);

    [Fact]
    public void From_AtFirstVertex_ReturnsFirstWeight()
        => Assert.Equal(new Barycentric(1, 0, 0), Barycentric.From(A, A, B, C));

    [Fact]
    public void From_AtSecondVertex_ReturnsSecondWeight()
        => Assert.Equal(new Barycentric(0, 1, 0), Barycentric.From(B, A, B, C));

    [Fact]
    public void From_AtThirdVertex_ReturnsThirdWeight()
        => Assert.Equal(new Barycentric(0, 0, 1), Barycentric.From(C, A, B, C));

    [Fact]
    public void From_InsideTriangle_ReturnsWeightsForPoint()
        => Assert.Equal(
            new Barycentric(0.5, 0.25, 0.25),
            Barycentric.From(new Vec2(0.5, 0.5), A, B, C));

    [Fact]
    public void From_OutsideTriangle_ReturnsNegativeWeight()
    {
        Barycentric weights = Barycentric.From(new Vec2(2, 2), A, B, C);

        Assert.Equal(new Barycentric(-1, 1, 1), weights);
    }

    [Fact]
    public void From_ClockwiseTriangle_ReturnsSameWeights()
        => Assert.Equal(
            new Barycentric(0.5, 0.25, 0.25),
            Barycentric.From(new Vec2(0.5, 0.5), A, C, B));

    [Fact]
    public void From_DegenerateTriangle_FallsBackToFirstVertex()
        => Assert.Equal(
            new Barycentric(1, 0, 0),
            Barycentric.From(new Vec2(1, 0), A, new Vec2(1, 0), B));

    [Fact]
    public void From_WeightsSumToOne()
    {
        Barycentric weights = Barycentric.From(new Vec2(0.3, 0.7), A, B, C);

        Assert.Equal(1, weights.A + weights.B + weights.C, precision: 12);
    }

    [Fact]
    public void Interpolate_ReturnsWeightedValue()
    {
        var weights = new Barycentric(0.5, 0.25, 0.25);

        Assert.Equal(17.5, weights.Interpolate(10, 20, 30));
    }

    [Fact]
    public void Interpolate_WithSelector_ReturnsWeightedSelectedValue()
    {
        var weights = new Barycentric(0.5, 0.25, 0.25);
        var first = new Sample(10);
        var second = new Sample(20);
        var third = new Sample(30);

        double result = weights.Interpolate(first, second, third, sample => sample.Value);

        Assert.Equal(17.5, result);
    }

    private sealed record Sample(double Value);
}
