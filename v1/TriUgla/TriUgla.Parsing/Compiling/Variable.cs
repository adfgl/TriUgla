using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Compiling
{
    public class Variable
    {
        public Variable(string id)
        {
            Identifier = id;
        }

        public int Line { get; set; }
        public int Column { get; set; }
        public string Identifier { get; }
        public TuValue Value { get; set; } = TuValue.Nothing;

        public override string ToString()
        {
            return $"{Identifier} = {Value}";
        }
    }
}
