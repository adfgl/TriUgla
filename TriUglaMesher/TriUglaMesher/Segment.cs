using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUglaMesher
{
    public abstract class Segment
    {
        public abstract SegmentNode Start { get; }
        public abstract SegmentNode End { get; }

        public abstract SegmentNode PointAt(double t);

        public abstract double Length { get; }

        public int NumSegments { get; set; } = 1;

        public abstract IReadOnlyList<Segment> Split(int parts);
        public IReadOnlyList<Segment> Split() => Split(Math.Max(1, NumSegments));
    }
}
