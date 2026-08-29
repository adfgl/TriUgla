namespace TriUgla.Tests;

public class MesherTests
{
    [Fact]
    public void InsertAndRemoveNode()
    {
        var mesh = new Mesh(CreateTriangle());
        var mesher = new Mesher(mesh);

        InsertNodeResult insertion = mesher.Insert(new Vec2(0.5, 0.5));

        Assert.Equal(InsertNodeStatus.InsertedIntoFace, insertion.Status);
        Node inserted = Assert.IsType<Node>(insertion.Node);
        Assert.Equal(4, mesh.Traversal.Nodes().Count());
        Assert.Equal(3, mesh.Traversal.Faces().Count());

        RemoveNodeResult removal = mesher.Remove(inserted);

        Assert.True(removal.Removed);
        Assert.True(inserted.Dead);
        Assert.Equal(3, mesh.Traversal.Nodes().Count());
        Assert.Single(mesh.Traversal.Faces());
        Assert.IsType<Face>(mesh.Find(new Vec2(0.5, 0.5)));
    }

    [Fact]
    public void RemoveRejectsNodeFromAnotherMesh()
    {
        var mesher = new Mesher(new Mesh(CreateTriangle()));
        var foreign = new Node { Position = new Vec2(0.5, 0.5) };

        RemoveNodeResult result = mesher.Remove(foreign);

        Assert.False(result.Removed);
        Assert.False(foreign.Dead);
    }

    [Fact]
    public void ExposesOwnedMesh()
    {
        var mesh = new Mesh(CreateTriangle());

        Assert.Same(mesh, new Mesher(mesh).Mesh);
    }

    static Face CreateTriangle()
    {
        var face = new Face();
        Linker.LinkTriangle(
            face,
            new Edge(), new Edge(), new Edge(),
            new Node { Position = new Vec2(0, 0) },
            new Node { Position = new Vec2(2, 0) },
            new Node { Position = new Vec2(0, 2) });
        return face;
    }
}
