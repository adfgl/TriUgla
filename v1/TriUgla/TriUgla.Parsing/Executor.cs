using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public class Executor
    {
        public TuValue Execute(string code, int timeoutSec = 30)
        {
            Scanner scanner = new Scanner(code);
            Parser parser = new Parser(scanner);
            TuRuntime stack = new TuRuntime();
            stack.Budget.SetTimeLimit(TimeSpan.FromSeconds(timeoutSec));

            var program = parser.Parse();
            TuValue result = program.Evaluate(stack);
            return result;
        }
    }
}
