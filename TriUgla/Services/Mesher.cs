namespace TriUgla;

public sealed class Mesher
{
    readonly Mesh _mesh;
    readonly MeshLocator _locator;
    readonly INodeInserter _nodeInserter;
    readonly INodeRemover _nodeRemover;

    public Mesher(Face root) : this(new Mesh(root))
    {
    }

    public Mesher(Mesh mesh)
    {
        _mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        Face root = mesh.Root;
        var stamps = new StampSource();
        Traversal = new MeshTraversal(root, stamps);
        _locator = new MeshLocator(mesh, Traversal, stamps);
        var splitter = new Splitter();
        _nodeInserter = new NodeInserter(new NodeFactory(), splitter, _locator);
        _nodeRemover = new NodeRemover();
    }

    public Mesh Mesh => _mesh;
    public Face Root => _mesh.Root;
    public MeshTraversal Traversal { get; }

    public LocateResult Locate(Vec2 point, Face? from = null)
        => _locator.Locate(point, from);

    public MeshElement? Find(Vec2 point, Face? from = null)
    {
        LocateResult result = Locate(point, from);
        return result.Node ?? (MeshElement?)result.Edge ?? result.Face;
    }

    public InsertNodeResult Insert(Vec2 position, Face? from = null)
    {
        InsertNodeResult result = _nodeInserter.Insert(position, from);
        TopologyChange? change = result.FaceSplit?.Change ?? result.EdgeSplit?.Change;
        if (change is not null) TopologyChanged(change.Value.AffectedFaces);
        return result;
    }

    public RemoveNodeResult Remove(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!Traversal.Nodes().Any(candidate => ReferenceEquals(candidate, node)))
        {
            return RemoveNodeResult.Failed(node);
        }

        RemoveNodeResult result = _nodeRemover.Remove(node);
        if (result.Removed) TopologyChanged(result.Change.AffectedFaces);
        return result;
    }

    void TopologyChanged(IReadOnlyList<Face> affectedFaces)
    {
        if (Traversal.Root.Dead)
        {
            Face replacement = affectedFaces.FirstOrDefault(face => !face.Dead)
                ?? throw new InvalidOperationException(
                    "A topology change retired the root without a replacement face.");
            _mesh.SetRoot(replacement);
            Traversal.SetRoot(replacement);
        }
        _locator.Reset();
    }
}
