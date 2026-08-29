namespace TriUgla;

public interface IEdgeInserter
{
    EdgeInsertResult Insert(
        Node start,
        Node end,
        EdgeConstraintKind kind = EdgeConstraintKind.Feature);
}
