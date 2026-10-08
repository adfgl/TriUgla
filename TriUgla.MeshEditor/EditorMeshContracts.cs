namespace TriUgla.MeshEditor;

public sealed record MeshView(
    IReadOnlyList<NodeView> Nodes,
    IReadOnlyList<FaceView> Faces,
    IReadOnlyList<EdgeView> BoundaryEdges,
    IReadOnlyList<EdgeView> LoopEdges,
    IReadOnlyList<EdgeView> ConstraintEdges,
    IReadOnlyList<ConstraintView> Constraints,
    bool Succeeded,
    int? ChangedNodeId,
    bool CanUndo,
    bool CanRedo);

public sealed record NodeView(int Id, double X, double Y, bool IsSuper);
public sealed record FaceView(int A, int B, int C, bool IsSuper, string Kind);
public sealed record EdgeView(int A, int B);
public sealed record ConstraintView(
    int Id,
    string Name,
    int SpanCount,
    int SegmentCount,
    double Length,
    int StartNodeId,
    int EndNodeId,
    IReadOnlyList<EdgeView> Edges);
public sealed record SelectionInfo(
    string Type,
    string Title,
    IReadOnlyList<SelectionProperty> Properties);
public sealed record SelectionProperty(string Name, string Value);
public sealed record ElementHit(string Type, int? Id, int? A, int? B);
