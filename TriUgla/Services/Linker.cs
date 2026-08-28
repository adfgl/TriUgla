namespace TriUgla;

public static class Linker
{
    public static void LinkTwins(Edge first, Edge second)
    {
        first.Twin = second;
        second.Twin = first;
    }

    public static void LinkNodeEdge(Node node, Edge edge)
    {
        edge.NodeStart = node;
        node.Edge = edge;
    }

    public static void LinkEdges(Edge first, Edge next)
    {
        first.Next = next;
        next.Prev = first;
    }

    public static void LinkFaceEdges(Face face, Edge first, Edge second, Edge third)
    {
        face.Edge = first;
        first.Face = face;
        second.Face = face;
        third.Face = face;
    }

    public static void LinkTriangle(
        Face face,
        Edge firstEdge,
        Edge secondEdge,
        Edge thirdEdge,
        Node firstNode,
        Node secondNode,
        Node thirdNode)
    {
        LinkNodeEdge(firstNode, firstEdge);
        LinkNodeEdge(secondNode, secondEdge);
        LinkNodeEdge(thirdNode, thirdEdge);

        LinkEdges(firstEdge, secondEdge);
        LinkEdges(secondEdge, thirdEdge);
        LinkEdges(thirdEdge, firstEdge);

        LinkFaceEdges(face, firstEdge, secondEdge, thirdEdge);
    }
}
