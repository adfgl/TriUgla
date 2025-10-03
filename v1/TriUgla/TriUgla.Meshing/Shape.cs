using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Geometry;

namespace TriUgla.Meshing
{
    public class Shape
    {
        public Polygon<Node> Body { get; set; }
        public List<Polygon<Node>> Holes { get; set; } = new List<Polygon<Node>>();

        public bool Contains(double x, double y, double eps)
        {
            if (Body.Contains(x, y, eps))
            {
                foreach (var node in Holes)
                {

                }
            }
            return false;
        }
    }
}
