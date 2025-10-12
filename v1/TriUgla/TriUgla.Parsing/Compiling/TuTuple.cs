using System.Collections;

namespace TriUgla.Parsing.Compiling
{
    public class TuTuple : TuObject, IEnumerable<double>
    {
        public TuTuple(IEnumerable<double> values)
        {
            Values = values.ToList();
        }

        public List<double> Values { get; set; }

        public override string ToString()
        {
            return $"<{String.Join(", ", Values)}>";
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
