using System.Collections;

namespace TriUgla.Parsing.Data
{
    public class TuTuple : TuObject, IEnumerable<TuValue>
    {
        readonly List<TuValue> _values;

        public TuTuple(int capacity = 4)
        {
            _values = new List<TuValue>(Math.Max(capacity, 4));
        }

        public IReadOnlyList<TuValue> Values => _values;

        public int Count => _values.Count;

        public TuValue this[int index]
        {
            get => _values[index];
            set
            {
                _values[index] = value;
            }
        }

        public void Add(TuValue value)
        {
            _values.Add(value);
        }

        public void Remove(TuValue value)
        {
            if (value.type == EDataType.List ||
                value.type == EDataType.Range)
            {
                TuTuple values = value.AsTuple();
                for (int i = _values.Count - 1; i >= 0; i--)
                {
                    TuValue v = _values[i];
                    foreach (var other in values)
                    {
                        if (other == v)
                        {
                            _values.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            else
            {
                _values.Remove(value);
            }
        }

        public override string ToString()
        {
            return $"<{string.Join(", ", _values)}>";
        }

        public IEnumerator<TuValue> GetEnumerator()
        {
            foreach (TuValue item in _values)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override TuTuple Clone()
        {
            TuTuple tpl = new TuTuple(_values.Count);
            foreach (TuValue item in _values)
            {
                tpl._values.Add(item.Copy());
            }
            return tpl;
        }
    }
}
