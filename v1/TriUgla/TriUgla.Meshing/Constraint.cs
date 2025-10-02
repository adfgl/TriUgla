using TriUgla.Geometry;

namespace TriUgla.Meshing
{
    public class Constraint
    {
        public Constraint(Node start, Node end, ConstraintProperties props = default)
        {
            Start = start;
            End = end;
            Properties = props;
        }

        public ConstraintProperties Properties { get; set; }
        public Node Start { get; set; }
        public Node End { get; set; }

        public List<Constraint> Split(Node node, double eps)
        {
            if (Node.CloseOrEqual(Start, node, eps) ||
                Node.CloseOrEqual(End, node, eps) ||
                !GeometryHelper.PointOnSegment(Start, End, node.X, node.Y, eps))
            {
                return [this];
            }

            return [
                new Constraint(Start, node, Properties),
                new Constraint(node, End, Properties)
            ];
        }

        public List<Constraint> Split(Constraint other, double eps)
        {
            List<Constraint> result = Split(other.Start, eps);
            if (result.Count > 1)
            {
                result.Add(other);
                return result;
            }

            result = Split(other.End, eps);
            if (result.Count > 1)
            {
                result.Add(other);
                return result;
            }

            if (GeometryHelper.Intersect(Start, End, other.Start, other.End, out double x, out double y))
            {
                double seed1 = Node.Interpolate(Start, End, x, y);
                double seed2 = Node.Interpolate(other.Start, other.End, x, y);
                double seed = (seed1 + seed2) * 0.5;

                Node inter = new Node(x, y, seed);
                result = Split(inter, eps);
                result.AddRange(other.Split(inter, eps));
                return result;
            }
            return [this, other];
        }
    }
}
