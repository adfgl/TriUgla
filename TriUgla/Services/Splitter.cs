namespace TriUgla;

public sealed class Splitter : ISplitter
{
    public FaceSplitResult Split(Face target, Node node)
    {
        Edge ab = target.Edge;
        EnsureTriangle(ab);

        Edge bc = ab.Next;
        Edge ca = ab.Prev;

        Node a = ab.NodeStart;
        Node b = bc.NodeStart;
        Node c = ca.NodeStart;
        Node d = node;

        (Edge bd, Edge db) = CreateTwins();
        (Edge cd, Edge dc) = CreateTwins();
        (Edge da, Edge ad) = CreateTwins();

        Face abd = target;
        Face bcd = new() { Kind = target.Kind };
        Face cad = new() { Kind = target.Kind };

        Linker.LinkTriangle(abd, ab, bd, da, a, b, d);
        Linker.LinkTriangle(bcd, bc, cd, db, b, c, d);
        Linker.LinkTriangle(cad, ca, ad, dc, c, a, d);

        return new FaceSplitResult(new TopologyChange(
            [abd, bcd, cad],
            [ab, bc, ca]));
    }

    public EdgeSplitResult Split(Edge target, Node node)
    {
        Edge ab = target;
        if (ab.Twin is null)
        {
            return SplitBoundary(ab, node);
        }

        Edge ba = ab.Twin;

        if (!ReferenceEquals(ba.Twin, ab))
        {
            throw new InvalidOperationException(
                "Cannot split an edge whose twin link is not reciprocal.");
        }

        EnsureTriangle(ab);
        EnsureTriangle(ba);

        Edge bc = ab.Next;
        Edge ca = ab.Prev;
        Edge ad = ba.Next;
        Edge db = ba.Prev;

        Node a = ab.NodeStart;
        Node b = ba.NodeStart;
        Node c = ca.NodeStart;
        Node d = db.NodeStart;
        Node e = node;

        ConstraintCounts forwardConstraints = Counts(ab);
        ConstraintCounts reverseConstraints = Counts(ba);
        RemoveConstraints(ab, forwardConstraints);
        RemoveConstraints(ba, reverseConstraints);

        Edge ae = ab;
        Edge ea = ba;
        (Edge eb, Edge be) = CreateTwins();
        (Edge ec, Edge ce) = CreateTwins();
        (Edge ed, Edge de) = CreateTwins();

        Face cae = ab.Face;
        Face ade = ba.Face;
        Face bce = new() { Kind = cae.Kind };
        Face dbe = new() { Kind = ade.Kind };

        Linker.LinkTriangle(cae, ca, ae, ec, c, a, e);
        Linker.LinkTriangle(bce, bc, ce, eb, b, c, e);
        Linker.LinkTriangle(ade, ad, de, ea, a, d, e);
        Linker.LinkTriangle(dbe, db, be, ed, d, b, e);

        ApplyConstraints(ae, eb, forwardConstraints);
        ApplyConstraints(ea, be, reverseConstraints);

        return new EdgeSplitResult(
            ae,
            eb,
            new TopologyChange(
                [cae, bce, ade, dbe],
                [ca, bc, ad, db]));
    }

    static EdgeSplitResult SplitBoundary(Edge ab, Node node)
    {
        EnsureTriangle(ab);

        Edge bc = ab.Next;
        Edge ca = ab.Prev;
        Node a = ab.NodeStart;
        Node b = bc.NodeStart;
        Node c = ca.NodeStart;
        ConstraintCounts constraints = Counts(ab);
        RemoveConstraints(ab, constraints);

        Edge ae = ab;
        Edge eb = new();
        (Edge ec, Edge ce) = CreateTwins();
        Face cae = ab.Face;
        Face bce = new() { Kind = cae.Kind };

        Linker.LinkTriangle(cae, ca, ae, ec, c, a, node);
        Linker.LinkTriangle(bce, bc, ce, eb, b, c, node);
        ApplyConstraints(ae, eb, constraints);

        return new EdgeSplitResult(
            ae,
            eb,
            new TopologyChange([cae, bce], [ca, bc]));
    }

    static (Edge First, Edge Second) CreateTwins()
    {
        var first = new Edge();
        var second = new Edge();
        Linker.LinkTwins(first, second);
        return (first, second);
    }

    static ConstraintCounts Counts(Edge edge)
        => new(edge.FeatureConstraints, edge.BoundaryConstraints);

    static void RemoveConstraints(Edge edge, ConstraintCounts counts)
    {
        for (int index = 0; index < counts.Features; index++)
            edge.Release(EdgeConstraintKind.Feature);
        for (int index = 0; index < counts.Boundaries; index++)
            edge.Release(EdgeConstraintKind.Boundary);
    }

    static void ApplyConstraints(Edge first, Edge second, ConstraintCounts counts)
    {
        for (int index = 0; index < counts.Features; index++)
        {
            first.Constrain(EdgeConstraintKind.Feature);
            second.Constrain(EdgeConstraintKind.Feature);
        }
        for (int index = 0; index < counts.Boundaries; index++)
        {
            first.Constrain(EdgeConstraintKind.Boundary);
            second.Constrain(EdgeConstraintKind.Boundary);
        }
    }

    readonly record struct ConstraintCounts(int Features, int Boundaries);

    static void EnsureTriangle(Edge first)
    {
        if (!IsTriangle(first))
        {
            throw new InvalidOperationException(
                "Splitter requires triangular faces with valid edge cycles.");
        }
    }

    static bool IsTriangle(Edge first)
    {
        Edge second = first.Next;
        Edge third = first.Prev;

        return ReferenceEquals(second.Next, third) &&
            ReferenceEquals(third.Next, first) &&
            ReferenceEquals(second.Prev, first) &&
            ReferenceEquals(third.Prev, second);
    }
}
