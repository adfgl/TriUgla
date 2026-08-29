namespace TriUgla;

public sealed class NodeInserter(
    INodeFactory nodes,
    ISplitter splitter,
    IMeshLocator locator) : INodeInserter
{
    public InsertNodeResult Insert(Vec2 position, Face? from = null)
    {
        LocateResult location = locator.Locate(position, from);

        if (location.Node is not null)
        {
            return UpdateExistingNode(location.Node, position, location);
        }

        if (location.Edge is not null)
        {
            return InsertIntoEdge(location.Edge, position, location);
        }

        if (location.Face is not null)
        {
            return InsertIntoFace(location.Face, position, location);
        }

        return InsertNodeResult.Outside(location);
    }

    InsertNodeResult UpdateExistingNode(
        Node existing,
        Vec2 position,
        LocateResult location)
    {
        existing.Position = nodes.Create(position, location).Position;
        return InsertNodeResult.ExistingNodeDataUpdated(existing, location);
    }

    InsertNodeResult InsertIntoEdge(
        Edge edge,
        Vec2 position,
        LocateResult location)
    {
        Node node = nodes.Create(position, location);
        EdgeSplitResult split = splitter.Split(edge, node);
        InsertNodeResult result = InsertNodeResult.InsertedIntoEdge(node, location, split);
        return result;
    }

    InsertNodeResult InsertIntoFace(
        Face face,
        Vec2 position,
        LocateResult location)
    {
        Node node = nodes.Create(position, location);
        FaceSplitResult split = splitter.Split(face, node);
        InsertNodeResult result = InsertNodeResult.InsertedIntoFace(node, location, split);
        return result;
    }
}
