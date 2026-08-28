namespace TriUgla;

public interface IEdgeLegalizer
{
    EdgeLegalizationResult Legalize(Queue<Edge> illegalEdges);
}
