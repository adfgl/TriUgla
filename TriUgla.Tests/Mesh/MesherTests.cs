namespace TriUgla.Tests;

public class MesherTests
{
    [Fact]
    public void InsertAndRemoveNode()
    {
        var mesher = new Mesher(CreateTriangle());

        InsertNodeResult insertion = mesher.Insert(new Vec2(0.5, 0.5));

        Assert.Equal(InsertNodeStatus.InsertedIntoFace, insertion.Status);
        Node inserted = Assert.IsType<Node>(insertion.Node);
        Assert.Equal(4, mesher.Traversal.Nodes().Count());
        Assert.Equal(3, mesher.Traversal.Faces().Count());

        RemoveNodeResult removal = mesher.Remove(inserted);

        Assert.True(removal.Removed);
        Assert.True(inserted.Dead);
        Assert.Equal(3, mesher.Traversal.Nodes().Count());
        Assert.Single(mesher.Traversal.Faces());
        Assert.IsType<Face>(mesher.Find(new Vec2(0.5, 0.5)));
    }

    [Fact]
    public void RemoveRejectsNodeFromAnotherMesh()
    {
        var mesher = new Mesher(CreateTriangle());
        var foreign = new Node { Position = new Vec2(0.5, 0.5) };

        RemoveNodeResult result = mesher.Remove(foreign);

        Assert.False(result.Removed);
        Assert.False(foreign.Dead);
    }

    [Fact]
    public void ExposesRootFace()
    {
        Face root = CreateTriangle();

        Assert.Same(root, new Mesher(root).Root);
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
