namespace TriUgla;

public abstract class FaceRankAspect
{
    public double Weight { get; set; } = 1d;
    public bool Enabled => Weight > 0d;

    public abstract double Violation01(Face face, in FaceStats stats);
}
