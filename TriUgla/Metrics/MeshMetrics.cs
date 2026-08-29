namespace TriUgla;

public sealed class MeshMetrics
{
    public MeshMetric<Edge> Angle { get; set; } = new();
    public MeshMetric<Edge> EdgeLength { get; set; } = new();
    public MeshMetric<Face> FaceArea { get; set; } = new();

    public override string ToString()
        => $"""
            Mesh metrics
            ------------
            Angle       : {Angle.ToString("°")}
            Edge length : {EdgeLength.ToString()}
            Face area   : {FaceArea.ToString()}
            """;
}
