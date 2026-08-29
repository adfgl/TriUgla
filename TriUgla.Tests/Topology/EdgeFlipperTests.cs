namespace TriUgla.Tests;

public class EdgeFlipperTests
{
    [Fact]
    public void FlipReplacesDiagonalAndPreservesOuterTwins()
    {
        Fixture f = CreateFixture();
        var flipper = new EdgeFlipper(new StubGeometry());

        EdgeFlipResult result = flipper.Flip(f.AB);
        Edge flipped = result.FlippedEdge;

        Assert.Equal(new[] { f.Top, f.Bottom }, result.Change.AffectedFaces);
        Assert.Same(f.AB, flipped);
        Assert.Same(f.C, flipped.NodeStart);
        Assert.Same(f.D, flipped.NodeEnd);
        Assert.Same(f.BA, flipped.Twin);
        Assert.Same(flipped, f.BA.Twin);
        Assert.Same(f.D, f.BA.NodeStart);
        Assert.Same(f.C, f.BA.NodeEnd);

        AssertTriangleRing(f.Top, f.AD);
        AssertTriangleRing(f.Bottom, f.DB);

        for (int i = 0; i < f.Outer.Length; i++)
        {
            Assert.Same(f.OuterTwins[i], f.Outer[i].Twin);
            Assert.Same(f.Outer[i], f.OuterTwins[i].Twin);
        }

        Assert.Equal(new[] { f.AD, f.DB }, result.Change.EdgesToLegalize);
    }

    [Fact]
    public void CanFlipUsesGeometryPredicates()
    {
        Fixture f = CreateFixture();
        var geometry = new StubGeometry
        {
            Convex = true,
            InsideCircumcircle = true
        };
        var flipper = new EdgeFlipper(geometry);

        bool canFlip = flipper.CanFlip(f.AB, out bool shouldFlip);

        Assert.True(canFlip);
        Assert.True(shouldFlip);
    }

    [Fact]
    public void CanFlipRejectsIneligibleEdge()
    {
        Fixture f = CreateFixture();
        f.AB.Constrain(EdgeConstraintKind.Feature);
        var flipper = new EdgeFlipper(new StubGeometry());

        bool canFlip = flipper.CanFlip(f.AB, out bool shouldFlip);

        Assert.False(canFlip);
        Assert.False(shouldFlip);
    }

    static void AssertTriangleRing(Face face, Edge first)
    {
        Edge second = first.Next;
        Edge third = second.Next;

        Assert.Same(first, face.Edge);
        Assert.Same(first, third.Next);
        Assert.Same(third, first.Prev);
        Assert.Same(first, second.Prev);
        Assert.Same(second, third.Prev);
        Assert.Same(face, first.Face);
        Assert.Same(face, second.Face);
        Assert.Same(face, third.Face);
    }

    static Fixture CreateFixture()
    {
        var a = new Node();
        var b = new Node();
        var c = new Node();
        var d = new Node();

        var ab = new Edge();
        var bc = new Edge();
        var ca = new Edge();
        var ba = new Edge();
        var ad = new Edge();
        var db = new Edge();
        var top = new Face();
        var bottom = new Face();

        Linker.LinkTwins(ab, ba);
        Linker.LinkTriangle(top, ab, bc, ca, a, b, c);
        Linker.LinkTriangle(bottom, ba, ad, db, b, a, d);

        Edge[] outer = [bc, ca, ad, db];
        Edge[] outerTwins = outer.Select(_ => new Edge()).ToArray();
        for (int i = 0; i < outer.Length; i++)
        {
            Linker.LinkTwins(outer[i], outerTwins[i]);
        }

        return new Fixture(
            ab, ba, bc, ca, ad, db,
            top, bottom, outer, outerTwins,
            a, b, c, d);
    }

    sealed record Fixture(
        Edge AB,
        Edge BA,
        Edge BC,
        Edge CA,
        Edge AD,
        Edge DB,
        Face Top,
        Face Bottom,
        Edge[] Outer,
        Edge[] OuterTwins,
        Node A,
        Node B,
        Node C,
        Node D);
}
