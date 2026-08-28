namespace TriUgla.Tests;

public class MeshLocatorTests
{
    [Fact]
    public void Locate_PointInsideFace_ReturnsFace()
    {
        (Mesh mesh, Face first, _) = CreateMesh();
        var locator = new MeshLocator(mesh);

        LocateResult result = locator.Locate(new Vec2(0.25, 0.25));

        Assert.True(result.IsFace);
        Assert.Same(first, result.Face);
    }

    [Fact]
    public void Locate_PointAcrossTwin_WalksToNeighbour()
    {
        (Mesh mesh, _, Face second) = CreateMesh();
        var locator = new MeshLocator(mesh);

        LocateResult result = locator.Locate(new Vec2(1.75, 1.75));

        Assert.True(result.IsFace);
        Assert.Same(second, result.Face);
    }

    [Fact]
    public void Locate_PointOnVertex_ReturnsNode()
    {
        (Mesh mesh, Face first, _) = CreateMesh();
        var locator = new MeshLocator(mesh);

        LocateResult result = locator.Locate(first.Edge.NodeStart.Position);

        Assert.True(result.IsNode);
        Assert.Same(first.Edge.NodeStart, result.Node);
    }

    [Fact]
    public void Locate_PointOnSharedEdge_ReturnsEdge()
    {
        (Mesh mesh, Face first, _) = CreateMesh();
        var locator = new MeshLocator(mesh);

        LocateResult result = locator.Locate(new Vec2(1, 1));

        Assert.True(result.IsEdge);
        Assert.Same(first.Edge.Next, result.Edge);
    }

    [Fact]
    public void Locate_PointOutsideMesh_ReturnsEmpty()
    {
        (Mesh mesh, _, _) = CreateMesh();
        var locator = new MeshLocator(mesh);

        LocateResult result = locator.Locate(new Vec2(3, 3));

        Assert.True(result.IsEmpty);
    }

    static (Mesh Mesh, Face First, Face Second) CreateMesh()
    {
        var a = new Node { Position = new Vec2(0, 0) };
        var b = new Node { Position = new Vec2(2, 0) };
        var c = new Node { Position = new Vec2(0, 2) };
        var d = new Node { Position = new Vec2(2, 2) };

        var ab = new Edge();
        var bc = new Edge();
        var ca = new Edge();
        var cb = new Edge();
        var bd = new Edge();
        var dc = new Edge();
        var first = new Face();
        var second = new Face();

        Linker.LinkTriangle(first, ab, bc, ca, a, b, c);
        Linker.LinkTriangle(second, cb, bd, dc, c, b, d);
        Linker.LinkTwins(bc, cb);

        return (new Mesh(first), first, second);
    }
}
