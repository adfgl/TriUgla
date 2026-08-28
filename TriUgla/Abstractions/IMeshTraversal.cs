namespace TriUgla;

public interface IMeshTraversal
{
    Face Root { get; }

    IEnumerable<Face> Faces(
        Face? from = null,
        CanTraverseAcrossEdge? canTraverse = null);

    IEnumerable<Edge> Edges(
        Face? from = null,
        CanTraverseAcrossEdge? canTraverse = null);

    IEnumerable<Node> Nodes(
        Face? from = null,
        CanTraverseAcrossEdge? canTraverse = null);

    MeshSnapshot Snapshot(
        Face? from = null,
        CanTraverseAcrossEdge? canTraverse = null);

    void ResetVisitStamps();
}
