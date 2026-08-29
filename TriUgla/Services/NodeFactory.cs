namespace TriUgla;

public sealed class NodeFactory : INodeFactory
{
    public Node Create(Vec2 position, LocateResult location)
        => new()
        {
            Position = position,
            Data = InterpolateData(position, location)
        };

    static NodeData InterpolateData(Vec2 position, LocateResult location)
    {
        if (location.Node is not null)
        {
            return location.Node.Data;
        }

        if (location.Edge is not null)
        {
            Vec2 first = location.Edge.NodeStart.Position;
            Vec2 second = location.Edge.NodeEnd.Position;
            Barycentric weights = Barycentric.FromSegment(position, first, second);
            return weights.Interpolate(
                location.Edge.NodeStart.Data,
                location.Edge.NodeEnd.Data,
                default);
        }

        if (location.Face is not null)
        {
            Node[] nodes = location.Face.Edges.Select(edge => edge.NodeStart).ToArray();
            if (nodes.Length != 3)
            {
                throw new InvalidOperationException(
                    "NodeFactory requires triangular faces for metadata interpolation.");
            }
            Barycentric weights = Barycentric.From(
                position,
                nodes[0].Position,
                nodes[1].Position,
                nodes[2].Position);
            return weights.Interpolate(nodes[0].Data, nodes[1].Data, nodes[2].Data);
        }

        return default;
    }
}
