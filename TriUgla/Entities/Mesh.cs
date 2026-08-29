namespace TriUgla;

public sealed class Mesh
{
    readonly MeshLocator _locator;

    public Mesh(Face root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Root = root;
        var stamps = new StampSource();
        Traversal = new MeshTraversal(root, stamps);
        _locator = new MeshLocator(this, Traversal, stamps);
    }

    public Face Root { get; private set; }
    public MeshTraversal Traversal { get; }

    public LocateResult Locate(Vec2 point, Face? from = null)
        => _locator.Locate(point, from);

    public MeshElement? Find(Vec2 point, Face? from = null)
    {
        LocateResult result = Locate(point, from);
        return result.Node ?? (MeshElement?)result.Edge ?? result.Face;
    }

    internal IMeshLocator Locator => _locator;

    internal void TopologyChanged(IReadOnlyList<Face> affectedFaces)
    {
        if (Root.Dead)
        {
            Root = affectedFaces.FirstOrDefault(face => !face.Dead)
                ?? throw new InvalidOperationException(
                    "A topology change retired the mesh root without a replacement face.");
            Traversal.SetRoot(Root);
        }
        _locator.Reset();
    }
}
