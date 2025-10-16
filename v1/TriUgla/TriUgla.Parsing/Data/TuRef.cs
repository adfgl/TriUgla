using TriUgla.Parsing.Runtime;

namespace TriUgla.Parsing.Data
{
    public class TuRef : TuObject
    {
        public TuRef(Variable var)
        {
            Variable = var;
        }

        public Variable Variable { get; }

        public override string ToString()
        {
            return $"ref: {Variable.Name}";
        }

        public override TuRef Clone()
        {
            return new TuRef(Variable);
        }
    }
}
