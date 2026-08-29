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
        Face bcd = new();
        Face cad = new();

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
        Edge ba = ab.Twin
            ?? throw new InvalidOperationException(
                "Cannot split a boundary edge without a twin.");

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

        int forwardConstraints = ab.ConstraintCount;
        int reverseConstraints = ba.ConstraintCount;
        RemoveConstraints(ab, forwardConstraints);
        RemoveConstraints(ba, reverseConstraints);

        Edge ae = ab;
        Edge ea = ba;
        (Edge eb, Edge be) = CreateTwins();
        (Edge ec, Edge ce) = CreateTwins();
        (Edge ed, Edge de) = CreateTwins();

        Face cae = ab.Face;
        Face ade = ba.Face;
        Face bce = new();
        Face dbe = new();

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

    static (Edge First, Edge Second) CreateTwins()
    {
        var first = new Edge();
        var second = new Edge();
        Linker.LinkTwins(first, second);
        return (first, second);
    }

    static void RemoveConstraints(Edge edge, int count)
    {
        for (int index = 0; index < count; index++)
        {
            edge.Relax();
        }
    }

    static void ApplyConstraints(Edge first, Edge second, int count)
    {
        for (int index = 0; index < count; index++)
        {
            first.Constrain();
            second.Constrain();
        }
    }

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
