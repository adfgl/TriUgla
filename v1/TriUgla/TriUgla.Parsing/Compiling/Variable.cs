using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Compiling
{
    public class Variable
    {
        public string Identifier { get; set; } = String.Empty;
        public TuValue Value { get; set; }

        public override string ToString()
        {
            return $"{Identifier} = {Value}";
        }
    }
}
