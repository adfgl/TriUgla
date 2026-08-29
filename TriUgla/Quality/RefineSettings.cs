namespace TriUgla;

public readonly record struct RefineSettings(
    int MaxSteiners,
    int FaceStagnationBudget,
    double ImproveEps)
{
    public static readonly RefineSettings Default = new(
        1_000_000,
        8,
        1e-4);
}
