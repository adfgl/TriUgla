namespace TriUgla;

/// <summary>
/// Provides geometric predicates for mesh operations.
/// </summary>
public interface IGeometry
{
    /// <summary>
    /// Classifies two closed segments: -1 disjoint, 0 endpoint/tangent contact,
    /// 1 proper crossing, and 2 collinear overlap.
    /// </summary>
    int Intersects(Vec2 p1, Vec2 p2, Vec2 q1, Vec2 q2)
        => Intersection.Intersect(p1, p2, q1, q2, out _) ? 1 : -1;

    /// <summary>
    /// Finds the orientation of a point relative to the directed line from a to b.
    /// </summary>
    EOrientaiton Orient(Node a, Node b, Vec2 point);

    /// <summary>
    /// Finds the orientation of a point relative to an edge.
    /// </summary>
    EOrientaiton Orient(Edge edge, Vec2 point);

    /// <summary>
    /// Checks whether a point lies inside the circle with ab as its diameter.
    /// </summary>
    bool InDiameterCircle(Node a, Node b, Vec2 point);

    /// <summary>
    /// Checks whether a point lies inside the circumcircle of triangle abc.
    /// </summary>
    bool InCircumcircle(Node a, Node b, Node c, Vec2 point);

    /// <summary>
    /// Checks whether a quad is convex.
    /// </summary>
    bool IsConvexQuad(Quad quad);
}
