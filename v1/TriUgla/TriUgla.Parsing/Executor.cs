using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public class Executor
    {

        public TuValue Execute(string code)
        {
            Scanner scanner = new Scanner(code);
            Parser parser = new Parser(scanner);
            TuStack stack = new TuStack();

            var program = parser.Parse();
            TuValue result = program.Evaluate(stack);
            return result;
        }
    }
}
