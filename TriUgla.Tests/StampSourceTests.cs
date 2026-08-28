namespace TriUgla.Tests;

public class StampSourceTests
{
    [Fact]
    public void FirstStampIsOne()
    {
        var source = new StampSource();

        Assert.True(source.TryNext(out var stamp));
        Assert.Equal(new Stamp(1), stamp);
    }

    [Fact]
    public void TryNextAdvancesStamp()
    {
        var source = new StampSource(new Stamp(42));

        Assert.True(source.TryNext(out var stamp));
        Assert.Equal(new Stamp(43), stamp);
    }

    [Fact]
    public void TryNextAtMaximumReturnsFalseAndNone()
    {
        var source = new StampSource(new Stamp(uint.MaxValue));

        Assert.False(source.TryNext(out var stamp));
        Assert.Equal(Stamp.None, stamp);
    }
}
