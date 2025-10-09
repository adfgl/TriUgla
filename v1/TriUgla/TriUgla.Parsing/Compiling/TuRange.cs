using System.Collections;

namespace TriUgla.Parsing.Compiling
{
    public class TuRange : TuObject, IEnumerable<double>
    {
        readonly double _from, _to, _by;
        double _current;

        public TuRange(double from, double to, double by = 1)
        {
            int steps = (int)((_from - _to) / _by);
            if (steps < 0) throw new Exception("Ivalid range.");

            _from =  from;
            _to = to;
            _by = by;

            Reset();
        }

        public double From { get; }
        public double To { get; }
        public double By { get; }
        public double Current => _current;

        bool s_first = true;

        public void Reset()
        {
            _current = _from;
            s_first = true;
        }

        public bool Next()
        {
            if (s_first)
            {
                s_first = false;
                return true;
            }
            double next = _current + _by;
            if (next >= _to)
            {
                return false;
            }

            _current = next;
            return true;
        }

        public IEnumerator<double> GetEnumerator()
        {
            while (Next())
            {
                yield return _current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return string.Join(", ", this) ;
        }

        public override TuRange Clone()
        {
            return new TuRange(_from, _to, _by);
        }
    }
}
