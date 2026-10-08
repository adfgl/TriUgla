namespace TriUgla;

/// <summary>An immutable, index-based quad-dominant representation of a mesh.</summary>
public sealed class QuadMesh
{
    public IReadOnlyList<QuadMeshNode> Nodes { get; }
    public IReadOnlyList<QuadMeshFace> Quads { get; }
    public IReadOnlyList<QuadMeshTriangle> Triangles { get; }
    public IReadOnlyList<QuadMeshConstraint> Constraints { get; }
    public int EdgeFlips { get; }

    internal QuadMesh(QuadMeshNode[] nodes, QuadMeshFace[] quads,
        QuadMeshTriangle[] triangles, QuadMeshConstraint[] constraints, int edgeFlips)
    {
        Nodes = Array.AsReadOnly(nodes);
        Quads = Array.AsReadOnly(quads);
        Triangles = Array.AsReadOnly(triangles);
        Constraints = Array.AsReadOnly(constraints);
        EdgeFlips = edgeFlips;
    }

    public static QuadMesh From(Mesh mesh) => new QuadMeshBuilder().Build(mesh);

    public static QuadMesh From(Mesh mesh, in QuadMeshSettings settings)
        => new QuadMeshBuilder().Build(mesh, settings);
}
