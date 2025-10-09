using TriUgla.Parsing.Compiling.RuntimeObjects;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Compiling
{
    public class Variable
    {
        public Token Identifier { get; set; }
        public TuValue Value { get; set; }
    }
}
