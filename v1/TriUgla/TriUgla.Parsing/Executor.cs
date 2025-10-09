using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Compiling.RuntimeObjects;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public class Executor
    {
        Evaluator _evaluator = new Evaluator();

        public Value Execute(string code)
        {
            Scanner scanner = new Scanner(code);
            Parser parser = new Parser(scanner);

            NodeProgram program = parser.Parse();
            return program.Accept(_evaluator);
        }
    }
}
