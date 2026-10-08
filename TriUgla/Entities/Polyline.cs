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
            try
            {
                new ConstraintSpan(nodes[index], nodes[index + 1]).Edges(edges);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve {pathType} segment {index} from " +
                    $"{nodes[index].Position} to {nodes[index + 1].Position}.",
                    exception);
            }
        }
    }
}
