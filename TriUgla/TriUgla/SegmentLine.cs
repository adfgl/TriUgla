namespace TriUgla
{
    public class SegmentLine : Segment
    {
        SegmentNode _start, _end;

        public SegmentLine(SegmentNode start, SegmentNode end)
        {
            _start = start;
            _end = end;
        }

        public SegmentLine(double x0, double y0, double x1, double y1) :
            this(new SegmentNode(x0, y0), new SegmentNode(x1, y1))
        {
            
        }

        public override SegmentNode Start => _start;
        public override SegmentNode End => _end;

        public override SegmentNode PointAt(double t)
        {
            return new SegmentNode(
                _start.X + t * (_end.X - _start.X),
                _start.Y + t * (_end.Y - _start.Y));
        }

        public override double Length => SegmentNode.Distance(_start, _end);

        public override IReadOnlyList<SegmentLine> Split(int parts)
        {
            if (parts <= 0) return Array.Empty<SegmentLine>();

            SegmentLine[] result = new SegmentLine[parts];
            double inv = 1.0 / parts;

            SegmentNode prev = PointAt(0.0); 
            for (int i = 0; i < parts; i++)
            {
                double t1 = (i + 1) * inv;
                SegmentNode next = PointAt(t1);
                result[i] = new SegmentLine(prev, next);
                prev = next;
            }
            return result;
        }
    }
}
