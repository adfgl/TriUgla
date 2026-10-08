namespace TriUgla.MeshEditor;

public sealed partial class EditorMeshModel
{
    public ElementHit? Find(double x, double y, double tolerance)
    {
        var point = new Vec2(x, y);
        MeshElement? element = _mesher.Find(point);
        if (element is Face face && tolerance > 0)
        {
            Node? node = face.Edges.Select(edge => edge.NodeStart)
                .Where(candidate => candidate.Position.Distance(point) <= tolerance)
                .OrderBy(candidate => candidate.Position.DistanceSquared(point))
                .FirstOrDefault();
            if (node is not null) element = _mesher.Find(node.Position);
            else
            {
                Edge? edge = face.Edges
                    .Select(candidate => (Edge: candidate, Distance: DistanceToSegment(point, candidate)))
                    .Where(candidate => candidate.Distance <= tolerance)
                    .OrderBy(candidate => candidate.Distance)
                    .Select(candidate => candidate.Edge)
                    .FirstOrDefault();
                if (edge is not null)
                    element = _mesher.Find(Vec2.Lerp(edge.NodeStart.Position, edge.NodeEnd.Position, .5));
            }
        }

        return element switch
        {
            Node node => new ElementHit("node", Id(node), null, null),
            Edge edge => new ElementHit("edge", null, Id(edge.NodeStart), Id(edge.NodeEnd)),
            Face foundFace => new ElementHit(
                "face",
                _mesher.Traversal.Faces().Where(candidate => !candidate.Dead).ToList().IndexOf(foundFace),
                null,
                null),
            _ => null
        };
    }

    Constraint? FindConstraint(int startId, int endId)
    {
        foreach (Constraint constraint in _mesher.Constraints)
        {
            foreach (ConstraintSpan span in constraint.Spans)
            {
                var spanEdges = new List<Edge>();
                try { span.Edges(spanEdges); }
                catch (InvalidOperationException) { continue; }

                if (spanEdges.Any(edge =>
                {
                    int a = Id(edge.NodeStart);
                    int b = Id(edge.NodeEnd);
                    return a == startId && b == endId || a == endId && b == startId;
                })) return constraint;
            }
        }
        return null;
    }

    IReadOnlyList<EdgeView> ConstraintEdges(Constraint constraint)
    {
        var result = new List<EdgeView>();
        foreach (ConstraintSpan span in constraint.Spans)
        {
            var edges = new List<Edge>();
            try { span.Edges(edges); }
            catch (InvalidOperationException) { continue; }
            result.AddRange(edges.Select(edge => new EdgeView(Id(edge.NodeStart), Id(edge.NodeEnd))));
        }
        return result;
    }

    static double DistanceToSegment(Vec2 point, Edge edge)
    {
        Vec2 start = edge.NodeStart.Position;
        Vec2 segment = edge.NodeEnd.Position - start;
        double lengthSquared = segment.LengthSquared;
        if (lengthSquared == 0) return point.Distance(start);
        double amount = Math.Clamp((point - start).Dot(segment) / lengthSquared, 0, 1);
        return point.Distance(start + segment * amount);
    }
}
