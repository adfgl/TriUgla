namespace TriUgla.Tests;

public class EdgeSplitTests
{
    [Fact]
    public void SplitCreatesFourTrianglesAndPreservesOuterTwins()
    {
        Fixture f = CreateFixture();
        var center = new Node();

        EdgeSplitResult result = new Splitter().Split(f.Target, center);

        Assert.Equal(4, result.AffectedFaces.Count);
        AssertTriangle(result.AffectedFaces[0], f.Outer[0], f.C, f.A, center);
        AssertTriangle(result.AffectedFaces[1], f.Outer[1], f.B, f.C, center);
        AssertTriangle(result.AffectedFaces[2], f.Outer[2], f.A, f.D, center);
        AssertTriangle(result.AffectedFaces[3], f.Outer[3], f.D, f.B, center);

        Assert.Same(f.Target, result.FirstHalf);
        Assert.Same(f.Twin, result.FirstHalf.Twin);
        Assert.Same(result.FirstHalf, f.Twin.Twin);
        Assert.Same(result.SecondHalf, result.SecondHalf.Twin!.Twin);

        for (int i = 0; i < f.Outer.Length; i++)
        {
            Assert.Same(f.OuterTwins[i], f.Outer[i].Twin);
            Assert.Same(f.Outer[i], f.OuterTwins[i].Twin);
        }

        Assert.Equal(f.Outer, result.EdgesToLegalize);
    }

    [Fact]
    public void SplitRejectsBoundaryEdge()
    {
        var edge = new Edge();

        var error = Assert.Throws<InvalidOperationException>(
            () => new Splitter().Split(edge, new Node()));

        Assert.Contains("boundary edge without a twin", error.Message);
    }

    [Fact]
    public void Split_CopiesFaceAndHalfEdgeDataToNewElements()
    {
        Fixture f = CreateFixture();
        f.Target.Face.Data = new TestData("top", 0, null);
        f.Twin.Face.Data = new TestData("bottom", 0, null);
        f.Target.Data = new TestData("forward", 0, null);
        f.Twin.Data = new TestData("reverse", 0, null);

        EdgeSplitResult result = new Splitter(new TestInterpolator())
            .Split(f.Target, new Node());

        Assert.Equal("top", GetColor(result.AffectedFaces[0]));
        Assert.Equal("top", GetColor(result.AffectedFaces[1]));
        Assert.Equal("bottom", GetColor(result.AffectedFaces[2]));
        Assert.Equal("bottom", GetColor(result.AffectedFaces[3]));
        Assert.Equal("forward", GetColor(result.SecondHalf));
        Assert.Equal("reverse", GetColor(result.SecondHalf.Twin!));
        Assert.NotSame(f.Target.Data, result.SecondHalf.Data);
        Assert.NotSame(f.Twin.Data, result.SecondHalf.Twin!.Data);
    }

    [Fact]
    public void Split_TransmitsDirectedConstraintCountsToBothHalves()
    {
        Fixture f = CreateFixture();
        f.Target.Constrain();
        f.Target.Constrain();
        f.Twin.Constrain();
        var inserted = new Node();

        EdgeSplitResult result = new Splitter().Split(f.Target, inserted);

        Assert.Equal(2, result.FirstHalf.ConstraintCount);
        Assert.Equal(2, result.SecondHalf.ConstraintCount);
        Assert.Equal(1, result.FirstHalf.Twin!.ConstraintCount);
        Assert.Equal(1, result.SecondHalf.Twin!.ConstraintCount);
        Assert.True(inserted.Constrained);
    }

    static string GetColor(MeshElement element)
        => Assert.IsType<TestData>(element.Data).Color;

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
