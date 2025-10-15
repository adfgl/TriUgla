using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Runtime
{
    public class Variable
    {
        public Variable(Token id)
        {
            Identifier = id;
        }

        public Token Identifier { get; }
        public TuValue Value { get; set; } = TuValue.Nothing;

        public override string ToString()
        {
            return $"{Identifier.value}";
        }
    }
}
