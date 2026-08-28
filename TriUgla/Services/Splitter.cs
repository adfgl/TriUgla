namespace TriUgla;

public sealed class Splitter(IDataInterpolator? dataInterpolator = null) : ISplitter
{
    public FaceSplitResult Split(Face target, Node node)
    {
        Edge ab = target.Edge;
        EnsureTriangle(ab);
        EnsureDataCanBeTransferred(target);

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

        TransferData(abd, bcd);
        TransferData(abd, cad);

        return new FaceSplitResult(
            [abd, bcd, cad],
            [ab, bc, ca]);
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
        EnsureDataCanBeTransferred(ab, ba, ab.Face, ba.Face);

        Edge bc = ab.Next;
        Edge ca = ab.Prev;
        Edge ad = ba.Next;
        Edge db = ba.Prev;

        Node a = ab.NodeStart;
        Node b = ba.NodeStart;
        Node c = ca.NodeStart;
        Node d = db.NodeStart;
        Node e = node;

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

        TransferData(cae, bce);
        TransferData(ade, dbe);
        TransferData(ae, eb);
        TransferData(ea, be);

        return new EdgeSplitResult(
            ae,
            eb,
            [cae, bce, ade, dbe],
            [ca, bc, ad, db]);
    }

    static (Edge First, Edge Second) CreateTwins()
    {
        var first = new Edge();
        var second = new Edge();
        Linker.LinkTwins(first, second);
        return (first, second);
    }

    void EnsureDataCanBeTransferred(params MeshElement[] sources)
    {
        if (dataInterpolator is null && sources.Any(source => source.Data is not null))
        {
            throw new InvalidOperationException(
                "Cannot transfer element data without an IDataInterpolator.");
        }
    }

    void TransferData(MeshElement source, MeshElement destination)
    {
        if (source.Data is not null)
        {
            destination.Data = dataInterpolator!.From(source.Data);
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
