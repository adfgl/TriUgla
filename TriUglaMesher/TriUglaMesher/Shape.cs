namespace TriUglaMesher
{
    public class Shape
    {
        public Shape(Polygon polygon)
        {
            Polygon = polygon;
        }

        public int Material {  get; set; }
        public bool Hole { get; set; }
        public Polygon Polygon { get; set; }
        public List<Shape> Children { get; set; } = new List<Shape>();

        public Shape? Contained(double x, double y, double eps)
        {
            if (!Polygon.Contains(x, y, eps))
                return null;

            foreach (Shape child in Children)
            {
                Shape? hit = child.Contained(x, y, eps);
                if (hit != null)
                    return hit;
            }
            return Hole ? null : this;
        }
    }
}
