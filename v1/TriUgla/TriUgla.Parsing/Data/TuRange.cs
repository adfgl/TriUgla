using System.Collections;

namespace TriUgla.Parsing.Data
{
    public class TuRange : TuObject, IEnumerable<TuValue>
    {
        readonly TuValue _from, _to, _by;
        TuValue _current;

        public TuRange(TuValue from, TuValue to, TuValue by)
        {
            TuValue steps = (to - from) / by;
            if (steps.AsNumeric() < 0) throw new Exception("Ivalid range.");

            _from =  from;
            _to = to;
            _by = by;

            if (by.type == EDataType.Float && from.type == EDataType.Integer)
            {
                _from = new TuValue(by.AsNumeric());
            }
            Reset();
        }

        public TuValue From { get; }
        public TuValue To { get; }
        public TuValue By { get; }
        public TuValue Current => _current;

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
            TuValue next = _current + _by;
            if ((next >= _to).AsBoolean())
            {
                return false;
            }

            _current = next;
            return true;
        }

        public IEnumerator<TuValue> GetEnumerator()
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
