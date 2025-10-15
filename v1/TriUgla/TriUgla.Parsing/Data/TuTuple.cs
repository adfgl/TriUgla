using System.Collections;

namespace TriUgla.Parsing.Data
{
    public class TuTuple : TuObject, IEnumerable<double>
    {
        public TuTuple(IEnumerable<double> values)
        {
            Values = values.ToList();
        }

        public TuTuple(IEnumerable<double> values, Func<double, double> fun)
        {
            Values = new List<double>();
            foreach (var value in values)
            {
                Values.Add(fun(value));
            }
        }

        public List<double> Values { get; set; }

        public override string ToString()
        {
            return $"<{string.Join(", ", Values)}>";
        }

        public IEnumerator<double> GetEnumerator()
        {
            foreach (var item in Values)
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
            return new TuTuple(Values);
        }
    }
}
