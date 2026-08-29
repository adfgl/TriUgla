namespace TriUgla;

public sealed class NodeFactory : INodeFactory
{
    public Node Create(Vec2 position, LocateResult location)
        => new() { Position = WithInterpolatedMetadata(position, location) };

    static Vec2 WithInterpolatedMetadata(Vec2 position, LocateResult location)
    {
        if (location.Node is not null)
        {
            Vec2 source = location.Node.Position;
            return new(position.X, position.Y, source.Z, source.W);
        }

        if (location.Edge is not null)
        {
            Vec2 first = location.Edge.NodeStart.Position;
            Vec2 second = location.Edge.NodeEnd.Position;
            Barycentric weights = Barycentric.FromSegment(position, first, second);
            Vec2 value = weights.Interpolate(first, second, Vec2.Zero);
            return new(position.X, position.Y, value.Z, value.W);
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
            Vec2 value = weights.Interpolate(
                nodes[0].Position,
                nodes[1].Position,
                nodes[2].Position);
            return new(position.X, position.Y, value.Z, value.W);
        }

        return position;
    }
}
