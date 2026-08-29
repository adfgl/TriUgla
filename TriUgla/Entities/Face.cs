using System.Security.Cryptography.X509Certificates;

namespace TriUgla;

public class Face : MeshElement
{
    public Edge Edge { get; set; } = null!;
    public FaceKind Kind { get; internal set; }

    public double SignedArea
    {
        get
        {
            if (Edge is null) return 0d;

            double twiceArea = 0d;
            foreach (Edge edge in Edges)
            {
                twiceArea += edge.NodeStart.Position.Cross(edge.NodeEnd.Position);
            }
            return twiceArea * 0.5d;
        }
    }

    public double Area => Math.Abs(SignedArea);

    public IEnumerable<Edge> Edges
    {
        get
        {
            var first = Edge;
            var current = first;
            do
            {
                yield return current;
                current = current.Next;
            }
            while (!ReferenceEquals(current, first));
        }
    }

    public bool Contains(Node node)
        => Edges.Any(e => ReferenceEquals(node, e.NodeStart));

    public bool Contains(Edge edge)
        => Edges.Any(e => ReferenceEquals(e, edge));
}
