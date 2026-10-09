namespace TriUgla;

public readonly record struct RefineSettings(
    int MaxSteiners,
    int FaceStagnationBudget,
    double ImproveEps,
    bool ContinueOnFaceStagnation = false,
    bool UseSteinerBudget = true,
    bool RefineLand = true,
    bool RefineLakes = false,
    int UnchangedFailureBudget = 1)
{
    /// <summary>
    /// Retained for source compatibility. Refinement no longer uses face-object
    /// quality trends as a termination condition.
    /// </summary>
    public bool ContinueOnFaceStagnation { get; init; } = ContinueOnFaceStagnation;

    /// <summary>
    /// Enables the hard <see cref="MaxSteiners"/> insertion limit. It is disabled
    /// by default so normal termination follows robust geometric predicates and
    /// face-progress stability rather than an arbitrary mesh-size budget.
    /// </summary>
    public bool UseSteinerBudget { get; init; } = UseSteinerBudget;

    /// <summary>Includes classified land/island faces in quality refinement.</summary>
    public bool RefineLand { get; init; } = RefineLand;

    /// <summary>Includes classified lake faces in quality refinement.</summary>
    public bool RefineLakes { get; init; } = RefineLakes;

    /// <summary>
    /// Number of retries allowed when the same geometric face fails without any
    /// intervening topology change.
    /// </summary>
    public int UnchangedFailureBudget { get; init; } = UnchangedFailureBudget;

    public static readonly RefineSettings Default = new(
        1_000_000,
        8,
        1e-4,
        false,
        false,
        true,
        false,
        1);
}
