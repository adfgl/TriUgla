namespace TriUgla.Tests;

public class EdgeTests
{
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
}
