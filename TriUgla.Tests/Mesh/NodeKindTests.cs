namespace TriUgla.Tests;

public class NodeKindTests
{
    [Fact]
    public void ConstraintCountTracksAppliedAndReleasedEdgeConstraints()
    {
        var start = new Node();
        var end = new Node();
        var edge = new Edge { NodeStart = start };
        edge.Next = new Edge { NodeStart = end };

        edge.Constrain(EdgeConstraintKind.Feature);
        Assert.Equal(1, start.ConstraintCount);
        Assert.Equal(1, end.ConstraintCount);

        Assert.True(edge.Release(EdgeConstraintKind.Feature));
        Assert.Equal(0, start.ConstraintCount);
        Assert.Equal(0, end.ConstraintCount);
    }
}
