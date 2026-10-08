namespace TriUgla;

public readonly record struct QuadMeshSettings(
    bool AllowEdgeFlips = true,
    int MaxEdgeFlips = 32,
    double MinimumQuadQuality = 0d)
{
    public QuadMeshSettings() : this(true, 32, 0d) { }
}
