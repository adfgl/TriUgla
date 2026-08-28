namespace TriUgla.Tests;

public class StampTests
{
    [Fact]
    public void ConstructorStoresValue()
    {
        var stamp = new Stamp(42);

        Assert.Equal(42u, stamp.Value);
    }

    [Fact]
    public void NoneHasValueZero()
    {
        Assert.Equal(new Stamp(0), Stamp.None);
    }

    [Fact]
    public void EqualityComparesValues()
    {
        Assert.Equal(new Stamp(42), new Stamp(42));
        Assert.NotEqual(new Stamp(42), new Stamp(43));
    }
}
