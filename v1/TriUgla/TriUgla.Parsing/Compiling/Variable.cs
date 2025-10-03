using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Compiling
{
    public class Variable
    {
        public Token Identifier { get; set; }
        public Value Value { get; set; }

        public override string ToString()
        {
            return Identifier.value.text!;
        }
    }
}
