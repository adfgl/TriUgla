namespace TriUgla;

public sealed class Node : MeshElement
{
    int _constraints = 0;
    NodeKind? _kindBeforeInsertion;

    public Vec2 Position;
    public NodeData Data;
    public NodeKind Kind { get; internal set; } = NodeKind.Normal;
    public Edge Edge = null!;

    public int ConstraintCount => _constraints;
    public bool Constrained => _constraints > 0;
    internal bool PromotedForInsertion => _kindBeforeInsertion is not null;

    internal void PromoteForInsertion()
    {
        if (Kind is NodeKind.Super or NodeKind.SteinerInsertion) return;
        _kindBeforeInsertion = Kind;
        Kind = NodeKind.SteinerInsertion;
    }

    internal void ReleaseInsertionRole()
    {
        if (Kind != NodeKind.SteinerInsertion || Constrained) return;
        Kind = _kindBeforeInsertion ?? NodeKind.SteinerRefinement;
        _kindBeforeInsertion = null;
    }

    public void Constrain() => _constraints++;

    internal void CopyConstraintState(Node source)
    {
        _constraints = source._constraints;
        _kindBeforeInsertion = source._kindBeforeInsertion;
    }

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
