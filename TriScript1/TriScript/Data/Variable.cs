using TriScript.UnitHandling;

namespace TriScript.Data
{
    public class Variable
    {
        public Variable(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public Value Value { get; set; } = Value.Nothing;
        public DimEval Eval { get; set; } = DimEval.None;
    }
}
