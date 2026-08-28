namespace TriUgla;

public sealed class FaceSplitter(LegalizationQueue illegalEdges) : IFaceSplitter
{
    public FaceSplitResult Split(Face target, Node node)
    {
        Edge ab = target.Edge;
        Edge bc = ab.Next;
        Edge ca = ab.Prev;

        if (!ReferenceEquals(bc.Next, ca) ||
            !ReferenceEquals(ca.Next, ab) ||
            !ReferenceEquals(bc.Prev, ab) ||
            !ReferenceEquals(ca.Prev, bc))
        {
            throw new InvalidOperationException(
                "FaceSplitter requires a triangular face with a valid three-edge cycle.");
        }

        Node a = ab.NodeStart;
        Node b = bc.NodeStart;
        Node c = ca.NodeStart;
        Node d = node;

        Edge bd = new();
        Edge db = new();
        Edge cd = new();
        Edge dc = new();
        Edge da = new();
        Edge ad = new();

        Linker.LinkTwins(bd, db);
        Linker.LinkTwins(cd, dc);
        Linker.LinkTwins(da, ad);

        Face abd = target;
        Face bcd = new();
        Face cad = new();

        Linker.LinkTriangle(abd, ab, bd, da, a, b, d);
        Linker.LinkTriangle(bcd, bc, cd, db, b, c, d);
        Linker.LinkTriangle(cad, ca, ad, dc, c, a, d);

        illegalEdges.Add(ab);
        illegalEdges.Add(bc);
        illegalEdges.Add(ca);

        return new FaceSplitResult(abd, bcd, cad);
    }
}
