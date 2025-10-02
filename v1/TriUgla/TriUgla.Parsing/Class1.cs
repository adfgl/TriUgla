using System.Data;
using System.Xml.Linq;

namespace TriUgla.Parsing
{
    public class Class1
    {

    }
}



////public Mesher(List<Vertex> vertices, List<int> body, List<List<int>> holes, List<(int, int)> edges, double eps)
////{
////    Rectangle bounds = Rectangle.FromPoints(vertices, o => o.X, o => o.Y);

////    QuadTree<Node> qt = new QuadTree<Node>(bounds, vertices.Count);

////    Polygon<Node> bodyPoly = BuildPolygon(qt, vertices, body, eps);
////    List<Polygon<Node>> holesPoly = new List<Polygon<Node>>(holes.Count);
////    foreach (List<int> hole in holes)
////    {
////        holesPoly.Add(BuildPolygon(qt, vertices, hole, eps));
////    }
////    RemoveInvalidHoles(bodyPoly, holesPoly, eps);

////    List<Constraint> constraints = new List<Constraint>(edges.Count);
////    foreach ((Node a, Node b) in bodyPoly.GetEdges())
////    {
////        Constraint constraint = new Constraint(a, b, new ConstraintProperties(CONSTRAINT_BODY));
////        constraints.Add(constraint);
////    }

////    foreach (Polygon<Node> hole in holesPoly)
////    {
////        foreach ((Node a, Node b) in hole.GetEdges())
////        {
////            Constraint constraint = new Constraint(a, b, new ConstraintProperties(CONSTRAINT_HOLE));
////            double midX = (a.X + b.X) * 0.5;
////            double midY = (a.Y + b.Y) * 0.5;

////            if (!bodyPoly.Contains(midX, midY, eps))
////            {
////                continue;
////            }

////            bool shouldAdd = true;
////            foreach (Polygon<Node> otherHole in holesPoly)
////            {
////                if (otherHole == hole) continue;

////                if (otherHole.Contains(midX, midY, eps))
////                {
////                    shouldAdd = false;
////                    break;
////                }
////            }

////            if (shouldAdd)
////            {
////                constraints.Add(constraint);
////            }
////        }
////    }

////    foreach ((int start, int end) in edges)
////    {
////        Node a = AddNode(qt, vertices[start], eps);
////        Node b = AddNode(qt, vertices[end], eps);

////        Constraint constraint = new Constraint(a, b, new ConstraintProperties(CONSTRAINT_HOLE));
////        double midX = (a.X + b.X) * 0.5;
////        double midY = (a.Y + b.Y) * 0.5;
////        if (Contains(bodyPoly, holesPoly, midX, midY, eps))
////        {
////            constraints.Add(constraint);
////        }
////    }

////}

//static void RemoveInvalidHoles(Polygon<Node> body, List<Polygon<Node>> holes, double eps)
//{
//    for (int i = holes.Count - 1; i >= 0; i++)
//    {
//        Polygon<Node> hole = holes[i];
//        if (!body.Contains(hole, eps) && !body.Intersects(hole))
//        {
//            holes.RemoveAt(i);
//        }
//    }
//}


//static bool Contains(Polygon<Node> body, List<Polygon<Node>> holes, double x, double y, double eps)
//{
//    if (!body.Contains(x, y, eps))
//    {
//        return false;
//    }

//    foreach (Polygon<Node> hole in holes)
//    {
//        if (hole.Contains(x, y, eps))
//        {
//            return false;
//        }
//    }
//    return true;
//}

//Polygon<Node> BuildPolygon(QuadTree<Node> qt, List<Vertex> vertices, List<int> indices, double eps)
//{
//    List<Node> unique = new List<Node>(indices.Count);
//    foreach (int idx in indices)
//    {
//        unique.Add(AddNode(qt, vertices[idx], eps));
//    }
//    return new Polygon<Node>(unique);
//}

//Node AddNode(QuadTree<Node> qt, Vertex vtx, double eps)
//{
//    double x = vtx.X;
//    double y = vtx.Y;
//    double seed = vtx.Seed;

//    Node? existing = qt.TryGet(x, y, eps);
//    if (existing is null)
//    {
//        existing = new Node(x, y, seed)
//        {
//            Index = qt.Items.Count
//        };
//    }
//    return existing;
//}

//void AddConstraint(List<Constraint> all, Constraint constraint, double eps)
//{
//    Queue<Constraint> queue = new Queue<Constraint>();
//    queue.Enqueue(constraint);

//    while (queue.Count > 0)
//    {
//        Constraint toInsert = queue.Dequeue();

//        bool wasSplit = false;
//        for (int i = 0; i < all.Count; i++)
//        {
//            Constraint existing = all[i];
//            List<Constraint> split = existing.Split(toInsert, eps);
//            if (split.Count <= 2) continue;

//            wasSplit = true;
//            all.RemoveAt(i);

//            foreach (var p in split)
//            {
//                if (ReferenceEquals(p, toInsert))
//                    continue;

//                if (ReferenceEquals(p, existing))
//                {
//                    all.Insert(i, p);
//                    i++;
//                }
//                else
//                {
//                    queue.Enqueue(p);
//                }
//            }

//            i = -1;
//        }

//        if (!wasSplit)
//        {
//            all.Add(constraint);
//        }
//    }
//}