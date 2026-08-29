namespace TriUgla;

public sealed class Polyline : INamable
{
    public Polyline(IEnumerable<Node> nodes, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        Nodes = nodes.Select(node => node ?? throw new ArgumentException(
            "A polyline cannot contain null nodes.", nameof(nodes))).ToList();
        Name = name;
    }

    public string? Name { get; set; }
    public List<Node> Nodes { get; set; }

    public double Length
        => Nodes.Zip(Nodes.Skip(1), (from, to) => from.Position.Distance(to.Position)).Sum();

    public Polyline Reverse()
    {
        Nodes.Reverse();
        return this;
    }

    public List<Edge> Edges(List<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        NodePathEdges.Append(Nodes, edges, "polyline");
        return edges;
    }
}

static class NodePathEdges
{
    public static void Append(IReadOnlyList<Node> nodes, List<Edge> edges, string pathType)
    {
        for (int index = 0; index < nodes.Count - 1; index++)
        {
            Edge edge = FindDirected(nodes[index], nodes[index + 1])
                ?? throw new InvalidOperationException(
                    $"Cannot resolve {pathType} segment {index}: no directed edge exists " +
                    $"from {nodes[index].Position} to {nodes[index + 1].Position}.");
            edges.Add(edge);
        }
    }

    static Edge? FindDirected(Node start, Node end)
    {
        if (start.Edge is null)
        {
            return null;
        }

        var pending = new Stack<Edge>();
        var visited = new HashSet<Edge>();
        pending.Push(start.Edge);
        while (pending.TryPop(out Edge? edge))
        {
            if (!visited.Add(edge) || !ReferenceEquals(edge.NodeStart, start))
            {
                continue;
            }

            if (ReferenceEquals(edge.NodeEnd, end))
            {
                return edge;
            }

            if (edge.Prev is not null && edge.Prev.Twin is Edge previousRotation)
            {
                pending.Push(previousRotation);
            }

            if (edge.Twin is Edge twin && twin.Next is Edge nextRotation)
            {
                pending.Push(nextRotation);
            }
        }

        return null;
    }
}
