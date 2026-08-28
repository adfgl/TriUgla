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
}