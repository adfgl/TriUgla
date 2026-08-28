namespace TriUgla;

public readonly struct LocateResult
{
    public Node? Node { get; }
    public Edge? Edge { get; }
    public Face? Face { get; }

    public bool IsNode => Node is not null;
    public bool IsEdge => Edge is not null;
    public bool IsFace => Face is not null;
    public bool IsEmpty => Node is null && Edge is null && Face is null;

    LocateResult(Node? node, Edge? edge, Face? face)
    {
        Node = node;
        Edge = edge;
        Face = face;
    }

    public static LocateResult Empty => new(null, null, null);

    public static LocateResult From(Node node) => new(node, null, null);

    public static LocateResult From(Edge edge) => new(null, edge, null);

    public static LocateResult From(Face face) => new(null, null, face);
}
