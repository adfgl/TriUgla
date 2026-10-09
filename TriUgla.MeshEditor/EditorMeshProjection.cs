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
                Id(node), node.Position.X, node.Position.Y,
                node.Data.Elevation, node.Data.Area,
                structure?.SuperNode(node) == true, node.Kind.ToString(),
                LogicalConstraintCount(node))).ToArray();
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
        ConstraintView[] constraints = _constraints.Select((constraint, index) =>
        {
            var edges = new List<Edge>();
            foreach (ConstraintLine span in constraint.Lines)
            {
                try { span.Edges(edges); }
                catch (InvalidOperationException) { }
            }
            EdgeView[] views = edges.Select(edge =>
                new EdgeView(Id(edge.NodeStart), Id(edge.NodeEnd))).ToArray();
            int start = constraint.Lines.Count > 0 ? Id(constraint.Lines[0].From) : -1;
            int end = constraint.Lines.Count > 0 ? Id(constraint.Lines[^1].To) : -1;
            return new ConstraintView(
                index,
                constraint.Name ?? $"Constraint {index}",
                constraint.Lines.Count,
                views.Length,
                edges.Sum(edge => edge.Length),
                start,
                end,
                views);
        }).ToArray();
        var view = new MeshView(
            nodes, faces, boundaryEdges, loopEdges, constraintEdges, constraints,
            succeeded, changedNodeId, _undo.Count > 0, _redo.Count > 0,
            succeeded ? null : _failureReason);
        _failureReason = null;
        return view;
    }

    int LogicalConstraintCount(Node node)
    {
        int features = _constraints.Count(constraint =>
            constraint.Points.Any(point => ReferenceEquals(point.Node, node)) ||
            constraint.Lines.Any(line => LineContains(line, node)));
        int boundaries = _mesher.Constraints.Loops.Count(loop =>
            loop.Nodes.Any(candidate => ReferenceEquals(candidate, node)));
        return features + boundaries;
    }

    static bool LineContains(ConstraintLine span, Node node)
    {
        if (ReferenceEquals(span.From, node) || ReferenceEquals(span.To, node)) return true;
        try { return span.Edges([]).Any(edge => edge.Contains(node)); }
        catch (InvalidOperationException) { return false; }
    }

    void Classify()
    {
        SuperStructure? structure = _mesher.SuperStructure;
        if (structure is not null)
            new FaceClassifier(_mesher.Mesh, _mesher.Traversal, structure).Classify();
    }

    public QuadOverlayView QuadState()
    {
        Classify();
        QuadMesh mesh = QuadMesh.From(_mesher.Mesh);
        return new QuadOverlayView(
            mesh.Nodes.Select(node => new QuadNodeView(
                node.Position.X, node.Position.Y, node.Kind.ToString())).ToArray(),
            mesh.Quads.Select(face => new QuadFaceView(
                face.Indices.A, face.Indices.B, face.Indices.C, face.Indices.D,
                face.Kind.ToString())).ToArray(),
            mesh.Triangles.Select(face => new QuadTriangleView(
                face.Indices.A, face.Indices.B, face.Indices.C,
                face.Kind.ToString())).ToArray(),
            mesh.EdgeFlips);
    }
}
