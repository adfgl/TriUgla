namespace TriUgla;

public readonly record struct InsertNodeResult(
    Node? Node,
    LocateResult Location,
    FaceSplitResult? FaceSplit,
    EdgeSplitResult? EdgeSplit,
    InsertNodeStatus Status)
{
    public static InsertNodeResult ExistingNodeDataUpdated(
        Node node,
        LocateResult location)
        => new(node, location, null, null, InsertNodeStatus.ExistingNodeDataUpdated);

    public static InsertNodeResult InsertedIntoFace(
        Node node,
        LocateResult location,
        FaceSplitResult split)
        => new(node, location, split, null, InsertNodeStatus.InsertedIntoFace);

    public static InsertNodeResult InsertedIntoEdge(
        Node node,
        LocateResult location,
        EdgeSplitResult split)
        => new(node, location, null, split, InsertNodeStatus.InsertedIntoEdge);

    public static InsertNodeResult Outside(LocateResult location)
        => new(null, location, null, null, InsertNodeStatus.Outside);
}
