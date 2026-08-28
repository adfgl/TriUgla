namespace TriUgla;

public interface IMesh
{
    Node? Insert(Vec2 position);
    bool Remove(Vec2 position);

    IEnumerable<Face> Faces();
    IEnumerable<Edge> Edges();
    IEnumerable<Node> Nodes();
}