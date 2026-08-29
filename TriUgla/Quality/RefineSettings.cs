namespace TriUgla;

public readonly record struct RefineSettings(
    int MaxSteiners,
    int FaceStagnationBudget,
    double ImproveEps,
    bool ContinueOnFaceStagnation = false,
    bool UseSteinerBudget = true)
{
    /// <summary>
    /// When true, refinement does not suppress faces whose measured quality has
    /// stopped improving. Cancellation, and the optional Steiner budget, remain
    /// the explicit safety stops.
    /// </summary>
    public bool ContinueOnFaceStagnation { get; init; } = ContinueOnFaceStagnation;

    /// <summary>
    /// Enables the hard <see cref="MaxSteiners"/> insertion limit. It is disabled
    /// by default so normal termination follows robust geometric predicates and
    /// face-progress stability rather than an arbitrary mesh-size budget.
    /// </summary>
    public bool UseSteinerBudget { get; init; } = UseSteinerBudget;

    public static readonly RefineSettings Default = new(
        1_000_000,
        8,
        1e-4,
        false,
        false);
}
