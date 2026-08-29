namespace TriUgla;

public readonly record struct RefineSettings(
    int MaxSteiners,
    int FaceStagnationBudget,
    double ImproveEps,
    bool ContinueOnFaceStagnation = false)
{
    /// <summary>
    /// When true, refinement does not suppress faces whose measured quality has
    /// stopped improving. Cancellation and <see cref="MaxSteiners"/> remain the
    /// explicit safety stops.
    /// </summary>
    public bool ContinueOnFaceStagnation { get; init; } = ContinueOnFaceStagnation;

    public static readonly RefineSettings Default = new(
        1_000_000,
        8,
        1e-4,
        false);
}
