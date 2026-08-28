namespace TriUgla.Tests;

public class Vec2Tests
{
    [Fact]
    public void Zero_HasZeroComponents()
        => Assert.Equal(new Vec2(0, 0), Vec2.Zero);

    [Fact]
    public void UnitX_PointsAlongXAxis()
        => Assert.Equal(new Vec2(1, 0), Vec2.UnitX);

    [Fact]
    public void UnitY_PointsAlongYAxis()
        => Assert.Equal(new Vec2(0, 1), Vec2.UnitY);

    [Fact]
    public void Make_SetsBothComponents()
        => Assert.Equal(new Vec2(3, 3), Vec2.Make(3));

    [Fact]
    public void LengthSquared_ReturnsSumOfSquaredComponents()
        => Assert.Equal(25, new Vec2(3, 4).LengthSquared);

    [Fact]
    public void Length_ReturnsVectorMagnitude()
        => Assert.Equal(5, new Vec2(3, 4).Length);

    [Fact]
    public void Normalize_ReturnsUnitVector()
    {
        Vec2 normalized = new Vec2(3, 4).Normalize();

        Assert.Equal(new Vec2(0.6, 0.8), normalized);
        Assert.Equal(1, normalized.Length, precision: 12);
    }

    [Fact]
    public void Normalize_ZeroVector_ReturnsZero()
        => Assert.Equal(Vec2.Zero, Vec2.Zero.Normalize());

    [Fact]
    public void Max_ReturnsLargestComponent()
        => Assert.Equal(5, new Vec2(1, 5).Max());

    [Fact]
    public void Min_ReturnsComponentWiseMinimum()
        => Assert.Equal(
            new Vec2(1, 2),
            Vec2.Min(new Vec2(1, 5), new Vec2(3, 2)));

    [Fact]
    public void Max_ReturnsComponentWiseMaximum()
        => Assert.Equal(
            new Vec2(3, 5),
            Vec2.Max(new Vec2(1, 5), new Vec2(3, 2)));

    [Fact]
    public void Dot_ReturnsScalarProduct()
        => Assert.Equal(16, new Vec2(1, 2).Dot(new Vec2(4, 6)));

    [Fact]
    public void Cross_ReturnsSignedArea()
        => Assert.Equal(-2, new Vec2(1, 2).Cross(new Vec2(4, 6)));

    [Fact]
    public void Cross_ParallelVectors_ReturnsZero()
        => Assert.Equal(0, new Vec2(1, 2).Cross(new Vec2(2, 4)));

    [Fact]
    public void Distance_ReturnsDistanceBetweenVectors()
        => Assert.Equal(5, new Vec2(1, 2).Distance(new Vec2(4, 6)));

    [Fact]
    public void DistanceSquared_ReturnsSquaredDistanceBetweenVectors()
        => Assert.Equal(25, new Vec2(1, 2).DistanceSquared(new Vec2(4, 6)));

    [Fact]
    public void Lerp_AtHalfway_ReturnsMidpoint()
        => Assert.Equal(
            new Vec2(2, 3.5),
            Vec2.Lerp(new Vec2(1, 5), new Vec2(3, 2), 0.5));

    [Fact]
    public void Lerp_AtZero_ReturnsStart()
    {
        var start = new Vec2(1, 5);

        Assert.Equal(start, Vec2.Lerp(start, new Vec2(3, 2), 0));
    }

    [Fact]
    public void Lerp_AtOne_ReturnsEnd()
    {
        var end = new Vec2(3, 2);

        Assert.Equal(end, Vec2.Lerp(new Vec2(1, 5), end, 1));
    }

    [Fact]
    public void Addition_AddsComponents()
        => Assert.Equal(new Vec2(5, 8), new Vec2(4, 6) + new Vec2(1, 2));

    [Fact]
    public void Subtraction_SubtractsComponents()
        => Assert.Equal(new Vec2(3, 4), new Vec2(4, 6) - new Vec2(1, 2));

    [Fact]
    public void UnaryNegation_NegatesComponents()
        => Assert.Equal(new Vec2(-4, -6), -new Vec2(4, 6));

    [Fact]
    public void Multiplication_VectorFirst_ScalesComponents()
        => Assert.Equal(new Vec2(8, 12), new Vec2(4, 6) * 2);

    [Fact]
    public void Multiplication_ScalarFirst_ScalesComponents()
        => Assert.Equal(new Vec2(8, 12), 2 * new Vec2(4, 6));

    [Fact]
    public void Division_DividesComponents()
        => Assert.Equal(new Vec2(2, 3), new Vec2(4, 6) / 2);

    [Fact]
    public void Equality_UsesBothComponents()
    {
        Assert.Equal(new Vec2(1, 2), new Vec2(1, 2));
        Assert.NotEqual(new Vec2(1, 2), new Vec2(1, 3));
    }

    [Fact]
    public void ToString_FormatsBothComponents()
        => Assert.Equal($"({1.5}, {-2d})", new Vec2(1.5, -2).ToString());
}
