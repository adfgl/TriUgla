namespace TriUgla;

/// <summary>
/// Provides geometric predicates for mesh operations.
/// </summary>
public interface IGeometry
{
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
