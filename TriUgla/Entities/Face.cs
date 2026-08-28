using System.Security.Cryptography.X509Certificates;

namespace TriUgla;

public class Face : MeshElement
{
    public Edge Edge { get; set; } = null!;

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
    {
        foreach (Edge edge in Edges)
        {
            if (ReferenceEquals(node, edge.NodeStart))
            {
                return true;
            }
        }
        return false;
    }
}