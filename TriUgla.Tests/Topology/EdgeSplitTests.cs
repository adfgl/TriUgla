namespace TriUgla.Tests;

public class EdgeSplitTests
{
    [Fact]
    public void SplitCreatesFourTrianglesAndPreservesOuterTwins()
    {
        Fixture f = CreateFixture();
        var center = new Node();

        EdgeSplitResult result = new Splitter().Split(f.Target, center);

        Assert.Equal(4, result.Change.AffectedFaces.Count);
        AssertTriangle(result.Change.AffectedFaces[0], f.Outer[0], f.C, f.A, center);
        AssertTriangle(result.Change.AffectedFaces[1], f.Outer[1], f.B, f.C, center);
        AssertTriangle(result.Change.AffectedFaces[2], f.Outer[2], f.A, f.D, center);
        AssertTriangle(result.Change.AffectedFaces[3], f.Outer[3], f.D, f.B, center);

        Assert.Same(f.Target, result.FirstHalf);
        Assert.Same(f.Twin, result.FirstHalf.Twin);
        Assert.Same(result.FirstHalf, f.Twin.Twin);
        Assert.Same(result.SecondHalf, result.SecondHalf.Twin!.Twin);

        for (int i = 0; i < f.Outer.Length; i++)
        {
            Assert.Same(f.OuterTwins[i], f.Outer[i].Twin);
            Assert.Same(f.Outer[i], f.OuterTwins[i].Twin);
        }

        Assert.Equal(f.Outer, result.Change.EdgesToLegalize);
    }

    [Fact]
    public void SplitBoundaryEdgeCreatesTwoTriangles()
    {
        Node a = new();
        Node b = new();
        Node c = new();
        Node midpoint = new();
        Edge ab = new();
        Edge bc = new();
        Edge ca = new();
        Linker.LinkTriangle(new Face(), ab, bc, ca, a, b, c);
        ab.Constrain(EdgeConstraintKind.Boundary);

        EdgeSplitResult result = new Splitter().Split(ab, midpoint);

        Assert.Equal(2, result.Change.AffectedFaces.Count);
        Assert.Null(result.FirstHalf.Twin);
        Assert.Null(result.SecondHalf.Twin);
        Assert.Same(a, result.FirstHalf.NodeStart);
        Assert.Same(midpoint, result.FirstHalf.NodeEnd);
        Assert.Same(midpoint, result.SecondHalf.NodeStart);
        Assert.Same(b, result.SecondHalf.NodeEnd);
        Assert.Equal(1, result.FirstHalf.ConstraintCount);
        Assert.Equal(1, result.SecondHalf.ConstraintCount);
    }

    [Fact]
    public void Split_TransmitsDirectedConstraintCountsToBothHalves()
    {
        Fixture f = CreateFixture();
        f.Target.Constrain(EdgeConstraintKind.Feature);
        f.Target.Constrain(EdgeConstraintKind.Boundary);
        f.Twin.Constrain(EdgeConstraintKind.Boundary);
        var inserted = new Node();

        EdgeSplitResult result = new Splitter().Split(f.Target, inserted);

        Assert.Equal(2, result.FirstHalf.ConstraintCount);
        Assert.Equal(2, result.SecondHalf.ConstraintCount);
        Assert.Equal(1, result.FirstHalf.FeatureConstraints);
        Assert.Equal(1, result.SecondHalf.FeatureConstraints);
        Assert.Equal(1, result.FirstHalf.BoundaryConstraints);
        Assert.Equal(1, result.SecondHalf.BoundaryConstraints);
        Assert.Equal(1, result.FirstHalf.Twin!.ConstraintCount);
        Assert.Equal(1, result.SecondHalf.Twin!.ConstraintCount);
        Assert.True(result.FirstHalf.Twin!.HasBoundary);
        Assert.True(result.SecondHalf.Twin!.HasBoundary);
        Assert.True(inserted.Constrained);
    }

    static void AssertTriangle(
        Face face,
        Edge boundary,
        Node first,
        Node second,
        Node third)
    {
        Edge[] edges = face.Edges.ToArray();

        Assert.Equal(3, edges.Length);
        Assert.Same(boundary, edges[0]);
        Assert.Same(first, edges[0].NodeStart);
        Assert.Same(second, edges[1].NodeStart);
        Assert.Same(third, edges[2].NodeStart);
        Assert.All(edges, edge => Assert.Same(face, edge.Face));
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

        Linker.LinkTwins(ab, ba);
        Linker.LinkTriangle(new Face(), ab, bc, ca, a, b, c);
        Linker.LinkTriangle(new Face(), ba, ad, db, b, a, d);

        Edge[] outer = [ca, bc, ad, db];
        Edge[] outerTwins = outer.Select(_ => new Edge()).ToArray();
        for (int i = 0; i < outer.Length; i++)
        {
            Linker.LinkTwins(outer[i], outerTwins[i]);
        }

        return new Fixture(ab, ba, outer, outerTwins, a, b, c, d);
    }

    sealed record Fixture(
        Edge Target,
        Edge Twin,
        Edge[] Outer,
        Edge[] OuterTwins,
        Node A,
        Node B,
        Node C,
        Node D);
}
