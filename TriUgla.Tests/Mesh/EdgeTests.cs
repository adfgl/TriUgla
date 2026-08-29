namespace TriUgla.Tests;

public class EdgeTests
{
    [Fact]
    public void ConstraintCountersTrackFeatureAndBoundaryIndependently()
    {
        Edge edge = LinkedEdge();

        edge.Constrain(EdgeConstraintKind.Feature);
        edge.Constrain(EdgeConstraintKind.Boundary);
        edge.Constrain(EdgeConstraintKind.Boundary);

        Assert.Equal(1, edge.FeatureConstraints);
        Assert.Equal(2, edge.BoundaryConstraints);
        Assert.Equal(3, edge.ConstraintCount);
        Assert.True(edge.HasFeature);
        Assert.True(edge.HasBoundary);
        Assert.True(edge.Constrained);
    }

    [Fact]
    public void ReleaseOnlyChangesRequestedConstraintKind()
    {
        Edge edge = LinkedEdge();
        edge.Constrain(EdgeConstraintKind.Feature);
        edge.Constrain(EdgeConstraintKind.Boundary);

        Assert.True(edge.Release(EdgeConstraintKind.Feature));
        Assert.False(edge.Release(EdgeConstraintKind.Feature));
        Assert.False(edge.HasFeature);
        Assert.True(edge.HasBoundary);
        Assert.Equal(1, edge.ConstraintCount);
    }

    [Fact]
    public void LengthGettersMeasureDistanceBetweenNodes()
    {
        Edge edge = new()
        {
            NodeStart = new Node { Position = new Vec2(1, 2) },
            Next = new Edge { NodeStart = new Node { Position = new Vec2(4, 6) } }
        };

        Assert.Equal(25, edge.LengthSquared);
        Assert.Equal(5, edge.Length);
    }

    [Fact]
    public void LengthGettersReturnZeroWhenNextEdgeIsMissing()
    {
        Edge edge = new() { NodeStart = new Node { Position = new Vec2(1, 2) } };

        Assert.Equal(0, edge.LengthSquared);
        Assert.Equal(0, edge.Length);
    }

    [Fact]
    public void LengthGettersReturnZeroWhenEndNodeIsMissing()
    {
        Edge edge = new()
        {
            NodeStart = new Node { Position = new Vec2(1, 2) },
            Next = new Edge()
        };

        Assert.Equal(0, edge.LengthSquared);
        Assert.Equal(0, edge.Length);
    }

    static Edge LinkedEdge()
        => new()
        {
            NodeStart = new Node(),
            Next = new Edge { NodeStart = new Node() }
        };
}
