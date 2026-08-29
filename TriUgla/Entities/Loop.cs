namespace TriUgla;

public sealed class Loop : INamable
{
    public Loop(IEnumerable<Node> nodes, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        Nodes = nodes.Select(node => node ?? throw new ArgumentException(
            "A loop cannot contain null nodes.", nameof(nodes))).ToList();
        Name = name;
    }

    public string? Name { get; set; }
    public List<Node> Nodes { get; set; }

    public Loop ForceClockwise()
    {
        Close();
        if (Nodes.Count >= 4 && SignedArea() > 0d)
        {
            ReversePreserveClosure();
        }

        return this;
    }

    public Loop ForceCounterClockwise()
    {
        Close();
        if (Nodes.Count >= 4 && SignedArea() < 0d)
        {
            ReversePreserveClosure();
        }

        return this;
    }

    public bool Closed()
        => Nodes.Count > 0 && ReferenceEquals(Nodes[0], Nodes[^1]);

    public Loop Close()
    {
        if (Nodes.Count > 0 && !Closed())
        {
            Nodes.Add(Nodes[0]);
        }

        return this;
    }

    public List<Edge> Edges(List<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        Close();
        NodePathEdges.Append(Nodes, edges, "loop");
        return edges;
    }

    public double SignedArea()
    {
        Close();
        if (Nodes.Count < 4)
        {
            return 0d;
        }

        double sum = 0d;
        for (int index = 0; index < Nodes.Count - 1; index++)
        {
            Vec2 current = Nodes[index].Position;
            Vec2 next = Nodes[index + 1].Position;
            sum += current.Cross(next);
        }

        return .5d * sum;
    }

    void ReversePreserveClosure()
    {
        Node first = Nodes[0];
        var reversed = new List<Node>(Nodes.Count) { first };
        for (int index = Nodes.Count - 2; index >= 1; index--)
        {
            reversed.Add(Nodes[index]);
        }

        reversed.Add(first);
        Nodes.Clear();
        Nodes.AddRange(reversed);
    }
}
