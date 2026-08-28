namespace TriUgla;

public sealed class NodeFactory(IDataInterpolator dataInterpolator) : INodeFactory
{
    public Node Create(
        Vec2 position,
        LocateResult location,
        ElementData? incomingData = null)
        => new()
        {
            Position = position,
            Data = CreateData(position, location, incomingData)
        };

    public ElementData? CreateData(
        Vec2 position,
        LocateResult location,
        ElementData? incomingData = null)
    {
        if (incomingData is not null)
        {
            return dataInterpolator.From(incomingData);
        }

        if (location.Node is not null)
        {
            return Copy(location.Node.Data);
        }

        if (location.Edge is not null)
        {
            return FromEdge(position, location.Edge);
        }

        if (location.Face is not null)
        {
            return FromFace(position, location.Face);
        }

        return null;
    }

    ElementData? FromEdge(Vec2 position, Edge edge)
    {
        Node first = edge.NodeStart;
        Node second = edge.NodeEnd;

        if (first.Data is not null && second.Data is not null)
        {
            return dataInterpolator.Between(
                first.Data,
                second.Data,
                EdgeAmount(position, first.Position, second.Position));
        }

        return CopyNearest(position, first, second);
    }

    ElementData? FromFace(Vec2 position, Face face)
    {
        Node[] nodes = face.Edges.Select(edge => edge.NodeStart).ToArray();
        if (nodes.Length != 3)
        {
            throw new InvalidOperationException(
                "NodeFactory requires triangular faces for data interpolation.");
        }

        if (nodes.All(node => node.Data is not null))
        {
            Barycentric weights = Barycentric.From(
                position,
                nodes[0].Position,
                nodes[1].Position,
                nodes[2].Position);

            return dataInterpolator.Between(
                nodes[0].Data!,
                nodes[1].Data!,
                nodes[2].Data!,
                weights);
        }

        return CopyNearest(position, nodes);
    }

    ElementData? CopyNearest(Vec2 position, params Node[] nodes)
    {
        Node? nearest = nodes
            .Where(node => node.Data is not null)
            .MinBy(node => node.Position.DistanceSquared(position));

        return Copy(nearest?.Data);
    }

    ElementData? Copy(ElementData? data)
        => data is null ? null : dataInterpolator.From(data);

    static double EdgeAmount(Vec2 point, Vec2 start, Vec2 end)
    {
        Vec2 direction = end - start;
        if (direction.LengthSquared == 0)
        {
            return 0;
        }

        return Math.Clamp((point - start).Dot(direction) / direction.LengthSquared, 0, 1);
    }
}
