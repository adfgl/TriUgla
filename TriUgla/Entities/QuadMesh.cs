namespace TriUgla;

/// <summary>A detached quad-dominant mesh with index-based rendering data.</summary>
public sealed class QuadMesh
{
    public Mesh Mesh { get; }
    public IReadOnlyList<QuadMeshNode> Nodes { get; }
    public IReadOnlyList<QuadMeshFace> Quads { get; }
    public IReadOnlyList<QuadMeshTriangle> Triangles { get; }
    public IReadOnlyList<QuadMeshConstraint> Constraints { get; }
    public int EdgeFlips { get; }

    internal QuadMesh(Mesh mesh, QuadMeshNode[] nodes, QuadMeshFace[] quads,
        QuadMeshTriangle[] triangles, QuadMeshConstraint[] constraints, int edgeFlips)
    {
        Mesh = mesh;
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
