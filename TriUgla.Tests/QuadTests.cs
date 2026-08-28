namespace TriUgla.Tests;

public class QuadTests
{
    [Fact]
    public void FromThrowsWithoutTwin()
    {
        var edge = new Edge();

        var error = Assert.Throws<InvalidOperationException>(() => Quad.From(edge));
        Assert.Contains("without a twin", error.Message);
    }

    [Fact]
    public void FromCreatesQuad()
    {
        var a = new Node();
        var b = new Node();
        var c = new Node();
        var d = new Node();
        var edge = CreateEdge(a, b, c, d);

        var quad = Quad.From(edge);

        Assert.Same(a, quad.A);
        Assert.Same(b, quad.B);
        Assert.Same(c, quad.C);
        Assert.Same(d, quad.D);
    }

    static Edge CreateEdge(Node a, Node b, Node c, Node d)
    {
        var edge = new Edge
        {
            NodeStart = a,
            Next = new Edge { NodeStart = c },
            Prev = new Edge { NodeStart = d }
        };
        edge.Twin = new Edge
        {
            Prev = new Edge { NodeStart = b }
        };
        return edge;
    }
}
