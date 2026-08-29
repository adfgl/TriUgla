namespace TriUgla;

public sealed class Mesher
{
    readonly Mesh _mesh;
    readonly INodeInserter _nodeInserter;
    readonly INodeRemover _nodeRemover;

    public Mesher(Mesh mesh)
    {
        _mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        var splitter = new Splitter();
        _nodeInserter = new NodeInserter(new NodeFactory(), splitter, mesh.Locator);
        _nodeRemover = new NodeRemover();
    }

    public Mesh Mesh => _mesh;

    public InsertNodeResult Insert(Vec2 position, Face? from = null)
    {
        InsertNodeResult result = _nodeInserter.Insert(position, from);
        TopologyChange? change = result.FaceSplit?.Change ?? result.EdgeSplit?.Change;
        if (change is not null) _mesh.TopologyChanged(change.Value.AffectedFaces);
        return result;
    }

    public RemoveNodeResult Remove(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!_mesh.Traversal.Nodes().Any(candidate => ReferenceEquals(candidate, node)))
        {
            return RemoveNodeResult.Failed(node);
        }

        RemoveNodeResult result = _nodeRemover.Remove(node);
        if (result.Removed) _mesh.TopologyChanged(result.Change.AffectedFaces);
        return result;
    }
}
