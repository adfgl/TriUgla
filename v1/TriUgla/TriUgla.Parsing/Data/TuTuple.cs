using System.Collections;

namespace TriUgla.Parsing.Data
{
    public class TuTuple : TuObject, IEnumerable<TuValue>
    {
        readonly List<TuValue> _values;
        EDataType _type = EDataType.Nothing;

        public TuTuple(int capacity = 4)
        {
            _values = new List<TuValue>(Math.Max(capacity, 4));
        }

        void EnsureCompatibility(in TuValue value)
        {
            if (!TuValue.Compatible(_type, value.type))
            {
                throw new Exception();
            }
        }

        public EDataType Type => _type;

        public IReadOnlyList<TuValue> Values => _values;

        public int Count => _values.Count;

        public TuValue this[int index]
        {
            get => _values[index];
            set
            {
                EnsureCompatibility(in value);
                _values[index] = value;
            }
        }

        public void Add(TuValue value)
        {
            if (value.type == EDataType.Nothing)
                throw new InvalidOperationException("Cannot add 'Nothing' value to the collection.");

            switch (value.type)
            {
                case EDataType.Tuple:
                case EDataType.Range:
                    TuTuple tpl = value.AsTuple();
                    if (tpl.Count == 0) return;
                    EnsureElementType(tpl.Type);
                    _values.AddRange(tpl);
                    return;

                default:
                    EnsureElementType(value.type);
                    _values.Add(value);
                    return;
            }
        }

        void EnsureElementType(EDataType incoming)
        {
            if (_type == EDataType.Nothing)
            {
                _type = incoming;
                return;
            }

            if (_type != incoming)
            {
                throw new InvalidOperationException(
                    $"Type mismatch: cannot add '{incoming}' to a collection of '{_type}'.");
            }
        }

        public void Remove(TuValue value)
        {
            if (value.type == EDataType.Tuple ||
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
