namespace TriUgla;

public sealed class ConstraintSpan : INamable
{
    const double MinimumDirectionDot = .99;
    const int MaximumTraversalSteps = 100_000;
    Node _from;
    Node _to;

    public ConstraintSpan(Node from, Node to, string? name = null)
    {
        _from = from ?? throw new ArgumentNullException(nameof(from));
        _to = to ?? throw new ArgumentNullException(nameof(to));
        Name = name;
    }

    public string? Name { get; set; }

    public Node From
    {
        get => _from;
        set => _from = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Node To
    {
        get => _to;
        set => _to = value ?? throw new ArgumentNullException(nameof(value));
    }

    public List<Edge> Edges(List<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        if (ReferenceEquals(From, To))
        {
            return edges;
        }

        Vec2 direction = Direction(From, To);
        if (direction == Vec2.Zero)
        {
            throw new InvalidOperationException(
                "A constraint span cannot connect distinct nodes at the same position.");
        }

        var visited = new HashSet<Node> { From };
        Node current = From;
        for (int step = 0; step < MaximumTraversalSteps; step++)
        {
            double currentDistance = current.Position.DistanceSquared(To.Position);
            Edge? next = OutgoingEdges(current)
                .Where(edge => !edge.Dead)
                .Select(edge => new
                {
                    Edge = edge,
                    Alignment = direction.Dot(Direction(edge)),
                    Distance = edge.NodeEnd.Position.DistanceSquared(To.Position)
                })
                .Where(candidate =>
                    ReferenceEquals(candidate.Edge.NodeEnd, To) ||
                    candidate.Alignment >= MinimumDirectionDot &&
                    candidate.Distance < currentDistance)
                .OrderByDescending(candidate => ReferenceEquals(candidate.Edge.NodeEnd, To))
                .ThenByDescending(candidate => candidate.Alignment)
                .ThenBy(candidate => candidate.Distance)
                .Select(candidate => candidate.Edge)
                .FirstOrDefault();

            if (next is null)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve constraint span from {From.Position} to {To.Position}: " +
                    $"no aligned outgoing edge continues from {current.Position}.");
            }

            edges.Add(next);
            current = next.NodeEnd;
            if (ReferenceEquals(current, To))
            {
                return edges;
            }

            if (!visited.Add(current))
            {
                throw new InvalidOperationException(
                    "Constraint span traversal encountered a cycle before reaching its destination.");
            }
        }

        throw new InvalidOperationException(
            $"Constraint span traversal exceeded {MaximumTraversalSteps} steps.");
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

    static IEnumerable<Edge> OutgoingEdges(Node node)
    {
        if (node.Edge is null)
        {
            yield break;
        }

        var pending = new Stack<Edge>();
        var visited = new HashSet<Edge>();
        pending.Push(node.Edge);
        while (pending.TryPop(out Edge? edge))
        {
            if (!visited.Add(edge) || !ReferenceEquals(edge.NodeStart, node))
            {
                continue;
            }

            yield return edge;

            if (edge.Prev is not null && edge.Prev.Twin is Edge previousRotation)
            {
                pending.Push(previousRotation);
            }

            if (edge.Twin is Edge twin && twin.Next is Edge nextRotation)
            {
                pending.Push(nextRotation);
            }
        }
    }
}
