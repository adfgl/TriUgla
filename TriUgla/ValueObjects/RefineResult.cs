namespace TriUgla;

public readonly record struct RefineResult(
    RefineStatus Status,
    int InsertedNodes,
    int RemainingBadFaces,
    int RemainingEncroachedSegments,
    string? FailureReason)
{
    public bool Completed => Status == RefineStatus.Completed;
}
