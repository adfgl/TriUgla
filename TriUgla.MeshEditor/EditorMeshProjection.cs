namespace TriUgla.MeshEditor;

public sealed partial class EditorMeshModel
{
    MeshView Snapshot(bool succeeded, int? changedNodeId)
    {
        SuperStructure? structure = _mesher.SuperStructure;
        Classify();
        MeshSnapshot snapshot = _mesher.Traversal.Snapshot();
        NodeView[] nodes = snapshot.Nodes.Where(node => !node.Dead)
            .Select(node => new NodeView(
                Id(node), node.Position.X, node.Position.Y, structure?.SuperNode(node) == true)).ToArray();
        FaceView[] faces = snapshot.Faces.Where(face => !face.Dead)
            .Select(face =>
            {
                int[] ids = face.Edges.Select(edge => Id(edge.NodeStart)).ToArray();
                return new FaceView(
                    ids[0], ids[1], ids[2],
                    structure?.SuperFace(face) == true,
                    face.Kind.ToString());
            }).ToArray();
        EdgeView[] boundaryEdges = snapshot.Edges
            .Where(edge => !edge.Dead && edge.Twin is null)
            .Select(edge => new EdgeView(Id(edge.NodeStart), Id(edge.NodeEnd))).ToArray();
        EdgeView[] loopEdges = snapshot.Edges
            .Where(edge => !edge.Dead && (edge.HasBoundary || edge.Twin?.HasBoundary == true))
            .Select(edge => new EdgeView(Id(edge.NodeStart), Id(edge.NodeEnd)))
            .DistinctBy(edge => edge.A < edge.B ? (edge.A, edge.B) : (edge.B, edge.A))
            .ToArray();
        EdgeView[] constraintEdges = snapshot.Edges
            .Where(edge => !edge.Dead && (edge.HasFeature || edge.Twin?.HasFeature == true))
            .Select(edge => new EdgeView(Id(edge.NodeStart), Id(edge.NodeEnd)))
            .DistinctBy(edge => edge.A < edge.B ? (edge.A, edge.B) : (edge.B, edge.A))
            .ToArray();
        ConstraintView[] constraints = _mesher.Constraints.Select((constraint, index) =>
        {
            var edges = new List<Edge>();
            foreach (ConstraintSpan span in constraint.Spans)
            {
                try { span.Edges(edges); }
                catch (InvalidOperationException) { }
            }
            EdgeView[] views = edges.Select(edge =>
                new EdgeView(Id(edge.NodeStart), Id(edge.NodeEnd))).ToArray();
            int start = constraint.Spans.Count > 0 ? Id(constraint.Spans[0].From) : -1;
            int end = constraint.Spans.Count > 0 ? Id(constraint.Spans[^1].To) : -1;
            return new ConstraintView(
                index,
                constraint.Name ?? $"Constraint {index}",
                constraint.Spans.Count,
                views.Length,
                edges.Sum(edge => edge.Length),
                start,
                end,
                views);
        }).ToArray();
        return new MeshView(
            nodes, faces, boundaryEdges, loopEdges, constraintEdges, constraints,
            succeeded, changedNodeId, _undo.Count > 0, _redo.Count > 0);
    }

    void Classify()
    {
        SuperStructure? structure = _mesher.SuperStructure;
        if (structure is not null)
            new FaceClassifier(_mesher.Mesh, _mesher.Traversal, structure).Classify();
    }
}
