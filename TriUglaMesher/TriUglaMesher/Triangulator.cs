using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUglaMesher
{
    public class Triangulator
    {
        public void Triangulate(List<Segment> polygon, List<List<Segment>> holes, List<Segment> edges, List<SegmentNode> points)
        {
            Polygon shape = new Polygon(ToPoints(polygon));
            List<Polygon> shapeHoles = new List<Polygon>();
            List<SegmentLine> constraints = new List<SegmentLine>();

            foreach (Polygon hole in holes.Select(o => new Polygon(ToPoints(o))))
            {
                if (!shape.Intersects(hole)) continue;


            }
        }

        public static List<IPoint> ToPoints(IEnumerable<Segment> segments)
        {
            List<IPoint> points = new List<IPoint>();
            foreach (Segment segment in segments)
            {
                IReadOnlyList<Segment> sub = segment.Split();
                points.Add(sub[0].Start);
                foreach (Segment sub2 in sub)
                {
                    points.Add(sub2.End);
                }
            }
            return points;
        }
    }
}
