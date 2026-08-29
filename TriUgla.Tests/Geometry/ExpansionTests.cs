using TriUgla;

namespace TriUgla.Tests;

public class ExpansionTests
{
    [Fact]
    public void TwoSum_SeparatesRoundedSumFromLostLowPart()
    {
        Expansion.TwoSum(1e16, 1d, out double high, out double low);

        Assert.Equal(1e16, high);
        Assert.Equal(1d, low);
    }

    [Fact]
    public void Split_ReconstructsOriginalNumber()
    {
        const double value = 123456789.12345679;

        Expansion.Split(value, out double high, out double low);

        Assert.Equal(value, high + low);
        Assert.NotEqual(0d, low);
    }

    [Fact]
    public void TwoProd_ReturnsRoundedProductAndResidual()
    {
        const double left = 134217727d;
        const double right = 134217729d;

        Expansion.TwoProd(left, right, out double high, out double low);

        Assert.Equal(left * right, high);
        Assert.Equal(-1d, low);
    }

    [Fact]
    public void Add_PreservesValueLostByOrdinaryFloatingPointAddition()
    {
        var expansion = new List<double> { 1e16 };

        Expansion.Add(expansion, 1d);
        Expansion.Add(expansion, -1e16);
        Expansion.Compress(expansion);

        Assert.Equal([1d], expansion);
        Assert.Equal(1, Expansion.Sign(expansion));
    }

    [Fact]
    public void Add_WithSameExpansion_IsAliasSafe()
    {
        var expansion = new List<double> { 1d, 1e16 };

        Expansion.Add(expansion, expansion);

        Assert.Equal(2e16, Expansion.Approximate(expansion));
        Assert.Contains(2d, expansion);
    }

    [Fact]
    public void Mul_ByScalar_PreservesLowComponents()
    {
        var expansion = new List<double> { 1d, 1e16 };

        Expansion.Mul(expansion, 3d);

        Assert.Equal([-1d, 30000000000000004d], expansion);
        Assert.Equal(30000000000000004d, Expansion.Approximate(expansion));
    }

    [Fact]
    public void Mul_Expansions_IsAliasSafeAndHandlesZero()
    {
        var expansion = new List<double> { 2d };
        Expansion.Mul(expansion, expansion);
        Assert.Equal(4d, Expansion.Approximate(expansion));

        Expansion.Mul(expansion, []);
        Assert.Empty(expansion);
    }

    [Fact]
    public void NegateAndSign_ReflectMostSignificantComponent()
    {
        var expansion = new List<double> { 1e-30, 2d };

        Expansion.Negate(expansion);

        Assert.Equal(-1, Expansion.Sign(expansion));
        Assert.Equal(-2d, Expansion.Approximate(expansion));
        Assert.Equal(0, Expansion.Sign([]));
    }

    [Fact]
    public void Compress_RemovesZeroComponents()
    {
        var expansion = new List<double> { 0d, 1d, 0d, 2d };

        int count = Expansion.Compress(expansion);

        Assert.Equal(count, expansion.Count);
        Assert.DoesNotContain(0d, expansion);
        Assert.Equal(3d, Expansion.Approximate(expansion));
    }
}
