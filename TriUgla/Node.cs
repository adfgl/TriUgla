namespace TriUgla;

public sealed class Node : MeshElement, IConstrainable
{
    int _constraints = 0;

    public Vec2 Position;
    public Edge Edge = null!;

    public bool Constrained => _constraints > 0;

    public void Constrain() => _constraints++;

    public void Relax()
    {
        if (Constrained)
        {
            _constraints--;
        }
    }
    
    public IEnumerable<Edge> Edges
    {
        get
        {
            Edge first = Edge;
            Edge current = first;
            do
            {
                yield return current;
                current = current.Prev.Twin ?? throw new InvalidOperationException(
                   $"Cannot enumerate edges around node at {Position}: " +
                   "the previous edge has no twin. The node may be on a mesh boundary " +
                   "or its topology links may be incomplete.");
            } while (!ReferenceEquals(first, current));
        }
    }
}
