namespace TriUgla.Parsing.Compiling
{
    public class TuTuple : TuObject
    {
        public TuTuple(IEnumerable<double> values)
        {
            Values = values.ToArray();
        }

        public double[] Values { get; set; }

        public override string ToString()
        {
            return String.Join(", ", Values);
        }
    }
}
