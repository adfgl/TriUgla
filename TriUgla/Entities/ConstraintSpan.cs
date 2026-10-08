namespace TriUgla;

public sealed class ConstraintSpan : INamable
{
    const double MinimumDirectionDot = .99;
    Node _from;
    Node _to;

    public ConstraintSpan(Node from, Node to, string? name = null)
    {
        _from = from ?? throw new ArgumentNullException(nameof(from));
        _to = to ?? throw new ArgumentNullException(nameof(to));
        if (ReferenceEquals(_from, _to))
        {
            throw new ArgumentException("A constraint span must connect two different nodes.", nameof(to));
        }
        Name = name;
    }

    public string? Name { get; set; }

    public Node From
    {
        get => _from;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(value, To))
            {
                throw new ArgumentException("A constraint span must connect two different nodes.", nameof(value));
            }
            _from = value;
        }
    }

    public Node To
    {
        get => _to;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(value, From))
            {
                throw new ArgumentException("A constraint span must connect two different nodes.", nameof(value));
            }
            _to = value;
        }
    }

    public List<Edge> Edges(List<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        Vec2 direction = Direction(From, To);
        if (direction == Vec2.Zero)
        {
            throw new InvalidOperationException(
                "A constraint span cannot connect distinct nodes at the same position.");
        }

        Node current = From;
        while (!ReferenceEquals(current, To))
        {
            Edge? next = FindAlong(current, direction);
            if (next is null)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve constraint span from {From.Position} to {To.Position}: " +
                    $"no aligned outgoing edge continues from {current.Position}.");
            }

            edges.Add(next);
            current = next.NodeEnd;
        }
        return edges;
    }

    public static bool NearlyColliniear(Vec2 directionToMatch, Edge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        Vec2 direction = Direction(edge);
        return direction != Vec2.Zero && directionToMatch.Dot(direction) >= MinimumDirectionDot;
    }

    public static Vec2 Direction(Edge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        return Direction(edge.NodeStart, edge.NodeEnd);
    }

    public static Vec2 Direction(Node from, Node to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        return (to.Position - from.Position).Normalize();
    }

    static Edge? FindAlong(Node node, Vec2 direction)
    {
        Edge? first = node.Edge;
        Edge? edge = first;
        Edge? best = null;
        double bestProjection = MinimumDirectionDot;

        while (edge is not null)
        {
            if (!edge.Dead)
            {
                Vec2 candidateDirection = Direction(edge);
                double projection = candidateDirection == Vec2.Zero
                    ? double.NegativeInfinity
                    : direction.Dot(candidateDirection);
                if (projection >= bestProjection)
                {
                    best = edge;
                    bestProjection = projection;
                }
            }

            edge = edge.Prev?.Twin;
            if (ReferenceEquals(edge, first)) break;
        }

        return best;
    }
}
