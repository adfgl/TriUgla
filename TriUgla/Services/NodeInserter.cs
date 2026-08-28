namespace TriUgla;

public sealed class NodeInserter(
    INodeFactory nodes,
    ISplitter splitter,
    IMeshLocator locator) : INodeInserter
{
    public InsertNodeResult Insert(
        Vec2 position,
        ElementData? incomingData = null,
        Face? from = null)
    {
        LocateResult location = locator.Locate(position, from);

        if (location.Node is not null)
        {
            return UpdateExistingNode(location.Node, position, location, incomingData);
        }

        if (location.Edge is not null)
        {
            return InsertIntoEdge(location.Edge, position, location, incomingData);
        }

        if (location.Face is not null)
        {
            return InsertIntoFace(location.Face, position, location, incomingData);
        }

        return InsertNodeResult.Outside(location);
    }

    InsertNodeResult UpdateExistingNode(
        Node existing,
        Vec2 position,
        LocateResult location,
        ElementData? incomingData)
    {
        existing.Data = nodes.CreateData(position, location, incomingData);
        InsertNodeResult result = InsertNodeResult.ExistingNodeDataUpdated(existing, location);
        existing.Data?.AfterInserted(existing, result);
        return result;
    }

    InsertNodeResult InsertIntoEdge(
        Edge edge,
        Vec2 position,
        LocateResult location,
        ElementData? incomingData)
    {
        Node node = nodes.Create(position, location, incomingData);
        EdgeSplitResult split = splitter.Split(edge, node);
        InsertNodeResult result = InsertNodeResult.InsertedIntoEdge(node, location, split);
        node.Data?.AfterInserted(node, result);
        return result;
    }

    InsertNodeResult InsertIntoFace(
        Face face,
        Vec2 position,
        LocateResult location,
        ElementData? incomingData)
    {
        Node node = nodes.Create(position, location, incomingData);
        FaceSplitResult split = splitter.Split(face, node);
        InsertNodeResult result = InsertNodeResult.InsertedIntoFace(node, location, split);
        node.Data?.AfterInserted(node, result);
        return result;
    }
}
