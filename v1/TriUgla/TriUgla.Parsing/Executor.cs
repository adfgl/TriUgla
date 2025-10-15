using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public class Executor
    {
        public TuValue Execute(string code, int timeoutSec = 5)
        {
            Scanner scanner = new Scanner(code);
            Parser parser = new Parser(scanner);
            TuRuntime stack = new TuRuntime();
            stack.Budget.SetTimeLimit(TimeSpan.FromSeconds(timeoutSec));
            stack.Budget.SetStepBudget(long.MaxValue);

            var program = parser.Parse();
            TuValue result = program.Evaluate(stack);

            if (stack.Budget.Aborted)
            {
                string msg = stack.Budget.Reason switch
                {
                    Runtime.RuntimeBudget.EStopReason.None => "Execution stopped (no specific reason).",
                    Runtime.RuntimeBudget.EStopReason.StepLimit => "Execution stopped: step limit reached.",
                    Runtime.RuntimeBudget.EStopReason.Timeout => "Execution stopped: time limit exceeded.",
                    Runtime.RuntimeBudget.EStopReason.Cancellation => "Execution cancelled by external request.",
                    Runtime.RuntimeBudget.EStopReason.FullStop => "Execution stopped manually (full stop).",
                    _ => "Execution stopped: unknown reason."
                };
                return new TuValue(msg);
            }
            return result;
        }
    }
}
