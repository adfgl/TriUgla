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

    public Face Root { get; }
    public MeshTraversal Traversal { get; }

    public LocateResult Locate(Vec2 point, Face? from = null)
        => _locator.Locate(point, from);

    public MeshElement? Find(Vec2 point, Face? from = null)
    {
        LocateResult result = Locate(point, from);
        return result.Node ?? (MeshElement?)result.Edge ?? result.Face;
    }
}
