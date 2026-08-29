namespace TriUgla.Tests;

public class MeshElementTests
{
    [Fact]
    public void TryVisitReturnsFalseForRepeatedStamp()
    {
        var element = new MeshElement();
        var stamp = new Stamp(1);

        Assert.True(element.TryVisit(stamp));
        Assert.False(element.TryVisit(stamp));
    }

    [Fact]
    public void TryVisitAcceptsNewStamp()
    {
        var element = new MeshElement();

        Assert.True(element.TryVisit(new Stamp(1)));
        Assert.True(element.TryVisit(new Stamp(2)));
    }

    [Fact]
    public void TryVisitRejectsNone()
    {
        var element = new MeshElement();

        var error = Assert.Throws<ArgumentException>(() => element.TryVisit(Stamp.None));
        Assert.Equal("stamp", error.ParamName);
    }
}
