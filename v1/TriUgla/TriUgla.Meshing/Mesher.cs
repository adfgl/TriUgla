using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TriUgla.Geometry;

namespace TriUgla.Meshing
{

    public class Mesher
    {
        public const int CONSTRAINT_USER = 3;
        public const int CONSTRAINT_HOLE = 2;
        public const int CONSTRAINT_BODY = 1;


        public Mesher(List<Vertex> vertices, List<int> body, List<List<int>> holes, List<(int, int)> edges, double eps)
        {
            Rectangle bounds = Rectangle.FromPoints(vertices, o => o.X, o => o.Y);
            QuadTree<Node> qt = new QuadTree<Node>(bounds, vertices.Count);


        }

        public static Mesh Triangulate(List<Vertex> vertices, List<int> body, List<List<int>> holes, List<(int, int)> edges, double eps)
        {
            Rectangle bounds = Rectangle.FromPoints(vertices, o => o.X, o => o.Y);

            Mesh mesh = new Mesh();
            mesh = SuperStructure.AddTriangle(mesh, bounds, 2);



            return mesh;
        }

       


    }
}
