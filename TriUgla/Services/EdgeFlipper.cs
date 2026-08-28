namespace TriUgla;

public sealed class EdgeFlipper(IGeometry geometry) : IEdgeFlipper
{
    public EdgeFlipResult Flip(Edge edge)
    {
        Edge cd = edge;
        Edge dc = cd.Twin
            ?? throw new InvalidOperationException(
                "Cannot flip a boundary edge without a twin.");

        if (!ReferenceEquals(dc.Twin, cd))
        {
            throw new InvalidOperationException(
                "Cannot flip an edge whose twin link is not reciprocal.");
        }

        if (cd.OrTwinConstrained)
        {
            throw new InvalidOperationException("Cannot flip a constrained edge.");
        }

        EnsureTriangle(cd);
        EnsureTriangle(dc);

        Edge bc = cd.Next;
        Edge ca = cd.Prev;
        Edge ad = dc.Next;
        Edge db = dc.Prev;

        Node a = cd.NodeStart;
        Node b = dc.NodeStart;
        Node c = ca.NodeStart;
        Node d = db.NodeStart;

        Face adc = cd.Face;
        Face dbc = dc.Face;

        Linker.LinkTriangle(adc, ad, dc, ca, a, d, c);
        Linker.LinkTriangle(dbc, db, bc, cd, d, b, c);

        return new EdgeFlipResult(cd, [adc, dbc], [ad, db]);
    }

    public bool CanFlip(Edge edge, out bool shouldFlip)
    {
        shouldFlip = false;

        Edge? twin = edge.Twin;
        if (edge.OrTwinConstrained ||
            twin is null ||
            !ReferenceEquals(twin.Twin, edge) ||
            !IsTriangle(edge) ||
            !IsTriangle(twin))
        {
            return false;
        }

        Quad quad = Quad.From(edge);
        if (!geometry.IsConvexQuad(quad))
        {
            return false;
        }

        shouldFlip = geometry.InCircumcircle(
            quad.A,
            quad.C,
            quad.D,
            quad.B.Position);
        return true;
    }

    static void EnsureTriangle(Edge first)
    {
        if (!IsTriangle(first))
        {
            throw new InvalidOperationException(
                "EdgeFlipper requires triangular faces with valid edge cycles.");
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
