namespace TriUgla;

public class Edge : IConstrainable
{
    int _constraints = 0;

    public Node NodeStart { get; set; } = null!;
    public Edge Next { get; set; } = null!;
    public Edge Prev { get; set; } = null!;
    public Face Face { get; set; } = null!;
    public Edge? Twin { get; set; }
    public Node NodeEnd => Next.NodeStart;

    public bool Constrained => _constraints > 0;
    public bool OrTwinConstrained => Constrained || (Twin != null && Twin.Constrained);

    public void Constrain()
    {
        _constraints++;
        NodeStart.Constrain();
        Next.NodeStart.Constrain();
    }

    public void Relax()
    {
        if (Constrained)
        {
            _constraints--;
            NodeStart.Relax();
            NodeEnd.Relax();
        }
    }

    public IEnumerable<Node> Nodes
    {
        get
        {
            yield return NodeStart;
            yield return NodeEnd;
        }
    }

    public bool Contains(Node node) =>
        ReferenceEquals(NodeStart, node) ||
        ReferenceEquals(NodeEnd, node);
}