namespace TriUgla.Tests;

public class StampTests
{
    [Fact]
    public void ConstructorCreatesStampWithGivenValue()
    {
        var stamp = new Stamp(42);

        Assert.True(stamp.Equals(new Stamp(42)));
    }

    [Fact]
    public void ZeroHasValueZero()
    {
        Assert.True(Stamp.Zero.Equals(new Stamp(0)));
        Assert.False(Stamp.Zero.Equals(new Stamp(1)));
    }

    [Fact]
    public void TryNextAdvancesStamp()
    {
        var stamp = new Stamp(42);

        var advanced = stamp.TryNext(out var next);

        Assert.True(advanced);
        Assert.True(next.Equals(new Stamp(43)));
    }

    [Fact]
    public void TryNextAtMaximumReturnsFalseAndDefaultStamp()
    {
        var stamp = new Stamp(uint.MaxValue);

        var advanced = stamp.TryNext(out var next);

        Assert.False(advanced);
        Assert.True(next.Equals(default));
    }

    [Fact]
    public void EqualsComparesStampValues()
    {
        var stamp = new Stamp(42);

        Assert.True(stamp.Equals(new Stamp(42)));
        Assert.False(stamp.Equals(new Stamp(43)));
    }
}
