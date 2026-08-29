namespace TriUgla;

public class Edge : MeshElement
{
    int _featureConstraints;
    int _boundaryConstraints;

    public Node NodeStart { get; set; } = null!;
    public Edge Next { get; set; } = null!;
    public Edge Prev { get; set; } = null!;
    public Face Face { get; set; } = null!;
    public Edge? Twin { get; set; }
    public Node NodeEnd => Next.NodeStart;

    public double LengthSquared
    {
        get
        {
            Node? end = Next?.NodeStart;
            return NodeStart is null || end is null
                ? 0d
                : NodeStart.Position.DistanceSquared(end.Position);
        }
    }

    public double Length => Math.Sqrt(LengthSquared);

    public int FeatureConstraints => _featureConstraints;
    public int BoundaryConstraints => _boundaryConstraints;
    public int ConstraintCount => _featureConstraints + _boundaryConstraints;
    public bool HasFeature => _featureConstraints > 0;
    public bool HasBoundary => _boundaryConstraints > 0;
    public bool Constrained => ConstraintCount > 0;
    public bool OrTwinConstrained => Constrained || (Twin != null && Twin.Constrained);

    public void Constrain(EdgeConstraintKind kind)
    {
        switch (kind)
        {
            case EdgeConstraintKind.Feature:
                _featureConstraints++;
                break;
            case EdgeConstraintKind.Boundary:
                _boundaryConstraints++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
        NodeStart.Constrain();
        NodeEnd.Constrain();
    }

    public bool Release(EdgeConstraintKind kind)
    {
        bool released = kind switch
        {
            EdgeConstraintKind.Feature => Release(ref _featureConstraints),
            EdgeConstraintKind.Boundary => Release(ref _boundaryConstraints),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
        if (!released) return false;

        NodeStart.Relax();
        NodeEnd.Relax();
        return true;
    }

    static bool Release(ref int count)
    {
        if (count == 0) return false;
        count--;
        return true;
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

    public static Edge? FindDirected(Node start, Node end)
    {
        foreach (Edge edge in start.Edges)
        {
            if (ReferenceEquals(end, edge.NodeEnd))
            {
                return edge;
            }
        }
        return null;
    }

    public static Edge? Find(Node start, Node end)
        => FindDirected(start, end) ?? FindDirected(end, start);
}
