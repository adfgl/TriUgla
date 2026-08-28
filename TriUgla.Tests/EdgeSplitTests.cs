namespace TriUgla.Tests;

public class EdgeSplitTests
{
    [Fact]
    public void SplitCreatesFourTrianglesAndPreservesOuterTwins()
    {
        Fixture f = CreateFixture();
        var center = new Node();
        var queue = new LegalizationQueue();

        EdgeSplitResult result = new Splitter(queue).Split(f.Target, center);

        AssertTriangle(result.CAe, f.Outer[0], f.C, f.A, center);
        AssertTriangle(result.BCe, f.Outer[1], f.B, f.C, center);
        AssertTriangle(result.ADe, f.Outer[2], f.A, f.D, center);
        AssertTriangle(result.DBe, f.Outer[3], f.D, f.B, center);

        Assert.Same(f.Target, result.FirstHalf);
        Assert.Same(f.Twin, result.FirstHalf.Twin);
        Assert.Same(result.FirstHalf, f.Twin.Twin);
        Assert.Same(result.SecondHalf, result.SecondHalf.Twin!.Twin);

        for (int i = 0; i < f.Outer.Length; i++)
        {
            Assert.Same(f.OuterTwins[i], f.Outer[i].Twin);
            Assert.Same(f.Outer[i], f.OuterTwins[i].Twin);
        }

        Assert.Equal(f.Outer, [queue.Take(), queue.Take(), queue.Take(), queue.Take()]);
    }

    [Fact]
    public void SplitRejectsBoundaryEdge()
    {
        var edge = new Edge();

        var error = Assert.Throws<InvalidOperationException>(
            () => new Splitter(new LegalizationQueue()).Split(edge, new Node()));

        Assert.Contains("boundary edge without a twin", error.Message);
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
