namespace TriUgla;

public sealed class LegalizationQueue
{
    readonly Queue<Edge> _edges = new();

    public int Count => _edges.Count;

    public void Add(Edge edge) => _edges.Enqueue(edge);

    public Edge Take() => _edges.Dequeue();
}
